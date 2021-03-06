﻿// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.RabbitMqTransport.Contexts
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Integration;
    using Logging;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using Util;


    public class RabbitMqModelContext :
        ModelContext,
        IDisposable
    {
        static readonly ILog _log = Logger.Get<RabbitMqModelContext>();

        readonly ConnectionContext _connectionContext;
        readonly IModel _model;

        readonly PayloadCache _payloadCache;
        readonly ConcurrentDictionary<ulong, PendingPublish> _published;
        readonly QueuedTaskScheduler _taskScheduler;
        readonly CancellationTokenSource _tokenSource;
        CancellationTokenRegistration _registration;
        ulong _publishTagMax;

        public RabbitMqModelContext(ConnectionContext connectionContext, IModel model, CancellationToken cancellationToken)
        {
            _connectionContext = connectionContext;
            _model = model;

            _payloadCache = new PayloadCache();
            _published = new ConcurrentDictionary<ulong, PendingPublish>();
            _taskScheduler = new QueuedTaskScheduler(TaskScheduler.Default, 1);

            _tokenSource = new CancellationTokenSource();
            _registration = cancellationToken.Register(OnCancellationRequested);

            _model.ModelShutdown += OnModelShutdown;
            _model.BasicAcks += OnBasicAcks;
            _model.BasicNacks += OnBasicNacks;
            _model.BasicReturn += OnBasicReturn;
            _model.ConfirmSelect();
        }

        public void Dispose()
        {
            _registration.Dispose();

            Close("ModelContext Disposed");
        }

        bool PipeContext.HasPayloadType(Type contextType)
        {
            return _payloadCache.HasPayloadType(contextType) || _connectionContext.HasPayloadType(contextType);
        }

        bool PipeContext.TryGetPayload<TPayload>(out TPayload context)
        {
            if (_payloadCache.TryGetPayload(out context))
                return true;

            return _connectionContext.TryGetPayload(out context);
        }

        TPayload PipeContext.GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        {
            TPayload payload;
            if (_payloadCache.TryGetPayload(out payload))
                return payload;

            if (_connectionContext.TryGetPayload(out payload))
                return payload;

            return _payloadCache.GetOrAddPayload(payloadFactory);
        }

        IModel ModelContext.Model => _model;

        ConnectionContext ModelContext.ConnectionContext => _connectionContext;

        CancellationToken PipeContext.CancellationToken => _tokenSource.Token;

        async Task ModelContext.BasicPublishAsync(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties,
            byte[] body)
        {
            PendingPublish pendingPublish = await Task.Factory.StartNew(() => PublishAsync(exchange, routingKey, mandatory, immediate, basicProperties, body),
                _tokenSource.Token, TaskCreationOptions.HideScheduler, _taskScheduler);

            await pendingPublish.Task;
        }

        async Task ModelContext.ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            await Task.Factory.StartNew(() => _model.ExchangeBind(destination, source, routingKey, arguments),
                _tokenSource.Token, TaskCreationOptions.HideScheduler, _taskScheduler);
        }

        async Task ModelContext.ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        {
            await Task.Factory.StartNew(() => _model.ExchangeDeclare(exchange, type, durable, autoDelete, arguments),
                _tokenSource.Token, TaskCreationOptions.HideScheduler, _taskScheduler);
        }

        async Task ModelContext.QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            await Task.Factory.StartNew(() => _model.QueueBind(queue, exchange, routingKey, arguments),
                _tokenSource.Token, TaskCreationOptions.HideScheduler, _taskScheduler);
        }

        async Task<QueueDeclareOk> ModelContext.QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            return await Task.Factory.StartNew(() => _model.QueueDeclare(queue, durable, exclusive, autoDelete, arguments),
                _tokenSource.Token, TaskCreationOptions.HideScheduler, _taskScheduler);
        }

        async Task<uint> ModelContext.QueuePurge(string queue)
        {
            return await Task.Factory.StartNew(() => _model.QueuePurge(queue),
                _tokenSource.Token, TaskCreationOptions.HideScheduler, _taskScheduler);
        }

        async Task ModelContext.BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            await Task.Factory.StartNew(() => _model.BasicQos(prefetchSize, prefetchCount, global),
                _tokenSource.Token, TaskCreationOptions.HideScheduler, _taskScheduler);
        }

        void ModelContext.BasicAck(ulong deliveryTag, bool multiple)
        {
            _model.BasicAck(deliveryTag, multiple);
        }

        void ModelContext.BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            _model.BasicNack(deliveryTag, multiple, requeue);
        }

        async Task<string> ModelContext.BasicConsume(string queue, bool noAck, IBasicConsumer consumer)
        {
            return await Task.Factory.StartNew(() => _model.BasicConsume(queue, noAck, consumer),
                _tokenSource.Token, TaskCreationOptions.HideScheduler, _taskScheduler);
        }

        void Close(string reason)
        {
            try
            {
                if (_model.IsOpen && _published.Count > 0)
                {
                    bool timedOut;
                    _model.WaitForConfirms(TimeSpan.FromSeconds(30), out timedOut);
                    if (timedOut)
                        _log.WarnFormat("Timeout waiting for pending confirms: {0}", _connectionContext.HostSettings.ToDebugString());
                    else
                    {
                        _log.DebugFormat("Pending confirms complete: {0}", _connectionContext.HostSettings.ToDebugString());
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Fault waiting for confirms", ex);
            }

            _model.Cleanup(200, reason);
        }

        PendingPublish PublishAsync(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties, byte[] body)
        {
            ulong publishTag = _model.NextPublishSeqNo;
            _publishTagMax = Math.Max(_publishTagMax, publishTag);
            var pendingPublish = new PendingPublish(_connectionContext, exchange, publishTag);
            try
            {
                _published.AddOrUpdate(publishTag, key => pendingPublish, (key, existing) =>
                {
                    existing.PublishNotConfirmed();
                    return pendingPublish;
                });

                _model.BasicPublish(exchange, routingKey, mandatory, immediate, basicProperties, body);
            }
            catch
            {
                PendingPublish ignored;
                _published.TryRemove(publishTag, out ignored);

                throw;
            }

            return pendingPublish;
        }

        void OnBasicReturn(object model, BasicReturnEventArgs args)
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat("BasicReturn: {0}-{1} {2}", args.ReplyCode, args.ReplyText, args.BasicProperties.MessageId);
        }

        void OnModelShutdown(object model, ShutdownEventArgs reason)
        {
            _tokenSource.Cancel();

            _model.ModelShutdown -= OnModelShutdown;
            _model.BasicAcks -= OnBasicAcks;
            _model.BasicNacks -= OnBasicNacks;
            _model.BasicReturn -= OnBasicReturn;

            FaultPendingPublishes();
        }

        void FaultPendingPublishes()
        {
            try
            {
                foreach (ulong key in _published.Keys)
                {
                    PendingPublish pending;
                    if (_published.TryRemove(key, out pending))
                        pending.PublishNotConfirmed();
                }
            }
            catch (Exception)
            {
            }
        }

        void OnBasicNacks(object model, BasicNackEventArgs args)
        {
            if (args.Multiple)
            {
                ulong[] ids = _published.Keys.Where(x => x <= args.DeliveryTag).ToArray();
                foreach (ulong id in ids)
                {
                    PendingPublish value;
                    if (_published.TryRemove(id, out value))
                        value.Nack();
                }
            }
            else
            {
                PendingPublish value;
                if (_published.TryRemove(args.DeliveryTag, out value))
                    value.Nack();
            }
        }

        void OnBasicAcks(object model, BasicAckEventArgs args)
        {
            if (args.Multiple)
            {
                ulong[] ids = _published.Keys.Where(x => x <= args.DeliveryTag).ToArray();
                foreach (ulong id in ids)
                {
                    PendingPublish value;
                    if (_published.TryRemove(id, out value))
                        value.Ack();
                }
            }
            else
            {
                PendingPublish value;
                if (_published.TryRemove(args.DeliveryTag, out value))
                    value.Ack();
            }
        }

        void OnCancellationRequested()
        {
            _tokenSource.Cancel();

            Close("Transport Stopped");
        }
    }
}