// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DataLayerInfo : MonoBehaviour
{
	[Serializable]
	public class CategoriesGroup
    {
		[SerializeField] private GameObject categoriesGroup = default;
		[SerializeField] private Toggle groupToggle = default;
		[SerializeField] private Image arrow = default;
		[SerializeField] private GameObject categoriesContainer = default;

		public GameObject CatGroup { get { return categoriesGroup; } }
		public Toggle GroupToggle { get { return groupToggle; } }
		public Image Arrow { get { return arrow; } }
		public GameObject CategoriesContainer { get { return categoriesContainer; } }
	}

	[Header("UI References")]
	[SerializeField] private Image dot = default;
	[SerializeField] private Text nameLabel = default;
	[SerializeField] private Text label = default;    // for filter and single category
	[SerializeField] private CategoriesGroup categoriesGroup = default;

	[Header("Prefabs")]
	[SerializeField] private Text categoryLabel = default;  // for multiple categories

	private Transform listContainer;
	private LayoutGroup layoutGroup;
	private LayoutGroup listContainerLayerGroup;

	//
	// Unity Methods
	//

	private void Start()
    {
		listContainer = ComponentManager.Instance.Get<ContoursTool>().InfoPanel.container;
		layoutGroup = GetComponent<LayoutGroup>();
		listContainerLayerGroup = listContainer.GetComponent<LayoutGroup>();

		categoriesGroup.GroupToggle.onValueChanged.AddListener(OnGroupToggleChanged);
		categoriesGroup.GroupToggle.isOn = false;
    }

	//
	// Event Methods
	//

	private void OnGroupToggleChanged(bool isOn)
	{
		categoriesGroup.Arrow.rectTransform.eulerAngles = new Vector3(0.0f, 0.0f, isOn ? 0.0f : -90.0f);
		categoriesGroup.CategoriesContainer.SetActive(isOn);
        StartCoroutine(DelayedReEnableLayout(2));
    }

    private IEnumerator DelayedReEnableLayout(int numOfFrames)
    {
		yield return new WaitForFrames(numOfFrames);
		GuiUtils.RebuildLayout(layoutGroup.transform);

		yield return new WaitForFrames(numOfFrames);
		GuiUtils.RebuildLayout(listContainerLayerGroup.transform);
	}

	//
	// Public Methods
	//

	public void SetDotColor(Color color)
	{
		dot.color = color;
	}

	public void SetName(string nameString)
	{
		nameLabel.text = nameString;
	}

	public void SetLabel(string labelString)
	{
		label.text = labelString;
	}

	public void ShowLabel(bool show)
    {
		label.gameObject.SetActive(show);
    }

	public void ShowCategoriesGroup(bool show)
	{
		categoriesGroup.CatGroup.SetActive(show);
	}

	public void AddCategoryToGroup(string categoryName)
    {
		var category = Instantiate(categoryLabel, categoriesGroup.CategoriesContainer.transform);
		category.text = categoryName;
    }

	//
	// Private Methods
	//

}