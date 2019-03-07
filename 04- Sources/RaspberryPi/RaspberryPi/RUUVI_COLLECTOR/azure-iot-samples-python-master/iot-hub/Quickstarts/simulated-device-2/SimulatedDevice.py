# Copyright (c) ICT Group B.V
# ICT CoLab
# Developed by: Samer Boules (samer.boules@ict.nl)

import random
import time
import sys
import iothub_client
from iothub_client import IoTHubClient, IoTHubClientError, IoTHubTransportProvider, IoTHubClientResult
from iothub_client import IoTHubMessage, IoTHubMessageDispositionResult, IoTHubError, DeviceMethodReturnValue

# The device connection string to authenticate the device with your ConNXT hub.
CONNECTION_STRING = "HostName=connxtv2acceptance-iothub.azure-devices.net;DeviceId=CLGW001;SharedAccessKey=P6GUps40aRBIt0isUE55Ip2qB24vmW1AeMDdZ/zyj6E="

# Using the MQTT protocol.
PROTOCOL = IoTHubTransportProvider.MQTT
MESSAGE_TIMEOUT = 10000

# Define the JSON message to send to IoT Hub.

#~1~
#At startup send a Device.DeviceInfo message for Gateway (optional)
#This message should be sent after startup and normally contains only static information.
#You should send it to show software versions, and other static data but nothing breaks if you don't send it.
#You can send this message more often, when that static data does change, but typically you should not send this more than a couple of times a day.
DEVICE_INFO = "{\"Timestamp\": \"2017-06-01T10:00:01.0000000Z\",\
			\"DeviceId\": \"CLGW001\",\
            \"SubSystem\": \"Device\",\
			\"MessageType\": \"DeviceInfo\",\
			\"DeviceType\": \"Gateway\",\
			\"HardwareVersion\": \"1\",\
			\"OperatingSystemVersion\": \"Rasbpian Linux\",\
            \"ApplicationVersion\": \"1.0.0\",\
			\"Parameters\": \
			{\
			\"ComputerName\": \"Samer's Raspberry Pi 3\", \
			\"RobotHWVersion\": \"2\",\
			\"TransportHWVersion\": \"3\"}\
			}"
            
#~2~
#Every minute send a Device.Telemetry message for each RuuviTag
#To prevent your device from going offline, you should occasionally send a message.
#In your case we'll do that by regularly (every minute?) sending a telemetry messages for each RuubiTag for which we can show the graphs in the portal.
#For testing you  could do this faster, e.g. every 10 seconds.
TELEMETRY = "{\"Timestamp\": \"2017-06-01T10:00:03.0000000Z\",\
			\"DeviceId\": \"CLGW001\",\
            \"ApplianceId\": \" CLRT001\",\
            \"SubSystem\": \"Device\",\
			\"MessageType\": \"Telemetry\",\
			\"Data\": \
			{\
			\" AccelerationX\": 0.000, \
			\" AccelerationY\": 0.000,\
			\" AccelerationZ\": 0.000,\
            \" Humidity\": 74,\
            \" Pressure\": 978,\
            \" Rssi\": -34,\
            \" Temperature\": 21.4,\
            \" Voltage\": 2.93\
            }\
			}"
            
#~3~
#At startup and every hour send a Device.Status message for each RuuviTag
#This message must be sent after boot, and as suggested above, every hour for each via BlueTooth connected RuuviTag.
DEVICE_STATUS_RUUVI = "{\"Timestamp\": \"2017-06-01T10:00:01.0000000Z\",    \
			\"DeviceId\": \"CLGW001\",                                      \
            \"ApplianceId\": \"CLRT001\",                                   \
            \"SubSystem\": \"Device\",                                      \
			\"MessageType\": \"Status\",                                    \
            \"SystemStatus\": \"Ok\",                                       \
            \"SubStates\": [                                                \
            ]                                                               \
			}" 
            
#~4~
#At startup and every hour send a Device.Status message for the gateway
#This message must be sent after boot and always when the connection has been lost and restored.  
#When you send a status message, the device comes online in conNXT.
#When you keep sending messages every couple of minutes your device stays online (no keep alive is needed when you send telemetry every minute).
#When you don't send the Device.Status message for the gateway and for each RuuviTag, they are shown offline in conNXT, even when sending telemetry.
#Note, when conNXT sees (telemetry) messages coming in while a device is offline, conNXT will send a 'Status' command and the gateway should resend a Status messages for itself and all RuuviTags.
#Since you will not be implementing commands just yet,  I suggest you send status messages on startup and every hour, so the eventually come back online even when the connection was lost.
DEVICE_STATUS_GATEWAY = "{                                          \
            \"Timestamp\": \"2017-06-01T10:00:01.0000000Z\",        \
			\"DeviceId\": \"CLGW001\",                              \
            \"SubSystem\": \"Device\",                              \
			\"MessageType\": \"Status\",                            \
            \"SystemStatus\": \"Ok\",                               \
            \"SubStates\":                                          \
            [                                                       \
            {                                                       \
            \"SubSystem\": \"CLRT001\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 1,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },                                                      \
            {                                                       \
            \"SubSystem\": \"CLRT002\",                             \
            \"Status\": \"Disconnected\",                           \
            \"DisplayOrder\": 2,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            }                                                       \
            ]                                                       \
			}" 

MSG_TXT = TELEMETRY




INTERVAL = 10

def send_confirmation_callback(message, result, user_context):
    print ( "IoT Hub responded to message with status: %s" % (result) )

def iothub_client_init():
    # Create an IoT Hub client
    client = IoTHubClient(CONNECTION_STRING, PROTOCOL)
    return client

# Handle direct method calls from IoT Hub
def device_method_callback(method_name, payload, user_context):
    global INTERVAL
    print ( "\nMethod callback called with:\nmethodName = %s\npayload = %s" % (method_name, payload) )
    device_method_return_value = DeviceMethodReturnValue()
    if method_name == "SetTelemetryInterval":
        try:
            INTERVAL = int(payload)
            # Build and send the acknowledgment.
            device_method_return_value.response = "{ \"Response\": \"Executed direct method %s\" }" % method_name
            device_method_return_value.status = 200
        except ValueError:
            # Build and send an error response.
            device_method_return_value.response = "{ \"Response\": \"Invalid parameter\" }"
            device_method_return_value.status = 400
    else:
        # Build and send an error response.
        device_method_return_value.response = "{ \"Response\": \"Direct method not defined: %s\" }" % method_name
        device_method_return_value.status = 404
    return device_method_return_value

def iothub_client_telemetry_sample_run():

    try:
        client = iothub_client_init()
        print ( "IoT Hub device sending periodic messages, press Ctrl-C to exit" )

        # Set up the callback method for direct method calls from the hub.
        client.set_device_method_callback(
            device_method_callback, None)

        while True:
            # Build the message.
            message = IoTHubMessage(MSG_TXT)

            # Send the message.
            print( "Sending message: %s" % message.get_string() )
            client.send_event_async(message, send_confirmation_callback, None)
            time.sleep(INTERVAL)

    except IoTHubError as iothub_error:
        print ( "Unexpected error %s from IoTHub" % iothub_error )
        return
    except KeyboardInterrupt:
        print ( "IoTHubClient sample stopped" )

if __name__ == '__main__':
    print ( "IoT Hub Quickstart #2 - Simulated device" )
    print ( "Press Ctrl-C to exit" )
    iothub_client_telemetry_sample_run()