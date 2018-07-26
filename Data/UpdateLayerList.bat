@ECHO OFF
setlocal EnableDelayedExpansion

SET "_SitesPath=Sites"
SET "_InputFile=layers.csv"
SET "_OutputPath=WebDB"
SET "_HeadersFile=%_OutputPath%\headers"
SET "_PatchesFile=%_OutputPath%\patches"

IF NOT EXIST %_OutputPath% MKDIR %_OutputPath%

rem Clear headers bin file
TYPE NUL>"!_HeadersFile!"

rem Clear the patches file
TYPE NUL>"!_PatchesFile!"

SET n=0

rem Iterate every line of the CSV
FOR /F "tokens=*" %%A in (%_InputFile%) DO (
	SET line="%%A"
	FOR /f "tokens=1-2* delims=," %%B in ("!line:,="^,"!") DO (
		rem Check that the first token is not "Group" or "Type"
		IF NOT "%%~B" == "" IF NOT "%%~B" == "Group" IF NOT "%%~B" == "Type" (
		
			rem Set the temporary output file name
			SET "_LayerPatchesFile=%_OutputPath%\%%~C_patches.txt"
			
			rem Clear the output file (just in case it wasn't deleted)
			TYPE NUL>!_LayerPatchesFile!
			
			SET "Found=0"
			
			pushd %_SitesPath%
			FOR /D %%D in (*) DO (
				SET "subdir=%%D"
				
				rem Ignore subdirectories starting with underscore
				IF NOT "!subdir:~0,1!"=="_" (
				
					rem Write list to LayerPatchesFile
					FOR %%F in ("%%D\%%~C_*.bin") DO (
						ECHO %%F>>"..\!_LayerPatchesFile!"
					)
					
					DIR "%%D\%%~C_*.bin" >nul 2>nul
					IF NOT ERRORLEVEL 1 (
						IF "!Found!"=="0" (
							ECHO Adding headers for layer %%~C
						)
						SET "Found=1"
					)
				)
			)
			popd
			
			IF "!Found!"=="0" (
				ECHO Could not find patches for %%~C layer
			) ELSE (
				cscript //nologo JoinHeaders.vbs "%_SitesPath%\" "!_LayerPatchesFile!" "!_HeadersFile!"

				rem Get each line from LayerPatchesFile, replace \ with / and add it to PatchesFile
				FOR /f "usebackq tokens=*" %%T in ("!_LayerPatchesFile!") DO (
				 	SET "war=%%T"
					CALL SET war=%%war:\=/%%
				 	ECHO !war!>>"!_PatchesFile!"
				)
			)
					
			rem Delete temporary LayerPatchesFile
			DEL "!_LayerPatchesFile!"
			
			SET vector[!n!]=%%~C
			SET /A n+=1
		)
	)
)
SET /A n-=1

GOTO skipfilecheck
ECHO.
ECHO.
ECHO Checking for ignored files ... (you can skip this by pressing Ctrl+C)

pushd %_SitesPath%
FOR /D %%D in (*) DO (
 	SET "subdir=%%D"

	rem Ignore subdirectories starting with underscore
	IF NOT "!subdir:~0,1!"=="_" (
		pushd %%D
		
		rem Iterate through all bin files in this sub dir
		FOR %%F in ("*.csv") DO (
			SET "Found=0"
			rem echo %%F
			
			rem Iterate through all layer names
			FOR /L %%V in (0,1,%n%) DO (
				rem check if file name starts with layer name
				ECHO %%F|findstr /c:"!vector[%%V]!" >nul && SET "Found=1"
			)
			IF "!Found!"=="0" (
				ECHO %%D\%%F is being ignored
			)
		)
		popd
	)
)
popd

:skipfilecheck

ECHO.
ECHO Done!
ECHO.
PAUSE
