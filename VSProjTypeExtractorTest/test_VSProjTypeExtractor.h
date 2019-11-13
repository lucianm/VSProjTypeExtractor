#pragma once
#include <gtest/gtest.h>

class CTestF_VSProjTypeExtractor : public ::testing::Test
{
protected:

	CTestF_VSProjTypeExtractor();
	virtual ~CTestF_VSProjTypeExtractor();

	virtual void SetUp(void);
	virtual void TearDown(void);
	static void SetUpTestCase(void);
	static void TearDownTestCase(void);

	std::string strTestDataPath;
};
