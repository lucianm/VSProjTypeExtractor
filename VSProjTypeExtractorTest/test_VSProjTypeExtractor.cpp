#include "test_VSProjTypeExtractor.h"

#include <string>
#include <sstream>
#include <iostream>
#include <vector>
#include <thread>
#include <stdexcept>

#include "VSProjLoaderInterface.h"

#define MYTEST_COUT std::cout << "[          ] [ INFO ] "

std::string CTestF_VSProjTypeExtractor::strTestDataPath;
std::mutex CTestF_VSProjTypeExtractor::mtxCout;

CTestF_VSProjTypeExtractor::CTestF_VSProjTypeExtractor()
{
	VspteModuleWrapper::Instance()->Load();
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
	size_t pos1 = strTestDataPath.find("\\bin\\");
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
	VspteModuleWrapper::Instance()->Vspte_CleanUp();
}


void CTestF_VSProjTypeExtractor::SetUpTestCase()
{
}

void CTestF_VSProjTypeExtractor::TearDownTestCase()
{
}

void CTestF_VSProjTypeExtractor::SingleExtractProjData(const char* projFileName, const char* testProjTypeGuid, const char* testProjTypeName)
{
	if (VspteModuleWrapper::Instance()->IsLoaded())
	{
		std::string strCurrentTestProjPath = strTestDataPath + projFileName;

		std::unique_lock<std::mutex> lock(mtxCout);
		MYTEST_COUT << "Extracting project type GUID for the test project located at '" << strCurrentTestProjPath.c_str() << "'..." << std::endl;
		lock.unlock();

		ExtractedProjData projData;
		bool bSuccess = false;
		EXPECT_NO_THROW(
			bSuccess = VspteModuleWrapper::Instance()->Vspte_GetProjData(
				strCurrentTestProjPath.c_str(),
				&projData)
		);

		lock.lock();
		EXPECT_TRUE(bSuccess) << "Calling Vspte_GetProjData has failed, maybe your VS installation does not support " << testProjTypeName << " projects !!!";
		if (bSuccess)
		{
			MYTEST_COUT << "Calling Vspte_GetProjData has SUCCEEDED" << std::endl;
		}
		lock.unlock();

		lock.lock();
		if (strncmp(projData._TypeGuid, testProjTypeGuid, VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH) != 0)
		{
			FAIL() << "Extracted project type GUID does not match expected " << testProjTypeGuid << " !!!";
		}
		else
		{
			MYTEST_COUT << "Project type GUID found for the " << testProjTypeName << " project is " << projData._TypeGuid << std::endl;
		}
		for (unsigned int i = 0; i < projData._numCfgPlatforms; i++)
		{
			MYTEST_COUT << "Found config / platform pair: '" << projData._pConfigsPlatforms[i]._config << "|" << projData._pConfigsPlatforms[i]._platform << "'" << std::endl;
		}
		lock.unlock();

		EXPECT_NO_THROW(VspteModuleWrapper::Instance()->Vspte_DeallocateProjDataCfgArray(&projData));
	}
}

TEST_F(CTestF_VSProjTypeExtractor, tc_SingleThreadFailures)
{
	if (VspteModuleWrapper::Instance()->IsLoaded())
	{
		std::string strCurrentTestProjPath = strTestDataPath + "\\NotExistingExternalProject.csproj";
		bool bSuccess = false;

		std::unique_lock<std::mutex> lock(mtxCout);
		MYTEST_COUT << "Extracting project data to nullptr object should FAIL" << std::endl;
		lock.unlock();

		EXPECT_FALSE(
			VspteModuleWrapper::Instance()->Vspte_GetProjData(
				strCurrentTestProjPath.c_str(),
				nullptr)
		);

		ExtractedProjData projData;

		lock.lock();
		MYTEST_COUT << "Extracting project data from nullptr path should FAIL" << std::endl;
		lock.unlock();
		EXPECT_FALSE(
			VspteModuleWrapper::Instance()->Vspte_GetProjData(
				nullptr,
				&projData)
		);

		lock.lock();
		MYTEST_COUT << "Extracting project data from invalid path should FAIL" << std::endl;
		lock.unlock();
		EXPECT_FALSE(
			VspteModuleWrapper::Instance()->Vspte_GetProjData(
				strCurrentTestProjPath.c_str(),
				&projData)
		);
	}
}

TEST_F(CTestF_VSProjTypeExtractor, tc_SingleThreadSingleProjectCSharp)
{
	SingleExtractProjData("\\ExternalDummyProject.csproj", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "C#");
}

TEST_F(CTestF_VSProjTypeExtractor, tc_SingleThreadMultipleProjects)
{
	SingleExtractProjData("\\ExternalDummyProject.csproj", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "C#");
	SingleExtractProjData("\\ExternalDummyProject_2.pyproj", "{888888a0-9f3d-457c-b088-3a5042f75d52}", "Python");
	SingleExtractProjData("\\ExternalDummyProject_3.wixproj", "{930c7802-8a8c-48f9-8165-68863bccd9dd}", "WiX");
}

TEST_F(CTestF_VSProjTypeExtractor, tc_MultipleThreadsMultipleProjects)
{
	MYTEST_COUT << "Starting parallel extraction of project type GUIDs..." << std::endl;

	std::thread proj_1(&SingleExtractProjData, "\\ExternalDummyProject.csproj", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "C#");
	std::thread proj_2(&SingleExtractProjData, "\\ExternalDummyProject_2.pyproj", "{888888a0-9f3d-457c-b088-3a5042f75d52}", "Python");
	std::thread proj_3(&SingleExtractProjData, "\\ExternalDummyProject_3.wixproj", "{930c7802-8a8c-48f9-8165-68863bccd9dd}", "WiX");

	proj_1.join();
	proj_2.join();
	proj_3.join();

	MYTEST_COUT << "Parallel extraction of project type GUIDs completed." << std::endl;
}