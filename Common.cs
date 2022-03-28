using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MinimaxAPISample
{
    public class Common
    {
        #region AutGetAccessToken
        public static string AutGetAccessToken(string url, string clientId, string clientSecret, string username, string password)
        {
            var postData = $"client_id={clientId}&client_secret={clientSecret}&grant_type=password&username={username}&password={password}&scope=minimax.si";
            var data = Encoding.UTF8.GetBytes(postData);

            using (var client = new HttpClient())
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new ByteArrayContent(data);
                requestMessage.Content.Headers.ContentLength = data.Length;
                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                var responseObj = client.SendAsync(requestMessage).Result;
                var responseString = responseObj.Content.ReadAsStringAsync().Result;

                if (responseString == null || responseString.ToLower() == "bad request")
                    return null;

                //Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
                //var accessToken = dict["access_token"].ToString();
                
                Token token = JsonConvert.DeserializeObject<Token>(responseString);

                return token.access_token;
            }
        }
        #endregion

        #region ApiGet
        public static JObject ApiGet(string url, string accessToken)
        {
            string result;
            HttpStatusCode status;

            var success = ApiGet(url, accessToken, out status, out result);

            Console.WriteLine(status);
            Console.WriteLine(result);

            JObject json = JObject.Parse(result);

            return json;
        }
        #endregion

        #region ApiGetRaw
        public static string ApiGetRaw(string url, string accessToken)
        {
            string result;
            HttpStatusCode status;

            var success = ApiGet(url, accessToken, out status, out result);

            return result;
        }
        #endregion

        #region ApiPost
        public static JObject ApiPost(string url, string accessToken, string data)
        {
            return ApiPostGeneric(url, accessToken, data, ApiGet);
        }
        #endregion

        #region ApiPostRaw
        public static string ApiPostRaw(string url, string accessToken, string data)
        {
            return ApiPostGeneric(url, accessToken, data, ApiGetRaw);
        }
        #endregion

        #region ApiPostGeneric
        private static T ApiPostGeneric<T>(string url, string accessToken, string data, Func<string, string, T> apiGet)
        {
            string result;
            HttpStatusCode status;
            string ourl;
            T json = default(T);

            ApiPost(url, accessToken, data, out ourl, out status, out result);

            Console.WriteLine(status);
            Console.WriteLine(result);

            if(ourl != null)
                json = apiGet(ourl, accessToken);

            return json;
        }

        #endregion

        #region ApiPut
        public static JObject ApiPut(string url, string accessToken, string data)
        {
            string result;
            HttpStatusCode status;

            ApiPut(url, accessToken, data, out status, out result);

            Console.WriteLine(status);
            Console.WriteLine(result);

            JObject json = null;
            if (result != null && result != string.Empty && result != "[]")
                json = JObject.Parse(result);

            return json;
        }
        #endregion

        #region ApiDelete
        public static void ApiDelete(string url, string accessToken)
        {
            string result;
            HttpStatusCode status;

            ApiDelete(url, accessToken, out status, out result);
        }
        #endregion

        #region ApiGet
        private static bool ApiGet(string apiMethodUrl, string accessToken, out HttpStatusCode statusCode, out string resultContentStr)
        {
            if (!apiMethodUrl.ToLower().StartsWith(Config.APIBaseUrl.ToLower()))
                apiMethodUrl = Config.APIBaseUrl + apiMethodUrl;

            bool success = false;

            resultContentStr = null;

            using (HttpClient httpClient = new HttpClient())
            {

                if (accessToken != null)
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var apiResponse = httpClient.GetAsync(apiMethodUrl).Result;

                statusCode = apiResponse.StatusCode;
                resultContentStr = apiResponse.Content.ReadAsStringAsync().Result;

                if (apiResponse.IsSuccessStatusCode)
                {
                    success = true;
                }
            }
            return success;
        }
        #endregion

        #region ApiPost
        private static void ApiPost(string apiMethodUrl, string accessToken, string data, out string newLocationUrl, out HttpStatusCode statusCode, out string validationMessages)
        {
            if (!apiMethodUrl.StartsWith(Config.APIBaseUrl))
                apiMethodUrl = Config.APIBaseUrl + apiMethodUrl;

            newLocationUrl = null;
            validationMessages = null;

            HttpContent ct = new StringContent(data);
            ct.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var apiResponse = httpClient.PostAsync(apiMethodUrl, ct).Result;

                statusCode = apiResponse.StatusCode;

                if (apiResponse.IsSuccessStatusCode)
                {
                    Uri newLocation = apiResponse.Headers.Location;
                    newLocationUrl = newLocation.AbsoluteUri;
                }
                else
                {
                    var contents = apiResponse.Content.ReadAsStringAsync();
                    validationMessages = contents.Result;
                }
            }
        }
        #endregion

        #region ApiPut
        private static void ApiPut(string apiMethodUrl, string accessToken, string data, out HttpStatusCode statusCode, out string result)
        {
            if (!apiMethodUrl.StartsWith(Config.APIBaseUrl))
                apiMethodUrl = Config.APIBaseUrl + apiMethodUrl;

            result = null;

            HttpContent ct = new StringContent(data);
            ct.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var apiResponse = httpClient.PutAsync(apiMethodUrl, ct).Result;

                statusCode = apiResponse.StatusCode;

                result = apiResponse.Content.ReadAsStringAsync().Result;
            }
        }
        #endregion

        #region ApiDelete
        private static void ApiDelete(string apiMethodUrl, string accessToken, out HttpStatusCode statusCode, out string validationMessages)
        {
            if (!apiMethodUrl.StartsWith(Config.APIBaseUrl))
                apiMethodUrl = Config.APIBaseUrl + apiMethodUrl;

            validationMessages = null;

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var apiResponse = httpClient.DeleteAsync(apiMethodUrl).Result;

                statusCode = apiResponse.StatusCode;

                if (!apiResponse.IsSuccessStatusCode)
                {
                    validationMessages = apiResponse.Content.ReadAsStringAsync().Result;
                }
            }
        }
        #endregion
    }

    #region Token
    public class Token
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public long expires_in { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
    }
    #endregion
}
