﻿using System;
using Toggl.Foundation.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class UnsyncableUserStateTests : BaseUnsyncableEntityStateTests
    {
        public UnsyncableUserStateTests()
            : base(new TheStartMethod())
        {
        }

        private sealed class TheStartMethod : TheStartMethod<IDatabaseUser>
        {
            protected override BaseUnsyncableEntityState<IDatabaseUser> CreateState(IRepository<IDatabaseUser> repository)
                => new UnsyncableUserState(repository);

            protected override IDatabaseUser CreateDirtyEntity()
                => User.Dirty(new Ultrawave.Models.User { Fullname = Guid.NewGuid().ToString() });
        }
    }
}
