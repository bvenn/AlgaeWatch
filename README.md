# AlgaeWatch

AlgaeWatch is a data logging project emerged from the interest in biological issues concerning climate change and its influence on water bodies and the organisms living in them. In previous studies during my bachelor’s programme, I dealt with the algae’s response to elevated water temperatures.
To study the naturally occurring variations of the water temperature in ponds and to determine its stages in the yearly development I decided to use a data logger to collect water temperatures in several depths. Additionally I aimed to collect light intensity data for monitoring the cloud conditions. I quickly realised most commercially available loggers lack at least one of the following criteria:

 - water resistance (of the sensors themselves, as well as the main module)
 - energy consumption
    - when used in remote areas the sensor should run independently for many days
 - size
    - a small size is desired to ensure mobility
 - flexibility (modifications)
    - if more data should be gathered, additional sensors should be added easily
 - costs
    - the project was aimed to cost less than 60,- €
 - remote monitoring
     - to make the data collection easier and automated
    - to be able to identify problems with the data logging
 - automated data analysis
    - to recognize hidden patterns within the data
 - automated visualization
    - to identify the right time point for biological samples to be taken from the pond/river/lake etc.

A year ago, I already build an Arduino based data logger, that saves the data on a memory card. It is powered by a power bank that has to be replaced every 8-10 days. It quickly turned out, that problems with the power supply stability made it necessary to ensure monitoring even outside the 9-day period. But not only design-related issues occurred. Sometimes heavy weather events distort the sensor location, or drought periods lowered the water level so that the top sensor has already jetted out of the water.
Now, a year later I found a solution addressing the above-mentioned problems and present an improved concept, combining the broad possibilities of microcontrollers and the power of F# together with SAFE to make data logging more convenient and sophisticated.

### Pond:

![Pond1](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/20180621_091458.jpg)

### Location of Sensors:

![Pond2](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/Pond.png)



### This project includes:
 - Real time data logging and transmission using Arduino resources
 - [SQLite database management](https://www.sqlite.org/about.html).
 - [SAFE](https://safe-stack.github.io/)
 - Automated FTP server data integration
 - Continuous wavelet transform for data analysis
 - [FSharp.Plotly visualization](https://github.com/muehlhaus/FSharp.Plotly)


To measure the temperatures several DS18B20 waterproof sensors were placed in a pond, hooked up
with an Arduino Nano in waterproof cases. For light measurements a BH1750 light sensor was positioned
40 cm above ground on a pipe stuck in the ground. To aquire the exact time of the measurements, a DS3231 real time clock was used.
A SIM800L module connects to internet through GPRS and transmits the data as a JSON string to the web service.
The project ist powered by a 6000 mAh power bank.
You can find the arduino sketch in /arduino/datalogger.ino.

 - Used Arduino libraries
    - BH1750.h
    - OneWire.h
    - DallasTemperature.h
    - DS3231.h
    - Narcoleptic.h
    - SoftwareSerial.h
    - Wire.h


The transmitted data is provided with a timestamp by the webserver and stored in a SQLite database.
For compatibility both data/time indications are stored in the following format "yyMMddHHmmss". All in all six temperature measurements are stored together with one
light intensity value. 

I noticed that heavy rain events had a big influence on the water temperatures. Especially in the top layer a drop of (> 5°C (9 °F) in 15 minutes) could be observed (27. May 2018).

![Rainevent](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/rainevent.png)

To incorporate rain data in the acquired measurements I made use of the Climate Data Center of the “Deutscher Wetterdienst” ([DWD](https://www.dwd.de/EN/climate_environment/cdc/cdc.html;jsessionid=AA27C86FF41C71805E761B7F4B1D957D.live21061). Multiple weather parameters of hundreds of weather stations all over Germany are stored and updated on a daily basis. Coincidentally,
such a station is right next to the pond where the temperature sensors are located (station id: 2486; latitude: 49.4262; longitude: 7.7557). The amount of rain is given every 10 minutes as mm / m² / 10min which indicates the litres of rain fallen on one square meter during the last ten minutes.
Because the most recent rain data have not yet completed the full quality control, the data cannot be integrated in real time but has to be fetched once in a while.


To examine the temperature data with respect to reoccurring patterns and to identify anomalies an approach called continuous wavelet transform (CWT) is applied. The CWT is a multiresolution analysis
method to gain insights into frequency components of a signal with simultaneous temporal classification. Wavelet in this context stands for small wave and describes a window
function which is convoluted with the original signal at every position in time ([Wavelet tutorial](http://users.rowan.edu/~polikar/WTtutorial.html)). Many wavelets exist, every one of them is useful for a certain application, thereby ‘searching’ for specific patterns in the data. By increasing the dimensions (scale) of the wavelet function, different frequency patterns are studied.
Many wavelets exist, every one of them is useful for a certain application, thereby scanning the data for specific patterns.
In contrast to the fourier transform, that gives a perfect frequency resolution but no time resolution, the CWT is capable of mediating between the two opposing properties of time resolution and frequency resolution (Heisenberg's uncertainty principle).
For high frequencies the time resolution outweighs the frequency resolution, whereas in low frequencies the time cannot be determined exactly, but the frequency is precise. This is beneficial, because when fast fluctuations are in the data it is not necessarily important to know the exact frequency, but the time when it happened. And when there is a slowly oscillating signal it is favourable to identify the unterlying frequency rather than the time point it occurred.
In this analysis the single spiked Ricker wavelet (also called Mexican hat wavelet) is used, which corresponds to the negative second derivative of the gaussian function.

![Wavelet overview](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/PicOverview.png)
![Wavelet detail](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/PicDetail2.png)

To visualize the collected data, FSharp.Plotly is used, an interactive F# charting library using plotly.js ([Plotly](https://github.com/muehlhaus/FSharp.Plotly)).
Last years data collected with the old sensor version is included, so analysis can also performed on these data.


## Installation

Please follow the installation instructions on [SAFE Stack](https://safe-stack.github.io/docs/quickstart/) and download the project.

To run the server use the following command:

```bash
fake build -t Run
```

## Usage

After entering the `fake build -t Run` command, you can open your Browser on http://localhost:8080/ and visiting the web page.

![Home screen]( https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/01_Home.png)

There are four options to choose from. By clicking `show last year` an interactive Plotly chart is loaded. This chart shows temperature data from all 6 temperature sensors, the light sensor, and the fetched rain data from the DWD.
Increasing indices [T1 .. T6] imply lower depth of the sensor. It is obvious, that there were some connection problems in the initial phase of the sensor and you can see how Sensor T2 was no longer covered with water during autumn 2018.
From december to february the pond was frozen in the top layer, but the deepest sensors recorded temperatures up to 7.5 °C. The light sensor measures in arbitrary units from 0 to 1024. The rain data show clearly the severe drought in germany in autumn 2018.

![Overview](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/02_Overview.png)


When clicking on the second button "show from-to" you can specify the time period the plot should cover. Hereby, the data is aquired from the SQLite data base.

![Selection](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/03_Selection1.png)

![Chunk](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/05_chunk.png)

To apply the continuous wavelet transform, the third button can be clicked, thereby specifying again the time period, and the sensor number, the transform should be applied on.

![WaveletSelection](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/06_waveletselection.png)



![Wavelet1](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/08_wavelet1.png)
![Wavelet2](https://raw.githubusercontent.com/bvenn/AlgaeWatch/master/src/Client/public/Screenshots/09_wavelet2.png)


### Receiving data from logger




### SAFE Stack Documentation

This project is powered by [SAFE Stack](https://safe-stack.github.io/).
You will find more documentation about the used F# components at the following places:

* [Suave](https://suave.io/index.html)
* [Fable](https://fable.io/docs/)
* [Elmish](https://elmish.github.io/elmish/)
* [Fable.Remoting](https://zaid-ajaj.github.io/Fable.Remoting/)
* [Fulma](https://fulma.github.io/Fulma/)


I hope this project is going to help increasing the awareness of the power of F# and open source projects to continue 
