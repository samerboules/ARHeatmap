﻿/* This script is developed by Samer Boules (samer.boules@ict.nl)
 * It's function is update the menu with RUUVI data. Connection to ConNXT and the actual update of data is done in PersistenceExample.cs
 * Managing the switch to the next menu is done by PersistentBall.cs
 * 18 March 2019
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
using UnityEditor;


namespace MagicLeap
{
    public class UpdateUI : MonoBehaviour
    {
        #region Public Variables
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
        #endregion

        public float currentTemperature;


        #region Private Variables
        //An ID that is specific to each device you create in AR space
        private static int deviceSpecificID;

        //Which Ruuvi you want to display on 
        private int currentRuuviDisplayed = 1;

        int count;
        #endregion

        #region My Functions
        //Reads the latest data on ConNXT for all available RUUVI tags
        void UpdateMenu()
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
            //List<MLContentBinding> allBindings = MLPersistentStore.AllBindings;
            //deviceSpecificID = allBindings.Count;
        }

        //Private function that takes the number of Ruuvi you want to display and update the UI texts accordingly
        //RuuviID range: from 1 to 4 (Don't send in 0 because this is the Raspberry Pi gateway which has no telemetry
        private void SetTextsToRuuviID(int RuuviID)
        {
            UpdateFromConNXT _UpdateFromConNXT = GameObject.Find("PersistenceExample").GetComponent<UpdateFromConNXT>();
#if false
            if (_UpdateFromConNXT.Ruuvis[RuuviID]._temperature == null)
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
                LastUpdatedText.text = "Updating...";
            }
            else
            {
#endif
                currentTemperature = float.Parse(_UpdateFromConNXT.Ruuvis[RuuviID]._temperature, System.Globalization.CultureInfo.InvariantCulture);

                //Update the text fields on the gui
                RUUVINameText.text = "CoLab RUUVI Tag 00" + RuuviID.ToString() + "\nDeviceID: " + _UpdateFromConNXT.Ruuvis[RuuviID]._deviceID;
                TemperatureTitleText.text = "Temperature";
                TemperatureText.text = _UpdateFromConNXT.Ruuvis[RuuviID]._temperature + " °C";
                HumidityTitleText.text = "Humidity";
                HumidityText.text = _UpdateFromConNXT.Ruuvis[RuuviID]._humidity + " %";
                PressureTitleText.text = "Pressure";
                PressureText.text = _UpdateFromConNXT.Ruuvis[RuuviID]._pressure + " hPa";
                AccelXTitleText.text = "Acceleration X";
                AccelXText.text = _UpdateFromConNXT.Ruuvis[RuuviID]._accelerationX + " m/s2";
                AccelYTitleText.text = "Acceleration Y";
                AccelYText.text = _UpdateFromConNXT.Ruuvis[RuuviID]._accelerationY + " m/s2";
                AccelZTitleText.text = "Acceleration Z";
                AccelZText.text = _UpdateFromConNXT.Ruuvis[RuuviID]._accelerationZ + " m/s2"; ;
                LastUpdatedText.text = "Last updated on " + _UpdateFromConNXT.Ruuvis[RuuviID]._timeStamp;
            //}
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
            if (currentRuuviDisplayed > 4)
            {
                currentRuuviDisplayed = 1;
                PlayerPrefs.SetInt("savedCurrentRuuviDisplayed", currentRuuviDisplayed);
                SetTextsToRuuviID(currentRuuviDisplayed);
            }
            else
            {
                currentRuuviDisplayed = currentRuuviDisplayed + 1;
                PlayerPrefs.SetInt("savedCurrentRuuviDisplayed", currentRuuviDisplayed);
                SetTextsToRuuviID(currentRuuviDisplayed);
            }
        }
#endregion

#region Unity Functions
        void Start()
        {
            InvokeRepeating("UpdateMenu", 0f, 10f);
            /*
            if (PlayerPrefs.HasKey("savedDeviceSpecificID"))
            {
                //Get the device Specific ID and the which RUUVI is displayed on this device
                deviceSpecificID = PlayerPrefs.GetInt("savedDeviceSpecificID");
                currentRuuviDisplayed = PlayerPrefs.GetInt("savedCurrentRuuviDisplayed");
            }
            else
            {
                //If this device is just created, then get how many device were created before and use this number as device Specific ID (
                //ex: if there has been 2 devices created before this one, then the device ID for this device is 2)

                //How many created/restored are there in the list?

                //PlayerPrefs.SetInt("savedDeviceSpecificID", deviceSpecificID);
                deviceSpecificID++;

                currentRuuviDisplayed = 1;
                PlayerPrefs.SetInt("savedCurrentRuuviDisplayed", currentRuuviDisplayed);
            }
            */


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
        }

        static public GameObject getChildGameObject(GameObject fromGameObject, string withName)
        {
            //Author: Isaac Dart, June-13.
            Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>();
            foreach (Transform t in ts) if (t.gameObject.name == withName) return t.gameObject;
            return null;
        }

        void Update()
        {
            GameObject _MeshingNodes = GameObject.Find("MeshingNodes");
            Control _Control = _MeshingNodes.GetComponent<Control>();

            //GameObject _ps = GameObject.Find("PSSunSurface");
            //ParticleSystem _ParticleSystem = _ps.GetComponent<ParticleSystem>();

            if (_Control.areParticlesActive == true)
            {
                GameObject _go = getChildGameObject(this.gameObject, "PSSunSurface");
                ParticleSystem particle = _go.GetComponent<ParticleSystem>();
                particle.Play();
                //particle.enableEmission = true;
            }
            else if (_Control.areParticlesActive == false)
            {
                GameObject _go = getChildGameObject(this.gameObject, "PSSunSurface");
                ParticleSystem particle = _go.GetComponent<ParticleSystem>();
                particle.Stop();
            }

            // PersistenceExample _PersistenceExample = gameObject.GetComponent<PersistenceExample>();
            // count = _PersistenceExample.countOfPointBehaviours;
            // MySpecificIndex.text = "count: " + count.ToString();
        }
#endregion


    }
}
