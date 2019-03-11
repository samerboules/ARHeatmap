using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public class TestAPInew : MonoBehaviour {
    public Text responseText;

    private string tokenEndPoint = "https://beta.connxt.eu/connect/token";
    private string clientID = "52840b9f-23db-475d-a920-272217be4402";
    private string clientSecret = "ZTXMI5Em/676HmLL0WUQUQ==";
    private string token;

    private class TokenResponse
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
    }
    // Defining structure 
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


    IEnumerator RequestToken()
    {
        //Create new www form
        WWWForm form = new WWWForm();

        //Dictionary<string, string> headers = form.headers;
        //headers["grant_type"] = "client_credentials";
        //headers["client_id"] = clientID;
        //headers["client_secret"] = clientSecret;
        //headers["scope"] = "devices:telemetry devices:get";

        //Add Fields for POST Request
        form.AddField("grant_type", "client_credentials");
        form.AddField("client_id", clientID);
        form.AddField("client_secret", clientSecret);
        form.AddField("scope", "devices:telemetry devices:get");

        //Transform POST fields into byte array in order to fit WWW constructor
        byte[] rawFormData = form.data;

        //Perform POST Request
        WWW request = new WWW(tokenEndPoint, rawFormData);

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

        token = result.AccessToken;
    }

    IEnumerator RequestDevices(string local_Token)
    {
        //Create new www form
        WWWForm form = new WWWForm();

        Dictionary<string, string> headers_ = form.headers;
        headers_["Authorization"] = local_Token;
        //headers["client_id"] = clientID;
        //headers["client_secret"] = clientSecret;
        //headers["scope"] = "devices:telemetry devices:get";

        //Add Fields for POST Request
        //form.AddField("grant_type", "client_credentials");
        //form.AddField("client_id", clientID);
        //form.AddField("client_secret", clientSecret);
        //form.AddField("scope", "devices:telemetry devices:get");

        //Transform POST fields into byte array in order to fit WWW constructor
        //byte[] rawFormData = form.data;

        //Perform POST Request
        WWW request = new WWW(tokenEndPoint, null, headers_);

        //Wait until POST_Request retuns
        yield return request;
    }



    // Use this for initialization
    IEnumerator Start()
    {

        //Create new www form
        WWWForm form = new WWWForm();

        //Dictionary<string, string> headers = form.headers;
        //headers["grant_type"] = "client_credentials";
        //headers["client_id"] = clientID;
        //headers["client_secret"] = clientSecret;
        //headers["scope"] = "devices:telemetry devices:get";

        //Add Fields for POST Request
        form.AddField("grant_type", "client_credentials");
        form.AddField("client_id", clientID);
        form.AddField("client_secret", clientSecret);
        form.AddField("scope", "devices:telemetry devices:get");

        //Transform POST fields into byte array in order to fit WWW constructor
        byte[] rawFormData = form.data;

        //Perform POST Request
        WWW request = new WWW(tokenEndPoint, rawFormData);

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

        token = result.AccessToken;


        //Create new www form
        WWWForm form2 = new WWWForm();

        Dictionary<string, string> headers_ = form.headers;
        headers_["Authorization"] = "Bearer " + token;
        //headers["client_id"] = clientID;
        //headers["client_secret"] = clientSecret;
        //headers["scope"] = "devices:telemetry devices:get";

        //Add Fields for POST Request
        //form.AddField("grant_type", "client_credentials");
        //form.AddField("client_id", clientID);
        //form.AddField("client_secret", clientSecret);
        //form.AddField("scope", "devices:telemetry devices:get");

        //Transform POST fields into byte array in order to fit WWW constructor
        //byte[] rawFormData = form.data;

        //Perform POST Request
        tokenEndPoint = "https://beta.connxt.eu/api/Devices";
        WWW request2 = new WWW(tokenEndPoint, null, headers_);

        //Wait until POST_Request retuns
        yield return request2;
        JArray devices = JArray.Parse(request2.text);

        //Create array of RuuviTags by the number of devices found
        RuuviTag[] Ruuvis = new RuuviTag[devices.Count];

        int localindex = 0;

        foreach (JObject device in devices)
        {
            string deviceId = device["deviceUid"].Value<string>();

            Ruuvis[localindex]._deviceID = deviceId;
            //Create new www form
            WWWForm form3 = new WWWForm();

            Dictionary<string, string> headers_2 = form.headers;
            headers_2["Authorization"] = "Bearer " + token;
            tokenEndPoint = $"https://beta.connxt.eu/api/Telemetry/{deviceId}/latest";
            WWW request3 = new WWW(tokenEndPoint, null, headers_2);

            //Wait until POST_Request retuns
            yield return request3;

            // Retrieve the telemetry for the device
            string telemetryString = request3.text;
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

        }
    }
	
	// Update is called once per frame
	void Update () {
        //RequestToken();
        //RequestDevices(token);
    }
}

/**
 * using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestAPI : MonoBehaviour {
    public Text responseText;

    private string tokenEndPoint = "https://beta.connxt.eu/connect/token";
    private string clientID = "52840b9f-23db-475d-a920-272217be4402";
    private string clientSecret = "ZTXMI5Em/676HmLL0WUQUQ==";
    private string token;

    private class TokenResponse
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
    }



    IEnumerator RequestToken()
    {
        //Create new www form
        WWWForm form = new WWWForm();

        //Dictionary<string, string> headers = form.headers;
        //headers["grant_type"] = "client_credentials";
        //headers["client_id"] = clientID;
        //headers["client_secret"] = clientSecret;
        //headers["scope"] = "devices:telemetry devices:get";

        //Add Fields for POST Request
        form.AddField("grant_type", "client_credentials");
        form.AddField("client_id", clientID);
        form.AddField("client_secret", clientSecret);
        form.AddField("scope", "devices:telemetry devices:get");

        //Transform POST fields into byte array in order to fit WWW constructor
        byte[] rawFormData = form.data;

        //Perform POST Request
        WWW request = new WWW(tokenEndPoint, rawFormData);

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

        token = result.AccessToken;
    }

    IEnumerator RequestDevices(string local_Token)
    {
        //Create new www form
        WWWForm form = new WWWForm();

        Dictionary<string, string> headers_ = form.headers;
        headers_["Authorization"] = local_Token;
        //headers["client_id"] = clientID;
        //headers["client_secret"] = clientSecret;
        //headers["scope"] = "devices:telemetry devices:get";

        //Add Fields for POST Request
        //form.AddField("grant_type", "client_credentials");
        //form.AddField("client_id", clientID);
        //form.AddField("client_secret", clientSecret);
        //form.AddField("scope", "devices:telemetry devices:get");

        //Transform POST fields into byte array in order to fit WWW constructor
        //byte[] rawFormData = form.data;

        //Perform POST Request
        WWW request = new WWW(tokenEndPoint, null, headers_);

        //Wait until POST_Request retuns
        yield return request;
    }


    // Use this for initialization
    void update()
    {
        RequestToken();
        RequestDevices(token);

    }
    /*
    void RequestDevices(string local_token)
    {
        //Create new www form
        WWWForm form = new WWWForm();

        //EndPoint to get devices
        tokenEndPoint = "https://beta.connxt.eu/api/Devices";

        Dictionary<string, string> headers = form.headers;
        headers["Authorization"] = local_token;
        
        //Perform GET Request
        WWW request = new WWW(tokenEndPoint, null , headers);

        //Start co-routine that is triggered when the POST request gets back
        StartCoroutine(OnRequestDevicesResponse(request));
    }

    private IEnumerator OnRequestDevicesResponse(WWW GET_Request)
    {
        //Wait until GET_Request retuns
        yield return GET_Request;

        //Devices = result.AccessToken;
    }
    */