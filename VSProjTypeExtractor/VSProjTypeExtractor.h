#pragma once

#ifdef BUILD_VSProjTypeExtractor
#define CDECL_VSPROJTYPEEXTRACTOR __declspec (dllexport)
#else
#define CDECL_VSPROJTYPEEXTRACTOR __declspec (dllimport)
#endif

#define VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH 39

extern "C" {
	bool CDECL_VSPROJTYPEEXTRACTOR GetProjTypeGuidString(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength, unsigned int VS_MajorVersion);
}