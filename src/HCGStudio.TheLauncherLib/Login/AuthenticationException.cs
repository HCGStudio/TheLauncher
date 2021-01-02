using System;

namespace HCGStudio.TheLauncherLib.Login
{
    public class AuthenticationException : ApplicationException
    {
        public AuthenticationException(string message, int statusCode, RemoteError error) : base(message)
        {
            StatusCode = statusCode;
            Error = error;
        }

        public int StatusCode { get; }
        public RemoteError Error { get; }
    }
}