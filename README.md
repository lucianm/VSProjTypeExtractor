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
- the most important API call is `Vspte_GetProjData`, on the first call it will start a hidden Visual Studio instance and create
a volatile solution, this will take few seconds on the first call, but subsequent calls (for reading several more project Guids from other files)
will be very quick;
- before any subsequent call, please make sure to call `Vspte_DeallocateProjDataCfgArray` on the already used ExtractedProjData object;
- optionally, at the end `Vspte_CleanUp()` can be called, but at application exit this will be called anyway on destruction of objects and garbage collection;
- sample code:
```
#include <VSProjTypeExtractor.h>

ExtractedProjData projData;;
bool bSuccess = Vspte_GetProjData(
	pathToYourExternalProject,
	projData);
if (bSuccess) {
	// projData will contain the project type Guid and config/platform pairs found in the project
}

// if Vspte_GetProjData needs to be called again subsequently, projData should be deallocated:
Vspte_DeallocateProjDataCfgArray(projData);


// optionally, when really no longer needed
Vspte_CleanUp();

```

### Dynamic (less invasive) integration

- Write a header, or [grab the one contained in the test application, *VSProjLoaderInterface.h*](https://github.com/lucianm/VSProjTypeExtractor/blob/master/VSProjTypeExtractorTest/VSProjLoaderInterface.h)
in which you should make sure to use exact copies of the #defines for VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH and VSPROJ_MAXSTRING_LENGTH, and also of the structs ExtractedCfgPlatform and ExtractedProjData,
for the rest of the implementation you are of course free to modify the code as you like, but if it were a copy it should look like this sample:
```
/*
    Implements loading / unloading VSProjTypeExtractor.dll dynamically, in order to avoid linking against the .lib
*/

#include <Windows.h>

//
// still need to define these ourselves, as an exact copy of the  respective #defines and structs from VSProjTypeExtractor.h:
//
#define VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH     39
#define VSPROJ_MAXSTRING_LENGTH              120

// configuration / platform pair
typedef struct
{
	char _config[VSPROJ_MAXSTRING_LENGTH];
	char _platform[VSPROJ_MAXSTRING_LENGTH];
} ExtractedCfgPlatform;

// extracted project data containing type GUID and array of found configuration / platform pairs
typedef struct
{
	char _TypeGuid[VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH];
	ExtractedCfgPlatform* _pConfigsPlatforms;
	unsigned int _numCfgPlatforms;
} ExtractedProjData;

// prototypes of exported functions
typedef bool  (__stdcall *Type_GetProjData)(const char* projPath, ExtractedProjData* pProjData);
typedef void* (__stdcall *Type_CleanUp)(void);
typedef void* (__stdcall *Type_DeallocateProjDataCfgArray)(ExtractedProjData* pProjData);


//
// wrapper around the external VSProjTypeExtractor.dll functionality
class VspteModuleWrapper
{
private:
	VspteModuleWrapper()
	{}
	~VspteModuleWrapper()
	{
		if (_hVSProjTypeExtractor)
		{
			Vspte_CleanUp();
			::FreeLibrary(_hVSProjTypeExtractor);
			//
			_hVSProjTypeExtractor = NULL;
			_Vspte_GetProjData = nullptr;
			_Vspte_CleanUp = nullptr;
			_Vspte_DeallocateProjDataCfgArray = nullptr;
		}
	}
	Type_GetProjData _Vspte_GetProjData = nullptr;
	Type_CleanUp _Vspte_CleanUp = nullptr;
	Type_DeallocateProjDataCfgArray _Vspte_DeallocateProjDataCfgArray = nullptr;
	HMODULE _hVSProjTypeExtractor = NULL;


public:
	/** @brief  Access to singleton

	*/
	static VspteModuleWrapper* Instance()
	{
		static VspteModuleWrapper s_Instance;
		return &s_Instance;
	}

	/** @brief  Loads VSProjTypeExtractor.dll if possible

		Needs to be called once before attempting to call @Vspte_GetProjData, @Vspte_DeallocateProjDataCfgArray or @Vspte_CleanUp
	*/
	void Load()
	{
		try {
			if (!_hVSProjTypeExtractor)
			{
				_hVSProjTypeExtractor = ::LoadLibrary("VSProjTypeExtractor");
				if (_hVSProjTypeExtractor)
				{
					_Vspte_GetProjData = reinterpret_cast<Type_GetProjData>(::GetProcAddress(_hVSProjTypeExtractor, "Vspte_GetProjData"));
					_Vspte_CleanUp = reinterpret_cast<Type_CleanUp>(::GetProcAddress(_hVSProjTypeExtractor, "Vspte_CleanUp"));
					_Vspte_DeallocateProjDataCfgArray = reinterpret_cast<Type_DeallocateProjDataCfgArray>(::GetProcAddress(_hVSProjTypeExtractor, "Vspte_DeallocateProjDataCfgArray"));
				}
			}
		}
		catch (...)
		{
		}
	}

	/** @brief  Queries if VSProjTypeExtractor.dll was successfully loaded

		Helpful for avoiding to call @Vspte_GetProjData, @Vspte_DeallocateProjDataCfgArray or @Vspte_CleanUp without effect
	*/
	bool IsLoaded()
	{
		return _hVSProjTypeExtractor != NULL && _Vspte_GetProjData != nullptr && _Vspte_CleanUp != nullptr && _Vspte_DeallocateProjDataCfgArray != nullptr;
	}

	/** @brief  Retrieves basic project data from an existing project

		The project data is extracted by silently automating the loading of the project in a volatile solution of a new,
		hidden Visual Studio instance.

		@param[in] projPath path to visual studio project file
		@param[in,out] pProjData for receiving the project type GUID and existing configurations
	*/
	bool Vspte_GetProjData(const char* projPath, ExtractedProjData* pProjData)
	{
		if (_Vspte_GetProjData)
		{
			return _Vspte_GetProjData(projPath, pProjData);
		}
		else
		{
			return false;
		}
	}

	/** @brief  Deallocates the configurations / platforms array of an ExtractedProjData instance already used in a call to @Vspte_GetProjData

		After a call to @Vspte_GetProjData and copying the data you're interested in from the ExtractedProjData object, you should call this
		in order to deallocate the configurations / platforms array with the correct runtime
	*/
	void Vspte_DeallocateProjDataCfgArray(ExtractedProjData* pProjData)
	{
		if (_Vspte_DeallocateProjDataCfgArray)
		{
			_Vspte_DeallocateProjDataCfgArray(pProjData);
		}
	}

	/** @brief  Optionally closes the volatile solution and quits the Visual Studio instance

		After a call to @Vspte_GetProjData, the Visual Studio instance is kept up and running with the volatile solution loaded,
		in order to save time in subsequent calls to @Vspte_GetProjData. Cleanup is done anyway on application exit, so calling it
		explicitely is not necessary, it's provided more for testing purposes
	*/
	void Vspte_CleanUp()
	{
		if (_Vspte_CleanUp)
		{
			_Vspte_CleanUp();
		}
	}
};
```

- As opposed to the static integration, a little bit more API calls and caution are needed, but for your convenience, a singleton `VspteModuleWrapper`
taking care of dynamically loading the DLL, mapping the function pointers and cleaning up is given in this sample header.
- Calling `VspteModuleWrapper::Instance()->Vspte_GetProjData`, the first time will start a hidden Visual Studio instance and create
a volatile solution, this will take few seconds on the first call, but subsequent calls (for reading several more project Guids and config/platform pairs from other files)
will be very quick;
- optionally, `VspteModuleWrapper::Instance()->Vspte_CleanUp()` can be called, but at application exit this will be called anyway on destruction of
objects and garbage collection
- for actually using the interface code, include this new header in your code and write something like:
```
#include "VSProjLoaderInterface.h"

if (!VspteModuleWrapper::Instance()->IsLoaded()) {
	// load the module if not already loaded
	VspteModuleWrapper::Instance()->Load();
}

// anytime before retrieving project guids, make sure to check if loading succeeded:
if (VspteModuleWrapper::Instance()->IsLoaded()) {
	char projTypeGuid[VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH] = { 0 };
	bool bSuccess = VspteModuleWrapper::Instance()->Vspte_GetProjData(
		pathToYourExternalProject,
		projData);
	if (bSuccess) {
		// projData will contain the project type Guid and config/platform pairs found in the project
	}

	// if Vspte_GetProjData needs to be called again subsequently, projData should be deallocated:
	VspteModuleWrapper::Instance()->Vspte_DeallocateProjDataCfgArray(projData);


	// call Vspte_GetProjData again...
	...
}


...


// optionally, when really no longer needed
if (VspteModuleWrapper::Instance()->IsLoaded()) {
	VspteModuleWrapper::Instance()->Vspte_CleanUp();
}

```

© 2020 Lucian Muresan
