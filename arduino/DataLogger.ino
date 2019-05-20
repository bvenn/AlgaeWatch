#include <BH1750.h>
#include <OneWire.h>
#include <DallasTemperature.h>
#include <DS3231.h>
#include <Narcoleptic.h>
#include <SoftwareSerial.h>
#include <Wire.h>

#define ONE_WIRE_BUS 2 //TemperatureDataPin on D2

SoftwareSerial mySerial(10, 11);
//SIM Module
//SIM800L:  TXD pin   -> Arduino Digital 10
//          RXD pin   -> Arduino Digital 11

BH1750 lightMeter;
//LightModule
// BH1750:  SDA pin   -> Arduino Analog 4 or the dedicated SDA pin
//          SCL pin   -> Arduino Analog 5 or the dedicated SCL pin

OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature sensors(&oneWire);

DS3231  rtc(SDA, SCL);
//Clock Module
// DS3231:  SDA pin   -> Arduino Analog 4 or the dedicated SDA pin
//          SCL pin   -> Arduino Analog 5 or the dedicated SCL pin

const int ledPin  = 4; //LED
float temp0 = 0;
float temp1 = 0;
float temp2 = 0;
float temp3 = 0;
float temp4 = 0;
float temp5 = 0;
float light = 0;
String dateStamp;// = "";

String apn = "";           //APN
String apn_u = "";                     //APN-Username
String apn_p = "";                     //APN-Password
String url = "";  //URL for HTTP-POST-REQUEST
String data;   //datastring

const long samplingInterval = 60000; //every 110 seconds (up to 2147483647)


void setup() {
  Narcoleptic.disableTimer1();
  Narcoleptic.disableTimer2();
  Narcoleptic.disableSPI();
  delay(500);
  Serial.begin(9600);
  mySerial.begin(9600);
  Wire.begin();
  sensors.begin();
  pinMode(LED_BUILTIN, OUTPUT);
  delay(500);
  rtc.begin();
  sensors.getTempCByIndex(0);
  sensors.getTempCByIndex(1);
  sensors.getTempCByIndex(2);
  sensors.getTempCByIndex(3);
  sensors.getTempCByIndex(4);
  lightMeter.begin(BH1750::ONE_TIME_HIGH_RES_MODE);
  delay(15000);
}

void loop() {
  loggTemp();
  delay(1000);
  Narcoleptic.delay(samplingInterval);
}


void loggTemp() {
  digitalWrite(LED_BUILTIN,HIGH);
  delay(2000);
  digitalWrite(LED_BUILTIN,LOW);
  mySerial.println("AT+CSCLK=0");
  delay(300);
  mySerial.println("AT+CSCLK=0");
  dateStamp = rtc.getDateStr(FORMAT_SHORT,FORMAT_BIGENDIAN,'_');
  float lux = lightMeter.readLightLevel();
  sensors.requestTemperatures();
  float t0 = sensors.getTempCByIndex(0);
  float t1 = sensors.getTempCByIndex(1);  
  float t2 = sensors.getTempCByIndex(2); 
  float t3 = sensors.getTempCByIndex(3);  
  float t4 = sensors.getTempCByIndex(4); 
  float t5 = rtc.getTemp();  
  digitalWrite(LED_BUILTIN, HIGH);   
  delay(200);                       
  digitalWrite(LED_BUILTIN, LOW);  
  delay(200);                       
  digitalWrite(LED_BUILTIN, HIGH);   
  delay(200);                       
  digitalWrite(LED_BUILTIN, LOW);  
  data = "{\"D\":" + dateStamp +",\"T1\":" + String(t0) + ",\"T2\":" + String(t1) + ",\"T3\":" + String(t2) + ",\"T4\":" + String(t3) + ",\"T5\":" + String(t4) + ",\"T6\":" + String(t5) + "}";
  gsm_sendhttp();
}


void gsm_sendhttp() {
  
  mySerial.println("AT");
  //runsl();//Print GSM Status an the Serial Output;
  delay(4000);
  mySerial.println("AT+SAPBR=3,1,Contype,GPRS");
  //runsl();
  delay(100);
  mySerial.println("AT+SAPBR=3,1,APN," + apn);
  //runsl();
  delay(100);
  mySerial.println("AT+SAPBR=3,1,USER," + apn_u); //Comment out, if you need username
  //runsl();
  delay(100);
  mySerial.println("AT+SAPBR=3,1,PWD," + apn_p); //Comment out, if you need password
  //runsl();
  delay(100);
  mySerial.println("AT+SAPBR =1,1");
  //runsl();
  delay(100);
  mySerial.println("AT+SAPBR=2,1");
  //runsl();
  delay(2000);
  mySerial.println("AT+HTTPINIT");
  //runsl();
  delay(100);
  mySerial.println("AT+HTTPPARA=CID,1");
  //runsl();
  delay(100);
  mySerial.println("AT+HTTPPARA=URL," + url);
  //runsl();
  delay(100);
  mySerial.println("AT+HTTPPARA=CONTENT,application/x-www-form-urlencoded");
  //runsl();
  delay(100);
  mySerial.println("AT+HTTPDATA=" + String(data.length()) + ",10000");
  //runsl();
  delay(100);
  mySerial.println(data);
  //runsl();
  delay(10000);
  mySerial.println("AT+HTTPACTION=1");
  //runsl();
  delay(5000);
  mySerial.println("AT+HTTPREAD");
  //runsl();
  delay(100);
  mySerial.println("AT+HTTPTERM");
  //runsl(); 
  delay(200);
  mySerial.println("AT+CSCLK=2");
  delay(200);
}

//Print GSM Status
//void runsl() {
//  while (mySerial.available()) {
//    Serial.write(mySerial.read());
//  }
//}
