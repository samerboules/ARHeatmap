# Copyright (c) ICT Group B.V
# ICT CoLab
# Developed by: Samer Boules (samer.boules@ict.nl)

import time
from ruuvitag_sensor.ble_communication import BleCommunicationNix
from ruuvitag_sensor.ruuvitag import RuuviTag
import random
import sys
import iothub_client
from iothub_client import IoTHubClient, IoTHubClientError, IoTHubTransportProvider, IoTHubClientResult
from iothub_client import IoTHubMessage, IoTHubMessageDispositionResult, IoTHubError, DeviceMethodReturnValue
import datetime

ble = BleCommunicationNix()

# list all your tags MAC: TAG_NAME
tags = {
    'C4:BC:81:D9:19:90': 'CLRT001',
    'CC:04:C4:5E:88:70': 'CLRT002',
    'C5:BE:93:03:DF:5B': 'CLRT003',
    'C7:43:E0:B0:D4:4B': 'CLRT004',
    'FD:A2:AE:0E:64:71': 'CLRT005',
    'DC:0D:6B:8E:18:08': 'CLRT006',
    'CE:47:34:CA:DB:60': 'CLRT007',
    'F4:2F:EB:60:CE:15': 'CLRT008',
    'F9:0C:D5:D5:9B:6E': 'CLRT009',
    'E1:5A:43:A0:94:74': 'CLRT010',
    'EB:E7:95:39:AD:80': 'CLRT011',
    'E5:19:E0:4D:E6:0C': 'CLRT012'
    
}

def UtcNow():
    now = datetime.datetime.utcnow()
    return now.strftime("%Y-%m-%dT%H:%M:%S.%fZ")

# The device connection string to authenticate the device with your ConNXT hub.
CONNECTION_STRING = "HostName=connxtv2acceptance-iothub.azure-devices.net;DeviceId=CLGW001;SharedAccessKey=P6GUps40aRBIt0isUE55Ip2qB24vmW1AeMDdZ/zyj6E="

# Using the MQTT protocol.
PROTOCOL = IoTHubTransportProvider.MQTT
MESSAGE_TIMEOUT = 10000

# Create an IoT Hub client
client = IoTHubClient(CONNECTION_STRING, PROTOCOL)
            
# Define the JSON message to send to IoT Hub.          
#~2~
#Every minute send a Device.Telemetry message for each RuuviTag
#To prevent your device from going offline, you should occasionally send a message.
#In your case we'll do that by regularly (every minute?) sending a telemetry messages for each RuubiTag for which we can show the graphs in the portal.
#For testing you  could do this faster, e.g. every 10 seconds.
#TELEMETRY = "{\"Timestamp\": \""+UtcNow()+"\",\
#              \"DeviceId\": \"CLGW001\",\
#              \"ApplianceId\": \"CLRT001\",\
#              \"SubSystem\": \"Device\",\
#              \"MessageType\": \"Telemetry\",\
#              \"Data\": {\
#              \"AccelerationX\": 0.000,\
#              \"AccelerationY\": 0.000,\
#              \"AccelerationZ\": 0.000,\
#              \"Humidity\": 74,\
#              \"Pressure\": 978,\
#              \"Rssi\": -34,\
#              \"Temperature\": 21.4,\
#              \"Voltage\": 2.93\
#              }}"

INTERVAL = 3
# set DataFormat
# 1 - Weather station
dataFormat = '1'

# Extended RuuviTag with name, and data output
class Rtag(RuuviTag):

    def __init__(self, mac, name):
        self._mac = mac
        self._name = name

    @property
    def name(self):
        return self._name

    def getData(self):
        sensor_object = RuuviTag(self._mac)
        return sensor_object.update()

    
now = time.strftime('%Y-%m-%d %H:%M:%S')
print(now+"\n")

dbData = {}


def update_values():
    pressure_var = 0000.00
    temperature_var = 00
    humidity_var = 00
    acceleration_x_var = 0
    acceleration_y_var = 0
    acceleration_z_var = 0
    for mac, name in tags.items():
        tag = Rtag(mac, name)

        print("Looking for {} ({})".format(tag._name, tag._mac))
        # if weather station
        if dataFormat == '1': # get parsed data
            data = tag.getData()
            print ("Data received:", data)

            dbData[tag._mac] = {'name': tag._name}
            # add each sensor with value to the lists
            for sensor, value in data.items():
                if sensor == 'pressure':
                    #global pressure_var
                    pressure_var = value
                    #print("The pressure now is: {} millibars (mb)".format(pressure_var))
                if sensor == 'temperature':
                    #global temperature_var
                    temperature_var = value
                    #print("The temperature now is: {} degree C".format(temperature_var))
                if sensor == 'humidity':
                    #global humidity_var
                    humidity_var = value
                    #print("The humidity now is: {}%".format(humidity_var))
                if sensor == 'acceleration_x':
                    #global acceleration_x_var
                    acceleration_x_var = value
                    #print("Acceleration_x now is: {}".format(acceleration_x_var))
                if sensor == 'acceleration_y':
                    #global acceleration_y_var
                    acceleration_y_var = value
                    #print("Acceleration_y now is: {}".format(acceleration_y_var))
                if sensor == 'acceleration_z':
                    #global acceleration_z_var
                    acceleration_z_var = value
                    #print("Acceleration_z now is: {}".format(acceleration_z_var))
                dbData[tag._mac].update({sensor: value})
                
            TELEMETRY = "{\"Timestamp\": \""+UtcNow()+"\",\
                  \"DeviceId\": \"CLGW001\",\
                  \"ApplianceId\": \""+str(tag._name)+"\",\
                  \"SubSystem\": \"Device\",\
                  \"MessageType\": \"Telemetry\",\
                  \"Data\": {\
                  \"AccelerationX\": "+str(acceleration_x_var)+",\
                  \"AccelerationY\": "+str(acceleration_y_var)+",\
                  \"AccelerationZ\": "+str(acceleration_z_var)+",\
                  \"Humidity\": "+str(humidity_var)+",\
                  \"Pressure\": "+str(pressure_var)+",\
                  \"Rssi\": -34,\
                  \"Temperature\": "+str(temperature_var)+",\
                  \"Voltage\": 2.93\
                  }}"

            MSG_TXT = TELEMETRY
            
            # Build the message.
            message = IoTHubMessage(MSG_TXT)

            print ( "conNXT device sending periodic messages, press Ctrl-C to exit" )
            
            # Set up the callback method for direct method calls from the hub.
            client.set_device_method_callback(
                device_method_callback, None)
            
            # Send the message.
            print( "Sending message: %s" % message.get_string() )
            client.send_event_async(message, send_confirmation_callback, None)
            
            
        time.sleep(INTERVAL)        
        print("\n")
        
        


    
def send_confirmation_callback(message, result, user_context):
    print ( "conNXT responded to message with status: %s" % (result) )



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

def connxt_client_telemetry_run():

    try:
        update_values()

    except IoTHubError as iothub_error:
        print ( "Unexpected error %s from conNXT" % iothub_error )
        return
    except KeyboardInterrupt:
        print ( "conNXTClient sample stopped" )

if __name__ == '__main__':
    print ( "Copyright (c) ICT Group B.V" )
    print ( "ICT CoLab" )
    print ( "Developed by: Samer Boules - samer.boules@ict.nl" )
    print ( "RUUVI to ConNXT application started..." )
    print ( "Press Ctrl-C to exit" )
    connxt_client_telemetry_run()
