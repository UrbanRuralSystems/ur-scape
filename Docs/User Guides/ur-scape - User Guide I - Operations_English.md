# **ur-scape** 

## User Guide I: Operations

<br>Current ur-scape Version: v0.9.88 Beta<br>
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
[**Chapter 1: Setting Up**](#Chapter 1: Setting Up)<br>
&nbsp;&nbsp;&nbsp;[1.1 Folder System](#1.1 Folder System)<br>
&nbsp;&nbsp;&nbsp;[1.2 Mapbox Set Up](#1.2 Mapbox Set Up)<br>
&nbsp;&nbsp;&nbsp;[1.3 Run ur-scape](#1.3 Run ur-scape)<br>
[**Chapter 2: Overview of ur-scape**](#Chapter 2: Overview of ur-scape)<br>
&nbsp;&nbsp;&nbsp;[2.1 Main Interface](#2.1 Main Interface)<br>
&nbsp;&nbsp;&nbsp;[2.2 Basic Functions](#2.2 Basic Functions)<br>
&nbsp;&nbsp;&nbsp;[2.3 Organising Layers & Groups](#2.2 Organising Layers & Groups)<br>
&nbsp;&nbsp;&nbsp;[2.4 Advanced Customisations](#1.4 Advanced Customisations) <br>
<br>
[**Chapter 3: ur-scape Tools**](#Chapter 3: ur-scape Tools)<br>
&nbsp;&nbsp;&nbsp;[3.1 Contours](#3.1 Contours)<br>
&nbsp;&nbsp;&nbsp;[3.2 Reachability](#3.2 Reachability)<br>
&nbsp;&nbsp;&nbsp;[3.3 Planning](#3.3 Planning)<br>
&nbsp;&nbsp;&nbsp;[3.4 Timeline](#3.4 Timeline)<br>
&nbsp;&nbsp;&nbsp;[3.5 Municipal Budget](#3.5 Municipal Budget)<br>
&nbsp;&nbsp;&nbsp;[3.6 Word Cloud](#3.6 Word Cloud)<br>
&nbsp;&nbsp;&nbsp;[3.7 Export](#3.7 Export)<br><br>
[**Chapter 4: Tutorials & Useful Links**](#Chapter 4: Tutorials & Useful Links)<br><br>
[**Chapter 5: Contact**](#Chapter 5: Contact)<br><br>

<br>
<br>
<br>

<div style="page-break-after: always;"></div>
# Chapter 1: Setting Up

## 1.1 Folder System

**Step 1: Extract Files**

- Extract all files from the zipped folder.
- The zipped folder contains several items, of which users will only interact with two items:
  - **'Data' file folder**: This folder comprises key files/folders that the user should be familiar with to modify/upload data onto ur-scape.
  - **urscape.exe**: This is the executable file that users can double-click to run ur-scape.
- The other files/ folders support the backend operations of ur-scape - any modifications to them may affect the operation of ur-scape. User should <u>disregard</u> these items.<br>

  ![](Images\20190523 ur-scape1-02.png)<br><br>

<div style="page-break-after: always;"></div>
**Step 2: Understanding and Setting Up the 'Data' File Folder**

Open the 'Data' file folder. This folder contains the following items that must <u>all</u> always be present.

- Can be edited/ customised (see Chapter 1.3 for list of customisations)

  - **Municipal Budget [file folder]** : Contains the reference administrative unit .csv per site for Municipal Budget tool to be used.
  - **Reachability [file folder]** : Contains the special reference .csv need for the Reachability tool, listing the different average speeds across different road classifications for different transport modes.
  - **Sites [file folder]** : Contains all data layers (in .csv and .bin format) organised by sites - to be visualised in ur-scape. Tip: If you do not want a data layer to be visualised, add an underscore at the start of the name (E.g. Global -> _Global).
  - **citations [.csv]** : List of the citation for each data layer. This will contain all citations for default data, and should be updated when new data layers are added.
  - **layers [.csv]** : List of the groups names and data layers - with their display colours in RGB code. 
  - **MapboxToken [.txt]** : This document loads all the Mapbox backgrounds.

- Do not edit

  - **Backgrounds [file folder]** : This folder is created on the first run of ur-scape with Internet connection. It stores all the tiles for various map backgrounds from Mapbox that have been loaded with Internet connection.
  - **Languages [file folder]** : Currently work in progress - for future functionality in ur-scape to support localisation.
  - **WordCloud [file folder]** : Contains data for WordCloud tool. 
  - **backgrounds [.csv]** : This file contains links to the respective Mapbox backgrounds. 
  - **typologies [.csv]** : This file contains all the different typologies within the Planning Tool.

<div style="page-break-after: always;"></div>
## 1.2 Mapbox Set Up

**Step 1: Create Mapbox Account** 

- Go to  [https://www.mapbox.com](https://www.mapbox.com). Click on 'Sign In'.

  ![](Images\20181108 Mapbox1.jpg)<br><br>

  <div style="page-break-after: always;"></div>

- Click on 'Sign up for Mapbox'.

  ![](Images\20181108 Mapbox2.png)
<div style="page-break-after: always;"></div>
**Step 2: Get Mapbox Token** 

- Once signed up, you will be directed to the Account Dashboard page.

  ![](Images\20181108 Mapbox3.png)<br><br>

- Scroll down the page to the 'Access tokens' section. Click on the 'Copy' icon next to the link.

  ![](Images\20181108 Mapbox5.png)

<div style="page-break-after: always;"></div>
**Step 3: Link Your Mapbox Token to ur-scape** 

- In the 'Data' file folder, open the 'MapboxToken.txt' file. Replace the existing link with the link you have just copied. Save and close the Notepad file.

  ![](Images\20181108 Mapbox6.png)
  
<div style="page-break-after: always;"></div>
**For Advanced Users: Create Your Own Background Style** 

Step 1: Navigate to the 'Studio' page.

![](Images\20181108 Mapbox7.png)<br><br>

<div style="page-break-after: always;"></div>
Step 2: Click on 'More options' and explore creating your own Background style.

![](Images\20181108 Mapbox8.png)

<div style="page-break-after: always;"></div>
Step 3: Once your new Background style is created, click on the 'Menu' button. 

![](Images\20181108 Mapbox9.png)<br><br>

Step 4: Click on the 'Copy' icon to copy the link of this new Background Style.

![](Images\20181108 Mapbox10.png)

<div style="page-break-after: always;"></div>
Step 5: In the 'Data' folder, open the 'backgrounds.csv' file. Create a new row by keying in the name of the new background style, and paste the link from Step 4. Save and close the Notepad file. Your new background style is now available on ur-scape.

![](Images\20181108 Mapbox11.png)<br><br>

<div style="page-break-after: always;"></div>
## 1.3 Run ur-scape 

+ Double-click on the 'urscape.exe' file to run ur-scape.

+ A window will pop-up. Under the 'Graphics' tab, you may customise the i) screen resolution, ii) graphics quality, and iii) whether you would like ur-scape to be windowed.

+ For the best visual experience, we recommend selecting the maximum screen resolution available, 'Fantastic' graphics quality, and a not windowed view of ur-scape.

<br>
<br>

<div style="page-break-after: always;"></div>
# Chapter 2: Overview of ur-scape

## 2.1 Main Interface

![](Images\20190523 ur-scape3.png)


1. **Main Frame**: This is the frame where the background and data are visualised. <br><br>

2. **Data Layers**: This leftmost panel contains all data layers that have been organised into groups.<br><br>

3. **Menu**: This is a secondary menu where users can Customise Layers (covered in Chapter 2.3: Customising Layers & Groups), Reset Layers, and also select additional settings.<br><br>

4. **Site Browser**: Users can see this list of sites with site-specific data here - and also click on the site names to pan to the area.<br><br>

5. **Tools**: Users can select from the tools available on ur-scape (see Chapter 3).<br><br>

6. **Information Panel**: This rightmost panel displays information such as the output of analyses run by tools used.<br><br>

7. **Backgrounds**: Users have the choice of five different background maps - None, Basic Background, Basic with Labels, Background with Traffic, and Satellite Imagery.<br><br>

8. **Zoom Controls**: Users can click on the ' + ' button to zoom in, and ' - ' to zoom out.<br><br>

9. **Compass**: The compass only appears when the map view is not in its default state (facing true North and flat). Users can click on the compass to reset the map view to default.<br><br>

10. **Quick Inspect**: By enabling the 'Quick Inspect', a secondary information box will appear next to the cursor. This will contain values that the cursor is hovering over at any point of time.<br><br>

11. **Scale Bar**: The scale bar represents the scale of the map. <br><br>

12. **Title Bar**: This contains the three buttons to minimise, resize or close ur-scape.<br><br>

<div style="page-break-after: always;"></div>
## 2.2 Basic Functions

**Switching on Data Layers**

+ To switch on a data layer, click on the button with the name of the data layer on it.
+ The button will be highlighted in its respective colour and the data will be displayed on the main frame. 

**Filtering Uncategorised Data**
+ To filter uncategorised data, click on the icon next to the data layer button. 
+ A box will appear displaying a slider.
+ You may manually drag the triangles on both ends to filter the data you want displayed. 
+ Alternatively, you may click on the number and manually key in specific values.
+ Once you're done filtering the data, click on the icon button next to the data layer to hide the box.

**Filtering Categorised Data**
+ To filter uncategorised data, click on the icon button next to the data layer.
+ A box will appear displaying a list of categories with a checkbox each. Categories that are displayed will have checkboxes with fill colour.
+ To have a quick preview of the data in each category of a layer, you can use your cursor to hover over each category. The data for that category will be highlighted on the main frame.
+ To not display a category, click on the checkbox next to the data category. The fill colour of the checkbox will be turned off and replaced by a cross.
+ To display only one category, double click on the checkbox next to it. This will automatically switch off the display of all other categories, leaving only this category displayed.
+ To display all categories, and you have:
  + Only one category displayed, double-click on that category. All categories will be displayed.
  + Multiple categories displayed, double-click on a category <u>twice</u>. All categories will be displayed.
+ Once you're done filtering the data, click on the icon button next to the data layer to hide the box.

**Activating A Tool** 
+ Click on the tool to activate.
+ To use more than one tool at a time, click the 'Add' tab to select an additional tool.

**Changing Map Background**

+ Click on a map background style to update display.
+ Map backgrounds are loaded on the fly from Mapbox and will require Internet connection.
+ Tip: to view areas with different backgrounds ur-scape without Internet connection, pre-load the tiles for these areas prior with  Internet connection. The tiles will be saved in the 'Tiles' folder.

**Re-orienting Map**
+ To re-orient map to face true north, click on the north arrow on the compass.

<div style="page-break-after: always;"></div>
**View/ Pan Map in 3D**

+ To view/ pan map in 3D, 'Right-click, Hold + Drag'.
+ To exit 3D view, click on the north arrow on the compass.

<br>
<br>
<br>

<div style="page-break-after: always;"></div>
## 2.3 Organising Layers & Groups

A key feature of ur-scape is the multiple data layers. Before delving into how these data layers and their groups can be organised, users should understand how layers and groups work.<br>

For each site in ur-scape, it contains its own unique set of data layers that can be overlaid for visualisation and analyses (as illustrated below). Essentially every individual data set being imported into ur-scape will be an individual data layer. 
![](Images\20190524 ur-scape16- 1.png)
The order of these layers is organised across all sites, using the 'Organise Layers' function. Any changes made to a layer (i.e. spelling, display colour, order) will be made to all instances of that layer across <u>all</u> sites.<br>
![](Images\20190524 ur-scape17- 1.png)<br>
![](Images\20190524 ur-scape18- 1.png)



<div style="page-break-after: always;"></div>
To customise data layers and groups, click on the hamburger icon to reveal the secondary menu.<br>

![](Images\20190523 ur-scape4-01.png)

<br> 'Organise Layers' is a secondary interface that allows users to: edit the name of data layers/groups, change the grouping of data layers, re-order of data layers/groups, and also change the display colour of data layers.<br>

![](Images\20190523 ur-scape5-01.png)

<div style="page-break-after: always;"></div>
**Edit Data Layer Name**<br>
To edit name of a data layer, simply click on the first field in Layer Properties panel on the right. A caret will appear, and you can type the new name of the layer.<br>

![](Images\20190524 ur-scape6-02.png)<br><br>**Edit Group Name**<br>
To edit name of a group, first click on the group you would like to re-name. The right panel will switch to the Group Properties panel. Here you simply need to click on the Name field and you may rename the group.<br>

![](Images\20190524 ur-scape7-02.png)
<div style="page-break-after: always;"></div>
**Change Data Layer Grouping**<br>
To change the group within which a data layer is displayed, you can either assign the data layer to: (i) an existing group or (ii) a new group.<br>

<u>(i) Assign Data Layer to an Existing Group</u><br>
To assign the data layer to another existing group, click on the dropdown arrow and select the group you would like the data layer to be saved under instead.<br><br>

 ![](Images\20190524 ur-scape8-02.png)<br>
<div style="page-break-after: always;"></div>
<u>(ii) Assign Data Layer to a New Group</u><br>
To assign the data layer to a new group, click on the ' + ' arrow. This will reveal a pop-up window where you can enter the name of the new group.<br><br>

 ![](Images\20190524 ur-scape9-02.png)<br><br>
 ![](Images\20190524 ur-scape10-02.png)

<div style="page-break-after: always;"></div>
**Change Data Layer Display Colour**<br>
To change the display colour, click on the colour swatch. A colour palette will appear for you to select from.<br>

![](Images\20190524 ur-scape11-2.png)<br><br>
You may also use the  'Random Picker' button to random pick a colour in the event you cannot decide. <br>

![](Images\20190524 ur-scape12-2.png)
<div style="page-break-after: always;"></div>
**Re-order Data Layer/ Groups**<br>
To re-order data layer/ groups, first select the layer/ group you would like to change its order. Next, use the 'Up' and 'Down' arrows to easily change its order.

![](Images\20190524 ur-scape13-2.png)<br><br>

**Delete Layer**<br>
To delete a layer, first select the layer, then click the 'Trash Bin' button at the bottom right corner of the window.<br>

![](Images\20190524 ur-scape14-2.png)<br><br>
<div style="page-break-after: always;"></div>
**Create New Group**<br>
To create a new group, click the ' + ' button.<br>

![](Images\20190524 ur-scape15-2.png)<br><br>

<div style="page-break-after: always;"></div>
## 2.4 Advanced Customisations 

Within the 'Data' file folder, the following customisations can be made. 

+ **Data Layer Colouring Type**
  + Go to  the 'Sites' folder and open the .csv file of the data layer.
  + In Row 4, you will see an input called 'Colouring' set to 'Multi'. This is the default colouring type.
  + You may customise the colouring, by keying in any of the following four coloring options:
    + Multi (default): Assigns colours to data layer values across multiple spectrums.
    + ReverseMulti: Assigns colours to data layer values across multiple spectrums. 
    + Single: Assigns colours to data layer values from darkest to lightest.
    + ReverseSingle: Assigns colours to data layer values from lightest to darkest.
  + Save the .csv file before closing.

+ **Customise Mobility Speeds (Reachability Tool)**
  + Go to the 'Reachability' folder and you will see a list of .csv files.
  + Locate the .csv file that you would like to customise the mobility speeds. 
  + The .csv file will contain all the modes available and their respective speeds for different road types.
  + To change the speed, simply key in the new speed and save the .csv file before closing.

+ **Add New Mobility Modes (Reachability Tool)**
  + Go to the 'Reachability' folder and you will see a list of .csv files.
  + Locate the .csv file that you would like to a new mobility mode. 
  + The .csv file will contain all the modes available and their respective speeds for different road types.
  + To add a new mobility mode, simply key in the new mode with the respective information on speeds for different road types.
  + Save the .csv file before closing.

<br>
<br>
<br>

<div style="page-break-after: always;"></div>
# Chapter 3: ur-scape Tools

In this v0.9.82 Beta release, ur-scape comes with seven tools - Contours, Reachability, Planning, Timeline, Municipal Budget, Word Cloud and Export. This chapter will explain how to use each tool in detail.<br><br>


## 3.1 Contours

The Contours tool demarcates areas where defined data of one or more layers intersect spatially. This is useful in overlaying multiple data layers to define multiple parameters as criteria for site selection.

**Step 1: Select Data Layers**

- Select data layers to be used by clicking on them.

**Step 2: Activate Contours Tool**

- Click on the 'Contours' tool to activate it.
- This will generate the resultant contour areas which demarcate where data from your selected data layers spatially intersect.

**Step 3: Filter Data**

- You may filter the data for each layer as you wish.
- The contour areas will be updated as you do so.

**Step 4: Store a Copy of Contours (optional)**

+ Once you are satisfied with the contour areas generated, click on the 'Store a copy of Contours' to save a snapshot. Your snapshot will be saved.
+ Note: You may save up to 3 snapshots, after which you will be prompted to delete a snapshot to allow for more snapshots to be taken. 

**Additional Contours Settings**

- Show contours: Toggles the visibility of the contours on/off.

- Show filtered data: Toggles the visibility of the filtered data in the background on/off.

- Exclude cells without data: Toggles the exclusion of cells without data on/off.

- Lock contours: Freezes the contours.

<br>
<br>

<div style="page-break-after: always;"></div>
## 3.2 Reachability

The Reachability tool generates time-based travel maps on the fly, taking into considering multiple transport modes - Car, Motorbike and Walk. 

Note: The Reachability tool can only be used where Road Network data is available.



**Step 1: Activate the Tool**

- Click on the 'Reachability' tool to activate it.
- Once the road network data layer is loaded, the tool is ready to be used. 
- Note: Starting this tool may require some waiting time depending on the volume of the road network data.

**Step 2: Select Single or Multiple Points**

- Click on either the 'Single Point' or 'Multiple Points' button.
- If you would like to only view the reachability of one point, select the former;
- Or select the latter for reachability of multiple points.

**Step 3: Select the Mobility Mode**

- The modes available by default are Car, Motorbike and Walk - with the Car being selected by default.
- You may choose to change the mobility mode.

**Step 4: Setting Total Travel Time**

- To set the total travel time, drag across the slider.

**Step 5: Begin Pinning Point(s)**

- Begin clicking on the map to pin the point(s).
- You should immediately see the reachability of the point being generated in blue.
- Tip: You may want to switch on a data layer that could guide the placement of these points (For example, jurisdiction data layer, healthcare amenities data layer).

**Step 5: Filter Reachability Time**

- On running the 'Reachability' tool, a data layer named Reachability will be generated and added to the data layers panel on the left.
- As the data is uncategorised, you may filter the reachability data displayed using the slider. 

<div style="page-break-after: always;"></div>
## 3.3 Planning

The Planning tool allows for the exploration of hypothetical planning scenarios by using simulated typologies to demonstrate the outcome of trade-off decisions. The outcomes of scenarios are clearly visualised across the following quantitative planning outcomes: population, crop production, permeable surface, gross floor area, plot ratio and water. 



**Step 1: Activate the Tool**

- Zoom to a case study site.
- Click on the 'Planning' tool to activate it.
- Note: At least one data set must be selected for the tool to work.

**Step 2: Select Drawing Tool**

- You may select either the 'Draw cell by cell' or 'Draw by shape' tool to draw.
  - 'Draw cell by cell' tool: You may select individual cells to draw typologies.
  - 'Draw by shape' tool: Use a polygon or free-form lasso to draw typologies over multiple cells.
- To select the tool, click on the icon representing the tool. 

**Step 3: Select Typology**

- There are 10 typologies available: _Ruko_ (Shop House), _Rumah Tinggal_ (Residential House), _Apartemen Tingkat Rendah_ (Low-Rise Apartment), _Apartemen Tingkat Tinggi_ (High-Rise Apartment), _Rumah Tambah_ (Expandable House), _Ruang Terbuka_ (Public Space), _Panen Bernilai Rendah_ (Low Value Crop), _Panen Bernilai Tinggi_ (High Value Crop), Pedestrian Bioswale and _Tanah Kosong_ (Bare Land).
- Select the typology to be drawn by clicking on the icon above it.

**Step 4: Begin Drawing**

- Prior to drawing, ensure that you have zoomed in sufficiently to allow for more accurate drawing. You can also 'right-click + drag' to pan to a 3D view. 
- If you selected 'Draw cell by cell' tool, there are two ways you can draw your selected typologies:
  - Click to select each cell individually to draw the typology in that cell. 
  - 'Click, hold + drag' over multiple cells consecutively.
- If you selected the 'Draw by shape' tool, there are also two ways you can draw your selected typologies:
  - Polygon lasso: Click multiple points to add vertices and sides of the polygon. The sides of the polygon will be drawn straight.
  - Free-form lasso: 'Click + Drag' to draw a free-form lasso boundary.
- As you draw, you will see the following:
  - Typology being built on the cell(s). For example, if the 'High Rise' typology is selected, a simple high-rise tower block will be visualised over the selected cell.

  - A linked bar chart over the drawn typologies. By default, this bar chart illustrates the overall change in two planning parameters - population and crop production.

  - A summary of the outcome of the scenarios displayed in the Output panel on the right. This quantifies the changes across the five planning parameters: population, crops production, green cover, gross floor area and cost summary.

- 'Erase Typology' tool
  - If you make a mistake and would like to remove the typology drawn over a particular cell or cells, click on the 'Erase typology' tool.
  - Once this tool is selected, click over the cell that you would like to erase.

**Step 5: Explore on Your Own** 

- Draw multiple areas of different typologies: Repeat Steps 2-4, but change the typology.
- Customise the bar charts: By default, the two planning parameters shows are Population and Crops production. This can be changed or more parameters can be added by marking the checkbox in the output panel.

<div style="page-break-after: always;"></div>
## 3.4 Timeline


The Timeline tool displays one or more data layers over time, and allows users to observe trends and patterns of a data set/ between data sets over time. 



**Step 1: Switch On Data Layer with Data Over Time**

- Data layer must have data over time for the given view frame - or pan to an area with data across time.
- You may select one or more data layers.

**Step 2: Activate the Tool**

- Click on the 'Timeline' tool to activate it.
- The timeline(s) will appear with the corresponding date of the data set.
- To navigate across time, 'Click + Drag' across the timeline. The main frame will display the data for the corresponding data.

<div style="page-break-after: always;"></div>
## 3.5 Municipal Budget

The Municipal Budget tool is a customised tool developed for the City of Bandung as an evaluation system to distribute the municipal budget for participatory planning. This tool uses a weighted average formula to assign scores to each _kelurahan_ (municipality) based on the values of the data layer(s) selected. 



**Step 1: Activate the Tool**

+ Zoom to the City of Bandung using the Site Browser.
+ Click on the 'Municipal Budget' tool to activate it.

**Step 2: Select Data Layers**

- Select data layers to be used by clicking on them.
- By selecting data layers, this will:
  - Create vertical sliders on the toolbar panel with the name of the data layer beneath it;
  - Generate an list of the _kelurahans_ with a corresponding percentage next to each _kelurahan_ in the Information Panel. This percentage represents the respective percentage proportion each _kelurahan_ should receive out of the total municipal budget (of 100%).

**Step 3: Adjust Weighting of Data Layers**

+ The vertical sliders will be used to adjust the weight of the importance of each data layer.
+ By sliding down, this decreases the importance of the data layer in the overall municipal budget consideration; and vice versa.
+ You will notice that as you adjust the weight of the data layers, the percentage proportion of budget each _kelurahan_ will receive changes dynamically.

**Step 4: Transect Locator**

+ A transect can be seen to cut across Bandung by default.
+ This transect displays the distribution of one or more data layers, and the final weighted average (in white) across the transect displayed.
+ By using the Transect Locator, you can adjust the transect to see different segments of Bandung.

**Step 5: Explore Output**

+ As mentioned in Step 2, a list of _kelurahans_ are generated in the Information Panel with the corresponding percentage of budget proportion next to each _kelurahan_. By hovering the cursor over each _kelurahan_ name on the list, this will generate an outline on the map to indicate the location of the _kelurahan_.
+ Likewise, by hovering the cursor over the map on the main frame, the name of the _kelurahan_ will be lit up in the Information Panel. 

<div style="page-break-after: always;"></div>
## 3.6 Word Cloud

The Word Cloud tool is a customised tool developed for the City of Bandung to collate and visualise the qualitative data collected from E-Musrenbang surveys. 



**Step 1: Activate the Tool**

+ Zoom to the City of Bandung using the Site Browser.
+ Click on the 'Word Cloud' tool to activate it.
+ Click on the 'Group Items' icon

**Step 2: Click on the E-Musrembang icon**

+ You may view the survey data in either Bahasa Indonesia, or English.

<br>
<br>

<div style="page-break-after: always;"></div>
## 3.7 Export

The Export tool is developed to allow users to export graphic and text files from ur-scape. The user can export screen grabs from ur-scape across three pre-set resolutions and output values.



**Step 1: Activate the Tool**

+ Click on the 'Export' tool to activate it.

**Step 2: Select Export Format Options**

- Four export format options are available:
  1. Full Interface: A .PNG file that captures the entire application interface, including the main frame display (with all selected layers and background displayed as a single graphic) will be exported.
  2. Background & Layers Combined: A .PNG file that captures only the main frame, including the display on the main frame as the user sees it (with all selected layers and background displayed as a single graphic) will be exported.
  3. Background & Layers Separate: A .PNG file that captures only the main frame, including the display on the main frame but with all selected layers and background as separate files, will be exported.
  4. Output Report: A .CSV file will be exported - containing the values of the layers selected/filtered, and from any tools used.


**Step 3: Select Additional Export Settings**

+ On selecting any of the export format options, a secondary menu will appear.
  + If you selected Export Format Options 1, 2 or 3 (refer to Step 2), you have the option of selecting the Image Size - Small, Medium or Large.
  + If you selected Export Format Option 4 (refer to Step 2), you currently have the option of selecting the file format as .CSV file. The .PDF export format is currently work in progress and will be subsequently released once ready. 

**Step 4: Run Tool**

+ Once you have selected the export format(s), click on the 'Export' icon at the end of the toolbar.
+ A new folder named 'Export' will be created in the main folder which contains the ur-scape application - and the exported files will be saved here.
+ Note: When exporting is in progress, the screen may momentarily switch to the frame that it is exporting. The screen will revert to the original interface once the export is done.

<div style="page-break-after: always;"></div>
# Chapter 4: Tutorials & Useful Links

### Contours Tool

Link: [https://vimeo.com/297675141/88bd3d94d3](https://vimeo.com/297675141/88bd3d94d3)

![](Images\20181029 ur-scape bandung contours.jpg)

This video shows how ur-scape's Contours Tool can be used. In this scenario in Bandung, high-density _kelurahans_ with poor access to water, and higher poverty levels, are identified using the tool. 

<br>

<div style="page-break-after: always;"></div>
### Reachability Tool

Link: [https://vimeo.com/297676994/421d2ef8da](https://vimeo.com/297676994/421d2ef8da)

![](Images\20181029 ur-scape reachability.jpg)

This video shows how ur-scape's Reachability Tool can be used. In this scenario, the accessibility of Bandung's healthcare amenities from residential areas are assessed across different mobility modes. 

<br>
<br>
<br>

<div style="page-break-after: always;"></div>
# Chapter 5: Contact

### Contact Information

Prof Dr Stephen Cairns (Principal Investigator): [cairns@arch.ethz.ch](mailto:cairns@arch.ethz.ch)<br>
Dr Devisari Tunas (Research Scenario Coordinator): [devisari.tunas@arch.ethz.ch](mailto:devisari.tunas@arch.ethz.ch)<br>
Rosita Samsudin (Project Coordinator): [samsudin@arch.ethz.ch](mailto:samsudin@arch.ethz.ch)<br>
Michael Joos (Senior Software Engineer): [joos@arch.ethz.ch](mailto:joos@arch.ethz.ch)<br>
Muhammad Salihin Bin Zaol-kefli (Software Engineer): [mzaolkefli@ethz.ch](mailto:mzaolkefli@ethz.ch)<br><br>
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

