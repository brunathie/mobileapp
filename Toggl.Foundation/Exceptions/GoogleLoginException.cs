using System;
namespace Toggl.Foundation.Exceptions
{
    public class GoogleLoginException : Exception
    {
        public bool LoginWasCanceled { get; }

        public GoogleLoginException(bool loginWasCanceled)
        {
            LoginWasCanceled = loginWasCanceled;
        }
    }
}
