using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using DG.Tweening;

public class ConNXTUpdate : MonoBehaviour {
    public RectTransform RuuviMenu;
//Declare text boxes that will be updated
    public Text RUUVINameText_001 = null;   
    public Text TemperatureText_001 = null;    
    public Text HumidityText_001 = null;
    public Text PressureText_001 = null;
    public Text AccelXText_001 = null;
    public Text AccelYText_001 = null;
    public Text AccelZText_001 = null;
    public Text LastUpdatedText_001 = null;

    // Structure to hold the data for each RUUVI tag
    private struct RuuviTag
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

    RuuviTag[] Ruuvis;

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

    private int currentRuuviDisplayed = 0;
    // Use this for initialization
    void Start () {
        
        /*
        * 
        * 
        * 
        * 
        * 
        * 
        */
        //RuuviMenu.gameObject.SetActive(false);
        //RuuviMenu.DOAnchorPos(new Vector2(0.7f, 0.7f), 0f);
        //RuuviMenu.gameObject.SetActive(true);
        //RuuviMenu.DOAnchorPos(new Vector2(0f, 0.7f), 1.5f);
        /* 
        * 
        * 
        * 
        * 
        * 
        */
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
            Ruuvis = new RuuviTag[devices.Count];

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
        }
    }

    //1-4
    private void SetTextsToRuuviID(int RuuviID)
    {
        //Update the text fields on the gui
        RUUVINameText_001.text = "CoLab RUUVI Tag ID " + Ruuvis[RuuviID]._deviceID;
        TemperatureText_001.text = Ruuvis[RuuviID]._temperature + " °C";
        HumidityText_001.text = Ruuvis[RuuviID]._humidity + " %";
        PressureText_001.text = Ruuvis[RuuviID]._pressure + " hPa";
        AccelXText_001.text = Ruuvis[RuuviID]._accelerationX;
        AccelYText_001.text = Ruuvis[RuuviID]._accelerationY;
        AccelZText_001.text = Ruuvis[RuuviID]._accelerationZ;
        LastUpdatedText_001.text = "Last updated on " + Ruuvis[RuuviID]._timeStamp;
    }

    public void SetTextsToNextRuuvi()
    {
        currentRuuviDisplayed++;
        SetTextsToRuuviID((currentRuuviDisplayed)%4);
    }
}
