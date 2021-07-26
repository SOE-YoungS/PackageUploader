﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Http
{
    public abstract class HttpRestClient : IHttpRestClient, IDisposable
    {
        private readonly string _clientRequestId;
        private readonly ILogger _logger;
        protected readonly HttpClient HttpClient;

        protected HttpRestClient(ILogger logger, HttpClient httpClient)
        {
            _clientRequestId = "";
            _logger = logger;
            HttpClient = httpClient;
        }

        public async Task<T> GetAsync<T>(string subUrl)
        {
            try
            {
                LogRequestVerboseAsync("GET " + subUrl, _clientRequestId);

                using var response = await HttpClient.GetAsync(subUrl).ConfigureAwait(false);
                var serverRequestId = GetRequestIdFromHeader(response);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonConvert.DeserializeObject<T>(responseString, _jsonSetting);

                    LogResponseVerboseAsync(result, serverRequestId);

                    return result;
                }

                throw new HttpRequestException($"GET '{subUrl}' failed with {response.StatusCode} [RequestId: {serverRequestId}].", null, response.StatusCode);
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogExceptionAsync(ex);
                throw;
            }
        }

        private static string GetRequestIdFromHeader(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Request-ID", out IEnumerable<string> headerValues))
            {
                return headerValues.FirstOrDefault();
            }

            return string.Empty;
        }

        private void LogRequestVerboseAsync(string requestUrl, object requestBody = null)
        {
            _logger.LogTrace(requestUrl);
            _logger.LogTrace(LogHeader);
            _logger.LogTrace("{requestUrl} [ClientRequestId: {_clientRequestId}]", requestUrl, _clientRequestId);
            if (requestBody != null)
            {
                _logger.LogTrace("Request Body:");
                var json = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
                _logger.LogTrace(json);
            }
            _logger.LogTrace(string.Empty);
        }

        private void LogResponseVerboseAsync(object obj, string serverRequestId)
        {
            _logger.LogTrace("Response Body: [RequestId: {serverRequestId}]", serverRequestId);
            var json = obj == null ? string.Empty : JsonConvert.SerializeObject(obj, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
            _logger.LogTrace(json);
            _logger.LogTrace(LogHeader);
        }

        private void LogExceptionAsync(Exception ex)
        {
            _logger.LogError(ex, "Exception:");
        }

        public void Dispose()
        {
            ((IDisposable)HttpClient).Dispose();
        }

        private readonly JsonSerializerSettings _jsonSetting = new();
        private const string LogHeader = "------------------------------------------------------------------------------------------";
    }
}
