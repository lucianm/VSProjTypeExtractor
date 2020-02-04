/*
    VSProjTypeExtractor - Visual Studio project type GUID extractor
    VSProjTypeExtractor.h - Header File
    Copyright (c) 2020, Lucian Muresan.

    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.

    You can contact the author at :
    - VSProjTypeExtractor homepage and source repository : https://github.com/lucianm/VSProjTypeExtractor
*/

#pragma once

// some resource version defines
#define STRINGIFY2(s) #s
#define STRINGIFY(s) STRINGIFY2(s)

#define VSPTE_VERSION_MAJOR               0
#define VSPTE_VERSION_MINOR               3
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
#define VSPTE_VER_LEGALCOPYRIGHT          "Copyright (C) 2020 Lucian Muresan"



#define VSPROJ_TYPEEXTRACT_APIVERSION         VSPTE_VERSION_MINOR
#define VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH     39

// convenience code for the case when an application does not want to link against us
#ifdef VSPROJTYPEEXTRACTOR_DYNLOAD

typedef bool  (__stdcall *Type_GetProjTypeGuidString)(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength);
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
	*/
	bool Vspte_GetProjTypeGuidString(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength)
	{
		if (_Vspte_GetProjTypeGuidString)
		{
			return _Vspte_GetProjTypeGuidString(projPath, projTypeGuid, projTypeGuidMaxLength);
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
		*/
		CDECL_VSPROJTYPEEXTRACTOR bool __stdcall Vspte_GetProjTypeGuidString(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength);

		/** @brief  Optionally closes the volatile solution and quits the Visual Studio instance

			After a call to @Vspte_GetProjTypeGuidString, the Visual Studio instance is kept up and running with the volatile solution loaded,
			in order to save time in subsequent calls to @Vspte_GetProjTypeGuidString. Cleanup is done anyway on application exit, so calling it
			explicitely is not necessary, it's provided more for testing purposes
		*/
		CDECL_VSPROJTYPEEXTRACTOR void __stdcall Vspte_CleanUp();
	}

#endif
