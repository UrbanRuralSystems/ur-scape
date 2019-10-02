// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections;

public class UrlSchemeHandler : MonoBehaviour
{
	private static readonly string Scheme = "urscape";
	private static readonly string OpenAction = "open";

	public void HandleUrl(string url)
	{
		if (string.IsNullOrEmpty(url))
			return;

		var dataManager = ComponentManager.Instance.Get<DataManager>();
		if (dataManager == null)
		{
			ComponentManager.Instance.OnRegistrationFinished += () => OnComponentRegistrationFinished (url);
			return;
		}
		else if (dataManager.sites == null || dataManager.sites.Count == 0)
		{
			OnComponentRegistrationFinished(url);
			return;
		}

		OnDataLoaded(url);
	}

	private void ParseUrl(string url)
	{
		var uri = new Uri(url);

		if (!uri.Scheme.Equals(Scheme))
			return;

		var uriParams = GetParams(uri.Query);

		if (uri.Host.Equals(OpenAction))
		{
			string siteName;
			if (uriParams.TryGetValue("site", out siteName) &&
				!string.IsNullOrEmpty(siteName))
			{
				ComponentManager.Instance.Get<SiteBrowser>().ChangeActiveSite(siteName);
			}
		}
	}

	private void OnComponentRegistrationFinished(string url)
	{
		var dataManager = ComponentManager.Instance.Get<DataManager>();
		dataManager.OnDataLoaded += () => OnDataLoaded(url);
	}

	private void OnDataLoaded(string url)
	{
		StartCoroutine(DelayedParse(url));
	}

	private IEnumerator DelayedParse(string url)
	{
        // Wait half a second after data is loaded and then parse url
		yield return new WaitForSeconds(0.5f);
		ParseUrl(url);
	}

	private Dictionary<string, string> GetParams(string uri)
	{
		var matches = Regex.Matches(uri, @"[\?&](([^&=]+)=([^&=#]*))", RegexOptions.Compiled);
		return matches.Cast<Match>().ToDictionary(
			m => Uri.UnescapeDataString(m.Groups[2].Value),
			m => Uri.UnescapeDataString(m.Groups[3].Value)
		);
	}
}
