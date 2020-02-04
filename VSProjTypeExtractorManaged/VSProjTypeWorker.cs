/*
    VSProjTypeExtractor - Visual Studio project type GUID extractor
    VSProjTypeWorker.cs - Managed code implementation
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

using System;
using EnvDTE;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;

namespace VSProjTypeExtractorManaged
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class ConfigPlatform
    {
        public String _config;
        public String _platform;

        public ConfigPlatform(String config, String platform)
        {
            this._config = config;
            this._platform = platform;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class ExtractedProjData
    {
        public String _TypeGuid;
        //public String _Guid;
        public ConfigPlatform[] _ConfigsPlatforms;

        public void AddConfigPlatform(String config, String platform)
        {
            int newArraySize = 1;
            if (this._ConfigsPlatforms != null)
            {
                newArraySize += this._ConfigsPlatforms.Length;
            }
            Array.Resize(ref this._ConfigsPlatforms, newArraySize);
            this._ConfigsPlatforms[newArraySize - 1] = new ConfigPlatform(config, platform);
        }
    }

    public class VSProjTypeWorker
    {
        private DTE _dte;
        private bool _bDteInstanciated = false;
        private int _VS_MajorVersion = 16;
        private double _projRetryAfterSeconds = 5;
        private int _projRetriesCount = 3;
        private int _solutionSleepAfterCreate = 5;
        private ConAndLog conlog = ConAndLog.Instance;
        private string _assemblyName;
        private string _assemblyFolder;
        private string _timeStampPrefix = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        private bool _saveVolatileSln = false;
        ConAndLog.OutMode _outModeLogging = ConAndLog.OutMode.OutNone;
        private string _strLogPath;

        public VSProjTypeWorker()
        {
            // read config file and initialize console logger
            Assembly thisAssembly = typeof(VSProjTypeWorker).Assembly;
            _assemblyFolder = Path.GetDirectoryName(thisAssembly.Location);
            _assemblyName = Path.GetFileNameWithoutExtension(thisAssembly.Location);
            try
            {
                SimpleXmlCfgReader cfgFile = new SimpleXmlCfgReader(_assemblyFolder + "\\" + _assemblyName + ".xml");

                // configure logger
                bool bLogConsole = Convert.ToBoolean(cfgFile.GetTextValueAtNode("config/logging/enable_console", Convert.ToString(false)));
                bool bLogFile = Convert.ToBoolean(cfgFile.GetTextValueAtNode("config/logging/enable_logfile", Convert.ToString(false)));
                _strLogPath = cfgFile.GetTextValueAtNode("config/logging/logfile_path", Path.GetTempPath());
                if (bLogConsole)
                {
                    _outModeLogging |= ConAndLog.OutMode.OutConsole;
                }
                if (bLogFile)
                {
                    _outModeLogging |= ConAndLog.OutMode.OutLogfile;
                }
                conlog.InitLogging(_outModeLogging, _strLogPath + "\\" + _timeStampPrefix + "_" + _assemblyName + ".log");

                // the rest of settings
                _VS_MajorVersion = Convert.ToInt32(cfgFile.GetTextValueAtNode("config/visual_studio/major_version", Convert.ToString(_VS_MajorVersion)));
                _saveVolatileSln = Convert.ToBoolean(cfgFile.GetTextValueAtNode("config/visual_studio/save_volatile_solution", Convert.ToString(_saveVolatileSln)));
                _solutionSleepAfterCreate = Convert.ToInt32(cfgFile.GetTextValueAtNode("config/visual_studio/workaround_busy_app/sleep_seconds_after_create_solution", Convert.ToString(_solutionSleepAfterCreate)));
                _projRetriesCount = Convert.ToInt32(cfgFile.GetTextValueAtNode("config/visual_studio/workaround_busy_app/num_retries_project", Convert.ToString(_projRetriesCount)));
                _projRetryAfterSeconds = Convert.ToDouble(cfgFile.GetTextValueAtNode("config/visual_studio/workaround_busy_app/sleep_seconds_before_retry_project", Convert.ToString(_projRetryAfterSeconds)));
            }
            catch (Exception e)
            {
                conlog.WriteLine(e.Message);
            }
        }

        ~VSProjTypeWorker()
        {
            CleanUp();
        }

        public void CleanUp()
        {
            if (_bDteInstanciated)
            {
                _dte.Solution.Close(_saveVolatileSln);
                conlog.WriteLine("Closed " + _timeStampPrefix + "_" + _assemblyName + ".sln");
                _dte.Quit();
                conlog.WriteLine("Closed Visual Studio instance");
                _bDteInstanciated = false;
                // and turn off the IOleMessageFilter
                // MessageFilter.Revoke();
                conlog.CloseLogging();
            }
        }

        public ExtractedProjData ExtractProjectData(string projPath)
        {
            ExtractedProjData projData = new ExtractedProjData();
            try
            {
                if (!conlog.IsInitialized())
                {
                    // initialize logger again if necessary
                    conlog.InitLogging(_outModeLogging, _strLogPath + "\\" + _timeStampPrefix + "_" + _assemblyName + ".log");
                }
                // load project by silently automating the Visual Studio installation specified by the major version
                if (!_bDteInstanciated)
                {
                    string VisualStudioDTEVerString = String.Format("VisualStudio.DTE.{0}.0", _VS_MajorVersion);
                    Type visualStudioType = Type.GetTypeFromProgID(VisualStudioDTEVerString);
                    _dte = Activator.CreateInstance(visualStudioType) as DTE;

                    // Register the IOleMessageFilter to handle any threading errors
                    // MessageFilter.Register();

                    _dte.MainWindow.Visible = false;
                    _dte.SuppressUI = true;
                    _dte.UserControl = false;
                    conlog.WriteLine("Instanciated " + VisualStudioDTEVerString);

                    _dte.Solution.Create(Path.GetTempPath(), _timeStampPrefix + "_" + _assemblyName + ".sln");
                    _bDteInstanciated = true;

                    // wait 5 more seconds TODO: check again  at some point, if this can be done more elegant by registering a message filter to
                    // handle busy call errors https://docs.microsoft.com/en-us/previous-versions/ms228772(v=vs.140)?redirectedfrom=MSDN
                    System.Threading.Thread.Sleep(1000 * _solutionSleepAfterCreate);
                    conlog.WriteLine(_assemblyName + ".sln created");
                }

                // add project to retrieve its type Guid
                if (_bDteInstanciated)
                {
                    // retry loading the project into solution several times, because sometimes at the first attempt we might get
                    // System.Runtime.InteropServices.COMException with RPC_E_SERVERCALL_RETRYLATER
                    Func<string, Project> AddProjectGetTypeGuid = delegate (string path)
                    {
                        Project projLoaded = _dte.Solution.AddFromFile(path);
                        conlog.WriteLine("Project '" + path + "' loaded into " + _timeStampPrefix + "_" + _assemblyName + ".sln");
                        return projLoaded;
                        //return projLoaded.Kind.ToString();
                    };
                    Project myLoadedProject = RetryCall.Do<string>(AddProjectGetTypeGuid, projPath, TimeSpan.FromSeconds(_projRetryAfterSeconds), _projRetriesCount);
                    projData._TypeGuid = myLoadedProject.Kind.ToString();
                    EnvDTE.Configurations projectConfigurations;
                    foreach (object projectConfigurationName in (object[])myLoadedProject.ConfigurationManager.ConfigurationRowNames)
                    {
                        projectConfigurations = myLoadedProject.ConfigurationManager.ConfigurationRow(projectConfigurationName.ToString());
                        foreach (EnvDTE.Configuration projectConfiguration in projectConfigurations)
                        {
                            projData.AddConfigPlatform(projectConfiguration.ConfigurationName, projectConfiguration.PlatformName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                conlog.WriteLine("\n{0}\noccured for project file '{1}' loaded in Visual Studio {2}", ex.ToString(), projPath, _VS_MajorVersion);
            }
            return projData;
        }
    }
}
