/* This script is developed by Samer Boules (samer.boules@ict.nl)
 * It's function is to connect to ConNXT server, recieve the telemetry data and save the data in a buffer
 * then update the text on the UI.
 * 
 * Connection to ConNXT is done in three steps
 * 0. Check internet connection. Update UI accordingly
 * 1. POST request to get a token using the conNXT credentials
 * 2. GET request using the token to get the devices
 * 3. GET request on EACH device to get the credintials
 * 
 * for ConNXT support please contact Samuel van Egmond (He helped alot with this project)
 */
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
using UnityEngine.XR.MagicLeap;

public class ConNXTUpdate : MonoBehaviour {
    #region Public Variables
    //That's only used in making animation of the menu
    public RectTransform RuuviMenu;
    //Declare UI text boxes that will be updated
    public Text RUUVINameText       = null;   
    public Text TemperatureText     = null;    
    public Text HumidityText        = null;
    public Text PressureText        = null;
    public Text AccelXText          = null;
    public Text AccelYText          = null;
    public Text AccelZText          = null;
    public Text LastUpdatedText     = null;
    //public Text DebuggingInfoText   = null;
    #endregion


    #region Private Variables
    // Structure to hold the data for each RUUVI tag
    private struct RuuviTag
    {
        public string _deviceID;
        public string _timeStamp;
        public string _temperature;
        public string _humidity;
        public string _pressure;
        public string _accelerationX;
        public string _accelerationY;
        public string _accelerationZ;
    }

    //Array of Ruuvis, each element corresponds to a Ruuvi Tag
    RuuviTag[] Ruuvis;

    //EndPoint to get the token
    private string tokenEndPoint    = "https://beta.connxt.eu/connect/token";

    //EndPoint to get the devices
    private string devicesEndPoint  = "https://beta.connxt.eu/api/Devices";

    //EndPoint for getting the telemetry. It's updated according to the deviceID
    //for example "https://beta.connxt.eu/api/Telemetry/CLRT001/latest"
    private string telemetryEndPoint;

    //ConNXT credential
    private string clientID         = "52840b9f-23db-475d-a920-272217be4402";
    private string clientSecret     = "ZTXMI5Em/676HmLL0WUQUQ==";

    //Token that will be recieved from ConNXT
    private string token;

    //This class is used to break down the Json of Token
    private class TokenResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }

    //Which Ruuvi you want to display on UI
    private int currentRuuviDisplayed = 1;

    PrivilegeRequester __privilegeRequester;
    bool firstLoopUpdate = true;
    #endregion

    private void Start()
    {
        InvokeRepeating("TriggerConNXTRepeating", 0f, 10f);
    }
    #region Unity Functions
    //Unity method called automatically each frame
    void Awake () {
        __privilegeRequester = GetComponent<PrivilegeRequester>();
        if (__privilegeRequester == null)
        {
            Debug.LogError("Missing PrivilegeRequester component");
            enabled = false;
            return;
        }
        __privilegeRequester.OnPrivilegesDone += HandlePrivilegesDone_;
        //StartCoroutine(UpdateDataFromConNXT());
    }

    void OnDestroy()
    {
        if (__privilegeRequester != null)
        {
            __privilegeRequester.OnPrivilegesDone -= HandlePrivilegesDone_;
        }
    }
    #endregion

    #region My Functions
    //Reads the latest data on ConNXT for all available RUUVI tags
    IEnumerator UpdateDataFromConNXT()
    {
        //Step 1: Get access token from ConNXT (POST request)
        //Create new www form
        WWWForm POSTForm = new WWWForm();

        //Add Fields for POST Request
        POSTForm.AddField("grant_type", "client_credentials");
        POSTForm.AddField("client_id", clientID);
        POSTForm.AddField("client_secret", clientSecret);
        POSTForm.AddField("scope", "devices:telemetry devices:get");

        //Transform POST fields into byte array in order to fit WWW constructor
        byte[] rawFormData = POSTForm.data;

        //Perform POST Request
        WWW request = new WWW(tokenEndPoint, rawFormData);

        //Wait until POST_Request retuns
        yield return request;


        TokenResponse result = JsonConvert.DeserializeObject<TokenResponse>(request.text);
        token = result.access_token;

        //Deserialize the JSON return to extract the token
        //var result = JsonConvert.DeserializeObject<TokenResponse>(
        //    request.text,
        //    new JsonSerializerSettings
        //    {
        //        ContractResolver = new DefaultContractResolver
        //        {
        //            NamingStrategy = new SnakeCaseNamingStrategy()
        //        }
        //    }
        //);

        //token = result.AccessToken;

        //Step 2: Get the number of devices on ConNXT (GET request)
        Dictionary<string, string> headers_ = POSTForm.headers;
        headers_["Authorization"] = "Bearer " + token;

        //Perform GET Request
        WWW DevicesRequest = new WWW(devicesEndPoint, null, headers_);

        //Wait until DevicesRequest retuns
        yield return DevicesRequest;
        JArray devices = JArray.Parse(DevicesRequest.text);

        //Create array of RuuviTag structs by the number of devices found
        Ruuvis = new RuuviTag[devices.Count];

        //Step 3: Loop over all devices and read the telemetry data (GET request)
        int localindex = 0;

        foreach (JObject device in devices)
        {
            string deviceId = device["deviceUid"].Value<string>();

            Ruuvis[localindex]._deviceID = deviceId;

            Dictionary<string, string> TelemetryHeaders = POSTForm.headers;
            TelemetryHeaders["Authorization"] = "Bearer " + token;
            telemetryEndPoint = $"https://beta.connxt.eu/api/Telemetry/{deviceId}/latest";
            WWW TelemetryRequest = new WWW(telemetryEndPoint, null, TelemetryHeaders);

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
        SetTextsToRuuviID(currentRuuviDisplayed);
    }

    //Private function that takes the number of Ruuvi you want to display and update the UI texts accordingly
    //RuuviID range: from 1 to 4 (Don't send in 0 because this is the Raspberry Pi gateway which has no telemetry
    private void SetTextsToRuuviID(int RuuviID)
    {
        RUUVINameText.text = "CoLab RUUVI Tag 00" + RuuviID.ToString() + "\nDeviceID: " + Ruuvis[RuuviID]._deviceID;
        if (Ruuvis[RuuviID]._temperature == null)
        {
            TemperatureText.text = " ";
            HumidityText.text = " ";
            PressureText.text = "";
            AccelXText.text = "";
            AccelYText.text = "";
            AccelZText.text = ""; ;
            LastUpdatedText.text = "Loading...";
        }
        else
        {
            //Update the text fields on the gui
            TemperatureText.text = Ruuvis[RuuviID]._temperature + " °C";
            HumidityText.text = Ruuvis[RuuviID]._humidity + " %";
            PressureText.text = Ruuvis[RuuviID]._pressure + " hPa";
            AccelXText.text = Ruuvis[RuuviID]._accelerationX + " m/s2";
            AccelYText.text = Ruuvis[RuuviID]._accelerationY + " m/s2";
            AccelZText.text = Ruuvis[RuuviID]._accelerationZ + " m/s2"; ;
            LastUpdatedText.text = "Last updated on " + Ruuvis[RuuviID]._timeStamp;
        }
    }

    //Public function called from other modules to display the next Ruuvi data on UI
    //Designed to be called on certain controller button or action (example: tab the touchpad)
    //First tap:    currentRuuviDisplayed =2    Menu=2
    //Second tap:   currentRuuviDisplayed =3    Menu=3
    //Third tap:    currentRuuviDisplayed =4    Menu=4
    //Fourth tap:   currentRuuviDisplayed =1    Menu=1
    //Fifth tap:    currentRuuviDisplayed =2    Menu=2
    public void SetTextsToNextRuuvi()
    {
        if(currentRuuviDisplayed > 4)
        {
            currentRuuviDisplayed = 1;
            SetTextsToRuuviID(currentRuuviDisplayed);
        }
        else
        {
            currentRuuviDisplayed = currentRuuviDisplayed + 1;
            SetTextsToRuuviID(currentRuuviDisplayed);
        }
    }

    void HandlePrivilegesDone_(MLResult result)
    {
        if (!result.IsOk)
        {
            Debug.LogError("Failed to get all requested privileges. MLResult: " + result);
            enabled = false;
            return;
        }
    }

    void TriggerConNXTRepeating()
    {
        StartCoroutine(UpdateDataFromConNXT());
    }
    #endregion
}
