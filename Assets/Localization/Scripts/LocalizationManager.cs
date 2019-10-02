// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli
//          Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using System.Text;

public interface ITranslator
{
	string Get(string original, bool addIfMissing = true);
}

public class PassThruTranslator : ITranslator
{
	public string Get(string original, bool addIfMissing) => original;
}

// Helper class to make code more readable
public static class Translator
{
	private static ITranslator translator = InitTranslator();
	public static string Get(string text, bool addIfMissing = true) => translator.Get(text, addIfMissing);

	private static ITranslator InitTranslator()
	{
		ITranslator instance = LocalizationManager.Instance;
		if (instance == null)
		{
			LocalizationManager.OnReady += OnTranslatorReady;
			instance = new PassThruTranslator();
		}
		return instance;
	}

	private static void OnTranslatorReady()
	{
		LocalizationManager.OnReady -= OnTranslatorReady;
		translator = LocalizationManager.Instance;
	}
}

public class LocalizationManager: MonoBehaviour, ITranslator
{
	private const string ConfigFilename = "config";

	public const string LanguageFile = "languages.csv";

	private const string ConfigLanguageKey = "Language";
	private const string DefaultLanguage = "English";

	public readonly List<string> languages = new List<string>();
	public string Current { get; private set; } = DefaultLanguage;
	private readonly Dictionary<string, string> localizedTexts = new Dictionary<string, string>();
	private string delimiters = "";
	private string languageFilePath;

	public static LocalizationManager Instance { get; private set; } = null;

	public static event UnityAction OnReady;
	public event UnityAction OnLanguageChanged;


	//
	// Unity Methods
	//

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
            return;
		}

		StartCoroutine(Init());
	}


	//
	// Public Methods
	//

	public void ChangeLanguage(int index)
	{
		StartCoroutine(ChangeLanguage(languages[index]));
	}

	public string Get(string original, bool addIfMissing = true)
	{
		if (string.IsNullOrWhiteSpace(original))
			return "";

		if (!localizedTexts.TryGetValue(original, out string translated))
		{
			if (addIfMissing)
			{
#if !UNITY_WEBGL
				using (StreamWriter sw = new StreamWriter(languageFilePath, true, Encoding.Unicode))
				{
					sw.WriteLine(TsvHelper.Escape(original) + delimiters);
				}
#endif
				localizedTexts.Add(original, null);
			}
			return original;
		}
        else if (string.IsNullOrWhiteSpace(translated))
        {
			return original;
        }

		return translated;
	}

	public static void WaitAndRun(UnityAction action)
	{
		if (Instance == null)
			OnReady += action;
		else
			action();
	}


	//
	// Private Methods
	//

	private IEnumerator Init()
	{
		languageFilePath = Path.Combine(Paths.Data, LanguageFile);

		// Read config file (or create a default one)
		yield return ReadConfigFile();

		// Read list of available languages and translations
		yield return FileRequest.GetText(languageFilePath, ReadLanguagesAndTranslations, OnLanguageFileDoesNotExist);

		// Update UI
		UpdateAllTexts();

		// Localization manager is finally ready
		Instance = this;
		OnReady?.Invoke();
	}

	private IEnumerator ReadConfigFile()
	{
		string configFile = Path.Combine(Paths.Data, ConfigFilename);
		yield return FileRequest.GetText(configFile, ParseConfigFile, () => OnConfigFileDoesNotExist(configFile));
	}

	private void ParseConfigFile(StreamReader sr)
	{
		while (!sr.EndOfStream)
		{
			string line = sr.ReadLine();
			if (line.StartsWith(ConfigLanguageKey))
			{
				Current = line.Substring(ConfigLanguageKey.Length + 1).Trim();
				break;
			}
		}
	}

	private void OnConfigFileDoesNotExist(string file)
	{
		Debug.LogWarning("Config file not found");
		CreateDefaultConfigFile(file);
	}

	private void CreateDefaultConfigFile(string file)
	{
#if !UNITY_WEBGL
		using (StreamWriter sw = new StreamWriter(file, false, Encoding.Unicode))
		{
			sw.WriteLine(ConfigLanguageKey + ":" + DefaultLanguage);
		}
#endif
	}

	private void UpdateConfigFile()
	{
#if !UNITY_WEBGL
		string configFile = Path.Combine(Paths.Data, ConfigFilename);
		string oldConfigFile = configFile + "_old";
		string line;
		bool found = false;
		File.Copy(configFile, oldConfigFile, true);
		using (StreamWriter sw = new StreamWriter(configFile, false, Encoding.Unicode))
		{
			using (StreamReader sr = new StreamReader(oldConfigFile))
			{
				while ((line = sr.ReadLine()) != null)
				{
					if (line.StartsWith(ConfigLanguageKey))
					{
						sw.WriteLine(ConfigLanguageKey + ":" + Current);
						found = true;
					}
					else
						sw.WriteLine(line);
				}
			}

			if (!found)
			{
				sw.WriteLine(ConfigLanguageKey + ":" + Current);
			}
		}
		File.Delete(oldConfigFile);
#endif
	}

	private IEnumerator ChangeLanguage(string language)
	{
		SetLanguage(language);
		
		// Load the translations from the language file
		yield return LoadTranslations();

		UpdateAllTexts();

		OnLanguageChanged?.Invoke();
	}

	private void SetLanguage(string language)
	{
		Current = language;
		UpdateConfigFile();
	}

	private IEnumerator LoadTranslations()
	{
		localizedTexts.Clear();

		yield return FileRequest.GetText(languageFilePath, ReadTranslations);
	}

	private void ReadLanguagesAndTranslations(StreamReader sr)
	{
		ReadLanguages(sr);

		if (languages.Count == 0)
			return;

		// Check if current language is in the list
		int index = languages.IndexOf(Current);
		if (index == -1)
		{
			index = 0;
			SetLanguage(languages[0]);
		}

		// Sort after languages after finding index
		languages.Sort();

		// Load the translations from the language file
		ReadTranslations(sr, index);
	}

	private void ReadLanguages(StreamReader sr)
	{
		languages.Clear();
		delimiters = "";

		// Read header
		string line = sr.ReadLine();
		if (line == null)
			return;

		MatchCollection matches = TsvHelper.regex.Matches(line);

		int columns = 0;
		if (matches != null)
		{
			columns = matches.Count;
			for (int i = 0; i < columns; i++)
			{
				var language = matches[i].Groups[2].Value;
				if (string.IsNullOrWhiteSpace(language))
					continue;

				if (languages.Contains(language))
					Debug.LogWarning("Language file contains duplicate language: " + language);
				else
					languages.Add(language);
			}
		}

		if (columns > 1)
			delimiters = new string(TsvHelper.Delimiter, columns - 1);
	}

	private void ReadTranslations(StreamReader sr)
	{
		// Read header
		string line = sr.ReadLine();
		MatchCollection matches = TsvHelper.regex.Matches(line);

		int column = 0;
		if (matches != null)
		{
			int columns = matches.Count;
			for (int i = 0; i < columns; i++)
			{
				if (matches[i].Groups[2].Value == Current)
				{
					column = i;
					break;
				}
			}
		}

		ReadTranslations(sr, column);
	}

	private void ReadTranslations(StreamReader sr, int column)
	{
		int languageCount = languages.Count;
		int lastLanguage = languageCount - 1;

		string line;
		MatchCollection matches;
		while ((line = sr.ReadLine()) != null)
		{
			if (string.IsNullOrWhiteSpace(line))
				continue;

			matches = TsvHelper.regex.Matches(line);
			while (matches.Count < languageCount ||
				(matches[lastLanguage].Groups[1].Value == "\"" &&
				!matches[lastLanguage].Groups[0].Value.EndsWith("\"")))
			{
				string extraLine = sr.ReadLine();
				if (extraLine == null)
					return;

				line += "\n" + extraLine;
				matches = TsvHelper.regex.Matches(line);
			}

			string original = matches[0].Groups[2].Value;
			if (string.IsNullOrWhiteSpace(original))
				continue;

			if (matches.Count <= column)
				continue;

			string translated = matches[column].Groups[2].Value;

			if (line[0] == '\"')
			{
				original = TsvHelper.Unescape(original);
				translated = TsvHelper.Unescape(translated);
			}

			if (!localizedTexts.ContainsKey(original))
				localizedTexts.Add(original, translated);
			else
				Debug.LogWarning("Duplicate line:" + original);
		}
	}

	private void OnLanguageFileDoesNotExist()
	{
		Debug.LogWarning("Language file not found!");
#if !UNITY_WEBGL
		CreateLanguageFile();
		using (var sr = new StreamReader(languageFilePath))
		{
			ReadLanguagesAndTranslations(sr);
		}
#endif
	}

#if !UNITY_WEBGL
	private void CreateLanguageFile()
	{
		using (var sw = new StreamWriter(languageFilePath, false, Encoding.Unicode))
		{
			sw.WriteLine("English\t ");
		}
	}
#endif

	private void UpdateAllTexts()
	{
#if !UNITY_WEBGL || UNITY_EDITOR
		// Check if it's the first time updating the texts
		if (Instance == null)
		{
			// Export all translatable texts before they get translated
			ExportTranslatableTexts();
		}
#endif

		var texts = FindObjectsOfType<LocalizedText>();
		foreach (var text in texts)
		{
			text.text = Get(text.OriginalText);
			LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);
		}
	}

	private void ExportTranslatableTexts()
	{
		var texts = Resources.FindObjectsOfTypeAll<LocalizedText>();
		foreach (var text in texts)
		{
			Get(text.text);
		}

#if UNITY_EDITOR
		Regex[] regexes = new Regex[] {
			new Regex("ranslator\\.Get\\(\\\"(.*?)\\\"\\)"),	// Regex: ranslator\.Get\(\"(.*?)\"\)
			new Regex("\\\"(.*?)\\\"\\/\\*translatable\\*\\/"),	// Regex: \"(.*?)\"\/\*translatable\*\/
		};

		string[] files = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
		foreach (var file in files)
		{
			using (var sr = new StreamReader(file))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					foreach (var regex in regexes)
					{
						MatchCollection matches = regex.Matches(line);
						foreach (Match match in matches)
						{
							Get(match.Groups[1].Value);
						}
					}
				}
			}
		}
#endif
	}
}
