// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker  (neudecker@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class DataLayerGroupPanel : MonoBehaviour
{
    [Header("UI References")]
    public Toggle groupToggle;
    public Text title;
    public Image arrow;
    public RectTransform layersContainer;

    //
    // Unity Methods
    //

    private void Start()
    {
        groupToggle.onValueChanged.AddListener(OnGroupToggleChanged);
        ComponentManager.Instance.Get<SiteBrowser>().OnAfterActiveSiteChange += OnAfterActiveSiteChange;
    }

    //
    // Event Methods
    //

    private void OnGroupToggleChanged(bool isOn)
    {
        arrow.rectTransform.eulerAngles = new Vector3(0.0f, 0.0f, isOn ? 0.0f : -90.0f);
        layersContainer.gameObject.SetActive(isOn);
        GuiUtils.RebuildLayout(transform);
    }

    private void OnAfterActiveSiteChange(Site site, Site previousSite)
    {
        groupToggle.isOn = true;
    }

    //
    // Public Methods
    //

    public void Init(string name)
    {
        UpdateName(name);
    }

    public void UpdateName(string name)
    {
        this.name = name;
        title.text = name;
    }

    public DataLayerPanel AddLayer(DataLayerPanel prefab, DataLayer layer)
    {
        // Create a new instance of the layer prefab and initialize it
        var layerCtrl = Instantiate(prefab);
        // Important: set the parent AFTER Instantiate to allow toggles to awake
        // Parent could be disabled and prevent proper initialization
        layerCtrl.transform.SetParent(layersContainer, false);
        layerCtrl.Init(layer);
        return layerCtrl;
    }

    public void Show(bool show)
    {
        if (gameObject.activeSelf != show)
            gameObject.SetActive(show);
    }

    public void UpdateVisibility()
    {
        int count = layersContainer.childCount;
        for (int i = 0; i < count; ++i)
        {
            if (layersContainer.GetChild(i).gameObject.activeSelf)
            {
                if (!gameObject.activeSelf)
                    gameObject.SetActive(true);
                return;
            }
        }
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
