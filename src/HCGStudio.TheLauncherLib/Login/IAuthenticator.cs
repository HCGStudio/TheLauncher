using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCGStudio.TheLauncherLib.Login
{
    public interface IAuthenticator
    {
        public ValueTask<Dictionary<string, string>> Authenticate();
    }
}