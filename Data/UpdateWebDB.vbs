Const SitesPath = "Sites"
Const LayersFilename = "layers.csv"
Const OutputPath = "WebDB"
Dim HeadersFilename, PatchesFilename
HeadersFilename = OutputPath & "\headers"
PatchesFilename = OutputPath & "\patches"

Const ForReading = 1
Const ForWriting = 2
Const ForAppending = 8


UpdateWebDB

Sub UpdateWebDB()
	
	Dim fso
	Set fso = CreateObject("Scripting.FileSystemObject")

	' Create WebDB directory
	If Not fso.FolderExists(OutputPath) Then 
		fso.CreateFolder(OutputPath)
	End If
	
	
	' Open WebDB headers file for appending
	Dim headersFile
	On Error Resume Next
	Set headersFile = fso.OpenTextFile(HeadersFilename, ForWriting, True)
	If Err.number <> 0 Then
		WScript.Echo("Could not open " & HeadersFilename)
		MsgBox(Err.Description)
		Exit Sub
	End If
	On Error GoTo 0


	' Open WebDB Patches file for writting
	Dim patchesFile
	On Error Resume Next
	Set patchesFile = fso.OpenTextFile(PatchesFilename, ForWriting, True, -1)
	If Err.number <> 0 Then
		WScript.Echo("Could not open " & PatchesFilename)
		MsgBox(Err.Description)
		Exit Sub
	End If
	On Error GoTo 0


	'Open layers.csv file and detect Unicode
	Set layersFile = fso.OpenTextFile(layersFilename, ForReading, False)
	intAsc1Chr = Asc(layersFile.Read(1))
	intAsc2Chr = Asc(layersFile.Read(1))
	layersFile.Close
	
	Dim OpenAsUnicode
	OpenAsUnicode = 0
	If intAsc1Chr = 255 And intAsc2Chr = 254 Then 
		OpenAsUnicode = -1
	End If
	Set layersFile = fso.OpenTextFile(layersFilename, ForReading, 0, OpenAsUnicode)


	Dim sitesFolder
	Set sitesFolder = fso.GetFolder(SitesPath)

	' Skip layers.csv header
	Dim line
	line = layersFile.ReadLine


	Dim cells
	Dim layerName
	Dim filenameStart
	Dim found
	Dim binFilename
	Dim skipFile
	
	skipFile = False

	' Read each row of layers.csv
	Do Until layersFile.AtEndOfStream
		line = layersFile.ReadLine
		cells = Split(line, ",")
		layerName = cells(1)
		filenameStart = layerName & "_"
		
		' Ignore 'Group' rows
		If Len(layerName) > 0 AND cells(0) <> "Group" Then
			found = 0
			
			' Iterate through every subdirectory inside Sites
			For Each subfolder in sitesFolder.SubFolders
				' Ignore subdirectories starting with underscore
				If Not StartsWith(subfolder.Name, "_") Then
					For Each file in subfolder.Files
						' Only files starting with layer name and with 'bin' extension
						If StartsWith(file.Name, filenameStart) And UCase(fso.GetExtensionName(file.Name)) = "BIN" Then
							found = found + 1
							
							' Open bin file and copy the first N bytes
							binFilename = subfolder.Name & "\" & file.Name
							With file.OpenAsTextStream()
								If InStr(binFileName, "_grid") > 0 Then
									headersFile.Write(.Read(44))	' 44 = 2 Int32 (Version) + 4 doubles (WENS) + 1 Int32 (Category Count)
								ElseIf InStr(binFileName, "_multi") > 0 Then
									headersFile.Write(.Read(40))	' 40 = 2 Int32 (Version) + 4 doubles (WENS)
								ElseIf InStr(binFileName, "_graph") > 0 Then
									headersFile.Write(.Read(56))	' 56 = 2 Int32 (Version) + 4 doubles (WENS) + 2 doubles (cell size X & Y)
								Else
									skipFile = True
								End if
								.Close
							End With

							If skipFile Then
								skipFile = False
							Else
								' Output file to WebDB patches
								patchesFile.WriteLine(subfolder.Name & "/" & file.Name)
							End If
						End If
					Next
				End If
			Next
			
			If found = 0 Then
				WScript.Echo("Could not find patches for layer " & layerName)
			Else
				WScript.Echo("Added headers for layer " & layerName)
			End If
		End If
	Loop
	
	layersFile.Close	
	patchesFile.Close
	headersFile.Close
	
End Sub

Function StartsWith(str, prefix)
    StartsWith = Left(str, Len(prefix)) = prefix
End Function
