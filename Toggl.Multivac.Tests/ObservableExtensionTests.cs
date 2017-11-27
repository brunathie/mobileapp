using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using Toggl.Multivac.Extensions;
using Xunit;

namespace Toggl.Multivac.Tests
{
    public class ObservableExtensionTests
    {
        public class TheConnectedReplayExtensionMethod
        {
            [Fact, LogIfTooSlow]
            public void Connects()
            {
                var connected = false;
                var observable = Observable.Create<object>(observer => {
                    connected = true;
                    return () => { };
                });

                observable.ConnectedReplay();

                connected.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void Replays()
            {
                var items = new List<string>();
                var observable = new Subject<string>();

                var result = observable.ConnectedReplay();
                observable.OnNext("first");
                observable.OnNext("second");
                
                result.Subscribe(items.Add);
                items.ShouldAllBeEquivalentTo(new[] { "first", "second" });
                result.Subscribe(items.Add);
                items.ShouldAllBeEquivalentTo(new[] { "first", "second", "first", "second" });
            }
        }
    }
}
