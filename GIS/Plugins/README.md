# ur-scape Plugins

### Setup
Follow these steps to have the plugins appear in QGIS:

* Copy one of the plugins in the ***Plugins*** folder.
* In QGIS, locate your current profile folder by going to ***Settings ‣ User Profiles ‣ Open Active Profile Folder***.
* In the profile folder, go to the ***python ‣ plugins*** subfolder and paste the copied plugin folder.
* Close QGIS and open it again. Go to ***Plugins ‣ Manage and Install Plugins...*** and enable the copied plugin in the ***Installed*** tab.

### Update

When you want to update the plugin to a different version or just make small changes in the plugin's code, you will need to:

* Go to ***Plugins ‣ Manage and Install Plugins...*** and enable ***Plugin Reloader*** in the ***Installed*** tab.
* Click on the ***Plugin Reloader*** icon after making changes to the code before running the plugin

### Python Script
The ***qgis2urscape.py*** script can be used separately from the plugin and be run in QGIS's Python Console.

### Requisites
* Compatible with QGIS v3.10 or newer

