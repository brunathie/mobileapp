using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;
using FsCheck;
using FsCheck.Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public abstract class BaseUnsyncableEntityStateTests
    {
        private IStartMethodTestHelper helper;

        public BaseUnsyncableEntityStateTests(IStartMethodTestHelper helper)
        {
            this.helper = helper;
        }

        [Theory, LogIfTooSlow]
        [ClassData(typeof(TwoParameterConstructorTestData))]
        public void ThrowsWhenArgumentsAreNull(bool hasEntity, bool hasReason)
            => helper.ThrowsWhenArgumentsAreNull(hasEntity, hasReason);

        [Fact, LogIfTooSlow]
        public void ThrowsWhenDatabaseOperationFails()
            => helper.ThrowsWhenDatabaseOperationFails();

        [Fact, LogIfTooSlow]
        public void ThrowsWhenTheReasonExceptionIsNotAnApiException()
            => helper.ThrowsWhenTheReasonExceptionIsNotAnApiException();

        [Property]
        public void TheErrorMessageMatchesTheMessageFromTheReasonException(NonNull<string> message)
            => helper.TheErrorMessageMatchesTheMessageFromTheReasonException(message);

        [Fact, LogIfTooSlow]
        public void TheSyncStatusOfTheEntityChangesToSyncFailedWhenEverythingWorks()
            => helper.TheSyncStatusOfTheEntityChangesToSyncFailedWhenEverythingWorks();

        [Fact, LogIfTooSlow]
        public void TheUpdatedEntityHasTheSameIdAsTheOriginalEntity()
            => helper.TheUpdatedEntityHasTheSameIdAsTheOriginalEntity();

        [Fact, LogIfTooSlow]
        public void TheOnlyThingThatChangesInTheUnsyncableEntityIsTheSyncStatusAndLastSyncErrorMessage()
            => helper.TheOnlyThingThatChangesInTheUnsyncableEntityIsTheSyncStatusAndLastSyncErrorMessage();

        public interface IStartMethodTestHelper
        {
            void ThrowsWhenArgumentsAreNull(bool hasEntity, bool hasReason);
            void TheErrorMessageMatchesTheMessageFromTheReasonException(NonNull<string> message);
            void ThrowsWhenDatabaseOperationFails();
            void TheSyncStatusOfTheEntityChangesToSyncFailedWhenEverythingWorks();
            void ThrowsWhenTheReasonExceptionIsNotAnApiException();
            void TheUpdatedEntityHasTheSameIdAsTheOriginalEntity();
            void TheOnlyThingThatChangesInTheUnsyncableEntityIsTheSyncStatusAndLastSyncErrorMessage();
        }

        internal abstract class TheStartMethod<TModel> : IStartMethodTestHelper
            where TModel : class, IBaseModel, IDatabaseSyncable
        {
            private IRepository<TModel> repository;

            public TheStartMethod()
            {
                repository = Substitute.For<IRepository<TModel>>();
            }

            public void ThrowsWhenArgumentsAreNull(bool hasEntity, bool hasReason)
            {
                TModel entity = hasEntity ? CreateDirtyEntity() : null;
                Exception reason = hasReason ? new ApiException(request, response, "Test.") : null;
                var state = CreateState(repository);

                Action callingStart = () => state.Start((reason, entity)).SingleAsync().Wait();

                callingStart.ShouldThrow<ArgumentNullException>();
            }

            public void ThrowsWhenDatabaseOperationFails()
            {
                var state = CreateState(repository);
                repository
                    .BatchUpdate(null, null)
                    .ReturnsForAnyArgs(_ => throw new TestException());

                Action callingStart = () => state.Start(
                    (new ApiException(request, response, "Test."), CreateDirtyEntity())).SingleAsync().Wait();

                callingStart.ShouldThrow<TestException>();
            }

            public void ThrowsWhenTheReasonExceptionIsNotAnApiException()
            {
                var state = CreateState(repository);
                var exception = new TestException();

                Action callingStart = () => state.Start(
                    (exception, CreateDirtyEntity())).SingleAsync().Wait();

                callingStart.ShouldThrow<TestException>().Where(e => e == exception);
            }

            public void TheErrorMessageMatchesTheMessageFromTheReasonException(NonNull<string> message)
            {
                var entity = CreateDirtyEntity();
                var response = Substitute.For<IResponse>();
                response.RawData.Returns(message.Get);
                var exception = new InternalServerErrorException(Substitute.For<IRequest>(), response);
                var reason = new BadRequestException(request, response);
                var state = CreateState(repository);
                prepareBatchUpdate(entity);

                var transition = state.Start((reason, entity)).SingleAsync().Wait();
                var unsyncableEntity = ((Transition<TModel>)transition).Parameter;

                unsyncableEntity.LastSyncErrorMessage.Should().Be(message.Get);
            }

            public void TheSyncStatusOfTheEntityChangesToSyncFailedWhenEverythingWorks()
            {
                var entity = CreateDirtyEntity();
                var state = CreateState(repository);
                prepareBatchUpdate(entity);

                var transition = state.Start((new BadRequestException(request, response), entity)).SingleAsync().Wait();
                var unsyncableEntity = ((Transition<TModel>)transition).Parameter;

                unsyncableEntity.SyncStatus.Should().Be(SyncStatus.SyncFailed);
            }

            public void TheUpdatedEntityHasTheSameIdAsTheOriginalEntity()
            {
                var entity = CreateDirtyEntity();
                var state = CreateState(repository);
                prepareBatchUpdate(entity);

                state.Start((new BadRequestException(request, response), entity)).SingleAsync().Wait();

                repository
                    .Received()
                    .BatchUpdate(
                        Arg.Is<IEnumerable<(long Id, TModel)>>(entities => entities.First().Id == entity.Id),
                        Arg.Any<Func<TModel, TModel, ConflictResolutionMode>>());
            }

            public void TheOnlyThingThatChangesInTheUnsyncableEntityIsTheSyncStatusAndLastSyncErrorMessage()
            {
                var entity = CreateDirtyEntity();
                var reason = new BadRequestException(request, response);
                var state = CreateState(repository);
                prepareBatchUpdate(entity);

                var transition = state.Start((reason, entity)).SingleAsync().Wait();
                var unsyncableEntity = ((Transition<TModel>)transition).Parameter;

                entity.ShouldBeEquivalentTo(unsyncableEntity, options
                    => options.IncludingProperties()
                        .Excluding(x => x.LastSyncErrorMessage)
                        .Excluding(x => x.SyncStatus));
            }

            private void prepareBatchUpdate(TModel entity)
            {
                repository
                    .BatchUpdate(
                        Arg.Is<IEnumerable<(long Id, TModel Entity)>>(entities => entities.First().Id == entity.Id),
                        Arg.Any<Func<TModel, TModel, ConflictResolutionMode>>())
                    .Returns(args =>
                    {
                        var arg = ((IEnumerable<(long Id, TModel Entity)>)args[0]).First();
                        return Observable.Return(new List<IConflictResolutionResult<TModel>> { new UpdateResult<TModel>(arg.Id, arg.Entity) });
                    });
            }

            protected abstract BaseUnsyncableEntityState<TModel> CreateState(IRepository<TModel> repository);

            protected abstract TModel CreateDirtyEntity();

            private static IRequest request => Substitute.For<IRequest>();

            private static IResponse response => Substitute.For<IResponse>();
        }
    }
}
