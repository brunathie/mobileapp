using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class TransitionHandlerProviderTests
    {
        public abstract class ConfigureTransitionMethodTests<TStateResult, TStateFactory>
            where TStateResult : class, new()
            where TStateFactory : class
        {
            protected TransitionHandlerProvider Provider { get; } = new TransitionHandlerProvider();

            protected abstract void CallMethod(TStateResult result, TStateFactory factory);
            protected abstract TStateFactory ToStateFactory(Action action);

            [Theory, LogIfTooSlow]
            [ClassData(typeof(TwoParameterConstructorTestData))]
            public void ThrowsIfAnyArgumentIsNull(bool useStateResult, bool useStateFactory)
            {
                var stateResult = useStateResult ? new TStateResult() : null;
                var stateFactory = useStateFactory ? Substitute.For<TStateFactory>() : null;

                Action callingMethod = () => CallMethod(stateResult, stateFactory);

                callingMethod.ShouldThrow<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsIfCalledTwiceWithSameStateResult()
            {
                var stateResult = new TStateResult();
                CallMethod(stateResult, Substitute.For<TStateFactory>());

                Action callingMethodSecondTime =
                    () => CallMethod(stateResult, Substitute.For<TStateFactory>());

                callingMethodSecondTime.ShouldThrow<Exception>();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotCallProvidedStateFactory()
            {
                var factoryWasCalled = false;

                CallMethod(new TStateResult(), ToStateFactory(() => factoryWasCalled = true));

                factoryWasCalled.Should().BeFalse();
            }
        }

        public sealed class TheConfigureTransitionMethod
            : ConfigureTransitionMethodTests<StateResult, Func<IObservable<ITransition>>>
        {
            protected override void CallMethod(StateResult result, Func<IObservable<ITransition>> factory)
                => Provider.ConfigureTransition(result, factory);

            protected override Func<IObservable<ITransition>> ToStateFactory(Action action)
                => () => { action(); return null; };
        }

        public sealed class TheGenericConfigureTransitionMethod
            : ConfigureTransitionMethodTests<StateResult<object>, Func<object, IObservable<ITransition>>>
        {
            protected override void CallMethod(StateResult<object> result, Func<object, IObservable<ITransition>> factory)
                => Provider.ConfigureTransition(result, factory);

            protected override Func<object, IObservable<ITransition>> ToStateFactory(Action action)
                => _ => { action(); return null; };
        }

        public sealed class TheGetTransitionHandlerMethod
        {
            private TransitionHandlerProvider provider { get; } = new TransitionHandlerProvider();

            [Fact, LogIfTooSlow]
            public void ThrowsIfArgumentIsNull()
            {
                Action callingMethodWithNull = () => provider.GetTransitionHandler(null);

                callingMethodWithNull.ShouldThrow<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullIfThereAreNoHandlers()
            {
                var handler = provider.GetTransitionHandler(Substitute.For<IStateResult>());

                handler.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsNullIfTheStateResultIsUnknown()
            {
                provider.ConfigureTransition(new StateResult(), () => null);
                provider.ConfigureTransition(new StateResult<object>(), _ => null);

                var handler = provider.GetTransitionHandler(Substitute.For<IStateResult>());

                handler.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsHandlerThatCallsProvidedStateFactory()
            {
                var stateResult = new StateResult();
                var expectedResult = Substitute.For<IObservable<ITransition>>();
                provider.ConfigureTransition(stateResult, () => expectedResult);

                var handler = provider.GetTransitionHandler(stateResult);
                var actualResult = handler(null);

                actualResult.Should().Be(expectedResult);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsHandlerThatCallsProvidedGenericStateFactory()
            {
                var stateResult = new StateResult<object>();
                var expectedResult = Substitute.For<IObservable<ITransition>>();
                provider.ConfigureTransition(stateResult, _ => expectedResult);

                var handler = provider.GetTransitionHandler(stateResult);
                var actualResult = handler(stateResult.Transition(null));

                actualResult.Should().Be(expectedResult);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsHandlerThatCallsProvidedGenericStateFactoryWithCorrectArgument()
            {
                var stateResult = new StateResult<object>();
                var expectedArgument = new object();
                object actualArgument = null;
                provider.ConfigureTransition(stateResult, obj => { actualArgument = obj; return null; });

                var handler = provider.GetTransitionHandler(stateResult);
                handler(stateResult.Transition(expectedArgument));

                actualArgument.Should().Be(expectedArgument);
            }
        }
    }
}
