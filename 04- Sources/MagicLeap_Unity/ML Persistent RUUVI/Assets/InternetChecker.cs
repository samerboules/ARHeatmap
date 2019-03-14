using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InternetChecker : MonoBehaviour
{
    public Text DebuggingInfoText = null;

    private const bool allowCarrierDataNetwork = false;
    private const string pingAddress = "8.8.8.8"; // Google Public DNS server
    private const float waitingTime = 5.0f;
    public bool internetConnectBool;
    private Ping ping;
    private float pingStartTime;

    public void Start()
    {
        bool internetPossiblyAvailable;
        switch (Application.internetReachability)
        {
            case NetworkReachability.ReachableViaLocalAreaNetwork:
                internetPossiblyAvailable = true;
                break;
            case NetworkReachability.ReachableViaCarrierDataNetwork:
                //internetPossiblyAvailable = allowCarrierDataNetwork;
                internetPossiblyAvailable = true;
                break;
            default:
                internetPossiblyAvailable = false;
                break;
        }
        if (!internetPossiblyAvailable)
        {
            InternetIsNotAvailable();
            return;
        }
        ping = new Ping(pingAddress);
        pingStartTime = Time.time;
    }

    public void Update()
    {
        if (ping != null)
        {
            bool stopCheck = true;
            if (ping.isDone)
                InternetAvailable();
            else if (Time.time - pingStartTime < waitingTime)
                stopCheck = false;
            else
                InternetIsNotAvailable();
            if (stopCheck)
                ping = null;
        }
    }

    public void InternetIsNotAvailable()
    {
        //Debug.Log("No Internet");
        DebuggingInfoText.text = "No Internet";

        internetConnectBool = false;
    }

    public void InternetAvailable()
    {
        //Debug.Log("Internet is available;)");
        DebuggingInfoText.text = "Connected to Internet";
        internetConnectBool = true;
    }
}