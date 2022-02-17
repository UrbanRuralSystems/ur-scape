// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ScalePanel : MonoBehaviour
{
    private class SavedLocation
    {
        public string name;
        public double lat;
        public double lon;
        public float distance;
    };

    [Header("UI References")]
    [SerializeField] private Button saveCurrScale = default;
    [SerializeField] private Button saveCurrLocation = default;
    [SerializeField] private RectTransform neckRT = default;
    [SerializeField] private RectTransform scalePanelRT = default;
    [SerializeField] private RectTransform savedScalesListContainer = default;
    [SerializeField] private RectTransform savedLocationsListContainer = default;

    [Header("Prefabs")]
    [SerializeField] private SavedScaleListItem savedScaleListItemPrefab = default;
    [SerializeField] private SavedLocationListItem savedLocationListItemPrefab = default;

    private MapController map;
    private Scalebar scalebar;

    private float? currentMapZoom;
    private double[] currentMapCentre = null;
    private readonly List<float> savedScales = new List<float>();
    private readonly List<SavedLocation> savedLocations = new List<SavedLocation>();

    private const string SavedScalesFile = "savedScales";
    private const string SavedLocationsFile = "savedLocations";

    //
    // Unity Methods
    //

    private void Start()
    {
		var componentManager = ComponentManager.Instance;
        map = componentManager.Get<MapController>();

        saveCurrScale.onClick.AddListener(OnSaveCurrScaleClick);
        saveCurrLocation.onClick.AddListener(OnSaveCurrLocationClick);
    }

    private void OnDisable()
    {
        UpdateSavedScales();
        UpdateSavedLocations();
    }

    //
    // Event Methods
    //

    public void OnSaveCurrScaleClick()
    {
        currentMapZoom = map.zoom;
        AddSavedScale();
    }

    private void OnRestoreSavedScaleClick()
    {
        if (currentMapZoom.HasValue)
            map.SetZoom(currentMapZoom.Value);
    }

    public void OnSaveCurrLocationClick()
    {
        currentMapZoom = map.zoom;
        AddSavedLocation();
    }

    private void OnRestoreSavedLocationClick()
    {
        if (currentMapZoom.HasValue)
            map.SetZoom(currentMapZoom.Value);

        if (currentMapCentre != null)
            map.SetCenter(currentMapCentre[1], currentMapCentre[0]);
    }

    //
    // Public Methods
    //

    public bool IsMouseOutsideScalePanel()
    {
        var saveCurrScaleRT = saveCurrScale.GetComponent<RectTransform>();

        Vector2 inputPos0 = saveCurrScaleRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos1 = neckRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos2 = scalePanelRT.InverseTransformPoint(Input.mousePosition);

        return !saveCurrScaleRT.rect.Contains(inputPos0) &&
                !neckRT.rect.Contains(inputPos1) &&
                !scalePanelRT.rect.Contains(inputPos2);
    }

    public IEnumerator Init(Scalebar scalebar)
    {
        this.scalebar = scalebar;

        yield return LoadSavedScales();

        // Update Scales UI
        if (savedScales.Count > 0)
            UpdateAllSavedScales();;

        yield return LoadSavedLocations();

        // Update Locations UI
        if (savedLocations.Count > 0)
            UpdateAllSavedLocations();

        gameObject.SetActive(false);
    }

    //
    // Private Methods
    //

#region Scale
    private IEnumerator LoadSavedScales()
    {
        string savedScalesFile = Path.Combine(Paths.Data, SavedScalesFile);
        yield return FileRequest.GetText(savedScalesFile, ParseSavedScalesFile, () => OnSavedScalesFileDoesNotExist(savedScalesFile));
    }

    private void ParseSavedScalesFile(StreamReader sr)
    {
        using (sr)
        {
            // Read first entry
            string line = sr.ReadLine();
            if (string.IsNullOrEmpty(line))
                return;

            float.TryParse(line, out float scale);
            savedScales.Add(scale);
            currentMapZoom = (float)scalebar.GetMapZoom(scalebar.maxSize, scale);

            while (!sr.EndOfStream)
            {
                line = sr.ReadLine();
                float.TryParse(line, out scale);
                savedScales.Add(scale);
            }
        }
    }

    private void OnSavedScalesFileDoesNotExist(string file)
    {
        Debug.LogWarning("SavedScales file not found");
        CreateSavedScalesConfigFile(file);
    }

    private void CreateSavedScalesConfigFile(string file)
    {
        scalebar.GetScalebarDistanceParams(out _, out _, out _, out int number, out string unit);
        int distance = number * (unit.Equals("km") ? 1000 : 1);
        savedScales.Add(distance);

#if !UNITY_WEBGL
        using (StreamWriter sw = new StreamWriter(file, false))
        {
            sw.WriteLine($"{distance}");
        }
#endif
    }

    private void UpdateAllSavedScales()
    {
        foreach (var distance in savedScales)
        {
            string unit = distance > 1000.0f ? "km" : "m";
            int number = (int)(distance * (string.Equals(unit, "km") ? 0.001f : 1.0f));
            AddToSavedScalesList(number, unit);
        }
    }

    private void AddSavedScale()
    {
        scalebar.GetScalebarDistanceParams(out _, out _, out _, out int number, out string unit);
        AddToSavedScalesList(number, unit);
    }

    private void AddToSavedScalesList(int number, string unit)
    {
        var savedScaleListItem = Instantiate(savedScaleListItemPrefab, savedScalesListContainer);
        savedScaleListItem.Init(number, unit);

        // Initialize listeners
        savedScaleListItem.ItemButton.onClick.AddListener(delegate ()
        {
            currentMapZoom = (float)scalebar.GetMapZoom(scalebar.maxSize, number * (string.Equals(unit, "km") ? 1000 : 1));
            OnRestoreSavedScaleClick();
        });
    }

    private void UpdateSavedScales()
    {
        int count = savedScalesListContainer.childCount;
#if !UNITY_WEBGL
        string savedScalesFile = Path.Combine(Paths.Data, SavedScalesFile);
        using (StreamWriter sw = new StreamWriter(savedScalesFile))
        {
            // Save all saved scales to file
            for (int i = 0; i < count; ++i)
            {
                var savedScaleListItem = savedScalesListContainer.GetChild(i).GetComponent<SavedScaleListItem>();

                var distanceVal = savedScaleListItem.DistanceVal;
                var distanceData = distanceVal.text.Split(' ');
                float.TryParse(distanceData[0], out float distance);
                string unit = distanceData[1];
                distance *= string.Equals(unit, "km") ? 1000.0f : 1.0f;

                sw.WriteLine($"{distance}");
            }
        }
#endif
    }
#endregion

#region Location
    private IEnumerator LoadSavedLocations()
    {
        string savedLocationsFile = Path.Combine(Paths.Data, SavedLocationsFile);
        yield return FileRequest.GetText(savedLocationsFile, ParseSavedLocationsFile, () => OnSavedLocationsFileDoesNotExist(savedLocationsFile));
    }

    private void ParseSavedLocationsFile(StreamReader sr)
    {
        using (sr)
        {
            // Read first entry
            string line = sr.ReadLine();
            if (string.IsNullOrEmpty(line))
                return;

            var data = line.Split(',');
            string name = data[0];
            double.TryParse(data[1], out double lat);
            double.TryParse(data[2], out double lon);
            float.TryParse(data[3], out float scale);
            savedLocations.Add(new SavedLocation { name = name, lat = lat, lon = lon, distance = scale });
            currentMapZoom = (float)scalebar.GetMapZoom(scalebar.maxSize, scale);

            while (!sr.EndOfStream)
            {
                line = sr.ReadLine();
                data = line.Split(',');
                name = data[0];
                double.TryParse(data[1], out lat);
                double.TryParse(data[2], out lon);
                float.TryParse(data[3], out scale);
                savedLocations.Add(new SavedLocation { name = name, lat = lat, lon = lon, distance = scale });
            }
        }
    }

    private void OnSavedLocationsFileDoesNotExist(string file)
    {
        Debug.LogWarning("SavedLocations file not found");
        CreateSavedLocationsConfigFile(file);
    }

    private void CreateSavedLocationsConfigFile(string file)
    {
        scalebar.GetScalebarDistanceParams(out _, out _, out _, out int number, out string unit);
        int distance = number * (unit.Equals("km") ? 1000 : 1);
        var firstEntry = new SavedLocation { name = "Default", lat = map.Latitude, lon = map.Longitude, distance = distance };
        savedLocations.Add(firstEntry);
        string line = $"{firstEntry.name},{firstEntry.lat},{firstEntry.lon},{firstEntry.distance}";

#if !UNITY_WEBGL
        using (StreamWriter sw = new StreamWriter(file, false))
        {
            sw.WriteLine(line);
        }
#endif
    }

    private void UpdateAllSavedLocations()
    {
        foreach (var savedScale in savedLocations)
        {
            string unit = savedScale.distance > 1000.0f ? "km" : "m";
            int number = (int)(savedScale.distance * (string.Equals(unit, "km") ? 0.001f : 1.0f));
            AddToSavedLocationsList(number, unit, savedScale.lat, savedScale.lon, savedScale.name);
        }
    }

    private void AddSavedLocation()
    {
        scalebar.GetScalebarDistanceParams(out _, out _, out _, out int number, out string unit);
        AddToSavedLocationsList(number, unit, map.Latitude, map.Longitude);
    }

    private void AddToSavedLocationsList(int number, string unit, double lat, double lon, string label = null)
    {
        var savedLocationListItem = Instantiate(savedLocationListItemPrefab, savedLocationsListContainer);
        savedLocationListItem.Init(number, unit, lat, lon, label);

        // Initialize listeners
        savedLocationListItem.ItemButton.onClick.AddListener(delegate ()
        {
            currentMapZoom = (float)scalebar.GetMapZoom(scalebar.maxSize, number * (string.Equals(unit, "km") ? 1000 : 1));
            currentMapCentre = new double[2] { lat, lon };
            OnRestoreSavedLocationClick();
        });
    }

    private void UpdateSavedLocations()
    {
        int count = savedLocationsListContainer.childCount;
#if !UNITY_WEBGL
        string savedLocationsFile = Path.Combine(Paths.Data, SavedLocationsFile);
        using (StreamWriter sw = new StreamWriter(savedLocationsFile))
        {
            // Save all saved scales to file
            for (int i = 0; i < count; ++i)
            {
                var savedLocationListItem = savedLocationsListContainer.GetChild(i).GetComponent<SavedLocationListItem>();

                var distanceVal = savedLocationListItem.DistanceVal;
                var distanceData = distanceVal.text.Split(' ');
                float.TryParse(distanceData[0], out float distance);
                string unit = distanceData[1];
                distance *= string.Equals(unit, "km") ? 1000.0f : 1.0f;

                sw.WriteLine($"{savedLocationListItem.EntryName.text},{savedLocationListItem.Lat},{savedLocationListItem.Lon},{distance}");
            }
        }
#endif
    }
#endregion
}