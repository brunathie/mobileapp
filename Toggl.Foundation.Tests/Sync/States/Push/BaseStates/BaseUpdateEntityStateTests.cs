using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Tests.Helpers;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public abstract class BaseUpdateEntityStateTests
    {
        private TheStartMethodHelper helper;

        public BaseUpdateEntityStateTests(TheStartMethodHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public void ReturnsTheFailTransitionWhenEntityIsNull()
            => helper.ReturnsTheFailTransitionWhenEntityIsNull();

        [Theory]
        [MemberData(nameof(ApiExceptions.ServerExceptions), MemberType = typeof(ApiExceptions))]
        public void ReturnsTheServerErrorTransitionWhenHttpFailsWithServerError(ServerErrorException exception)
            => helper.ReturnsTheServerErrorTransitionWhenHttpFailsWithServerError(exception);

        [Theory]
        [MemberData(nameof(ApiExceptions.ClientExceptionsWhichAreNotReThrownInSyncStates), MemberType = typeof(ApiExceptions))]
        public void ReturnsTheClientErrorTransitionWhenHttpFailsWithClientError(ClientErrorException exception)
            => helper.ReturnsTheClientErrorTransitionWhenHttpFailsWithClientError(exception);

        [Fact]
        public void ReturnsTheUnknownErrorTransitionWhenHttpFailsWithNonApiError()
            => helper.ReturnsTheUnknownErrorTransitionWhenHttpFailsWithNonApiError();

        [Fact]
        public void ReturnsTheFailTransitionWhenDatabaseOperationFails()
            => helper.ReturnsTheFailTransitionWhenDatabaseOperationFails();

        [Fact]
        public void UpdateApiCallIsCalledWithTheInputEntity()
            => helper.UpdateApiCallIsCalledWithTheInputEntity();

        [Fact]
        public void ReturnsTheEntityChangedTransitionWhenEntityChangesLocally()
            => helper.ReturnsTheEntityChangedTransitionWhenEntityChangesLocally();

        [Fact]
        public void ReturnsTheUpdatingSuccessfulTransitionWhenEntityDoesNotChangeLocallyAndAllFunctionsAreCalledWithCorrectParameters()
            => helper.ReturnsTheUpdatingSuccessfulTransitionWhenEntityDoesNotChangeLocallyAndAllFunctionsAreCalledWithCorrectParameters();

        public interface TheStartMethodHelper
        {
            void ReturnsTheFailTransitionWhenEntityIsNull();
            void ReturnsTheServerErrorTransitionWhenHttpFailsWithServerError(ServerErrorException exception);
            void ReturnsTheClientErrorTransitionWhenHttpFailsWithClientError(ClientErrorException exception);
            void ReturnsTheUnknownErrorTransitionWhenHttpFailsWithNonApiError();
            void ReturnsTheFailTransitionWhenDatabaseOperationFails();
            void UpdateApiCallIsCalledWithTheInputEntity();
            void ReturnsTheEntityChangedTransitionWhenEntityChangesLocally();
            void ReturnsTheUpdatingSuccessfulTransitionWhenEntityDoesNotChangeLocallyAndAllFunctionsAreCalledWithCorrectParameters();
        }

        internal abstract class TheStartMethod<TModel, TApiModel> : BasePushEntityStateTests<TModel, TApiModel>, TheStartMethodHelper
            where TModel : class, IBaseModel, IDatabaseSyncable, TApiModel
            where TApiModel : class
        {
            private ITogglApi api;
            private IRepository<TModel> repository;

            public TheStartMethod()
                : this(Substitute.For<ITogglApi>(), Substitute.For<IRepository<TModel>>())
            {
            }

            public TheStartMethod(ITogglApi api, IRepository<TModel> repository)
                : base(api, repository)
            {
                this.api = api;
                this.repository = repository;
            }

            public void ReturnsTheFailTransitionWhenEntityIsNull()
            {
                var state = createUpdateState(api, repository);
                var transition = state.Start(null).SingleAsync().Wait();
                var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

                transition.Result.Should().Be(state.UnknownError);
                parameter.Reason.Should().BeOfType<ArgumentNullException>();
            }

            public void ReturnsTheServerErrorTransitionWhenHttpFailsWithServerError(ServerErrorException exception)
            {
                var state = createUpdateState(api, repository);
                var entity = CreateDirtyEntity(1);
                GetUpdateFunction(api)(Arg.Any<TModel>())
                    .Returns(_ => Observable.Throw<TApiModel>(exception));

                var transition = state.Start(entity).SingleAsync().Wait();
                var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

                transition.Result.Should().Be(state.ServerError);
                parameter.Reason.Should().BeAssignableTo<ServerErrorException>();
            }

            public void ReturnsTheClientErrorTransitionWhenHttpFailsWithClientError(ClientErrorException exception)
            {
                var state = createUpdateState(api, repository);
                var entity = CreateDirtyEntity(1);
                GetUpdateFunction(api)(Arg.Any<TModel>())
                    .Returns(_ => Observable.Throw<TApiModel>(exception));

                var transition = state.Start(entity).SingleAsync().Wait();
                var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

                transition.Result.Should().Be(state.ClientError);
                parameter.Reason.Should().BeAssignableTo<ClientErrorException>();
            }

            public void ReturnsTheUnknownErrorTransitionWhenHttpFailsWithNonApiError()
            {
                var state = createUpdateState(api, repository);
                var entity = CreateDirtyEntity(1);
                GetUpdateFunction(api)(Arg.Any<TModel>())
                    .Returns(_ => Observable.Throw<TApiModel>(new TestException()));

                var transition = state.Start(entity).SingleAsync().Wait();
                var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

                transition.Result.Should().Be(state.UnknownError);
                parameter.Reason.Should().BeOfType<TestException>();
            }

            public void ReturnsTheFailTransitionWhenDatabaseOperationFails()
            {
                var state = createUpdateState(api, repository);
                var entity = CreateDirtyEntity(1);
                repository
                    .BatchUpdate(Arg.Any<IEnumerable<(long, TModel)>>(), Arg.Any<Func<TModel, TModel, ConflictResolutionMode>>())
                    .Returns(_ => Observable.Throw<IEnumerable<IConflictResolutionResult<TModel>>>(new TestException()));

                var transition = state.Start(entity).SingleAsync().Wait();
                var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

                transition.Result.Should().Be(state.UnknownError);
                parameter.Reason.Should().BeOfType<TestException>();
            }

            public void UpdateApiCallIsCalledWithTheInputEntity()
            {
                var state = createUpdateState(api, repository);
                var entity = CreateDirtyEntity(1);
                GetUpdateFunction(api)(entity)
                    .Returns(Observable.Return(Substitute.For<TApiModel>()));
                repository
                    .BatchUpdate(Arg.Any<IEnumerable<(long, TModel)>>(), Arg.Any<Func<TModel, TModel, ConflictResolutionMode>>())
                    .Returns(Observable.Return(new[] { new UpdateResult<TModel>(entity.Id, entity) }));

                state.Start(entity).SingleAsync().Wait();

                AssertUpdateReceived(api, entity);
            }

            public void ReturnsTheEntityChangedTransitionWhenEntityChangesLocally()
            {
                var state = createUpdateState(api, repository);
                var at = new DateTimeOffset(2017, 9, 1, 12, 34, 56, TimeSpan.Zero);
                var entity = CreateDirtyEntity(1, at);
                GetUpdateFunction(api)(Arg.Any<TModel>())
                    .Returns(Observable.Return(entity));
                repository
                    .BatchUpdate(Arg.Any<IEnumerable<(long, TModel)>>(), Arg.Any<Func<TModel, TModel, ConflictResolutionMode>>())
                    .Returns(Observable.Return(new[] { new IgnoreResult<TModel>(entity.Id) }));

                var transition = state.Start(entity).SingleAsync().Wait();
                var parameter = ((Transition<TModel>)transition).Parameter;

                transition.Result.Should().Be(state.EntityChanged);
                parameter.Id.Should().Be(entity.Id);
            }

            public void ReturnsTheUpdatingSuccessfulTransitionWhenEntityDoesNotChangeLocallyAndAllFunctionsAreCalledWithCorrectParameters()
            {
                var state = createUpdateState(api, repository);
                var at = new DateTimeOffset(2017, 9, 1, 12, 34, 56, TimeSpan.Zero);
                var entity = CreateDirtyEntity(1, at);
                var serverEntity = CreateDirtyEntity(2, at);
                var localEntity = CreateDirtyEntity(3, at);
                var updatedEntity = CreateDirtyEntity(4, at);
                GetUpdateFunction(api)(entity)
                    .Returns(Observable.Return(serverEntity));
                repository
                    .GetById(entity.Id)
                    .Returns(Observable.Return(localEntity));
                repository
                    .BatchUpdate(Arg.Any<IEnumerable<(long, TModel)>>(), Arg.Any<Func<TModel, TModel, ConflictResolutionMode>>())
                    .Returns(Observable.Return(new[] { new UpdateResult<TModel>(localEntity.Id, updatedEntity) }));

                var transition = state.Start(entity).SingleAsync().Wait();
                var parameter = ((Transition<TModel>)transition).Parameter;

                transition.Result.Should().Be(state.UpdatingSucceeded);
                parameter.ShouldBeEquivalentTo(updatedEntity, options => options.IncludingProperties());
                repository.Received().BatchUpdate(
                    Arg.Is<IEnumerable<(long Id, TModel Entity)>>(
                        x => x.First().Id == entity.Id && x.First().Entity.Id == serverEntity.Id),
                    Arg.Any<Func<TModel, TModel, ConflictResolutionMode>>());
            }

            protected override void PrepareApiCallFunctionToThrow(Exception e)
                => GetUpdateFunction(api)(Arg.Any<TModel>())
                    .Returns(_ => Observable.Throw<TApiModel>(e));

            protected override void PrepareDatabaseFunctionToThrow(Exception e)
                => repository.Update(Arg.Any<long>(), Arg.Any<TModel>())
                    .Returns(_ => Observable.Throw<TModel>(e));

            protected abstract Func<TModel, IObservable<TApiModel>> GetUpdateFunction(ITogglApi api);

            private BaseUpdateEntityState<TModel> createUpdateState(ITogglApi api, IRepository<TModel> repository)
                => CreateState(api, repository) as BaseUpdateEntityState<TModel>;

            protected abstract TModel CreateDirtyEntity(long id, DateTimeOffset lastUpdate = default(DateTimeOffset));

            protected abstract void AssertUpdateReceived(ITogglApi api, TModel entity);
        }
    }
}
