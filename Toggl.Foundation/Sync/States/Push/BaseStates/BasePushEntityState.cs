using System;
using System.Reactive.Linq;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Sync.States
{
    public abstract class BasePushEntityState<TModel>
        where TModel : class, IBaseModel, IDatabaseSyncable
    {
        public StateResult<(Exception, TModel)> ServerError { get; } = new StateResult<(Exception, TModel)>();
        public StateResult<(Exception, TModel)> ClientError { get; } = new StateResult<(Exception, TModel)>();
        public StateResult<(Exception, TModel)> UnknownError { get; } = new StateResult<(Exception, TModel)>();

        protected ITogglApi Api { get; }
        protected IRepository<TModel> Repository { get; }

        public BasePushEntityState(ITogglApi api, IRepository<TModel> repository)
        {
            Api = api;
            Repository = repository;
        }

        protected Func<TModel, IObservable<TModel>> Overwrite(TModel entity)
            => pushedEntity => Repository.Update(entity.Id, pushedEntity).Select(CopyFrom);

        protected Func<Exception, IObservable<ITransition>> Fail(TModel entity)
            => exception => shouldRethrow(exception)
                ? Observable.Throw<ITransition>(exception)
                : Observable.Return(failTransition(entity, exception));

        private bool shouldRethrow(Exception e)
            => e is ApiDeprecatedException || e is ClientDeprecatedException || e is UnauthorizedException || e is OfflineException;

        private ITransition failTransition(TModel entity, Exception e)
            => e is ServerErrorException
                ? ServerError.Transition((e, entity))
                : e is ClientErrorException
                    ? ClientError.Transition((e, entity))
                    : UnknownError.Transition((e, entity));

        public abstract IObservable<ITransition> Start(TModel entity);

        protected abstract TModel CopyFrom(TModel entity);
    }
}
