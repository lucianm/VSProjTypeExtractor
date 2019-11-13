#pragma once

#ifdef BUILD_VSProjTypeExtractor
#define CDECL_VSPROJTYPEEXTRACTOR __declspec (dllexport)
#else
#define CDECL_VSPROJTYPEEXTRACTOR __declspec (dllimport)
#endif

#define VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH 39

extern "C" {
	/** @brief  Determines project type GUID of an existing project

		The project type GUID is determined by silently automating the loading of the project in a volatile solution of a new,
		hidden Visual Studio instance.

		@param[in] projPath path to visual studio project file
		@param[in,out] projTypeGuid character string pre-allocated to the lenght provided in projTypeGuidMaxLength for receiving the project type GUID
		@param[in] projTypeGuidMaxLength maximum length of the project type GUID, if VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH is provided, only the first GUID is retrieved
		@param[in] VS_MajorVersion major Visual Studio version to use
	*/
	bool CDECL_VSPROJTYPEEXTRACTOR Vspte_GetProjTypeGuidString(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength, unsigned int VS_MajorVersion);

	/** @brief  Optionally closes the volatile solution and quits the Visual Studio instance

		After a call to @Vspte_GetProjTypeGuidString, the Visual Studio instance is kept up and running with the volatile solution loaded,
		in order to save time in subsequent calls to @Vspte_GetProjTypeGuidString. Cleanup is done anyway on application exit, so calling it
		explicitely is not necessary, it's provided more for testing purposes
	*/
	void CDECL_VSPROJTYPEEXTRACTOR Vspte_CleanUp();
}