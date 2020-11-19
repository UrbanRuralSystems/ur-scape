"""---------------------------------------------------------------------
Please set basic parameters
---------------------------------------------------------------------"""
outputPath = "Insert Data Path" # Do not use any backslash (e.g. C:/Documents/ur-scape/Data)
name = "Insert Layer Name" # Set layer name in Camel case (e.g. Population Density)
field = "Insert Field Name" # Name of the field to be used from shapefile. Will be ignored if raster.
resolution = "1" # 0-Neighbourhood, 1-City, 2-Metropolitan, 3-National, 4-Continental, 5-Global

"""Please set metadata"""
units = "Insert Units" # set layer units (e.g. People per Sq km or set number from list: 0 = 'population/Sq2', 1 = AMSL )
location = "Insert Location" # set location of layer (e.g. Palembang)
source = "Insert Source" # set source of layer (e.g. Statistics Bureau)
date = "YYYY.MM.DD" # Year of data in format YYYY.MM.DD (month and day are optional)
color = "1" # 0=Red, 1=Orange, 2=Yellow, 3=Light Green , 4 = Dark Green, 5 = Cyan, 6 = Light Blue, 7 = Dark Blue, 8 = Purple, 9 = Pink
group = "Exported From QGIS"
citation = "Insert Citation" # insert citation - 
mandatoryCitation = False # should the citation be always shown?
link = "Insert Link" # link to dataset if aplied 

"""---------------------------------------------------------------------
Please set advanced parameters below only if you are advanced user
---------------------------------------------------------------------"""
noDataValue = None
noDataList = None
useBand = 1 # set the band for multi-band data (default is 1)
extentAsCanvas = False
resolutionPatch = [1, 2, 5 , 10, 50, 360] # in degrees units
resolutionLevels = [10, 100, 300, 0.5/60.0, 5/60.0, 25/60.0]
# resolution is in [metres, metres, metres, degrees, degrees,degrees,]
keepSameResolution = False
preventHigherResolution = True

forMunicipalBudget = False # Create basic data for municipal budget
forReachability = False # Create basic data for reachability
resamplingMethod = 0 # 0 Nearest neighbor, 1 Bilinear, 5 average, 7 Maximum, 8 minimum, 9 summary
activeGeometryFix = False
convertNoData = False  # False will output noData (default); True will ignore the cell for aggregation
clipToNoData = False
#version for indonesia
"""
networkMap ={"Highway":["Tol"],\
             "Highway Link":["Tautan"],\
             "Primary":["Arteri primer","Kolektor primer"],\
             "Secondary":["Arteri sekunder","Kolektor sekunder"],\
             "Ignore":["Lokal primer","Lokal sekunder"]}   

""" #version for OSM   
networkMap ={"Highway":["motorway","trunk"],\
             "Highway Link":["motorway_link", "trunk_link"],\
             "Primary":["primary"],\
             "Secondary":["secondary"],\
             "Ignore":["footway"]}     

"""---------------------------------------------------------------------
Setup for developer
---------------------------------------------------------------------"""
resolutionEPSG = ['3395', '3395', '3395', '4326', '4326', '4326','3395'] # '3857'= metres, '4326' = degress
resolutionSign = ['D', 'D', 'D', 'C', 'B', 'A','D' ]
unitsList= ['None' , 'category','Count','Index' , 'population/SqKm2', 'population/ha','AMSL', 'Percentage', 'Yield : tons / ha', 'Minutes','Hours', 'kWh/m2', 'mm/Year', 'PPP USD', 'Radiance']
isUnitsRelative= [False, False, False, False, True, True,False,False, True,False,False,True,False,False,False]
unitsMultiply=[ 1 , 1 , 1 , 1 , 1 , 100 , 1 , 100 ] # difference for relative numbers from km2
colorHSV = None
debuggingMode = False
onlyYear = False

"""---------------------------------------------------------------------
You can't touch this
---------------------------------------------------------------------"""
import gdal, os, sys, osr, processing, csv, math, colorsys,traceback,numpy,datetime,numbers,shutil 
from tempfile import mkstemp
from gdalconst import *
from qgis.core import (QgsProject
                      ,QgsDistanceArea
                      ,QgsCoordinateReferenceSystem
                      ,QgsPointXY
                      ,QgsRectangle
                      ,QgsProcessingUtils
                      ,QgsVectorLayer
                      ,QgsRasterLayer
                      )
from qgis.utils import iface
from PyQt5.QtCore import QFileInfo
from processing.core.Processing import Processing
isPlugin = (__name__ != '__console__')

if isPlugin :
	Processing.initialize()

gdal.AllRegister()
CHECK_DISK_FREE_SPACE = False

class Exporter:
    "This class will export data to urscape"
    
    def __init__(self, task=None):
        self.task = task
        setup = Setup(task)
        if not setup.hasProblem():
            try:
                CheckLayer(setup)
            except Exception as error:
                self.handleError (error)
            
    def handleError(self, error):
        print ("Oops, please following report error to developer:")
        tbl = traceback.format_exc().splitlines() 
        exception = tbl[-1] + " in line:" +tbl[-2].split("line")[-1]
        print(exception)
        self.setException(exception)
        if debuggingMode:
            raise  

    def setException(self, exception):
        if self.task is not None:
            self.task.exception = exception
 
class Setup:
    "this class include basic setup"
    def __init__(self, task=None):
        self.task = task
        self.layer = iface.activeLayer()
        self.fullName = self.layer.dataProvider().dataSourceUri()
        self.units = self.defineUnits ()
        self.problem = self.primaryCheck()
        self.inputCRS = self.layer.crs().authid()
        self.aggregate =  resamplingMethod == 5
        self.summary = resamplingMethod == 9
        self.noDataList = [] # Setup noDataList used inside UpdateCategory and SetNoData 
        if not self.problem:
            self.updateResolution()
            self.updatePath()
            self.updateType()
            self.updateCategory()
            self.setNoData()

    def isCanceledAndUpdateProgress(self, progress):
        if self.task is not None:
            self.task.setProgress(progress)
        return self.task is not None and self.task.isCanceled()

    def defineUnits(self):
        if isinstance(units, numbers.Number) :
            if len(unitsList) > units:
                self.isRelative = isUnitsRelative[units]
                self.unitsMultiply = unitsMultiply[units]
                return unitsList[units]
            else:
                self.isRelative = False
                self.unitsMultiply = 1
                return "n/a"
                
        else: 
           self.isRelative = False
           self.unitsMultiply = 1
           return units
           
    def setNoData(self):
        """Add value if raster has layer define no data value
           no Data for verctor is defined in createCategoryVector as this
           function knows the traslation from field to raster id"""
        if not self.isVector:
            extent = self.layer.extent()
            provider = self.layer.dataProvider()
            rows = self.layer.rasterUnitsPerPixelY()
            cols = self.layer.rasterUnitsPerPixelX()
            block = provider.block(1, extent,  rows, cols)
            noDataFromRaster = block.noDataValue()

            if noDataFromRaster != 'isfinite':
                self.noDataList.append(noDataFromRaster)
        
        """Check the isnstance of noDataValue, after implementing UI this will
        be always list"""
        if noDataList != None:
            #all values must be numeric for raster or non-categorized vector
            self.noDataList.extend(  [val for val in noDataList if isinstance(val,numbers.Number)])
        
        """Check the isnstance of noDataValue, after implementing UI this will
        be always list  TODO remove after implementing UI """
        if noDataValue != None and isinstance(noDataValue, numbers.Number):
                self.noDataList.append(float(noDataValue))
        
    def hasProblem(self):
        return  self.secondaryCheck(self.problem)
        
    def getFieldCat(self):
        fieldCat =  "catID" if self.isCategorized  else field
        return fieldCat
    
    def updateResolution(self):
        """ For user genefit input resolution can be defined in metres for some 
        of resolution levels, however units used in urscape are always in degrees.
        therefore units in metres need to be translated to degrees"""

        self.res =  resolutionLevels[int(resolution)]
        if not resolutionEPSG[int(resolution)] == '4326':
            self.res = geoCalculator().metressToDegressBetwenLons(self.res  )
            
        self.maxPatchSize = resolutionPatch[int(resolution)]
        
        """ Apply safety check for cases when user tries to export higher 
        resolution then raster actual resolution"""
        if (isinstance(self.layer,  QgsRasterLayer)):
            #Check if raster is in degrees or metress
            crs = self.layer.crs()
            unit = crs.mapUnits()
            self.isInMetres = crs.mapUnits() == 0
            unitsPerPixelX = self.layer.rasterUnitsPerPixelX()

            if self.isInMetres: # Input is in metress and output in degrees
                unitsPerPixelX  = geoCalculator().metressToDegressBetwenLons(unitsPerPixelX  ) 
                self.aggregationRes  =  resolutionLevels[int(resolution)] # for aggregation keep original value
            else:
                self.aggregationRes  = self.res   
                
            # If true, it will force the same resolution as input raster
            if keepSameResolution:
                print("Keep Same Resolution is checked, we will keep same resolution as input raster ")
                self.res = unitsPerPixelX ;
                
            # If meet conditions, it will force same  resolution as input raster
            if preventHigherResolution and self.res < unitsPerPixelX  :
                print()
                if (resolutionSign[int(resolution)] == "D"): # user's setup is in metres
                    inputSizeInMetres = str("{:5.2f}".format(unitsPerPixelX * 111000 )) + " metres"
                    outputSizeInMetres =   str(resolutionLevels[int(resolution)])  + " metres"
                else: # user's setup is in deggres
                    inputSizeInMetres =  str("{:5.5f}".format(unitsPerPixelX  )) + " degress"
                    outputSizeInMetres =  str(resolutionLevels[int(resolution)]) + " deggres"
                    
                print("You are trying to export higher resolution then resolution of the input raster")
                print("Input raster has cell size ~ " + inputSizeInMetres)
                print ("You are trying to export in: " + outputSizeInMetres)
                print("Because Prevent Higher Resolution is checked, we will export in reolution of input raster")
                print ("If you want to export in Higher resolution anyway, please uncheck Prevent Higher Resolution and try again")
                self.res = unitsPerPixelX 
        else: 
            self.isInMetres = False 
            self.inputCRS =  'EPSG:4326' 
            self.aggregationRes = self.res # vector have same resolution for aggregation

    def updatePath(self):
        layerPath = outputPath +  "/Sites/" +location +"/"
        budgetPath = outputPath +  "/Municipal Budget/"
        self.finalPath = budgetPath if forMunicipalBudget else layerPath

        if not os.path.exists(self.finalPath):
            print("Creating new folder for: " + location)
            os.makedirs(self.finalPath)
            
            """ create temporary working folder"""
        path = os.path.dirname(os.path.abspath(self.fullName)) 
    
    def updateType(self):
        if isinstance(self.layer ,QgsVectorLayer) :
            self.isVector = True 
            self.type = self.layer.geometryType() 
            if self.type == 0: # points 
                self.isPoint =True
                self.Units =  "points pers cell"
            else:
                self.isPoint =False
                
        else:
            self.type = -1
            self.isVector = False
            self.isPoint = False

    def updateCategory(self):
        if self.isVector :
            # non numeric fields will be processed as categorized urscape layer
            self.isCategorized = not self.layer.fields().field(field).isNumeric()
        else:
            self.isCategorized =  os.path.isfile(self.fullName+".csv")
        if self.isCategorized:
            if self.isVector :
                self.createCategoryVector()
            else:
                self.createCategoryRaster()
        
    def createCategoryRaster(self):
        """ check if raster has dbf file"""
        categories = []  
        csvFilePath =  self.fullName+".csv"
        hasCsv = os.path.isfile(csvFilePath)
        if hasCsv:
            print("Yeah we have found csv for the file")
            with open(csvFilePath, newline='') as csvfile:
                reader = csv.DictReader(csvfile)
                
                setHeader = ""
                for thisName in next(reader): 
                    if field in thisName:
                        print("We even found your field name in csv. Such a lovely day.")
                        setHeader = thisName
                if not setHeader == "":
                    for row in reader:
                        #mask out if noDataValue same as category name
                        if noDataList is not None:
                            if str(row[setHeader]) not in noDataList:
                                categories.append(row[setHeader])
                        if noDataValue is not None:
                            if str(feature[field])  != noDataValue:
                                categories.append(row[setHeader])
                else:
                    print ("filed name not in CSV")   
                    self.isCategorized = False     
        else:
            print("no categories in the csv file")
            self.isCategorized = False
        self.categories = categories
       
    
    def createCategoryVector(self):

        parameters = {'INPUT':self.layer,\
                'FIELD_NAME':"catID",\
                'FIELD_TYPE': 0,\
                'FIELD_LENGTH':3,\
                'FIELD_PRECISION':2,\
                'OUTPUT': 'memory:'\
                }

        result = processing.run('qgis:addfieldtoattributestable', parameters)

        catLayer = result['OUTPUT']
        if debuggingMode:
            QgsProject.instance().addMapLayer(catLayer) # adding to canvas
    
        """ check for all categories in dataset"""
        categories = []
        for feature in catLayer.getFeatures():
            cleanRecord = self.cleanCategoryString(feature[field])
            if not cleanRecord in categories:
                categories.append(cleanRecord)
        categories.sort()  
        
        """ write categories"""
        catLayer.startEditing()
        for feature in catLayer.getFeatures():  
            cleanRecord = self.cleanCategoryString(feature[field])
            categoryId = categories.index(cleanRecord)
            colId = feature.fieldNameIndex("catID") 
            catLayer.changeAttributeValue(feature.id(),colId, categoryId+1)
           
            """mask out if noDataValue same as category name"""
            if noDataList is not None:
                if str(feature[field]) in noDataList and not categoryId+1 in self.noDataList:
                    self.noDataList.append(categoryId+1)
            if noDataValue is not None:
                if str(feature[field]) == noDataValue and not categoryId+1 in self.noDataList:
                    self.noDataList.append(categoryId+1)

        catLayer.commitChanges()
    
        if len(categories)>128 and not forMunicipalBudget:
            print ("WARNING! You are using more than 128 categories. ur-scape won't show this correctly.")

        self.categories = categories
        self.layer = catLayer
 
    
    "Check basic inputs before update Setup"    
    def primaryCheck(self):
        problem = False
        problem = self.testResolutionInput(problem)
        problem = self.testPath(problem)
        problem = self.testFieldNameInput(problem)
        problem = self.checkAvailableSize(problem)
        return problem
    "Check more generic data"      
    def secondaryCheck(self, problem):
        problem = self.testFiles(problem)
        problem = self.testScenarios(problem)
        if isinstance(iface.activeLayer() ,QgsVectorLayer) :  
            problem = self.testMunicipalBudget(problem)
 
        return problem
     
    """Check if user has anough avaialble space on his disk for creating new
    rasters during the data processing"""
    def checkAvailableSize (self , problem):
        
        # Skip if there is problem in previous checks 
        if problem:
            return problem
        
        # get availble disk space
        total, used, free = shutil.disk_usage(QgsProcessingUtils.tempFolder())

        # Get extent and Sizes
        extent = self.layer.extent()
        
        xSize = extent.xMaximum() - extent.xMinimum()
        ySize = extent.yMaximum() - extent.yMinimum()
        
        #For Vector a size needs to be extimated base one output resolution 
        if isinstance(self.layer ,QgsVectorLayer): # check with user resolution
           
            # Check input and output units
            inputInMetres = self.layer.crs().mapUnits() == 0
            outputInMetres = not resolutionEPSG[int(resolution)] == '4326'
            # Adjust size base on units differnce (110000 = ~ 1 degree)
            if inputInMetres and not outputInMetres  :
                cols = xSize / 110000 / resolutionLevels[int(resolution)]
                rows = ySize / 110000 / resolutionLevels[int(resolution)]
            elif not inputInMetres and outputInMetres :
                cols = xSize * 110000 / resolutionLevels[int(resolution)]
                rows = ySize * 110000 / resolutionLevels[int(resolution)]
            else:
                cols = xSize / resolutionLevels[int(resolution)]
                rows = ySize/ resolutionLevels[int(resolution)]

        else: # For raster divide size by units per pixel (always same units)
            cols = xSize/self.layer.rasterUnitsPerPixelX()
            rows = ySize/self.layer.rasterUnitsPerPixelY()

        # pixels total, multiplied by float and with buffer 10%
        requiredSize =  cols * rows * 64/8
        if requiredSize > free:
            print("You dont have enough space on your disk to continue with export." )
            print ("You need at least " + str("{:5.2f}".format(requiredSize * 0.000001)) + " MB of free space")
            print ("You have currectly available " + str("{:5.2f}".format(free * 0.000001)) + "MB" )
            return True
        else:
            return problem
          
     
    def testScenarios(self, problem):
        if hasattr(self, "type"):
            if (forMunicipalBudget and not self.type == 2 ):
                print("wrong data type for municipal budget")
                return True
            elif (forReachability and not self.type == 1 ):
                print("wrong data type for reachability")
                return True
        else:
           return problem    
            
    def testResolutionInput(self, problem):
            try: 
                test = resolutionLevels[int(resolution)]
                return problem
            except IndexError:
                print ("Oops, resolution is invalid !")
                return True 
    
    def testFieldNameInput (self, problem):
        try: 
            if isinstance(iface.activeLayer() ,QgsVectorLayer) :
                test = iface.activeLayer().fields().field(field)
            return problem
        except KeyError:
            print("Oops, field is not in the layer !")
            return True

    def testFiles (self, problem):
        filePath = outputPath + "/layers.csv"
        try:
            if not self.testPath(False):
                with open(filePath, 'r', newline='') as test:
                    pass
            return problem
        except:
            print("Could not open " + filePath + ". Please close the file")
            return True          
    
    def testPath (self, problem):
        if os.path.exists(outputPath): 
            return problem
        else:
            print("Oops, OutputPath does not exist !")
            return True 
            
    def testMunicipalBudget (self, problem):
        if iface.activeLayer().geometryType() == 2 or not forMunicipalBudget:
            return problem
        else:
            print("Oops, Municipal Budget can be done only from polygons!")
            return True 
    
    def testRasterReference (self, problem):
        raster = gdal.Open(iface.activeLayer().dataProvider().dataSourceUri(), 1)
        if not (raster.GetProjectionRef() == ""):
            return problem
        else:
            print("Oops, no projection defined for raster layer!")
            return True   
            
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
        
class LayerWriter:
    "This class handle writing layers to Layers.csv"
    def __init__(self,name,group):
        path = self.findLayersCsv()
        if path:
            self.checkName(path)    
     
    def checkName (self, path):
        encoding = self.getEncoding(path)
        spamList = None
        with open(path, 'r', newline='', encoding=encoding) as csvfile:
            spamreader = csv.reader(csvfile,delimiter = ',')
            spamList = [ x for x in spamreader]
        if not any(name in s for s in spamList):
            self.addLayerToList(path,name, group,spamList)  
            print("Writting layer " + name + " to the file: Layers.csv")
        elif name:
            print ("It seems like we already have layer " + name +" in the file: Layers.csv.")
            self.checkColors(spamList)

    def addLayerToList (self, path, name,group,layerList):
        colorRGB = self.getColorRGB()
        index = len(layerList) # by default it write layer to the end
        for layer in  layerList: # check if group is in the list
            if any("Group"== s for s in layer ):
                if any(group.lower() == s.lower() for s in layer ):
                    index = layerList.index(layer) 
        if index < len (layerList): # if group exist write under the index of the group
            layerList.insert (index+1, ['Layer',name,colorRGB[0],colorRGB[1],colorRGB[2]])
        else:     # append new group and layer on the end
            layerList.append(['','','','',''])
            layerList.append(['Group',group,'','',''])
            layerList.append ( ['Layer',name,colorRGB[0],colorRGB[1],colorRGB[2]])
        
        with open(path, 'w', newline='', encoding='utf-16le') as csvfile:
            spamwriter = csv.writer(csvfile, delimiter=',')
            for row in  layerList:
                if len(row)>0:
                    spamwriter.writerow(row)
           
        
    def findLayersCsv(self):
        path = outputPath + "/layers.csv"
        if os.path.isfile(path):
            return path
        else:
            return None
            print("I can't find the layers.csv file, it is up to you to add it to ur-scape.")
    
    def getColorRGB(self):
        # prepare full HSV color
        if colorHSV is None:
            h = int(color)/10
            s = 1
            v = 1
        else:
            h = colorHSV[0]
            s = colorHSV[1]
            v = colorHSV[2]
        colorRGB = colorsys.hsv_to_rgb(h,s,v)
        return tuple(int(band*255) for band in colorRGB)
        
    def getEncoding(self, path):
        try: 
            with open(path, 'r', newline='', encoding='utf-8') as csvfile:
                spamreader = csv.reader(csvfile,delimiter = ',')
                spamList = [ x for x in spamreader]  #TODO Check why this is needed 
            return 'utf-8';
        except:
            return 'utf-16le';
    
    def checkColors(self, inList):
        # inform user that color will be ignored if layer with different color already exists
        colorRGB = self.getColorRGB()
        for layer in  inList:
            if any(name == s for s in layer):
                if (layer[2].isdigit()):
                    if not (int(layer[2]) == colorRGB[0] and int(layer[3]) == colorRGB[1] and int(layer[4]) == colorRGB[2]):
                        print("We have same name but different color, will be ignored")

class FileWriter:
    "Create Csv File"
    
    def __init__(self,raster, setup):
        if forReachability:
            """Create name for graph file"""
            dateCode = list(date)[-2] + list(date)[-1] if onlyYear else date.replace('.', '')
            fileName = name + '_D_'+location+'_'+dateCode+ '_graph.csv'
            self.path = setup.finalPath+'/' +fileName
            with open(self.path, 'w') as output_file:
                #output_file.write("lenght;source;target;x1;y1;x2;y2;classification;WKT" + "\n") # for testing network in QGIS only
                output_file.write("lenght,source,target,x1,y1,x2,y2,classification" + "\n")

            self.oldGraphData = None
            self.oldGraphVarify = None 
            
        else:    

            extents = self.getExtents(raster,setup) 
            for i in range (0,len(extents)): 
                if extents[i] is not None:
                    rasterExtent = self.clipRaster(raster, extents[i],i,0)
                    self.getBand(rasterExtent,setup)
                    self.writeGridToFile(i,setup,extents[i])
                else:
                    print("Skiping the patch, because is only no Data inside")    
            
            if not forMunicipalBudget:
                LayerWriter(name, group)
            print ("Done here, have a great day")
    
    
    def getExtents(self,raster,setup):
        
        rlayer = QgsRasterLayer(raster, QFileInfo(raster).baseName())
        
        """use extent from canvas or raster"""
        if extentAsCanvas:
            my_crs=QgsCoordinateReferenceSystem(4326)
            QgsProject.instance().setCrs(my_crs)
            ex = iface.mapCanvas().extent()
        else:
            inRaster = gdal.Open(raster,GA_ReadOnly)
            inCountX = inRaster.RasterXSize
            inCountY = inRaster.RasterYSize
            gt = inRaster .GetGeoTransform()    
            inMinX = gt[0] 
            inMinY = gt[3]
            inDegPerCellX = gt[1]
            inDegPerCellY = gt[5]
            inMaxX = inMinX + inDegPerCellX * inCountX
            inMaxY = inMinY + inDegPerCellY * inCountY
        # Fix input raster cell size signs
        if inDegPerCellX < 0:
            print("Changing negative cell width to positive")
            temp = inMinX
            inMinX = inMaxX
            inMaxX = temp
            inDegPerCellX = -inDegPerCellX
        if inDegPerCellY > 0:
            print("Changing positive cell height to negative")
            temp = inMinY
            inMinY = inMaxY
            inMaxY = temp
            inDegPerCellY = -inDegPerCellY
            inWidth = inMaxX - inMinX
            inHeight = inMaxY - inMinY
        ex = QgsRectangle(inMinX,inMinY ,inMaxX,inMaxY )

        """check if the extend on Latitude not bigger then 85 (not recognized by Mercator)"""
        pixelSizeY = rlayer.rasterUnitsPerPixelY()
        if (ex.yMaximum() > 85):
            numOfCells =  math.ceil((ex.yMaximum() - 85) / pixelSizeY )
            yMaxCliped =  ex.yMaximum() -  numOfCells *  pixelSizeY 
        else:
            yMaxCliped =  ex.yMaximum()

        if (ex.yMinimum() < -85):
            numOfCells =  math.floor((ex.yMinimum() + 85) / pixelSizeY )
            yMinCliped =  ex.yMinimum() - numOfCells *  pixelSizeY 
        else:
            yMinCliped =  ex.yMinimum()

        """translate max as floor and ceil to get full range"""
        xMinFloor = math.floor(ex.xMinimum()) 
        xMaxCeil = math.ceil(ex.xMaximum()) 
        yMinFloor = math.floor(yMinCliped) 
        yMaxCeil = math.ceil(yMaxCliped ) 

        xRange = xMaxCeil-xMinFloor
        yRange = yMaxCeil-yMinFloor
        maxPatchSize = setup.maxPatchSize
        res = setup.res
        extents = [] 
        index = 0 # delete after testing noData clipping
        """ get extend for each 1 degree by 1 degree square or full extent"""
        if (( xRange >= maxPatchSize) or  (yRange >= maxPatchSize)) and not extentAsCanvas:
            for x in numpy.arange (xMinFloor, xMaxCeil, maxPatchSize ):
                for y in numpy.arange (yMinFloor, yMaxCeil,  maxPatchSize) :
                    xMinE = ex.xMinimum() if (ex.xMinimum()>x) else x
                    xMaxE = ex.xMaximum() if (ex.xMaximum()<(x+ maxPatchSize)) else (x + maxPatchSize )
                    yMinE = yMinCliped if (yMinCliped>y) else y
                    yMaxE = yMaxCliped  if (yMaxCliped< (y+ maxPatchSize) ) else (y + maxPatchSize )
                
                    trueRes = res * 0.00001 if int(resolution) <= 2 else res # to make sure the patch is bigger then cell size
                
                    if(xMaxE - xMinE) > trueRes and (yMaxE - yMinE) > trueRes:
                        extent = QgsRectangle (xMinE, yMinE, xMaxE, yMaxE)
                        index = index +1 # delete after testing noData clipping
                        extent = self.ClipToNoData(raster, extent, setup)

                        extents.append (extent)
        else:
            extent = QgsRectangle (ex.xMinimum(), yMinCliped , ex.xMaximum(),yMaxCliped )
            extent = self.ClipToNoData(raster, extent, setup)
            extents.append(extent)

        return extents
    
    def clipRaster (self,rasterIn, extent, index, cat ):
        
        clipRaster = QgsProcessingUtils.tempFolder() + "/Clipped_raster.tif"
        # processing.algorithmHelp('gdal:cliprasterbyextent')
        parameterClip = { 'INPUT': rasterIn,\
                        'PROJWIN': extent,\
                        'NODATA':0,\
                        'OUTPUT': clipRaster}
                            
        processing.run('gdal:cliprasterbyextent', parameterClip)
        """ one more resample to make sure Xcount and Y are alway same, e.g for municipal Budget"""
    
        if debuggingMode:
            layerTesting = QgsRasterLayer(clipRaster,"Clip Raster" + (str)(index))
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas
     
        return clipRaster 
    
    def ClipToNoData(self, rasterIn, extent, setup):
        
        """return extent without Changes is clipNoData is not activated"""
        if not clipToNoData:
            return extent
        
        """get input variables from raster"""
        rasterIn = gdal.Open(rasterIn,GA_ReadOnly)
        xCount,yCount = rasterIn.RasterXSize, rasterIn.RasterYSize
        data = rasterIn.GetRasterBand(useBand).ReadAsArray(0, 0,  xCount,yCount)
        gt = rasterIn.GetGeoTransform()
        xRasterMin, yRasterMin, width, height = gt[0], gt[3], gt[1], gt[5]
      
        """start and end index from min and max"""
        xStart = int(math.floor(( extent.xMinimum()-xRasterMin )/width))
        yStart = int(math.floor((extent.yMaximum()-yRasterMin )/height))
        xEnd = int(math.floor(( extent.xMaximum()-xRasterMin )/width))
        yEnd = int(math.floor((extent.yMinimum() -yRasterMin  )/height))
        isItEmpty = True
        
        """ Check for first no data on X axis and save position to yStart"""
        for y in range(yStart,yEnd ):
            breakFromFirstLoop = False
            for x in range(xStart, xEnd):
                if not math.isnan(data[y,x]):
                    yStart = y
                    breakFromFirstLoop = True
                    isItEmpty = False
                    break
            if breakFromFirstLoop:
                break;        
        """Shortcut: if yStart does not change it means that all of the dataset is NoData!"""
        if isItEmpty:
            return None
                    
        """ Check for first no data on Y axis and save position to xStart"""
        for x in range(xStart, xEnd):
            breakFromFirstLoop = False            
            for y in range(yStart,yEnd ):
                if not math.isnan(data[y,x]):
                    xStart = x
                    breakFromFirstLoop = True 
                    break
            if breakFromFirstLoop:
                break;  

        """ Check for last no data on X axis and save position to yEnd"""
        for y in range(yEnd-1, yStart-1, -1):
            breakFromFirstLoop = False            
            for x in range(xEnd-1, xStart-1, -1):
               if not math.isnan(data[y,x]):
                    yEnd = y
                    breakFromFirstLoop = True 
                    break
            if breakFromFirstLoop:
                break;                      
        """ Check for last no data on Y axis and save position to xEnd"""
        for x in range(xEnd-1, xStart-1, -1):
            breakFromFirstLoop = False 
            for y in range(yEnd-1, yStart-1, -1):
               if not math.isnan(data[y,x]):
                    xEnd = x
                    breakFromFirstLoop = True 
                    break
            if breakFromFirstLoop:
                break;  
                
        """update extent values"""
        eMinX = xRasterMin + xStart * width
        eMinY = yRasterMin + yStart * height
        eMaxX = xRasterMin + xEnd * width
        eMaxY = yRasterMin + yEnd * height

        """tranclate extent to QGIS extent string format """ 
        extent = QgsRectangle (eMinX, eMinY, eMaxX , eMaxY )
        return extent
    
    def writeGridToFile(self,index,setup,extent):
        """ get info about size and position"""
        minX,minY,maxX,maxY = self.getCleanExtent(setup,extent)

        """ writing data"""
        dateCode = list(date)[-2] + list(date)[-1] if onlyYear else date.replace('.', '')
        sign ='_'+ resolutionSign[int(resolution)]+'_'

        fileStringTemp = name+ sign +location+'@'+ str(index)+'_'+dateCode+ '_grid.csv'
        fileString = fileStringTemp if not forMunicipalBudget else location + '.csv'
        output_file = open(setup.finalPath+'/' + fileString , 'w',newline='',encoding= 'utf-16')
        if not forMunicipalBudget:
            output_file.write("METADATA,TRUE"+ '\n')
            output_file.write("Layer Name,"+name+ '\n')
            if source.strip() and source != "Insert Source":
                output_file.write("Source," + source + '\n')
            if citation.strip() and citation != "Insert Citation":
                if mandatoryCitation:
                    output_file.write("MandatoryCitation,"+'"' + citation +'"' + '\n')
                else:
                    output_file.write("Citation," +'"' + citation + '"' +'\n')
            if link.strip() and link != "Insert Link":
                output_file.write("Link," + link + '\n')
            output_file.write("Colouring,"+"Multi"+ '\n') #+ defined by user
        else:
            output_file.write("METADATA,FALSE"+ '\n')
        
        """ write down category"""
        if (setup.isCategorized and not setup.isPoint ):
            output_file.write("CATEGORIES,TRUE"+ '\n')
            for i in range(len(setup.categories)):
                output_file.write(str(setup.categories[i] + ","+ str(i+1) + '\n'))
        else:
            output_file.write("CATEGORIES,FALSE"+ '\n')
        
        if not setup.isCategorized and setup.units.strip() and units != "Insert Units":
            output_file.write("Units,"+ setup.units + '\n')

        output_file.write("West,"+ str(minX) + '\n')
        output_file.write("North,"+str(minY)+ '\n')
        output_file.write("East,"+ str(maxX)+ '\n')
        output_file.write("South,"+ str(maxY)+ '\n')
        output_file.write("Count X," + str(setup.countX)+ '\n')
        output_file.write("Count Y," + str(setup.countY)+ '\n')
        output_file.write("VALUE,MASK" + '\n')
            
        values, masks = setup.band.data, setup.band.mask

        for y in range(0, setup.countY):
            for x in range(0, setup.countX): 

                if forMunicipalBudget: 
                    value= "-1" if masks[y,x] or values[y,x] == "0" else values[y,x]
                    output_file.write(value + "\n")
                else:
                    value = values[y,x] if not masks[y,x]  else "0"
                    mask = "1" if not masks[y,x] else "0"
                    output_file.write(value + "," + mask + "\n")


        print ("File patch "+ str(index)+" generated for " + location)
  
        """ close all and delete working dir"""
        output_file.close()
    
    def appendGraphFile(self,rasterMain, rasterVarify, feature):

        """ Get rasters and transformation information"""
        datasetMain = gdal.Open(rasterMain , GA_ReadOnly)
        datasetVarify = gdal.Open(rasterVarify , GA_ReadOnly)
        countX,countY = datasetMain.RasterXSize, datasetMain.RasterYSize
        countXVar,countYVar = datasetVarify.RasterXSize, datasetVarify.RasterYSize
        gt = datasetMain.GetGeoTransform()    
        minX, minY, w, h = gt[0], gt[3], gt[1], gt[5]

        """ get values as 2D array """
        dataMain = datasetMain.GetRasterBand(useBand).ReadAsArray(0, 0, countX, countY)
        dataVarify = datasetVarify.GetRasterBand(useBand).ReadAsArray(0, 0, countXVar, countYVar)
        
        lenght = resolutionLevels [int(resolution)]
        lenghtDiagonal = (lenght**2+lenght**2)**(.5)
        
        """ classification have to be in following format :  highway = 16, 
        highway link = 8, primary = 4, secondary = 2, other = 1
        AKA power over 2 defined by reversed position in networkMap Dictionary """ 
        power = list(reversed(list(networkMap.keys()))).index(feature)
        cl = str(2 ** power) # class is defined as incremental order of 1,2,4,8,16
        
        """if first time, then make header and overwrite old file"""
        output_file = open(self.path , 'a')

        """Metrix to check TopLeft, Top, TopRight, Left Cell"""
        metrix = [[-1, -1], [-1,0], [-1,1], [0,-1]] 
        isOldValue = [False,False,False,False]
        """ Check each cell and write nod if meet condition, skip edges rows and columns""" 
        for y in range(1, countY):
            for x in range(1, countX-1):
                isValue =  dataMain[y,x] > 0            

                if isValue:
                    """Get values to be writen as Source nod"""
                    source = str(y*(countX)+x)
                    halfW,halfH = w*0.5, h*0.5
                    X1,Y1 = str(minX + w*x + halfW),str(minY + h*y + halfH)

                    """ Get Values in next cells, Top Left, Top, Top Right, Left"""
                    isOtherValue = [dataMain[y + m[0] , x+ m[1]]>0 for m in metrix]
                    if self.oldGraphData is not None:
                        isOldValue = [self.oldGraphData[y + m[0] , x+ m[1]]>0 for m in metrix]
                
                    if isOtherValue[0] or isOldValue[0]:
                        dataCheck = dataVarify if isOtherValue[0] else self.oldGraphVarify
                    
                        edgeVarifyRD = dataCheck [(y-1)*3+2,(x-1)*3+2] > 0 
                        edgeVarifyDM = dataCheck [(y)*3,(x-1)*3+2] > 0 
                        edgeVarifyRM = dataCheck [(y-1)*3+2,(x)*3] > 0 

                        if edgeVarifyRD or edgeVarifyDM or edgeVarifyRM or cl== "16":   
                            """informations about Top Left nod"""
                            target = str(y*countX-countX+x-1)
                            X2, Y2 = str(minX + w*(x-1) + halfW),str(minY + h*(y-1)+ halfH)
                            #output_file.write(str(lenghtDiagonal)+";"+source+";" + target+";"+X1+";"+Y1+";"+X2+";"+Y2 + ";"+ cl+";"+"LINESTRING ("+X1+" " +Y1+","+ X2+ " "+Y2+")" + "\n")
                            output_file.write(str(lenghtDiagonal)+","+source+"," + target+","+X1+","+Y1+","+X2+","+Y2 + ","+ cl+ "\n")
                
                    if isOtherValue[1] or isOldValue[1]:
                        dataCheck = dataVarify if isOtherValue[1] else self.oldGraphVarify
                        edgeVarifyLD = dataCheck [(y-1)*3+2,(x)*3+0] > 0 
                        edgeVarifyMD = dataCheck [(y-1)*3+2,(x)*3+1] > 0 
                        edgeVarifyRD = dataCheck [(y-1)*3+2,(x)*3+2] > 0 

                        if edgeVarifyLD or edgeVarifyMD or edgeVarifyRD or cl== "16":   
                            """ informations about Top nod"""
                            target = str(y*countX-countX+x)
                            X2, Y2 = str(minX + w*(x) + halfW),str(minY + h*(y-1)+ halfH)
                            #output_file.write(str(lenght)+";"+source+";" + target+";"+X1+";"+Y1+";"+X2+";"+Y2 + ";"+ cl+";"+"LINESTRING ("+X1+" " +Y1+","+ X2+ " "+Y2+")" + "\n")
                            output_file.write(str(lenght)+","+source+"," + target+","+X1+","+Y1+","+X2+","+Y2 + ","+ cl+ "\n")
                
                    if isOtherValue[2] or isOldValue[2]:
                        dataCheck = dataVarify if isOtherValue[2] else self.oldGraphVarify
                        edgeVarifyLD = dataCheck [(y-1)*3+2,(x+1)*3+0] > 0 
                        edgeVarifyMD = dataCheck [(y-1)*3+2,(x)*3+2] > 0 
                        edgeVarifyLM = dataCheck [(y)*3+0,(x+1)*3+0] > 0 
    
                        if edgeVarifyLD or edgeVarifyMD or edgeVarifyLM or cl== "16":    
                            """ informations about Top Right nod"""
                            target = str(y*countX-countX+(x+1))
                            X2, Y2 = str(minX + w*(x+1) + halfW),str(minY + h*(y-1)+ halfH)
                            #output_file.write(str(lenghtDiagonal)+";"+source+";" + target+";"+X1+";"+Y1+";"+X2+";"+Y2 + ";"+ cl+";"+"LINESTRING ("+X1+" " +Y1+","+ X2+ " "+Y2+")" + "\n")
                            output_file.write(str(lenghtDiagonal)+","+source+"," + target+","+X1+","+Y1+","+X2+","+Y2 + ","+ cl+ "\n")
                    if isOtherValue[3] or isOldValue[3]:
                        dataCheck = dataVarify if isOtherValue[3] else self.oldGraphVarify
                        edgeVarifyRT = dataCheck [(y)*3+0,(x-1)*3+2] > 0 
                        edgeVarifyRM = dataCheck [(y)*3+1,(x-1)*3+2] > 0 
                        edgeVarifyRD = dataCheck [(y)*3+2,(x-1)*3+2] > 0 
                        if edgeVarifyRT or edgeVarifyRM or edgeVarifyRD or cl== "16": 
                            """ informations about left  nod"""
                            target = str(y*countX+(x-1))
                            X2, Y2 = str(minX + w*(x-1) + halfW),str(minY + h*(y)+ halfH)
                            #output_file.write(str(lenght)+";"+source+";" + target+";"+X1+";"+Y1+";"+X2+";"+Y2 + ";"+ cl+";"+"LINESTRING ("+X1+" " +Y1+","+ X2+ " "+Y2+")" + "\n")
                            output_file.write(str(lenght)+","+source+"," + target+","+X1+","+Y1+","+X2+","+Y2 + ","+ cl+ "\n")
  
        self.oldGraphData = dataMain.copy()
        self.oldGraphVarify = dataVarify.copy()

        output_file.close()      
    
    def getBand (self,raster,setup):
        """ get values"""
        ds = gdal.Open(raster , GA_ReadOnly)
        gt = ds.GetGeoTransform()
        band = ds.GetRasterBand(useBand)
        xCount,yCount = ds.RasterXSize,ds.RasterYSize
    
        """ for testing values """
        if debuggingMode:
            layerTesting = QgsRasterLayer(raster,"Raster for Band Extrapolation" )
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas
        
        """ get masked array"""
        bandDataAllTypes = band.ReadAsArray(0, 0, xCount, yCount)
        bandData =  bandDataAllTypes.astype('float')


        maskedData = numpy.ma.masked_values ( bandData,float('nan') )
        maskedData = numpy.ma.masked_invalid(maskedData)
        """Check if mask is array (noDataValue is not in dataset) and create if not"""

        if type(maskedData.mask)== numpy.bool_:
            maskedData.mask = numpy.ndarray(shape=(maskedData.data.size),dtype=bool)

        if forMunicipalBudget:
            dataInt = maskedData.astype(int)
            data = dataInt.astype(str)
        else:
            data = maskedData.astype(str)

        setup.band = data;
        setup.countX = xCount
        setup.countY = yCount
        setup.geoTransform = gt

    def getCleanExtent(self,setup,extent):
        if extentAsCanvas:
            """if it is for municipal Budget then is extend as canvas"""

            my_crs=QgsCoordinateReferenceSystem(4326)
            QgsProject.instance().setCrs(my_crs)
            ex = iface.mapCanvas().extent()
            minX=ex.xMinimum()
            minY=ex.yMinimum()
            maxX=ex.xMaximum()
            maxY=ex.yMaximum()
        else :
            multiplayer = 1 if resolutionEPSG[int(resolution)] == '4326'else 0.0000111 # 100000 stands for degree in metres
            cellSizeInDegree = resolutionLevels[int(resolution)]*multiplayer
            scientificNotation =  '%E' % cellSizeInDegree
            ndigitsString  =  scientificNotation.split("-")[-1] 
            ndigits = int(float(ndigitsString ))
            gt = setup.geoTransform
            
            minX=round(extent.xMinimum(),ndigits)
            minY=round(extent.yMinimum(),ndigits) if extent.yMinimum() < 85 else 85 # fixing maximal extent
            maxX=round(extent.xMaximum(), ndigits)
            maxY=round(extent.yMaximum(), ndigits)

        return minX,maxY,maxX,minY
      
class CheckLayer:
    "Check what type of file is layer (Raster, Vector, Network, MunicipalBudget)"
    
    def __init__(self, setup):
        if forReachability and setup.isVector: 
            self.graphLayer (setup)
        elif forMunicipalBudget and setup.isVector: 
            self.municipalBudgetLayer(setup)
        elif setup.isVector:     
            self.standartVectorLayer(setup)
        else:
            self.standartRasterLayer(setup)    
    
    def standartVectorLayer (self, setup):
        if setup.isVector :

            if setup.type == 0 : # points
                layerToCount= self.vectorToRaster(setup.layer, setup,True,1,"")
                if setup.isCanceledAndUpdateProgress(25.0): return None
                layerToProcess = self.countPointsInCell(layerToCount, setup)
                if setup.isCanceledAndUpdateProgress(50.0): return None
                rasterNoData = self.processNoData (setup,layerToProcess)
                if setup.isCanceledAndUpdateProgress(75.0): return None
                FileWriter(rasterNoData,setup)
                if setup.isCanceledAndUpdateProgress(100.0): return None

            elif setup.type == 1: # lines
                layerToRaster = self.fixGeometry(setup.layer)
                if setup.isCanceledAndUpdateProgress(25.0): return None
                rasterToProcess = self.vectorToRaster(layerToRaster,setup,True,1,"")
                if setup.isCanceledAndUpdateProgress(50.0): return None
                rasterNoData = self.processNoData (setup,rasterToProcess)
                if setup.isCanceledAndUpdateProgress(75.0): return None
                FileWriter(rasterNoData,setup)
                if setup.isCanceledAndUpdateProgress(100.0): return None

            elif setup.type == 2 : # polygons
                layerToRaster = self.fixGeometry(setup.layer)
                if setup.isCanceledAndUpdateProgress(25.0): return None
                rasterToProcess =self.vectorToRaster(layerToRaster,setup,False,1,"")
                if setup.isCanceledAndUpdateProgress(50.0): return None
                rasterNoData = self.processNoData (setup,rasterToProcess)
                if setup.isCanceledAndUpdateProgress(75.0): return None
                FileWriter(rasterNoData,setup)
                if setup.isCanceledAndUpdateProgress(100.0): return None

            else: # e.g WFS
                print(" I dont recognize this layer")
            
    def standartRasterLayer(self, setup):
        print("Ok, Seems that we are dealing with geo-tif. Here we go!")
        rasterNoData = self.processNoData (setup, setup.fullName)
        if setup.isCanceledAndUpdateProgress(25.0): return None
        if (setup.aggregate or setup.summary):
            rasterToWrite = self.aggregateAndSum(setup, rasterNoData)
        else:
            rasterToWrite = self.rasterToUnits(setup,rasterNoData)   
        if setup.isCanceledAndUpdateProgress(50.0): return None
        rasterToWrite = self.metresToDegress (rasterToWrite ,setup,1,"")
        if setup.isCanceledAndUpdateProgress(75.0): return None
        FileWriter(rasterToWrite,setup)
        if setup.isCanceledAndUpdateProgress(100.0): return None
    
    def processNoData(self,setup,path):
        #Get values list from raster
        inRaster = gdal.Open(path,GA_ReadOnly)
        
        # Prepare variables for saving temp files
        today = datetime.datetime.now().strftime("%Y%m%d_%H%M%S_")
        tempFolder = QgsProcessingUtils.tempFolder()
        filename, file_extension = os.path.splitext(path)
        
        inRaster = gdal.Open(path)

        # Transalte when dataset is not Geotiff because it can be scaled (e.g. NetCDF format)
        if  file_extension != ".tif":
            translatedPath = tempFolder + '/Translated_' + today + name
            inRaster = gdal.Translate(translatedPath,rawRaster,**{'unscale': True})
        
        countX = inRaster.RasterXSize
        countY = inRaster.RasterYSize

        inBand = inRaster.GetRasterBand(useBand)
        inData = inBand.ReadAsArray(0, 0, countX, countY)
        inDataFloat = numpy.array(inData , dtype='float') # always translate everything to float

        # trasnalte each value from noData to Not a Number value
        for inNoData in setup.noDataList:
            inDataFloat[inDataFloat== inNoData] = float('nan')  
        
        # Create Output Raster
        driver = gdal.GetDriverByName('GTiff')
        newRasterPath = tempFolder + '/' + today + name

        raster = driver.Create(newRasterPath, countX, countY, 1, gdal.GDT_Float64)
        raster.SetGeoTransform(inRaster.GetGeoTransform() )
        band = raster.GetRasterBand(1)
        band.SetNoDataValue(float('nan'))
        band.WriteArray(inDataFloat)
        rasterSRS = osr.SpatialReference()
        rasterSRS.ImportFromWkt(inRaster.GetProjection())
        raster.SetProjection(rasterSRS.ExportToWkt())
        band.FlushCache()
        band = None
        raster = None
        
        if debuggingMode:
            layerTesting = QgsRasterLayer(newRasterPath,"Raster From NoData")
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas 
        
        return newRasterPath
    
    def graphLayer (self, setup):
        print ("...working on Reachability Data")
        """ Get Graph's links for each separated class in the networkMap.
        LastKey mark the last list from networkMap which ignore values inside
        and rest safe as other """
        graphFile = FileWriter(None,setup)
        lastKey = list(networkMap)[-1]
        progressPercent = 100 / float(len(networkMap.items()))
        counter = 0
        for key, value in networkMap.items():
            layerToRaster = self.getSeparatedFeatureLayer(setup, value, key, lastKey)
            rasterToProcess = self.vectorToRaster(layerToRaster,setup, False, 3, key)
            rasterToCheck= self.vectorToRaster(layerToRaster,setup, False, 3, key)
            rasterToGraph = self.rasterToGraph(rasterToProcess ,setup)
            graphFile.appendGraphFile (rasterToGraph ,rasterToCheck ,key);

            if setup.isCanceledAndUpdateProgress(counter * progressPercent): return None
            counter += 1
            
        print("Done")    
    
    def municipalBudgetLayer (self, setup):

        if setup.type == 2:
            print ("...and seems like you plan to create data for Municipal Budget") 
            rasterToProcess =self.vectorToRaster(setup.layer, setup, False, 1, "mb")
            if setup.isCanceledAndUpdateProgress(33.0): return None
            rasterNoData = self.processNoData (setup,rasterToProcess)
            if setup.isCanceledAndUpdateProgress(66.0): return None
            FileWriter( rasterNoData,setup)
            if setup.isCanceledAndUpdateProgress(100.0): return None
            print ("Congratulations, you created special data for Municipal Budget.")
        else: 
            print ("You are trying to create Municipal Budget data from non-polygon type")
            print ("I am sorry Bill I can not let you do that")  
        
    def fixGeometry(self,layerIn):
    
        """ fix geometries... it is by default as far validating is slower anyway"""
        if activeGeometryFix:
            parameterReproject = { 'INPUT': layerIn,\
                            'OUTPUT': 'memory:'}
            result = processing.run('native:fixgeometries', parameterReproject)
            print ("Geometry Fixed.")
            return result['OUTPUT']
        else:
            return layerIn
        
    def vectorToRaster(self,layerInput,setup,makeBigger, resBoost, cat):
        # project layer for geting size of the cell in degress
        parameterReproject = { 'INPUT':layerInput,\
                            'TARGET_CRS': 'EPSG:4326' ,\
                            'OUTPUT': 'memory:'}
        result = processing.run('qgis:reprojectlayer', parameterReproject)
        reprojectedVector = result['OUTPUT']
        
        if debuggingMode:
            QgsProject.instance().addMapLayer(reprojectedVector) # adding to canvas
        
        # reproject separately extend (case of all points)
        parameterReprojectExtent = { 'INPUT': setup.layer,\
                                    'TARGET_CRS': 'EPSG:4326',\
                                    'OUTPUT': 'memory:'}
        result = processing.run('qgis:reprojectlayer', parameterReprojectExtent)
        reprojectedExtent = result['OUTPUT']
        
        if  debuggingMode:
            QgsProject.instance().addMapLayer(reprojectedExtent) # adding to canvas

        if (makeBigger): # in case of point and roads the raster extent must be bigger
            e = reprojectedExtent.extent()
            r = setup.res
            reprojectedExtent = QgsRectangle (e.xMinimum() -r, e.yMinimum()-r , e.xMaximum() +r, e.yMaximum()+r )
        
        if extentAsCanvas: # user can set the extent follow canvas extent
            #Set scale and extend to default CRS
            my_crs=QgsCoordinateReferenceSystem(4326)
            QgsProject.instance().setCrs(my_crs)
            scale = iface.mapCanvas().scale()
            tempExtent = iface.mapCanvas().extent()

            #Set scale and extend to requered CRS
            my_crs=QgsCoordinateReferenceSystem('EPSG:4326')
            QgsProject.instance().setCrs(my_crs)
            reprojectedExtent = iface.mapCanvas().extent()

            #Set scale and extend again to default CRS
            my_crs=QgsCoordinateReferenceSystem(4326)
            QgsProject.instance().setCrs(my_crs)
            iface.mapCanvas().setExtent(tempExtent)
            iface.mapCanvas().zoomScale(scale)
            iface.mapCanvas().refresh()

        reprojectedRaster = QgsProcessingUtils.tempFolder()+ "/" + cat + "Rasterized_Layer.tif"
        
        if os.path.isfile(reprojectedRaster):
            os.remove(reprojectedRaster)

        # create grided data. resBoost used by graph to  create higger resolution
        #For help--> processing.algorithmHelp("gdal:rasterize")"""
        
        parameterRasterize = {'INPUT': reprojectedVector,\
                      'FIELD': setup.getFieldCat(),\
                      'UNITS': 1,\
                      'WIDTH': setup.res / resBoost,\
                      'HEIGHT':setup.res / resBoost,\
                      'EXTENT':reprojectedExtent,\
                      'DATA_TYPE': 6,\
                      'INVERT': False,\
                      'INIT': float('nan'),\
                      'OUTPUT':reprojectedRaster }
        try:
            processing.run("gdal:rasterize",parameterRasterize)  
        except:
            processing.run("gdal:rasterize",parameterRasterize)
            print("Bug in QGIS after opening, but we've fixed it...")
        
        if debuggingMode:
            layerTesting = QgsRasterLayer(reprojectedRaster,"Rasterized layer")
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas

        return reprojectedRaster    
        
    def rasterToUnits(self,setup, raster):
        # change reolution first. For help--> processing.algorithmHelp("gdal:translate")
   
        reprojectedRaster = QgsProcessingUtils.tempFolder()+"/Reprojected_Layer.tif"
        if os.path.isfile(reprojectedRaster):
            os.remove(reprojectedRaster)
       
        inCRS = setup.layer.crs().authid()
        parameterWarp = {'INPUT': raster,\
                        'SOURCE_CRS': inCRS,\
                        'TARGET_CRS': 'EPSG:4326' ,\
                        'TARGET_RESOLUTION': setup.res,\
                        'NODATA':float('nan'),\
                        'RESAMPLING':resamplingMethod,\
                        'DATA_TYPE':6,\
                        'MULTITHREADING':False,\
                        'OUTPUT':reprojectedRaster}
        processing.run("gdal:warpreproject", parameterWarp)
        
        if not QgsRasterLayer(reprojectedRaster,"Reprojected Raster").isValid():
            print("Not enaugh storage for this size and resolution")
        
        if debuggingMode:
            layerTesting = QgsRasterLayer(reprojectedRaster,"Reprojected Raster")
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas

        return reprojectedRaster
        
    def metresToDegress (self, layerForWarp, setup, resolutionRatio, cat) : 
        
        if not setup.isInMetres:
            return layerForWarp # no need to translate to degress when already
          
        rasterDegress = QgsProcessingUtils.tempFolder()+"/"+ cat + "Raster_Degress.tif"
        if os.path.isfile(rasterDegress):
            os.remove(rasterDegress)
        
        if debuggingMode:
            layerTesting = QgsRasterLayer(layerForWarp,"layerForWarp")
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas    

        parameterWarp = {'INPUT': layerForWarp,\
                    'TARGET_CRS': 'EPSG:4326',\
                    'TARGET_RESOLUTION': 0,\
                    'RESAMPLING':resamplingMethod,\
                    'MULTITHREADING':False,\
                    'OUTPUT': rasterDegress}
         
        processing.run("gdal:warpreproject", parameterWarp)
        
        if debuggingMode:
            layerTesting = QgsRasterLayer(rasterDegress,"Raster in Degress")
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas
    
        return rasterDegress
    
    def countPointsInCell(self,raster,setup):
    
        """ reprojects points to same CRS as raster"""
    
        parameterReproject = { 'INPUT': setup.layer,\
                            'TARGET_CRS': 'EPSG:4326',\
                            'OUTPUT': "memory:rp"}
        rp = processing.run('qgis:reprojectlayer', parameterReproject)
        pl = rp["OUTPUT"]

        """ Get Transform of the ratser"""
        ds = gdal.Open(raster )
        cols,rows = ds.RasterXSize, ds.RasterYSize
        gt = ds.GetGeoTransform()
        # read array from cells
        data = ds.GetRasterBand(useBand).ReadAsArray(0, 0, cols,rows)
        valueData = data.copy()
        """make sure all values are 0 on start (case when category is mix of values and text)"""
        for y in range(0, rows):
            for x in range(0, cols):
                data[y, x] =0

        for y in range(0, rows):
            for x in range(0, cols):
                valueData[y, x] =float("nan")

        """ for each point in cell add 1 if not noDataValue"""
       
        for feature in pl.getFeatures():
            xP,yP = feature.geometry().asPoint()
            xR = int(math.floor((xP- gt[0])/gt[1]))
            yR = int(math.floor((yP - gt[3])/gt[5]))
            if (xR>=0 and yR>=0) and not (str(feature[field]) == noDataValue):
                data[yR,xR] = data[yR,xR]+1
        if not setup.isCategorized:
            for feature in pl.getFeatures():
                xP,yP = feature.geometry().asPoint()

                xR = int(math.floor((xP- gt[0])/gt[1]))
                yR = int(math.floor((yP - gt[3])/gt[5]))

                if(yR >= 0 and xR >=0): # in case some point are off extent
                    if not math.isnan(valueData[yR,xR]):
                        valueData[yR,xR] = (valueData[yR,xR]  + feature[field])*0.5
                    else:
                        valueData[yR,xR] = feature[field]
    
            data = valueData   
        newRaster= QgsProcessingUtils.tempFolder()+"/rasterForCountingPoints.tif"
        originX = gt[0]
        originY = gt[3]
        pixelWidth = gt[1]
        pixelHeight = gt[5]

        driver = gdal.GetDriverByName('GTiff')
        outRaster = driver.Create(newRaster, cols, rows, 1, gdal.GDT_Float64)
        outRaster.SetGeoTransform((originX, pixelWidth, 0, originY, 0, pixelHeight))
        outband = outRaster.GetRasterBand(useBand)
        outband.WriteArray(data)
        outRasterSRS = osr.SpatialReference()
        outRasterSRS.ImportFromWkt(ds.GetProjectionRef())
        outRaster.SetProjection(outRasterSRS.ExportToWkt())
        outband.FlushCache()
        outband = None
        outRaster = None

        if debuggingMode:
            layerTesting = QgsRasterLayer(newRaster,"Raster with counted points")
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas
    
        return newRaster
        
    def rasterToGraph(self,raster, setup):
       
        """ Get rasters and tranformation information"""
        dataset= gdal.Open(raster , GA_ReadOnly)

        countX,countY = dataset.RasterXSize, dataset.RasterYSize
        gt = dataset.GetGeoTransform()    
        minX, minY, w, h = gt[0], gt[3], gt[1], gt[5]

        """ get values as 2D array"""
        data = dataset.GetRasterBand(useBand).ReadAsArray(0, 0, countX, countY)
    
        newY , newX = int(countY/3), int(countX/3)
        newArray = [[0] * newX for i in range(newY)]

        """ check each cell and write nod if meet condition, skip edges rows and columns"""
        for y in range(0, int(countY/3)):
            for x in range(0, int(countX/3)):
                # check data in cell 3X3, skipping corners (more info in TS#56)
                tm = data[(y*3) + 0,(x*3)+1]>0
                ml = data[(y*3) + 1,(x*3)+0]>0
                mm = data[(y*3) + 1,(x*3)+1]>0
                mr = data[(y*3) + 1,(x*3)+2]>0
                dm = data[(y*3) + 2,(x*3)+1]>0 
                if tm or ml or mm or mr or dm:
                    value = 1
                else: 
                    value = 0 
                newArray[y][x] = value

        gtNew = [minX, w*3, 0, minY, 0, h*3]
        driver = gdal.GetDriverByName('GTiff')
        newOutputRaster = QgsProcessingUtils.tempFolder()+"Agregated_Raster.tif"
        dst_ds = driver.Create(newOutputRaster, int(countX/3), int(countY/3),  1, gdal.GDT_Int16)
        band = dst_ds.GetRasterBand(useBand)
        band.WriteArray(numpy.array( newArray) )
        band.SetNoDataValue(float("nan"))

        dst_ds.SetGeoTransform(gtNew)
        srs = osr.SpatialReference()
        srs.ImportFromEPSG(4326)

        dst_ds.SetProjection( srs.ExportToWkt() )
        dst_ds = None
        
        if debuggingMode:
            layerTesting = QgsRasterLayer(newOutputRaster,"Agregated raster")
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas
        
        return newOutputRaster 
    def getSeparatedFeatureLayer(self,setup, roadTypesList, key, lastKey):
        """ create temporary layer for each group of features"""    
        setCRS = setup.layer.crs().authid()
        layerTemp = QgsVectorLayer("LineString?crs="+setCRS,"LayerTemp", "memory")
        
        """ copy field names"""
        attr = setup.layer.dataProvider().fields().toList()
        dp = layerTemp.dataProvider()
        dp.addAttributes(attr)
        layerTemp.updateFields()

        """ add feature if in list and simuntaneosly add to ignore list for the
        last round when all what is not in ignore list will be added to other """
        features = []
        last = key == lastKey
        for feature in setup.layer.getFeatures():
            if not last and feature[field] in roadTypesList :
                features.append(feature)
            elif last and feature[field] not in roadTypesList:
                features.append(feature)
        dp.addFeatures(features)
        
        if not last: # ignore roadtypes added for previous category
            networkMap[lastKey].extend(roadTypesList)
    
        """add cotegory column """ 
        parameters = {'INPUT':layerTemp,\
              'FIELD_NAME':"catID",\
              'FIELD_TYPE': 0,\
              'FIELD_LENGTH':3,\
              'FIELD_PRECISION':2,\
              'OUTPUT': "memory:separateCat" ,\
                }
        SeparateCat = processing.run('qgis:addfieldtoattributestable', parameters)

        """chnage name for index""" 
        SeparateCat["OUTPUT"].startEditing()
        for feature in SeparateCat["OUTPUT"].getFeatures():
            colId = feature.fieldNameIndex("catID") 
            SeparateCat["OUTPUT"].changeAttributeValue(feature.id(),colId, 1)
        SeparateCat["OUTPUT"].commitChanges()
        
        if debuggingMode:
            QgsProject.instance().addMapLayer(layerTemp )
            
        return SeparateCat["OUTPUT"]  
    
    def aggregateAndSum(self, setup,rasterPath):
        # Make sure newResolution is positive and newResolutionY is negative!
        newResolutionX = abs(setup.aggregationRes)
        newResolutionY = -abs(setup.aggregationRes)

        ### Prepare Input Raster ###
        inRaster = gdal.Open(rasterPath,GA_ReadOnly)
        projRef = inRaster.GetProjection()
        
        inCountX = inRaster.RasterXSize
        inCountY = inRaster.RasterYSize
        gt = inRaster.GetGeoTransform()    
        inMinX = gt[0] 
        inMaxY = gt[3]
        inDegPerCellX = gt[1]
        inDegPerCellY = gt[5]

        # Fix input raster cell size signs
        if inDegPerCellX < 0:
            print("Changing negative cell width to positive")
            inMinX = inMinX + inDegPerCellX * inCountX
            inDegPerCellX = -inDegPerCellX
        if inDegPerCellY > 0:
            print("Changing positive cell height to negative")
            inMaxY = inMaxY + inDegPerCellY * inCountY
            inDegPerCellY = -inDegPerCellY

        # Make sure that the new raster is lower resolution than the input
        if newResolutionX < inDegPerCellX or abs(newResolutionY) < abs(inDegPerCellY):
            raise Exception("newResolution can't be smaller than the layer resolution: " + str(max(inDegPerCellX,inDegPerCellY)))

        inBand = inRaster.GetRasterBand(useBand)
        inData = inBand.ReadAsArray(0, 0, inCountX, inCountY)
        inData = numpy.array(inData , dtype='float') # always translate everything to float
        
        ### Use geolocator if inpt raster if in deggres and output resolution metres ###

        scaleX = newResolutionX / inDegPerCellX
        scaleY = newResolutionY / inDegPerCellY 

        ### Prepare Output Raster ###
        outMinX = inMinX
        outMaxY = inMaxY
        outCountX = int(math.floor(inCountX / scaleX))
        outCountY = int(math.floor(inCountY / scaleX))
        outDegPerCellX = inDegPerCellX * scaleX
        outDegPerCellY = inDegPerCellY * scaleY
        areaMultX = (1 / inDegPerCellX)
        areaMultY = (1 / inDegPerCellY)
        
        outData =  numpy.ndarray(shape=(outCountY,outCountX), dtype=numpy.dtype('f8'), order='F')
        ## no need for output data # if outputNoData is None: outputNoData = setup.noData
        outData[:] = float("nan")  # Initialize array with NoData

        inWidth = inCountX * inDegPerCellX
        inHeight = inCountY * inDegPerCellY
        outWidth = outCountX * outDegPerCellX
        outHeight = outCountY * outDegPerCellY
        sizePercent = 100 * (1 - (outWidth * outHeight) / (inWidth * inHeight))
        if sizePercent >= 1: # this means that the new raster will be 1% (or more) smaller
            print("\nWARNING: the output raster will be smaller (" + str(outWidth) + " by " + str(abs(outHeight)) + "). That's " + "{:.1f}".format(sizePercent).replace(".0", "") + "% smaller\n")

        # Go thru all cells in the output grid to calculate the aggregated value
        for outY in range(0, outCountY):

            # Calculate vertical extent (north, south) of output cell 
            outN = outMaxY + outY * outDegPerCellY
            outS = outN + outDegPerCellY
           
            # Calculate vertical range of affected input cells
            inFromY = int(math.floor(outY * scaleY))
            inToY = int(math.ceil((outY + 1) * scaleY))
            
            for outX in range(0, outCountX):
                # Calculate horizontal extent (west, east) of output cell
                outW = outMinX + outX * outDegPerCellX
                outE = outW + outDegPerCellX
            
                # Calculate horizontal range of affected input cells
                inFromX = int(math.floor(outX * scaleX))
                inToX = int(math.ceil((outX + 1) * scaleX))
        
                #print(">>>>>> Output[" + str(outX) + "," + str(outY) + "]")
                aggregatedValue = 0
                aggregatedSqKm = 0
                aggregatedRatio = 0
                # Go thru each Input grid cell that intersect with the Output grid cell
                for inY in range (abs(inFromY), abs(inToY)):
                    # Calculate vertical extent (north, south) of input cell
                    inN = inMaxY + inY * inDegPerCellY
                    inS = inN + inDegPerCellY
            
                    # Calculate vertical intersection between Input and Output cells
                    south = max(outS, inS)
                    north = min(outN, inN)

                    # Calculate height of the intersection area
                    if setup.inputCRS == 'EPSG:4326':
                        heightKm = geoCalculator().distanceBetweenLats(south, north);
                    else:
                        heightKm = (south - north) * 0.001; # translate from metres to Km
            
                    for inX in range(inFromX, inToX):
                        inValue = inData[inY,inX]
                        #print("   <<< Input[" + str(inX) + "," + str(inY) + "] = " + str(inValue))
                
                        if math.isnan(inValue):
                            if not convertNoData:
                                #print("   <<< Input[" + str(inX) + "," + str(inY) + "] = NO-DATA")
                                aggregatedSqKm = 0
                                inY = inToY  # Hack: break out of the second loop
                                break
                            else:
                                continue
                
                        # Calculate horizontal extent (west, east) of input cell
                        inW = inMinX + inX * inDegPerCellX
                        inE = inW + inDegPerCellX
                
                        # Calculate horizontal intersection between Input and Output cells
                        west = max(outW, inW)
                        east = min(outE, inE)
                
                        # Calculate intersection area
                        if setup.inputCRS == 'EPSG:4326':
                            widthKm = geoCalculator().distanceBetweenLons(west, east);
                        else:
                            widthKm = (west - east) * 0.001 # translate from metres to Km
                        
                        areaRatio = (float)((east-west) *areaMultX   * (south - north)* areaMultY);
                        
                        intersectionAreaSqKm = widthKm * heightKm
                        aggregatedSqKm += intersectionAreaSqKm
                        aggregatedRatio +=  areaRatio

                        if setup.isRelative :
                            aggregatedValue += inValue * intersectionAreaSqKm 
        
                        else:
                            aggregatedValue += inValue * areaRatio
       
                # Finally assign the aggregated average values or summarized values
                if aggregatedSqKm == 0:
                    outData[outY,outX] = float("nan")
                else:
                    if setup.isRelative and not setup.summary:
                        outData[outY,outX] = aggregatedValue  
                    elif setup.isRelative and setup.summary:
                        outData[outY,outX] = aggregatedValue * setup.unitsMultiply
                    elif not setup.isRelative and setup.summary  :
                        outData[outY,outX] = aggregatedValue 
                    elif not setup.isRelative and not setup.summary :
                        outData[outY,outX] = aggregatedValue / aggregatedRatio

        # Create Output Raster
        if projRef is None:
            projRef = 'GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0],UNIT["degree",0.0174532925199433],AUTHORITY["EPSG","4326"]]'
        driver = gdal.GetDriverByName('GTiff')
        today = datetime.datetime.now()
        newRasterPath = QgsProcessingUtils.tempFolder() + '/' + today.strftime("%Y%m%d_%H%M%S_") + name
        raster = driver.Create(newRasterPath, outCountX, outCountY, 1, gdal.GDT_Float64)
        raster.SetGeoTransform((outMinX, outDegPerCellX, 0, outMaxY, 0, outDegPerCellY))
        band = raster.GetRasterBand(1)
        band.SetNoDataValue(float("nan"))
        band.WriteArray(outData)
        rasterSRS = osr.SpatialReference()
        rasterSRS.ImportFromWkt(projRef)
        raster.SetProjection(rasterSRS.ExportToWkt())
        band.FlushCache()

        band = None
        raster = None
        if debuggingMode:
            layerTesting = QgsRasterLayer(newRasterPath,"Raster From Aggregate")
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas 
        
        return newRasterPath
        
class geoCalculator:
    EarthRadiusKm = 6378.137 # Radius of earth in kilometers
    Rad2Km = EarthRadiusKm 
    Deg2Rad = math.pi / 180.0
    Deg2HalfRad = math.pi / 360.0
    Deg2Km = Deg2Rad * Rad2Km 

    def convertLatToRadians(self,lat):
        return math.log(math.tan((90.0 + lat) * self.Deg2HalfRad))

    def distanceBetweenLats(self,lat1, lat2):
        r1 =self.convertLatToRadians(lat1) # lat1 in radians
        r2 = self.convertLatToRadians(lat2) # lat2 in radians
        return (r2 - r1) * self.Rad2Km      # Vertical distance in kilometers

    def distanceBetweenLons(self,lon1, lon2): # Should be distanceBetweenLats?
        return (lon2 - lon1) * self.Deg2Km  # Horizontal distance in kilometers
    
    def metressToDegressBetwenLons(self, distanceInMetres):
        return distanceInMetres/self.Deg2Km * 0.001 # to metres
    
    def areaInSqKm(self,lat1, lon1, lat2, lon2):
        dX = distanceBetweenLons(lon1, lon2)  # Horizontal distance in kilometers
        dY = distanceBetweenLats(lat1, lat2)  # Vertical distance in kilometers
        return dX * dY
        
    def measureInMetres(pointX1, pointY1,pointX2, pointY2):
        #Setup Measure tool
        d= QgsDistanceArea()
        crs = QgsCoordinateReferenceSystem()
        crs.createFromSrsId(3395) # 3857 pseudo mercator not in QGIS CRS default database using World Mercator EPSG:3395
        d.setSourceCrs(crs, QgsProject.instance().transformContext())  
        d.setEllipsoid('WGS84')
        measure = d.measureLine( QgsPointXY(pointX1,pointY1),QgsPointXY(pointX2,pointY2))
        return measure
        
    def measureInDegress(point1, point2):
        #Setup Measure tool
        d= QgsDistanceArea()
        crs = QgsCoordinateReferenceSystem()
        crs.createFromSrsId(3395) # 3857 pseudo mercator not in QGIS CRS default database using World Mercator EPSG:3395
        d.setSourceCrs(crs, QgsProject.instance().transformContext())  
        d.setEllipsoid('WGS84')
        measure = d.convertLengthMeasurement(point2-point1, QgsUnitTypes.DistanceUnit.DistanceDegrees)
        return measure
        
if not isPlugin :
    Exporter()

# This version is from 23.07.2020 - not for public
