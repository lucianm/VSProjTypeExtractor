#pragma once

// some resource version defines
#define STRINGIFY2(s) #s
#define STRINGIFY(s) STRINGIFY2(s)

#define VSPTE_VERSION_MAJOR               0
#define VSPTE_VERSION_MINOR               1
#define VSPTE_VERSION_REVISION            0
#define VSPTE_VERSION_BUILD               0

#define VSPTE_VER_FILE_DESCRIPTION_STR    "VS automation for extracting project type GUID from existing project"
#define VSPTE_VER_FILE_VERSION            VSPTE_VERSION_MAJOR, VSPTE_VERSION_MINOR, VSPTE_VERSION_REVISION, VSPTE_VERSION_BUILD
#define VSPTE_VER_FILE_VERSION_STR        STRINGIFY(VSPTE_VERSION_MAJOR)    \
                                      "." STRINGIFY(VSPTE_VERSION_MINOR)    \
                                      "." STRINGIFY(VSPTE_VERSION_REVISION) \
                                      "." STRINGIFY(VSPTE_VERSION_BUILD)    \

#define VSPTE_VER_PRODUCTNAME_STR         "VSProjTypeExtractor"
#define VSPTE_VER_PRODUCT_VERSION         VSPTE_VER_FILE_VERSION
#define VSPTE_VER_PRODUCT_VERSION_STR     VSPTE_VER_FILE_VERSION_STR
#define VSPTE_VER_ORIGINAL_FILENAME_STR   VSPTE_VER_PRODUCTNAME_STR ".dll"
#define VSPTE_VER_INTERNAL_NAME_STR       VSPTE_VER_ORIGINAL_FILENAME_STR
#define VSPTE_VER_LEGALCOPYRIGHT          "Copyright (C) 2019 Lucian Muresan"



#define VSPROJ_TYPEEXTRACT_APIVERSION         VSPTE_VERSION_MINOR
#define VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH     39

// convenience code for the case when an application does not want to link against us
#ifdef VSPROJTYPEEXTRACTOR_DYNLOAD

typedef bool  (__stdcall *Type_GetProjTypeGuidString)(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength, unsigned int VS_MajorVersion);
typedef void* (__stdcall *Type_CleanUp)(void);

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
			_Vspte_GetProjTypeGuidString = nullptr;
			_Vspte_CleanUp = nullptr;
		}
	}
	Type_GetProjTypeGuidString _Vspte_GetProjTypeGuidString = nullptr;
	Type_CleanUp _Vspte_CleanUp = nullptr;
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

		Needs to be called once before attempting to call @Vspte_GetProjTypeGuidString or @Vspte_CleanUp
	*/
	void Load()
	{
		try {
			if (!_hVSProjTypeExtractor)
			{
				_hVSProjTypeExtractor = ::LoadLibrary("VSProjTypeExtractor");
				if (_hVSProjTypeExtractor)
				{
					_Vspte_GetProjTypeGuidString = reinterpret_cast<Type_GetProjTypeGuidString>(::GetProcAddress(_hVSProjTypeExtractor, "Vspte_GetProjTypeGuidString"));
					_Vspte_CleanUp = reinterpret_cast<Type_CleanUp>(::GetProcAddress(_hVSProjTypeExtractor, "Vspte_CleanUp"));
				}
			}
		}
		catch (...)
		{
		}
	}

	/** @brief  Queries if VSProjTypeExtractor.dll was successfully loaded

		Helpful for avoiding to call @Vspte_GetProjTypeGuidString or @Vspte_CleanUp without effect
	*/
	bool IsLoaded()
	{
		return _hVSProjTypeExtractor != NULL && _Vspte_GetProjTypeGuidString != nullptr && _Vspte_CleanUp != nullptr;
	}

	/** @brief  Determines project type GUID of an existing project

		The project type GUID is determined by silently automating the loading of the project in a volatile solution of a new,
		hidden Visual Studio instance.

		@param[in] projPath path to visual studio project file
		@param[in,out] projTypeGuid character string pre-allocated to the lenght provided in projTypeGuidMaxLength for receiving the project type GUID
		@param[in] projTypeGuidMaxLength maximum length of the project type GUID, if VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH is provided, only the first GUID is retrieved
		@param[in] VS_MajorVersion major Visual Studio version to use
	*/
	bool Vspte_GetProjTypeGuidString(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength, unsigned int VS_MajorVersion)
	{
		if (_Vspte_GetProjTypeGuidString)
		{
			return _Vspte_GetProjTypeGuidString(projPath, projTypeGuid, projTypeGuidMaxLength, VS_MajorVersion);
		}
		else
		{
			return false;
		}
	}

	/** @brief  Optionally closes the volatile solution and quits the Visual Studio instance

		After a call to @Vspte_GetProjTypeGuidString, the Visual Studio instance is kept up and running with the volatile solution loaded,
		in order to save time in subsequent calls to @Vspte_GetProjTypeGuidString. Cleanup is done anyway on application exit, so calling it
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

#else

	#ifdef BUILD_VSProjTypeExtractor
	#define CDECL_VSPROJTYPEEXTRACTOR __declspec (dllexport)
	#else
	#define CDECL_VSPROJTYPEEXTRACTOR __declspec (dllimport)
	#endif

	extern "C" {
		/** @brief  Determines project type GUID of an existing project

			The project type GUID is determined by silently automating the loading of the project in a volatile solution of a new,
			hidden Visual Studio instance.

			@param[in] projPath path to visual studio project file
			@param[in,out] projTypeGuid character string pre-allocated to the lenght provided in projTypeGuidMaxLength for receiving the project type GUID
			@param[in] projTypeGuidMaxLength maximum length of the project type GUID, if VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH is provided, only the first GUID is retrieved
			@param[in] VS_MajorVersion major Visual Studio version to use
		*/
		CDECL_VSPROJTYPEEXTRACTOR bool __stdcall Vspte_GetProjTypeGuidString(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength, unsigned int VS_MajorVersion);

		/** @brief  Optionally closes the volatile solution and quits the Visual Studio instance

			After a call to @Vspte_GetProjTypeGuidString, the Visual Studio instance is kept up and running with the volatile solution loaded,
			in order to save time in subsequent calls to @Vspte_GetProjTypeGuidString. Cleanup is done anyway on application exit, so calling it
			explicitely is not necessary, it's provided more for testing purposes
		*/
		CDECL_VSPROJTYPEEXTRACTOR void __stdcall Vspte_CleanUp();
	}

#endif
