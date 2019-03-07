import time
from ruuvitag_sensor.ble_communication import BleCommunicationNix
from ruuvitag_sensor.ruuvitag import RuuviTag
#from ruuvitag_sensor.url_decoder import UrlDecoder

ble = BleCommunicationNix()

# list all your tags MAC: TAG_NAME
tags = {
    'C4:BC:81:D9:19:90': 'CLRT001'
}

# set DataFormat
# 1 - Weather station
dataFormat = '1'

db = True # Enable or disable database saving True/False
dbFile = '/home/pi/ruuvitag/ruuvitag.db' # path to db file

pressure_var = 0000.00
temperature_var = 00
humidity_var = 00
acceleration_x_var = 0
acceleration_y_var = 0
acceleration_z_var = 0



if db:
    import sqlite3
    # open database
    conn = sqlite3.connect(dbFile)

    # check if table exists
    cursor = conn.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='sensors'")
    row = cursor.fetchone() 
    if row is None:
        print("DB table not found. Creating 'sensors' table ...")
        conn.execute('''CREATE TABLE sensors
            (
                id              INTEGER     PRIMARY KEY AUTOINCREMENT   NOT NULL,
                timestamp       NUMERIC     DEFAULT CURRENT_TIMESTAMP,
                mac             TEXT        NOT NULL,
                name            TEXT        NULL,
                temperature     NUMERIC     NULL,
                humidity        NUMERIC     NULL,
                pressure        NUMERIC     NULL
            );''')
        print("Table created successfully\n")

# Extended RuuviTag with name, and data output
class Rtag(RuuviTag):

    def __init__(self, mac, name):
        self._mac = mac
        self._name = name

    @property
    def name(self):
        return self._name

    def getData(self):
        sensor_object = RuuviTag('C4:BC:81:D9:19:90')
        return sensor_object.update()

    
now = time.strftime('%Y-%m-%d %H:%M:%S')
print(now+"\n")

dbData = {}
    
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
                pressure_var = value
                print("The pressure now is: {} millibars (mb)".format(pressure_var))
            if sensor == 'temperature':
                temperature_var = value
                print("The temperature now is: {} degree C".format(temperature_var))
            if sensor == 'humidity':
                humidity_var = value
                print("The humidity now is: {}%".format(humidity_var))
            if sensor == 'acceleration_x':
                acceleration_x_var = value
                print("Acceleration_x now is: {}".format(acceleration_x_var))
            if sensor == 'acceleration_y':
                acceleration_y_var = value
                print("Acceleration_y now is: {}".format(acceleration_y_var))
            if sensor == 'acceleration_z':
                acceleration_z_var = value
                print("Acceleration_z now is: {}".format(acceleration_z_var))
            dbData[tag._mac].update({sensor: value})
    print("\n")
        

if db:
    # save data to db
    print("Saving data to database ...")
    for mac, content in dbData.items():
        conn.execute("INSERT INTO sensors (timestamp,mac,name,temperature,humidity,pressure) \
            VALUES ('{}', '{}', '{}', '{}', '{}', '{}')".\
            format(now, mac, content['name'], content['temperature'], content['humidity'], content['pressure']))
    conn.commit()
    conn.close()
    print("Done.")
