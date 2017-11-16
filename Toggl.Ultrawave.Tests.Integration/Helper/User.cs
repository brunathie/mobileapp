using System;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Tests.Integration.Helper;

namespace Toggl.Ultrawave.Tests.Integration
{
    internal static class User
    {
        public static async Task<Credentials> Create()
        {
            var (email, password) = await CreateEmailPassword();

            return Credentials.WithPassword(email, password);
        }

        public static async Task<(Email email, string password)> CreateEmailPassword()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            var email = $"{Guid.NewGuid()}@mocks.toggl.com".ToEmail();
            var password = "123456";

            var api = new TogglApi(new ApiConfiguration(ApiEnvironment.Staging, Credentials.None, Configuration.UserAgent));
            await api.User.SignUp(email, password);

            return (email, password);
        }
    }
}
