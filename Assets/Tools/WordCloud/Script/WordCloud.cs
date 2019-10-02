// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//			Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Events;
using System.Text;
using SimpleJSON;

public class WordCloudDatabase
{
	public string name;
	public List<WordCloudTopic> topics = new List<WordCloudTopic>();

	public static IEnumerator LoadDataFromAPI(API api, UnityAction<WordCloudDatabase> callback)
	{
		yield return FileRequest.GetFromURL(api.URL, (data) =>
		{
			var db = ParseDataFromAPI(Encoding.UTF8.GetString(data), api);
			if (db != null)
				callback(db);
		});
	}

	private static WordCloudDatabase ParseDataFromAPI(string text, API setup)
	{
		var entriesDict = new Dictionary<string, List<GeolocatedValue>>();

		// Init Dictionary
		foreach (var value in setup.valueFields)
			entriesDict.Add(value, new List<GeolocatedValue>());

		var root = JSONNode.Parse(text);

		int missingCoords = 0;
		int invalidCoords = 0;
		int oobCoords = 0;
		bool hasValidValues = false;

		// Get coordinates from each node and pass the values for header from setup 
		foreach (var nodes in root)
		{
			var nodesDict = nodes.Value;

			// Check if long and lat names are in API data
			if (!nodesDict.HasKey(setup.latitudeField) || !nodesDict.HasKey(setup.longitudeField))
			{
				missingCoords++;
				continue;
			}

			var nodeLon = nodesDict.GetValueOrDefault(setup.longitudeField, null);
			var nodeLat = nodesDict.GetValueOrDefault(setup.latitudeField, null);

			if (!TryGetDouble(nodeLon, out double lon) || !TryGetDouble(nodeLat, out double lat))
			{
				invalidCoords++;
				continue;
			}

			if (lon > GeoCalculator.MaxLongitude || lon < GeoCalculator.MinLatitude || lat > GeoCalculator.MaxLatitude || lat < GeoCalculator.MinLatitude)
			{
				oobCoords++;
				continue;
			}

			var coord = new Coordinate
			{
				Longitude = lon,
				Latitude = lat
			};

			foreach (var valueField in setup.valueFields)
			{
				var node = nodesDict.GetValueOrDefault(valueField, null);
				if (node != null && !string.IsNullOrWhiteSpace(node.Value))
				{
					hasValidValues = true;
					entriesDict[valueField].Add(new GeolocatedValue
					{
						value = node.Value,
						coord = coord
					});
				}
			}
		}

		if (missingCoords > 0)
		{
			Debug.LogError(missingCoords + " missing coordinates in API: " + setup.name);
			return null;
		}
		if (invalidCoords > 0)
		{
			Debug.LogWarning(invalidCoords + " invalid coordinates in API: " + setup.name);
		}
		if (oobCoords > 0)
		{
			Debug.LogWarning(oobCoords + " out-of-bound coordinates in API: " + setup.name);
		}

		if (!hasValidValues)
			return null;

		// Create new database
		var database = new WordCloudDatabase();
		database.name = setup.name;

		// Create topic items from entries
		foreach (var pair in entriesDict)
		{
			var topic = new WordCloudTopic();
			topic.name = pair.Key;

			foreach (var entry in pair.Value)
			{
				topic.topicItems.Add(new TopicItem
				{
					coord = entry.coord,
					stringLabel = entry.value
				});
			}
			database.topics.Add(topic);
		}
		return database;
	}

	private static bool TryGetDouble(JSONNode node, out double value)
	{
		if (node.IsNumber)
		{
			value = node.AsDouble;
			return true;
		}

		if (node.IsString && double.TryParse(node.Value, out value))
			return true;

		value = 0;
		return false;
	}
}

public class WordCloudTopic
{
	public string name;
	public readonly List<TopicItem> topicItems = new List<TopicItem>();
	public readonly List<WordCloudItem> activeItems = new List<WordCloudItem>();
}

public class GeolocatedValue
{
	public Coordinate coord;
	public string value;
}

public class TopicItem
{
	public Coordinate coord;
	public string stringLabel;
	public bool grouped;
	public int groupId;
	public int stringId;
	public int size;
	public bool visible = true;
	public GameObject label;
	public Color color;

	public double Dist(TopicItem sr)
	{
		return coord.DistanceTo(sr.coord);
	}

	public void Lerp(TopicItem sr)
	{
		coord.Longitude = (coord.Longitude + sr.coord.Longitude) * 0.5f;
		coord.Latitude = (coord.Latitude + sr.coord.Latitude) * 0.5f;
	}
}

public class WordCloudItem
{
	public readonly Coordinate coord;
	public readonly TextMesh label;

	public WordCloudItem(Coordinate coord, TextMesh label)
	{
		this.coord = coord;
		this.label = label;
	}
}

public class WordCloud : Tool
{
	[Header("UI Reference")]
    public Toggle groupToggle;
	public ToggleGroup databaseToggleGroup;
	public Text message;

	[Header("Prefabs")]
    public TextMesh labelPrefab;
	public Toggle databasePrefab;
	public Toggle topicPrefab;
	public Transform containerPrefab;

    [Header("Misc")]
    [Range(0f, 0.1f)]
    public float groupDist = 0.0001f;
    [Range(1f, 60)]
    public float baseTextSize = 10f;
	[Range(1f, 60)]
	public float textScale = 14f;
	[Range(MapController.MinZoomLevel, MapController.MaxZoomLevel)]
    public float zoomStart = 11f;
	[Range(MapController.MinZoomLevel, MapController.MaxZoomLevel)]
	public float zoomEnd = 13f;

	private float groupDistMain = 0;
    private Transform labelsContainer;

    private readonly List<Transform> databaseToggles = new List<Transform>();
    private readonly List<Transform> topicContainers = new List<Transform>();
	private WordCloudTopic activeTopic;

    private float zoomNormalized;
	private Coroutine loadCR;


	//
	// Inheritance Methods
	//

	public override void OnToggleTool(bool isOn)
	{
		if (isOn)
		{
			TurnOn();
		}
		else
		{
			TurnOff();
		}
	}


    //
    // Private Methods
    //

    private void TurnOn()
    {
        groupDistMain = groupToggle.isOn? groupDist : 0;

		groupToggle.onValueChanged.AddListener(OnGroupItToggleChange);

        map = FindObjectOfType<MapController>();
        map.OnMapUpdate += OnMapUpdate;
        map.OnZoomChange += OnMapZoomChange;
		labelsContainer = new GameObject("WordCloud").transform;
		labelsContainer.SetParent(map.transform, false);

        // Force update
        UpdateZoom(map.zoom);

		var siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
		siteBrowser.OnAfterActiveSiteChange += OnAfterActiveSiteChange;

		LoadData(siteBrowser.ActiveSite);
	}

	private void OnAfterActiveSiteChange(Site newSite, Site previousSite)
	{
		LoadData(newSite);
	}

	private void LoadData(Site site)
	{
		if (loadCR != null)
		{
			StopCoroutine(loadCR);
		}

		loadCR = StartCoroutine(LoadData(site.Name));
	}

	private IEnumerator LoadData(string siteName)
	{
		ClearUI();

		var databases = new List<WordCloudDatabase>();

		message.text = Translator.Get("Loading data") + " ...";
		message.gameObject.SetActive(true);

		yield return null;

		var wordCloudPath = Paths.Data + "WordCloud";
		var path = wordCloudPath + Path.DirectorySeparatorChar + siteName;

        if (Directory.Exists(path))
		{
			var files = Directory.GetFiles(path, "*.csv");
            for (int i = 0; i < files.Length; ++i)
            {
                yield return SurveyConfig.Load(files[i], (s) =>
                {
                    if (s != null)
                    {
                        s.name = Path.GetFileNameWithoutExtension(files[i]);
                        databases.Add(s);
                    }
                });
            }
		}
        // connecting to API
        var configAPIPath = Paths.Data + "api.csv";
        API[] apis = null;
        yield return APIConfig.Load(configAPIPath, (a) => apis = a);
        if (apis != null)
        {
            for (int i = 0; i < apis.Length; ++i)
            {
                // check if site name is as currect or empty what is mark for global data
                if (apis[i].site.Contains(siteName) || apis[i].site == "*")
                {
                    yield return WordCloudDatabase.LoadDataFromAPI(apis[i], (db) => databases.Add(db));
                }
            }
        }

        if (databases.Count > 0)
		{
			message.gameObject.SetActive(false);
			groupToggle.interactable = true;
			CreateDataUI(databases);
		}
		else
		{
			groupToggle.interactable = false;
			message.text = Translator.Get("There's no data available for this site");

			if (Directory.Exists(wordCloudPath))
			{
				var dataManager = ComponentManager.Instance.Get<DataManager>();
				var dirs = Directory.GetDirectories(wordCloudPath);
				var examples = "\n" + Translator.Get("The folowing sites have data") + ": ";
				bool haveExamples = false;
				foreach (var dir in dirs)
				{
					var dirName = dir.Substring(wordCloudPath.Length + 1);
					if (dataManager.HasSite(dirName))
					{
						examples += dirName + ", ";
						haveExamples = true;
					}
				}
				if (haveExamples)
				{
					message.text += examples.Substring(0, examples.Length - 2);
				}
			}
		}
		loadCR = null;
	}

	private void CreateDataUI(List<WordCloudDatabase> databases)
	{
		foreach (var database in databases)
		{
			// Create database toggle
			var toggleDatabase = Instantiate(databasePrefab, transform, false);
			toggleDatabase.name = database.name;
			toggleDatabase.GetComponentInChildren<Text>().text = database.name;
			toggleDatabase.group = databaseToggleGroup;
			databaseToggles.Add(toggleDatabase.transform);

			var container = Instantiate(containerPrefab, transform, false);
			container.gameObject.SetActive(false);
			topicContainers.Add(container);

			toggleDatabase.onValueChanged.AddListener((isOn) => OnDatabaseToggleChange(isOn, container, database));

			// Create topic toggles
			foreach (var topic in database.topics)
			{
				var toggle = Instantiate(topicPrefab, container, false);
				toggle.name = topic.name;
				toggle.group = container.GetComponent<ToggleGroup>();
				toggle.GetComponentInChildren<Text>().text = topic.name;
				toggle.onValueChanged.AddListener((isOn) => OnTopicToggleChange(isOn, topic));
			}
		}
	}

    private void TurnOff()
    {
		// Stop coroutines
		if (loadCR != null)
		{
			StopCoroutine(loadCR);
			loadCR = null;
		}

		// Remove listeners
		ComponentManager.Instance.Get<SiteBrowser>().OnAfterActiveSiteChange -= OnAfterActiveSiteChange;
		groupToggle.onValueChanged.RemoveAllListeners();

		ClearUI();

		// Delete 3d labels container
		if (labelsContainer != null)
		{
			Destroy(labelsContainer.gameObject);
			labelsContainer = null;
		}
	}

	private void ClearUI()
	{
		// Delete 3D labels
		if (activeTopic != null)
		{
			DeleteAll(activeTopic.activeItems);
			activeTopic = null;
		}

		// Delete DB toggles
		foreach (var toggle in databaseToggles)
		{
			Destroy(toggle.gameObject);
		}
		databaseToggles.Clear();

		// Delete topic containers
		foreach (var container in topicContainers)
		{
			Destroy(container.gameObject);
		}
		topicContainers.Clear();
	}

	private void OnDatabaseToggleChange(bool isOn, Transform topicsContainer, WordCloudDatabase database)
    {
        if (isOn)
        {
			topicsContainer.gameObject.SetActive(true);
			if (topicsContainer.childCount > 0)
			{
				topicsContainer.GetChild(0).GetComponent<Toggle>().isOn = true;
			}
		}
        else
        {
			topicsContainer.GetComponent<ToggleGroup>().SetAllTogglesOff();
			topicsContainer.gameObject.SetActive(false);
        }
    }

	private void OnTopicToggleChange(bool isOn, WordCloudTopic topic)
	{
		// Delete old 3d labels
		if (activeTopic != null)
		{
			DeleteAll(activeTopic.activeItems);
		}

        // create 3d labels for each Survey result
        if (isOn)
        {
			activeTopic = topic;

			// TODO improve grouping algorithm 
			var groupedSet = PseudoGroupIt(topic.topicItems);

            for (int i = 0; i < groupedSet.Count; ++i)
            {
                //if (!groupedSet[i].grouped) 
                //{
					var label = Instantiate(labelPrefab, labelsContainer, false);
					label.transform.name = groupedSet[i].stringLabel;
					label.text = groupedSet[i].stringLabel;
					label.fontSize = (int)(baseTextSize + groupedSet[i].size * textScale);

                    Coordinate coords = groupedSet[i].coord;
					label.transform.localPosition = map.GetUnitsFromCoordinates(coords);
                   // Check if coordinates is not 0,0. Missing coordinates value is often market as 0,0 in APIs
                    if (coords.Latitude != 0 || coords.Longitude != 0)
                        activeTopic.activeItems.Add(new WordCloudItem(coords, label));
                //}
            }

			UpdateText();
			UpdatePositions(activeTopic.activeItems);
		}
		else
		{
			activeTopic = null;
		}
    }

    private void OnGroupItToggleChange(bool isOn)
    {
        groupDistMain = isOn? groupDist : 0;

		// Update active topic
		if (activeTopic != null)
		{
			OnTopicToggleChange(true, activeTopic);
		}
	}

	private void DeleteAll(List<WordCloudItem> items)
    {
		if (items != null)
		{
			foreach (var item in items)
			{
				Destroy(item.label.gameObject);
			}
			items.Clear();
		}
	}

    private void OnMapUpdate()
    {
		if (activeTopic != null)
		{
			UpdatePositions(activeTopic.activeItems);
		}
    }

    private void OnMapZoomChange(float zoom)
    {
        UpdateZoom(zoom);
        UpdateText();
    }

    private void UpdateZoom(float zoom)
    {
		zoomEnd = Mathf.Max(zoomEnd, zoomStart + 0.1f);
		zoomNormalized = Mathf.InverseLerp(zoomStart, zoomEnd, zoom);
    }

    private void UpdateText()
    {
		if (activeTopic == null)
			return;

        var invTextSize = 1f / textScale;

        // Biggest size is always visible
        float biggestSize = 1;
        foreach (var item in activeTopic.activeItems)
        {
            biggestSize = Mathf.Max(biggestSize, (item.label.fontSize - baseTextSize) * invTextSize);
        }

        // Exponential alpha change    
        foreach (var item in activeTopic.activeItems)
        {
            var size = (item.label.fontSize - baseTextSize) * invTextSize;
            var color = item.label.color;
            color.a = Mathf.Pow(zoomNormalized, biggestSize - size);
			item.label.color = color;
        }
    }

	private static readonly Quaternion NinetyDegreeZRotation = Quaternion.Euler(0, 0, 45);
    private void UpdatePositions(List<WordCloudItem> items)
    {
        if (items != null)
        {
			Quaternion q = Camera.main.transform.rotation * NinetyDegreeZRotation;
			foreach (var item in items)
            {
                var t = item.label.transform;
                t.localPosition = map.GetUnitsFromCoordinates(item.coord);
                t.rotation = q;
            }
        }
    }
	
    private List<TopicItem> PseudoGroupIt(List<TopicItem> set)
    {
        List<TopicItem> newSet = new List<TopicItem>();

        // copy items to secure that original is not overwritten
        foreach (var sr in set)
        {
			newSet.Add(new TopicItem
			{
				coord = sr.coord,
				groupId = sr.groupId,
				stringId = sr.stringId,
				size = sr.size,
				stringLabel = sr.stringLabel,
				grouped = false
			});
        }

        // Pseudo groupping algorithm check which items are same and close and lerp coords
		if (groupDistMain > 0)
		{
			int count = newSet.Count;
			for (int i = 0; i < count; ++i)
			{
				if (!newSet[i].grouped)
				{
					for (int j = 0; j < count; ++j)
					{
						if (i != j && !newSet[j].grouped && newSet[i].stringId == newSet[j].stringId)
						{
							bool isNear = newSet[i].Dist(set[j]) < groupDistMain;
							if (isNear)
							{
								newSet[i].Lerp(newSet[j]);
								newSet[i].size += 1;
								newSet[j].grouped = true;
							}
						}
					}
				}
			}
		}

		return newSet;
    }

}
