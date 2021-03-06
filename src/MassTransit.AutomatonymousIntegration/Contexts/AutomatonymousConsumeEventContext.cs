// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace Automatonymous.Contexts
{
    using System;
    using System.Threading;
    using MassTransit;
    using MassTransit.Context;


    public class AutomatonymousConsumeEventContext<TInstance, TData> :
        ConsumeContextProxy<TData>,
        ConsumeEventContext<TInstance, TData>
        where TData : class
    {
        readonly BehaviorContext<TInstance, TData> _context;

        public AutomatonymousConsumeEventContext(BehaviorContext<TInstance, TData> context, ConsumeContext<TData> consumeContext)
            : base(consumeContext)
        {
            _context = context;
        }

        public override TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        {
            return _context.GetOrAddPayload(() => payloadFactory());
        }

        public override bool HasPayloadType(Type contextType)
        {
            return _context.HasPayloadType(contextType);
        }

        public override bool TryGetPayload<TPayload>(out TPayload context)
        {
            return _context.TryGetPayload(out context);
        }

        CancellationToken InstanceContext<TInstance>.CancellationToken => _context.CancellationToken;
        TInstance InstanceContext<TInstance>.Instance => _context.Instance;

        bool InstanceContext<TInstance>.HasPayloadType(Type contextType)
        {
            return _context.HasPayloadType(contextType);
        }

        bool InstanceContext<TInstance>.TryGetPayload<TPayload>(out TPayload payload)
        {
            return _context.TryGetPayload(out payload);
        }

        TPayload InstanceContext<TInstance>.GetOrAddPayload<TPayload>(Automatonymous.PayloadFactory<TPayload> payloadFactory)
        {
            return _context.GetOrAddPayload(payloadFactory);
        }

        Event<TData> EventContext<TInstance, TData>.Event => _context.Event;
        TData EventContext<TInstance, TData>.Data => _context.Data;
        Event EventContext<TInstance>.Event => _context.Event;
    }


    public class AutomatonymousConsumeEventContext<TInstance> :
        ConsumeContextProxy,
        ConsumeEventContext<TInstance>
    {
        readonly BehaviorContext<TInstance> _context;

        public AutomatonymousConsumeEventContext(BehaviorContext<TInstance> context, ConsumeContext consumeContext)
            : base(consumeContext)
        {
            _context = context;
        }

        public override TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        {
            return _context.GetOrAddPayload(() => payloadFactory());
        }

        public override bool HasPayloadType(Type contextType)
        {
            return _context.HasPayloadType(contextType);
        }

        public override bool TryGetPayload<TPayload>(out TPayload context)
        {
            return _context.TryGetPayload(out context);
        }

        CancellationToken InstanceContext<TInstance>.CancellationToken => _context.CancellationToken;
        TInstance InstanceContext<TInstance>.Instance => _context.Instance;

        bool InstanceContext<TInstance>.HasPayloadType(Type contextType)
        {
            return _context.HasPayloadType(contextType);
        }

        bool InstanceContext<TInstance>.TryGetPayload<TPayload>(out TPayload payload)
        {
            return _context.TryGetPayload(out payload);
        }

        TPayload InstanceContext<TInstance>.GetOrAddPayload<TPayload>(Automatonymous.PayloadFactory<TPayload> payloadFactory)
        {
            return _context.GetOrAddPayload(payloadFactory);
        }

        Event EventContext<TInstance>.Event => _context.Event;
    }
}