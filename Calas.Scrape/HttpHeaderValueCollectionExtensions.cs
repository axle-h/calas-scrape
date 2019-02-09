using System;
using System.Net.Http.Headers;

namespace Calas.Scrape
{
    public static class HttpHeaderValueCollectionExtensions
    {
        public static HttpHeaderValueCollection<THeader> Set<THeader>(this HttpHeaderValueCollection<THeader> collection, params string[] values)
            where THeader : class
        {
            foreach (var value in values)
            {
                collection.ParseAdd(value);
            }

            return collection;
        }

        public static void SetChromeUserAgent(this HttpRequestHeaders headers)
        {
            headers.Host = "calas.uk";
            headers.AcceptLanguage.Set("en-GB", "en-US;q=0.9", "en;q=0.8");
            headers.CacheControl = new CacheControlHeaderValue
                                   {
                                       MaxAge = TimeSpan.Zero
                                   };
            headers.Connection.Set("keep-alive");
            headers.Add("DNT", "1");
            headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            headers.UserAgent.ParseAdd("AppleWebKit/537.36 (KHTML, like Gecko)");
            headers.UserAgent.ParseAdd("Chrome/71.0.3578.98 Safari/537.36");
        }
    }
}