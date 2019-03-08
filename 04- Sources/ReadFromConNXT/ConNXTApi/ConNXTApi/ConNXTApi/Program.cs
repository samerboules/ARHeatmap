using Newtonsoft.Json.Linq;
using System;

namespace ConNXTApi
{

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


    class Program
    {
        static void Main(string[] args)
        {
            
            using (var api = new ConNXTApiConnector("https://beta.connxt.eu", "52840b9f-23db-475d-a920-272217be4402", "ZTXMI5Em/676HmLL0WUQUQ=="))
            {
                api.ConnectAsync().Wait();

                // Retrieve the devices
                string devicesString = api.GetDevicesAsync().Result;
                if (!string.IsNullOrEmpty(devicesString))
                {
                    JArray devices = JArray.Parse(devicesString);
                    Console.WriteLine($"{devices.Count} devices found.");
                    Console.WriteLine();

                    //Create array of RuuviTags by the number of devices found
                    RuuviTag [] Ruuvis = new RuuviTag[devices.Count];

                    //local index to fill the structure of devices. Note that device number 0 is the gateway (RaspberryPi)
                    int localindex = 0;

                    // Loop over all devices
                    foreach (JObject device in devices)
                    {                    
                        string deviceId = device["deviceUid"].Value<string>();
                        Console.WriteLine($"Telemetry for device: {deviceId}");

                        
                        Ruuvis[localindex]._deviceID = deviceId;
                        Console.WriteLine($"_deviceID: {Ruuvis[localindex]._deviceID}");

                        // Retrieve the telemetry for the device
                        string telemetryString = api.GetTelemetryAsync(deviceId).Result;
                        if (!string.IsNullOrEmpty(telemetryString))
                        {
                            // Loop over the telemetry values
                            JObject telemetry = JObject.Parse(telemetryString);
                            Console.WriteLine($"{"TimeStamp",-15} = {telemetry["messageTimeStamp"]}");

                            //Update timestamp in structure
                            Ruuvis[localindex]._timeStamp = telemetry["messageTimeStamp"].ToString();

                            foreach (JObject dataPoint in telemetry["dataPoints"] as JArray)
                            {
                                Console.WriteLine($"{dataPoint["key"],-15} = {dataPoint["value"]}");

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
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
