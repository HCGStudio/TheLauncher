using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCGStudio.TheLauncherLib.Login
{
    public class OfflineAuthenticator : IAuthenticator
    {
        public string UserName { get; set; } = "Steve";
        public Guid Token { get; set; }

        public async ValueTask<Dictionary<string, string>> Authenticate()
        {
            return await Task.FromResult(new Dictionary<string, string>
            {
                {"auth_player_name", UserName},
                {"auth_uuid", Token.ToString("N")},
                {"auth_access_token", Token.ToString("N")},
                {"user_type", "Legacy"}
            });
        }
    }
}