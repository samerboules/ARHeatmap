using Newtonsoft.Json.Linq;
using System;

namespace ConNXTApi
{
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

                    // Loop over all devices
                    foreach (JObject device in devices)
                    {
                        string deviceId = device["deviceUid"].Value<string>();
                        Console.WriteLine($"Telemetry for device: {deviceId}");

                        // Retrieve the telemetry for the device
                        string telemetryString = api.GetTelemetryAsync(deviceId).Result;
                        if (!string.IsNullOrEmpty(telemetryString))
                        {
                            // Loop over the telemetry values
                            JObject telemetry = JObject.Parse(telemetryString);
                            Console.WriteLine($"{"TimeStamp",-15} = {telemetry["messageTimeStamp"]}");
                            foreach (JObject dataPoint in telemetry["dataPoints"] as JArray)
                            {
                                Console.WriteLine($"{dataPoint["key"],-15} = {dataPoint["value"]}");
                            }
                        }

                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
