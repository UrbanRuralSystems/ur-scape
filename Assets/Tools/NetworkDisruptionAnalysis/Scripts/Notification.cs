// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class Notification : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button closeButton = default;
    [SerializeField] private Text title = default;
    [SerializeField] private Text message = default;

    //[Header("Prefabs")]

    private string newTitle, newMessage;

    //
    // Unity Methods
    //

    private void Start()
    {
        closeButton.onClick.AddListener(OnCloseClick);
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    //
    // Event Methods
    //

    private void OnCloseClick()
    {
        gameObject.SetActive(false);
    }

    private void OnLanguageChanged()
    {
        var translator = LocalizationManager.Instance;
        title.text = translator.Get(newTitle);
        message.text = translator.Get(newMessage);
    }

    //
    // Public Methods
    //

    public void Init(string titleVal, string messageVal)
    {
        var translator = LocalizationManager.Instance;
        title.text = newTitle = translator.Get(titleVal);
        message.text = newMessage = translator.Get(messageVal);
    }

    //
    // Private Methods
    //

    
}