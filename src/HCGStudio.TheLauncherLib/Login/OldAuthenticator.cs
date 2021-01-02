using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static HCGStudio.TheLauncherLib.Tools.HttpSingleton;

namespace HCGStudio.TheLauncherLib.Login
{
    public class OldAuthenticator : IAuthenticator
    {
        public OldAuthenticator(string userName, string password, Guid clientToken)
        {
            UserName = userName;
            Password = password;
            ClientToken = clientToken;
        }

        public OldAuthenticator(string accessToken, Guid clientToken, Guid profileGuid, string playerName,
            bool userIsLegacy = false)
        {
            AccessToken = accessToken;
            ClientToken = clientToken;
            ProfileGuid = profileGuid;
            PlayerName = playerName;
            UserIsLegacy = userIsLegacy;
        }

        public string UserName { get; } = string.Empty;
        public string Password { get; } = string.Empty;
        public string AccessToken { get; } = string.Empty;
        public Guid ClientToken { get; }
        public Guid ProfileGuid { get; } = Guid.Empty;
        public string PlayerName { get; } = string.Empty;
        public bool UserIsLegacy { get; }
        public string BaseUrl { get; protected set; } = "https://authserver.mojang.com";

        public async ValueTask<Dictionary<string, string>> Authenticate()
        {
            var (accessToken, guid, name, legacy) =
                string.IsNullOrEmpty(AccessToken)
                    ? await AuthenticateWithPasswordAsync()
                    : await AuthenticateWithRefreshAsync();
            return await Task.FromResult(new Dictionary<string, string>
            {
                {"auth_player_name", name},
                {"auth_uuid", guid.ToString("N")},
                {"auth_access_token", accessToken},
                {"user_type", legacy ? "Legacy" : "Mojang"}
            });
        }

        private async ValueTask<(string, Guid, string, bool)> AuthenticateWithPasswordAsync()
        {
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                agent = new
                {
                    name = "minecraft",
                    version = 1
                },
                username = UserName,
                password = Password,
                clientToken = ClientToken.ToString("N"),
                requestUser = true
            }));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/authenticate")
            {
                Content = content
            };

            var response = await Http.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var resultString = await response.Content.ReadAsStringAsync();
                try
                {
                    var error = JsonConvert.DeserializeObject<RemoteError>(resultString);
                    throw new AuthenticationException(error.ErrorMessage, (int) response.StatusCode, error);
                }
                catch
                {
                    throw new AuthenticationException(resultString, (int) response.StatusCode, new());
                }
            }

            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var accessToken = result.SelectToken("accessToken")?.ToString() ?? string.Empty;
            var guidString = result.SelectToken("selectedProfile.id")?.ToString() ?? string.Empty;
            var guid = Guid.Parse(guidString);
            var name = result.SelectToken("selectedProfile.name")?.ToString() ?? string.Empty;
            var legacy = (bool) (result.SelectToken("selectedProfile.legacyProfile") ?? false);
            return (accessToken, guid, name, legacy);
        }

        private async ValueTask<(string, Guid, string, bool)> AuthenticateWithRefreshAsync()
        {
            if (await ValidateAsync())
                return (AccessToken, ProfileGuid, PlayerName, UserIsLegacy);
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                accessToken = AccessToken,
                clientToken = ClientToken.ToString("N"),
                selectedProfile = new
                {
                    id = ProfileGuid.ToString("N"),
                    name = PlayerName
                },
                requestUser = true
            }));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/refresh")
            {
                Content = content
            };

            var response = await Http.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var resultString = await response.Content.ReadAsStringAsync();
                try
                {
                    var error = JsonConvert.DeserializeObject<RemoteError>(resultString);
                    throw new AuthenticationException(error.ErrorMessage, (int) response.StatusCode, error);
                }
                catch
                {
                    throw new AuthenticationException(resultString, (int) response.StatusCode, new());
                }
            }

            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var accessToken = result.SelectToken("accessToken")?.ToString() ?? string.Empty;
            var guidString = result.SelectToken("selectedProfile.id")?.ToString() ?? string.Empty;
            var guid = Guid.Parse(guidString);
            var name = result.SelectToken("selectedProfile.name")?.ToString() ?? string.Empty;
            var legacy = (bool) (result.SelectToken("selectedProfile.legacyProfile") ?? false);
            return (accessToken, guid, name, legacy);
        }

        private async ValueTask<bool> ValidateAsync()
        {
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                accessToken = AccessToken,
                clientToken = ClientToken.ToString("N")
            }));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/validate")
            {
                Content = content
            };

            var response = await Http.SendAsync(request);
            return response.StatusCode == HttpStatusCode.NoContent;
        }
    }
}