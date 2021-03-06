﻿using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Toggl.Foundation.Sync.States
{
    internal sealed class ResetAPIDelayState
    {
        public StateResult PushNext { get; } = new StateResult();

        private readonly IRetryDelayService delay;

        public ResetAPIDelayState(IRetryDelayService delay)
        {
            this.delay = delay;
        }

        public IObservable<ITransition> Start()
            => Observable.Return(PushNext.Transition())
                .Do(_ => delay.Reset());
    }
}
