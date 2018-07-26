Const ForAppending = 8

Dim StdIn, StdOut
Set StdIn = WScript.StdIn
Set StdOut = WScript.StdOut

JoinHeaders WScript.Arguments(0), WScript.Arguments(1), WScript.Arguments(2)
'WScript.Echo( WScript.Arguments(0) & " | " & WScript.Arguments(1) & " | " & WScript.Arguments(2))
'JoinHeaders "Sites\", "Sites\Lists\Aerosol_patches.txt", "Sites\Lists\headers.bin"

Sub JoinHeaders(layersDir, list, outFileName)
	
	Dim fso
	Set fso = CreateObject("Scripting.FileSystemObject")

	Dim outFile
	On Error Resume Next
	Set outFile = fso.OpenTextFile(outFileName, ForAppending, True)

	If Err.number <> 0 Then 
		MsgBox(Err.Description)
		Exit Sub
	End If
	On Error GoTo 0

	Dim bytes, count
	Dim inFile, binFileName
	Set listFile = fso.OpenTextFile(list)
	
	Do Until listFile.AtEndOfStream
		binFileName = listFile.ReadLine

		Set inFile = fso.GetFile(layersDir & binFileName)
		If Not IsNull(inFile) Then
			With inFile.OpenAsTextStream()
				If InStr(binFileName, "_grid") > 0 Then
					outFile.Write(.Read(36))	' 36 = 4 doubles (WENS) + 1 int (Category Count)
				ElseIf InStr(binFileName, "_graph") > 0 Then
					outFile.Write(.Read(48))	' 48 = 4 doubles (WENS) + 2 doubles (cell size X & Y)
				End if
				.Close
			End With
		End If
	Loop

	listFile.Close	
	outFile.Close
	
End Sub
