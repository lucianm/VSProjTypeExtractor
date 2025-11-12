/*
    VSProjTypeExtractor - Visual Studio project type GUID extractor
    VSProjTypeExtractor.cpp - Implementation File
    Copyright (c) 2020, Lucian Muresan.

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
#include <fstream>



namespace VSProjTypeExtractor {
    public ref class ClassWorker
    {
    private:
        ClassWorker(){}
        ClassWorker(const ClassWorker%) { throw gcnew System::InvalidOperationException("ClassWorker cannot be copy-constructed"); }
        static ClassWorker m_instance;
        VSProjTypeExtractorManaged::VSProjTypeWorker m_managedWorker;
        System::Object m_LockableObject;
    public:
        static property ClassWorker^ Instance { ClassWorker^ get() { return % m_instance; } }
        boolean GetProjDataManaged(System::String^ projPath, VSProjTypeExtractorManaged::ExtractedProjData^ projData)
        {
            msclr::lock lock(%m_LockableObject);
            return m_managedWorker.ExtractProjectData(projPath, projData);
        }
        void CleanUp()
        {
            msclr::lock lock(%m_LockableObject);
            m_managedWorker.CleanUp();
        }
    };

    public ref class ConsoleLogger
    {
    private:
        ConsoleLogger() :
            m_managedConLog(VSProjTypeExtractorManaged::ConAndLog::Instance)
        {
            m_LockableObject = gcnew System::Object();
        }
        ConsoleLogger(const ConsoleLogger%) { throw gcnew System::InvalidOperationException("ConsoleLogger cannot be copy-constructed"); }
        static ConsoleLogger m_instance;
        VSProjTypeExtractorManaged::ConAndLog^ m_managedConLog;
        System::Object^ m_LockableObject;
    public:
        static property ConsoleLogger^ Instance { ConsoleLogger^ get() { return % m_instance; } }

        void WriteLineDebug(System::String^ format, ... array<Object^>^ args)
        {
            // No lock needed — backend is thread-safe
            m_managedConLog->WriteLineDebug(format, args);
        }

        void WriteLineInfo(System::String^ format, ... array<Object^>^ args)
        {
            // No lock needed — backend is thread-safe
            m_managedConLog->WriteLineInfo(format, args);
        }

        void WriteLineWarn(System::String^ format, ... array<Object^>^ args)
        {
            // No lock needed — backend is thread-safe
            m_managedConLog->WriteLineWarn(format, args);
        }

        void WriteLineError(System::String^ format, ... array<Object^>^ args)
        {
            // No lock needed — backend is thread-safe
            m_managedConLog->WriteLineError(format, args);
        }

        void WriteLineFatal(System::String^ format, ... array<Object^>^ args)
        {
            // No lock needed — backend is thread-safe
            m_managedConLog->WriteLineFatal(format, args);
        }

        void WriteLineException(System::Exception^ ex, System::String^ format, ... array<System::Object^>^ args)
        {
            // No lock needed — backend is thread-safe
            m_managedConLog->WriteLineException(ex, format, args);
        }

        void WriteLineRethrow(System::Exception^ ex, System::String^ format, ... array<System::Object^>^ args)
        {
            // No lock needed — backend is thread-safe
            m_managedConLog->WriteLineRethrow(ex, format, args);
        }
    };
}


bool Vspte_GetProjData(const char* projPath, ExtractedProjData* projData)
{
    bool bSuccess = false;
    if (!projData)
    {
        VSProjTypeExtractor::ConsoleLogger::Instance->WriteLineError("Invalid argument, projData is a null pointer!!!");
        return false;
    }
    if (!projPath)
    {
        VSProjTypeExtractor::ConsoleLogger::Instance->WriteLineError("Invalid argument, projPath is a null pointer!!!");
        return false;
    }
    else
    {
        std::ifstream test_if_exists(projPath);
        if (!test_if_exists)
        {
            VSProjTypeExtractor::ConsoleLogger::Instance->WriteLineError("Invalid argument, path '{0}' does not exist!!!", gcnew System::String(projPath));
            return false;
        }
    }
    
    // clean out data
    memset(projData, 0, sizeof(ExtractedProjData));
    System::String^ strProjPath = gcnew System::String(projPath);

    try
    {
        VSProjTypeExtractorManaged::ExtractedProjData^ ProjData = gcnew VSProjTypeExtractorManaged::ExtractedProjData();
        bSuccess = VSProjTypeExtractor::ClassWorker::Instance->GetProjDataManaged(strProjPath, ProjData);

        if (bSuccess &&
            ProjData->_TypeGuid->Length >= VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH - 1 &&
            static_cast<unsigned int>(ProjData->_TypeGuid->Length) <= VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH)
        {
            // Single marshal_context for all conversions
            msclr::interop::marshal_context context;

            const char* typeGuidCStr = context.marshal_as<const char*>(ProjData->_TypeGuid);
            strncpy_s(projData->_TypeGuid, VSPROJ_TYPEEXTRACT_MAXGUID_LENGTH, typeGuidCStr, _TRUNCATE);

            // Config/Platform entries
            if (ProjData->_ConfigsPlatforms && ProjData->_ConfigsPlatforms->Length > 0)
            {
                projData->_numCfgPlatforms = ProjData->_ConfigsPlatforms->Length;
                projData->_pConfigsPlatforms = new ExtractedCfgPlatform[projData->_numCfgPlatforms];

                for (unsigned int i = 0; i < projData->_numCfgPlatforms; i++)
                {
                    memset(&projData->_pConfigsPlatforms[i], 0, sizeof(ExtractedCfgPlatform));

                    const char* cfgCStr = context.marshal_as<const char*>(ProjData->_ConfigsPlatforms[i]->_config);
                    strncpy_s(projData->_pConfigsPlatforms[i]._config, VSPROJ_MAXSTRING_LENGTH, cfgCStr, _TRUNCATE);

                    const char* platformCStr = context.marshal_as<const char*>(ProjData->_ConfigsPlatforms[i]->_platform);
                    strncpy_s(projData->_pConfigsPlatforms[i]._platform, VSPROJ_MAXSTRING_LENGTH, platformCStr, _TRUNCATE);
                }
            }
        }

        return bSuccess;
    }
    catch (System::Exception^ e)
    {
        VSProjTypeExtractor::ConsoleLogger::Instance->WriteLineException(e, "occurred for project file '{0}'", strProjPath);
        return false;
    }
}

void Vspte_DeallocateProjDataCfgArray(ExtractedProjData* projData)
{
    if (!projData)
    {
        VSProjTypeExtractor::ConsoleLogger::Instance->WriteLineFatal("Invalid argument, projData is a null pointer!!!");
        return;
    }

    delete[] projData->_pConfigsPlatforms;
    projData->_numCfgPlatforms = 0;
}

void Vspte_CleanUp()
{
    VSProjTypeExtractor::ClassWorker::Instance->CleanUp();
}
