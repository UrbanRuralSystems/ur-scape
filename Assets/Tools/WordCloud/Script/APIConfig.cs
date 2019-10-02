// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using System.Collections.Generic;
using System.IO;

public static class APIConfig
{
    public enum AttributeId
    {
        Name,
		Site,
		Format,
        Url,
        Longitude,
        Latitude,
        Value,
        Type,
        Group,
        Username,
        Password,
        Token
    }

    public class Attribute
    {
        public readonly AttributeId id;
        public readonly string label;
        public readonly bool isRequired;

        public Attribute(AttributeId id, string label, bool isRequired)
        {
            this.id = id;
            this.label = label;
            this.isRequired = isRequired;
        }
    }

    public static readonly Attribute[] Attributes = {
						// Id	     		Label		Required	
		new Attribute(AttributeId.Name,     "Name",      true),
		new Attribute(AttributeId.Site,     "Site",      true),
		new Attribute(AttributeId.Format,   "Format",    true),
		new Attribute(AttributeId.Url,      "Url",       true),
		new Attribute(AttributeId.Longitude,"Longitude", true),
		new Attribute(AttributeId.Latitude, "Latitude",  true),
		new Attribute(AttributeId.Value,    "Value",     true),
		new Attribute(AttributeId.Type,     "Type",      false),
		new Attribute(AttributeId.Group,    "Group",     false),
		new Attribute(AttributeId.Username, "Username",  false),
		new Attribute(AttributeId.Password, "Password",  false),
		new Attribute(AttributeId.Token,    "Token",     false),
    };

	private static readonly int RequiredAttributesCount = CountRequiredAttributes();
	private static int CountRequiredAttributes()
	{
		int count = 0;
		foreach (var attrib in Attributes)
			if (attrib.isRequired)
				count++;
		return count;
	}

	public static IEnumerator Load(string filename, UnityAction<API[]> callback)
    {
        yield return FileRequest.GetText(filename, (stream) => callback(ParseAPIConfigFile(stream, filename)));
    }

    private static API[] ParseAPIConfigFile(StreamReader sr, string filename)
    {
        List<API> setups = new List<API>();

		// Read header row
		string line = sr.ReadLine();
        var matches = CsvHelper.regex.Matches(line);

		var headers = new List<string>();
		for (int i = 0; i < matches.Count; ++i)
		{
			headers.Add(matches[i].Groups[2].Value);
		}

		// Create API setup for each line
		while ((line = sr.ReadLine()) != null)
        {
            var setup = ParseAPI(line, headers);
			if (setup != null)
				setups.Add(setup);
        }

        return setups.ToArray();
    }

    private static API ParseAPI(string line, List<string> headers)
    {
        var api = new API();
        var matches = CsvHelper.regex.Matches(line);

		int requiredCount = 0;
		for (int i = 0; i < matches.Count; ++i)
		{
			var header = headers[i];
			var cell = matches[i].Groups[2].Value;

			// Check if attribute has values
			if (!HasAttribute(header, cell, out Attribute attribute))
			{
				if (attribute != null && attribute.isRequired)
				{
					Debug.LogWarning("Error reading API config file. Missing required attribute: " + header);
					return null;
				}
				continue;
			}

			if (attribute.isRequired)
				requiredCount++;

			// Assign the attribute value to the API object
			switch (attribute.id)
			{
				case AttributeId.Name:
					api.name = cell;
					break;
				case AttributeId.Site:
					api.site = cell;
					break;
				case AttributeId.Format:
					api.format = cell;
					break;
				case AttributeId.Url:
					api.URL = cell;
					break;
				case AttributeId.Longitude:
					api.longitudeField = cell;
					break;
				case AttributeId.Latitude:
					api.latitudeField = cell;
					break;
				case AttributeId.Value:
					api.valueFields = cell.Split(',');
					break;
				case AttributeId.Type:
					api.type = cell;
					break;
				case AttributeId.Group:
					bool.TryParse(cell, out api.isGroupDefault);
					break;
				case AttributeId.Username:
					api.username = cell;
					break;
				case AttributeId.Password:
					api.password = cell;
					break;
				case AttributeId.Token:
					api.token = cell;
					break;
			}
		}

		if (requiredCount < RequiredAttributesCount)
			return null;

		return api;
    }

    public static bool HasAttribute(string header, string cell, out Attribute attribute)
    {
		for (int i = 0; i < Attributes.Length; i++)
        {
            if (Attributes[i].label.EqualsIgnoreCase(header))
            {
				attribute = Attributes[i];
				return !string.IsNullOrWhiteSpace(cell);
			}
        }

		Debug.LogWarning("API config file has unknown header: " + header);

		attribute = null;
		return false;
    }
}

