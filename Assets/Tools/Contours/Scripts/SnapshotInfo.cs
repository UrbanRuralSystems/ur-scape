// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SnapshotInfo : MonoBehaviour
{
	[Header("UI References")]
	public Text title;
	public Text value;
	public Image snapshotColour;
    public Transform container;
	[SerializeField] private Toggle groupToggle = default;
	[SerializeField] private Image arrow = default;

	[Header("Prefabs")]
	[SerializeField] private UncategorisedDataLayerItem uncategorisedItemPrefab = default;
	[SerializeField] private CategorisedDataLayerItem categorisedItemPrefab = default;

	[Header("Settings")]
	public Color disabledKey = Color.gray;
	public Color disabledValue = Color.gray;

	private Color originalTitleColor;
	private Color originalValueColor;
	private DataLayers dataLayers;
	private LayoutGroup layoutGroup;

    //
    // Unity Methods
    //

    private void Awake()
	{
		originalTitleColor = title.color;
		originalValueColor = value.color;

		if (disabled)
			SetDisabledColors();

		var componentManager = ComponentManager.Instance;
		dataLayers = componentManager.Get<DataLayers>();
		layoutGroup = componentManager.Get<ContoursTool>().InfoPanel.container.GetComponent<LayoutGroup>();

		// Initialize listener
		//translator.OnLanguageChanged += OnLanguageChanged;
		groupToggle.onValueChanged.AddListener(OnGroupToggleChanged);
	}

	//
	// Public Methods
	//

	private bool disabled = false;
	public bool Disabled
	{
		get
		{
			return disabled;
		}
		set
		{
			if (disabled != value)
			{
				disabled = value;
				if (isActiveAndEnabled)
				{
					if (disabled)
						SetDisabledColors();
					else
						SetOriginalColors();
				}
			}
		}
	}

	public void SetTitle(string title)
	{
		this.title.text = title;
	}

	public void SetValue(string value)
	{
		this.value.text = value;
	}

	public void SetTitleValue(string title, string value)
	{
		this.title.text = title;
		this.value.text = value;
	}

	public string GetTitle()
	{
		return title.text;
	}

	public string GetValue()
	{
		return value.text;
	}

	public void InitInfoGroup()
	{
		foreach (var layerPanel in dataLayers.activeLayerPanels)
		{
			var dataLayer = layerPanel.DataLayer;
			if (!dataLayers.availableLayers.Contains(dataLayer))
				continue;

			if (!dataLayer.HasLoadedPatchesInView())
				continue;

			var data = dataLayer.loadedPatchesInView[0].Data;
			var gridData = data as GridData;
			bool isCategorized = gridData.IsCategorized;

			if (isCategorized)
			{
				var item = Instantiate(categorisedItemPrefab, container, false);
				item.SetDisplayName(dataLayer.Name);
				item.SetDotColor(dataLayer.Color);

				int catCount = gridData.categories.Length;
				int selectedCount = gridData.categoryFilter.GetSetCount(catCount);

				if (selectedCount > 1)
				{
					for (int c = 0; c < catCount; ++c)
					{
						string name = gridData.categories[c].name;

						// Add active categories if it does not exist in legendItems
						// Destroy and remove inactive categories if it exists in legendItems
						if (gridData.categoryFilter.IsSet(c))
						{
							item.AddCategory(name);
						}
					}
				}
				else if (selectedCount == 1)
				{
					for (int c = 0; c < catCount; ++c)
					{
						string name = gridData.categories[c].name;

						// Add active categories if it does not exist in legendItems
						// Destroy and remove inactive categories if it exists in legendItems
						if (gridData.categoryFilter.IsSet(c))
						{
							item.AddCategory(name);
							break;
						}
					}
				}
			}
			else
			{
				var item = Instantiate(uncategorisedItemPrefab, container, false);
				item.SetDisplayName(dataLayer.Name);
				item.SetDotColor(dataLayer.Color);

				float diff = dataLayer.MaxVisibleValue - dataLayer.MinVisibleValue;
				float min = dataLayer.MinFilter * diff + dataLayer.MinVisibleValue;
				float max = dataLayer.MaxFilter * diff + dataLayer.MinVisibleValue;

				item.SetFilterRange(min, max, gridData.units);
			}
		}
	}

	public void SetAsSnapshot(bool isSnapshot)
    {
		snapshotColour.gameObject.SetActive(isSnapshot);
		groupToggle.gameObject.SetActive(isSnapshot);
		container.gameObject.SetActive(isSnapshot);

		if (isSnapshot)
			InitInfoGroup();
    }

	//
	// Private Methods
	//

	private void SetDisabledColors()
	{
		title.color = disabledKey;
		value.color = disabledValue;
	}

	private void SetOriginalColors()
	{
		title.color = originalTitleColor;
		value.color = originalValueColor;
	}

	private IEnumerator DelayedReEnableLayout(int numOfFrames)
	{
		yield return new WaitForFrames(numOfFrames);
		GuiUtils.RebuildLayout(layoutGroup.transform);
	}

	//
	// Event Methods
	//

	//private void OnLanguageChanged()
	//{
	//	int count = container.childCount;

	//	for (int i = 0; i < count; ++i)
	//	{
	//		var dataLayerInfo = container.GetChild(i).GetComponent<DataLayerInfo>();
	//		var label = dataLayerInfo.label.text;
	//		dataLayerInfo.SetLabel(translator.Get(label));
	//	}
	//}

	private void OnGroupToggleChanged(bool isOn)
	{
		arrow.rectTransform.eulerAngles = new Vector3(0.0f, 0.0f, isOn ? 0.0f : -90.0f);
		value.gameObject.SetActive(isOn);
		container.gameObject.SetActive(isOn);
		StartCoroutine(DelayedReEnableLayout(2));
	}
}