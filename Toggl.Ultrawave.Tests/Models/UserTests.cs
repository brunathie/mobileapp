using System;
using FluentAssertions;
using Toggl.Ultrawave.Models;
using Xunit;

namespace Toggl.Ultrawave.Tests.Models
{
    public sealed class UserTests
    {
        public sealed class TheUserModel
        {
            private string validJson =>
                "{\"id\":9000,\"api_token\":\"1971800d4d82861d8f2c1651fea4d212\",\"default_workspace_id\":777,\"email\":\"johnt@swift.com\",\"fullname\":\"John Swift\",\"timeofday_format\":\"h:mm A\",\"date_format\":\"MM/DD/YYYY\",\"beginning_of_week\":0,\"language\":\"en_US\",\"image_url\":\"https://www.toggl.com/system/avatars/9000/small/open-uri20121116-2767-b1qr8l.png\",\"at\":\"2013-03-06T12:18:42+00:00\"}";

            private User validUser => new User
            {
                Id = 9000,
                ApiToken = "1971800d4d82861d8f2c1651fea4d212",
                DefaultWorkspaceId = 777,
                Email = "johnt@swift.com",
                Fullname = "John Swift",
                TimeOfDayFormat = "h:mm A",
                DateFormat = "MM/DD/YYYY",
                BeginningOfWeek = 0,
                Language = "en_US",
                ImageUrl = "https://www.toggl.com/system/avatars/9000/small/open-uri20121116-2767-b1qr8l.png",
                At = new DateTimeOffset(2013, 3, 6, 12, 18, 42, TimeSpan.Zero),
            };

            [Fact, LogIfTooSlow]
            public void HasConstructorWhichCopiesValuesFromInterfaceToTheNewInstance()
            {
                var clonedObject = new User(validUser);

                clonedObject.Should().NotBeSameAs(validUser);
                clonedObject.ShouldBeEquivalentTo(validUser, options => options.IncludingProperties());
            }

            [Fact, LogIfTooSlow]
            public void CanBeDeserialized()
            {
                SerializationHelper.CanBeDeserialized(validJson, validUser);
            }

            [Fact, LogIfTooSlow]
            public void CanBeSerialized()
            {
                SerializationHelper.CanBeSerialized(validJson, validUser);
            }
        }
    }
}
