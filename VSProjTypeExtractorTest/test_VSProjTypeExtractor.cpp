#include "test_VSProjTypeExtractor.h"

#include "..\VSProjTypeExtractor\VSProjTypeExtractor.h"

#include <string>
#include <sstream>
#include <iostream>
#include <vector>
#include <stdexcept>

#include <Windows.h>

#define MYTEST_COUT std::cout << "[          ] [ INFO ] "

CTestF_VSProjTypeExtractor::CTestF_VSProjTypeExtractor()
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
	strTestDataPath = std::string(binPath.begin(), binPath.begin() + dwRes);
	size_t pos1 = strTestDataPath.find("\\VSProjTypeExtractorTest\\");
	strTestDataPath = strTestDataPath.substr(0, pos1);
	strTestDataPath += "\\VSProjTypeExtractorTest\\data";
}

CTestF_VSProjTypeExtractor::~CTestF_VSProjTypeExtractor()
{
}

void CTestF_VSProjTypeExtractor::SetUp()
{
}

void CTestF_VSProjTypeExtractor::TearDown()
{
	Vspte_CleanUp();
}


void CTestF_VSProjTypeExtractor::SetUpTestCase()
{
}

void CTestF_VSProjTypeExtractor::TearDownTestCase()
{
}

TEST_F(CTestF_VSProjTypeExtractor, tc_SingleThreadSingleProjectCSharp)
{

	strTestDataPath += "\\ExternalDummyProject.csproj";

	char testProjTypeGuid[] = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
	MYTEST_COUT << "Extracting project type GUID for the test project located at '" << strTestDataPath.c_str() << "'..." << std::endl;
	char projTypeGuid[VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH] = { 0 };
	bool bSuccess = false;
	EXPECT_NO_THROW(
		bSuccess = Vspte_GetProjTypeGuidString(
			strTestDataPath.c_str(),
			projTypeGuid,
			VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH,
			16) /* try with VS2019 */
	);
	EXPECT_TRUE(bSuccess) << "Calling Vspte_GetProjTypeGuidString has failed, maybe your VS installation does not support C# projects !!!";
	if (bSuccess)
	{
		MYTEST_COUT << "Calling Vspte_GetProjTypeGuidString has SUCCEEDED" << std::endl;
	}

	if (strncmp(projTypeGuid, testProjTypeGuid, VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH) != 0)
	{
		FAIL() << "Extracted project type GUID does not match expected " << testProjTypeGuid << " !!!";
	}
	else
	{
		MYTEST_COUT << "Project type GUID found for the C# project is " << projTypeGuid << std::endl;
	}
}
