using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Toggl.Multivac
{
    public struct Email
    {
        //Taken from reference source: https://github.com/Microsoft/referencesource/blob/master/System.ComponentModel.DataAnnotations/DataAnnotations/EmailAddressAttribute.cs#L54
        private const string pattern = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";
        private static readonly Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture); 

        private readonly string email;

        public static Email Invalid { get; } = new Email();

        private Email(string email) 
            : this() => this.email = email;
        
        public bool IsValid
            => !string.IsNullOrEmpty(email);

        public override string ToString() => email;

        public string ToFullName()
            => !IsValid
                ? String.Empty
                : String.Join(" ", split(email)?.Select(capitalize) ?? new string[0]);

        public static Email FromString(string email)
        {
            if (!CheckEmailStringValidity(email)) return Invalid;
            return new Email(email);
        }

        public static bool CheckEmailStringValidity(string email)
            => email != null && regex.Match(email).Length > 0;

        private IEnumerable<string> split(string email)
            => email?.Split('@')?.First()?.Replace("\"", "").Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

        private string capitalize(string name)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            return name[0].ToString().ToUpper() + name.Substring(1);
        }
    }
}
