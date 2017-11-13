using System;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class UserApi : BaseApi, IUserApi
    {
        private readonly UserEndpoints endPoints;

        public UserApi(UserEndpoints endPoints, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials)
            : base(apiClient, serializer, credentials)
        {
            this.endPoints = endPoints;
        }

        public IObservable<IUser> Get()
            => CreateObservable<User>(endPoints.Get, AuthHeader);

        public IObservable<IUser> Update(IUser user)
            => CreateObservable(endPoints.Put, AuthHeader, user as User ?? new User(user), SerializationReason.Post);
    }
}
