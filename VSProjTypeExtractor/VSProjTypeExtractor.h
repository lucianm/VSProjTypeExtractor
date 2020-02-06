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
#define VSPTE_STRINGIFY2(s) #s
#define VSPTE_STRINGIFY(s) VSPTE_STRINGIFY2(s)

#define VSPTE_VERSION_MAJOR               0
#define VSPTE_VERSION_MINOR               4
#define VSPTE_VERSION_REVISION            0
#define VSPTE_VERSION_BUILD               0

#define VSPTE_VER_FILE_DESCRIPTION_STR    "VS automation for extracting project type GUID and configurations from existing project"
#define VSPTE_VER_FILE_VERSION            VSPTE_VERSION_MAJOR, VSPTE_VERSION_MINOR, VSPTE_VERSION_REVISION, VSPTE_VERSION_BUILD
#define VSPTE_VER_FILE_VERSION_STR        VSPTE_STRINGIFY(VSPTE_VERSION_MAJOR)    \
                                      "." VSPTE_STRINGIFY(VSPTE_VERSION_MINOR)    \
                                      "." VSPTE_STRINGIFY(VSPTE_VERSION_REVISION) \
                                      "." VSPTE_STRINGIFY(VSPTE_VERSION_BUILD)    \

#define VSPTE_VER_PRODUCTNAME_STR         "VSProjTypeExtractor"
#define VSPTE_VER_PRODUCT_VERSION         VSPTE_VER_FILE_VERSION
#define VSPTE_VER_PRODUCT_VERSION_STR     VSPTE_VER_FILE_VERSION_STR
#define VSPTE_VER_ORIGINAL_FILENAME_STR   VSPTE_VER_PRODUCTNAME_STR ".dll"
#define VSPTE_VER_INTERNAL_NAME_STR       VSPTE_VER_ORIGINAL_FILENAME_STR
#define VSPTE_VER_LEGALCOPYRIGHT          "Copyright (C) 2020 Lucian Muresan"



#define VSPROJ_TYPEEXTRACT_APIVERSION         VSPTE_VERSION_MINOR
#define VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH     39
#define VSPROJ_MAXSTRING_LENGTH              120


#ifdef BUILD_VSProjTypeExtractor
#define CDECL_VSPROJTYPEEXTRACTOR __declspec (dllexport)
#else
#define CDECL_VSPROJTYPEEXTRACTOR __declspec (dllimport)
#endif

extern "C" {

    /** configuration / platform pair
    */
    typedef struct
    {
        char _config[VSPROJ_MAXSTRING_LENGTH];
        char _platform[VSPROJ_MAXSTRING_LENGTH];
    } ExtractedCfgPlatform;

    /** extracted project data containing type GUID and array of found configuration / platform pairs
    */
    typedef struct
    {
        char _TypeGuid[VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH];
        ExtractedCfgPlatform* _pConfigsPlatforms;
        unsigned int _numCfgPlatforms;
    } ExtractedProjData;

    /** @brief  Retrieves basic project data from an existing project

        The project data is extracted by silently automating the loading of the project in a volatile solution of a new,
        hidden Visual Studio instance.

        @param[in] projPath path to visual studio project file
        @param[in,out] pProjData for receiving the project type GUID and existing configurations
    */
    CDECL_VSPROJTYPEEXTRACTOR bool __stdcall Vspte_GetProjData(const char* projPath, ExtractedProjData* projData);

    /** @brief  Deallocates the configurations / platforms array of an ExtractedProjData instance already used in a call to @Vspte_GetProjData

        After a call to @Vspte_GetProjData and copying the data you're interested in from the ExtractedProjData object, you should call this
        in order to deallocate the configurations / platforms array with the correct runtime
    */
    CDECL_VSPROJTYPEEXTRACTOR void __stdcall Vspte_DeallocateProjDataCfgArray(ExtractedProjData* projData);

    /** @brief  Optionally closes the volatile solution and quits the Visual Studio instance

        After a call to @Vspte_GetProjData, the Visual Studio instance is kept up and running with the volatile solution loaded,
        in order to save time in subsequent calls to @Vspte_GetProjData. Cleanup is done anyway on application exit, so calling it
        explicitely is not necessary, it's provided more for testing purposes
    */
    CDECL_VSPROJTYPEEXTRACTOR void __stdcall Vspte_CleanUp();
}
