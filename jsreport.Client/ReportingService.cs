﻿using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simple.OData.Client;

namespace JsReport
{
    public class ReportingService
    {
        private readonly string _username;
        private readonly string _password;
        public Uri ServiceUri { get; set; }

        public int Timeout { get; set; }

        public ReportingService(string serviceUri, string username, string password)
        {
            _username = username;
            _password = password;
            ServiceUri = new Uri(serviceUri);
            Timeout = 5000;
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient() {BaseAddress = ServiceUri};

            if (_username != null)
            {
                //ASCII
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                                                                           System.Convert.ToBase64String
                                                                                               (Encoding.UTF8.GetBytes(
                                                                                                   String.Format(
                                                                                                       "{0}:{1}",
                                                                                                       _username,
                                                                                                       _password))));
            }

            //  client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        public async Task<Report> RenderAsync(RenderRequest request)
        {
            request.Options = request.Options ?? new RenderOptions();
            request.CopyToDynamicTemplate();

            var client = CreateClient();

            var settings = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
            var response =
                await
                client.PostAsync("/api/report",
                                 new StringContent(JsonConvert.SerializeObject(request, settings), Encoding.UTF8,
                                                   "application/json"));

            if (response.StatusCode != HttpStatusCode.OK)
                throw new JsReportException("Unable to render template. ", response);

            response.EnsureSuccessStatusCode();

            return await ReportFromResponse(response);
        }

        private static async Task<Report> ReportFromResponse(HttpResponseMessage response)
        {
            var stream = await response.Content.ReadAsStreamAsync();

            return new Report
                {
                    Content = stream,
                    ContentType = response.Content.Headers.ContentType,
                    FileExtension = response.Headers.Single(k => k.Key == "File-Extension").Value.First(),
                    PermanentLink =
                        response.Headers.Any(k => k.Key == "Permanent-Link")
                            ? response.Headers.Single(k => k.Key == "Permanent-Link").Value.First()
                            : null
                };
        }

        public async Task<Report> ReadReportAsync(string permanentLink)
        {
            var client = CreateClient();

            var response = await client.GetAsync(permanentLink);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new JsReportException("Unable to retrieve report content. ", response);

            response.EnsureSuccessStatusCode();

            return await ReportFromResponse(response);
        }

        public async Task<IEnumerable<string>> GetRecipesAsync()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/api/recipe");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<string>>();
        }

        public async Task<IEnumerable<string>> GetEnginesAsync()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/api/engine");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<string>>();
        }

        public async Task<string> GetServerVersionAsync()
        {
            var client = CreateClient();

            return await client.GetStringAsync("/api/version");
        }

        public ODataClient CreateODataClient()
        {
            return new ODataClient(new ODataClientSettings()
                {
                    UrlBase = ServiceUri.ToString() + "odata",
                    BeforeRequest = (r) =>
                        {
                            if (_username == null)
                                return;

                            var encoded =
                                System.Convert.ToBase64String(
                                    System.Text.Encoding.UTF8.GetBytes(_username + ":" + _password));
                            r.Headers.Add("Authorization", "Basic " + encoded);
                        }
                });
        }
    }
}