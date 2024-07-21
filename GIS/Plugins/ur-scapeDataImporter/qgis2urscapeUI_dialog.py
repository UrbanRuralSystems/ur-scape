# -*- coding: utf-8 -*-
"""
/***************************************************************************
 Qgis2UrscapeUIDialog
                                 A QGIS plugin
 This plugin provides a GUI for the ur-scape Data Importer plugin.
                             -------------------
        begin                : 2020-06-05
        git sha              : $Format:%H$
        copyright            : (C) 2024 Singapore ETH Centre, Future Cities Laboratory
        author:              : Muhammad Salihin Bin Zaol-kefli
        maintainer:          : Joshua Vargas
        email                : joshua.vargas@sec.ethz.ch
 ***************************************************************************/

/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/
"""

from PyQt5 import QtCore, QtWidgets, uic
from PyQt5.QtGui import QColor, QPalette
from PyQt5.QtWidgets import (QLabel
                            ,QLineEdit
                            ,QVBoxLayout
                            ,QSpacerItem
                            ,QSizePolicy
                            ,QCheckBox
                            ,QApplication
                            ,QFileDialog
                            ,QWidget
                            )
from qgis.core import (QgsProject
                      ,QgsSettings
                      ,QgsVectorLayer
                      ,QgsMessageOutput
                      ,QgsTask
                      ,QgsApplication
                      ,QgsMessageLog
                      ,Qgis
                      )
from qgis.utils import iface
from qgis.PyQt.QtCore import QStandardPaths
from qgis.PyQt.QtWidgets import QGridLayout
from qgis.gui import QgsMessageBar
import sys, os, re, datetime, importlib, traceback
from . import qgis2urscape as q2u  # Assuming qgis2urscape.py is in the same directory
importlib.reload(q2u)

uiDirectory = os.path.dirname(os.path.abspath(__file__))
debuggingMode = True

class Logger(object):
    
    TAG = 'ur-scape'
    
    def __init__(self, textUIElement):
        self.orig_stdout = sys.stdout
        self.log = textUIElement
        self.logFile = None
        sys.stdout = self
        
    def attach(self):
        QgsApplication.messageLog().messageReceived.connect(self.onLogMessageReceived)
        
    def detach(self):
        try:
            QgsApplication.messageLog().messageReceived.disconnect(self.onLogMessageReceived)
        except TypeError:
            pass

    def openLogFile(self, filePath):
        try:
            logFilePath = os.path.join(filePath, "urscapeDataImporter.log.txt")
            self.logFile = open(logFilePath, "w")
            if self.logFile is not None:
                QgsMessageLog.logMessage("Created log file: " + logFilePath, level=Qgis.Info)
        except: pass
    
    def onLogMessageReceived(self, message, tag, level):
        if tag != self.TAG:
            if tag is not None:
                message = ">> " + tag + ": " + message
            message += "\n"
            self.writeToLogFile(message)
            #self.writeToLogTab(message)

    def write(self, message):
        #if message != "\n":
        #    QgsMessageLog.logMessage(message, self.TAG, Qgis.Info)
        self.writeToLogFile(message)
        self.writeToLogTab(message)
        # self.orig_stdout.write(message)

    def writeToLogTab(self, message):
        if self.log is not None:
            try:
                self.log.insertPlainText(message)
            except:
                exceptionMsg = "Exception while trying to add message to Log tab:\n"
                exceptionMsg += traceback.format_exc() + "\n"
                QgsMessageLog.logMessage(exceptionMsg, self.TAG, Qgis.Critical)

    def writeToLogFile(self, message):
        if self.logFile is not None:
            try:
                self.logFile.write(message)
                self.logFile.flush()
            except:
                exceptionMsg = "Exception while trying to write message to log file:\n"
                exceptionMsg += traceback.format_exc() + "\n"
                QgsMessageLog.logMessage(exceptionMsg, self.TAG, Qgis.Critical)
                self.logFile.close()
                self.logFile = None

    def flush(self):
        #this flush method is needed for python 3 compatibility.
        #this handles the flush command by doing nothing.
        #you might want to specify some extra behavior here.
        pass

    def close(self):
        sys.stdout = self.orig_stdout
        self.orig_stdout = None
        self.log = None
        try:
            self.detach()
        except:
            pass

        if self.logFile is not None:
            self.logFile.flush()
            self.logFile.close()
            self.logFile = None

    def clear(self):
        if self.log is not None:
            try:
                self.log.clear()
            except:
                exceptionMsg = "Exception while attempting to clear Log tab:\n"
                exceptionMsg += traceback.format_exc() + "\n"
                QgsMessageLog.logMessage(exceptionMsg, self.TAG, Qgis.Critical)


class Qgis2UrscapeUIDialog(QtWidgets.QDialog):
    def __init__(self, parent=None):
        # Constructor
        super(Qgis2UrscapeUIDialog, self).__init__(parent)
        uic.loadUi(os.path.join(uiDirectory, 'qgis2urscapeUI_dialog_base.ui'), self) # Load the .ui file
        
        # Initialize logger
        self.startLogger()
        
        # Disable buttons when pressing the ENTER key
        self.btnRefresh.setAutoDefault(False)
        self.btnPrevSettings.setAutoDefault(False)
        self.btnRun.setAutoDefault(False)
        self.btnClose.setAutoDefault(False)
        self.btnOpenAttributeTable.setAutoDefault(False)
        self.btnBrowse.setAutoDefault(False)
        self.btnCancel.setAutoDefault(False)
        
        # Init variables
        self.initVars()
        
        # Init UI
        self.initUI()
        
        # Signals
        self.initSignals()
        
        # Show the GUI
        self.show()

    #
    # Init Functions
    #

    def initMsgBar(self):
        self.msgBar = QgsMessageBar()
        self.layout().addWidget(self.msgBar, 0, 0, 1, 1)

    def initInputLayer(self):
        self.selectedInputLayer = None
        self.layerIDs = []

    def initShapefileField(self):
        self.selectedShapefileField = None
        self.shapefileFields = []

    def initQGISInput(self):
        self.initInputLayer()
        self.initShapefileField()

    def initOutputType(self):
        self.outputTypes = {
            "Data Layer":0,\
            "Reachability":1,\
            "Municipal Budget":2
        }
        self.selectedOutputType = None

    def initOutputPath(self):
        s = QgsSettings()
        storedOutputPath = s.value("outputPath")
        #print(storedOutputPath)
        self.txtOutputPath.setText(uiDirectory if not storedOutputPath else storedOutputPath)

    def initLocation(self):
        self.isPrevWorld = self.isWorld = False

    def initResolution(self):
        self.resD = [
            ("Neighbourhood - 10 x 10 metres", "0"),
            ("City - 100 x 100 metres", "1"),
            ("Metropolitan - 300 x 300 metres", "2"),
            ("Custom", "6")
        ]
        self.resABC = [
            ("National - 0.5 min (0.0083333 degrees)", "3"),
            ("Continental - 5 min (0.08333333 degrees)", "4"),
            ("Global - 25 min (0.416 degrees)", "5")
        ]
        self.resolutions = self.resD
        # self.prevSelectedWorldRes = self.prevSelectedOthersRes = self.selectedRes = None
        self.prevSelectedOthersRes = self.selectedRes = None
        self.txtCustomRes.setText("")
        self.wgtCustomRes.hide()

    def initUnits(self):
        self.units = ["Please select an option", "Custom"]
        for i in range(len(q2u.unitsList)): 
            self.units.insert(i + 1, q2u.unitsList[i])
        # print(self.units)
        self.selectedUnits = None
        self.txtCustomUnits.setText("")
        self.chkRelative.setChecked(False)
        self.wgtCustomUnits.hide()

    def initDate(self):
        self.txtYYYY.setText("")
        self.txtMM.setText("")
        self.txtDD.setText("")
        self.date = ""

    def initManadatoryParams(self):
        self.initOutputType()
        self.initOutputPath()
        self.initLocation()
        self.initResolution()
        self.initUnits()
        self.initDate()

    def initLayerColor(self):
        self.colors = [
            ("Red", 0),
            ("Orange", 1),
            ("Yellow", 2),
            ("Light Green", 3),
            ("Dark Green", 4),
            ("Cyan", 5),
            ("Light Blue", 6),
            ("Dark Blue", 7),
            ("Purple", 8),
            ("Pink", 9),
            ("Custom", 10)
        ]
        # Init wgtLayerColor widget
        self.wgtLayerColor.setAutoFillBackground(True)
        self.palette = self.wgtLayerColor.palette()
        self.qColor = QColor()
        self.wgtCustomLayerColor.hide()

    def initOptionalParams(self):
        self.initLayerColor()
        self.defaultLayerGrp = "Exported from QGIS"

    def initMaxPatchSize(self):
        self.maxPatchSizesD = [1, 2, 5]
        self.maxPatchSizesABC = [10, 50, 360]
        self.maxPatchSizes = self.maxPatchSizesD

    def initResamplingMthd(self):
        self.resamplingMthds = [
            ("Nearest Neighbor", 0),
            ("Bilinear", 1),
            ("Average", 5),
            ("Maximum", 7),
            ("Minimum", 8),
            ("Summary", 9)
        ]
        self.selectedResamplingMthd = None

    def initNoDatas(self):
        self.noDatas = [
            ("n/a")
        ]
        self.txtNoDatas = []
        self.txtNoData.setText(self.noDatas[0])
        self.chkNoDataList.setChecked(False)
        
        self.noDataAggregations = [
            ("Aggregating one or more NoData cells results in NoData", False),
            ("Ignore NoData cells during aggregation", True)
        ]
        self.convertNoData = False
        self.noDataValue = None

    def initNetworkMap(self):
        self.roadClasses = ["Default", "Custom"]
        
        self.defaultNetworkMapVals = list(q2u.networkMap.values())
        self.customNetworkMapVals = list(q2u.networkMap.values())
        self.networkMap = dict(q2u.networkMap)
        self.networkMapVals = []

        # for key, value in self.networkMap.items():
        #     print("Key: " + str(key) + ", Value: " + str(value))
    
    def initCheckboxes(self):
        self.forMunicipalBudget = False
        self.forReachability = False

    def initAdvancedParams(self):
        self.defaultRasterBand = "1"
        self.initMaxPatchSize()
        self.initResamplingMthd()
        self.initNoDatas()
        self.initNetworkMap()
        self.initCheckboxes()

    def initVars(self):
        # QGIS Input
        self.initQGISInput()
        
        # Mandatory Parameters
        self.initManadatoryParams()
        
        # Optional Parameters
        self.initOptionalParams()
        
        # Metadata
        self.chkMandatory.setChecked(False)
        
        # Advanced Parameters
        self.initAdvancedParams()

        # Others
        self.q2uExportTask = None
        self.qgsSettings = QgsSettings()

    def initCmbBoxUI(self, list, cmbBox, eventFunc, index=0, placeholderText=False, activatedFunc=None):
        for item in list:
            cmbBox.addItem(item[0] if type(item) is tuple else item)

        if placeholderText:
            cmbBox.view().setRowHidden(0, True)
            cmbBox.activated.connect(activatedFunc)
            
        cmbBox.wheelEvent = lambda event: None

        eventFunc(index)

    def initNoDataUI(self):
        # Add initial QLineEdits in list of noDatas
        self.addToNoDatasList(self.txtNoData.text())
        self.addToNoDatasList("")

        self.updateNoDatasUI(False)

        # Add noData aggregations
        self.initCmbBoxUI(self.noDataAggregations, self.cmbNoDataCalculation, self.onNoDataCalculationChanged)

    def initNetworkMapUI(self):
        self.addToNetworkMapLabels()
        self.addToNetworkMapValues()

        self.initCmbBoxUI(self.roadClasses, self.cmbRoadClasses, self.onRoadClassesChanged)
        
    def initUI(self):
        self.initMsgBar()

        self.updateInputLayersUI()
        self.cmbInputLayer.wheelEvent = lambda event: None
        
        self.onShapefileFieldChanged(0)
        self.cmbShapefileField.wheelEvent = lambda event: None

        # Add output types
        self.updateOutputType(isinstance(iface.activeLayer(), QgsVectorLayer))
        self.cmbOutputType.wheelEvent = lambda event: None
        
        # Add resolutions
        self.initCmbBoxUI(self.resolutions, self.cmbRes, self.onResolutionChanged, 1)
        # Add units
        self.initCmbBoxUI(self.units, self.cmbUnits, self.onUnitsChanged, 0, True, self.onUnitsActivate)
        # Add colors
        self.initCmbBoxUI(self.colors, self.cmbLayerColor, self.onLayerColorChanged)
        # Add resampling methods
        self.initCmbBoxUI(self.resamplingMthds, self.cmbResamplingMthd, self.onResamplingMthdChanged)

        self.initNoDataUI()
        self.initNetworkMapUI()
        
        self.setGUITabOrder()

    def initSignals(self):
        self.btnRefresh.clicked.connect(self.onRefresh)
        self.cmbShapefileField.currentIndexChanged.connect(self.onShapefileFieldChanged)
        self.btnOpenAttributeTable.clicked.connect(self.onOpenAttributeTable)
        self.cmbOutputType.currentIndexChanged.connect(self.onOutputTypeChanged)
        self.btnBrowse.clicked.connect(self.onBrowseFile)
        self.txtLocation.textChanged.connect(self.onLocationChanged)
        self.cmbRes.currentIndexChanged.connect(self.onResolutionChanged)
        self.cmbUnits.currentIndexChanged.connect(self.onUnitsChanged)
        self.cmbLayerColor.currentIndexChanged.connect(self.onLayerColorChanged)
        self.hsCustomColor.valueChanged.connect(self.onCustomLayerColorSliderChanged)
        self.chkMaxPatchSizeOverride.stateChanged.connect(self.onMaxPatchSizeOverrideChanged)
        self.cmbResamplingMthd.currentIndexChanged.connect(self.onResamplingMthdChanged)
        self.cmbNoDataCalculation.currentIndexChanged.connect(self.onNoDataCalculationChanged)
        self.chkNoDataList.stateChanged.connect(self.onChkNoDataListChanged)
        self.cmbRoadClasses.currentIndexChanged.connect(self.onRoadClassesChanged)
        self.btnCancel.clicked.connect(self.onCancel)
        self.btnPrevSettings.clicked.connect(self.onPrevSettings)
        self.btnRun.clicked.connect(self.onRun)
        self.btnClose.clicked.connect(self.onClose)
    
    #
    # UI Signal Functions
    #
    
    def onRefresh(self):
        try: self.cmbInputLayer.currentIndexChanged.disconnect(self.onLayerChanged)
        except Exception: pass
        
        # Clear cmbInputLayer and repopulate again with updated list of layers
        # in QGIS project
        self.cmbInputLayer.clear()
        self.layerIDs = []
        for key, value in QgsProject.instance().mapLayers().items():
            if not value.isTemporary():
                self.layerIDs.append(key)
                self.cmbInputLayer.addItem(value.name())
                
        # Still no available layers in QGIS after Refresh
        if not self.layerIDs:
            return

        # Use previous selectedInputLayer if still available
        # Otherwise, use current active layer
        index = -1
        if self.selectedInputLayer is not None:
            if self.selectedInputLayer in QgsProject.instance().mapLayers().values():
                index = self.cmbInputLayer.findText(self.selectedInputLayer.name(), QtCore.Qt.MatchFixedString)
            else:
                activeLayer = iface.activeLayer()
                if activeLayer is not None:
                    index = self.cmbInputLayer.findText(activeLayer.name(), QtCore.Qt.MatchFixedString)
                    self.updateShapefileField(isinstance(iface.activeLayer(), QgsVectorLayer))
        else:
            index = 0

        if index >= 0:
            self.cmbInputLayer.setCurrentIndex(index)
            self.updateSelectedInputLayer(index)
            iface.setActiveLayer(self.selectedInputLayer)
        self.cmbInputLayer.currentIndexChanged.connect(self.onLayerChanged)
        
    def onLayerChanged(self, index):
        self.updateSelectedInputLayer(index)
        iface.setActiveLayer(self.selectedInputLayer)
        self.updateShapefileField(isinstance(iface.activeLayer(), QgsVectorLayer))
        self.updateOutputType(isinstance(iface.activeLayer(), QgsVectorLayer))
        self.onOutputTypeChanged(0)

    def onShapefileFieldChanged(self, index):
        self.cmbShapefileField.setCurrentIndex(index)
        if self.shapefileFields:
            self.selectedShapefileField = self.shapefileFields[index]

    def onOpenAttributeTable(self):
        iface.showAttributeTable(iface.activeLayer())

    def onOutputTypeChanged(self, index):
        self.cmbOutputType.setCurrentIndex(index)
        item = self.cmbOutputType.itemText(index)
        # print("Selected item: " + item)
        try:
            self.selectedOutputType = self.outputTypes[item]
        except: pass

        if self.selectedOutputType == 0:    # Data Layer
            if isinstance(iface.activeLayer(), QgsVectorLayer):
                self.dataLayerVectorUIElems()
            else:
                self.dataLayerRasterUIElems()
        elif self.selectedOutputType == 1:    # Reachability
            self.reachabilityVectorUIElems()
        elif self.selectedOutputType == 2:    # Municipal Budget
            self.municipalBudgetVectorUIElems()

        # print("SelectedOutputType: " + str(self.selectedOutputType))
        
        self.wgtChkboxes.adjustSize()
    
    def onBrowseFile(self):
        self.txtOutputPath.setText(QFileDialog.getExistingDirectory())
        self.setOutputPath(self.txtOutputPath.text())

    def onLocationChanged(self, text):
        self.updateCmbRes()
        self.isPrevWorld = self.isWorld
    
    def onResolutionChanged(self, index):
        self.cmbRes.setCurrentIndex(index)
        if self.resolutions:
            lastItemIndex = len(self.resolutions) - 1
            self.selectedRes = self.resolutions[index][1]

            if index == lastItemIndex and self.resolutions[lastItemIndex][0] == "Custom":
                self.wgtCustomRes.show()
                self.txtMaxPatchSize.setText(str(self.maxPatchSizesABC[-1]))
            else:
                self.wgtCustomRes.hide()
                # Update txtMaxPatchSize value according to Resolution selected
                self.txtMaxPatchSize.setText(str(self.maxPatchSizes[index]))

            if self.isPrevWorld == self.isWorld:
                if not self.isWorld:
                    self.prevSelectedOthersRes = index
                # else:
                #     self.prevSelectedWorldRes = index
        #print("SelectedRes: " + self.selectedRes + "-" + self.resolutions[index][0])

    def onUnitsChanged(self, index):
        self.cmbUnits.setCurrentIndex(index)
        if index == len(self.units) - 1:
            self.wgtCustomUnits.show()
            self.selectedUnits = "Custom"
        elif index != 0:    # index 0 is for placeholder text
            self.wgtCustomUnits.hide()
            self.selectedUnits = self.units[index]
            # print("SelectedUnits: " + self.selectedUnits)

    def onUnitsActivate(self, index=-1, enable=False):
        if index:
            self.cmbUnits.model().item(0).setEnabled(enable)

    def changeLayerColor(self, index):
        if self.cmbLayerColor.currentIndex() == index:
            self.onLayerColorChanged(index)
        else:
            self.cmbLayerColor.setCurrentIndex(index)
    
    def onLayerColorChanged(self, index):
        self.cmbLayerColor.setCurrentIndex(index)
        if index == len(self.colors) - 1:
            self.wgtCustomLayerColor.show()
            self.updateLayerCustomColor(self.hsCustomColor.value())
        else:
            self.wgtCustomLayerColor.hide()
            self.updateLayerPredefinedColor(self.colors[index][1])
    
    def onCustomLayerColorSliderChanged(self, value):
        self.updateLayerCustomColor(value)

    def onMaxPatchSizeOverrideChanged(self, int):
        self.txtMaxPatchSize.setEnabled(self.chkMaxPatchSizeOverride.isChecked())
        
    def onResamplingMthdChanged(self, index):
        self.cmbResamplingMthd.setCurrentIndex(index)
        self.selectedResamplingMthd = self.resamplingMthds[index][1]
        
        if self.selectedResamplingMthd == 5 or self.selectedResamplingMthd == 9:
            self.lblNoDataCalculation.show()
            self.cmbNoDataCalculation.show()
        else:
            self.lblNoDataCalculation.hide()
            self.cmbNoDataCalculation.hide()
        #print("SelectedResamplingMthd: " + str(self.selectedResamplingMthd) + "-" + str(self.resamplingMthds[index][0]))

    def onNoDataCalculationChanged(self, index):
        self.convertNoData = self.noDataAggregations[index][1]
        #print("ConvertNoData: " + str(self.convertNoData))

    def onNoDataTextChanged(self, text):
        currTextLen = len(text)
        
        if currTextLen - 1 == 0:
            # Add new QLineEdit
            #print("Adding new noData to list...")
            self.addToNoDatasList("")
            
        if not text:
            # Remove current QLineEdit
            #print("Removing current noData from list...")
            for txtNoData in self.txtNoDatas:
                if len(self.txtNoDatas) > 2 and not txtNoData.text():
                    txtNoData.textChanged.disconnect(self.onNoDataTextChanged)
                    txtNoData.deleteLater()
                    self.txtNoDatas.remove(txtNoData)
                    break
                    
            #for txtNoData in self.txtNoDatas:
                #print(txtNoData.text())
                
    def onChkNoDataListChanged(self, int):
        self.updateNoDatasUI(self.chkNoDataList.isChecked())
        
    def onRoadClassesChanged(self, index):
        self.cmbRoadClasses.setCurrentIndex(index)
        self.updateNetworkMapVals(index)

    def onTaskProgressChanged(self, progress):
        # print("Task Progress: " + str(self.q2uExportTask.progress()))
        self.progressBar.setValue(self.q2uExportTask.progress())
        
    def onPrevSettings(self):
        self.loadPrevSettings()
        pass

    def onCancel(self):
        if self.q2uExportTask is not None:
            self.q2uExportTask.cancel()
            #print("Task was cancelled")

    def onRun(self):
        # Reset any warning messages
        self.clearMsgBar();

        self.setNetworkMap()    # Required before checking for empty fields
        result = self.checkMandatoryFields()
        
        # Prompt error pop-up if there is error, otherwise run exporter
        if result[0]:
            self.warningMsgBar(result[1])
        else:
            # Set vars needed
            self.setOutputPath(self.txtOutputPath.text())
            if self.selectedUnits == "Custom":
                self.setCustomUnits()
            self.setGroup()
            self.setUseBand()
            self.setNoDataValue()

            self.btnCancel.setEnabled(True)
            self.btnRun.setEnabled(False)
            
            self.logger.clear()
            self.logger.attach()
            
            # Pass vars values from UI to imported module,
            # Run Exporter in the background
            #print("Running new export instance...")
            self.setq2uParams()
            self.q2uExportTask = QgsTask.fromFunction("q2u Export", q2u.Exporter, on_finished=self.completeQ2UExport)
            self.q2uExportTask.progressChanged.connect(self.onTaskProgressChanged)
            QgsApplication.taskManager().addTask(self.q2uExportTask)

            self.tabWidget.setCurrentIndex(1)   # Change to Log tab
            
            self.saveCurrSettings()

    def onClose(self):
        self.prepareForClose()
        self.done(0)
        
    def closeEvent(self, event):
        self.prepareForClose()
        super(Qgis2UrscapeUIDialog, self).closeEvent(event)
        
    def prepareForClose(self):
        self.setNetworkMap()
        self.saveCurrSettings()
        
        self.onCancel()
        if self.q2uExportTask is not None and self.q2uExportTask.isCanceled():
            self.resetTask()
        self.stopLogger()

    def saveCurrSettings(self):
    # Save settings for:
        # Input Layer
        self.qgsSettings.setValue("inputLayer", self.cmbInputLayer.currentIndex())
        # Field
        self.qgsSettings.setValue("field", self.cmbShapefileField.currentIndex())
        # Output Path
        self.qgsSettings.setValue("outputPath", self.txtOutputPath.text())
        # Layer Name
        self.qgsSettings.setValue("layerName", self.txtLayerName.text())
        # Location
        self.qgsSettings.setValue("location", self.txtLocation.text())
        # Resolution + Custom
        self.qgsSettings.setValue("resolution", self.cmbRes.currentIndex())
        self.qgsSettings.setValue("customResolution", self.txtCustomRes.text())
        # Units + Custom + Relative
        self.qgsSettings.setValue("units", self.cmbUnits.currentIndex())
        self.qgsSettings.setValue("customUnits", self.txtCustomUnits.text())
        self.qgsSettings.setValue("relative", self.chkRelative.isChecked())
        # Date: YYYY, MM, DD
        self.qgsSettings.setValue("dateYear", self.txtYYYY.text())
        self.qgsSettings.setValue("dateMonth", self.txtMM.text())
        self.qgsSettings.setValue("dateDay", self.txtDD.text())
        # Layer Color
        self.qgsSettings.setValue("layerColor", self.cmbLayerColor.currentIndex())
        self.qgsSettings.setValue("customLayerColor", self.hsCustomColor.value())
        # Layer Group
        self.qgsSettings.setValue("layerGroup", self.txtLayerGroup.text())
        # Source
        self.qgsSettings.setValue("source", self.txtSource.text())
        # Citation + Mandatory
        self.qgsSettings.setValue("citation", self.txtCitation.text())
        self.qgsSettings.setValue("mandatory", self.chkMandatory.isChecked())
        # URL Link
        self.qgsSettings.setValue("urlLink", self.txtURLLink.text())
        # Raster Band
        self.qgsSettings.setValue("rasterBand", self.txtRasterBand.text())
        # Max Patch Size + Override
        self.qgsSettings.setValue("maxPatchSize", self.txtMaxPatchSize.text())
        self.qgsSettings.setValue("override", self.chkMaxPatchSizeOverride.isChecked())
        # Resampling Method
        self.qgsSettings.setValue("resamplingMethod", self.cmbResamplingMthd.currentIndex())
        # No Data Calculation
        self.qgsSettings.setValue("noDataCalculation", self.cmbNoDataCalculation.currentIndex())
        # No Data Value + List
        self.qgsSettings.setValue("list", self.chkNoDataList.isChecked())
        self.qgsSettings.setValue("noDataValue", self.txtNoData.text())
        self.qgsSettings.setValue("noDataValues", list(txtNoData.text() for txtNoData in self.txtNoDatas))
        # Network Maps
        self.qgsSettings.setValue("roadClasses", self.cmbRoadClasses.currentIndex())
        self.qgsSettings.setValue("customNetworkMapVals", self.customNetworkMapVals)
        # All the checkboxes
        self.qgsSettings.setValue("clipToNoDataOuterArea", self.chkClipToNoDataOuterArea.isChecked())
        self.qgsSettings.setValue("keepSameResAsInput", self.chkKeepSameResAsInput.isChecked())
        self.qgsSettings.setValue("preventHigherRes", self.chkPreventHigherRes.isChecked())
        self.qgsSettings.setValue("fixGeometry", self.chkFixGeometry.isChecked())
        self.qgsSettings.setValue("clipToQGISCanvas", self.chkClipToQGISCanvas.isChecked())
        # Output Type
        self.qgsSettings.setValue("outputType", self.cmbOutputType.currentIndex())
    
    def loadPrevSettings(self):
    # Load settings for:
        # Input Layer
        self.loadPrevCmbIndex(self.cmbInputLayer, self.qgsSettings.value("inputLayer"), self.cmbInputLayer.currentIndex())
        # Field
        self.loadPrevCmbIndex(self.cmbShapefileField, self.qgsSettings.value("field"), self.cmbShapefileField.currentIndex())
        # Output Path
        self.loadPrevTxt(self.txtOutputPath, self.qgsSettings.value("outputPath"), uiDirectory)
        # Layer Name
        self.loadPrevTxt(self.txtLayerName, self.qgsSettings.value("layerName"), self.txtLayerName.text())
        # Location
        self.loadPrevTxt(self.txtLocation, self.qgsSettings.value("location"), self.txtLocation.text())
        # Resolution + Custom
        self.loadPrevCmbIndex(self.cmbRes, self.qgsSettings.value("resolution"), self.cmbRes.currentIndex())
        self.loadPrevTxt(self.txtCustomRes, self.qgsSettings.value("customResolution"), self.txtCustomRes.text())
        # Units + Custom + Relative
        self.loadPrevCmbIndex(self.cmbUnits, self.qgsSettings.value("units"), self.cmbUnits.currentIndex())
        self.loadPrevTxt(self.txtCustomUnits, self.qgsSettings.value("customUnits"), self.txtCustomUnits.text())
        self.loadPrevCheckbox(self.chkRelative, self.qgsSettings.value("relative"), self.chkRelative.isChecked())
        # Date: YYYY, MM, DD
        self.loadPrevTxt(self.txtYYYY, self.qgsSettings.value("dateYear"), self.txtYYYY.text())
        self.loadPrevTxt(self.txtMM, self.qgsSettings.value("dateMonth"), self.txtMM.text())
        self.loadPrevTxt(self.txtDD, self.qgsSettings.value("dateDay"), self.txtDD.text())
        # Layer Color
        self.loadPrevCmbIndex(self.cmbLayerColor, self.qgsSettings.value("layerColor"), self.cmbLayerColor.currentIndex())
        storedCustomLayerColor = self.qgsSettings.value("customLayerColor")
        self.hsCustomColor.setValue(self.hsCustomColor.value() if not storedCustomLayerColor else int(storedCustomLayerColor))
        # Layer Group
        self.loadPrevTxt(self.txtLayerGroup, self.qgsSettings.value("layerGroup"), self.txtLayerGroup.text())
        # Source
        self.loadPrevTxt(self.txtSource, self.qgsSettings.value("source"), self.txtSource.text())
        # Citation + Mandatory
        self.loadPrevTxt(self.txtCitation, self.qgsSettings.value("citation"), self.txtCitation.text())
        self.loadPrevCheckbox(self.chkMandatory, self.qgsSettings.value("mandatory"), self.chkMandatory.isChecked())
        # URL Link
        self.loadPrevTxt(self.txtURLLink, self.qgsSettings.value("urlLink"), self.txtURLLink.text())
        # Raster Band
        self.loadPrevTxt(self.txtRasterBand, self.qgsSettings.value("rasterBand"), self.txtRasterBand.text())
        # Max Patch Size + Override
        self.loadPrevTxt(self.txtMaxPatchSize, self.qgsSettings.value("maxPatchSize"), self.txtMaxPatchSize.text())
        self.loadPrevCheckbox(self.chkMaxPatchSizeOverride, self.qgsSettings.value("override"), self.chkMaxPatchSizeOverride.isChecked())
        # Resampling Method
        self.loadPrevCmbIndex(self.cmbResamplingMthd, self.qgsSettings.value("resamplingMethod"), self.cmbResamplingMthd.currentIndex())
        # No Data Calculation
        self.loadPrevCmbIndex(self.cmbNoDataCalculation, self.qgsSettings.value("noDataCalculation"), self.cmbNoDataCalculation.currentIndex())
        # No Data Value + List
        self.loadPrevNoDataValuesList(self.qgsSettings.value("list"), self.qgsSettings.value("noDataValue"), self.qgsSettings.value("noDataValues"))
        # Network Maps
        self.loadPrevCmbIndex(self.cmbRoadClasses, self.qgsSettings.value("roadClasses"), self.cmbRoadClasses.currentIndex())
        self.loadPrevNetworkMap(self.qgsSettings.value("customNetworkMapVals"), self.defaultNetworkMapVals)
        # All the checkboxes
        self.loadPrevCheckbox(self.chkClipToNoDataOuterArea, self.qgsSettings.value("clipToNoDataOuterArea"), self.chkClipToNoDataOuterArea.isChecked())
        self.loadPrevCheckbox(self.chkKeepSameResAsInput, self.qgsSettings.value("keepSameResAsInput"), self.chkKeepSameResAsInput.isChecked())
        self.loadPrevCheckbox(self.chkPreventHigherRes, self.qgsSettings.value("preventHigherRes"), self.chkPreventHigherRes.isChecked())
        self.loadPrevCheckbox(self.chkFixGeometry, self.qgsSettings.value("fixGeometry"), self.chkFixGeometry.isChecked())
        self.loadPrevCheckbox(self.chkClipToQGISCanvas, self.qgsSettings.value("clipToQGISCanvas"), self.chkClipToQGISCanvas.isChecked())
        # Output Type
        self.loadPrevCmbIndex(self.cmbOutputType, self.qgsSettings.value("outputType"), self.cmbOutputType.currentIndex())
    
    #
    # Update Functions
    #
    
    def updateSelectedInputLayer(self, layerIndex):
        layers = QgsProject.instance().mapLayers()
        if layers and self.layerIDs:
            if 0 <= layerIndex < len(self.layerIDs):
                self.selectedInputLayer = layers[self.layerIDs[layerIndex]]
    
    def updateInputLayersUI(self):
        try: self.cmbInputLayer.currentIndexChanged.disconnect(self.onLayerChanged)
        except Exception: pass
        
        self.layerIDs = []
        for key, value in QgsProject.instance().mapLayers().items():
            if not value.isTemporary():
                self.layerIDs.append(key)
                self.cmbInputLayer.addItem(value.name())
        activeLayer = iface.activeLayer()
        if activeLayer is not None:
            index = self.cmbInputLayer.findText(activeLayer.name(), QtCore.Qt.MatchFixedString)
            if index >= 0:
                self.cmbInputLayer.setCurrentIndex(index)
                self.onLayerChanged(index)
        self.cmbInputLayer.currentIndexChanged.connect(self.onLayerChanged)
        
    def updateShapefileField(self, enabled):
        self.cmbShapefileField.clear()
        self.shapefileFields = []
        if enabled:
            activeLayer = iface.activeLayer()
            fields = activeLayer.fields()
            
            fieldIndex = -1;
            for field in fields:
                fieldIndex += 1

                # Obtain categories in each field
                categories = []
                categoriesCount = 0
                featureIndex = 0
                for feature in activeLayer.getFeatures():
                    featureIndex += 1
                    if featureIndex > 200: break
                    #cleanRecord = self.cleanCategoryString(feature[fieldIndex])
                    cleanRecord = feature[fieldIndex]
                    if not cleanRecord in categories:
                        categories.append(cleanRecord)
                        categoriesCount += 1
                        if categoriesCount == 4:
                            break
                        
                categories.sort()

                # Format categories to be appended to field name in dropdown
                categoriesList = ""
                if not categories:
                    categoriesList = " (This field is empty)"
                else:
                    categoriesList = " ("
                    limit = totalCategories = len(categories)
                    if limit > 3:
                        limit = 3
                    for i in range(limit):
                        categoriesList += str(categories[i])
                        if i != limit - 1:
                            categoriesList += ", "
                    categoriesList += ", ...)" if totalCategories > 3 else ")"
                    
                self.cmbShapefileField.addItem(field.name() + categoriesList)
                self.shapefileFields.append(field.name())
                
            # Show Field UI elements
            self.lblShapefileField.show()
            self.wgtShapefileField.show()
        else:
            # Reset and hide Field UI elements
            self.cmbShapefileField.clear()
            self.lblShapefileField.hide()
            self.wgtShapefileField.hide()

        if self.shapefileFields:
            self.selectedShapefileField = self.shapefileFields[0]

    def updateOutputType(self, enabled):
        # Reachability should only be available for vector type lines
        self.cmbOutputType.clear()
        if enabled and self.selectedInputLayer is not None:
            for outputType in self.outputTypes.keys():
                if self.selectedInputLayer.geometryType() != 1 and outputType == "Reachability":
                    continue
                self.cmbOutputType.addItem(outputType)
        # Have the first outputType added to the comboBox
        else:
            self.cmbOutputType.addItem(list(self.outputTypes.keys())[0])
        
        self.cmbOutputType.setEnabled(enabled)
            
    def updateCmbRes(self):
        self.isWorld = self.txtLocation.text().lower() == ("World").lower()
        self.resolutions = self.resABC if self.isWorld else self.resD
        self.maxPatchSizes = self.maxPatchSizesABC if self.isWorld else self.maxPatchSizesD
    
        # Clear cmbRes only if changing Location from Others to World or vice versa
        if self.isPrevWorld != self.isWorld:
            self.cmbRes.clear()
            for item in self.resolutions:
                self.cmbRes.addItem(item[0])

        if not self.isWorld:
            self.onResolutionChanged(self.prevSelectedOthersRes if self.prevSelectedOthersRes is not None else 1)
        else:
            # self.onResolutionChanged(self.prevSelectedWorldRes if self.prevSelectedWorldRes is not None else 2)
            self.onResolutionChanged(2)

    def updateLayerPredefinedColor(self, index):
        self.qColor.setHsvF(index/(len(self.colors) - 1),1,1)
        self.updateLayerColor()

    def updateLayerCustomColor(self, hue, saturation=1, value=1):
        self.qColor.setHsvF(hue/360, saturation, value)
        self.updateLayerColor()
        
    def updateLayerColor(self):
        self.palette.setColor(QPalette.Background, self.qColor)
        self.wgtLayerColor.setPalette(self.palette)

    def updateNoDatas(self):
        self.noDatas.clear()
        for txtNoData in self.txtNoDatas:
            text = txtNoData.text()
            if text:
                self.noDatas.append(text)
        #print(self.noDatas)
        
    def updateNoDatasUI(self, toggle):
        if toggle:
            self.txtNoData.hide()
            
            # Update 1st element in list of noDatas
            # before making the list visible
            self.txtNoDatas[0].setText(self.txtNoData.text())
            self.wgtListNoDatas.show()
        else:
            # Update txtNoData to be of same value as
            # 1st element in list of noDatas
            self.txtNoData.setText(self.txtNoDatas[0].text())
            self.txtNoData.show()
            self.wgtListNoDatas.hide()
            
    def updateNetworkMapVals(self, index):
        networkMapValues = list(self.networkMap.values())
        networkMapValuesLen = len(networkMapValues)
        
        # Update values and toggle QLineEdit enabled depending on Default or Custom option
        for i in range(networkMapValuesLen):
            if index == 0:
                self.customNetworkMapVals[i] = list(filter(None, [txt.strip() for txt in self.networkMapVals[i].text().split(",")]))
            
            self.networkMapVals[i].setText(','.join(self.defaultNetworkMapVals[i]) if index == 0 else ','.join(self.customNetworkMapVals[i]))
            self.networkMapVals[i].setEnabled(False if index == 0 else True)

    #
    # Miscellaneous Functions
    #
    
    def addToNoDatasList(self, text):
        hasEmpty = False
        # Check if there already exists 1 empty QLineEdit
        for txtNoData in self.txtNoDatas:
            if not txtNoData.text():
                hasEmpty = True
                break
        
        # Add new QLineEdit to self.vblNoDatas
        if not hasEmpty:
            qLineEdit = QLineEdit(text)
            qLineEdit.textChanged.connect(self.onNoDataTextChanged)
            self.txtNoDatas.append(qLineEdit)
            self.vblNoDatas.addWidget(qLineEdit)

    def resetNoDatas(self):
        for txtNoData in self.txtNoDatas:
            txtNoData.setText("")

        self.noDatas = [
            ("n/a")
        ]
        self.txtNoData.setText(self.noDatas[0])
        self.chkNoDataList.setChecked(False)

    def addToNetworkMapLabels(self):
        networkMapKeys = list(self.networkMap.keys())
        for i in range(len(networkMapKeys)):
            qLabel = QLabel(networkMapKeys[i])
            qLabel.setAlignment(QtCore.Qt.AlignRight | QtCore.Qt.AlignVCenter)
            qLabel.setIndent(3)
            self.glNetworkMap.addWidget(qLabel, i, 0, 1, 1)
            self.glNetworkMap.setHorizontalSpacing(20)

    def addToNetworkMapValues(self):
        networkMapValues = list(self.networkMap.values())
        networkMapValuesLen = len(networkMapValues)
        
        for i in range(networkMapValuesLen):
            values = list(networkMapValues[i])

            # Add QLineEdit for every item in values
            qLineEdit = QLineEdit(','.join(values))
            self.networkMapVals.append(qLineEdit)
            self.glNetworkMap.addWidget(qLineEdit, i, 1, 1, -1)
    
    def setOutputPath(self, path):
        s = QgsSettings()
        s.setValue("outputPath", path)
        #print(s.value("outputPath"))
        
    def setCustomUnits(self):
        self.selectedUnits = self.txtCustomUnits.text()
        # print("SelectedUnits: " + str(self.selectedUnits))

    def setGroup(self):
        if not self.txtLayerGroup.text():
            self.txtLayerGroup.setText(self.defaultLayerGrp)
        #print(self.txtLayerGroup.text())

    def setUseBand(self):
        if not self.txtRasterBand.text():
            self.txtRasterBand.setText(self.defaultRasterBand)
        #print(self.txtRasterBand.text())
        
    def setNoDataValue(self):
        if not self.chkNoDataList.isChecked():
            self.noDatas[0] = self.txtNoData.text()
        else:
            self.noDatas = list(txtNoData.text() for txtNoData in self.txtNoDatas)
        
        self.noDataValue = self.noDatas
        #print(self.noDataValue)

    def setNetworkMap(self):
        networkMapValsTxtLst = []
        for networkVal in self.networkMapVals:
            networkMapValTxt = list(filter(None, [txt.strip() for txt in networkVal.text().split(",")]))
            networkMapValsTxtLst.append(networkMapValTxt)
            # print("NetworkMapVals: " + str(networkMapValsTxtLst))

        counter = 0
        for key in self.networkMap.keys():
            self.networkMap[str(key)] = networkMapValsTxtLst[counter]
            counter += 1
            
        self.customNetworkMapVals = list(self.networkMap.values())

        # for key, value in self.networkMap.items():
        #     print("Key: " + str(key) + ", Value: " + str(value))

    def printq2uParams(self):
        # Mandatory Parameters
        print("OutputPath: " + q2u.outputPath)
        print("LayerName: " + q2u.name)
        print("ShapefileField: " + str(q2u.field))
        print("Resolution: " + str(q2u.resolution))
        print("Units: " + q2u.units)
        print("Location: " + q2u.location)
        print("Date: " + q2u.date)
        
        # Optional Parameters
        print("LayerColor: " + q2u.color)
        print("LayerGroup: " + q2u.group)
        
        # Metadata
        print("Source: " + q2u.source)
        print("Citation: " + q2u.citation)
        print("MandatoryCitation: " + str(q2u.mandatoryCitation))
        print("Link: " + q2u.link)
        
        # Advanced Parameters
        print("NoDataList: " + str(q2u.noDataList))
        print("ConvertNoData: " + str(q2u.convertNoData))
        print("RasterBand: " + str(q2u.useBand))
        print("MaxPatchSize: " + str(q2u.resolutionPatch[int(q2u.resolution)]))
        print("ResamplingMethod: " + str(q2u.resamplingMethod))
        print("ExtentAsCanvas: " + str(q2u.extentAsCanvas))
        print("KeepSameRes: " + str(q2u.keepSameResolution))
        print("PreventHigherRes: " + str(q2u.preventHigherResolution))
        print("ForMunicipalBudget: " + str(q2u.forMunicipalBudget))
        print("ForReachability: " + str(q2u.forReachability))
        print("ActiveGeomFix: " + str(q2u.activeGeometryFix))
        print("ClipToNoData: " + str(q2u.clipToNoData))
        print("NetworkMap: " + str(q2u.networkMap))
        
    def setq2uParams(self):
        # Mandatory Parameters
        q2u.outputPath = self.txtOutputPath.text()
        q2u.name = self.txtLayerName.text()
        q2u.field = self.selectedShapefileField
        q2u.resolution = self.selectedRes
        q2u.units = self.selectedUnits
        q2u.location = self.txtLocation.text()
        q2u.date = self.date
        
        # Optional Parameters
        q2u.group = self.txtLayerGroup.text()
        q2u.color = self.cmbLayerColor.currentIndex()
        if q2u.color == len(self.colors) - 1:
            q2u.color = 0
            q2u.colorHSV = self.qColor.getHsvF()
        else:
            q2u.colorHSV = None 
        
        # Metadata
        q2u.source = self.txtSource.text()
        q2u.citation = self.txtCitation.text()
        q2u.mandatoryCitation = self.chkMandatory.isChecked()
        q2u.link = self.txtURLLink.text()
        
        # Advanced Parameters
        q2u.noDataList = self.noDataValue
        q2u.convertNoData = self.convertNoData
        q2u.useBand = int(self.txtRasterBand.text())

        if int(q2u.resolution) in range(6):
            # Reset lists to only have 6 elements when selected resolution
            # is within given range but length of lists exceed 6
            if len(q2u.resolutionPatch) > 6:
                q2u.resolutionPatch = q2u.resolutionPatch[:6]
                q2u.resolutionLevels = q2u.resolutionLevels[:6]
        else:
            # Add 7th element to lists for custom resolution only when selected
            # resolution is outside of given range and length of lists does not
            # exceed 6
            # Otherwise, just do assignment
            if len(q2u.resolutionPatch) <= 6:
                q2u.resolutionPatch.append(int(self.txtMaxPatchSize.text()))
                q2u.resolutionLevels.append(int(self.txtCustomRes.text()))
            else:
                q2u.resolutionLevels[6] = int(self.txtCustomRes.text())
        q2u.resolutionPatch[int(q2u.resolution)] = int(self.txtMaxPatchSize.text())
        
        q2u.resamplingMethod = self.selectedResamplingMthd
        q2u.extentAsCanvas = self.chkClipToQGISCanvas.isChecked()
        q2u.keepSameResolution = self.chkKeepSameResAsInput.isChecked()
        q2u.preventHigherResolution = self.chkPreventHigherRes.isChecked()
        q2u.forMunicipalBudget = self.forMunicipalBudget
        q2u.forReachability = self.forReachability
        q2u.activeGeometryFix = self.chkFixGeometry.isChecked()
        q2u.clipToNoData = self.chkClipToNoDataOuterArea.isChecked()
        q2u.networkMap = self.networkMap
        
        # self.printq2uParams()

    def checkMandatoryFields(self):
        if self.selectedInputLayer is None:
            return True, "Please select an Input Layer"
        
        result = self.areAnyTxtFieldsEmpty()
        if result[0]:
            return True, result[1]

        outputPath = self.txtOutputPath.text().replace('\\', '/')
        self.txtOutputPath.setText(outputPath)
        if not os.path.exists(outputPath):
            return True, "Output folder doesn't exist. Please provide a valid output location"
            
        return False, ""

    def areAnyDateFieldsInvalid(self):
        yyyy = self.txtYYYY.text()
        mm = self.txtMM.text()
        dd = self.txtDD.text()
        
        if not yyyy:
            return True, "Please provide a year value for Date"
        if not bool(re.match('[0-9]{4}', yyyy)):
            return True, "Date's year has to be in YYYY format"
        
        if dd:
            if not bool(re.match('[0-9]{2}', dd)):
                return True, "Date's day has to be in DD format"
            if not mm:
                return True, "Please provide a month value for Date"
            if not bool(re.match('[0-9]{2}', mm)):
                return True, "Date's month has to be in MM format"
        
        self.date = yyyy
        if mm:
            if not bool(re.match('[0-9]{2}', mm)):
                return True, "Date's month has to be in MM format"
            if dd:
                self.date += "." + mm + "." + dd
            else:
                # self.date += "." + mm
                return True, "Please provide a day value for Date"

        if yyyy and mm and dd:
            try:
                datetime.datetime(int(yyyy), int(mm), int(dd))
            except ValueError:
                return True, "Provided date is invalid"
                
        return False, ""

    def isUnitDeselected(self):
        if self.selectedUnits is None:
            return True, "Please select an option for Units"
        return False, ""

    def anyCustomTxtFieldsEmpty(self):
        if self.wgtCustomRes.isVisible() and not self.txtCustomRes.text():
            return True, "Please provide a value for Resolution"
        if self.wgtCustomUnits.isVisible() and not self.txtCustomUnits.text():
            return True, "Please provide a value for Units"
        return False, ""
        
    def isNoDataFieldEmpty(self):
        if self.convertNoData == False:
            isEmpty = False
            if self.chkNoDataList.isChecked():
                if not self.noDatas:
                    isEmpty = True
            else:
                isEmpty = (self.txtNoData.text() == "")
            if isEmpty:
                return True, "No Data Value fields are required"
        return False, ""

    def isAnyNetworkMapEmpty(self):
        for key, value in self.networkMap.items():
            if len(value) == 0:
                return True, "Please provide a value for " + str(key)
        return False, ""

    def areAnyTxtFieldsEmpty(self):
        if self.selectedOutputType == 0:    # Data Layer
            if not self.txtOutputPath.text(): return True, "Please provide an Output Path"
            if not self.txtLayerName.text(): return True, "Please provide a Layer Name"
            if not self.txtLocation.text(): return True, "Please provide a Location"
            result = self.isUnitDeselected()
            if result[0]: return True, result[1]
            result = self.anyCustomTxtFieldsEmpty()
            if result[0]: return True, result[1]
            result = self.areAnyDateFieldsInvalid()
            if result[0]: return True, result[1]
            result = self.isNoDataFieldEmpty()
            if result[0]: return True, result[1]
            if not self.txtMaxPatchSize.text(): return True, "Please provide a value for Max Patch Size"
        elif self.selectedOutputType == 1:    # Reachability
            if not self.txtOutputPath.text(): return True, "Please provide an Output Path"
            if not self.txtLocation.text(): return True, "Please provide a Location"
            result = self.isAnyNetworkMapEmpty()
            if result[0]: return True, result[1]
        elif self.selectedOutputType == 2:    # Municipal Budget
            if not self.txtOutputPath.text(): return True, "Please provide an Output Path"
            if not self.txtLocation.text(): return True, "Please provide a Location"
            result = self.anyCustomTxtFieldsEmpty()
            if result[0]: return True, result[1]
            result = self.isNoDataFieldEmpty()
            if result[0]: return True, result[1]
        return False, ""

    def cleanCategoryString (self, rawCategory):
        """ Clean values if e.g.: record appears as array"""
        rawRecord  = str(rawCategory).strip()
        rawRecordWithoutBrackets = rawRecord.lstrip("[").rstrip("]")
        rawRecords = rawRecordWithoutBrackets.split(",")
        cleanRecords = []
        for r in rawRecords: 
            if (r.startswith("'") and r.endswith("'")):
                cleanRecords.append( r.strip("'"))
            else:
                cleanRecords.append(r)
        cleanRecord  = ' & '.join(cleanRecords)
        return cleanRecord

    def clearMsgBar(self):
        self.msgBar.clearWidgets()
            
    def warningMsgBar(self, msg):
        if msg:
            self.msgBar.pushMessage("Warning", msg, level=Qgis.Warning, duration=-1)

    def startLogger(self):
        # Redirect console output to Log tab
        self.logger = Logger(self.txtLog)
        if debuggingMode == True:
            self.logger.openLogFile(QStandardPaths.writableLocation(QStandardPaths.DesktopLocation))

    def stopLogger(self):
        if self.logger is not None:
            self.logger.detach()
            self.logger.close()
            self.logger = None
    
    def setGUITabOrder(self):
        if self.networkMapVals:
            QWidget.setTabOrder(self.txtURLLink, self.cmbRoadClasses)
            QWidget.setTabOrder(self.cmbRoadClasses, self.networkMapVals[0])
            
            for i in range(len(self.networkMapVals) - 1):
                QWidget.setTabOrder(self.networkMapVals[i], self.networkMapVals[i + 1])
            
            QWidget.setTabOrder(self.networkMapVals[-1], self.txtRasterBand)

    def resetTask(self):
        self.progressBar.setValue(0.0)
        self.q2uExportTask.progressChanged.disconnect(self.onTaskProgressChanged)
        self.q2uExportTask = None
        self.btnCancel.setEnabled(False)
        self.btnRun.setEnabled(True)

    def completeQ2UExport(self, exception, result=None):
        self.logger.detach()
        self.logger.write("\n")
        self.logger.write("\n") # write on purpose two new lines
        self.resetTask()
        
    def loadPrevTxt(self, txtField, storedVal, defaultVal):
        txtField.setText(txtField.text() if not storedVal else storedVal)
        
    def loadPrevCmbIndex(self, cmbBox, storedVal, defaultVal):
        cmbBox.setCurrentIndex(defaultVal if not storedVal else max(0, min(int(storedVal), cmbBox.count() - 1)))
        
    def loadPrevCheckbox(self, checkbox, storedVal, defaultVal):
        if isinstance(storedVal, str):
            checkbox.setChecked(False if storedVal.lower() == "false" else True)
        else:
            checkbox.setChecked(defaultVal if not storedVal else storedVal)
        
    def loadPrevNoDataValuesList(self, storedList, storedNoDataValue, storedNoDataValues):
        # load prev chkNoDataList value
        if isinstance(storedList, str):
            self.chkNoDataList.setChecked(False if storedList.lower() == "false" else True)
        else:
            self.chkNoDataList.setChecked(self.chkNoDataList.isChecked() if not storedList else storedList)
            
        # load prev no data value
        self.txtNoData.setText(self.txtNoData.text() if not storedNoDataValue else storedNoDataValue)
        
        # load prev no data value
        # add empty QLineEdits to accomodate saved no data values
        noDataValuesLen = 0 if not storedNoDataValues else len(storedNoDataValues)
        txtNoDatasLen = len(self.txtNoDatas)
        if txtNoDatasLen < noDataValuesLen:
            for i in range(noDataValuesLen - txtNoDatasLen ):
                qLineEdit = QLineEdit("")
                qLineEdit.textChanged.connect(self.onNoDataTextChanged)
                self.txtNoDatas.append(qLineEdit)
                self.vblNoDatas.addWidget(qLineEdit)
        for i in range(noDataValuesLen):
            self.txtNoDatas[i].setText("" if not storedNoDataValues[i] else storedNoDataValues[i])
            
    def loadPrevNetworkMap(self, storedNetworkMapVals, defaultNetworkMapVals):
        self.customNetworkMapVals = list(defaultNetworkMapVals) if not storedNetworkMapVals else list(storedNetworkMapVals)
        
        networkMapValues = list(self.networkMap.values())
        networkMapValuesLen = len(networkMapValues)
        
        # Update values and toggle QLineEdit enabled depending on Default or Custom option
        for i in range(networkMapValuesLen):
            self.networkMapVals[i].setText(','.join(self.customNetworkMapVals[i]))

    def dataLayerRasterUIElems(self):
        # QGIS Input
        self.lblShapefileField.hide()
        self.wgtShapefileField.hide()

        # Mandatory Parameters
        self.lblLayerName.show()
        self.txtLayerName.show()
        self.txtLayerName.setEnabled(True)
        self.lblRes.show()
        self.cmbRes.show()
        self.lblUnits.show()
        self.cmbUnits.show()
        self.lblDate.show()
        self.wgtDate.show()

        # Optional Parameters
        self.frmHLineOptionalParams.show()
        self.lblOptionalParams.show()
        self.lblLayerColor.show()
        self.wgtLayerColorGrp.show()
        self.lblLayerGroup.show()
        self.txtLayerGroup.show()

        # Metadata
        self.frmHLineMetadata.show()
        self.lblMetadata.show()
        self.lblSource.show()
        self.txtSource.show()
        self.lblCitation.show()
        self.wgtCitation.show()
        self.lblURLLink.show()
        self.txtURLLink.show()

        # Advanced Parameters
        self.lblRasterBand.show()
        self.txtRasterBand.show()
        self.lblMaxPatchSize.show()
        self.wgtMaxPatchSize.show()
        self.lblResamplingMthd.show()
        self.cmbResamplingMthd.show()
        self.onResamplingMthdChanged(self.cmbResamplingMthd.currentIndex())
        self.lblNoDataValue.show()
        self.wgtNoDataValue.show()
        self.chkClipToQGISCanvas.show()
        self.chkKeepSameResAsInput.show()
        self.chkPreventHigherRes.show()
        self.chkClipToNoDataOuterArea.show()
        self.lblRoadClasses.hide()
        self.cmbRoadClasses.hide()
        self.wgtNetworkMap.hide()

        # Custom widgets
        self.onResolutionChanged(self.cmbRes.currentIndex())
        self.onUnitsChanged(self.cmbUnits.currentIndex())
        self.onLayerColorChanged(self.cmbLayerColor.currentIndex())

        self.forMunicipalBudget = False
        self.forReachability = False

    def dataLayerVectorUIElems(self):
        # QGIS Input
        self.lblShapefileField.show()
        self.wgtShapefileField.show()

        # Mandatory Parameters
        self.lblLayerName.show()
        self.txtLayerName.show()
        self.txtLayerName.setEnabled(True)
        self.lblRes.show()
        self.cmbRes.show()
        self.lblUnits.show()
        self.cmbUnits.show()
        self.lblDate.show()
        self.wgtDate.show()

        # Optional Parameters
        self.frmHLineOptionalParams.show()
        self.lblOptionalParams.show()
        self.lblLayerColor.show()
        self.wgtLayerColorGrp.show()
        self.lblLayerGroup.show()
        self.txtLayerGroup.show()

        # Metadata
        self.frmHLineMetadata.show()
        self.lblMetadata.show()
        self.lblSource.show()
        self.txtSource.show()
        self.lblCitation.show()
        self.wgtCitation.show()
        self.lblURLLink.show()
        self.txtURLLink.show()

        # Advanced Parameters
        self.lblRasterBand.hide()
        self.txtRasterBand.hide()
        self.lblMaxPatchSize.show()
        self.wgtMaxPatchSize.show()
        self.lblResamplingMthd.hide()
        self.cmbResamplingMthd.hide()
        self.lblNoDataCalculation.hide()
        self.cmbNoDataCalculation.hide()
        self.lblNoDataValue.show()
        self.wgtNoDataValue.show()
        self.chkClipToQGISCanvas.show()
        self.chkKeepSameResAsInput.hide()
        self.chkPreventHigherRes.hide()
        self.chkClipToNoDataOuterArea.show()
        self.lblRoadClasses.hide()
        self.cmbRoadClasses.hide()
        self.wgtNetworkMap.hide()
        
        # Custom widgets
        self.onResolutionChanged(self.cmbRes.currentIndex())
        self.onUnitsChanged(self.cmbUnits.currentIndex())
        self.onLayerColorChanged(self.cmbLayerColor.currentIndex())

        self.forMunicipalBudget = False
        self.forReachability = False

    def reachabilityVectorUIElems(self):
        # QGIS Input
        self.lblShapefileField.show()
        self.wgtShapefileField.show()

        # Mandatory Parameters
        self.lblLayerName.show()
        self.txtLayerName.show()
        self.txtLayerName.setText("Network")
        self.txtLayerName.setEnabled(False)
        self.lblRes.hide()
        self.cmbRes.hide()
        self.lblUnits.hide()
        self.cmbUnits.hide()
        self.lblDate.hide()
        self.wgtDate.hide()

        # Optional Parameters
        self.frmHLineOptionalParams.hide()
        self.lblOptionalParams.hide()
        self.lblLayerColor.hide()
        self.wgtLayerColorGrp.hide()
        self.lblLayerGroup.hide()
        self.txtLayerGroup.hide()

        # Metadata
        self.frmHLineMetadata.hide()
        self.lblMetadata.hide()
        self.lblSource.hide()
        self.txtSource.hide()
        self.lblCitation.hide()
        self.wgtCitation.hide()
        self.lblURLLink.hide()
        self.txtURLLink.hide()

        # Advanced Parameters
        self.lblRasterBand.hide()
        self.txtRasterBand.hide()
        self.lblMaxPatchSize.hide()
        self.wgtMaxPatchSize.hide()
        self.lblResamplingMthd.hide()
        self.cmbResamplingMthd.hide()
        self.lblNoDataCalculation.hide()
        self.cmbNoDataCalculation.hide()
        self.lblNoDataValue.hide()
        self.wgtNoDataValue.hide()
        self.chkClipToQGISCanvas.hide()
        self.chkKeepSameResAsInput.hide()
        self.chkPreventHigherRes.hide()
        self.chkClipToNoDataOuterArea.hide()
        self.lblRoadClasses.show()
        self.cmbRoadClasses.show()
        self.wgtNetworkMap.show()
        
        # Custom widgets
        self.wgtCustomRes.hide()
        self.wgtCustomUnits.hide()
        self.wgtCustomLayerColor.hide()

        self.forMunicipalBudget = False
        self.forReachability = True

    def municipalBudgetVectorUIElems(self):
        # QGIS Input
        self.lblShapefileField.show()
        self.wgtShapefileField.show()

        # Mandatory Parameters
        self.lblLayerName.hide()
        self.txtLayerName.hide()
        self.lblRes.show()
        self.cmbRes.show()
        self.lblUnits.hide()
        self.cmbUnits.hide()
        self.lblDate.hide()
        self.wgtDate.hide()

        # Optional Parameters
        self.frmHLineOptionalParams.hide()
        self.lblOptionalParams.hide()
        self.lblLayerColor.hide()
        self.wgtLayerColorGrp.hide()
        self.lblLayerGroup.hide()
        self.txtLayerGroup.hide()

        # Metadata
        self.frmHLineMetadata.hide()
        self.lblMetadata.hide()
        self.lblSource.hide()
        self.txtSource.hide()
        self.lblCitation.hide()
        self.wgtCitation.hide()
        self.lblURLLink.hide()
        self.txtURLLink.hide()

        # Advanced Parameters
        self.lblRasterBand.hide()
        self.txtRasterBand.hide()
        self.lblMaxPatchSize.hide()
        self.wgtMaxPatchSize.hide()
        self.lblResamplingMthd.hide()
        self.cmbResamplingMthd.hide()
        self.lblNoDataCalculation.hide()
        self.cmbNoDataCalculation.hide()
        self.lblNoDataValue.show()
        self.wgtNoDataValue.show()
        self.chkClipToQGISCanvas.hide()
        self.chkKeepSameResAsInput.hide()
        self.chkPreventHigherRes.hide()
        self.chkClipToNoDataOuterArea.show()
        self.lblRoadClasses.hide()
        self.cmbRoadClasses.hide()
        self.wgtNetworkMap.hide()
        
        # Custom widgets
        self.wgtCustomRes.hide()
        self.wgtCustomUnits.hide()
        self.wgtCustomLayerColor.hide()

        self.forMunicipalBudget = True
        self.forReachability = False
