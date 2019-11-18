"""---------------------------------------------------------------------
Please set basic parameters
---------------------------------------------------------------------"""
outputPath = "Insert Data Path "  # Do not use any backslash (e.g. C:/Documents/ur-scape/Data)
name = "Insert Layer Name" # Set layer name in Camel case (e.g. Population Density)
field = "Insert Field Name" # Name of the field to be used from shapefile. Will be ignored if raster.
resolution = "1" # 0-Neighbourhood, 1-City, 2-Metropolitan, 3-National, 4-Continental, 5-Global

"""Please set metadata"""
units = "Insert Units" # set layer units (e.g. People per Sq km)
location = "Insert Site Name" # set location of layer (e.g. Palembang)
source = "Insert Source" # set source of layer (e.g. Statistics Bureau)
date = "YYYY.MM.DD" # Year of data in format YYYY.MM.DD (month and day are optional)
color = "1" # 0=Red, 1=Orange, 2=Yellow, 3=Light Green , 4 = Dark Green, 5 = Cyan, 6 = Light Blue, 7 = Dark Blue, 8 = Purple, 9 = Pink
group = "Exported from QGIS"
citation = "Insert Citation" # insert citation - 
mandatoryCitation = False # should the citation be always shown?
link = "Insert Link" # link to dataset if aplied 
"""---------------------------------------------------------------------
Please set advanced parameters below only if you are advanced user
---------------------------------------------------------------------"""
noDataValue = "n/a"
extentAsCanvas = False
resolutionPatch = [1, 2, 5 , 10, 50, 360] # in degrees units
resolutionLevels = [5, 100, 1000, 0.0083333, 0.08333333, 0.416] 
# resolution is in [metres, metres, metres, degrees, degrees,degrees,]

forMunicipalBudget = False# Create basic data for municipal budget
forReachability = False # Create basic data for reachability
resamplingMethod = 0 # 0 Nearest neighbor, 1 Bilinear, 5 average, 7 Maximum, 8 minimum

 #version for indonesia

networkMap ={"Other":["Lokal primer","Lokal sekunder"],\
             "Secondary":["Arteri sekunder","Kolektor sekunder"],\
             "Primary":["Arteri primer","Kolektor primer"],\
             "Highway Link":["Tautan"],\
             "Highway":["Tol"]}   
"""
 #version for OSM   
networkMap ={"Other":[""],\
             "Secondary":["secondary"],\
             "Primary":["primary"],\
             "Highway Link":["motorway_link", "trunk_link"],\
             "Highway":["motorway","trunk"]}     
"""
"""---------------------------------------------------------------------
Setup for developer
---------------------------------------------------------------------"""
resolutionEPSG = ['3857', '3857', '3857', '4326', '4326', '4326'] # '3857'= metres, '4326' = degress
resolutionSign = ['D', 'D', 'D', 'C', 'B', 'A' ]
debuggingMode = True
onlyYear = False

"""---------------------------------------------------------------------
You can't touch this
---------------------------------------------------------------------"""
import gdal, os, sys, osr, shutil, processing, csv, math, colorsys,traceback,numpy
from tempfile import mkstemp
from gdalconst import *
from qgis.core import QgsProject

gdal.AllRegister()

CHECK_DISK_FREE_SPACE = False

class Exporter:
    "This class will export data to urscape"
    
    def __init__(self):
        setup = Setup()
        if not setup.hasProblem():
            try:
                CheckLayer(setup)
            except Exception as error:
                self.handleError (error)
            
    def handleError(self, error):
        print ("Oops, please following report error to developer:")
        tbl = traceback.format_exc().splitlines() 
        print(tbl[-1] + " in line:" +tbl[-2].split("line")[-1])
        if debuggingMode:
            raise  
 
class Setup:
    "this class include basic setup"
    def __init__(self):
        self.layer = iface.activeLayer()
        self.fullName = self.layer.dataProvider().dataSourceUri()
        self.units = units
        self.noData = self.setNoData()
        self.problem = self.primaryCheck()
        if not self.problem:
            self.updateResolution()
            self.updatePath()
            self.updateType()
            self.updateCategory()

    def setNoData(self):
        if str(noDataValue).isdigit():
            return  float(noDataValue)  
        else: 
            return  -sys.float_info.max

    
    def hasProblem(self):
        return  self.secondaryCheck(self.problem)
    
    def updateResolution(self):    
        self.res = resolutionLevels[int(resolution)]
        self.epsg = 'EPSG:'+ resolutionEPSG[int(resolution)]
        self.maxPatchSize = resolutionPatch[int(resolution)]

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
            
            self.isCategorized = self.layer.fields().field(field).type()==10
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
            noCommaInCategory = str(feature[field]).split(',')
            cleanRecord = noCommaInCategory[0].strip()
            if not cleanRecord in categories:
                categories.append(cleanRecord)
        categories.sort()  
        
        """ write shorted values"""
    
        catLayer.startEditing()
        for feature in catLayer.getFeatures():  
            colId = feature.fieldNameIndex("catID") 
            noCommaInCategory = str(feature[field]).split(',')
            categoryId = categories.index(noCommaInCategory[0].strip())
            catLayer.changeAttributeValue(feature.id(),colId, categoryId+1)
            """mask out if noDataValue same as category name"""
            if str(feature[field]) == noDataValue:
                self.noData = categoryId+1

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
        return problem
    "Check more generic data"      
    def secondaryCheck(self, problem):
        problem = self.testFiles(problem)
        problem = self.testScenarios(problem)
        if isinstance(iface.activeLayer() ,QgsVectorLayer) :  
            problem = self.testMunicipalBudget(problem)
 
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
        try:
            if not self.testPath(False):
                test = open(outputPath + "/layers.csv", 'r',newline='') 
            return problem
        except:
            print( "Opps, Could not open file! Please close Layers.CSV!")
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
            
class LayerWriter:
    "This class handle writing layers to Layers.csv"
    def __init__(self,name,group):
        path = self.findLayersCsv()
        if path:
            self.checkName(path)    
     
    def checkName (self, path):
        encoding = self.getEncoding(path)
        with open(path, 'r',newline='',encoding= encoding ) as csvfile:
            spamreader = csv.reader(csvfile,delimiter = ',')
            list = [ x for x in spamreader]  
            if not any(name in s for s in list):
                self.addLayerToList(path,name, group,list)  
                print("Writting layer " + name + " to the file: Layers.csv")  
            else:
                print ("It seems like we already have layer " + name +" in the file: Layers.csv.")      

    def addLayerToList (self, path, name,group,layerList):
        color = self.getColorRGB()
        index = len(layerList) # by default it write layer to the end
        for layer in  layerList: # check if group is in the list
            if any(group in s for s in layer):
                index = layerList.index(layer) 
        if index < len (layerList): # if group exist write under the index of the group
            layerList.insert (index+1, ['Layer',name,color[0],color[1],color[2]])
        else:     # append new group and layer on the end
            layerList.append(['','','','',''])
            layerList.append(['Group',group,'','',''])
            layerList.append ( ['Layer',name,color[0],color[1],color[2]])

        with open(path, 'w',newline='',encoding= 'utf-16') as csvfile:
            spamwriter = csv.writer( csvfile,delimiter=',')
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
        c = layerHVS = [int(color)/10, 1, 1 ] # prepare full HSV color
        colorRGB = colorsys.hsv_to_rgb(c[0],c[1],c[2])
        colorOut = [int(band*255)for band in colorRGB ]    

        return  colorOut[0],colorOut[1],colorOut[2]
        
    def getEncoding(self, path):
        try: 
            with open(path, 'r',newline='',encoding= 'utf-8') as csvfile:
                spamreader = csv.reader(csvfile,delimiter = ',')
                list = [ x for x in spamreader]  
            return 'utf-8';
        except:
            return 'utf-16le';

class FileWriter:
    "Create Csv File"
    
    def __init__(self,raster, setup):
        if forReachability:
            """Create name for graph file"""
            dateCode = list(date)[-2] + list(date)[-1] if onlyYear else date.replace('.', '')
            fileName = name + '_D_'+location+'_'+dateCode+ '_graph.csv'
            self.path = setup.finalPath+'/' +fileName
            output_file = open(self.path, 'w')
            #output_file.write("lenght;source;target;x1;y1;x2;y2;classification;WKT" + "\n") # for testing network in QGIS only
            output_file.write("lenght,source,target,x1,y1,x2,y2,classification" + "\n")
        
            self.oldGraphData = None
            self.oldGraphVarify = None 
            
        else:    
            extents = self.getExtents(raster,setup) 
            for i in range (0,len(extents)): 
                rasterExtent = self.clipRaster(raster, extents[i],i,0)
                self.getBand(rasterExtent,setup)
            
                self.writeGridToFile(i,setup)
                
            LayerWriter(name, group)
            print ("Done here, have a great day")
    
    
    def getExtents(self,raster,setup):
        fileInfo = QFileInfo(raster)
        rlayer = QgsRasterLayer(raster, fileInfo.baseName())
        if extentAsCanvas:
            my_crs=QgsCoordinateReferenceSystem(4326)
            QgsProject.instance().setCrs(my_crs)
            ex = iface.mapCanvas().extent()
        else:
            ex = rlayer.extent()

        xminFloor = math.floor(ex.xMinimum()) 
        xmaxCeil = math.ceil(ex.xMaximum()) 
        yminCliped = ex.yMinimum() if ex.yMinimum() > -85 else -85
        ymaxCliped = ex.yMaximum() if ex.yMaximum() < 85 else 85
        yminFloor = math.floor(yminCliped) 
        ymaxCeil = math.ceil(ymaxCliped ) 

        extends = []
        xRange = xmaxCeil-xminFloor
        yRange = ymaxCeil-yminFloor
        i =0
        maxPatchSize = setup.maxPatchSize
        res = setup.res
        """ get extend for each 1 degree by 1 degree square"""
   
        if (( xRange > maxPatchSize) or  (yRange > maxPatchSize)) and not extentAsCanvas:

            for x in numpy.arange (xminFloor, xmaxCeil, maxPatchSize ):
                for y in numpy.arange (yminFloor, ymaxCeil,  maxPatchSize) :
                
                    xminE = ex.xMinimum() if (ex.xMinimum()>x) else x
                    xmaxE = ex.xMaximum() if (ex.xMaximum()<(x+ maxPatchSize)) else (x + maxPatchSize )
                    yminE = yminCliped if (yminCliped>y) else y
                    ymaxE = ymaxCliped  if (ymaxCliped< (y+ maxPatchSize) ) else (y + maxPatchSize )
                
                    trueRes = res * 0.00001 if int(resolution) <= 2 else res # to make sure the patch is bigger then cell size
                
                    if(xmaxE - xminE) > trueRes and (ymaxE - yminE) > trueRes:
                        extent = QgsRectangle (xminE, yminE, xmaxE, ymaxE)
                        extends.append (extent)

        else:
            extent = QgsRectangle (ex.xMinimum(), ymaxCliped , ex.xMaximum(), yminCliped)
            extends.append(extent)

        return extends
    
    def clipRaster (self,rasterIn, extent, index, cat ):
        clipRaster = QgsProcessingUtils.tempFolder() + "/Clipped_raster.tif"
    
        # processing.algorithmHelp('gdal:cliprasterbyextent')
        parameterClip = { 'INPUT': rasterIn,\
                        'PROJWIN': extent,\
                        #'DATA_TYPE':6,\
                        'OUTPUT': clipRaster}
        processing.run('gdal:cliprasterbyextent', parameterClip)
        """ one more resample to make sure Xcount and Y are alway same, e.g for municipal Budget"""
    
        if debuggingMode:
            layerTesting = QgsRasterLayer(clipRaster,"Clip Raster" + (str)(index))
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas
     
        return clipRaster 
    
    def writeGridToFile(self,index,setup):
        """ get info about size and position"""
        minX,minY,maxX,maxY = self.getCleanExtent(setup)

        """ writing data"""
        dateCode = list(date)[-2] + list(date)[-1] if onlyYear else date.replace('.', '')
        sign ='_'+ resolutionSign[int(resolution)]+'_'

        fileStringTemp = name+ sign +location+'@'+ str(index)+'_'+dateCode+ '_grid.csv'
        fileString = fileStringTemp if not forMunicipalBudget else location + '.csv'
        output_file = open(setup.finalPath+'/' + fileString , 'w')

        output_file.write("METADATA,TRUE"+ '\n')
        output_file.write("Layer Name,"+name+ '\n')
        if source.strip() and source != "Insert Source":
            output_file.write("Source," + source + '\n')
        if citation.strip() and citation != "Insert Citation":
            if mandatoryCitation:
                output_file.write("MandatoryCitation," + citation + '\n')
            else:
                output_file.write("Citation," + citation + '\n')
        if link.strip() and link != "Insert Link":
            output_file.write("Link," + link + '\n')
        output_file.write("Colouring,"+"Multi"+ '\n') #+ defined by user
    
        """ write down category"""
        if (setup.isCategorized):
            output_file.write("CATEGORIES,TRUE"+ '\n')
            for i in range(len(setup.categories)):
                categoryNameASCII = setup.categories[i].encode("ascii","replace")
                categoryName = categoryNameASCII.decode('UTF-8') 
                output_file.write(str(categoryName + ","+ str(i+1) + '\n'))
        else:
            output_file.write("CATEGORIES,FALSE"+ '\n')
   
        if not forMunicipalBudget:
            if not setup.isCategorized and units.strip() and units != "Insert Units":
                output_file.write("Units,"+ units + '\n')
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
                value = values[y,x] if not masks[y,x] else "0"
                mask = "1" if not masks[y,x] or setup.isPoint else "0"
            
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
        dataMain = datasetMain.GetRasterBand(1).ReadAsArray(0, 0, countX, countY)
        dataVarify = datasetVarify.GetRasterBand(1).ReadAsArray(0, 0, countXVar, countYVar)

        lenght = resolutionLevels [int(resolution)]
        lenghtDiagonal = (lenght**2+lenght**2)**(.5)
        """ check what classification is curently generated """
        power = 0
        for key in networkMap:
            keyList = networkMap[key]
            for subKey in keyList:
                if str(subKey).lower().strip() in feature.lower().strip():
                    power = list(networkMap.keys()).index(key)
                
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
                            output_file.write(str(lenght)+","+source+"," + target+","+X1+","+Y1+","+X2+","+Y2 + ","+ cl+ "\n")
                
                    if isOtherValue[1] or isOldValue[1]:
                        dataCheck = dataVarify if isOtherValue[1] else self.oldGraphVarify
                        edgeVarifyLD = dataCheck [(y-1)*3+2,(x)*3+0] > 0 
                        edgeVarifyMD = dataCheck [(y-1)*3+2,(x)*3+1] > 0 
                        edgeVarifyRD = dataCheck [(y-1)*3+2,(x)*3+2] > 0 

                        if edgeVarifyLD or edgeVarifyMD or edgeVarifyRD or cl== "16":   
                            """ informations about Top nod"""
                            target = str(y*countX-countX+x)
                            X2, Y2 = str(minX + w*(x) + halfW),str(minY + h*(y-1)+ halfH)
                            #output_file.write(str(lenghtDiagonal)+";"+source+";" + target+";"+X1+";"+Y1+";"+X2+";"+Y2 + ";"+ cl+";"+"LINESTRING ("+X1+" " +Y1+","+ X2+ " "+Y2+")" + "\n")
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
                            #output_file.write(str(lenghtDiagonal)+";"+source+";" + target+";"+X1+";"+Y1+";"+X2+";"+Y2 + ";"+ cl+";"+"LINESTRING ("+X1+" " +Y1+","+ X2+ " "+Y2+")" + "\n")
                            output_file.write(str(lenghtDiagonal)+","+source+"," + target+","+X1+","+Y1+","+X2+","+Y2 + ","+ cl+ "\n")
  
        self.oldGraphData = dataMain.copy()
        self.oldGraphVarify = dataVarify.copy()

        output_file.close()      
    
    def getBand (self,raster,setup):
        """ get values"""
     
        ds = gdal.Open(raster , GA_ReadOnly)
        gt = ds.GetGeoTransform()
        band = ds.GetRasterBand(1)
        xCount,yCount = ds.RasterXSize,ds.RasterYSize
    
        """ for testing values TODO to special function"""
        if debuggingMode:
            layerTesting = QgsRasterLayer(raster,"Raster for Band Extrapolation" )
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas
        
        """ get masked array"""
        bandData = band.ReadAsArray(0, 0, xCount, yCount)
        maskedData = numpy.ma.masked_values ( bandData,setup.noData)

        """Check if mask is array (noDataValue is not in dataset) and create if not"""
        if type(maskedData.mask)== numpy.bool_:
            maskedData.mask = numpy.ndarray(shape=(maskedData.data.size),dtype=bool)

        setup.band = maskedData.astype(str)
        setup.countX = xCount
        setup.countY = yCount
        setup.geoTransform = gt

    def getCleanExtent(self,setup):
        if extentAsCanvas:
            """if it is for municipal Budget then is extend as canvas"""

            my_crs=QgsCoordinateReferenceSystem(4326)
            QgsProject.instance().setCrs(my_crs)
            ex = iface.mapCanvas().extent()
            minX=ex.xMinimum()
            minY=ex.yMaximum()
            maxX=ex.xMaximum()
            maxY=ex.yMinimum()
            print ("COUNTX,COUNTY: " + str(setup.countX) +","+str(setup.countY) + ". has your budget data same?")
        else :
            multiplayer = 1 if resolutionEPSG[int(resolution)] == '4326'else 0.00001 # 100000 stands for degree in metres
            cellSizeInDegree = resolutionLevels[int(resolution)]*multiplayer
            scientificNotation =  '%E' % cellSizeInDegree
            ndigitsString  =  scientificNotation.split("-")[-1] 
            ndigits = int(float(ndigitsString ))
            gt = setup.geoTransform
            minX=round(gt[0],ndigits)
            minY=round(gt[3],ndigits) if gt[3] < 85 else 85 # fixing maximal extent
            maxX=round(gt[0]+gt[1]*setup.countX, ndigits)
            maxY=round(gt[3]+gt[5]*setup.countY, ndigits)

        return minX,minY,maxX,maxY
      
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
                layerToDegress= self.countPointsInCell(layerToCount, setup)
                rasterToGrid = self.metresToDegress (layerToDegress, setup,1,"")
                FileWriter(rasterToGrid,setup)

            elif setup.type == 1: # lines
                layerToRaster = self.fixGeometry(setup.layer)
                rasterToDegress = self.vectorToRaster(layerToRaster,setup,False,1,"")
                rasterToGrid = self.metresToDegress (rasterToDegress, setup,1,"")
                FileWriter(rasterToGrid,setup)

            elif setup.type == 2 : # polygons
                layerToRaster = self.fixGeometry(setup.layer)
                rasterToDegress =self.vectorToRaster(layerToRaster,setup,False,1,"")
                rasterToGrid = self.metresToDegress (rasterToDegress, setup,1,"") 
                FileWriter(rasterToGrid,setup)

            else: # e.g WFS
                print(" I dont recognize this layer")
            
    def standartRasterLayer(self, setup):
        print("Ok, Seems that we are dealing with geo-tif. Here we go!")

        rasterToDegress= self.rasterToUnits(setup)
        rasterToGrid = self.metresToDegress (rasterToDegress,setup,1,"")
        FileWriter(rasterToGrid,setup)
            
    def graphLayer (self, setup):
        print ("...working on Reachability Data")
            
        """ Get Graph's links for each separated class in the network"""
        graphFile = FileWriter(None,setup)
        for category in reversed(setup.categories):
            layerToRaster = self.getSeparatedFeatureLayer(setup, category)
            rasterToDegress = self.vectorToRaster(layerToRaster,setup, False, 3, category)   
            rasterToCheck= self.metresToDegress (rasterToDegress, setup, 3, category) 
            rasterToGraph = self.aggregateRaster(rasterToCheck,setup)
            graphFile.appendGraphFile (rasterToGraph ,rasterToCheck ,category);
    
    def municipalBudgetLayer (self, setup):

        if setup.type == 2:
            print ("...and seems like you plan to create data for Municipal Budget") 
            layerToWarp =self.vectorToRaster(setup.layer, setup, False, 1, "mb")
            rasterToGrid = self.metresToDegress (layerToWarp,setup,1, "mb") 
            FileWriter(rasterToGrid,setup)
            print ("Congratulations, you created special data for Municipal Budget.")
        else: 
            print ("You are trying to create Municipal Budget data from non-polygon type")
            print ("I am sorry Bill I can not let you do that")  
        
    def fixGeometry(self,layerIn):
    
        """ fix geometries... it is by default as far validating is slower anyway"""
 
    
        parameterReproject = { 'INPUT': layerIn,\
                            'OUTPUT': 'memory:'}
        result = processing.run('native:fixgeometries', parameterReproject)
        return result['OUTPUT']

        print ("Geometry Fixed. Just in Case.")
        
    def vectorToRaster(self,layerInput,setup,makeBigger, resolutionRatio, cat):
        """ project layer for geting size of the cell in metres"""
        parameterReproject = { 'INPUT':layerInput,\
                            'TARGET_CRS': setup.epsg ,\
                            'OUTPUT': 'memory:'}
        result = processing.run('qgis:reprojectlayer', parameterReproject)
        reprojectedVector = result['OUTPUT']
    
        if debuggingMode:
            QgsProject.instance().addMapLayer(reprojectedVector) # adding to canvas
        
        """ reproject separately extend (case of all points)"""
        parameterReprojectExtent = { 'INPUT': setup.layer,\
                                    'TARGET_CRS': setup.epsg ,\
                                    'OUTPUT': 'memory:'}
        result = processing.run('qgis:reprojectlayer', parameterReprojectExtent)
    
        reprojectedExtent = result['OUTPUT']
        if  debuggingMode:
            QgsProject.instance().addMapLayer(reprojectedExtent) # adding to canvas
        
        try: # TODO change this to setup controled
            fieldCat = "catID" if setup.isCategorized  else field
        except:
            fieldCat = field
              
        """ exception for point without specifiing field """

        try: # check field name input
            if  hasattr(iface.activeLayer(), "fileds"):
                test02 = iface.activeLayer().fields().field(field)
        except KeyError:
            field_names = [field.name() for field in layerIn.fields() ]
            fieldCat = field_names[0]
  
        if (makeBigger):
            e = reprojectedExtent.extent()
            r = setup.res
            reprojectedExtent = QgsRectangle (e.xMinimum() -r, e.yMinimum()-r , e.xMaximum() +r, e.yMaximum()+r )
        if extentAsCanvas: 
            """Set scale and extend to default CRS"""
            my_crs=QgsCoordinateReferenceSystem(4326)
            QgsProject.instance().setCrs(my_crs)
            scale = iface.mapCanvas().scale()
            tempExtent = iface.mapCanvas().extent()

            """Set scale and extend to requered CRS"""
            my_crs=QgsCoordinateReferenceSystem(setup.epsg)
            QgsProject.instance().setCrs(my_crs)
            reprojectedExtent = iface.mapCanvas().extent()

            """Set scale and extend again to default CRS"""
            my_crs=QgsCoordinateReferenceSystem(4326)
            QgsProject.instance().setCrs(my_crs)
            iface.mapCanvas().setExtent(tempExtent)
            iface.mapCanvas().zoomScale(scale)
            iface.mapCanvas().refresh()

        reprojectedRaster = QgsProcessingUtils.tempFolder()+ "/" + cat + "Rasterized_Layer.tif"
        if os.path.isfile(reprojectedRaster):
            os.remove(reprojectedRaster)

        """ create grided data. For help--> processing.algorithmHelp("gdal:rasterize")"""
        parameterRasterize = {'INPUT': reprojectedVector,\
                      'FIELD': fieldCat,\
                      'UNITS': 1,\
                      'WIDTH': setup.res / resolutionRatio,\
                      'HEIGHT':setup.res / resolutionRatio,\
                      'EXTENT':reprojectedExtent,\
                      'DATA_TYPE': 6,\
                      'INVERT': False,\
                      'INIT': setup.noData,\
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
        
    def rasterToUnits(self,setup):
        # change reolution first. For help--> processing.algorithmHelp("gdal:translate")
   
        reprojectedRaster = QgsProcessingUtils.tempFolder()+"/Reprojected_Layer.tif"
        if os.path.isfile(reprojectedRaster):
            os.remove(reprojectedRaster)
       
        inCRS = setup.layer.crs().authid()

        parameterWarp = {'INPUT': setup.layer,\
                        'SOURCE_CRS': inCRS,\
                        'TARGET_CRS': setup.epsg ,\
                        'TARGET_RESOLUTION': setup.res,\
                        'NODATA':setup.noData,\
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
        rasterDegress = QgsProcessingUtils.tempFolder()+"/"+ cat + "Raster_Degress.tif"
        if os.path.isfile(rasterDegress):
            os.remove(rasterDegress)
        parameterWarp = {'INPUT': layerForWarp,\
                    'SOURCE_CRS': setup.epsg ,\
                    'TARGET_CRS': 'EPSG:4326',\
                    'TARGET_RESOLUTION': 0,\
                    #'NODATA':setup.noData,\
                    'RESAMPLING':resamplingMethod,\
                    #'DATA_TYPE':6,\
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
                            'TARGET_CRS': setup.epsg ,\
                            'OUTPUT': "memory:rp"}
        rp = processing.run('qgis:reprojectlayer', parameterReproject)
        pl = rp["OUTPUT"]

        """ Get Transform of the ratser"""
        ds = gdal.Open(raster )
        cols,rows = ds.RasterXSize, ds.RasterYSize
        gt = ds.GetGeoTransform()
        # read array from cells
        data = ds.GetRasterBand(1).ReadAsArray(0, 0, cols,rows)
    
        """make sure all values are 0 on start (case when category is mix of values and text)"""
        for y in range(0, rows):
            for x in range(0, cols):
                data[y, x] =0
        valueData = data.copy()
        
        """ for each point in cell add 1 if not noDataValue"""
       
        for feature in pl.getFeatures():
            xP,yP = feature.geometry().asPoint()
            xR = int(math.floor((xP- gt[0])/gt[1]))
            yR = int(math.floor((yP - gt[3])/gt[5]))
            if (xR>=0 and yR>=0) and not (str(feature[field]) == noDataValue):
                data[yR,xR] = data[yR,xR]+1
        if setup.isCategorized:
            for feature in pl.getFeatures():
                xP,yP = feature.geometry().asPoint()

                xR = int(math.floor((xP- gt[0])/gt[1]))
                yR = int(math.floor((yP - gt[3])/gt[5]))

                if(yR >= 0 and xR >=0): # in case some point are of extent
                    if not (data[yR,xR]) ==0:
                        valueData[yR,xR] = valueData[yR,xR]  +feature[field]/ (data[yR,xR])
                    else:
                        valueData[yR,xR] = 0   
    
            data = valueData   

        newRaster= QgsProcessingUtils.tempFolder()+"/rasterForCountingPoints.tif"
        originX = gt[0]
        originY = gt[3]
        pixelWidth = gt[1]
        pixelHeight = gt[5]

        driver = gdal.GetDriverByName('GTiff')
        outRaster = driver.Create(newRaster, cols, rows, 1, gdal.GDT_Float32)
        outRaster.SetGeoTransform((originX, pixelWidth, 0, originY, 0, pixelHeight))
        outband = outRaster.GetRasterBand(1)
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
        
    def aggregateRaster(self,raster, setup):
       
        """ Get rasters and tranformation information"""
        dataset= gdal.Open(raster , GA_ReadOnly)

        countX,countY = dataset.RasterXSize, dataset.RasterYSize
        gt = dataset.GetGeoTransform()    
        minX, minY, w, h = gt[0], gt[3], gt[1], gt[5]

        """ get values as 2D array"""
        data = dataset.GetRasterBand(1).ReadAsArray(0, 0, countX, countY)
    
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
        newOutputRaster = QgsProcessingUtils.tempFolder()+"Agregated_Ratser.tif"
        dst_ds = driver.Create(newOutputRaster, int(countX/3), int(countY/3),  1, gdal.GDT_Int16)
        band = dst_ds.GetRasterBand(1)
        band.WriteArray(numpy.array( newArray) )
        band.SetNoDataValue(setup.noData)

        dst_ds.SetGeoTransform(gtNew)
        srs = osr.SpatialReference()


        srs.ImportFromEPSG(4326)

        dst_ds.SetProjection( srs.ExportToWkt() )
        dst_ds = None
        
        if debuggingMode:
            layerTesting = QgsRasterLayer(newOutputRaster,"Agregated raster")
            QgsProject.instance().addMapLayer( layerTesting ) # adding to canvas
        
        return newOutputRaster 
    
    def getSeparatedFeatureLayer(self,setup, category):
        """ create temporary layer for each group of features"""    
        setCRS = setup.layer.crs().authid()
        layerTemp = QgsVectorLayer("LineString?crs="+setCRS,"LayerTemp", "memory")
        
        """ copy field names"""
        attr = setup.layer.dataProvider().fields().toList()
        dp = layerTemp.dataProvider()
        dp.addAttributes(attr)
        layerTemp.updateFields()

        """ keep only relevent features """
        features = []
        for feature in setup.layer.getFeatures():
            if feature[field] == category:
                features.append(feature)
        dp.addFeatures(features)
    
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
  
      
Exporter()

"""2019-11-18"""