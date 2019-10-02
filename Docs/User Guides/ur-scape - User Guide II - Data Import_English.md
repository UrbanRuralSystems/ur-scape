# **ur-scape** 

## User Guide II: Data Import

<br>Current ur-scape Version: v0.9.91<br>
Licence Type: MIT Licence

**Developed By**<br>
Future Cities Laboratory (FCL)<br>
Singapore-ETH Centre

**Supported By**<br>
National Research Foundation (NRF)<br>
Asian Development Bank (ADB)<br>
Swiss Secretariat for Economic Affairs (SECO)

**Development Team**<br>
Prof Dr Stephen Cairns (Principal Investigator)<br>
David Neudecker (Design Leader & Data Engineer)<br>
Michael Joos (Senior Software Engineer)<br>
Muhammad Salihin Bin Zaol-kefli (Software Engineer)<br>
Serene Chen (User Experience Designer)<br>

**SECO Project Team**<br>
Dr Devisari Tunas (Project Leader)<br>
Rosita Samsudin (Project Coordinator)<br>
Dr Laksmi Darmoyono (Data Collector & Indonesian Coordinator)<br>
Rina Wulandari (Data Scientist)<br>

<br>
<br>
<br>

June 2019

<div style="page-break-after: always;"></div>
## About ur-scape

<br>ur-scape is an open-source spatial planning tool designed to support sustainable futures in rapidly developing urban and rural regions where data is often difficult to access and uneven in quality, and where development needs are especially urgent and challenging. ur-scape does this by bringing diverse kinds of data together and encouraging people to explore the data intuitively and in real-time.

ur-scape helps city makers, be they governments, businesses or communities, improve the quality of planning and design decisions. It helps develop liveable neighbourhoods, build responsive towns, reduce city ‘stress points’ (flooding, traffic snarls, poverty) and enhance ‘sweet spots’ (accessible, equitable, economically vibrant), and progress towards strategic development goals (regional, national and SDGs).

Developed by the Urban-Rural Systems (URS) team at Future Cities Laboratory (FCL), ur-scape is supported by the Asian Development Bank (ADB) and the Swiss Secretariat for Economic Affairs (SECO). Pilot implementation of the ur-scape tool is supported by the municipal planning authorities of the city of Bandung, the Indonesian Ministry of Agrarian Affairs and Spatial Planning (MASP), and the Bauhaus Weimar University.

<------------------------- [Write up about SECO PROJECT] ------------------------->

<br>
<br>
<br>

<div style="page-break-after: always;"></div>
## Table of Contents

<br>
[**Chapter 1: Introduction**](#Chapter 1: Introduction)<br>

[**Chapter 2: Importing Data with QGIS**](#Chapter 2: Importing Data with QGIS)<br>
&nbsp;&nbsp;&nbsp;[2.1 Overview](#2.1 Overview)<br>
&nbsp;&nbsp;&nbsp;[2.2 Vector (Polygon) Data](#2.2 Vector (Polygon) Data)<br>
&nbsp;&nbsp;&nbsp;[2.3 Vector (Polyline) Data](#2.3 Vector (Polyline) Data)<br>
&nbsp;&nbsp;&nbsp;[2.4 Vector (Point) Data](#2.4 Vector (Point) Data)<br>
&nbsp;&nbsp;&nbsp;[2.5 Raster Data](#2.5 Raster Data)<br>

[**Chapter 3: Importing Data with Excel**](#Chapter 3: Importing Data with Excel)<br>

[**Chapter 4: Preparing Tool-Specific Data (Advanced)**](#Chapter 4: Preparing Tool-Specific Data (Advanced))<br>
&nbsp;&nbsp;&nbsp;[4.1 Reachability: Road Network Data](#4.1 Reachability: Road Network Data)<br>
&nbsp;&nbsp;&nbsp;[4.2 Municipal Budget: Administrative Units Data](#4.2 Municipal Budget: Administrative Units Data)<br>

[**Chapter 5: Tutorials & Useful Links**](#Chapter 4: Tutorials & Useful Links)<br>

[**Chapter 6: Contact**](#Chapter 5: Contact)<br>



<div style="page-break-after: always;"></div>
# Chapter 1: Introduction

This user manual covers the alpha version of the data import tool for ur-scape, which currently runs on the open-source Geographic Information System (GIS) desktop application, QGIS. This import method has been developed for vector and raster data - and both categorised and uncategorised data.

**Requirements**

- QGIS 3.4.5 Madeira (Desktop application) 
- ur-scape Data Import Tool (Python script provided)
- Vector/ Raster data



<div style="page-break-after: always;"></div>
# Chapter 2: Importing Data with QGIS

## 2.1 Overview

To understand how to import data onto ur-scape, this chapter will recap some key points from Chapter 1.1 - Folder System, from User Guide I: Operations. 

The ur-scape package comprises three items:

  - **'Data' file folder**: This folder comprises key files/folders that the user should be familiar with to modify/upload data onto ur-scape.
  - **'urscape_Data' file folder**: Users should <u>disregard</u> this folder. It contains backend files/folders for ur-scape - any modifications to them may affect the operation of ur-scape.
  - **urscape.exe**: This is the executable file that users can double-click to run ur-scape.

Within the 'Data' file folder, the following items are important to importing data:

- **Sites [file folder]**: All data layers to be visualised on ur-scape are stored in this folder. Data layers should be automatically added here from the QGIS import process should the file path be defined correctly (this is explained in detail in the following chapters). Note: The default format for ur-scape data is .csv, and a mirroring .bin file will always be generated on running ur-scape. You may ignore the .bin folder.

- **layers [.csv]**: This is a list of the group names and data layers - with their respective display colours in RGB code. New data layers will be automatically added here, should the file path be defined correctly (this is likewise explained in detail in the following chapters). 

It should be highlighted that for a data layer to be visualised on ur-scape, there are two crucial points to note:

1. The .csv file of that data layer must be saved in the respective sites folder

2. The layer name must be accordingly defined in the layers.csv

   

<div style="page-break-after: always;"></div>
## 2.2 Vector (Polygon) Data



**Step 1: Enable the Python Console on QGIS**<br>On the top panel of your QGIS application, select Plugins > Python Console.

![](Images\20190515 QGIS1-01.png)<br><br>


(zoomed in to secondary Plugins menu)

![](Images\20190515 QGIS1B-02.png)
<div style="page-break-after: always;"></div>
**Step 2: Enable the Editor**<br>Click on the 'Show Editor' icon to view the Python editor interface.

![](Images\20190515 QGIS2-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS2B-02.png)

<div style="page-break-after: always;"></div>
**Step 3: Load ur-scape data import tool script**<br>Click on the 'Open Script' icon to load the ur-scape data import tool script.

![](Images\20190515 QGIS3-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS3B-02.png)<br><br>


![](Images\20190515 QGIS4-01.png)
<div style="page-break-after: always;"></div>
**Step 4: Load polygon vector data**<br> Add your polygon shapefile by dragging it from the Browser panel to the Layers panel.

![](Images\20190515 QGIS5-01.png)<br><br>


(zoomed in to Layers Panel)

![](Images\20190515 QGIS5B-02.png)
<div style="page-break-after: always;"></div>
**Step 5: Define output path (line 4 of script)**<br>Define the full folder path to where you would like to save the output files in line 4 of the Python script. 

If you will be handling multiple data sets in the same location, it is recommended to key in a path directory straight to where ur-scape and the files are saved - i.e. the 'Data' folder (C:/ur-scape/Application/Data).

Important Note: On copying the folder path from your Windows Explorer, the path will contain backslashes (\\) by default, which is not recognised by the script. You will be required to change the backslashes to forward slashes (/).

![](Images\20190515 QGIS6-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPolyline6B-02.png)
<div style="page-break-after: always;"></div>
**Step 6: Define name of layer (line 5 of script)**<br>Define the name of the data layer in line 5 of the Python script. Do note that the layer name should be in camel case (e.g. Drainage Capacity).

![](Images\20190515 QGIS7-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS7B-02.png)
<div style="page-break-after: always;"></div>
**Step 7: Identify field to be visualised**<br>Open the attribute table of your data layer (by right-clicking on the data layer to open a secondary menu as shown below). 

![](Images\20190515 QGIS8-01.png)

 Identify the field you would like to visualise.

![](Images\20190515 QGISPolyline9-01.png)



<div style="page-break-after: always;"></div>
**Step 8: Define name of field (line 6 of script)**<br> Type the name of the field you would like to visualise (identified in the above step) in line 6 of the Python script. Note that the spelling of the field must be identically spelled according to the field name in the attribute table.

 ![](Images\20190515 QGIS10-01.png)<br><br>

(zoomed in to Python Editor)

![](Images\20190515 QGIS10B-02.png)
<div style="page-break-after: always;"></div>
**Step 9: Define resolution (line 7 of script)**<br>ur-scape data is visualised as dots or rather cells, each containing a value. This resolution refers to the size of these cells.<br>

The following resolution are available, and should be selected according to the scale of the data. <br>0 = Neighbourhood (10 x 10m)<br>1 = City (100 x 100m)<br>2 = Metropolitan (300 x 300m)<br>3 = National (30 seconds; equals ~1x1km)<br>4 = Continental (300 seconds; equals ~10x10km)<br>5 = Global (30 min; equals ~50x50km)<br>

![](Images\20190515 QGIS11-01.png)<br><br>

<div style="page-break-after: always;"></div>
(zoomed in to Python Editor)

![](Images\20190515 QGIS11B-02.png)

<div style="page-break-after: always;"></div>
**Step 10: Define metadata (lines 10-15 of script)**<br>Key in the units, location and date of the data layer. You may set the colour of the layer in line 14 if you set the output path you set in Step 5 to the ur-scape 'Data' folder.

![](Images\20190515 QGIS12-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS12B-02.png)
<div style="page-break-after: always;"></div>
**Step 11: Run Script**<br>Click on the 'Run Script' icon to begin.

![](Images\20190515 QGIS13-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS13B-02.png)
<div style="page-break-after: always;"></div>
**Step 12: Once the tool is done, you will find the file in the output folder defined.**

![](Images\20190515 QGIS14-01.png)



![](Images\20190515 QGIS15-01.png)
<div style="page-break-after: always;"></div>
**Step 13: Imported data**

The imported data will be visualised on ur-scape with the basic parameters and metadata defined in the earlier steps. <br>

![](Images\20181031 QGIS17.jpg)

<div style="page-break-after: always;"></div>
## 2.3 Vector (Polyline) Data

If you have gone through Chapter 2.2 Vector (Polygon Data), you may skip to Step 4.

**Step 1: Enable the Python Console on QGIS**<br>On the top panel of your QGIS application, select Plugins > Python Console.

![](Images\20190515 QGIS1-01.png)<br><br>


(zoomed in to secondary Plugins menu)

![](Images\20190515 QGIS1B-02.png)
<div style="page-break-after: always;"></div>
**Step 2: Enable the Editor**<br>Click on the 'Show Editor' icon to view the Python editor interface.

![](Images\20190515 QGIS2-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS2B-02.png)

<div style="page-break-after: always;"></div>
**Step 3: Load ur-scape data import tool script**<br>Click on the 'Open Script' icon to load the ur-scape data import tool script.

![](Images\20190515 QGIS3-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS3B-02.png)<br><br>


![](Images\20190515 QGIS4-01.png)
<div style="page-break-after: always;"></div>
**Step 4: Load polyline vector data**<br> Add your polyline shapefile by dragging it from the Browser panel to the Layers panel.

![](Images\20190515 QGISPolyline5-01.png)<br><br>


(zoomed in to Layers Panel)

![](Images\20190515 QGISPolyline5B-02.png)
<div style="page-break-after: always;"></div>
**Step 5: Define output path (line 4 of script)**<br>Define the full folder path to where you would like to save the output files in line 4 of the Python script. 

If you will be handling multiple data sets in the same location, it is recommended to key in a path directory straight to where ur-scape and the files are saved - i.e. the 'Data' folder (C:/ur-scape/Application/Data).

Important Note: On copying the folder path from your Windows Explorer, the path will contain backslashes (\\) by default, which is not recognised by the script. You will be required to change the backslashes to forward slashes (/).

![](Images\20190515 QGISPolyline6-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPolyline6B-02.png)
<div style="page-break-after: always;"></div>
**Step 6: Define name of layer (line 5 of script)**<br>Define the name of the data layer in line 5 of the Python script. Do note that the layer name should be in camel case (e.g. Drainage Capacity).

![](Images\20190515 QGISPolyline7-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPolyline7B-02.png)
<div style="page-break-after: always;"></div>
**Step 7: Identify field to be visualised**<br>Open the attribute table of your data layer (by right-clicking on the data layer to open a secondary menu as shown below). 

![](Images\20190515 QGISPolyline8-01.png)

 Identify the field you would like to visualise.

![](Images\20190515 QGISPolyline9-01.png)



<div style="page-break-after: always;"></div>
**Step 8: Define name of field (line 6 of script)**<br> Type the name of the field you would like to visualise (identified in the above step) in line 6 of the Python script. Note that the spelling of the field must be identically spelled according to the field name in the attribute table.

 ![](Images\20190515 QGISPolyline10-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPolyline10B-02.png)
<div style="page-break-after: always;"></div>
**Step 9: Define resolution of exported data (line 7 of script)**<br>ur-scape data is visualised as dots or rather cells, each containing a value. This resolution refers to the size of these cells.<br>

The following resolution are available, and should be selected according to the scale of the data. <br>0 = Neighbourhood (10 x 10m)<br>1 = City (100 x 100m)<br>2 = Metropolitan (300 x 300m)<br>3 = National (30 seconds; equals ~1x1km)<br>4 = Continental (300 seconds; equals ~10x10km)<br>5 = Global (30 min; equals ~50x50km)<br>

It should be noted that for polyline data, a higher resolution can be selected regardless of the actual scale of the data, for a more precise visualisation of the polylines. Please see Step 13: Imported data.<br> 

![](Images\20190515 QGISPolyline11-01.png)<br><br>

<div style="page-break-after: always;"></div>
(zoomed in to Python Editor)

![](Images\20190515 QGISPolyline11B-02.png)
<div style="page-break-after: always;"></div>
**Step 10: Define metadata (lines 10-15 of script)**<br>Key in the units, location and date of the data layer. You may set the colour of the layer in line 14 if you set the output path you set in Step 5 to the ur-scape 'Data' folder.

![](Images\20190515 QGISPolyline12-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPolyline12B-02.png)
<div style="page-break-after: always;"></div>
**Step 11: Run Script**<br>Click on the 'Run Script' icon to begin.

![](Images\20190515 QGISPolyline13-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPolyline13B-02.png)
<div style="page-break-after: always;"></div>
**Step 12: Once the tool is done, you will find the file in the output folder defined.**

![](Images\20190515 QGISPolyline14-01.png)



![](Images\20190515 QGISPolyline15-01.png)
<div style="page-break-after: always;"></div>
**Step 13: Imported data**

The imported data will be visualised on ur-scape with the basic parameters and metadata defined in the earlier steps. <br>
As mentioned in Step 9, it is recommended that a higher resolution be selected for polyline data, to ensure more precise visualisation of the polylines - as illustrated below.

![](Images\20190515 QGISPolyline16-01.jpg)
Drainage capacity data imported at 100 x 100m resolution.<br>

![](Images\20190515 QGISPolyline17-01.jpg)
Drainage capacity data imported at 10 x10m resolution.

## 2.4 Vector (Point) Data

Disclaimer: As the QGIS import tool is still in development, the only information the current QGIS import tool can import for point shapefiles is the number of points. <br><br>

**Step 1: Run ur-scape**<br>On the top panel of your QGIS application, select Plugins > Python Console.
![](Images\20190515 QGIS1-01.png)<br><br>


(zoomed in to secondary Plugins menu)

![](Images\20190515 QGIS1B-02.png)
<div style="page-break-after: always;"></div>
**Step 2: Enable the Editor**<br>Click on the 'Show Editor' icon to view the Python editor interface.

![](Images\20190515 QGIS2-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS2B-02.png)
<div style="page-break-after: always;"></div>
**Step 3: Load ur-scape data import tool script**<br>Click on the 'Open Script' icon to load the ur-scape data import tool script.

![](Images\20190515 QGIS3-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS3B-02.png)<br><br>


![](Images\20190515 QGIS4-01.png)
<div style="page-break-after: always;"></div>
**Step 4: Load point vector data**<br> Add your point shapefile by dragging it from the Browser panel to the Layers panel.

![](Images\20190515 QGISPoint5-01.png)<br><br>


(zoomed in to Layers Panel)

![](Images\20190515 QGISPoint5B-02.png)
<div style="page-break-after: always;"></div>
**Step 5: Define output path (line 4 of script)**<br>Define the full folder path to where you would like to save the output files in line 4 of the Python script. 

If you will be handling multiple data sets in the same location, it is recommended to key in a path directory straight to where ur-scape and the files are saved. This will be the 'Sites' sub-folder found in the 'Data' folder. The path will look something like C:/Users/Work/Desktop/ur-scape/Data/Sites.

Important Note: On copying the folder path from your Windows Explorer, the path will contain backslashes (/) by default, which is not recognised by the script. You will be required to change the backslashes to forward slashes (\\).

![](Images\20190515 QGISPoint6-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPoint6B-02.png)
<div style="page-break-after: always;"></div>
**Step 6: Define name of layer (line 5 of script)**<br>Define the name of the data layer in line 5 of the Python script. Do note that the layer name should be in camel case (e.g. Green Cover).

![](Images\20190515 QGISPoint7-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPoint7B-02.png)
<div style="page-break-after: always;"></div>
**Step 7: Define name of field (line 6 of script)**<br>For point shapefiles, this line need not be filled.

![](Images\20190515 QGISPoint8-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPoint8B-02.png)



<div style="page-break-after: always;"></div>
**Step 8: Define resolution (line 7 of script)**<br>ur-scape data is visualised as dots or rather cells, each containing a value. This resolution refers to the size of these cells.<br>

The following resolution are available, and should be selected according to the scale of the data. <br>0 = Neighbourhood (10 x 10m)<br>1 = City (100 x 100m)<br>2 = Metropolitan (300 x 300m)<br>3 = National (30 seconds; equals ~1x1km)<br>4 = Continental (300 seconds; equals ~10x10km)<br>5 = Global (30 min; equals ~50x50km)<br>

![](Images\20190515 QGISPoint9-01.png)<br><br>

<div style="page-break-after: always;"></div>
(zoomed in to Python Editor)

![](Images\20190515 QGISPoint9B-02.png)
<div style="page-break-after: always;"></div>
**Step 9: Define metadata (lines 11-14 of script)**<br>Key in the respective metadata of the data layer. You may set the colour of the layer in line 14 if you set the output path you set in Step 5 to the ur-scape 'Data' folder.

It should be highlighted that you do not need to insert the unit name for point shapefiles, as the import tool is currently only able to import the number of points per cell - which is the default unit name.

![](Images\20190515 QGISPoint10-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPoint10B-02.png)
<div style="page-break-after: always;"></div>
**Step 10: Run Script**<br>Click on the 'Run Script' icon to begin.

![](Images\20190515 QGISPoint11-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISPoint11B-02.png)
<div style="page-break-after: always;"></div>
**Step 11: Once the tool is done, you will find the file in the output folder defined.**

![](Images\20190515 QGISPoint12-01.png)



![](Images\20190515 QGISPoint13-01.png)



<div style="page-break-after: always;"></div>
**Step 12: Imported data**

The imported data will be visualised on ur-scape with the basic parameters and metadata defined in the earlier steps. <br>

![](Images\20190515 QGISPoint14-01.jpg)

<div style="page-break-after: always;"></div>
## 2.5 Raster Data

**Step 1: Run ur-scape**<br>On the top panel of your QGIS application, select Plugins > Python Console.
![](Images\20190515 QGIS1-01.png)<br><br>


(zoomed in to secondary Plugins menu)

![](Images\20190515 QGIS1B-02.png)
<div style="page-break-after: always;"></div>
**Step 2: Enable the Editor**<br>Click on the 'Show Editor' icon to view the Python editor interface.

![](Images\20190515 QGIS2-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS2B-02.png)
<div style="page-break-after: always;"></div>
**Step 3: Load ur-scape data import tool script**<br>Click on the 'Open Script' icon to load the ur-scape data import tool script.

![](Images\20190515 QGIS3-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS3B-02.png)<br><br>


![](Images\20190515 QGIS4-01.png)
<div style="page-break-after: always;"></div>
**Step 4: Load raster data**<br> Add your raster file by dragging it from the Browser panel to the Layers panel.

![](Images\20190523 QGISRaster5-01.png)<br><br>


(zoomed in to Layers Panel)

![](Images\20190523 QGISRaster5B-02.png)
<div style="page-break-after: always;"></div>
**Step 5: Define output path (line 4 of script)**<br>Define the full folder path to where you would like to save the output files in line 4 of the Python script. 

If you will be handling multiple data sets in the same location, it is recommended to key in a path directory straight to where ur-scape and the files are saved. This will be the 'Sites' sub-folder found in the 'Data' folder. The path will look something like C:/Users/Work/Desktop/ur-scape/Data/Sites.

Important Note: On copying the folder path from your Windows Explorer, the path will contain backslashes (/) by default, which is not recognised by the script. You will be required to change the backslashes to forward slashes (\\).

![](Images\20190523 QGISRaster6-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190523 QGISRaster6B-02.png)
<div style="page-break-after: always;"></div>
**Step 6: Define name of layer (line 5 of script)**<br>Define the name of the data layer in line 5 of the Python script. Do note that the layer name should be in camel case (e.g. Green Cover).

![](Images\20190523 QGISRaster7-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190523 QGISRaster7B-02.png)
<div style="page-break-after: always;"></div>
**Step 7: Define name of field (line 6 of script)**<br>For raster data, this line need not be filled.

![](Images\20190523 QGISRaster8-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190523 QGISRaster8B-02.png)



<div style="page-break-after: always;"></div>
**Step 8: Define resolution (line 7 of script)**<br>ur-scape data is visualised as dots or rather cells, each containing a value. This resolution refers to the size of these cells.<br>

The following resolution are available, and should be selected according to the scale of the data. <br>0 = Neighbourhood (10 x 10m)<br>1 = City (100 x 100m)<br>2 = Metropolitan (300 x 300m)<br>3 = National (30 seconds; equals ~1x1km)<br>4 = Continental (300 seconds; equals ~10x10km)<br>5 = Global (30 min; equals ~50x50km)<br>

![](Images\20190523 QGISRaster9-01.png)<br><br>

<div style="page-break-after: always;"></div>
(zoomed in to Python Editor)

![](Images\20190523 QGISRaster9B-02.png)
<div style="page-break-after: always;"></div>
**Step 9: Define metadata (lines 11-14 of script)**<br>Key in the units, location and date of the data layer. You may set the colour of the layer in line 14 if you set the output path you set in Step 5 to the ur-scape 'Data' folder.

![](Images\20190523 QGISRaster10-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190523 QGISRaster10B-02.png)
<div style="page-break-after: always;"></div>
**Step 10: Run Script**<br>Click on the 'Run Script' icon to begin.

![](Images\20190523 QGISRaster11-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190523 QGISRaster11B-02.png)
<div style="page-break-after: always;"></div>
**Step 11: Once the tool is done, you will find the file in the output folder defined.**

![](Images\20190523 QGISRaster12-01.png)



![](Images\20190523 QGISRaster13-01.png)



<div style="page-break-after: always;"></div>
**Step 12: Imported data**

The imported data will be visualised on ur-scape with the basic parameters and metadata defined in the earlier steps. <br>

![](Images\20190523 QGISRaster14-01.jpg)

<div style="page-break-after: always;"></div>
# Chapter 3: Importing Data with Excel

This method of importing data will require only Microsoft Excel. Note: this method will only work for data that is based on administrative boundaries. You will need the pre-generated .csv files of the administrative boundaries.

**Step 1: Preparing the Reference .csv file**

- Open the reference .csv file (e.g. Kecamatan.csv - depending on the type of administration-based data you are looking to import).

- In Row 4, set 'CATEGORIES' to 'FALSE'.

- Under Row 4, insert a new row to be labelled 'NAMETOVALUE', and set the cell next to it to 'TRUE'. 

  ![](Images\20181112 csv import.PNG) 

**Step 2: Check and Sort Excel Data**

- Open the Excel file with data you are going to import.
- Check that the names of the administrative boundaries exactly match the names in the reference .csv file (Step 1).
- Sort the data alphabetically by name of the administrative boundary.

**Step 3: Copy Excel Data**

- Copy the sorted Excel data and paste into the pre-generated .csv file in Step 1.
- The data should be pasted next to their respective administrative boundary names.

  ![](Images\20181112 csv import2.PNG) 
<div style="page-break-after: always;"></div>
**Step 4: Save File**

+ File should be saved to the folder, and named in the following convention:

  ![](C:\Work\ur-scape\User Guide\Images\file name.JPG)

<br><br>**Step 5: Add New Data Layer to ur-scape**

+ Make sure this new data is saved in the respective sub-folder in 'Sites' folder (e.g. Global data should be saved in the 'Global' sub-folder).
+ Update the layers.csv file
   - Open the layers.csv file.
   - Check if the data layer is already listed in this .csv
      - If the data layer is not already listed, you will need to create a new entry. Insert a new row into the corresponding group and specify the 'Type', 'Name' and RGB colour code.
      - If the data layer is already listed, you can close the .csv file.
   - Save the .csv file before closing.

<div style="page-break-after: always;"></div>
# Chapter 4: Preparing Tool-Specific Data (Advanced)

## 4.1 Reachability: Road Network Data

The Reachability tool generates time-based travel maps on the fly, taking into considering multiple transport modes - Car, Motorbike and Walk. This tool requires the road network data to be imported before it can be run. Unlike the other data layers, the road network data layer is a special layer and requires additional steps to be imported.

**Step 1: Enable the Python Console on QGIS**<br>On the top panel of your QGIS application, select Plugins > Python Console.

![](Images\20190515 QGIS1-01.png)<br><br>


(zoomed in to secondary Plugins menu)

![](Images\20190515 QGIS1B-02.png)
<div style="page-break-after: always;"></div>
**Step 2: Enable the Editor**<br>Click on the 'Show Editor' icon to view the Python editor interface.

![](Images\20190515 QGIS2-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS2B-02.png)

<div style="page-break-after: always;"></div>
**Step 3: Load ur-scape data import tool script**<br>Click on the 'Open Script' icon to load the ur-scape data import tool script.

![](Images\20190515 QGIS3-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS3B-02.png)<br><br>


![](Images\20190515 QGIS4-01.png)
<div style="page-break-after: always;"></div>
**Step 4: Load road network data**<br> Add your road network shapefile by dragging it from the Browser panel to the Layers panel.

![](Images\20190515 QGISRoads5-01.png)<br><br>


(zoomed in to Layers Panel)

![](Images\20190515 QGISRoads5B-02.png)
<div style="page-break-after: always;"></div>
**Step 5: Define output path (line 4 of script)**<br>Define the full folder path to where you would like to save the output files in line 4 of the Python script. 

If you will be handling multiple data sets in the same location, it is recommended to key in a path directory straight to where ur-scape and the files are saved - i.e. the 'Data' folder (C:/ur-scape/Application/Data).

Important Note: On copying the folder path from your Windows Explorer, the path will contain backslashes (\\) by default, which is not recognised by the script. You will be required to change the backslashes to forward slashes (/).

![](Images\20190515 QGISRoads6-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISRoads6B-02.png)
<div style="page-break-after: always;"></div>
**Step 6: Define name of layer (line 5 of script)**<br>Define the name of the data layer in line 5 of the Python script. Do note that the layer name should be in camel case (e.g. Network).

![](Images\20190515 QGISRoads7-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISRoads7B-02.png)
<div style="page-break-after: always;"></div>
**Step 7: Identify field to be visualised**<br>Open the attribute table of your data layer (by right-clicking on the data layer to open a secondary menu as shown below). 

![](Images\20190515 QGISRoads8-01.png)

 Identify the field that contains the information on the category of the roads within the road network.

![](Images\20190515 QGISRoads9-01.png)



<div style="page-break-after: always;"></div>
**Step 8: Define name of field (line 6 of script)**<br> Type the name of the field you would like to visualise (identified in the above step) in line 6 of the Python script. Note that the spelling of the field must be identically spelled according to the field name in the attribute table.

 ![](Images\20190515 QGISRoads10-01.png)<br><br>

(zoomed in to Python Editor)

![](Images\20190515 QGISRoads10B-02.png)
<div style="page-break-after: always;"></div>
**Step 9: Define resolution (line 7 of script)**<br>ur-scape data is visualised as dots or rather cells, each containing a value. This resolution refers to the size of these cells.<br>

The following resolution are available, and should be selected according to the scale of the data. <br>0 = Neighbourhood (10 x 10m)<br>1 = City (100 x 100m)<br>2 = Metropolitan (300 x 300m)<br>3 = National (30 seconds; equals ~1x1km)<br>4 = Continental (300 seconds; equals ~10x10km)<br>5 = Global (30 min; equals ~50x50km)<br>

![](Images\20190515 QGISRoads11-01.png)<br><br>

<div style="page-break-after: always;"></div>
(zoomed in to Python Editor)

![](Images\20190515 QGISRoads11B-02.png)
<div style="page-break-after: always;"></div>
<div style="page-break-after: always;"></div>
**Step 10: Define metadata (lines 10-15 of script)**<br>Key in the units, location and date of the data layer. You may set the colour of the layer in line 14 if you set the output path you set in Step 5 to the ur-scape 'Data' folder.

![](Images\20190515 QGISRoads12-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISRoads12B-02.png)
<div style="page-break-after: always;"></div>
**Step 11: Set advanced parameters for Reachability (line 26 of script)**<br>
By default, 'forReachability' is set equals to 'False'. To import this road network data, set 'forReachability' = 'True'.

![](Images\20190515 QGISRoads13-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISRoads13B-02.png)
<div style="page-break-after: always;"></div>
**Step 12: Define road classification (lines 29-33 of script)**<br>The Reachability tool recognises that different road classifications within a road network have different average speeds. This is captured in the special .csv in the Reachability folder (recap from User Guide I: Operations - Chapter 3.2 Reachability), where users can edit the various average speeds across different road classifications and transport modes. 

![](Images\20190515 QGISRoads14.png)<br><br>

As various road network data have different road classifications, the import tool is set up such that the road classifications have to be defined within the hierarchy of the overall network. This is done in the 'networkMap' (lines 29-33 of the script), where the network hierarchy is divided into 4 classes numbered "1, "2", "4", "8" and "16".

This hierarchy is linked to the special Reachability .csv as shown above, where each class is linked to a hierarchy/class of road:
"1" = Other
"2" = Secondary
"4" = Primary
"8" = Highway Link
"16" = Highway

![](Images\20190515 QGISRoads15-01.png)<br><br>

(zoomed in to Python editor)

![](Images\20190515 QGISRoads15B-02.png)<br>

<div style="page-break-after: always;"></div>
Having identified the field to be imported in Step 7, you will now have to assign the road categories into this 'networkMap'. To see what road categories the data comprises, open the data's Properties (right-click on the data layer to open a secondary menu as shown below) and navigate to the 'Symbology' tab on the left.

![](Images\20190515 QGISRoads16-01.png)

![](Images\20190515 QGISRoads16B-01.png)<br>

Click on the topmost drop-down menu, and select 'Categorized' symbology type.
![](Images\20190515 QGISRoads17-01.png)<br>


Select the field that contains the information of the road categories. 
![](Images\20190515 QGISRoads18-01.png)<br>


Click the 'Classify' button to add all values. 
![](Images\20190515 QGISRoads19-01.png)<br>


Once all values are loaded, you can adjust the symbology as you like, then click 'OK'. 
![](Images\20190515 QGISRoads20-01.png)<br>

You can now see all the road categories within this network data layer. Order the road categories accordingly to each class in lines 29-33 of the script. 
![](Images\20190515 QGISRoads21-01.png)


<div style="page-break-after: always;"></div>
**Step 13: Run Script**<br>Click on the 'Run Script' icon to begin.

![](Images\20190515 QGISRoads22-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISRoads22B-02.png)
<div style="page-break-after: always;"></div>
**Step 14: Once the tool is done, you will find the file in the output folder defined.**

![](Images\20190515 QGISRoads23-01.png)

<br>

![](Images\20190515 QGISRoads24-01.png)<br>

<div style="page-break-after: always;"></div>
When successfully imported, the road network data can be used in the Reachability tool.
![](Images\20190515 QGISRoads25-01.jpg)

<div style="page-break-after: always;"></div>
## 4.2 Municipal Budget: Administrative Units Data

The Municipal Budget tool is a customised tool developed for the City of Bandung as an evaluation system to distribute the municipal budget for participatory planning. This tool uses a weighted average formula to assign scores to each administrative unit (e.g. _kelurahan_ - the 'district' unit in Indonesia) based on the values of the data layer(s) selected. 

This tools requires a reference .csv, which is a list of the administrative unit (e.g. neighbourhoods, districts, etc), for calculation in the tool and displaying information in the Information Panel on the right.

To prepare this special administrative units .csv, please follow the following steps. 

**Step 1: Enable the Python Console on QGIS**<br>On the top panel of your QGIS application, select Plugins > Python Console.

![](Images\20190515 QGIS1-01.png)<br><br>


(zoomed in to secondary Plugins menu)

![](Images\20190515 QGIS1B-02.png)
<div style="page-break-after: always;"></div>
**Step 2: Enable the Editor**<br>Click on the 'Show Editor' icon to view the Python editor interface.

![](Images\20190515 QGIS2-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS2B-02.png)

<div style="page-break-after: always;"></div>
**Step 3: Load ur-scape data import tool script**<br>Click on the 'Open Script' icon to load the ur-scape data import tool script.

![](Images\20190515 QGIS3-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGIS3B-02.png)<br><br>


![](Images\20190515 QGIS4-01.png)
<div style="page-break-after: always;"></div>
**Step 4: Load administrative unit data**<br> Add your administrative unit shapefile by dragging it from the Browser panel to the Layers panel.

![](Images\20190515 QGISMB5-01.png)<br><br>


(zoomed in to Layers Panel)

![](Images\20190515 QGISMB5B-02.png)
<div style="page-break-after: always;"></div>
**Step 5: Define output path (line 4 of script)**<br>Define the full folder path to where you would like to save the output files in line 4 of the Python script. 

If you will be handling multiple data sets in the same location, it is recommended to key in a path directory straight to where ur-scape and the files are saved - i.e. the 'Data' folder (C:/ur-scape/Application/Data).

Important Note: On copying the folder path from your Windows Explorer, the path will contain backslashes (\\) by default, which is not recognised by the script. You will be required to change the backslashes to forward slashes (/).

![](Images\20190515 QGISMB6-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISMB6B-02.png)
<div style="page-break-after: always;"></div>
**Step 6: Define name of layer (line 5 of script)**<br>Define the name of the data layer in line 5 of the Python script. For this special .csv, the name should be the name of the site. Do note that the layer name should be in camel case (e.g. Bandung).

![](Images\20190515 QGISMB7-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISMB7B-02.png)
<div style="page-break-after: always;"></div>
**Step 7: Identify administrative unit field**<br>Open the attribute table of your data layer (by right-clicking on the data layer to open a secondary menu as shown below). 

![](Images\20190515 QGISMB8-01.png)

 Identify the field containing information on administrative unit that you would like to be used in the Municipal Budget tool.

![](Images\20190515 QGISMB9-01.png)



<div style="page-break-after: always;"></div>
**Step 8: Define name of field (line 6 of script)**<br> Type the name of the field you would like to visualise (identified in the above step) in line 6 of the Python script. Note that the spelling of the field must be identically spelled according to the field name in the attribute table.

 ![](Images\20190515 QGISMB10-01.png)<br><br>

(zoomed in to Python Editor)

![](Images\20190515 QGISMB10B-02.png)
<div style="page-break-after: always;"></div>
**Step 9: Define resolution (line 7 of script)**<br>ur-scape data is visualised as dots or rather cells, each containing a value. This resolution refers to the size of these cells.<br>

The following resolution are available, and should be selected according to the scale of the data. <br>0 = Neighbourhood (10 x 10m)<br>1 = City (100 x 100m)<br>2 = Metropolitan (300 x 300m)<br>3 = National (30 seconds; equals ~1x1km)<br>4 = Continental (300 seconds; equals ~10x10km)<br>5 = Global (30 min; equals ~50x50km)<br>

![](Images\20190515 QGISMB11-01.png)<br><br>

<div style="page-break-after: always;"></div>
(zoomed in to Python Editor)

![](Images\20190515 QGISMB11B-02.png)
<div style="page-break-after: always;"></div>
<div style="page-break-after: always;"></div>
**Step 10: Define metadata (lines 10-15 of script)**<br>Key in the units, location and date of the data layer. You may set the colour of the layer in line 14 if you set the output path you set in Step 5 to the ur-scape 'Data' folder.

![](Images\20190515 QGISMB12-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISMB12B-02.png)
<div style="page-break-after: always;"></div>
**Step 11: Set advanced parameters for Municipal Budget (lines 20 & 25 script)**<br>On line 20, the 'extentAsCanvas' is set to "False" by default. When exporting this special Municipal Budget reference .csv, you have to set it to "True" instead. This function sets the extent of the imported layer to be canvas view. 

Important note: This function has to always be set to "True" when importing <u>any</u> data layer to be used in the Municipal Budget tool, as the extent of the data layer has to be exactly the same. 

Likewise, change the 'forMunicipalBudget' (line 25) from "False" to "True". This setting is only applicable for importing the special administrative units .csv. 

![](Images\20190515 QGISMB13-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISMB13B-02.png)
<div style="page-break-after: always;"></div>
**Step 13: Run Script**<br>Click on the 'Run Script' icon to begin.

![](Images\20190515 QGISMB14-01.png)<br><br>


(zoomed in to Python Editor)

![](Images\20190515 QGISMB14B-02.png)
<div style="page-break-after: always;"></div>
**Step 12: Once the tool is done, you will find the file in the output folder defined.**

![](Images\20190515 QGISMB15-01.png)



![](Images\20190515 QGISMB16-01.png)<br>
<div style="page-break-after: always;"></div>
When successfully imported, the list of selected administrative units will appear in the Information Panel on the right and proportional percentages calculated accordingly.
![](Images\20190515 QGISMB17-01.jpg)




<div style="page-break-after: always;"></div>
# Chapter 5: Tutorials & Useful Links

### How to Import Data to ur-scape on QGIS

[https://vimeo.com/283034116/7b29287ac3](https://vimeo.com/283034116/7b29287ac3) (English)<br>
[https://vimeo.com/299823943/3c5eae580c](https://vimeo.com/299823943/3c5eae580c) (Bahasa Indonesia)

![](Images\20181109 QGIS Tutorial.png)

This tutorial shows how to use QGIS and our ur-scape import Python script to prepare vector and raster data for ur-scape.



<div style="page-break-after: always;"></div>
# Chapter 6: Contact

### Contact Information

Prof Dr Stephen Cairns (Principal Investigator): [cairns@arch.ethz.ch](mailto:cairns@arch.ethz.ch)<br>
Dr Devisari Tunas (Research Scenario Coordinator): [devisari.tunas@arch.ethz.ch](mailto:devisari.tunas@arch.ethz.ch)<br>
Rosita Samsudin (Project Coordinator): [samsudin@arch.ethz.ch](mailto:samsudin@arch.ethz.ch)<br>
Michael Joos (Senior Software Engineer): [joos@arch.ethz.ch](mailto:joos@arch.ethz.ch)<br>Muhammad Salihin Bin Zaol-kefli (Software Engineer): [mzaolkefli@ethz.ch](mailto:mzaolkefli@ethz.ch)<br><br>
Website: [http://ur.systems](http://ur.systems)

<br>

### Future Cities Laboratory (FCL)

Future Cities Laboratory<br>
Singapore-ETH Centre<br>
1 Create Way<br>
CREATE Tower, #06-01<br>
Singapore 138602<br><br>
Website: [http://www.fcl.ethz.ch](http://www.fcl.ethz.ch/)<br>
Email: [info@fcl.ethz.ch](mailto:info@fcl.ethz.ch)<br>
T:+65 6601 6076

