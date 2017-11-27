using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.PrimeRadiant.Exceptions;
using Xunit;

namespace Toggl.PrimeRadiant.Tests
{
    public abstract class SingleObjectStorageTests<TTestModel> : BaseStorageTests<TTestModel>
        where TTestModel : class, ITestModel, IDatabaseSyncable, new()
    {
        protected sealed override IObservable<TTestModel> Create(TTestModel testModel)
            => Storage.Create(testModel);

        protected sealed override IObservable<TTestModel> Update(long id, TTestModel testModel)
            => Storage.Update(testModel);

        protected sealed override IObservable<Unit> Delete(long id)
            => Storage.Delete();

        protected abstract ISingleObjectStorage<TTestModel> Storage { get; }

        [Fact, LogIfTooSlow]
        public void TheSingleMethodThrowsIfThereIsNoDataInTheRepository()
        {
            Func<Task> callingGetLastInAnEmptyRepository =
                async () => await Storage.Single();

            callingGetLastInAnEmptyRepository
                .ShouldThrow<EntityNotFoundException>();
        }

        [Fact, LogIfTooSlow]
        public async Task TheSingleMethodAlwaysReturnsASingleElement()
        {
            var testEntity = new TTestModel();
            await Storage.Create(testEntity);

            var element = await Storage.Single();
            element.Should().Be(testEntity);
        }

        [Fact, LogIfTooSlow]
        public async Task TheCreateModelThrowsIfAnItemAlreadyExistsRegardlessOfId()
        {
            var testEntity = new TTestModel();
            await Storage.Create(GetModelWith(1));

            Func<Task> callingCreateASecondTime =
                async () => await Storage.Create(GetModelWith(2));

            callingCreateASecondTime
                .ShouldThrow<EntityAlreadyExistsException>();
        }

        [Theory, LogIfTooSlow]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(100)]
        public void TheBatchUpdateMehtodThrowsWhenThereIsMoreThanEntityToUpdate(int entitiesToUpdate)
        {
            var batch = Enumerable.Range(0, entitiesToUpdate).Select(id => ((long)id, new TTestModel()));

            Func<Task> callingBatchUpdate = async () => await callBatchUpdate(batch);

            callingBatchUpdate.ShouldThrow<ArgumentException>();
        }

        private IObservable<IEnumerable<IConflictResolutionResult<TTestModel>>> callBatchUpdate(IEnumerable<(long, TTestModel)> batch)
            => Storage.BatchUpdate(batch, (a, b) => ConflictResolutionMode.Ignore, Substitute.For<IRivalsResolver<TTestModel>>());
    }
}
