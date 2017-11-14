using System;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class UserApi : BaseApi, IUserApi
    {
        private readonly UserEndpoints endPoints;
        private readonly IJsonSerializer serializer;

        public UserApi(UserEndpoints endPoints, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials)
            : base(apiClient, serializer, credentials)
        {
            this.endPoints = endPoints;
            this.serializer = serializer;
        }

        public IObservable<IUser> Get()
            => CreateObservable<User>(endPoints.Get, AuthHeader);

        public IObservable<IUser> SignUp(Email email, string password)
        {
            if (!email.IsValid)
                throw new ArgumentException(nameof(email));

            var dto = new SignUpParameters
            {
                Email = email.ToString(),
                Password = password,
                Workspace = new SignUpParameters.WorkspaceParameters
                {
                    Name = $"{email.ToFullName()}'s workspace",
                    InitialPricingPlan = PricingPlans.Free
                }
            };
            var json = serializer.Serialize(dto, SerializationReason.Post, null);
            return CreateObservable<User>(endPoints.Post, new HttpHeader[0], json);
        }

        private class SignUpParameters
        {
            public string Email { get; set; }

            public string Password { get; set; }

            public WorkspaceParameters Workspace { get; set; }

            internal class WorkspaceParameters
            {
                public string Name { get; set; }

                public PricingPlans InitialPricingPlan { get; set; }
            }
        }

        public IObservable<IUser> Update(IUser user)
            => CreateObservable(endPoints.Put, AuthHeader, user as User ?? new User(user), SerializationReason.Post);
    }
}
