// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if !UNITY_WEBGL
public static class NetworkDisruptionAnalysis
{
    private const string NDA = "NetworkDisruptionAnalysis";
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
    private static readonly string RootPath;
#endif
    private static readonly string NDA_Path;
    public static readonly string NDA_VirtualEnvPath;
    private static readonly string NDA_TempResultsPath;
    private static readonly string NDA_ResultsPath;
    private static readonly string PythonInterpreter;

    private static ReachabilityTool reachabilityTool;
    public static CustomProcess currActiveProcess;
    public static bool isAnalysisCancelled;
    public static bool isAnalysisCompleted;
    public static bool isSetupCompleted;

    static NetworkDisruptionAnalysis()
    {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        RootPath = Directory.GetCurrentDirectory();
#endif
        NDA_Path = Paths.Data + NDA;
        NDA_VirtualEnvPath = Path.Combine(NDA_Path, "venv");
        NDA_TempResultsPath = Path.Combine(NDA_Path, "TempResults");
        NDA_ResultsPath = Path.Combine(NDA_Path, "Results");
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
        PythonInterpreter = Path.Combine(NDA_VirtualEnvPath, $"bin{Path.DirectorySeparatorChar}python3.6");
#elif (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        PythonInterpreter = Path.Combine($"{RootPath}{Path.DirectorySeparatorChar}{NDA_VirtualEnvPath}", $"Scripts{Path.DirectorySeparatorChar}python.exe");
#endif
    }

    //
    // Public Methods
    //

    public static void SetupNDA()
    {
        // Create shell/batch script to setup virtual environment
        string setupFilePath;
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
        setupFilePath = $"{Paths.Data}" + Path.Combine(NDA, "setup.sh");
#elif (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        setupFilePath = $"{Paths.Data}" + Path.Combine(NDA, "setup.bat");
#endif

        if (!Directory.Exists(NDA_VirtualEnvPath))
        {
            using (StreamWriter sw = new StreamWriter(setupFilePath))
            {
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
                sw.WriteLine("#!/bin/bash");
                sw.WriteLine("if [ -x /usr/libexec/path_helper ]; then");
	            sw.WriteLine("eval `/usr/libexec/path_helper -s`");
                sw.WriteLine("fi");
#endif
                sw.WriteLine($"cd {Paths.Data + NDA}");
				sw.WriteLine("virtualenv -p python3 venv");
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
                sw.WriteLine("source venv/bin/activate");
                sw.WriteLine("pip install --upgrade pip");
				sw.WriteLine("pip3 install -r requirements.txt");
#elif (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
                string batContent = @".\venv\Scripts\activate" +
                                    " && python -m pip install --upgrade pip" +
                                    " && pip3 install -r wheelhouse.txt" +
                                    " && pip3 install -r requirements.txt";

                sw.WriteLine(batContent);
#endif
            }

#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            // Make shell script executable
            // Grant file execution permission to all
            currActiveProcess = new CustomProcess("chmod", new string[] { "+x", setupFilePath });

            currActiveProcess.OnFinished += (_, e) =>
            {
                //string line;
                if (!currActiveProcess.Output.EndOfStream)
                {
                    Debug.Log(currActiveProcess.Output.ReadToEnd());
                    //while ((line = currActiveProcess.Output.ReadLine()) != null) Debug.Log(line);
                }

                if (!currActiveProcess.Errors.EndOfStream)
                {
                    Debug.LogError(currActiveProcess.Errors.ReadToEnd());
                    //while ((line = currActiveProcess.Errors.ReadLine()) != null) Debug.LogError(line);
                }
                else
                {
                    RunSetup(setupFilePath);
                }
            };
            currActiveProcess.RunInBG();
#elif (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            RunSetup(setupFilePath);
#endif
        }
    }

    public static void RunNDAProcess(ReachabilityTool tool)
    {
        reachabilityTool = tool;

        if (!Directory.Exists(NDA_VirtualEnvPath))
            Debug.LogError("Virtual environment not found.");

        RunCatchmentOSMData();

        reachabilityTool.networkDisruptionAnalysisToggle.isOn = false;
    }

    public static bool HasMissingPackages()
    {
        bool missing = false;
        var script = Path.Combine(NDA_Path, "check_for_missing_packages.py");
        var args = new string[] {
            script,
            Path.Combine(NDA_Path, "requirements.txt")
        };
        currActiveProcess = new CustomProcess(PythonInterpreter, args);

        currActiveProcess.OnFinished += (_, e) =>
        {
			//string line;
			if (!currActiveProcess.Output.EndOfStream)
            {
				Debug.Log(currActiveProcess.Output.ReadToEnd());
				//while ((line = currActiveProcess.Output.ReadLine()) != null) Debug.Log(line);
			}

            if (!currActiveProcess.Errors.EndOfStream)
            {
                Debug.LogError(currActiveProcess.Errors.ReadToEnd());
                //while ((line = currActiveProcess.Errors.ReadLine()) != null) Debug.LogError(line);
            }
            else
            {
                missing = bool.Parse(currActiveProcess.Output.ReadToEnd());
            }
        };
        currActiveProcess.RunInBG();

        return missing;
    }

    public static void StopNDAProcess()
    {
        if (currActiveProcess != null)
        {
            currActiveProcess.OnFinished += (_, e) =>
            {
                //string line;
                if (!currActiveProcess.Output.EndOfStream)
                {
                    Debug.Log(currActiveProcess.Output.ReadToEnd());
                    //while ((line = currActiveProcess.Output.ReadLine()) != null) Debug.Log(line);
                }

                if (!currActiveProcess.Errors.EndOfStream)
                {
                    Debug.LogError(currActiveProcess.Errors.ReadToEnd());
                    //while ((line = currActiveProcess.Errors.ReadLine()) != null) Debug.LogError(line);
                }
                else
                {
                    // Debug.Log("Stopping NDA Process...");
                    isAnalysisCancelled = true;
                }
            };
            currActiveProcess.Cancel();
        }
    }

    //
    // Private Methods
    //

    private static void RunSetup(string setupFilePath)
    {
        currActiveProcess = new CustomProcess(setupFilePath);

        currActiveProcess.OnFinished += (_, e) =>
        {
            //string line;
            if (!currActiveProcess.Output.EndOfStream)
            {
                Debug.Log(currActiveProcess.Output.ReadToEnd());
                //while ((line = currActiveProcess.Output.ReadLine()) != null) Debug.Log(line);
            }

            if (!currActiveProcess.Errors.EndOfStream)
            {
                Debug.LogError(currActiveProcess.Errors.ReadToEnd());
                //while ((line = currActiveProcess.Errors.ReadLine()) != null) Debug.LogError(line);
            }
            else
            {
                if (!currActiveProcess.IsCancelled())
                {
                    File.Delete(setupFilePath);
                    isSetupCompleted = true;
                    // Debug.Log("Setup completed");
                }
            }
        };
        currActiveProcess.RunInBG();
    }

    private static Site GetActiveSite()
    {
        var siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
        return siteBrowser.ActiveSite;
    }

    private static AreaBounds GetActiveSiteAreaBounds()
    {
        return GetActiveSite().Bounds;
    }

    private static Coordinate CalcActiveSiteCenter()
    {
        var bounds = GetActiveSiteAreaBounds();
        var max = GeoCalculator.LonLatToMeters(bounds.east, bounds.north);
        var min = GeoCalculator.LonLatToMeters(bounds.west, bounds.south);
        var center = GeoCalculator.MetersToLonLat((min.x + max.x) * 0.5, (min.y + max.y) * 0.5);

        return center;
    }

    private static void RunCatchmentOSMData()
    {
        // Debug.Log("RunCatchmentOSMData start");
        // Prep
        var script = Path.Combine(NDA_Path, "catchment_osm_data.py");
        var bounds = GetActiveSiteAreaBounds();
        var center = CalcActiveSiteCenter();
        var path_gdf = Path.Combine(NDA_TempResultsPath, $"osm_all_roads{Path.DirectorySeparatorChar}osm_all_roads_");
        
        /*
            0. Python script
            1. Temp results path
            2. Site's coordinates
            3. Catchment distance
            4. Road path
            5. City infrastructure .shp file
            6. Boundaries
        */
        var args = new string[] {
                script,
                NDA_TempResultsPath,
                $"{center.Latitude},{center.Longitude}",//"10.807616,106.689010",
                reachabilityTool.NDAParamPanel.Distance,//"6000",
                path_gdf,
                reachabilityTool.NDAParamPanel.CityInfrastructure,
                $"{bounds.south},{bounds.west},{bounds.north},{bounds.east}"//"10.7110,106.5749,10.8520,106.8147"
            };

        currActiveProcess = new CustomProcess(PythonInterpreter, args);

        currActiveProcess.OnFinished += (_, e) =>
        {
            //string line;
            if (!currActiveProcess.Output.EndOfStream)
            {
                Debug.Log(currActiveProcess.Output.ReadToEnd());
                //while ((line = currActiveProcess.Output.ReadLine()) != null) Debug.Log(line);
            }

            if (!currActiveProcess.Errors.EndOfStream)
            {
                Debug.LogError(currActiveProcess.Errors.ReadToEnd());
                //while ((line = currActiveProcess.Errors.ReadLine()) != null) Debug.LogError(line);
            }
            else
            {
                if (!currActiveProcess.IsCancelled())
                {
                    // Debug.Log("RunCatchmentOSMData done");
                    RunCatchmentExposure();
                }
            }
        };
        currActiveProcess.RunInBG();
    }

    private static void RunCatchmentExposure()
    {
        // Debug.Log("RunCatchmentExposure start");
        // Prep
        var script = Path.Combine(NDA_Path, "catchment_exposure.py");
        var path_gdf = Path.Combine(NDA_TempResultsPath, $"osm_all_roads{Path.DirectorySeparatorChar}osm_all_roads_");
        var output_name = $"HQ{reachabilityTool.NDAParamPanel.FloodReturnPeriod}";

        /*
            0. Python script
            1. Temp results path
            2. Road path
            3. Disruption type .shp file
            4. Output file name
        */
        var args = new string[] {
                "-Wignore",
                script,
                NDA_TempResultsPath,
                path_gdf,
                reachabilityTool.NDAParamPanel.DisruptionTypePath,
                output_name
            };

        currActiveProcess = new CustomProcess(PythonInterpreter, args);

        currActiveProcess.OnFinished += (_, e) =>
        {
            //string line;
            if (!currActiveProcess.Output.EndOfStream)
            {
                Debug.Log(currActiveProcess.Output.ReadToEnd());
                //while ((line = currActiveProcess.Output.ReadLine()) != null) Debug.Log(line);
            }

            if (!currActiveProcess.Errors.EndOfStream)
            {
                Debug.LogError(currActiveProcess.Errors.ReadToEnd());
                //while ((line = currActiveProcess.Errors.ReadLine()) != null) Debug.LogError(line);
            }
            else
            {
                if (!currActiveProcess.IsCancelled())
                {
                    // Debug.Log("RunCatchmentExposure done");
                    RunCatchmentAnalysis();
                }
            }
        };
        currActiveProcess.RunInBG();
    }

    private static void RunCatchmentAnalysis()
    {
        // Debug.Log("RunCatchmentAnalysis start");
        // Prep
        var script = Path.Combine(NDA_Path, "catchment_analysis.py");
        var floodReturnPeriod = $"HQ{reachabilityTool.NDAParamPanel.FloodReturnPeriod}";
        var travelTime = $"{reachabilityTool.traveTimeSlider.value * reachabilityTool.travelTimeScale}";

        /*
            0. Python script
            1. Temp results path
            2. Results path
            3. Output file name
            4. Flood return period
            5. Travel time
            6. Selected disruption categories
            7. Selected scenario .shp file
            8. Field
            9. Attribute
        */
        var args = new string[] {
                script,
                NDA_TempResultsPath,
                NDA_ResultsPath,
                reachabilityTool.NDAParamPanel.OutputName,
                floodReturnPeriod,
                travelTime,
                reachabilityTool.NDAParamPanel.Disruptions,
                reachabilityTool.NDAParamPanel.ScenarioPath,
                reachabilityTool.NDAParamPanel.Field,
                reachabilityTool.NDAParamPanel.Attribute
            };

        currActiveProcess = new CustomProcess(PythonInterpreter, args);

        currActiveProcess.OnFinished += (_, e) =>
        {
            //string line;
            if (!currActiveProcess.Output.EndOfStream)
            {
                Debug.Log(currActiveProcess.Output.ReadToEnd());
                //while ((line = currActiveProcess.Output.ReadLine()) != null) Debug.Log(line);
            }

            if (!currActiveProcess.Errors.EndOfStream)
            {
                Debug.LogError(currActiveProcess.Errors.ReadToEnd());
                //while ((line = currActiveProcess.Errors.ReadLine()) != null) Debug.LogError(line);
            }
            else
            {
                if (!currActiveProcess.IsCancelled())
                {
                    // Debug.Log("RunCatchmentAnalysis done");
                    isAnalysisCompleted = true;
                }
            }
        };
        currActiveProcess.RunInBG();
    }
}
#endif