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
namespace MassTransit.AutomatonymousIntegration.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Automatonymous;
    using EntityFrameworkIntegration;
    using EntityFrameworkIntegration.Saga;
    using NUnit.Framework;
    using Saga;
    using TestFramework;


    [TestFixture]
    public class When_using_EntityFramework :
        InMemoryTestFixture
    {
        SuperShopper _machine;
        readonly SagaDbContextFactory _sagaDbContextFactory;
        readonly Lazy<ISagaRepository<ShoppingChore>> _repository;

        protected override void ConfigureInputQueueEndpoint(IReceiveEndpointConfigurator configurator)
        {
            _machine = new SuperShopper();

            configurator.StateMachineSaga(_machine, _repository.Value);
        }

        public When_using_EntityFramework()
        {
            _sagaDbContextFactory =
                () => new SagaDbContext<ShoppingChore, EntityFrameworkShoppingChoreMap>(SagaDbContextFactoryProvider.GetLocalDbConnectionString());
            _repository = new Lazy<ISagaRepository<ShoppingChore>>(() => new EntityFrameworkSagaRepository<ShoppingChore>(_sagaDbContextFactory));
        }

        [TestFixtureTearDown]
        public void Teardown()
        {
        }

        async Task<ShoppingChore> GetSaga(Guid id)
        {
            using (var dbContext = _sagaDbContextFactory())
            {
                var sagaInstance = dbContext.Set<ShoppingChore>().SingleOrDefault(x => x.CorrelationId == id);
                return sagaInstance;
            }
        }

        [Test]
        public async Task Should_have_removed_the_state_machine()
        {
            Guid correlationId = Guid.NewGuid();

            await InputQueueSendEndpoint.Send(new GirlfriendYelling
            {
                CorrelationId = correlationId
            });

            Guid? sagaId = await _repository.Value.ShouldContainSaga(correlationId, TestTimeout);
            Assert.IsTrue(sagaId.HasValue);

            await InputQueueSendEndpoint.Send(new SodOff
            {
                CorrelationId = correlationId
            });

            sagaId = await _repository.Value.ShouldNotContainSaga(correlationId, TestTimeout);
            Assert.IsFalse(sagaId.HasValue);
        }

        [Test]
        public async Task Should_have_the_state_machine()
        {
            Guid correlationId = Guid.NewGuid();

            await InputQueueSendEndpoint.Send(new GirlfriendYelling
            {
                CorrelationId = correlationId
            });

            Guid? sagaId = await _repository.Value.ShouldContainSaga(correlationId, TestTimeout);

            Assert.IsTrue(sagaId.HasValue);

            await InputQueueSendEndpoint.Send(new GotHitByACar
            {
                CorrelationId = correlationId
            });

            sagaId = await _repository.Value.ShouldContainSaga(x => x.CorrelationId == correlationId
                && x.CurrentState == _machine.Dead.Name, TestTimeout);

            Assert.IsTrue(sagaId.HasValue);

            ShoppingChore instance = await GetSaga(correlationId);

            Assert.IsTrue(instance.Screwed);
        }
    }


    class EntityFrameworkShoppingChoreMap :
        SagaClassMapping<ShoppingChore>
    {
        public EntityFrameworkShoppingChoreMap()
        {
            Property(x => x.CurrentState);
            Property(x => x.Everything);

            Property(x => x.Screwed);
        }
    }
}