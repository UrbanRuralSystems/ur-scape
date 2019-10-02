// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class LogosPanel : MonoBehaviour
{
	public float maxLogoWidth = 100;
	public float maxLogoHeight = 150;

	private Dictionary<Patch, List<string>> patches = new Dictionary<Patch, List<string>>();
	private Dictionary<string, LogoInfo> logos = new Dictionary<string, LogoInfo>();

	private class LogoInfo
	{
		public int count = 1;
		public readonly GameObject go;
		public LogoInfo(GameObject go)
		{
			this.go = go;
		}
	}

	//
	// Unity Methods
	//

	private void Start()
	{
		ComponentManager.Instance.OnRegistrationFinished += OnRegistrationFinished;
	}

	private void OnDestroy()
	{
		if (ComponentManager.Instance.Has<DataLayers>())
		{
			var dataLayers = ComponentManager.Instance.Get<DataLayers>();
			dataLayers.OnLayerVisibilityChange -= OnDataLayerVisibilityChange;
		}
	}

	//
	// Event Methods
	//

	private void OnRegistrationFinished()
	{
		var componentManager = ComponentManager.Instance;

		var dataLayers = componentManager.Get<DataLayers>();
		dataLayers.OnLayerVisibilityChange += OnDataLayerVisibilityChange;
	}

	private void OnDataLayerVisibilityChange(DataLayer layer, bool visible)
	{
		if (visible)
			layer.OnPatchVisibilityChange += OnPatchVisibilityChange;
		else
			layer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
	}

	private void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
	{
		if (visible)
		{
			if (TryGetLogo(patch, out string patchLogoURLs))
			{
				var matches = CsvHelper.regex.Matches(patchLogoURLs);
				if (matches.Count > 0)
				{
					var logoURLs = new List<string>();
					patches.Add(patch, logoURLs);

					foreach (Match match in matches)
					{
						var logoURL = match.Groups[2].Value;
						logoURLs.Add(logoURL);

						if (logos.TryGetValue(logoURL, out LogoInfo info))
						{
							info.count++;
						}
						else
						{
							string dir = Path.GetDirectoryName(patch.Filename);
							string filename = Path.Combine(dir, logoURL);

							var logoTexture = new Texture2D(2, 2);
							logoTexture.LoadImage(File.ReadAllBytes(filename)); // This will auto-resize the texture
							var logoSprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), Vector2.zero, 100);

							var logoImage = new GameObject("Logo").AddComponent<Image>();
							logoImage.preserveAspect = true;
							logoImage.sprite = logoSprite;
							logoImage.transform.SetParent(transform, false);
							var rt = logoImage.transform as RectTransform;
							rt.pivot = Vector2.one;
							float height = logoTexture.height;
							float width = logoTexture.width;
							if (width > maxLogoWidth)
							{
								height = maxLogoWidth * height / width;
								width = maxLogoWidth;
							}
							rt.sizeDelta = new Vector2(width, Mathf.Min(maxLogoHeight, height));

							info = new LogoInfo(logoImage.gameObject);
							logos.Add(logoURL, info);
						}
					}
				}
			}
		}
		else
		{
			if (patches.TryGetValue(patch, out List<string> patchLogos))
			{
				patches.Remove(patch);

				foreach (var patchLogo in patchLogos)
				{
					var info = logos[patchLogo];
					info.count--;
					if (info.count == 0)
					{
						logos.Remove(patchLogo);
						Destroy(info.go);
					}
				}
			}
		}

		GuiUtils.RebuildLayout(transform);
	}

	private static bool TryGetLogo(Patch patch, out string logo)
	{
		if (patch.Data is GridData gridData && gridData.metadata != null)
		{
			if (gridData.metadata.TryGetValue("Logo", out logo))
				return true;
		}

		logo = null;
		return false;
	}
}
