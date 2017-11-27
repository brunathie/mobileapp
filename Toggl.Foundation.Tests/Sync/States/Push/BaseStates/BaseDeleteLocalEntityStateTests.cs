using System;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync.States;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public abstract class BaseDeleteLocalEntityStateTests<TModel>
        where TModel : class
    {
        private IRepository<TModel> repository;

        public BaseDeleteLocalEntityStateTests()
        {
            repository = Substitute.For<IRepository<TModel>>();
        }
        
        [Fact, LogIfTooSlow]
        public void ReturnsFailTransitionWhenEntityIsNull()
        {
            var state = CreateState(repository);

            var transition = state.Start(null).SingleAsync().Wait();

            transition.Result.Should().Be(state.DeletingFailed);
        }

        [Fact, LogIfTooSlow]
        public void ReturnsFailTransitionWhenDatabaseOperationFails()
        {
            var state = CreateState(repository);
            var entity = CreateEntity();
            PrepareDatabaseOperationToThrow(repository, new TestException());

            var transition = state.Start(entity).SingleAsync().Wait();

            transition.Result.Should().Be(state.DeletingFailed);
        }

        [Fact, LogIfTooSlow]
        public void ReturnsDeletedTransitionWhenEverythingIsOk()
        {
            var state = CreateState(repository);
            var entity = CreateEntity();
            repository.Delete(Arg.Any<long>()).Returns(Observable.Return(Unit.Default));

            var transition = state.Start(entity).SingleAsync().Wait();

            transition.Result.Should().Be(state.Deleted);
        }

        [Fact, LogIfTooSlow]
        public void DeletesTheEntityFromTheLocalDatabase()
        {
            var state = CreateState(repository);
            var entity = CreateEntity();
            repository.Delete(Arg.Any<long>()).Returns(Observable.Return(Unit.Default));

            var transition = state.Start(entity).SingleAsync().Wait();

            repository.Received().Delete(Arg.Any<long>());
        }

        protected abstract BaseDeleteLocalEntityState<TModel> CreateState(IRepository<TModel> repository);

        protected abstract TModel CreateEntity();

        protected abstract void PrepareDatabaseOperationToThrow(IRepository<TModel> repositor, Exception e);
    }
}
