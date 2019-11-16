@echo off
SetLocal EnableExtensions
SetLocal EnableDelayedExpansion

set "packageName=VSProjTypeExtractor"
set "BinariesSubdirRelease=bin\x64\Release"
set "BinariesSubdirDebug=bin\x64\Debug"
set "DirBinaryDist=tmp\Dist\BinaryDist"
set "DirSdkDist=tmp\Dist\SdkDist"

:: initial cleanup
rmdir /S /Q tmp\Dist
del /F /Q %packageName%*.zip

:: prepare directory structure
for %%f in (%DirBinaryDist% %DirSdkDist%\%BinariesSubdirRelease% %DirSdkDist%\%BinariesSubdirDebug% %DirSdkDist%\include) do (
	mkdir %%f
)

:: copy binaries
copy %BinariesSubdirRelease%\%packageName%*.dll %DirBinaryDist%
for %%f in (%BinariesSubdirRelease% %BinariesSubdirDebug%) do (
	copy %%f\%packageName%*.dll %DirSdkDist%\%%f
	copy %%f\%packageName%.pdb %DirSdkDist%\%%f
	copy %%f\%packageName%Managed.pdb %DirSdkDist%\%%f
	copy %%f\%packageName%.lib %DirSdkDist%\%%f
)

:: copy documentation
copy LICENSE %DirBinaryDist%
copy *.md %DirBinaryDist%
copy LICENSE %DirSdkDist%
copy *.md %DirSdkDist%

:: copy header
copy %packageName%\%packageName%.h %DirSdkDist%\include

:: extract version from built DLL
for /F "USEBACKQ" %%f in (`powershell -NoLogo -NoProfile -Command ^(Get-Item %BinariesSubdirRelease%\%packageName%.dll^).VersionInfo.FileVersion`) do (set "packageVersion=%%f")

:: compress binary and SDK distributions
7z.exe a -mmt=%NUMBER_OF_PROCESSORS% -mx=9 -tzip %packageName%Binaries-%packageVersion%.zip .\%DirBinaryDist%\*
7z.exe a -mmt=%NUMBER_OF_PROCESSORS% -mx=9 -tzip %packageName%Sdk-%packageVersion%.zip .\%DirSdkDist%\*

:: final cleanup
rmdir /S /Q tmp\Dist

EndLocal
