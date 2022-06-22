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
CONNECTION_STRING = "HostName=connxt-iothub.azure-devices.net;DeviceId=CLGW001;SharedAccessKey=JrGP7ZX7njKebOkVVY9yLb05IIyR1AL19WJ6A1IJDFk="



# Using the MQTT protocol.
PROTOCOL = IoTHubTransportProvider.MQTT
MESSAGE_TIMEOUT = 10000

client = IoTHubClient(CONNECTION_STRING, PROTOCOL)
# Define the JSON message to send to IoT Hub.
            
#~4~
#At startup and every hour send a Device.Status message for the gateway
#This message must be sent after boot and always when the connection has been lost and restored.  
#When you send a status message, the device comes online in conNXT.
#When you keep sending messages every couple of minutes your device stays online (no keep alive is needed when you send telemetry every minute).
#When you don't send the Device.Status message for the gateway and for each RuuviTag, they are shown offline in conNXT, even when sending telemetry.
#Note, when conNXT sees (telemetry) messages coming in while a device is offline, conNXT will send a 'Status' command and the gateway should resend a Status messages for itself and all RuuviTags.
#Since you will not be implementing commands just yet,  I suggest you send status messages on startup and every hour, so the eventually come back online even when the connection was lost.
DEVICE_STATUS_GATEWAY = "{                                          \
            \"Timestamp\": \""+UtcNow()+"\",                        \
            \"DeviceId\": \"CLGW001\",                              \
            \"SubSystem\": \"Device\",                              \
            \"MessageType\": \"Status\",                            \
            \"SystemStatus\": \"Ok\",                               \
            \"SubStates\":                                          \
            [                                                       \
            {                                                       \
            \"SubSystem\": \"CLRT001\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 0,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },                                                      \
            {                                                       \
            \"SubSystem\": \"CLRT002\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 1,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },                                                       \
            {                                                       \
            \"SubSystem\": \"CLRT003\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 2,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },                                                       \
            {                                                       \
            \"SubSystem\": \"CLRT004\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 3,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },                                                       \
            {                                                       \
            \"SubSystem\": \"CLRT005\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 4,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },\
            {                                                       \
            \"SubSystem\": \"CLRT006\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 5,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },\
            {                                                       \
            \"SubSystem\": \"CLRT007\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 6,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },\
            {                                                       \
            \"SubSystem\": \"CLRT008\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 7,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },\
            {                                                       \
            \"SubSystem\": \"CLRT009\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 8,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },\
            {                                                       \
            \"SubSystem\": \"CLRT010\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 9,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },\
            {                                                       \
            \"SubSystem\": \"CLRT011\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 10,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },\
            {                                                       \
            \"SubSystem\": \"CLRT012\",                             \
            \"Status\": \"Ok\",                                     \
            \"DisplayOrder\": 11,                                    \
            \"Value\": \"\",                                        \
            \"Unit\": \"\",                                         \
            \"Information\": \"\"                                  \
            },\
            ]                                                       \
            }" 

MSG_TXT = DEVICE_STATUS_GATEWAY




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