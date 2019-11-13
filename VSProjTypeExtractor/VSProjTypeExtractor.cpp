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
		System::String^ GetProjTypeGuidStringManaged(System::String^ projPath, unsigned int vs_MajVer )
		{
			msclr::lock lock(this);
			return m_managedWorker.ExtractProjectTypeGuid(projPath, vs_MajVer);
		}
	};
}


bool GetProjTypeGuidString(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength, unsigned int VS_MajorVersion)
{
	bool bSuccess = false;
	System::String^ strProjPath = gcnew System::String(projPath);
	System::String^ strProjTypeGuid = VSProjTypeExtractor::ClassWorker::Instance->GetProjTypeGuidStringManaged(strProjPath, VS_MajorVersion);

	if (strProjTypeGuid->Length >= VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH - 1 && unsigned int (strProjTypeGuid->Length) <= projTypeGuidMaxLength)
	{
		msclr::interop::marshal_context^ context = gcnew msclr::interop::marshal_context();
		const char* str = context->marshal_as<const char*>(strProjTypeGuid);
		strcpy_s(projTypeGuid, projTypeGuidMaxLength, str);
		delete context;
		bSuccess = true;
	}
	return bSuccess;
}
