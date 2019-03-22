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

/* This script is also edited by Samer Boules (samer.boules@ict.nl)
 * The edited part's function change the deletion of device from touchpad tap into touchpad radial scroll, also use touchpad tap to change menu to next RUUVI data
 * 
 * 18 March 2019
 */
using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// PersistentBall is responsible for relaying controller event
    /// to destroy this content
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(MLPersistentBehavior))]
    public class PersistentBall : MonoBehaviour
    {
        #region Private Variables
        ControllerConnectionHandler _controllerConnectionHandler;
        #endregion

        #region Public Events
        /// <summary>
        /// Triggered when this content is to be destroyed
        /// </summary>
        public event Action<GameObject> OnContentDestroy;
        #endregion

        #region Unity Methods
        /// <summary>
        /// Clean Up
        /// </summary>
        private void OnDestroy()
        {
            if (_controllerConnectionHandler != null)
            {
                MLInput.OnControllerTouchpadGestureStart -= HandleControllerTouchpadGestureStart;
                _controllerConnectionHandler = null;
            }
        }

        /// <summary>
        /// Register for controller input only when a controller enters the trigger area
        /// </summary>
        /// <param name="other">Collider of the Controller</param>
        private void OnTriggerEnter(Collider other)
        {
            ControllerConnectionHandler controllerConnectionHandler = other.GetComponent<ControllerConnectionHandler>();
            if (controllerConnectionHandler == null)
            {
                return;
            }

            _controllerConnectionHandler = controllerConnectionHandler;
            MLInput.OnControllerTouchpadGestureStart += HandleControllerTouchpadGestureStart;
        }

        /// <summary>
        /// Unregister controller input when controller leaves the trigger area
        /// </summary>
        /// <param name="other">Collider of the Controller</param>
        private void OnTriggerExit(Collider other)
        {
            ControllerConnectionHandler controllerConnectionHandler = other.GetComponent<ControllerConnectionHandler>();
            if (_controllerConnectionHandler == controllerConnectionHandler)
            {
                _controllerConnectionHandler = null;
                MLInput.OnControllerTouchpadGestureStart -= HandleControllerTouchpadGestureStart;
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handler for Gesture Start events
        /// </summary>
        /// <param name="controllerId">Controller Id</param>
        /// <param name="gesture">Touchpad Gesture</param>
        /// 
        //This function is edited by Samer Boules
        //I changed the deletion of device from touchpad tap into touchpad Radial Scroll.
        //Added functionality is to detect the touchpad tap when the device is highlighted, when detected, switch the displayed menu to the next Ruuvi data
        private void HandleControllerTouchpadGestureStart(byte controllerId, MLInputControllerTouchpadGesture gesture)
        {
            if (_controllerConnectionHandler != null
                && _controllerConnectionHandler.IsControllerValid(controllerId)
                && gesture.Type == MLInputControllerTouchpadGestureType.RadialScroll)
            {
                if (OnContentDestroy != null)
                {
                    OnContentDestroy(gameObject);
                }
            }
            else if (_controllerConnectionHandler != null
                && _controllerConnectionHandler.IsControllerValid(controllerId)
                && gesture.Type == MLInputControllerTouchpadGestureType.Swipe)
            {

                //If highlighted and touchpad tap, switch to next RUUVI data
                UpdateUI _UpdateUI = gameObject.GetComponent<UpdateUI>();
                _UpdateUI.SetTextsToNextRuuvi();
            }
        }
        #endregion
    }
}
