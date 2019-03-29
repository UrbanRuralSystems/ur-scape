"""---------------------------------------------------------------------
Please set basic parameters
---------------------------------------------------------------------"""
outputPath = "path"  # Do not use any backslash (e.g. C:/Documents/ur-scape/Data)
name = "name" # Set layer name in Camel case (e.g. Population Density)
field = "field" # Name of the field to be used from shapefile. Will be ignored if raster.
resolution = "5" # 0-Neighbourhood, 1-City, 2-Metropolitan, 3-National, 4-Continental, 5-Global

"""Please set metadata"""
units = "none" # set layer units (e.g. People per Sq km)
location = "World" # set location of layer (e.g. Palembang)
source = "none" # set source of layer (e.g. Statistics Bureau)
date = "2016" # Year of data
color = "1" # 0=Red, 1=Orange, 2=Yellow, 3=Light Green , 4 = Dark Green, 5 = Cyan, 6 = Light Blue, 7 = Dark Blue, 8 = Purple, 9 = Pink
group = "Exported from GIS"

"""---------------------------------------------------------------------
Please set advanced parameters below only if you are advanced user
---------------------------------------------------------------------"""
extentAsCanvas = False
resolutionPatch = [1, 1, 1 , 10, 50, 360] # in degrees units
resolutionLevels = [10, 100, 300, 0.0083333, 0.08333333, 0.416] 
# resolution is in [metres, metres, metres, degrees, degrees,degrees,]

forMunicipalBudget = False# Create basic data for municipal budget
forReachability = False # Create basic data for reachability
resamplingMethod = 0 # 0 Nearest neighbor, 1 Bilinear, 5 average, 7 Maximum, 8 minimum

networkMap ={"1":["Lokal primer","Lokal sekunder"],\
             "2":["Arteri sekunder","Kolektor sekunder"],\
             "4":["Arteri primer","Kolektor primer"],\
             "8":["Tautan"],\
             "16":["Tol"]}

"""---------------------------------------------------------------------
Setup for developer
---------------------------------------------------------------------"""
resolutionEPSG = ['3857', '3857', '3857', '4326', '4326', '4326'] # '3857'= metres, '4326' = degress
resolutionSign = ['D', 'D', 'D', 'C', 'B', 'A' ]
debuggingMode = False
versionAfter0980 = True

"""---------------------------------------------------------------------
You can't touch this
---------------------------------------------------------------------"""
import gdal, os, sys, osr, shutil, processing, csv, math, colorsys,traceback,numpy
from tempfile import mkstemp
from gdalconst import *
from qgis.core import QgsProject

gdal.AllRegister()

CHECK_DISK_FREE_SPACE = False
 
def ProblemsInputCheck():
    problem = False
    print ("")
    try: # check resulution input
        test01 = resolutionLevels[int(resolution)]
    except IndexError:
        print ("Oops, wrong value in variable resolution. Please set 1,2 or 3")
        problem = True    
       
    try: # check field name input
        if hasattr(iface.activeLayer(), 'fields'):
            test02 = iface.activeLayer().fields().field(field)
    except KeyError:
        print("Oops,field:"+ field+ " is not in selected layer, please check again")
        problem = True
    
    if not os.path.exists(outputPath): # check if path exist
        print("Oops,outputPath you specified does not exist, please check again")
        problem = True
        
    if (hasattr(iface.activeLayer(), 'fields')):
        if not iface.activeLayer().geometryType() == 2 and forMunicipalBudget: # check if Municipal Budget allowed on type of geometry
            print ("Oops. Seems like you are trying to create Municipal Budget from lines or points") 
            print ("Maybe you forgot forMunicipalBudget variable set on True?!")
            problem = True
    
    if not hasattr(iface.activeLayer(), 'fields') and forMunicipalBudget: 
       print ("Oops, You can not create Municipal Budget from Layer without Fields")
       print ("Maybe you forgot forMunicipalBudget variable set on True?!")
       problem = True
       
    if hasattr(iface.activeLayer(), 'fields') and forMunicipalBudget :
        try: #check if is it sting type of the field for municipal budget
            if not iface.activeLayer().fields().field(field).type()==10:
                print ("Oops, for Municipal budget please use layer or field with administrative boundaries names")  
                print ("Maybe you forgot forMunicipalBudget variable set on True?!")
                problem = True
        except KeyError:
            print ("") # this was reported above already
    
    if not hasattr(iface.activeLayer(), 'fields'):
        raster = gdal.Open(iface.activeLayer().dataProvider().dataSourceUri(), 1)
        if (raster.GetProjectionRef() == ""):
            print ("Oops. Seems there is there is missing Projection reference")
            print("Please Assign Projection in Menu/Raster/Projections / Assign projection")
            problem = True
            
    if not resolution.isdigit():
        print("Please make sure resolution is a number in range 0-5 ")
        problem = True        

    else: 
        if int(resolution)>5:
            print("Please make sure resolution is in range 0-5 ")
            problem = True      
    return problem
    
 
def RunAlgorithm():

    global year , maxPatchSize, totalCount
    """ get layer"""
    activeLayer = iface.activeLayer()
    fullName = activeLayer.dataProvider().dataSourceUri()
    if debuggingMode:
        print("active layer: " + str(activeLayer))
        print("name layer: " + str(activeLayer.name()))
        print("data source: " + str(fullName))
        
    """ set resolution"""
    res= resolutionLevels[int(resolution)] 
    maxPatchSize = resolutionPatch[int(resolution)]
    epsg =    'EPSG:'+ resolutionEPSG[int(resolution)] 
    
    """ put to write file, set date"""
    datSplited = list(date)
    year =datSplited[-2] + datSplited[-1]
    
    """ misc"""
    totalCount = 0
    
    if name is not None:
        if forReachability or forMunicipalBudget:
            CheckPath(fullName ) 
            CheckLayerByScenario(activeLayer, res, epsg)
        else:
            CheckPath(fullName ) 
            CheckLayerByType(activeLayer,res, epsg)
    else:
        print("TODO multiple layers")
            


def CleanUp():
    global oldGraphData, oldGraphVarify#, allBandData
    if not debuggingMode:
        try:
            shutil.rmtree(pathTemp)
            print("Deleting temp folder successfull")
        except:
            print ("We might have some issues with lock on the temp folder...")
            if os.path.isdir(pathTemp):
                print ("Oops. Something went wrong, You need to delete temp folder Manually")
            else: 
                print ("All Cleaned up, ready for the next Job")
    """ + delete globals"""
    del oldGraphData 
    del oldGraphVarify
   
        

def CheckPath(fullName ):
    global finalPath
    layerPath = outputPath +  "/sites/" +location +"/"
    budgetPath = outputPath +  "/Municipal Budget/"
    finalPath = budgetPath if forMunicipalBudget else layerPath

    if not os.path.exists(finalPath):
        print("Creating new folder for: " + location)
        os.makedirs(finalPath)
            
    """ create temporary working folder"""
    path = os.path.dirname(os.path.abspath(fullName)) 
    global pathTemp
    pathTemp = os.path.dirname(os.path.abspath(fullName)) +  "/temporary_Workspace/"
    if not os.path.exists(pathTemp):
        os.makedirs(pathTemp)   
            
    return True 
    del finalPath
def FindLayersCsv():
    layersCsvPath = outputPath + "/layers.csv"
    hasLayersCsv = os.path.isfile(layersCsvPath)
    if hasLayersCsv:
        return layersCsvPath
    else: 
        print("I can't find the layers.csv file, it is up to you to add it to ur-scape.")
        return None
def WriteLayerToCsv(nameLayer, groupName, layersCsvPath):
        groupInFile =  False
        missingInFile = True
        c = layerHVS = [int(color)/10, 1, 1 ] # prepare full HSV color
        colorOut = [int(band*255)for band in colorsys.hsv_to_rgb(c[0],c[1],c[2])]
        with open(layersCsvPath, 'r',newline='') as csvfile:
            spamreader = csv.reader(csvfile, delimiter=',')
            for row in  spamreader:
                if len(row)>1: #in case of empty line, after manual edit of file
                    if groupName in row[1]:
                        groupInFile= True
                    if str(nameLayer) == str(row[1]):
                        print (nameLayer + " layer is already in layers.csv, skipping...")
                        missingInFile = False
                        break
        if missingInFile:
            with open(layersCsvPath, 'a',newline='') as csvfile:
                spamwriter = csv.writer(csvfile, delimiter=',')
                if not groupInFile:
                    spamwriter.writerow(['','','','',''])
                    spamwriter.writerow(['Group',groupName,colorOut[0],colorOut[1],colorOut[2]])
                print (nameLayer + " layer isn't in layers.csv, writing...")
                spamwriter.writerow(['Layer',nameLayer,colorOut[0],colorOut[1],colorOut[2]])
        
        
def CreateCategoryRaster(layer, fullName):
    """ check if raster has dbf file"""
    categories = []  
    csvFilePath =  fullName+".csv"
    hasCsv = os.path.isfile(csvFilePath)
    if hasCsv:
        print("Yeah we have found csv for the file")
        with open(csvFilePath, newline='') as csvfile:
            reader = csv.DictReader(csvfile)
            setHeader = ""
            for thisName in next(reader): # TODO: this part need to be tested on more examples
                if field in thisName:
                    print("We even found your field name in csv. Such a lovely day.")
                    setHeader = thisName
            for row in reader:
                categories.append(row[setHeader])
    else:
        print("no categories")
    return categories

def CreateCategoryVector(layer):
    #global categorizedVector
    categorizedVector = pathTemp + "categorizedVector.shp"

    parameters = {'INPUT':layer,\
              'FIELD_NAME':"catID",\
              'FIELD_TYPE': 0,\
              'FIELD_LENGTH':3,\
              'FIELD_PRECISION':2,\
              'OUTPUT':categorizedVector,\
             }
    try:
        processing.run('qgis:addfieldtoattributestable', parameters)
    except:
        print ("!!! Oops I'm having problems adding new parameters, try restarting QGIS.")
    
    catLayer = QgsVectorLayer(categorizedVector,"polygon","ogr")

    """ check for all categories in dataset"""
    categories = []
    for feature in catLayer.getFeatures():
        noCommaInCategory = str(feature[field]).split(',')
        cleanRecord = noCommaInCategory[0]    
        if not cleanRecord in categories:
            categories.append(cleanRecord)
    categories.sort()  
    
    """ write shorted values"""
    
    catLayer.startEditing()
    for feature in catLayer.getFeatures():            
        colId = feature.fieldNameIndex("catID") 
        noCommaInCategory = str(feature[field]).split(',')
        categoryId = categories.index(noCommaInCategory[0])
        catLayer.changeAttributeValue(feature.id(),colId, categoryId+1)

    catLayer.commitChanges()
    
    if len(categories)>32 and not forMunicipalBudget:
        print ("WARNING! You are using more then 32 categories. ur-scape won't show this correctly.")
    


    return categories, catLayer

def GetSeparatedFeatureLayer(layer, featureToWriteName,isPoint):
    """ krete temporary layer for each group of features"""
    
    setCRS = layer.crs().authid()
    if (isPoint):
        layerTemp = QgsVectorLayer("Point?crs="+setCRS,"LayerTemp", "memory")
    else:
        layerTemp = QgsVectorLayer("LineString?crs="+setCRS,"LayerTemp", "memory")
    """ copy field names"""
    attr = layer.dataProvider().fields().toList()
    dp = layerTemp.dataProvider()
    dp.addAttributes(attr)
    layerTemp.updateFields()
    """ keep only relevent features """
    features = []
    for feature in layer.getFeatures():
        if feature[field] == featureToWriteName:
            features.append(feature)
    dp.addFeatures(features)
    
    """add cotegory column """
    # layerTempCat = pathTemp + featureToWriteName+"SeparateCat.shp"    
    parameters = {'INPUT':layerTemp,\
              'FIELD_NAME':"catID",\
              'FIELD_TYPE': 0,\
              'FIELD_LENGTH':3,\
              'FIELD_PRECISION':2,\
              'OUTPUT': "memory:separateCat" ,\
             }
    SeparateCat = processing.run('qgis:addfieldtoattributestable', parameters)

    #layerTempCategory = QgsVectorLayer(SeparateCat["OUTPUT"], "CAT", "ogr")

    SeparateCat["OUTPUT"].startEditing()
    for feature in SeparateCat["OUTPUT"].getFeatures():
        colId = feature.fieldNameIndex("catID") 
        #categoryId = categories.index(feature[field])
        SeparateCat["OUTPUT"].changeAttributeValue(feature.id(),colId, 1)
    SeparateCat["OUTPUT"].commitChanges()
    #del layerTempCategory
    
    # only for testing
    # QgsProject.instance().addMapLayer(layerTemp )
    
    return SeparateCat["OUTPUT"]
    
def FixGeometry(layerIn):
    
    """ fix geometries... it is by default as far validating is slower anyway"""
    # processing.algorithmHelp('native:fixgeometries')
    tempVector = pathTemp + "fixedVector.shp"
    
    parameterReproject = { 'INPUT': layerIn,\
                           'OUTPUT': tempVector}
    processing.run('native:fixgeometries', parameterReproject)
    return tempVector
    del tempVector
    print ("Geometry Fixed. Just in Case.")
 
def VectorToRaster(layerIn, extent, setResolution, epsg, thisName,makeBigger):
    reprojectedRaster = pathTemp + thisName+ str("%.2f" % setResolution)+"_RP.tif"
    reprojectedExtent = pathTemp + "reprojectedExtent.shp"
    reprojectedVector = pathTemp + "reprojectedVector.shp"
    
    """ project layer for geting size of the cell in metres"""
    parameterReproject = { 'INPUT': layerIn,\
                           'TARGET_CRS': epsg ,\
                           'OUTPUT': reprojectedVector}
    processing.run('qgis:reprojectlayer', parameterReproject)
    # reproject separately extend (case of all points)
   
    parameterReprojectExtent = { 'INPUT': extent,\
                                 'TARGET_CRS': epsg ,\
                                 'OUTPUT': reprojectedExtent}
    processing.run('qgis:reprojectlayer', parameterReprojectExtent)

    fieldCat = "catID" if category  else field

    if (makeBigger):
        layer = QgsVectorLayer(reprojectedExtent,"Point","ogr")
        e = layer.extent()
        r = setResolution
        reprojectedExtent = QgsRectangle (e.xMinimum() -r, e.yMinimum()-r , e.xMaximum() +r, e.yMaximum()+r )
    if extentAsCanvas: 
        """Set scale and extend to default CRS"""
        my_crs=QgsCoordinateReferenceSystem(4326)
        QgsProject.instance().setCrs(my_crs)
        scale = iface.mapCanvas().scale()
        tempExtent = iface.mapCanvas().extent()

        """Set scale and extend to requered CRS"""
        my_crs=QgsCoordinateReferenceSystem(epsg)
        QgsProject.instance().setCrs(my_crs)
        reprojectedExtent = iface.mapCanvas().extent()

        """Set scale and extend again to default CRS"""
        my_crs=QgsCoordinateReferenceSystem(4326)
        QgsProject.instance().setCrs(my_crs)
        iface.mapCanvas().setExtent(tempExtent)
        iface.mapCanvas().zoomScale(scale)
        iface.mapCanvas().refresh()


    # create grided data. For help--> processing.algorithmHelp("gdal:rasterize")
    parameterRasterize = {'INPUT': reprojectedVector,\
                      'FIELD': fieldCat,\
                      'UNITS': 1,\
                      'WIDTH': setResolution,\
                      'HEIGHT':setResolution,\
                      'EXTENT':reprojectedExtent,\
                      'DATA_TYPE': 6,\
                      'INVERT': False,\
                      'INIT': -1,\
                      'OUTPUT': reprojectedRaster}
    try:
        processing.run("gdal:rasterize",parameterRasterize)  
    except:
        processing.run("gdal:rasterize",parameterRasterize)
        print("bug in qgis after openning, but we solved....")
   
   # only for testing
   # layerTesting = QgsRasterLayer(reprojectedRaster,"testing..")
    #QgsProject.instance().addMapLayer(layerTesting )

    return reprojectedRaster

    
def AggregateRaster(raster, res, thisName):
       
    """ Get rasters and tranformation information"""
    dataset= gdal.Open(raster , GA_ReadOnly)

    countX,countY = dataset.RasterXSize, dataset.RasterYSize
    gt = dataset.GetGeoTransform()    
    minX, minY, w, h = gt[0], gt[3], gt[1], gt[5]

    """ get values as 2D array"""
    data = dataset.GetRasterBand(1).ReadAsArray(0, 0, countX, countY)
    
    newY , newX = int(countY/3), int(countX/3)
    #newArray = [[0] * int(countX/3)] * int(countY/3)
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
    #Raster = np.array(values_array)
    driver = gdal.GetDriverByName('GTiff')
    newOutputRaster = pathTemp + thisName + "100_NOR.tif"
    dst_ds = driver.Create(newOutputRaster, int(countX/3), int(countY/3),  1, gdal.GDT_Int16)
    band = dst_ds.GetRasterBand(1)
    band.WriteArray(numpy.array( newArray) )
    band.SetNoDataValue(-1)

    dst_ds.SetGeoTransform(gtNew)
    srs = osr.SpatialReference()

    #srs.ImportFromEPSG(dataset.GetProjectionRef())
    srs.ImportFromEPSG(4326)

    dst_ds.SetProjection( srs.ExportToWkt() )
    dst_ds = None
    return newOutputRaster 
    
def RasterToUnits(layer, resolution ,outEPSG):
    # change reolution first. For help--> processing.algorithmHelp("gdal:translate")
   
    reprojectedRaster = pathTemp + " reprojectedRaster.tif"
    inCRS = layer.crs().authid()

    parameterWarp = {'INPUT': layer,\
                    'SOURCE_CRS': inCRS,\
                    'TARGET_CRS': outEPSG ,\
                    'TARGET_RESOLUTION': resolution,\
                    'NODATA':-1,\
                    'RESAMPLING':resamplingMethod,\
                    'DATA_TYPE':6,\
                    'MULTITHREADING':False,\
                    'OUTPUT':reprojectedRaster}
    processing.run("gdal:warpreproject", parameterWarp)

    return reprojectedRaster

def MetresToDegress (layerForWarp, thisName, res, epsg):   
    outputRaster = pathTemp + thisName+ str("%.2f" % res)+"_OR.tif"

    parameterWarp = {'INPUT': layerForWarp,\
                    'SOURCE_CRS': epsg ,\
                    'TARGET_CRS': 'EPSG:4326',\
                    'TARGET_RESOLUTION': 0,\
                    'NODATA':-1,\
                    'RESAMPLING':resamplingMethod,\
                    'DATA_TYPE':6,\
                    'MULTITHREADING':False,\
                    'OUTPUT': outputRaster}
    processing.run("gdal:warpreproject", parameterWarp)
 
    #layerTesting = QgsRasterLayer(outputRaster2,"testing..")
    #QgsProject.instance().addMapLayer(layerTesting )
    
    return outputRaster
    del outputRaster
    
def CreateGridFile(categories, gt, bandData,thisName, index, countX,countY,isPoint):
    global totalCount
    """ get info about size and position"""
    if extentAsCanvas:
        my_crs=QgsCoordinateReferenceSystem(4326)
        QgsProject.instance().setCrs(my_crs)
        ex = iface.mapCanvas().extent()
        minX=ex.xMinimum()
        minY=ex.yMaximum()
        maxX=ex.xMaximum()
        maxY=ex.yMinimum()
        print ("COUNTX,COUNTY: " + str(countX) +","+str(countY) + ". has your budget data same?")
    else :
        minX=gt[0]
        minY=gt[3] if gt[3] < 85 else 85 # fixing bug in QGIS
        maxX=gt[0]+gt[1]*countX 
        maxY=gt[3]+gt[5]*countY
    """special adjustment for point solution"""
    end, headValue = "","VALUE"
    if(len(categories) > 2) and isPoint and versionAfter0980:
        for i in range(2, len(categories)):
            end = end +","
            headValue = headValue + ",VALUE"
    end = end + "\n"
    headValue =  headValue + ",VALUE\n" if isPoint else headValue + ",MASK\n"
    
    
    """ writing data"""
    sign ='_'+ resolutionSign[int(resolution)]+'_'
    ending = '_multi.csv' if isPoint and versionAfter0980 else '_grid.csv'
    fileStringTemp = name+ sign +location+'@'+ str(index)+'_'+year+ ending
    fileString = fileStringTemp if not forMunicipalBudget else location + '.csv'
    output_file = open(finalPath+'/' + fileString , 'w')

    output_file.write("METADATA,TRUE"+ end)
    output_file.write("Layer Name,"+name+ end)
    output_file.write("Source,"+source+ end)
    output_file.write("Colouring,"+"Multi"+ end) #+ defined by user
    
    """ write down category"""
    if (category):
        output_file.write("CATEGORIES,TRUE"+end)
        for i in range(len(categories)):
            output_file.write(str(categories[i])+ ","+ str(i+1) + end)
    else:
        output_file.write("CATEGORIES,FALSE"+end)
   
    if not forMunicipalBudget:
        output_file.write("Units,"+ units + end)
        output_file.write("West,"+ str(minX) +end)
        output_file.write("North,"+str(minY)+ end)
        output_file.write("East,"+ str(maxX)+end)
        output_file.write("South,"+ str(maxY)+end)
        output_file.write("Count X," + str(countX)+ end)
        output_file.write("Count Y," + str(countY)+ end)
        output_file.write(headValue )
    else:
        
        output_file.write("VALUE,MASK" + end)
               
    for y in range(0, countY):
        for x in range(0, countX):
            if isPoint and not versionAfter0980:
                valueArray = bandData[y,x].split(",")
                value, majority = -1,0
                for index in range (0, len(valueArray)) : 
                    if int(valueArray[index]) > majority :
                        value,majority = index +1, int(valueArray[index]) 


            else: 
                value =  bandData[y,x] if not forMunicipalBudget else int(float(bandData[y,x] ))
                if debuggingMode and not isPoint:
                    if int(float(bandData[y,x])) > 0 :
                        totalCount = totalCount + int(float(bandData[y,x] ))

                
            mask=""
            if not isPoint or (isPoint and not versionAfter0980):
                mask = 0 if float(value) < 0 else 1 
                mask = ","+str(mask)
            
            output_file.write(str(value) + mask+ "\n")
    
    #for y in range(0, 214):
    #    output_file.write("0.0,0\n")
        
    print ("File patch "+ str(index)+" generated for " + location)
    if debuggingMode:
        print ("Total Count for all patch so far is = " + str(totalCount))    
    """ close all and delete working dir"""
    output_file.close()

def CreateGraphFile(rasterMain, rasterVarify, thisName, feature, firstTime):
    global oldGraphData, oldGraphVarify
    
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
    
    """Create name for file"""
    fileString = thisName + '_D_'+location+'_'+year+ '_graph.csv'
    

    lenght = resolutionLevels [int(resolution)]
    lenghtDiagonal = (lenght**2+lenght**2)**(.5)
    """ check what classification is curently generated """
    cl = "1"
    for key in networkMap:
        keyList = networkMap[key]
        for subKey in keyList:
            if str(subKey).lower().strip() in feature.lower().strip():
                cl = key
  
    """if first time, then make header and overwrite old file"""
    if firstTime:
        output_file = open(finalPath+'/' + fileString , 'w')
        #output_file.write("lenght;source;target;x1;y1;x2;y2;classification;WKT" + "\n") # for testing network in QGIS only
        output_file.write("lenght,source,target,x1,y1,x2,y2,classification" + "\n")
    else:
        output_file = open(finalPath+'/' + fileString , 'a')
   

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
                if oldGraphData is not None:
                    isOldValue = [oldGraphData[y + m[0] , x+ m[1]]>0 for m in metrix]
                
                if isOtherValue[0] or isOldValue[0]:
                    dataCheck = dataVarify if isOtherValue[0] else oldGraphVarify
                    
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
                    dataCheck = dataVarify if isOtherValue[1] else oldGraphVarify
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
                    dataCheck = dataVarify if isOtherValue[2] else oldGraphVarify
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
                    dataCheck = dataVarify if isOtherValue[3] else oldGraphVarify
                    edgeVarifyRT = dataCheck [(y)*3+0,(x-1)*3+2] > 0 
                    edgeVarifyRM = dataCheck [(y)*3+1,(x-1)*3+2] > 0 
                    edgeVarifyRD = dataCheck [(y)*3+2,(x-1)*3+2] > 0 
                    if edgeVarifyRT or edgeVarifyRM or edgeVarifyRD or cl== "16": 
                        """ informations about left  nod"""
                        target = str(y*countX+(x-1))
                        X2, Y2 = str(minX + w*(x-1) + halfW),str(minY + h*(y)+ halfH)
                        #output_file.write(str(lenghtDiagonal)+";"+source+";" + target+";"+X1+";"+Y1+";"+X2+";"+Y2 + ";"+ cl+";"+"LINESTRING ("+X1+" " +Y1+","+ X2+ " "+Y2+")" + "\n")
                        output_file.write(str(lenghtDiagonal)+","+source+"," + target+","+X1+","+Y1+","+X2+","+Y2 + ","+ cl+ "\n")
  
    oldGraphData = dataMain.copy()
    oldGraphVarify = dataVarify.copy()

    output_file.close()        
            
def CountPointsInCell(raster,points,thisName,epsgs):
    
    """ reprojects points to same CRS as raster"""
    #reprojectedPoints= pathTemp + "rpc.shp"
    
    parameterReproject = { 'INPUT': points,\
                           'TARGET_CRS': epsgs ,\
                           'OUTPUT': "memory:rp"}
    rp = processing.run('qgis:reprojectlayer', parameterReproject)
    pl = rp["OUTPUT"]

    """ Get Transform of the ratser"""
    ds = gdal.Open(raster )
    cols,rows = ds.RasterXSize, ds.RasterYSize
    gt = ds.GetGeoTransform()
    # read array from cells
    data = ds.GetRasterBand(1).ReadAsArray(0, 0, cols,rows)

    for y in range(0, rows):
        for x in range(0, cols):
            value = data[y, x]
            valueWithCount = 0
            if value > 0: # if there is some data
                valueWithCount = GetPointCount(x,y,gt[0], gt[3],gt[1],gt[5] ,pl)
            
            data[y, x] = valueWithCount
    
    # TODO clean up
    newRaster= pathTemp +thisName+"newRaster.tif"
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
   # only for testing
    #layerTesting = QgsRasterLayer(newRaster,"testing..")
    #QgsProject.instance().addMapLayer(layerTesting )
    
    return newRaster
def GetPointCount(i,j, Ox,Oy,h,k, lyr):
    #Create a measure object. 
    xMin, xMax = Ox + h*(i+0),Ox + h*(i+1)
    yMin,yMax = Oy + k*(j+0), Oy + k*(j+1)
    count =0
    for feature in lyr.getFeatures():
        p = feature.geometry().asPoint()
        if p[0]>=xMin and p[0] <= xMax:
            if p[1]<=yMin and p[1] >= yMax:
                count +=1
    return count
def GetExtents(raster,res):
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
    """ get extend for each 1 degree by 1 degree square"""
   
    if (( xRange > maxPatchSize) or  (yRange > maxPatchSize)) and not extentAsCanvas:
        #QgsRectangle (double xMin, double yMin=0, double xMax=0, double yMax=0)
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
    
def ClipRaster (rasterIn, extent, index, cat ):
    clipRaster = pathTemp + "ClippedPatch_"+str(index)+" of "+str(cat)+ "_CR.tif"
     # processing.algorithmHelp('gdal:cliprasterbyextent')
    parameterClip = { 'INPUT': rasterIn,\
                    'PROJWIN': extent,\
                    'NODATA': -1,\
                    'DATA_TYPE':6,\
                    'OUTPUT': clipRaster}
    processing.run('gdal:cliprasterbyextent', parameterClip)
    """ one more resample to make sure Xcount and Y are alway same, e.g for municipal Budget"""
    
    finalRaster = pathTemp + str(cat) +" of cat for index: "+ str(index)+"_FR.tif"
    parameterWarp = {'INPUT': clipRaster,\
                    'SOURCE_CRS': 'EPSG:4326' ,\
                    'TARGET_CRS': 'EPSG:4326',\
                    'TARGET_RESOLUTION': 0,\
                    'NODATA':-1,\
                    'RESAMPLING':resamplingMethod,\
                    'TARGET_EXTENT': extent,\
                    'DATA_TYPE':6,\
                    'MULTITHREADING':False,\
                    'OUTPUT': finalRaster}
    processing.run("gdal:warpreproject", parameterWarp)

    return clipRaster 
    
def GetBand (raster, allBandData, isPoint):
    """ get values"""
    ds = gdal.Open(raster , GA_ReadOnly)
    band = ds.GetRasterBand(1)
    countX = ds.RasterXSize
    countY = ds.RasterYSize
    oneBandData = band.ReadAsArray(0, 0, countX, countY)
    
    gt = ds.GetGeoTransform()

    if allBandData is None: # first time use array as it is, most of cases
        allBandData = oneBandData.astype(str)
    else: # Add mmore values as string in case of points
        for y in range(0, countY):
            for x in range(0, countX):
                previusString = allBandData[y,x].split('.')[0]
                newString = str(oneBandData[y,x]).split('.')[0] 
                allBandData[y,x] =  previusString  +','+ newString

    return allBandData , gt, countX,countY 
    
def CheckLayerByType(activeLayer,res, epsg):
    global categories, category
    categories = []
    
    oldData = True
    isPoint, category = False, False
    """ Check what type of file is layer (Raster or Vector)"""
    if isinstance(activeLayer,QgsVectorLayer) :
        layer = QgsVectorLayer(activeLayer.source(),"polygon","ogr")
        
        """ Check if layer should be categorised (if field type is 10 (string))"""
        category = activeLayer.fields().field(field).type()==10
        if category:
            categories, catLayer = CreateCategoryVector(layer) 
        
      
        if layer.geometryType() == 0 : # Points Export
            print ("I am working with vector and obviously it is Point type." )
 
            if category:
                layerToRasterTemp= VectorToRaster(layer,layer ,res  ,epsg ,name,True)
                rasterDummy= MetresToDegress (layerToRasterTemp,"Dummy", res, epsg) 
                extents = GetExtents(rasterDummy,res) 
                
                for j in range (0,len(extents)): 
                    bandToWrite = None
              
                    for i in range (0,len(categories)):
                        layerToRatser = GetSeparatedFeatureLayer(layer, categories[i],True)
                    
                        layerToCount= VectorToRaster(layerToRatser,layer ,res  ,epsg ,name,True)
                        layerToWarp = CountPointsInCell(layerToCount,layerToRatser , name, epsg)
                        rasterToGrid = MetresToDegress (layerToWarp,categories[i], res, epsg) 
                        rasterExtent = ClipRaster(rasterToGrid, extents[j],j,i)
                        bandToWrite, gt, cX, cY = GetBand(rasterExtent, bandToWrite, True)
                      
                    CreateGridFile(categories,gt, bandToWrite, name,j, cX,cY,True) 
            else:
                layerToCount= VectorToRaster(layer, layer ,res  ,epsg, name,True)
                layerToDegress= CountPointsInCell(layerToCount, layer, name, epsg)
                rasterToGrid = MetresToDegress (layerToDegress, name, res, epsg)
                extents = GetExtents(rasterToGrid,res) 
                for i in range (0,len(extents)): 
                    rasterExtent = ClipRaster(rasterToGrid, extents[i],i,0)
                    bandToWrite, gt, cX, cY = GetBand(rasterExtent, None, False)
                    CreateGridFile(categories,gt, bandToWrite, name,i, cX,cY,False)
                
            pathToLayersCSV = FindLayersCsv () # separated loop for Layer.csv

            if pathToLayersCSV is not None:
                WriteLayerToCsv(name, name,pathToLayersCSV)
    
        elif layer.geometryType() == 1:
            print ("We got categorised Lines."if category else "We got Lines.") 
            
            layerIn = catLayer if category else layer

            """layerToRaster = FixGeometry(layerIn.source())"""
            rasterToDegress = VectorToRaster(layerIn.source(),layer, res, epsg, name,False)

            rasterToGrid = MetresToDegress (rasterToDegress, name, res, epsg)
            extents = GetExtents(rasterToGrid,res)
            
            for i in range (0,len(extents)): 
                rasterExtent = ClipRaster(rasterToGrid, extents[i],i,0)
                bandToWrite, gt, cX, cY = GetBand(rasterExtent, None, False)
                CreateGridFile(categories,gt, bandToWrite, name,i, cX,cY,False)
            
            if FindLayersCsv ()is not None:
                WriteLayerToCsv(name, group,FindLayersCsv ())
            
            print ("Done here")
            
        elif layer.geometryType() == 2 : 
            print ("We got categorised Polygon."if category else "We got Polygon.") 
            
            layerIn = catLayer if category else layer

            """layerToRaster = FixGeometry(layerIn)"""
            rasterToDegress =VectorToRaster(layerIn.source(),layer, res,epsg, name,False)
            rasterToGrid = MetresToDegress (rasterToDegress, name, res,epsg) 
            extents = GetExtents(rasterToGrid,res)
            for i in range (0,len(extents)): 
                rasterExtent = ClipRaster(rasterToGrid, extents[i],i,0)
                bandToWrite, gt, cX, cY = GetBand(rasterExtent, None, False)
                CreateGridFile(categories,gt, bandToWrite, name,i, cX,cY,False)
                
            
            #pathToLayersCSV = FindLayersCsv () 
            if FindLayersCsv () is not None:
                WriteLayerToCsv(name, group,FindLayersCsv())
      
    elif isinstance(activeLayer,QgsRasterLayer) :
        print("Ok, Seems that we are dealing with geo-tif. Here we go!")
        layerIn = QgsRasterLayer(activeLayer.source())
        
        if extentAsCanvas:
            my_crs=QgsCoordinateReferenceSystem(4326)
            QgsProject.instance().setCrs(my_crs)
            ex = iface.mapCanvas().extent()
            layer = QgsRasterLayer(ClipRaster(layerIn, ex,0,0))
        else: 
            layer =layerIn
            
        categories = CreateCategoryRaster(layer, activeLayer.dataProvider().dataSourceUri())
        category= False if not categories else True
        
        rasterToDegress= RasterToUnits(layer,res, epsg)
        rasterToGrid = MetresToDegress (rasterToDegress, name, res,epsg)
        extents = GetExtents(rasterToGrid,res)
        for i in range (0,len(extents)): 
            rasterExtent = ClipRaster(rasterToGrid, extents[i],i,0)
            bandToWrite, gt, cX, cY = GetBand(rasterExtent, None, False)
            CreateGridFile(categories,gt, bandToWrite, name,i, cX,cY,False)
           

        
        if FindLayersCsv ()is not None:
            WriteLayerToCsv(name, group,FindLayersCsv ())
    else:
        print("Hey I don't know this type of the file! Sorry I am out")
        
    del category



def CheckLayerByScenario(activeLayer, resolution, epsg):
    global category
    if isinstance(activeLayer,QgsVectorLayer) :
        layer = QgsVectorLayer(activeLayer.source(),"polygon","ogr")
    
    """ Check if layer should be categorized (if field type is 10 (string))"""
    category = activeLayer.fields().field(field).type()==10
    if category:
        categories, catLayer = CreateCategoryVector(layer) 
    
    if forReachability:
        if layer.geometryType() == 1:
            print ("...working on Reachability Data")
            
            """ Get Graph's links for each separated class in the network"""
        
            firstTime = True
            for category in reversed(categories):
                layerToRaster = GetSeparatedFeatureLayer(layer, category, False)
                rasterToDegress = VectorToRaster(layerToRaster,layer, resolution/3, epsg,category,False)   
                rasterToCheck= MetresToDegress (rasterToDegress, category, resolution/3,epsg) 
                rasterToGraph = AggregateRaster(rasterToCheck,resolution, category)
                CreateGraphFile (rasterToGraph ,rasterToCheck ,name,category, firstTime )
                firstTime = False

            print ("Done here") 
        else: 
            print ("You are trying to create reachability data from non-line type")
            print ("I am sory Bill I can not let you do that")  
        
    elif forMunicipalBudget: 
        if layer.geometryType() == 2:
            print ("...and seems like you plan to create data for Municipal budget") 
            categoryToWrite = category
            layerIn = catLayer if category else layer    
            layerToWarp =VectorToRaster(layerIn.source(),layer, resolution,epsg, name,False)
            rasterToGrid = MetresToDegress (layerToWarp, name, resolution,epsg) 
            extents = GetExtents(rasterToGrid,resolution)
            rasterExtent = ClipRaster(rasterToGrid, extents[0],0,0) 
            bandToWrite, gt, cX, cY = GetBand(rasterExtent, None, False)
            CreateGridFile(categories,gt, bandToWrite, name,0, cX,cY,False)
            print ("Congratulations, you created special data for Municipal Budget.")
        else: 
            print ("You are trying to create municipal data from non-polygon type")
            print ("I am sory Bill I can not let you do that")  

if not ProblemsInputCheck():
    """Define couple of global Variables """
    oldGraphData, oldGraphVarify  = None, None
    
    try:
        RunAlgorithm() 
        print("__________________________________________")
    except Exception as error:
        print ("") # print only part of traceback to avoid intimidation (red) raise message
        print ("Oops we have an unknown error, please report following to developer:")
        tbl = traceback.format_exc().splitlines() # get lines of trace back
        print(tbl[-1] + " in line:" +tbl[-2].split("line")[-1])
        if debuggingMode:
            raise
    CleanUp()