using System;
using System.Reactive.Linq;
using FluentAssertions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Xunit;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;

namespace Toggl.Foundation.Tests.Sync.States
{
    public abstract class BasePushOneEntityStateTests
    {
        private ITheStartMethodHelper helper;

        public BasePushOneEntityStateTests()
        {
            this.helper = new TheStartMethod();
        }

        [Fact, LogIfTooSlow]
        public void ThrowsWhenEntityIsNull()
            => helper.ThrowsWhenEntityIsNull();

        [Fact, LogIfTooSlow]
        public void ReturnsCreateTransitionWhenTheEntityIsNotPublishedAndNotDeleted()
            => helper.ReturnsCreateTransitionWhenTheEntityIsNotPublishedAndNotDeleted();

        [Fact, LogIfTooSlow]
        public void ReturnsUpdateTransitionWhenTheEntityIsPublishedAndNotDeleted()
            => helper.ReturnsUpdateTransitionWhenTheEntityIsPublishedAndNotDeleted();

        [Fact, LogIfTooSlow]
        public void ReturnsDeleteTransitionWhenTheEntityIsPublishedAndIsDeleted()
            => helper.ReturnsDeleteTransitionWhenTheEntityIsPublishedAndIsDeleted();

        [Fact, LogIfTooSlow]
        public void ReturnsDeleteLocallyTransitionWhenTheEntityIsNotPublishedAndIsDeleted()
            => helper.ReturnsDeleteLocallyTransitionWhenTheEntityIsNotPublishedAndIsDeleted();

        public interface ITheStartMethodHelper
        {
            void ThrowsWhenEntityIsNull();
            void ReturnsCreateTransitionWhenTheEntityIsNotPublishedAndNotDeleted();
            void ReturnsUpdateTransitionWhenTheEntityIsPublishedAndNotDeleted();
            void ReturnsDeleteTransitionWhenTheEntityIsPublishedAndIsDeleted();
            void ReturnsDeleteLocallyTransitionWhenTheEntityIsNotPublishedAndIsDeleted();
        }

        internal sealed class TheStartMethod : ITheStartMethodHelper
        {
            public void ThrowsWhenEntityIsNull()
            {
                Action startWithNull = () => createState().Start(null).SingleAsync().Wait();

                startWithNull.ShouldThrow<ArgumentNullException>();
            }

            public void ReturnsCreateTransitionWhenTheEntityIsNotPublishedAndNotDeleted()
            {
                var state = createState();
                var entity = TestModel.Dirty(-123);

                var transition = state.Start(entity).SingleAsync().Wait();

                transition.Result.Should().Be(state.CreateEntity);
                ((Transition<TestModel>)transition).Parameter.Should().Be(entity);
            }

            public void ReturnsUpdateTransitionWhenTheEntityIsPublishedAndNotDeleted()
            {
                var state = createState();
                var entity = TestModel.Dirty(123);

                var transition = state.Start(entity).SingleAsync().Wait();

                transition.Result.Should().Be(state.UpdateEntity);
                ((Transition<TestModel>)transition).Parameter.Should().Be(entity);
            }

            public void ReturnsDeleteTransitionWhenTheEntityIsPublishedAndIsDeleted()
            {
                var state = createState();
                var entity = TestModel.DirtyDeleted(123);

                var transition = state.Start(entity).SingleAsync().Wait();

                transition.Result.Should().Be(state.DeleteEntity);
                ((Transition<TestModel>)transition).Parameter.Should().Be(entity);
            }

            public void ReturnsDeleteLocallyTransitionWhenTheEntityIsNotPublishedAndIsDeleted()
            {
                var state = createState();
                var entity = TestModel.DirtyDeleted(-123);

                var transition = state.Start(entity).SingleAsync().Wait();

                transition.Result.Should().Be(state.DeleteEntityLocally);
                ((Transition<TestModel>)transition).Parameter.Should().Be(entity);
            }

            private PushOneEntityState<TestModel> createState()
                => new PushOneEntityState<TestModel>();
        }
    }
}
