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

    private string TokenEndPoint = "https://beta.connxt.eu/connect/token";
    private string DevicesEndPoint = "https://beta.connxt.eu/api/Devices";
    private string TelemetryEndPoint;
    private string ClientID = "52840b9f-23db-475d-a920-272217be4402";
    private string ClientSecret = "ZTXMI5Em/676HmLL0WUQUQ==";
    private string Token;

    private class TokenResponse
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
    }

    // Use this for initialization
    void Start () {
        //call update measurements in a coroutine
        StartCoroutine(UpdateDataFromConNXT());
    }

    //Reads the latest data on ConNXT for all available RUUVI tags
    IEnumerator UpdateDataFromConNXT()
    {
        while (true)
        {
            //Step 1: Get access token from ConNXT (POST request)
            //Create new www form
            WWWForm POSTForm = new WWWForm();

            //Add Fields for POST Request
            POSTForm.AddField("grant_type", "client_credentials");
            POSTForm.AddField("client_id", ClientID);
            POSTForm.AddField("client_secret", ClientSecret);
            POSTForm.AddField("scope", "devices:telemetry devices:get");

            //Transform POST fields into byte array in order to fit WWW constructor
            byte[] rawFormData = POSTForm.data;

            //Perform POST Request
            WWW request = new WWW(TokenEndPoint, rawFormData);

            //Wait until POST_Request retuns
            yield return request;

            //Deserialize the JSON return to extract the token
            var result = JsonConvert.DeserializeObject<TokenResponse>(
                request.text,
                new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                }
            );

            Token = result.AccessToken;

            //Step 2: Get the number of devices on ConNXT (GET request)
            Dictionary<string, string> headers_ = POSTForm.headers;
            headers_["Authorization"] = "Bearer " + Token;

            //Perform GET Request
            WWW DevicesRequest = new WWW(DevicesEndPoint, null, headers_);

            //Wait until DevicesRequest retuns
            yield return DevicesRequest;
            JArray devices = JArray.Parse(DevicesRequest.text);

            //Create array of RuuviTag structs by the number of devices found
            RuuviTag[] Ruuvis = new RuuviTag[devices.Count];

            //Step 3: Loop over all devices and read the telemetry data (GET request
            int localindex = 0;

            foreach (JObject device in devices)
            {
                string deviceId = device["deviceUid"].Value<string>();

                Ruuvis[localindex]._deviceID = deviceId;

                Dictionary<string, string> TelemetryHeaders = POSTForm.headers;
                TelemetryHeaders["Authorization"] = "Bearer " + Token;
                TelemetryEndPoint = $"https://beta.connxt.eu/api/Telemetry/{deviceId}/latest";
                WWW TelemetryRequest = new WWW(TelemetryEndPoint, null, TelemetryHeaders);

                //Wait until TelemetryRequest retuns
                yield return TelemetryRequest;

                // Retrieve the telemetry for the device
                string telemetryString = TelemetryRequest.text;
                if (!string.IsNullOrEmpty(telemetryString))
                {
                    // Loop over the telemetry values
                    JObject telemetry = JObject.Parse(telemetryString);

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
