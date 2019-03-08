﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Castle.Config;
using Castle.Infrastructure.Exceptions;
using Castle.Infrastructure.Extensions;
using Castle.Infrastructure.Json;

namespace Castle.Infrastructure
{
    internal class HttpMessageSender : IMessageSender
    {
        private readonly HttpClient _httpClient;

        public HttpMessageSender(CastleOptions options)
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(options.BaseUrl), 
                Timeout = TimeSpan.FromMilliseconds(options.Timeout)
            };

            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(":" + options.ApiSecret));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        }

        public async Task<TResponse> Post<TResponse>(string endpoint, object payload)
            where TResponse : class, new()
        {
            var jsonContent = PayloadToJson(payload);
            var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = jsonContent
            };

            return await SendRequest<TResponse>(message);
        }

        public async Task<TResponse> Get<TResponse>(string endpoint) 
            where TResponse : class, new()
        {
            var message = new HttpRequestMessage(HttpMethod.Get, endpoint);
            return await SendRequest<TResponse>(message);
        }

        public async Task<TResponse> Put<TResponse>(string endpoint)
            where TResponse : class, new()
        {
            var message = new HttpRequestMessage(HttpMethod.Put, endpoint);
            return await SendRequest<TResponse>(message);
        }

        public async Task<TResponse> Delete<TResponse>(string endpoint, object payload)
            where TResponse : class, new()
        {
            var jsonContent = PayloadToJson(payload);
            var message = new HttpRequestMessage(HttpMethod.Delete, endpoint)
            {
                Content = jsonContent
            };
            return await SendRequest<TResponse>(message);
        }

        private async Task<TResponse> SendRequest<TResponse>(HttpRequestMessage requestMessage)
            where TResponse : class, new()
        {
            try
            {
                var response = await _httpClient.SendAsync(requestMessage);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvertForCastle.DeserializeObject<TResponse>(content);
                }

                throw await response.ToCastleException(requestMessage.RequestUri.AbsoluteUri);
            }
            catch (OperationCanceledException)
            {
                throw new CastleTimeoutException(
                    requestMessage.RequestUri.AbsoluteUri, 
                    (int)_httpClient.Timeout.TotalMilliseconds);
            }
        }

        private static StringContent PayloadToJson(object payload)
        {
            return new StringContent(
                JsonConvertForCastle.SerializeObject(payload), 
                Encoding.UTF8, 
                "application/json");
        }
    }
}