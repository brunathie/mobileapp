using System;
using FluentAssertions;
using Xunit;

namespace Toggl.Multivac.Tests
{
    public sealed class EmailTests
    {
        public sealed class TheFromStringMethod
        {
            [Fact, LogIfTooSlow]
            public void ReturnsInvalidEmailIfTheProvidedEmailIsNotValid()
            {
                var email = Email.FromString("foo@");

                email.Should().Be(Email.Invalid);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsValidEmailIfTheProvidedEmailIsValid()
            {
                var email = Email.FromString("susancalvin@psychohistorian.museum");

                email.Should().NotBe(Email.Invalid);
            }
        }

        public sealed class TheIsValidProperty
        {
            [Fact, LogIfTooSlow]
            public void ReturnsFalseIfTheEmailWasCreatedUsingTheDefaultConstructor()
            {
                var email = new Email();

                email.IsValid.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsFalseIfTheEmailIsTheSameInstanceAsTheInvalidStaticProperty()
            {
                var email = Email.Invalid;

                email.IsValid.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsFalseForAnEmailCreatedWithAnInvalidEmailString()
            {
                var email = Email.FromString("foo@");

                email.IsValid.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTrueForAProperlyInitializedValidEmail()
            {
                var email = Email.FromString("susancalvin@psychohistorian.museum");

                email.IsValid.Should().BeTrue();
            }
        }

        public sealed class TheToStringMethod
        {
            [Fact, LogIfTooSlow]
            public void ReturnsNullWhenTheEmailIsInvalid()
            {
                var email = Email.Invalid;

                email.ToString().Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTheStringUsedForConstructionWhenTheEmailIsValid()
            {
                var emailString = "susancalvin@psychohistorian.museum";
                var email = Email.FromString(emailString);

                email.ToString().Should().Be(emailString);
            }
        }

        public sealed class TheToFullNameMethod
        {
            [Theory, LogIfTooSlow]
            [InlineData("john@gmail.com", "John")]
            [InlineData("john123@gmail.com", "John123")]
            [InlineData("john.smith@gmail.com", "John Smith")]
            [InlineData("multiple.names.are.used@gmail.com", "Multiple Names Are Used")]
            [InlineData("Already.Has.some.capital.Letters@toggl.com", "Already Has Some Capital Letters")]
            [InlineData("does-not-split-by-dashes@domain.com", "Does-not-split-by-dashes")]
            [InlineData("šimon@gmail.cz", "Šimon")]
            [InlineData("ägypter@gmail.de", "Ägypter")]
            [InlineData("леонтий@gmail.ru", "Леонтий")]
            [InlineData("ıwithout.a.dot@gmail.com", "Iwithout A Dot")]
            [InlineData("iwith.a.dot@gmail.com", "Iwith A Dot")]
            [InlineData("あabc@gmail.com", "あabc")]
            [InlineData("\"quoted..email\"@weird.but.valid.com", "Quoted Email")]
            public void CapitalizesEveryPartOfTheNameSplitByDots(string emailAddress, string expectedName)
            {
                var email = Email.FromString(emailAddress);

                var fullName = email.ToFullName();

                fullName.Should().Be(expectedName);
            }

            [Theory, LogIfTooSlow]
            [InlineData("twodots..inarow@gmail.com")]
            [InlineData(".starts.with.a.dot@domain.at")]
            [InlineData("just an invalid email")]
            [InlineData("@example.com")]
            [InlineData("emoji.\uD83D\uDE49mail@gmail.com")]
            [InlineData("emoji.🙉mail@gmail.com")]
            public void ReturnsEmtpyStringForInvalidEmails(string emailAddress)
            {
                var email = Email.FromString(emailAddress);

                var fullName = email.ToFullName();

                fullName.Should().Be(String.Empty);
            }
        }
    }
}
