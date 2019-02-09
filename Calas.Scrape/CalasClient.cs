using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Calas.Scrape.Models;
using Calas.Scrape.Scrapers;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Calas.Scrape
{
    public class CalasClient : IDisposable
    {
        private const int BatchSize = 10;
        private static readonly Uri BaseUrl = new Uri("https://calas.uk/");

        private readonly ILogger<CalasClient> _logger;
        private readonly HttpClientHandler _handler;
        private readonly HttpClient _htmlClient;
        private readonly HttpClient _ajaxClient;

        public CalasClient(ILogger<CalasClient> logger)
        {
            _logger = logger;
            var cookieContainer = new CookieContainer();
            _handler = new HttpClientHandler
                       {
                           CookieContainer = cookieContainer,
                           UseCookies = true,
                           AllowAutoRedirect = true
                       };
            _ajaxClient = new HttpClient(_handler, false) { BaseAddress = BaseUrl };
            _ajaxClient.DefaultRequestHeaders.SetChromeUserAgent();
            _ajaxClient.DefaultRequestHeaders.Accept.Set("*/*");

            _htmlClient = new HttpClient(_handler, false) { BaseAddress = BaseUrl };
            _htmlClient.DefaultRequestHeaders.SetChromeUserAgent();
            _htmlClient.DefaultRequestHeaders.Accept.Set("text/html", "application/xhtml+xml", "application/xml;q=0.9", "*/*;q=0.8");
        }

        public async Task LoginAsync(string username, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "");
            request.Headers.Referrer = new Uri("https://calas.co/");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                                                        {
                                                            ["wfw_login"] = "1",
                                                            ["un"] = username,
                                                            ["pw"] = password
                                                        });

            try
            {
                var response = await _htmlClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                EnsureSessionCookie();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to login, wrong username or password maybe?", e);
            }
        }

        public async Task<ICollection<Contact>> GetContactsAsync(int start, int limit)
        {
            _logger.LogInformation("Scraping contacts");

            var contactScraper = new TableScraper<Contact>();
            var enquiryScraper = new TableScraper<Enquiry>();

            if (start > 0)
            {
                // When start > 0 , the server doesn't send a header...
                // So we need to go grab the first record to get the header.

                var table0Html = await RunAsync("clients/contacts", 0, 1);
                var contact0 = contactScraper.ScrapeTable(table0Html.DocumentNode).ToList();
            }

            var tableHtml = await RunAsync("clients/contacts", start, limit);


            var batches = contactScraper.ScrapeTable(tableHtml.DocumentNode)
                                        .Select((contact, i) => (batch: i / BatchSize, contact: contact))
                                        .GroupBy(x => x.batch)
                                        .Select(grp => grp.Select(x => x.contact).ToList());

            var contacts = new List<Contact>();
            foreach (var batch in batches)
            {
                _logger.LogInformation("Scraping contact ids = " + string.Join(", ", batch.Select(x => x.Id)));

                var batchTasks = batch.Select(async contact =>
                                                 {
                                                     var response = await _htmlClient.GetAsync($"clients/contacts/{contact.Id}");
                                                     response.EnsureSuccessStatusCode();
                                                     var htmlString = await response.Content.ReadAsStringAsync();

                                                     var html = new HtmlDocument();
                                                     html.LoadHtml(htmlString);

                                                     contactScraper.ScrapeForm(html, "clients_contact", contact);



                                                     var enquiryTable = await RunTabAsync(html.DocumentNode,
                                                                                          "load_contact_enqlist_tab",
                                                                                          ("contactid", contact.Id),
                                                                                          ("enquiryid", "0"));

                                                     contact.Enquiries = enquiryScraper.ScrapeTable(enquiryTable.DocumentNode).ToList();
                                                     foreach (var enquiry in contact.Enquiries)
                                                     {
                                                         enquiry.ContactId = contact.Id;
                                                     }

                                                     return contact;
                                                 });
                var batchContacts = await Task.WhenAll(batchTasks);
                contacts.AddRange(batchContacts);
            }

            return contacts;
        }

        private async Task<HtmlDocument> RunTabAsync(HtmlNode node, string tabName, params (string key, string value)[] extraContent)
        {
            var result = await SubmitRunFormAsync(node, extraContent.Append(("ajax_task", tabName)).ToArray());
            var tabHtml = JsonConvert.DeserializeAnonymousType(result, new {Html = ""}).Html;
            var tabDocument = new HtmlDocument();
            tabDocument.LoadHtml(tabHtml);
            return tabDocument;
        }


        private async Task<HtmlDocument> RunAsync(string path, int? start, int? limit)
        {
            EnsureSessionCookie();

            var htmlResponse = await _htmlClient.GetAsync(path);
            htmlResponse.EnsureSuccessStatusCode();

            var htmlString = await htmlResponse.Content.ReadAsStringAsync();
            var html = new HtmlDocument();
            html.LoadHtml(htmlString);

            var resultString = await SubmitRunFormAsync(html.DocumentNode,
                                                 ("querystart", start.ToString()),
                                                 ("querylimit", limit.ToString()));

            var result = JsonConvert.DeserializeObject<RunResponse>(resultString);

            _logger.LogInformation($"retrieved {result.Count} {path} records from total: {result.Total}");

            var document = new HtmlDocument();
            document.LoadHtml(result.Table);

            return document;
        }

        private async Task<string> SubmitRunFormAsync(HtmlNode node, params (string key, string value)[] extraContent)
        {
            var runForm = node.SelectSingleNode(".//form[@action='/run.php']");
            if (runForm == null)
            {
                throw new InvalidOperationException("Cannot find /run.php form");
            }

            var requestDictionary = runForm.SelectNodes(".//input[@type='hidden']")
                                           .Select(x => (name: x.GetAttributeValue("name", ""), value: x.GetAttributeValue("value", "")))
                                           .GroupBy(x => x.name)
                                           .ToDictionary(x => x.Key, x => x.First().value);

            foreach (var (key, value) in extraContent)
            {
                requestDictionary[key] = value;
            }

            var content = new FormUrlEncodedContent(requestDictionary);
            var response = await _ajaxClient.PostAsync("/run.php", content);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }


        private void EnsureSessionCookie()
        {
            var cookies = _handler.CookieContainer.GetCookies(_htmlClient.BaseAddress);
            if (string.IsNullOrEmpty(cookies["PHPSESSID"]?.Value))
            {
                throw new InvalidOperationException("No session ID");
            }
        }

        public void Dispose()
        {
            _htmlClient?.Dispose();
            _ajaxClient?.Dispose();
            _handler?.Dispose();
        }

        private class RunResponse
        {
            [JsonProperty("sofar")]
            public int Count { get; set; }

            public int Total { get; set; }

            public string Table { get; set; }
        }
    }
}
