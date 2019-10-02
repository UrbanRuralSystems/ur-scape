// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public class DataFormat
{
	public string name;
	public string[] extensions;
	public DataFormat(string name, params string[] extensions)
	{
		this.name = name;
		this.extensions = extensions;
	}
}

public class BasicInfo
{
	public AreaBounds bounds;
	public bool isRaster;
	public int width;               // Only applicable if isRaster is true
	public int height;              // Only applicable if isRaster is true
	public double degreesPerPixel;  // Only applicable if isRaster is true
	public string suggestedLayerName;
	public string suggestedUnits;
}

public abstract class Interpreter
{
	public abstract DataFormat GetDataFormat();
	public abstract BasicInfo GetBasicInfo(string filename);
	public abstract bool Read(string filename, ProgressInfo progress, out PatchData data);
	public abstract bool Write(string filename, Patch patch);

	public readonly static List<DataFormat> DataFormats = new List<DataFormat>();
	private readonly static Dictionary<string, Type> extensionToInterpreterMap = new Dictionary<string, Type>();
	private readonly static Dictionary<Type, Interpreter> interpreters = new Dictionary<Type, Interpreter>();

	static Interpreter()
	{
		Assembly currAssembly = Assembly.GetExecutingAssembly();
		Type baseType = typeof(Interpreter);

		foreach (Type type in currAssembly.GetTypes())
		{
			if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(baseType))
			{
				if (Activator.CreateInstance(type) is Interpreter interpreter)
				{
					var dataFormat = interpreter.GetDataFormat();
					if (dataFormat != null && dataFormat.extensions != null && dataFormat.extensions.Length > 0)
					{
						//UnityEngine.Debug.Log("Registering " + dataFormat.name + " (" + string.Join(",", dataFormat.extensions)  + ")");

						DataFormats.Add(dataFormat);
						foreach (var extension in dataFormat.extensions)
						{
							if (extensionToInterpreterMap.ContainsKey(extension))
							{
								Debug.LogError("Interpreter already exists for extension " + extension);
							}
							else
							{
								extensionToInterpreterMap.Add(extension, interpreter.GetType());
							}
						}
					}
				}
			}
		}
	}

	public static Interpreter Get(string filename)
	{
		Interpreter interpreter = null;
		var extension = Path.GetExtension(filename).Substring(1);
		if (extensionToInterpreterMap.TryGetValue(extension, out Type type))
		{
			if (!interpreters.TryGetValue(type, out interpreter))
			{
				interpreter = Activator.CreateInstance(type) as Interpreter;
				interpreters.Add(type, interpreter);
			}
		}
		return interpreter;
	}

}
