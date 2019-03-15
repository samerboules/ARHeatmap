// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2018 Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Creator Agreement, located
// here: https://id.magicleap.com/creator-terms
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Demonstrates how to persist objects dynamically by interfacing with
    /// the MLPersistence API. This facilitates restoration of existing
    /// and creation of new persistent points.
    /// </summary>
    [RequireComponent(typeof(PrivilegeRequester))]
    public class PersistenceExample : MonoBehaviour
    {
        #region Private Variables
        public Text DebuggingInfoText = null;

        [SerializeField, Tooltip("Content to create")]
        GameObject _content;
        List<MLPersistentBehavior> _pointBehaviors = new List<MLPersistentBehavior>();

        [SerializeField, Tooltip("Status Text")]
        Text _statusText;

        [SerializeField, Tooltip("Destroyed content effect")]
        GameObject _destroyedContentEffect;

        [SerializeField, Tooltip("Text to count restored objects")]
        Text _countRestoredText;
        string _countRestoredTextFormat;
        int _countRestoredGood = 0;
        int _countRestoredBad = 0;

        [SerializeField, Tooltip("Text to count created objects")]
        Text _countCreatedText;
        string _countCreatedTextFormat;
        int _countCreatedGood = 0;
        int _countCreatedBad = 0;

        [SerializeField, Tooltip("Visualizers to enable when the privilege is granted")]
        GameObject[] _visualizers;

        [SerializeField, Tooltip("Controller")]
        ControllerConnectionHandler _controller;

        [SerializeField, Tooltip("Distance in front of Controller to create content")]
        float _distance = 0.2f;

        PrivilegeRequester _privilegeRequester;

        [SerializeField, Tooltip("PCF Visualizer when debugging")]
        PCFVisualizer _pcfVisualizer;
        #endregion // Private Variables

        #region Unity Methods
        /// <summary>
        /// Validate properties. Attach event listener to when privileges are granted
        /// on Awake because the privilege requester requests on Start.
        /// </summary>
        void Awake()
        {
            if (_content == null || _content.GetComponent<MLPersistentBehavior>() == null)
            {
                Debug.LogError("Error: PersistenceExample._content is not set or is missing MLPersistentBehavior behavior, disabling script.");
                enabled = false;
                return;
            }

            if (_destroyedContentEffect == null)
            {
                Debug.LogError("Error: PersistenceExample._destroyedContentEffect is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_controller == null)
            {
                Debug.LogError("Error: PersistenceExample._controller is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: PersistenceExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_countCreatedText == null)
            {
                Debug.LogError("Error: PersistenceExample._countCreatedText is not set, disabling script.");
                enabled = false;
                return;
            }
            _countCreatedTextFormat = _countCreatedText.text;
            _countCreatedText.text = string.Format(_countCreatedTextFormat, _countCreatedGood, _countCreatedBad);

            if (_countRestoredText == null)
            {
                Debug.LogError("Error: PersistenceExample._countRestoredText is not set, disabling script.");
                enabled = false;
                return;
            }
            _countRestoredTextFormat = _countRestoredText.text;
            _countRestoredText.text = string.Format(_countRestoredTextFormat, _countRestoredGood, _countRestoredBad);

            // _privilegeRequester is expected to request for PwFoundObjRead privilege
            _privilegeRequester = GetComponent<PrivilegeRequester>();
            _privilegeRequester.OnPrivilegesDone += HandlePrivilegesDone;
            _statusText.text = "Status: Requesting Privileges";
        }

        /// <summary>
        /// Clean up
        /// </summary>
        void OnDestroy()
        {
            foreach (MLPersistentBehavior pointBehavior in _pointBehaviors)
            {
                if (pointBehavior != null)
                {
                    RemoveContentListeners(pointBehavior);
                    Destroy(pointBehavior.gameObject);
                }
            }

            if (MLPersistentCoordinateFrames.IsStarted)
            {
                MLPersistentCoordinateFrames.Stop();
            }

            if (MLPersistentStore.IsStarted)
            {
                MLPersistentStore.Stop();
            }

            if (_privilegeRequester != null)
            {
                _privilegeRequester.OnPrivilegesDone -= HandlePrivilegesDone;
            }

            MLInput.OnControllerButtonDown -= HandleControllerButtonDown;
        }
        #endregion // Unity Methods

        #region Event Handlers
        /// <summary>
        ///
        /// </summary>
        /// <param name="controllerId"></param>
        /// <param name="button"></param>
        void HandleControllerButtonDown(byte controllerId, MLInputControllerButton button)
        {
            if (!_controller.IsControllerValid(controllerId))
            {
                return;
            }

            if (button == MLInputControllerButton.Bumper)
            {
                Vector3 position = _controller.transform.position + _controller.transform.forward * _distance;
                CreateContent(position, _controller.transform.rotation);
            }
            else if (button == MLInputControllerButton.HomeTap)
            {
                _pcfVisualizer.ToggleDebug();
            }
        }





        private string tokenEndPoint = "https://beta.connxt.eu/connect/token";
        //EndPoint to get the devices
        private string devicesEndPoint = "https://beta.connxt.eu/api/Devices";
        //ConNXT credential
        private string clientID = "52840b9f-23db-475d-a920-272217be4402";
        private string clientSecret = "ZTXMI5Em/676HmLL0WUQUQ==";

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
                    DebuggingInfoText.text = "CONNXT connection test success, you can create RUUVIs now!";
                    //DebuggingInfoText.text = www.downloadHandler.text;
                    //Deserialize the JSON return to extract the token
                    TokenResponse result = JsonConvert.DeserializeObject<TokenResponse>(www.downloadHandler.text);
                    token = result.access_token;


                    DebuggingInfoText.text = result.access_token;
                }
            }

            WWWForm devicesForm = new WWWForm();

            //Add Fields for POST Request
            devicesForm.AddField("grant_type", "client_credentials");
            devicesForm.AddField("client_id", clientID);
            devicesForm.AddField("client_secret", clientSecret);
            devicesForm.AddField("scope", "devices:telemetry devices:get");

            //Step 2: Get the number of devices on ConNXT (GET request)
            Dictionary<string, string> headers_ = devicesForm.headers;
            headers_["Authorization"] = "Bearer " + token;

            //Perform GET Request
            WWW DevicesRequest = new WWW(devicesEndPoint, null, headers_);

            //Wait until DevicesRequest retuns
            yield return DevicesRequest;
            devices = JArray.Parse(DevicesRequest.text);

            //Create array of RuuviTag structs by the number of devices found
            Ruuvis = new RuuviTag[devices.Count];

            DebuggingInfoText.text = "Devices: " + devices.Count.ToString();

            //Step 3: Loop over all devices and read the telemetry data (GET request)
            int localindex = 0;

            foreach (JObject device in devices)
            {
                string deviceId = device["deviceUid"].Value<string>();

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
                localindex++;
            }

            DebuggingInfoText.text = "Last Data Update was at " + Ruuvis[4]._timeStamp;
        }


        IEnumerator GetRequest(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

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

        void UpdateData()
        {
            StartCoroutine(GetRequest("https://beta.connxt.eu"));
        }




  


        /// <summary>
        /// Responds to privilege requester result.
        /// </summary>
        /// <param name="result"/>
        void HandlePrivilegesDone(MLResult result)
        {
            _privilegeRequester.OnPrivilegesDone -= HandlePrivilegesDone;
            if (!result.IsOk)
            {
                if (result.Code == MLResultCode.PrivilegeDenied)
                {
                    Instantiate(Resources.Load("PrivilegeDeniedError"));
                }

                Debug.LogErrorFormat("Error: PersistenceExample failed to get requested privileges, disabling script. Reason: {0}", result);
                _statusText.text = "<color=red>Failed to acquire necessary privileges</color>";
                enabled = false;
                return;
            }
            _statusText.text = "Status: Starting up Systems";


            // start the UpdateData repeating function every 60seconds
            InvokeRepeating("UpdateData", 0f, 60f);


            result = MLPersistentStore.Start();
            if (!result.IsOk)
            {
                if (result.Code == MLResultCode.PrivilegeDenied)
                {
                    Instantiate(Resources.Load("PrivilegeDeniedError"));
                }

                Debug.LogErrorFormat("Error: PersistenceExample failed starting MLPersistentStore, disabling script. Reason: {0}", result);
                enabled = false;
                return;
            }

            result = MLPersistentCoordinateFrames.Start();
            if (!result.IsOk)
            {
                if (result.Code == MLResultCode.PrivilegeDenied)
                {
                    Instantiate(Resources.Load("PrivilegeDeniedError"));
                }

                MLPersistentStore.Stop();
                Debug.LogErrorFormat("Error: PersistenceExample failed starting MLPersistentCoordinateFrames, disabling script. Reason: {0}", result);
                enabled = false;
                return;
            }

            if (MLPersistentCoordinateFrames.IsReady)
            {
                PerformStartup();
            }
            else
            {
                MLPersistentCoordinateFrames.OnInitialized += HandleInitialized;
            }
        }

        /// <summary>
        /// Proceeds with further start up operations if the system successfully initialized.
        /// </summary>
        void HandleInitialized(MLResult status)
        {
            MLPersistentCoordinateFrames.OnInitialized -= HandleInitialized;

            if (status.IsOk)
            {
                PerformStartup();
            }
            else
            {
                _statusText.text = string.Format("<color=red>{0}</color>", status);
                Debug.LogErrorFormat("Error: MLPersistentCoordinateFrames failed to initialize, disabling script. Reason: {0}", status);
                enabled = false;
            }
        }

        /// <summary>
        /// Handler when Content status changes
        /// </summary>
        /// <param name="status">MLPersistentBehavior.PersistentBehaviorStatus</param>
        /// <param name="result">MLResult</param>
        void HandleContentStatusUpdate(MLPersistentBehavior.Status status, MLResult result)
        {
            switch (status)
            {
                case MLPersistentBehavior.Status.BINDING_CREATED:
                    _countCreatedGood++;
                    UpdateCreatedCountText();
                    break;
                case MLPersistentBehavior.Status.BINDING_CREATE_FAILED:
                    _countCreatedBad++;
                    UpdateCreatedCountText();
                    ShowError(result);
                    break;
                case MLPersistentBehavior.Status.RESTORE_SUCCESSFUL:
                    _countRestoredGood++;
                    UpdateRestoredCountText();
                    break;
                case MLPersistentBehavior.Status.RESTORE_FAILED:
                    _countRestoredBad++;
                    UpdateRestoredCountText();

                    // MLResultCode.SnapshotPoseNotFound means the content is bound to a PCF
                    // that does not belong to the current map which is normal behavior
                    if (result.Code != MLResultCode.SnapshotPoseNotFound)
                    {
                        ShowError(result);
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion // Event Handlers

        #region Private Methods
        /// <summary>
        /// Activate PCF Visualizer, restore content, and update status text
        /// </summary>
        void PerformStartup()
        {
            MLInput.OnControllerButtonDown += HandleControllerButtonDown;
            _pcfVisualizer.gameObject.SetActive(true);
            _statusText.text = "Status: Restoring Content";
            ReadAllStoredObjects();
            _statusText.text = "";
        }

        /// <summary>
        /// Update counter for restored content
        /// </summary>
        void UpdateRestoredCountText()
        {
            _countRestoredText.text = string.Format(_countRestoredTextFormat, _countRestoredGood, _countRestoredBad);
        }

        /// <summary>
        /// Update counter for created content
        /// </summary>
        void UpdateCreatedCountText()
        {
            _countCreatedText.text = string.Format(_countCreatedTextFormat, _countCreatedGood, _countCreatedBad);
        }

        /// <summary>
        /// Visualize and log error
        /// </summary>
        /// <param name="result">The specific error</param>
        void ShowError(MLResult result)
        {
            Debug.LogErrorFormat("Error: {0}", result);
            _statusText.text = string.Format("<color=red>Error: {0}</color>", result);
        }

        /// <summary>
        /// Reads all stored game object ids.
        /// </summary>
        void ReadAllStoredObjects()
        {
            List<MLContentBinding> allBindings = MLPersistentStore.AllBindings;
            foreach (MLContentBinding binding in allBindings)
            {
                GameObject gameObj = Instantiate(_content, Vector3.zero, Quaternion.identity);
                gameObj.name = binding.ObjectId;
                MLPersistentBehavior persistentBehavior = gameObj.GetComponent<MLPersistentBehavior>();
                _pointBehaviors.Add(persistentBehavior);
                AddContentListeners(persistentBehavior);
            }
        }

        /// <summary>
        /// Instantiates a new object with MLPersistentBehavior. The MLPersistentBehavior is
        /// responsible for restoring and saving itself.
        /// </summary>
        void CreateContent(Vector3 position, Quaternion rotation)
        {
            GameObject gameObj = Instantiate(_content, position, rotation);
            MLPersistentBehavior persistentBehavior = gameObj.GetComponent<MLPersistentBehavior>();
            persistentBehavior.UniqueId = Guid.NewGuid().ToString();
            _pointBehaviors.Add(persistentBehavior);
            AddContentListeners(persistentBehavior);
        }

        /// <summary>
        /// Removes the points and destroys its binding to prevent future restoration
        /// </summary>
        /// <param name="gameObj">Game Object to be removed</param>
        void RemoveContent(GameObject gameObj)
        {
            MLPersistentBehavior persistentBehavior = gameObj.GetComponent<MLPersistentBehavior>();
            RemoveContentListeners(persistentBehavior);
            _pointBehaviors.Remove(persistentBehavior);
            persistentBehavior.DestroyBinding();
            Instantiate(_destroyedContentEffect, persistentBehavior.transform.position, Quaternion.identity);

            Destroy(persistentBehavior.gameObject);
        }

        /// <summary>
        /// Add listeners to content events
        /// </summary>
        /// <param name="persistentBehavior">Dynamic Content</param>
        void AddContentListeners(MLPersistentBehavior persistentBehavior)
        {
            persistentBehavior.OnStatusUpdate += HandleContentStatusUpdate;

            PersistentBall contentBehavior = persistentBehavior.GetComponent<PersistentBall>();
            contentBehavior.OnContentDestroy += RemoveContent;
        }

        /// <summary>
        /// Remove listeners from content events
        /// </summary>
        /// <param name="persistentBehavior">Dynamic Content</param>
        void RemoveContentListeners(MLPersistentBehavior persistentBehavior)
        {
            // it is safe to unsubscribe to events even if we're not originally subscribing to them
            persistentBehavior.OnStatusUpdate -= HandleContentStatusUpdate;

            PersistentBall contentBehavior = persistentBehavior.GetComponent<PersistentBall>();
            contentBehavior.OnContentDestroy -= RemoveContent;
        }
        #endregion // Private Methods
    }
}
