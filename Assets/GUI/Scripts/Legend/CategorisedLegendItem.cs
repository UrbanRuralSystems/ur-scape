// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CategorisedLegendItem : LegendItem
{
    [SerializeField] private RectTransform categoriesInfo = default;

    [Header("Prefabs")]
    [SerializeField] private GameObject categoryItem = default;

    private readonly Dictionary<string, GameObject> categoryItems = new Dictionary<string, GameObject>();

    //
    // Unity Methods
    //

    private void Awake()
    {
        Init();
    }

    //
    // Event Methods
    //

    protected override void OnGroupToggleChanged(bool isOn)
    {
        arrow.rectTransform.eulerAngles = new Vector3(0.0f, 0.0f, isOn ? 0.0f : -90.0f);
        categoriesInfo.gameObject.SetActive(isOn);
        StartCoroutine(DelayedReEnableLayout(2));
    }

    //
    // Public Methods
    //

    public void AddCategory(string name, Color color)
    {
        if (!categoryItems.ContainsKey(name))
        {
            var item = Instantiate(categoryItem, categoriesInfo, false);
            var text = item.GetComponentInChildren<Text>();
            var image = item.GetComponentInChildren<Image>();

            item.name = text.text = name;
            image.color = color;

            categoryItems.Add(name, item);

            if (categoryItems.Count > 0)
            {
                groupToggle.gameObject.SetActive(true);
            }
            else
            {
                groupToggle.isOn = false;
                groupToggle.gameObject.SetActive(false);
            }

            StartCoroutine(DelayedReEnableLayout(2));
        }
    }

    public void RemoveCategory(string name)
    {
        if (categoryItems.TryGetValue(name, out GameObject item))
        {
            Destroy(item);
            categoryItems.Remove(name);

            if (categoryItems.Count > 0)
            {
                groupToggle.gameObject.SetActive(true);
            }
            else
            {
                groupToggle.isOn = false;
                groupToggle.gameObject.SetActive(false);
            }

            StartCoroutine(DelayedReEnableLayout(2));
        }
    }

    //
    // Private Methods
    //



}