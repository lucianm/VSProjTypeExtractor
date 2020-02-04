# VSProjTypeExtractor

Visual Studio project type GUID extractor


## What is it all about?

**_VSProjTypeExtractor_** can be used in **[FASTBuild](http://fastbuild.org/docs/home.html)** together with the new
_VSProjectExternal_ function to automatically determine the `ProjectTypeGuid` from the actual external visual studio
project:
```
VSProjectExternal( 'SomeExternal-vsproj' )
{
	.ExternalProjectPath = 'path_to\ExternalProject.csproj'
}
```


## How does it work?

**_VSProjTypeExtractor_** consists of 2 DLLs, `VSProjTypeExtractorManaged.dll` which is written in C# and is responsible
of the actual work, by automating Visual Studio in the background for loading the project file just to be able to query
the type GUID, and the `VSProjTypeExtractor.dll` **C** wrapper which can be used in any native apllication to call this
functionality.


## Requirements, Usage

**_VSProjTypeExtractor_** targets .NET Framework v4.7.2, so the 2 DLLs which you can download from the
[Releases](https://github.com/lucianm/VSProjTypeExtractor/releases) section will need this version of the .NET framework
to be installed on your system. Just place them somewhere in the 'PATH' or along with FBuild.exe (a build supporting
_VSProjectExternal_, post-v0.99 in any case) in the same directory.

The module has a configuration file [VsProjTypeExtractorManaged.xml](https://github.com/lucianm/VSProjTypeExtractor/blob/master/VSProjTypeExtractorManaged/VsProjTypeExtractorManaged.xml),
please have a look in the comments there if logging should be enabled (it is default off) or Visual Studio automation timing needs to be tuned.


## Developer information on integrating in applications

Integrating **_VSProjTypeExtractor_** in your application is quite easy, you can do it eitehr by linking against
_VSProjTypeExtractor.lib_, which will make your application runtime dependent on **_VSProjTypeExtractor.dll_**, or even
simpler, by including [VSProjTypeExtractor.h](https://github.com/lucianm/VSProjTypeExtractor/blob/master/VSProjTypeExtractor/VSProjTypeExtractor.h)
after a `#define VSPROJTYPEEXTRACTOR_DYNLOAD` statement, which will benefit you of only requiring **_VSProjTypeExtractor.dll_**
at runtime if the _VSProjectExternal_ functionality is used without providing a `ProjectTypeGuid` in the BFF declaration.

### Static integration by linking against _VSProjTypeExtractor.lib_

- You will need the latest VSProjTypeExtractorSdk-x.x.x.x.zip from the [Releases](https://github.com/lucianm/VSProjTypeExtractor/releases) section.
- Make sure to link against _VSProjTypeExtractor.lib_;
- the only really necessary API call is `Vspte_GetProjTypeGuidString`, on the first call it will start a hidden Visual Studio instance and create
a volatile solution, this will take few seconds on the first call, but subsequent calls (for reading several more project Guids from other files)
will be very quick;
- optionally, `Vspte_CleanUp()` can be called, but at application exit this will be called anyway on destruction of objects and garbage collection;
- sample code:
```
#include <VSProjTypeExtractor.h>

char projTypeGuid[VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH] = { 0 };
bool bSuccess = Vspte_GetProjTypeGuidString(
	charPtrToYourExternalProject,
	projTypeGuid,
	VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH);
if (bSuccess) {
	// projTypeGuid will contain the project type Guid
}


// optionally, when really no longer needed
Vspte_CleanUp();

```

### Dynamic (less invasive) integration

- You will just need a copy of the latest header [VSProjTypeExtractor.h](https://github.com/lucianm/VSProjTypeExtractor/blob/master/VSProjTypeExtractor/VSProjTypeExtractor.h)
in your code, and at runtime the latest VSProjTypeExtractorBinaries-x.x.x.x.zip from the [Releases](https://github.com/lucianm/VSProjTypeExtractor/releases)
section is needed.
- Make sure to `#define VSPROJTYPEEXTRACTOR_DYNLOAD` before including the header;
- As opposed to the static integration, a little bit more API calls and caution are needed, but for your convenience, a singleton `VspteModuleWrapper`
taking care of dynamically loading the DLL, mapping the function pointers and cleaning up is already provided in the header.
- Calling `VspteModuleWrapper::Instance()->Vspte_GetProjTypeGuidString`, the first time will start a hidden Visual Studio instance and create
a volatile solution, this will take few seconds on the first call, but subsequent calls (for reading several more project Guids from other files)
will be very quick;
- optionally, `VspteModuleWrapper::Instance()->Vspte_CleanUp()` can be called, but at application exit this will be called anyway on destruction of
objects and garbage collection
- sample code:
```
#include <Windows.h>
#define VSPROJTYPEEXTRACTOR_DYNLOAD
#include <VSProjTypeExtractor.h>

if (!VspteModuleWrapper::Instance()->IsLoaded()) {
	// load the module if not already loaded
	VspteModuleWrapper::Instance()->Load();
}

// anytime before retrieving project guids, make sure to check if loading succeeded:
if (VspteModuleWrapper::Instance()->IsLoaded()) {
	char projTypeGuid[VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH] = { 0 };
	bool bSuccess = VspteModuleWrapper::Instance()->Vspte_GetProjTypeGuidString(
		charPtrToYourExternalProject,
		projTypeGuid,
		VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH);
	if (bSuccess) {
		// projTypeGuid will contain the project type Guid, use it where needed
	}
}


// optionally, when really no longer needed
if (VspteModuleWrapper::Instance()->IsLoaded()) {
	VspteModuleWrapper::Instance()->Vspte_CleanUp();
}

```

© 2020 Lucian Muresan
