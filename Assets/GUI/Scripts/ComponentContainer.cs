// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ComponentContainer : MonoBehaviour
{
    [Header("Prefabs (Optional)")]
    public Toggle colapseTogglePrefab;
    public RectTransform dividerPrefab;
    public RectTransform[] panelPrefabs;

    //
    // Unity Methods
    //

    private IEnumerator Start()
    {
        int count = panelPrefabs.Length;
        if (count > 0)
        {
            Create(panelPrefabs[0]);
            for (int i = 1; i < count; i++)
            {
                if (dividerPrefab != null)
                    Create(dividerPrefab);

                Create(panelPrefabs[i]);
            }
        }

        yield return WaitFor.Frames(WaitFor.InitialFrames);

        var mapViewArea = ComponentManager.Instance.Get<MapViewArea>();

        if (colapseTogglePrefab != null && mapViewArea != null)
        {
            var toggle = Instantiate(colapseTogglePrefab, mapViewArea.transform, false);
            toggle.name = colapseTogglePrefab.name;
            toggle.onValueChanged.AddListener((on) => gameObject.SetActive(on));
        }
    }


    //
    // Private Methods
    //

    private T Create<T>(T prefab) where T: Object
    {
        T instance = Instantiate(prefab, transform, false);
        instance.name = prefab.name;
        return instance;
    }


}
