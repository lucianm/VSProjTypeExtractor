#pragma once
#include <gtest/gtest.h>

#include <mutex>

class CTestF_VSProjTypeExtractor : public ::testing::Test
{
protected:

	CTestF_VSProjTypeExtractor();
	virtual ~CTestF_VSProjTypeExtractor();

	virtual void SetUp(void);
	virtual void TearDown(void);
	static void SetUpTestCase(void);
	static void TearDownTestCase(void);

	static void SingleExtractProjGuid(const char* projFileName, const char* testProjTypeGuid, const char* testProjTypeName);

	static std::string strTestDataPath;
	static std::mutex mtxCout;
};
