using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Measurmentsupdatescrpt : MonoBehaviour {

    public Text TemperatureText = null;
    private int Temperature = 00;

    // Generate a random number between two numbers
    private int RandomNumber(int min, int max)
    {
        System.Random random = new System.Random();
        return random.Next(min, max);
    }

    private void updateMeasurements()
    {
        Temperature = RandomNumber(5, 40);
        TemperatureText.text = Temperature.ToString() + "°C";
    }

    // Use this for initialization
    void Start () {
        TemperatureText.text = "35°C";
        InvokeRepeating("updateMeasurements", 0f, 10f);
    }
	
	// Update is called once per frame
	void Update () {
        
    }


}
