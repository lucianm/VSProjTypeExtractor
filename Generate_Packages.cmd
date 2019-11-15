@echo off
SetLocal EnableExtensions
SetLocal EnableDelayedExpansion

set "packageName=VSProjTypeExtractor"
set "BinariesDir=bin\x64\Release"
set "DirBinaryDist=tmp\Dist\BinaryDist"
set "DirSdkDist=tmp\Dist\SdkDist"

:: initial cleanup
rmdir /S /Q tmp\Dist
del /F /Q %packageName%*.zip

:: prepare directory structure
for %%f in (%DirBinaryDist% %DirSdkDist%\bin %DirSdkDist%\include %DirSdkDist%\lib) do (
	mkdir %%f
)

:: copy binaries
copy %BinariesDir%\%packageName%*.dll %DirBinaryDist%
copy %BinariesDir%\%packageName%*.dll %DirSdkDist%\bin

:: copy documentation
copy LICENSE %DirBinaryDist%
copy *.md %DirBinaryDist%
copy LICENSE %DirSdkDist%
copy *.md %DirSdkDist%

:: copy header
copy %packageName%\%packageName%.h %DirSdkDist%\include

:: copy library
copy %BinariesDir%\%packageName%.lib %DirSdkDist%\lib

:: extract version from built DLL
for /F "USEBACKQ" %%f in (`powershell -NoLogo -NoProfile -Command ^(Get-Item %BinariesDir%\%packageName%.dll^).VersionInfo.FileVersion`) do (set "packageVersion=%%f")

:: compress binary and SDK distributions
7z.exe a -mmt=%NUMBER_OF_PROCESSORS% -mx=9 -tzip %packageName%Binaries-%packageVersion%.zip .\%DirBinaryDist%\*
7z.exe a -mmt=%NUMBER_OF_PROCESSORS% -mx=9 -tzip %packageName%Sdk-%packageVersion%.zip .\%DirSdkDist%\*

:: final cleanup
rmdir /S /Q tmp\Dist

EndLocal
