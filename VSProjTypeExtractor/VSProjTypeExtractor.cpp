/*
    VSProjTypeExtractor - Visual Studio project type GUID extractor
    VSProjTypeExtractor.cpp - Implementation File
    Copyright (c) 2019, Lucian Muresan.

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

#include "VSProjTypeExtractor.h"

#include <msclr/marshal.h>
#include <msclr/lock.h>
#include <string.h>



namespace VSProjTypeExtractor {
	public ref class ClassWorker
	{
	private:
		ClassWorker(){}
		ClassWorker(const ClassWorker%) { throw gcnew System::InvalidOperationException("ClassWorker cannot be copy-constructed"); }
		static ClassWorker m_instance;
		VSProjTypeExtractorManaged::VSProjTypeWorker m_managedWorker;
	public:
		static property ClassWorker^ Instance { ClassWorker^ get() { return % m_instance; } }
		System::String^ GetProjTypeGuidStringManaged(System::String^ projPath)
		{
			msclr::lock lock(this);
			return m_managedWorker.ExtractProjectTypeGuid(projPath);
		}
		void CleanUp()
		{
			msclr::lock lock(this);
			m_managedWorker.CleanUp();
		}
	};
}


bool Vspte_GetProjTypeGuidString(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength)
{
	bool bSuccess = false;
	System::String^ strProjPath = gcnew System::String(projPath);
	try {
		System::String^ strProjTypeGuid = VSProjTypeExtractor::ClassWorker::Instance->GetProjTypeGuidStringManaged(strProjPath);

		if (strProjTypeGuid->Length >= VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH - 1 && unsigned int(strProjTypeGuid->Length) <= projTypeGuidMaxLength)
		{
			msclr::interop::marshal_context^ context = gcnew msclr::interop::marshal_context();
			const char* str = context->marshal_as<const char*>(strProjTypeGuid);
			strcpy_s(projTypeGuid, projTypeGuidMaxLength, str);
			delete context;
			bSuccess = true;
		}
	}
	catch (System::Exception^ e)
	{
		System::Console::WriteLine("\n{0}\noccured for project file '{1}' loaded in Visual Studio {2}", e->ToString(), strProjPath);
		if (e->InnerException)
		{
			System::Console::WriteLine("\nInner exception: {0}\n", e->InnerException->ToString());
		}
	}
	return bSuccess;
}

void Vspte_CleanUp()
{
	VSProjTypeExtractor::ClassWorker::Instance->CleanUp();
}
