# Copyright (c) ICT Group B.V
# ICT CoLab
# Developed by: Samer Boules (samer.boules@ict.nl)

import random
import time
import sys
import iothub_client
from iothub_client import IoTHubClient, IoTHubClientError, IoTHubTransportProvider, IoTHubClientResult
from iothub_client import IoTHubMessage, IoTHubMessageDispositionResult, IoTHubError, DeviceMethodReturnValue
import datetime

def UtcNow():
    now = datetime.datetime.utcnow()
    return now.strftime("%Y-%m-%dT%H:%M:%S.%fZ")
    
    
# The device connection string to authenticate the device with your ConNXT hub.
CONNECTION_STRING = "HostName=connxtv2acceptance-iothub.azure-devices.net;DeviceId=CLGW001;SharedAccessKey=P6GUps40aRBIt0isUE55Ip2qB24vmW1AeMDdZ/zyj6E="

# Using the MQTT protocol.
PROTOCOL = IoTHubTransportProvider.MQTT
MESSAGE_TIMEOUT = 10000


client = IoTHubClient(CONNECTION_STRING, PROTOCOL)
# Define the JSON message to send to IoT Hub.
           
            
#~3~
#At startup and every hour send a Device.Status message for each RuuviTag
#This message must be sent after boot, and as suggested above, every hour for each via BlueTooth connected RuuviTag.
DEVICE_STATUS_RUUVI = "{\"Timestamp\": \""+UtcNow()+"\",    \
            \"DeviceId\": \"CLGW001\",                                      \
            \"ApplianceId\": \"CLRT011\",                                   \
            \"SubSystem\": \"Device\",                                      \
            \"MessageType\": \"Status\",                                    \
            \"SystemStatus\": \"Ok\",                                       \
            \"SubStates\": [                                                \
            ]                                                               \
            }" 

MSG_TXT = DEVICE_STATUS_RUUVI
INTERVAL = 2

def send_confirmation_callback(message, result, user_context):
    print ( "IoT Hub responded to message with status: %s" % (result) )

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
        print ( "IoT Hub device sending periodic messages, press Ctrl-C to exit" )

        # Set up the callback method for direct method calls from the hub.
        client.set_device_method_callback(
            device_method_callback, None)

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