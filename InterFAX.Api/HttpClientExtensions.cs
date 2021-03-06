﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using InterFAX.Api.Dtos;

namespace InterFAX.Api
{
    internal static class HttpClientExtensions
    {
        public static async Task<T> GetResourceAsync<T>(this HttpClient httpClient, string requestUri, IOptions options = null)
        {
            var task = httpClient.GetAsync(requestUri.AddOptions(options));
            var response = await task;
            if (response.IsSuccessStatusCode) return await response.Content.ReadAsAsync<T>();

            throw new ApiException(response.StatusCode, await response.ToError());
        }

        public static async Task<T> PostAsync<T>(this HttpClient httpClient, string requestUri, IOptions options = null)
        {
            var task = httpClient.PostAsync(requestUri.AddOptions(options), new StringContent(""));
            var response = await task;
            if (response.IsSuccessStatusCode) return await response.Content.ReadAsAsync<T>();

            throw new ApiException(response.StatusCode, await response.ToError());
        }

        public static async Task<HttpResponseMessage> PostAsync(this HttpClient httpClient, string requestUri, IOptions options = null, HttpContent content = null)
        {
            var task = httpClient.PostAsync(requestUri.AddOptions(options), content ?? new StringContent(""));
            var response = await task;
            if (response.IsSuccessStatusCode) return response;

            throw new ApiException(response.StatusCode, await response.ToError());
        }

        public static async Task<HttpResponseMessage> PostRangeAsync(this HttpClient httpClient, string requestUri, ByteArrayContent content, RangeHeaderValue range)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Range = range;
            request.Content = content;
            var task = httpClient.SendAsync(request);
            var response = await task;
            if (response.IsSuccessStatusCode) return response;

            throw new ApiException(response.StatusCode, await response.ToError());
        }

        public static async Task<string> DeleteResourceAsync(this HttpClient httpClient, string requestUri)
        {
            var task = httpClient.DeleteAsync(requestUri);
            var response = await task;
            if (response.IsSuccessStatusCode) return response.ReasonPhrase;

            throw new ApiException(response.StatusCode, await response.ToError());
        }

        public static string AddOptions(this string requestUri, IOptions options)
        {
            if (options == null) return requestUri;

            var optionsDictionary = options.ToDictionary();
            if (optionsDictionary.Keys.Count == 0) return requestUri;

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            foreach (var key in optionsDictionary.Keys)
                queryString[key] = optionsDictionary[key];
            return $"{requestUri}?{queryString}";
        }

        private static async Task<Error> ToError(this HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentLength == 0)
            {
                return new Error
                {
                    Code = (int)response.StatusCode,
                    Message = response.ReasonPhrase,
                    MoreInfo = "No content returned"
                };
            }

            try { return await response.Content.ReadAsAsync<Error>(); }
            catch 
            { 
                return new Error 
                { 
                    Code = (int)response.StatusCode, 
                    Message = response.ReasonPhrase,
                    MoreInfo = await response.Content.ReadAsStringAsync()
                }; 
            }
        }
    }
}