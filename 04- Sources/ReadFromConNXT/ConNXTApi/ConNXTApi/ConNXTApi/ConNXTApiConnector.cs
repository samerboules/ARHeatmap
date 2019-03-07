using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ConNXTApi
{
    /// <summary>
    /// Connector based on the conNXT Rest API using the conNXTApi Client
    /// </summary>
    public class ConNXTApiConnector : IDisposable
    {
        // Full conNXT API URL : "beta.connxt.eu"
        public string EndPoint { get; private set; }
        public string ClientId { get; private set; }
        public string ClientSecret { get; private set; }
        public string Scope { get; private set; }

        private HttpClient _httpClient;
        private DateTime _tokenValidUntil = DateTime.MinValue;
        private readonly object _tokenLock = new object();

        private class TokenResponse
        {
            public string AccessToken { get; set; }
            public int ExpiresIn { get; set; }
            public string TokenType { get; set; }
        }

        public ConNXTApiConnector(string endpoint, string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new Exception("Please add the Api endpoint to the server defintion");
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new Exception("Please add the Api Client Id and Client Secret to the server defintion");

            this.EndPoint = endpoint.StartsWith("https:") ? endpoint : "https://" + endpoint;
            this.EndPoint = this.EndPoint.EndsWith("/api") ? this.EndPoint : this.EndPoint + "/api";
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
            this.Scope = "devices:telemetry devices:get";
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
        }

        public async Task ConnectAsync()
        {
            await DisconnectAsync();
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(this.EndPoint)
            };

            Console.WriteLine("=======================================================" + "\r\n" +
                              "     Endpoint: " + this.EndPoint + "\r\n" +
                              "       Secret: " + this.ClientSecret + "\r\n" +
                              "        Scope: " + this.Scope + "\r\n" +
                              "=======================================================");
        }

        public Task DisconnectAsync()
        {
            if (_httpClient != null)
            {
                _httpClient.CancelPendingRequests();
                _httpClient.Dispose();
                _httpClient = null;
            }

            return Task.CompletedTask;
        }

        public Task<string> GetDevicesAsync(string deviceType = null)
        {

            if (_httpClient == null)
                throw new Exception("Get Devices not available as the API connection is not yet started");

            EnsureValidAuthentication();

            HttpResponseMessage response;
            response = _httpClient.GetAsync($"api/Devices").Result;
            
            if ((int)response.StatusCode != 200)
            {
                Console.WriteLine($"Get Devices Failed: {(int)response.StatusCode} - {response.ReasonPhrase}");
                return new Task<string>(null);
            }

            return response.Content.ReadAsStringAsync();
        }

        public Task<string> GetTelemetryAsync(string deviceId)
        {

            if (_httpClient == null)
                throw new Exception("Get Telemetry not available as the API connection is not yet started");

            EnsureValidAuthentication();

            HttpResponseMessage response = _httpClient.GetAsync($"api/Telemetry/{deviceId}/latest").Result;
            if ((int)response.StatusCode != 200)
            {
                Console.WriteLine($"Get Telemetry Failed: {(int)response.StatusCode} - {response.ReasonPhrase}");
                return new Task<string>(null);
            }

            return response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Ensures there is a valid authentication token by getting a new token if the old one expires
        /// Copied from the ConNXTApiCLient in the Task Module.
        /// </summary>
        /// <returns>An awaitable Task</returns>
        private void EnsureValidAuthentication()
        {
            // Check if the token expires within a minute
            var refreshTime = DateTime.Now.AddSeconds(-60);
            if (_tokenValidUntil < refreshTime)
            {
                lock (_tokenLock)
                {
                    if (_tokenValidUntil < refreshTime)
                    {
                        MultipartFormDataContent formData = new MultipartFormDataContent
                        {
                            { new StringContent("client_credentials"), "grant_type" },
                            { new StringContent(this.ClientId), "client_id" },
                            { new StringContent(this.ClientSecret), "client_secret" },
                            { new StringContent(string.Join(" ", this.Scope)), "scope" }
                        };

                        var request = new HttpRequestMessage(HttpMethod.Post, "connect/token")
                        {
                            Content = formData
                        };

                        var sendRequestTask = _httpClient.SendAsync(request);
                        sendRequestTask.Wait();
                        sendRequestTask.Result.EnsureSuccessStatusCode();

                        var responseContentReadTask = sendRequestTask.Result.Content.ReadAsStringAsync();
                        responseContentReadTask.Wait();

                        var result = JsonConvert.DeserializeObject<TokenResponse>(
                            responseContentReadTask.Result,
                            new JsonSerializerSettings
                            {
                                ContractResolver = new DefaultContractResolver
                                {
                                    NamingStrategy = new SnakeCaseNamingStrategy()
                                }
                            }
                        );

                        _tokenValidUntil = DateTime.Now.AddSeconds(result.ExpiresIn);
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    }
                }
            }
        }
    }
}
