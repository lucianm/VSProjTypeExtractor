#include "VSProjTypeExtractor.h"

#include <msclr/marshal.h>
#include <string.h>



namespace VSProjTypeExtractor {
	public ref class ClassWorker
	{
	public:
		System::String^ GetProjTypeGuidStringManaged(System::String^ projPath, unsigned int vs_MajVer )
		{
			return VSProjTypeExtractorManaged::VSProjTypeWorker::ExtractProjectTypeGuid(projPath, vs_MajVer);
		}
	};
}

bool GetProjTypeGuidString(const char* projPath, char* projTypeGuid, unsigned int projTypeGuidMaxLength, unsigned int VS_MajorVersion)
{
	bool bSuccess = false;
	VSProjTypeExtractor::ClassWorker work;
	System::String^ strProjPath = gcnew System::String(projPath);
	System::String^ strProjTypeGuid = work.GetProjTypeGuidStringManaged(strProjPath, VS_MajorVersion);

	if (strProjTypeGuid->Length >= VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH - 1 && strProjTypeGuid->Length <= projTypeGuidMaxLength)
	{
		msclr::interop::marshal_context^ context = gcnew msclr::interop::marshal_context();
		const char* str = context->marshal_as<const char*>(strProjTypeGuid);
		strcpy_s(projTypeGuid, projTypeGuidMaxLength, str);
		delete context;
		bSuccess = true;
	}
	return bSuccess;
}
