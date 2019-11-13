// VSProjTypeExtractorTest.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include "gtest/gtest.h"

#ifdef _DEBUG
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#define new DEBUG_NEW
#endif


int main(int argc, char** argv)
{
	::testing::InitGoogleTest(&argc, argv);
	return RUN_ALL_TESTS();
}
