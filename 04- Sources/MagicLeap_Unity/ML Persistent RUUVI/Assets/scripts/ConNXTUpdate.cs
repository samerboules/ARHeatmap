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

namespace MagicLeap
{
    public class ConNXTUpdate : MonoBehaviour
    {
        #region Public Variables
        //That's only used in making animation of the menu
        public RectTransform RuuviMenu;
        //Declare UI text boxes that will be updated
        public Text RUUVINameText = null;
        public Text TemperatureTitleText = null;
        public Text TemperatureText = null;
        public Text HumidityTitleText = null;
        public Text HumidityText = null;
        public Text PressureTitleText = null;
        public Text PressureText = null;
        public Text AccelXTitleText = null;
        public Text AccelXText = null;
        public Text AccelYTitleText = null;
        public Text AccelYText = null;
        public Text AccelZTitleText = null;
        public Text AccelZText = null;
        public Text LastUpdatedText = null;
        public Text MySpecificIndex = null;
        //public Text DebuggingInfoText   = null;
        #endregion


        #region Private Variables
        //Which Ruuvi you want to display on UI
        private int currentRuuviDisplayed=1;
        #endregion
        #region My Functions
        //Reads the latest data on ConNXT for all available RUUVI tags
        void UpdateUI()
        {
            //First Run?
            /*
            if (!PlayerPrefs.HasKey($"isFirstTime_{objectSpecificIndex}") || PlayerPrefs.GetInt($"isFirstTime_{objectSpecificIndex}") == 1)
            {
                // Set and save all your PlayerPrefs here.
                // Now set the value of isFirstTime to be false in the PlayerPrefs.
                currentRuuviDisplayed = 1;
                PlayerPrefs.SetInt($"currentRuuviDisplayed_{objectSpecificIndex}", 1);
                PlayerPrefs.SetInt($"isFirstTime_{objectSpecificIndex}", 0);
                PlayerPrefs.Save();
            }
            else //Not first run
            {
                currentRuuviDisplayed = PlayerPrefs.GetInt($"currentRuuviDisplayed_{objectSpecificIndex}");
            }
            */
            SetTextsToRuuviID(currentRuuviDisplayed);
        }

        //Private function that takes the number of Ruuvi you want to display and update the UI texts accordingly
        //RuuviID range: from 1 to 4 (Don't send in 0 because this is the Raspberry Pi gateway which has no telemetry
        private void SetTextsToRuuviID(int RuuviID)
        {
            PersistenceExample _PersistenceExample = GameObject.Find("PersistenceExample").GetComponent<PersistenceExample>();
            RUUVINameText.text = "CoLab RUUVI Tag 00" + RuuviID.ToString() + "\nDeviceID: " + _PersistenceExample.Ruuvis[RuuviID]._deviceID;
            if (_PersistenceExample.Ruuvis[RuuviID]._temperature == null)
            {
                TemperatureTitleText.text = " ";
                TemperatureText.text = " ";
                HumidityTitleText.text = " ";
                HumidityText.text = " ";
                PressureTitleText.text = " ";
                PressureText.text = "";
                AccelXTitleText.text = " ";
                AccelXText.text = "";
                AccelYTitleText.text = " ";
                AccelYText.text = "";
                AccelZTitleText.text = " ";
                AccelZText.text = ""; ;
                LastUpdatedText.text = "Loading...";
            }
            else
            {
                //Update the text fields on the gui
                TemperatureTitleText.text = "Temperature";
                TemperatureText.text = _PersistenceExample.Ruuvis[RuuviID]._temperature + " °C";
                HumidityTitleText.text = "Humidity";
                HumidityText.text = _PersistenceExample.Ruuvis[RuuviID]._humidity + " %";
                PressureTitleText.text = "Pressure";
                PressureText.text = _PersistenceExample.Ruuvis[RuuviID]._pressure + " hPa";
                AccelXTitleText.text = "Acceleration X";
                AccelXText.text = _PersistenceExample.Ruuvis[RuuviID]._accelerationX + " m/s2";
                AccelYTitleText.text = "Acceleration Y";
                AccelYText.text = _PersistenceExample.Ruuvis[RuuviID]._accelerationY + " m/s2";
                AccelZTitleText.text = "Acceleration Z";
                AccelZText.text = _PersistenceExample.Ruuvis[RuuviID]._accelerationZ + " m/s2"; ;
                LastUpdatedText.text = "Last updated on " + _PersistenceExample.Ruuvis[RuuviID]._timeStamp;
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
            /*
            if (currentRuuviDisplayed > 4)
            {
                currentRuuviDisplayed = 1;
                PlayerPrefs.SetInt($"currentRuuviDisplayed_{objectSpecificIndex}", currentRuuviDisplayed);
                SetTextsToRuuviID(currentRuuviDisplayed);
            }
            else
            {
                currentRuuviDisplayed = currentRuuviDisplayed + 1;
                PlayerPrefs.SetInt($"currentRuuviDisplayed_{objectSpecificIndex}", currentRuuviDisplayed);
                SetTextsToRuuviID(currentRuuviDisplayed);
            }
            */
        }
        #endregion

        #region Unity Functions
        void Awake()
        {
            /*
            if (!PlayerPrefs.HasKey("isFirstTime") || PlayerPrefs.GetInt("isFirstTime") == 1)
            {
                // Set and save all your PlayerPrefs here.
                // Now set the value of isFirstTime to be false in the PlayerPrefs.

                //How many created/restored are there in the list?
                List<MLContentBinding> allBindings = MLPersistentStore.AllBindings;

                //So I am number (WhateverInTheList + 1)
                objectSpecificIndex = allBindings.Count + 1;

                //Save this number in the PlayerPrefs
                PlayerPrefs.SetInt($"objectSpecificIndex_{objectSpecificIndex}", objectSpecificIndex);
                MySpecificIndex.text = "My Specific Index : " + PlayerPrefs.GetInt($"objectSpecificIndex_{objectSpecificIndex}").ToString();
                PlayerPrefs.SetInt("isFirstTime", 0);
                PlayerPrefs.Save();
            }
            else
            {
                MySpecificIndex.text = "My Specific Index : " + PlayerPrefs.GetInt($"objectSpecificIndex_{objectSpecificIndex}").ToString();
            }
            */
            InvokeRepeating("UpdateUI", 0f, 10f);
        }
        #endregion

        
    }
}
