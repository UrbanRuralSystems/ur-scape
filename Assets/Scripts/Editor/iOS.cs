// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System.IO;

public class iOS
{
	[PostProcessBuildAttribute(1)]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
		if (target == BuildTarget.iOS)
		{
			if (File.Exists("ios_post.sh"))
			{
				Command("ios_post.sh " + pathToBuiltProject);
			}
		}
	}

	private static void Command(string cmd)
	{
		var processInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", cmd);
		processInfo.UseShellExecute = false;
		processInfo.RedirectStandardOutput = true;
		processInfo.RedirectStandardError = true;

		var process = System.Diagnostics.Process.Start(processInfo);
		process.ErrorDataReceived += delegate(object sender, System.Diagnostics.DataReceivedEventArgs e) {
			Debug.LogError(e.Data);
		};
		process.OutputDataReceived += delegate(object sender, System.Diagnostics.DataReceivedEventArgs e) {
			Debug.Log(e.Data);
		};
		process.Exited += delegate(object sender, System.EventArgs e) {
			Debug.LogError(e.ToString());
		};

		do{
			string line = process.StandardOutput.ReadLine();
			if(line == null){
				break;
			}
			Debug.Log(line);
		}while(true);

		do{
			string line = process.StandardError.ReadLine();
			if(line == null){
				break;
			}
			Debug.LogError(line);
		}while(true);

		process.WaitForExit();
		process.Close();
	}
}
