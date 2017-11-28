using System;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Tests.Sync.States
{
    public abstract class BasePushEntityStateTests<TModel, TApiModel>
        where TModel : class, IBaseModel, IDatabaseSyncable, TApiModel
    {
        private ITogglApi api;
        private IRepository<TModel> repository;

        public BasePushEntityStateTests(ITogglApi api, IRepository<TModel> repository)
        {
            this.api = api;
            this.repository = repository;
        }

        public void ReturnsFailTransitionWhenEntityIsNull()
        {
            var state = CreateState(api, repository);

            var transition = state.Start(null).SingleAsync().Wait();
            var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

            transition.Result.Should().Be(state.UnknownError);
            parameter.Reason.Should().BeOfType<ArgumentNullException>();
        }

        public void ReturnsClientErrorTransitionWhenHttpFailsWithClientErrorException(ClientErrorException exception)
        {
            var state = CreateState(api, repository);
            var entity = CreateDirtyEntityWithNegativeId();
            PrepareApiCallFunctionToThrow(exception);

            var transition = state.Start(entity).SingleAsync().Wait();
            var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

            transition.Result.Should().Be(state.ClientError);
            parameter.Reason.Should().BeAssignableTo<ClientErrorException>();
        }

        public void ReturnsServerErrorTransitionWhenHttpFailsWithServerErrorException(ServerErrorException exception)
        {
            var state = CreateState(api, repository);
            var entity = CreateDirtyEntityWithNegativeId();
            PrepareApiCallFunctionToThrow(exception);

            var transition = state.Start(entity).SingleAsync().Wait();
            var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

            transition.Result.Should().Be(state.ServerError);
            parameter.Reason.Should().BeAssignableTo<ServerErrorException>();
        }

        public void ReturnsUnknownErrorTransitionWhenHttpFailsWithNonApiException()
        {
            var state = CreateState(api, repository);
            var entity = CreateDirtyEntityWithNegativeId();
            PrepareApiCallFunctionToThrow(new TestException());

            var transition = state.Start(entity).SingleAsync().Wait();
            var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

            transition.Result.Should().Be(state.UnknownError);
            parameter.Reason.Should().BeOfType<TestException>();
        }

        public void ReturnsFailTransitionWhenDatabaseOperationFails()
        {
            var state = CreateState(api, repository);
            var entity = CreateDirtyEntityWithNegativeId();
            PrepareDatabaseFunctionToThrow(new TestException());

            var transition = state.Start(entity).SingleAsync().Wait();
            var parameter = ((Transition<(Exception Reason, TModel)>)transition).Parameter;

            transition.Result.Should().Be(state.UnknownError);
            parameter.Reason.Should().BeOfType<TestException>();
        }

        public void ThrowsWhenCertainExceptionsAreCaught(Exception exception)
        {
            var state = CreateState(api, repository); ;
            PrepareApiCallFunctionToThrow(exception);
            Exception caughtException = null;

            try
            {
                state.Start(Substitute.For<TModel>()).Wait();
            }
            catch (Exception e)
            {
                caughtException = e;
            }

            caughtException.Should().NotBeNull();
            caughtException.Should().BeAssignableTo(exception.GetType());
        }

        protected abstract BasePushEntityState<TModel> CreateState(ITogglApi api, IRepository<TModel> repository);

        protected abstract TModel CreateDirtyEntityWithNegativeId();

        protected abstract TModel CreateCleanWithPositiveIdFrom(TModel entity);

        protected abstract TModel CreateCleanEntityFrom(TModel entity);

        protected abstract void PrepareApiCallFunctionToThrow(Exception e);

        protected abstract void PrepareDatabaseFunctionToThrow(Exception e);
    }
}
