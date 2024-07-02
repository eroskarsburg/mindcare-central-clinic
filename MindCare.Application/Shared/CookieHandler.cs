using MindCare.Application.Entities;
using System.Net;

namespace MindCare.Application.Shared
{
    public class CookieHandler
    {
        private readonly HttpClient _client;
        private readonly CookieContainer _cookieContainer;

        public CookieHandler()
        {
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer
            };
            _client = new HttpClient(handler);
        }

        public async Task<string> MakeRequestAsync(string url)
        {
            var response = await _client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public Cookie GetCookie(Uri uri, string cookieName)
        {
            var cookies = _cookieContainer.GetCookies(uri);

            return cookies[cookieName];
        }

        public async Task<Cookie> ReturnCookieValue(string url)
        {
            var uri = new Uri("https://" + url);

            var cookieHandler = new CookieHandler();

            var content = await cookieHandler.MakeRequestAsync(url);

            var cookie = cookieHandler.GetCookie(uri, "cookiesession");

            return cookie;
        }

        public Session GetCurrent(string url)
        {
            Session? current = null;
            var cookie = ReturnCookieValue(url);

            if (cookie != null)
            {
                string[]? splitted = cookie.ToString().Split(',');

                User user = new()
                {
                    Username = splitted[1],
                    Password = splitted[2]
                };
            }
            return current;
        }
    }
}
