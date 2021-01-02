using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using static HCGStudio.TheLauncherLib.Tools.HttpSingleton;

namespace HCGStudio.TheLauncherLib.Login
{
    public class MicrosoftAuthenticator : IAuthenticator
    {
        public MicrosoftAuthenticator(string refreshToken = "")
        {
            RefreshToken = refreshToken;
            LoginApp = OperatingSystem.IsWindows() ? "HCGStudio.TheLauncherLogin.exe" : "TheLauncherLogin";
        }

        public string RefreshToken { get; private set; }
        private string LoginApp { get; }
#nullable disable
        private async Task<(string, string)> GetTokenByCodeResponse(Stream jsonStream)
        {
            var rpsTicketContent =
                await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(jsonStream);
            var rpsTicket = rpsTicketContent["access_token"].ToString();
            var refreshToken = rpsTicketContent["refresh_token"].ToString();
            var xblMessage =
                new HttpRequestMessage(HttpMethod.Post, "https://user.auth.xboxlive.com/user/authenticate");
            xblMessage.Headers.Accept.Add(new("application/json"));
            xblMessage.Content = new StringContent(JsonSerializer.Serialize(new
            {
                Properties = new
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = rpsTicket
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            }), Encoding.UTF8, "application/json");
            var getXbl = await Http.SendAsync(xblMessage);
            var xblContent =
                await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(
                    await getXbl.Content.ReadAsStreamAsync());
            var xblToken = xblContent?["Token"].ToString();


            var xstsMessage = new HttpRequestMessage(HttpMethod.Post, "https://xsts.auth.xboxlive.com/xsts/authorize");
            xstsMessage.Headers.Accept.Add(new("application/json"));
            xstsMessage.Content = new StringContent(JsonSerializer.Serialize(new
            {
                Properties = new
                {
                    SandboxId = "RETAIL",
                    UserTokens = new[] {xblToken}
                },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT"
            }), Encoding.UTF8, "application/json");
            var getXsts = await Http.SendAsync(xstsMessage);

            var xstsContent =
                await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(
                    await getXsts.Content.ReadAsStreamAsync());
            var xstsToken = xstsContent["Token"].ToString();
            var displayClaims = (JsonElement) xstsContent["DisplayClaims"];
            var xui = displayClaims.GetProperty("xui");
            var uhs = xui[0].GetProperty("uhs").GetString();

            var minecraftLogin = new HttpRequestMessage(HttpMethod.Post,
                "https://api.minecraftservices.com/authentication/login_with_xbox");
            minecraftLogin.Headers.Accept.Add(new("application/json"));
            minecraftLogin.Content = new StringContent(JsonSerializer.Serialize(new
            {
                identityToken = $"XBL3.0 x={uhs};{xstsToken}"
            }), Encoding.UTF8, "application/json");

            var minecraftResponse = await Http.SendAsync(minecraftLogin);
            var minecraft =
                await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(
                    await minecraftResponse.Content.ReadAsStreamAsync());
            return (minecraft["access_token"].ToString(), refreshToken);
        }

        private async Task<(string, string)> GetToken(string code)
        {
            var getRpsTicket = await
                Http.PostAsync("https://login.live.com/oauth20_token.srf",
                    new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", "00000000402b5328"),
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string>("redirect_uri", "https://login.live.com/oauth20_desktop.srf"),
                        new KeyValuePair<string, string>("scope", "service::user.auth.xboxlive.com::MBI_SSL")
                    }));

            return await GetTokenByCodeResponse(await getRpsTicket.Content.ReadAsStreamAsync());
        }

        private async Task<(string, string)> GetTokenByRefresh(string refreshToken)
        {
            var getRpsTicket = await
                Http.PostAsync("https://login.live.com/oauth20_token.srf",
                    new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", "00000000402b5328"),
                        new KeyValuePair<string, string>("refresh_token", refreshToken),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string>("redirect_uri", "https://login.live.com/oauth20_desktop.srf"),
                        new KeyValuePair<string, string>("scope", "service::user.auth.xboxlive.com::MBI_SSL")
                    }));

            return await GetTokenByCodeResponse(await getRpsTicket.Content.ReadAsStreamAsync());
        }

        private async ValueTask<(string, Guid)> GetGuidAndUserNameFromAccessTokenAsync(string accessToken)
        {
            var request =
                new HttpRequestMessage(HttpMethod.Get, " https://api.minecraftservices.com/minecraft/profile");
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(accessToken);
            var response = await Http.SendAsync(request);
            var result =
                await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(
                    await response.Content.ReadAsStreamAsync());
            if (result != null && result.TryGetValue("id", out var userId) &&
                result.TryGetValue("name", out var userName))
                return (userName.ToString(), Guid.Parse(userId.ToString() ?? throw new InvalidOperationException()));
            throw new AuthenticationException("Login failed.", (int) response.StatusCode, new());
        }

        public async ValueTask<Dictionary<string, string>> Authenticate()
        {
            string accessToken, refreshToken;
            if (!string.IsNullOrEmpty(RefreshToken))
            {
                (accessToken, refreshToken) = await GetTokenByRefresh(RefreshToken);
            }
            else
            {
                using var login = Process.Start(new ProcessStartInfo(LoginApp)
                {
                    RedirectStandardOutput = true
                });
                if (login == null)
                    throw new NullReferenceException();
                await login.WaitForExitAsync();
                var result = await login.StandardOutput.ReadToEndAsync();
                if (string.IsNullOrEmpty(result) || !result.StartsWith("https://login.live.com/oauth20_desktop.srf"))
                    throw new AuthenticationException("Login failed", 403, new());
                var code = HttpUtility.ParseQueryString(result)[0];
                (accessToken, refreshToken) = await GetToken(code);
            }

            RefreshToken = refreshToken;
            var (name, guid) = await GetGuidAndUserNameFromAccessTokenAsync(accessToken);
            return await Task.FromResult(new Dictionary<string, string>
            {
                {"auth_player_name", name},
                {"auth_uuid", guid.ToString("N")},
                {"auth_access_token", accessToken},
                {"user_type", ""}
            });
        }
#nullable restore
    }
}