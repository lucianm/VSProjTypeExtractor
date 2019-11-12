// VSProjTypeExtractorTest.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

//#include <iostream>
#include "..\VSProjTypeExtractor\VSProjTypeExtractor.h"

#include <string>
#include <sstream>
#include <iostream>
#include <vector>
#include <stdexcept>

#include <Windows.h>

#define TEST_PROJTYPE_GUID "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"

int main()
{
	// determine executable path in order to find the test data (a project) just as it is stored with the sources
	std::vector<TCHAR> binPath(MAX_PATH);
	DWORD dwRes = ::GetModuleFileName(0, &binPath[0], DWORD(binPath.size()));
	if (dwRes == 0)
	{
		// this went terribly wrong
		throw std::runtime_error("::GetModuleFileName failed");
	}
	else
	{
		// enlarge buffer until it fits
		while (dwRes == binPath.size())
		{
			binPath.resize(binPath.size() * 2);
			dwRes = ::GetModuleFileName(0, &binPath[0], DWORD(binPath.size()));
		}
	}
	std::string strTestDataPath(binPath.begin(), binPath.begin() + dwRes);
	size_t pos1 = strTestDataPath.find("\\VSProjTypeExtractorTest\\");
	strTestDataPath = strTestDataPath.substr(0, pos1);

	strTestDataPath += "\\VSProjTypeExtractorTest\\ExternalDummyProject.csproj";

	std::cout << "Extracting project type GUID for the test project located at '" << strTestDataPath.c_str() << "'..." << std::endl;

	char projTypeGuid[VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH] = { 0 };
	bool bSuccess = GetProjTypeGuidString(
		strTestDataPath.c_str(),
		projTypeGuid,
		VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH,
		16); // try with VS2019

	if (!bSuccess)
	{
		throw std::exception("Calling GetProjTypeGuidString has failed !!!");
	}
	else
	{
		std::cout << "Calling GetProjTypeGuidString has SUCCEEDED" << std::endl;
	}

	if (strncmp(projTypeGuid, TEST_PROJTYPE_GUID, VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH) != 0)
	{
		std::ostringstream strMsg;
		strMsg << "Extracted project type GUID does not match expected " << TEST_PROJTYPE_GUID << " !!!";

		throw std::exception(strMsg.str().c_str());
	}
	else
	{
		std::cout << "Project type GUID found for the test project is " << projTypeGuid << std::endl;
	}
}
