using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;


public class ConNXTUpdate : MonoBehaviour {

    //Declare text boxes that will be updated
    public Text RUUVINameText = null;
    public Text TemperatureText = null;
    public Text HumidityText = null;
    public Text PressureText = null;
    public Text AccelXText = null;
    public Text AccelYText = null;
    public Text AccelZText = null;
    public Text LastUpdatedText = null;

    // Structure to hold the data for each RUUVI tag
    struct RuuviTag
    {
        // Declaring different data types 
        public string _deviceID;
        public string _timeStamp;
        public string _temperature;
        public string _humidity;
        public string _pressure;
        public string _accelerationX;
        public string _accelerationY;
        public string _accelerationZ;
    }

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
            /*
            Console.WriteLine("=======================================================" + "\r\n" +
                              "     Endpoint: " + this.EndPoint + "\r\n" +
                              "       Secret: " + this.ClientSecret + "\r\n" +
                              "        Scope: " + this.Scope + "\r\n" +
                              "=======================================================");
             */

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
                //Console.WriteLine($"Get Devices Failed: {(int)response.StatusCode} - {response.ReasonPhrase}");
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
                //Console.WriteLine($"Get Telemetry Failed: {(int)response.StatusCode} - {response.ReasonPhrase}");
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

    //Reads the latest data on ConNXT for all available RUUVI tags
    private void updateMeasurements()
    {
        TemperatureText.text = TemperatureText.text = "99" + " °C"; ;
        using (var api = new ConNXTApiConnector("https://beta.connxt.eu", "52840b9f-23db-475d-a920-272217be4402", "ZTXMI5Em/676HmLL0WUQUQ=="))
        {
            api.ConnectAsync().Wait();

            // Retrieve the devices
            string devicesString = api.GetDevicesAsync().Result;
            if (!string.IsNullOrEmpty(devicesString))
            {
                JArray devices = JArray.Parse(devicesString);
                //Console.WriteLine($"{devices.Count} devices found.");
                //Console.WriteLine();

                //Create array of RuuviTags by the number of devices found
                RuuviTag[] Ruuvis = new RuuviTag[devices.Count];

                //local index to fill the structure of devices. Note that device number 0 is the gateway (RaspberryPi)
                int localindex = 0;

                // Loop over all devices
                foreach (JObject device in devices)
                {
                    string deviceId = device["deviceUid"].Value<string>();
                    //Console.WriteLine($"Telemetry for device: {deviceId}");


                    Ruuvis[localindex]._deviceID = deviceId;
                    //Console.WriteLine($"_deviceID: {Ruuvis[localindex]._deviceID}");

                    // Retrieve the telemetry for the device
                    string telemetryString = api.GetTelemetryAsync(deviceId).Result;
                    if (!string.IsNullOrEmpty(telemetryString))
                    {
                        // Loop over the telemetry values
                        JObject telemetry = JObject.Parse(telemetryString);
                        //Console.WriteLine($"{"TimeStamp",-15} = {telemetry["messageTimeStamp"]}");

                        //Update timestamp in structure
                        Ruuvis[localindex]._timeStamp = telemetry["messageTimeStamp"].ToString();

                        foreach (JObject dataPoint in telemetry["dataPoints"] as JArray)
                        {
                            //Console.WriteLine($"{dataPoint["key"],-15} = {dataPoint["value"]}");

                            //Fill the structure with the updated data
                            if (dataPoint["key"].ToString() == "AccelerationX")
                            {
                                Ruuvis[localindex]._accelerationX = dataPoint["value"].ToString();
                            }
                            else if (dataPoint["key"].ToString() == "AccelerationY")
                            {
                                Ruuvis[localindex]._accelerationY = dataPoint["value"].ToString();
                            }
                            else if (dataPoint["key"].ToString() == "AccelerationZ")
                            {
                                Ruuvis[localindex]._accelerationZ = dataPoint["value"].ToString();
                            }
                            else if (dataPoint["key"].ToString() == "Temperature")
                            {
                                Ruuvis[localindex]._temperature = dataPoint["value"].ToString();
                            }
                            else if (dataPoint["key"].ToString() == "Pressure")
                            {
                                Ruuvis[localindex]._pressure = dataPoint["value"].ToString();
                            }
                            else if (dataPoint["key"].ToString() == "Humidity")
                            {
                                Ruuvis[localindex]._humidity = dataPoint["value"].ToString();
                            }

                        }
                    }
                    localindex++;
                    //Console.WriteLine();
                }

                //Update the text fields on the gui
                RUUVINameText.text = "CoLab RUUVI Tag ID " + Ruuvis[1]._deviceID;
                TemperatureText.text = Ruuvis[1]._temperature + " °C";
                HumidityText.text = Ruuvis[1]._humidity + " %";
                PressureText.text = Ruuvis[1]._pressure + " hPa";
                AccelXText.text = Ruuvis[1]._accelerationX;
                AccelYText.text = Ruuvis[1]._accelerationY;
                AccelZText.text = Ruuvis[1]._accelerationZ;
                LastUpdatedText.text = "Last updated on " + Ruuvis[1]._timeStamp;
            }
        }
    }

    // Use this for initialization
    void Start () {
        InvokeRepeating("updateMeasurements", 0f, 10f);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
