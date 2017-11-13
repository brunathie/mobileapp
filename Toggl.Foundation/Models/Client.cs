using System;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Models
{
    internal partial class Client
    {
        internal sealed class Builder
        {
            private const string errorMessage = "You need to set the {0} before building a client";

            public static Builder Create(long id) => new Builder(id);

            public long Id { get; }

            public string Name { get; private set; }

            public SyncStatus SyncStatus { get; private set; }

            public long? WorkspaceId { get; private set; }

            public DateTimeOffset? At { get; private set; }

            public DateTimeOffset? ServerDeletedAt { get; private set; }

            public bool IsDeleted { get; private set; }

            private Builder(long id)
            {
                Id = id;
            }

            public Client Build()
            {
                ensureValidity();
                return new Client(this);
            }

            public Builder SetSyncStatus(SyncStatus syncStatus)
            {
                SyncStatus = syncStatus;
                return this;
            }

            public Builder SetWorkspaceId(long workspaceId)
            {
                WorkspaceId = workspaceId;
                return this;
            }

            public Builder SetName(string name)
            {
                Name = name;
                return this;
            }

            public Builder SetAt(DateTimeOffset at)
            {
                At = at;
                return this;
            }

            public Builder SetServerDeletedAt(DateTimeOffset? serverDeleteAt)
            {
                ServerDeletedAt = serverDeleteAt;
                return this;
            }

            public Builder SetIsDeleted(bool isDeleted)
            {
                IsDeleted = isDeleted;
                return this;
            }

            private void ensureValidity()
            {
                if (string.IsNullOrEmpty(Name))
                    throw new InvalidOperationException(string.Format(errorMessage, "name"));

                if (WorkspaceId == null || WorkspaceId == 0)
                    throw new InvalidOperationException(string.Format(errorMessage, "workspace id"));

                if (At == null)
                    throw new InvalidOperationException(string.Format(errorMessage, "at"));
            }
        }

        private Client(Builder builder)
        {
            Id = builder.Id;
            Name = builder.Name;
            At = builder.At.Value;
            IsDeleted = builder.IsDeleted;
            SyncStatus = builder.SyncStatus;
            WorkspaceId = builder.WorkspaceId.Value;
            ServerDeletedAt = builder.ServerDeletedAt;
        }
    }
}
