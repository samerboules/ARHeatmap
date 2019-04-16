using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine.Networking;

public class UpdateFromConNXT : MonoBehaviour {

    public Text DebuggingInfoText = null;
    public Text TimerText = null;


    //EndPoint to get the token
    private string tokenEndPoint = "https://beta.connxt.eu/connect/token";
    //EndPoint to get the devices
    private string devicesEndPoint = "https://beta.connxt.eu/api/Devices";
    //ConNXT credential
    private string clientID = "52840b9f-23db-475d-a920-272217be4402";
    private string clientSecret = "ZTXMI5Em/676HmLL0WUQUQ==";
    private int timer = 0;

    // Structure to hold the data for each RUUVI tag
    public struct RuuviTag
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
    public RuuviTag[] Ruuvis;

    //This class is used to break down the Json of Token
    private class TokenResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }
    //Token that will be recieved from ConNXT
    private string token;

    JArray devices;
    private string telemetryEndPoint;

    /*******************************************************************
    * NAME :            IEnumerator GetTelemetry()
    *
    * DESCRIPTION :      Gets the telemetry data from ConNXT
    *                    * Connection to ConNXT is done in three steps
    *                    * 1. POST request to get a token using the conNXT credentials
    *                    * 2. GET request using the token and ConNXT credentials to get the connected devices
    *                    * 3. GET request on EACH device to get the telemetry data
    *
    * INPUTS :
    *       PARAMETERS:
    *           None
    *       GLOBALS:
    *           tokenEndPoint           string          End point to get the token data
    *           devicesEndPoint         string          End point to get the devices
    *           telemetryEndPoint       string          End point to get the telemetry data
    *           clientID                string          ConNXT credintials
    *           clientSecret            string          ConNXT credintials
    *           
    *           devices                 JArray          Json Array placeholder that will contain the connected devices on ConNXT
    *           TokenResponse           class           Structure to deserialize the Json string that will contain token data
    *           Ruuvis                  RuuviTag[]      Array of RuuviTag that will contain the telemetry data for each device connected to ConNXT
    *                   
    *                   
    * OUTPUTS :
    *       PARAMETERS:
    *           None
    *       GLOBALS :
    *           None
    *       RETURN :
    *            Type:   void                   ---
    *            Values: ----------             ---
    *                    
    *                    
    * PROCESS :
    *                   [1]  GET request to the desired uri
    *                   [2]  Wait for it
    *                   [3]  If there is a network error, log it
    *                   [4]  If there is no error, start the telemetry request sequence
    *
    * NOTES :           For ConNXT support please contact Samuel van Egmond (He helped alot with this project)
    *                   
    * CHANGES :
    * REF NO    DATE    WHO     DETAIL
    *           15Mar19 SB      Original Code (Test Pass)
    *           18Mar19 SB      Add descriptive comment header
    *                           
    */
    IEnumerator GetTelemetry()
    {

        //DebuggingInfoText.text = "Updating data from CONNXT...;
        //Step 1: Get access token from ConNXT (POST request)
        //Create new www form
        WWWForm tokenForm = new WWWForm();

        //Add Fields for POST Request
        tokenForm.AddField("grant_type", "client_credentials");
        tokenForm.AddField("client_id", clientID);
        tokenForm.AddField("client_secret", clientSecret);
        tokenForm.AddField("scope", "devices:telemetry devices:get");

        using (UnityWebRequest www = UnityWebRequest.Post(tokenEndPoint, tokenForm))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                DebuggingInfoText.text = "Network Error: " + www.error;
            }
            else
            {
                DebuggingInfoText.text = "Updating data, please wait... (CONNXT connection test success)";
                //DebuggingInfoText.text = www.downloadHandler.text;
                //Deserialize the JSON return to extract the token
                TokenResponse result = JsonConvert.DeserializeObject<TokenResponse>(www.downloadHandler.text);
                token = result.access_token;

                DebuggingInfoText.text = "Updating data, please wait... (Token acquired)";
            }
        }


        //Step 2: Get the number of devices on ConNXT (GET request)
        WWWForm devicesForm = new WWWForm();

        //Add Fields for POST Request
        devicesForm.AddField("grant_type", "client_credentials");
        devicesForm.AddField("client_id", clientID);
        devicesForm.AddField("client_secret", clientSecret);
        devicesForm.AddField("scope", "devices:telemetry devices:get");

        Dictionary<string, string> headers_ = devicesForm.headers;
        headers_["Authorization"] = "Bearer " + token;

        //Perform GET Request
        WWW DevicesRequest = new WWW(devicesEndPoint, null, headers_);

        //Wait until DevicesRequest retuns
        yield return DevicesRequest;
        devices = JArray.Parse(DevicesRequest.text);

        //Create array of RuuviTag structs by the number of devices found
        Ruuvis = new RuuviTag[devices.Count];

        DebuggingInfoText.text = "Number of active devices on ConNXT: " + devices.Count.ToString() + "\nUpdating data, please wait..." ;
        timer = 0;

        //Step 3: Loop over all devices and read the telemetry data (GET request)
        int localindex = 0;

        //foreach (JObject device in devices)
        for(localindex = 0; localindex < devices.Count; localindex++)
        {
            string deviceId = devices[localindex]["deviceUid"].Value<string>();
            DebuggingInfoText.text = "Number of active devices on ConNXT: " + devices.Count.ToString() + "\nUpdating Device " + localindex.ToString() + ", please wait...";

            Ruuvis[localindex]._deviceID = deviceId;

            WWWForm telemetryForm = new WWWForm();
            //Add Fields for POST Request
            telemetryForm.AddField("grant_type", "client_credentials");
            telemetryForm.AddField("client_id", clientID);
            telemetryForm.AddField("client_secret", clientSecret);
            telemetryForm.AddField("scope", "devices:telemetry devices:get");

            Dictionary<string, string> TelemetryHeaders = telemetryForm.headers;
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
            //localindex++;
        }   
        DebuggingInfoText.text = "Update Complete.\nLast update was at: " + Ruuvis[Ruuvis.Length-1]._timeStamp;
    }

    /*******************************************************************
    * NAME :            IEnumerator GetRequest(string uri)
    *
    * DESCRIPTION :     This function takes a uri string and tries to connect to it over network
    *                   If the connection success, start getting the telemtry data. If failed, log error.
    *
    * INPUTS :
    *       PARAMETERS:
    *           uri     string               The target webpage uri example: "https://beta.connxt.eu"
    *       GLOBALS :
    *           None
    *                   
    *                   
    * OUTPUTS :
    *       PARAMETERS:
    *           None
    *       GLOBALS :
    *           None
    *       RETURN :
    *            Type:   void                   ---
    *            Values: ----------             ---
    *                    
    *                    
    * PROCESS :
    *                   [1]  GET request to the desired uri
    *                   [2]  Wait for it
    *                   [3]  If there is a network error, log it
    *                   [4]  If there is no error, start the telemetry request sequence
    *
    * NOTES :           This is not an essential function. You can go ahead and request telemetry right away, but 
    *                   this function just makes sure that the network connection is good.
    *                   
    * CHANGES :
    * REF NO    DATE    WHO     DETAIL
    *           15Mar19 SB      Original Code (Test Pass)
    *           18Mar19 SB      Add descriptive comment header
    *                           
    */
    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                DebuggingInfoText.text = "Error: " + webRequest.error;
            }
            else
            {
                DebuggingInfoText.text = "Connected to the Internet. Testing CONNXT connection...";
                StartCoroutine(GetTelemetry());
            }
        }
    }


    /*******************************************************************
    * NAME :            void UpdateData()
    *
    * DESCRIPTION :     This function is called periodically to start the server communication co-routine
    *
    * INPUTS :
    *       PARAMETERS:
    *           None     ------                ------
    *       GLOBALS :
    *           function   IEnumerator GetRequest(string uri)
    *                   
    *                   
    * OUTPUTS :
    *       PARAMETERS:
    *           None
    *       GLOBALS :
    *           None
    *       RETURN :
    *            Type:   void                   ---
    *            Values: ----------             ---
    *                    
    *                    
    * PROCESS :
    *                   [1]  Starts co-routine that tests the connection to ConNXT server
    *                   [2]  
    *                   [3]    
    *                   [4]    
    *                   [5]  
    *
    * NOTES :           
    *                   
    * CHANGES :
    * REF NO    DATE    WHO     DETAIL
    *           15Mar19 SB      Original Code (Test Pass)
    *           18Mar19 SB      Add comment descriptive header
    *                           
    */
    void UpdateData()
    {
        StartCoroutine(GetRequest("https://beta.connxt.eu"));
    }


    void stopwatch()
    {
        timer++;
        TimerText.text = timer.ToString();
    }

    // Use this for initialization
    void Start () {
        // start the UpdateData repeating function every 60seconds
        InvokeRepeating("UpdateData", 0f, 60f);
        InvokeRepeating("stopwatch", 0f, 1f);
    }
}
