namespace Toggl.Foundation.MvvmCross.Services
{
    public sealed class PasswordManagerResult
    {
        public string Email { get; }

        public string Password { get; }
        public static PasswordManagerResult None { get; } = new PasswordManagerResult("", "");

        public PasswordManagerResult(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }
}
