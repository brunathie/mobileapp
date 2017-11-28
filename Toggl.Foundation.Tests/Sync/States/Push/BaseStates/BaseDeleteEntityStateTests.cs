using System;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Tests.Helpers;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public abstract class BaseDeleteEntityStateTests
    {
        private IStartMethodTestHelper helper;

        public BaseDeleteEntityStateTests(IStartMethodTestHelper helper)
        {
            this.helper = helper;
        }

        [Fact, LogIfTooSlow]
        public void ReturnsFailTransitionWhenEntityIsNull()
            => helper.ReturnsFailTransitionWhenEntityIsNull();

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ApiExceptions.ClientExceptionsWhichAreNotReThrownInSyncStates), MemberType = typeof(ApiExceptions))]
        public void ReturnsClientErrorTransitionWhenHttpFailsWithClientErrorException(ClientErrorException exception)
            => helper.ReturnsClientErrorTransitionWhenHttpFailsWithClientErrorException(exception);

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ApiExceptions.ServerExceptions), MemberType = typeof(ApiExceptions))]
        public void ReturnsServerErrorTransitionWhenHttpFailsWithServerErrorException(ServerErrorException reason)
            => helper.ReturnsServerErrorTransitionWhenHttpFailsWithServerErrorException(reason);

        [Fact, LogIfTooSlow]
        public void ReturnsUnknownErrorTransitionWhenHttpFailsWithNonApi()
            => helper.ReturnsUnknownErrorTransitionWhenHttpFailsWithNonApiException();

        [Fact, LogIfTooSlow]
        public void ReturnsFailTransitionWhenDatabaseOperationFails()
            => helper.ReturnsFailTransitionWhenDatabaseOperationFails();

        [Fact, LogIfTooSlow]
        public void ReturnsSuccessfulTransitionWhenEverythingWorks()
            => helper.ReturnsSuccessfulTransitionWhenEverythingWorks();

        [Fact, LogIfTooSlow]
        public void CallsDatabaseDeleteOperationWithCorrectParameter()
            => helper.CallsDatabaseDeleteOperationWithCorrectParameter();

        [Fact, LogIfTooSlow]
        public void DoesNotDeleteTheEntityLocallyIfTheApiOperationFails()
            => helper.DoesNotDeleteTheEntityLocallyIfTheApiOperationFails();

        [Fact, LogIfTooSlow]
        public void MakesApiCallWithCorrectParameter()
            => helper.MakesApiCallWithCorrectParameter();

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ApiExceptions.ExceptionsWhichCauseRethrow), MemberType = typeof(ApiExceptions))]
        public void ThrowsWhenCertainExceptionsAreCaught(Exception exception)
            => helper.ThrowsWhenCertainExceptionsAreCaught(exception);

        public interface IStartMethodTestHelper
        {
            void ReturnsFailTransitionWhenEntityIsNull();
            void ReturnsClientErrorTransitionWhenHttpFailsWithClientErrorException(ClientErrorException exception);
            void ReturnsServerErrorTransitionWhenHttpFailsWithServerErrorException(ServerErrorException exception);
            void ReturnsUnknownErrorTransitionWhenHttpFailsWithNonApiException();
            void ReturnsFailTransitionWhenDatabaseOperationFails();
            void ReturnsSuccessfulTransitionWhenEverythingWorks();
            void MakesApiCallWithCorrectParameter();
            void CallsDatabaseDeleteOperationWithCorrectParameter();
            void DoesNotDeleteTheEntityLocallyIfTheApiOperationFails();
            void ThrowsWhenCertainExceptionsAreCaught(Exception exception);
        }

        internal abstract class TheStartMethod<TModel, TApiModel> : BasePushEntityStateTests<TModel, TApiModel>, IStartMethodTestHelper
            where TModel : class, IBaseModel, IDatabaseSyncable, TApiModel
        {
            private ITogglApi api;
            private IRepository<TModel> repository;

            public TheStartMethod()
                : this(Substitute.For<ITogglApi>(), Substitute.For<IRepository<TModel>>())
            {
            }

            private TheStartMethod(ITogglApi api, IRepository<TModel> repository)
                : base(api, repository)
            {
                this.api = api;
                this.repository = repository;
            }

            public void ReturnsSuccessfulTransitionWhenEverythingWorks()
            {
                var state = createDeleteState(api, repository);
                var entity = CreateDirtyEntityWithNegativeId();
                var clean = CreateCleanEntityFrom(entity);
                var withPositiveId = CreateCleanWithPositiveIdFrom(entity);
                GetDeleteFunction(api)(Arg.Any<TModel>())
                    .Returns(Observable.Return(Unit.Default));
                repository.Delete(Arg.Any<long>())
                    .Returns(Observable.Return(Unit.Default));

                var transition = state.Start(entity).SingleAsync().Wait();

                transition.Result.Should().Be(state.DeletingFinished);
            }

            public void MakesApiCallWithCorrectParameter()
            {
                var state = createDeleteState(api, repository);
                var entity = CreateDirtyEntityWithNegativeId();
                var deleteFunction = GetDeleteFunction(api);
                var calledDelete = false;
                deleteFunction(Arg.Is(entity))
                    .Returns(_ =>
                    {
                        calledDelete = true;
                        return Observable.Return(Unit.Default);
                    });

                state.Start(entity).SingleAsync().Wait();

                calledDelete.Should().BeTrue();
            }

            public void CallsDatabaseDeleteOperationWithCorrectParameter()
            {
                var state = createDeleteState(api, repository);
                var entity = CreateDirtyEntityWithNegativeId();
                GetDeleteFunction(api)(entity)
                    .Returns(Observable.Return(Unit.Default));

                state.Start(entity).SingleAsync().Wait();

                repository.Received().Delete(entity.Id);
            }

            public void DoesNotDeleteTheEntityLocallyIfTheApiOperationFails()
            {
                var state = createDeleteState(api, repository);
                var entity = CreateDirtyEntityWithNegativeId();
                PrepareApiCallFunctionToThrow(new TestException());

                state.Start(entity).SingleAsync().Wait();

                repository.DidNotReceive().Delete(Arg.Any<long>());
            }

            protected override void PrepareApiCallFunctionToThrow(Exception e)
                => GetDeleteFunction(api)(Arg.Any<TModel>())
                    .Returns(_ => Observable.Throw<Unit>(e));

            protected override void PrepareDatabaseFunctionToThrow(Exception e)
                => repository.Delete(Arg.Any<long>())
                    .Returns(_ => Observable.Throw<Unit>(e));

            private BaseDeleteEntityState<TModel> createDeleteState(ITogglApi api, IRepository<TModel> repository)
                => CreateState(api, repository) as BaseDeleteEntityState<TModel>;

            protected abstract Func<TModel, IObservable<Unit>> GetDeleteFunction(ITogglApi api);
        }
    }
}
