using System.Net.Http;
using System.Reflection;

namespace HCGStudio.TheLauncherLib.Tools
{
    public static class HttpSingleton
    {
        private static HttpClient? _http;

        public static HttpClient Http
        {
            get
            {
                if (_http != null) return _http;
                _http = new(new HttpClientHandler {AllowAutoRedirect = true});
                var name = Assembly.GetExecutingAssembly().GetName();
                _http.DefaultRequestHeaders.UserAgent.Add(new(name.Name!, name.Version?.ToString()));
                return _http;
            }
        }
    }
}