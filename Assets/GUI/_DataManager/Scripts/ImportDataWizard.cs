// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class ImportInfo
{
	public class ImportFinishedEvent : UnityEvent<Site, DataLayer> { }

	public string inputFilename;
	public string outputFilename;

	public int fieldIndex;

	public Site site;
	public string newSiteName;

	public LayerGroup group;
	public string newGroupName;

	public DataLayer layer;
	public string newLayerName;
	public Color newLayerColor;

	public IntCategory[] categories;

	public int level;

	public int year;
	public int month;

	public string units;

	public DataResolution resolution;

	public bool needsResampling;

	public List<MetadataPair> metadata;

	public ImportFinishedEvent OnFinishImport;
}

public class ImportThreadInfo
{
	public string inputFilename;
	public string outputFilename;
	public string units;
	public List<MetadataPair> metadata;
	public IntCategory[] categories;	//?

	public Thread thread;
	public bool running;
	public ProgressInfo progress = new ProgressInfo();

	public Resampler resampler;
	public double resX;
	public double resY;
}

public class ImportDataWizard : MonoBehaviour
{
    [Header("UI Referencess")]
    public WizardDialog wizardDlg;
    public ImportDataPanel importDataPanel;
    public GameObject resamplingSettingsPanel;

	public ImportInfo.ImportFinishedEvent OnFinishImport = new ImportInfo.ImportFinishedEvent();

	//
	// Unity Methods
	//

	private void Awake()
    {
        //importDataPanel.Init(wizardDlg);
        //wizardDlg.title.text = Translator.Get("Import Data");
        //wizardDlg.Show(this, importDataPanel.gameObject);
    }


    //
    // Inheritance Methods
    //
/*
    public GameObject OnWizardNext()
    {
		if (wizardDlg.Current == importDataPanel.gameObject && DataExists())
		{
			//AskToReplaceExistingData(() => wizardDlg.Next(GetNextWizardPanel()));
			return null;
		}

		return GetNextWizardPanel();
    }

	public bool OnWizardFinish()
	{
		if (wizardDlg.Current == importDataPanel.gameObject && DataExists())
		{
			//AskToReplaceExistingData(ImportData);
			return false;
		}

		ImportData();

		return false;
	}

	public bool OnWizardBack()
    {
        return true;
    }

    public bool OnWizardClose()
    {
        return true;
    }

    public void OnWizardPanelChanged(GameObject previous, GameObject current)
    {
    }


	//
	// Private Methods
	//

	private GameObject GetNextWizardPanel()
	{
		//if (wizardDlg.Current == importDataPanel)
			//return resamplingSettingsPanel;

		return null;
	}


	private void ImportData()
	{
		
		var info = new ImportInfo
		{
			inputFilename = importDataPanel.FullFilename,
			OnFinishImport = OnFinishImport
		};
		InitInfoCategories(info);

        var dataImporter = new GameObject("DataImporter").AddComponent<DataImporter>();
		dataImporter.Import(info);
		

		wizardDlg.CloseDialog(DialogAction.Ok);
	}

/*	private bool DataExists()
	{
		return File.Exists(importDataPanel.GetOutputFilename());
	}*/

}
