using System;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public abstract class BasePushStateTests
    {
        private ITheStartMethodHelper helper;

        public BasePushStateTests(ITheStartMethodHelper helper)
        {
            this.helper = helper;
        }

        [Fact, LogIfTooSlow]
        public void ConstructorThrowsWithNullDatabase()
            => helper.ConstructorThrowsWithNullDatabase();

        [Fact, LogIfTooSlow]
        public void ReturnsNothingToPushTransitionWhenTheRepositoryReturnsNoEntity()
            => helper.ReturnsNothingToPushTransitionWhenTheRepositoryReturnsNoEntity();

        [Fact, LogIfTooSlow]
        public void ReturnsPushEntityTransitionWhenTheRepositoryReturnsSomeEntity()
            => helper.ReturnsPushEntityTransitionWhenTheRepositoryReturnsSomeEntity();

        [Fact, LogIfTooSlow]
        public void ThrowsWhenRepositoryThrows()
            => helper.ThrowsWhenRepositoryThrows();

        [Fact, LogIfTooSlow]
        public void ReturnsPushEntityTransitionWithTheOldestEntity()
            => helper.ReturnsPushEntityTransitionWithTheOldestEntity();

        public interface ITheStartMethodHelper
        {
            void ConstructorThrowsWithNullDatabase();
            void ReturnsNothingToPushTransitionWhenTheRepositoryReturnsNoEntity();
            void ReturnsPushEntityTransitionWhenTheRepositoryReturnsSomeEntity();
            void ReturnsPushEntityTransitionWithTheOldestEntity();
            void ThrowsWhenRepositoryThrows();
        }

        internal abstract class TheStartMethod<TModel> : ITheStartMethodHelper
            where TModel : class, IBaseModel, IDatabaseSyncable
        {
            private readonly IRepository<TModel> repository;

            public TheStartMethod()
            {
                repository = Substitute.For<IRepository<TModel>>();
            }

            public void ConstructorThrowsWithNullDatabase()
            {
                Action creatingWithNullArgument = () => CreateState(null);

                creatingWithNullArgument.ShouldThrow<ArgumentNullException>();
            }

            public void ReturnsNothingToPushTransitionWhenTheRepositoryReturnsNoEntity()
            {
                var state = CreateState(repository);
                SetupRepositoryToReturn(repository, new TModel[] { });

                var transition = state.Start().SingleAsync().Wait();

                transition.Result.Should().Be(state.NothingToPush);
            }

            public void ReturnsPushEntityTransitionWhenTheRepositoryReturnsSomeEntity()
            {
                var state = CreateState(repository);
                var entity = CreateUnsyncedEntity();
                SetupRepositoryToReturn(repository, new[] { entity });

                var transition = state.Start().SingleAsync().Wait();
                var parameter = ((Transition<TModel>)transition).Parameter;

                transition.Result.Should().Be(state.PushEntity);
                parameter.ShouldBeEquivalentTo(entity, options => options.IncludingProperties());
            }

            public void ReturnsPushEntityTransitionWithTheOldestEntity()
            {
                var at = new DateTimeOffset(2017, 9, 1, 12, 34, 56, TimeSpan.Zero);
                var state = CreateState(repository);
                var entity = CreateUnsyncedEntity(at);
                var entity2 = CreateUnsyncedEntity(at.AddDays(-2));
                var entity3 = CreateUnsyncedEntity(at.AddDays(-1));
                SetupRepositoryToReturn(repository, new[] { entity, entity2, entity3 });

                var transition = state.Start().SingleAsync().Wait();
                var parameter = ((Transition<TModel>)transition).Parameter;

                transition.Result.Should().Be(state.PushEntity);
                parameter.ShouldBeEquivalentTo(entity2, options => options.IncludingProperties());
            }

            public void ThrowsWhenRepositoryThrows()
            {
                var state = CreateState(repository);
                SetupRepositoryToThrow(repository);

                Action callingStart = () => state.Start().SingleAsync().Wait();

                callingStart.ShouldThrow<Exception>();
            }

            protected abstract BasePushState<TModel> CreateState(IRepository<TModel> repository);

            protected abstract void SetupRepositoryToReturn(IRepository<TModel> repository, TModel[] entities);

            protected abstract void SetupRepositoryToThrow(IRepository<TModel> repository);

            protected abstract TModel CreateUnsyncedEntity(DateTimeOffset lastUpdate = default(DateTimeOffset));
        }
    }
}
