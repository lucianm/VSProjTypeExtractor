/*
    VSProjTypeExtractor - Visual Studio project type GUID extractor
    VSProjTypeWorker.cs - Managed code implementation
    Copyright (c) 2019 - 2025, Lucian Muresan.

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

using EnvDTE;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace VSProjTypeExtractorManaged
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class ConfigPlatform
    {
        public string _config;
        public string _platform;

        public ConfigPlatform(string config, string platform)
        {
            _config = config;
            _platform = platform;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class ExtractedProjData
    {
        public string _TypeGuid;
        public ConfigPlatform[] _ConfigsPlatforms;

        public void AddConfigPlatform(string config, string platform)
        {
            int newArraySize = (_ConfigsPlatforms?.Length ?? 0) + 1;
            Array.Resize(ref _ConfigsPlatforms, newArraySize);
            _ConfigsPlatforms[newArraySize - 1] = new ConfigPlatform(config, platform);
        }
    }

    internal sealed class RetryableProjectLoadException : Exception
    {
        public RetryableProjectLoadException(string message) : base(message) { }
    }

    public class VSProjTypeWorker
    {
        private DTE _dte;
        private bool _bDteInstanciated = false;
        private int _VS_MajorVersion = 16;
        private double _projInitialRetryAfterSeconds = 0.3;
        private int _projRetriesCount = 3;
        private double _solutionSleepAfterCreate = 5.0;
        private ConAndLog conlog = ConAndLog.Instance;
        private string _assemblyName;
        private string _assemblyFolder;
        private string _timeStampPrefix = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        private bool _saveVolatileSln = false;
        private bool _showVisualStudio = false;
        private ConAndLog.OutMode _outModeLogging = ConAndLog.OutMode.OutNone;
        private string _strLogPath;

        public VSProjTypeWorker()
        {
            // Read configuration XML and initialize logger
            Assembly thisAssembly = typeof(VSProjTypeWorker).Assembly;
            _assemblyFolder = Path.GetDirectoryName(thisAssembly.Location);
            _assemblyName = Path.GetFileNameWithoutExtension(thisAssembly.Location);

            try
            {
                var cfgFile = new SimpleXmlCfgReader(Path.Combine(_assemblyFolder, $"{_assemblyName}.xml"));

                bool bLogConsole = true;
                bool bLogFile = Convert.ToBoolean(cfgFile.GetTextValueAtNode("config/logging/enable_logfile", "false"));
                _strLogPath = cfgFile.GetTextValueAtNode("config/logging/logfile_path", Path.GetTempPath());

                if (bLogConsole) _outModeLogging |= ConAndLog.OutMode.OutConsole;
                if (bLogFile) _outModeLogging |= ConAndLog.OutMode.OutLogfile;

                conlog.SetLogLevel(cfgFile.GetTextValueAtNode("config/logging/level", Convert.ToString(ConAndLog.LogLevel.DEBUG)));
                conlog.InitLogging(_outModeLogging, Path.Combine(_strLogPath, $"{_timeStampPrefix}_{_assemblyName}.log"));

                _VS_MajorVersion = Convert.ToInt32(cfgFile.GetTextValueAtNode("config/visual_studio/major_version", _VS_MajorVersion.ToString()));
                conlog.WriteLineInfo("VS major version as read from config file      : " + _VS_MajorVersion);
                int verEnv = 0;
                int.TryParse(Environment.GetEnvironmentVariable("PROJTYPEXTRACT_VSVERSION"), out verEnv);
                if (verEnv > 0)
                {
                    _VS_MajorVersion = verEnv;
                    conlog.WriteLineInfo("VS major version from PROJTYPEXTRACT_VSVERSION : " + _VS_MajorVersion);
                }

                _saveVolatileSln = Convert.ToBoolean(cfgFile.GetTextValueAtNode("config/visual_studio/save_volatile_solution", _saveVolatileSln.ToString()));
                _showVisualStudio = Convert.ToBoolean(cfgFile.GetTextValueAtNode("config/visual_studio/show_UI", _showVisualStudio.ToString()));
                _solutionSleepAfterCreate = double.Parse(cfgFile.GetTextValueAtNode("config/visual_studio/workaround_busy_app/sleep_seconds_after_create_solution", _solutionSleepAfterCreate.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);

                _projInitialRetryAfterSeconds = double.Parse(cfgFile.GetTextValueAtNode("config/visual_studio/workaround_busy_app/retry_project/initial_sleep_seconds", _projInitialRetryAfterSeconds.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
                double approxTotalSec = double.Parse(cfgFile.GetTextValueAtNode("config/visual_studio/workaround_busy_app/retry_project/estimated_total_seconds", "10"), CultureInfo.InvariantCulture);
                int minAttempts = Convert.ToInt32(cfgFile.GetTextValueAtNode("config/visual_studio/workaround_busy_app/retry_project/min_attempts", "2"));
                _projRetriesCount = ComputeMaxAttempts(_projInitialRetryAfterSeconds, approxTotalSec, minAttempts);
            }
            catch (Exception e)
            {
                conlog.WriteLineRethrow(e, "VSProjTypeWorker initialization failure!");
            }
        }

        ~VSProjTypeWorker()
        {
            CleanUp();
        }

        public void CleanUp()
        {
            if (!_bDteInstanciated) return;

            try
            {
                _dte.Solution.Close(_saveVolatileSln);
                conlog.WriteLineDebug($"Closed {_timeStampPrefix}_{_assemblyName}.sln");
                _dte.Quit();
                _bDteInstanciated = false;
                MessageFilter.Revoke();
                conlog.CloseLogging();
            }
            catch { }
        }

        private int ComputeMaxAttempts(double baseIntervalSec, double approxTotalDurationSec, int minAttempts = 2)
        {
            if (baseIntervalSec <= 0) throw new ArgumentException("baseIntervalSec must be positive");
            if (approxTotalDurationSec <= 0) throw new ArgumentException("approxTotalDurationSec must be positive");

            int attempts = (int)Math.Ceiling(Math.Log((approxTotalDurationSec / baseIntervalSec) + 1, 2) + 1);
            return Math.Max(attempts, minAttempts);
        }

        /// <summary>
        /// WiX-friendly validation: require Kind be present and not all-zero GUID.
        /// Other fields (Name, Collection) are optional and logged but don't block extraction.
        /// </summary>
        private bool ValidateProjectObject(Project proj, string path)
        {
            if (proj == null) return false;

            try
            {
                string kind = null;
                try { kind = proj.Kind; }
                catch (Exception ex)
                {
                    conlog.WriteLineDebug($"Project '{path}' Kind access threw: {ex.Message}");
                    return false;
                }

                if (string.IsNullOrEmpty(kind) || kind == "{00000000-0000-0000-0000-000000000000}")
                {
                    conlog.WriteLineDebug($"Project '{path}' has invalid Kind='{kind ?? "null"}' — waiting for stabilization.");
                    return false;
                }

                // Non-blocking checks (informational)
                try
                {
                    if (proj.Collection == null)
                        conlog.WriteLineDebug($"Project '{path}' Collection is null (non-blocking).");
                }
                catch (Exception ex) { conlog.WriteLineDebug($"Project '{path}' Collection access threw (non-blocking): {ex.Message}"); }

                try
                {
                    if (string.IsNullOrWhiteSpace(proj.Name))
                        conlog.WriteLineDebug($"Project '{path}' Name empty or whitespace (non-blocking).");
                }
                catch (Exception ex) { conlog.WriteLineDebug($"Project '{path}' Name access threw (non-blocking): {ex.Message}"); }

                // Kind present -> usable (even if other properties unavailable)
                return true;
            }
            catch (COMException ex)
            {
                conlog.WriteLineDebug($"Project '{path}' validation failed due to COMException (0x{ex.ErrorCode:X8}): {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                conlog.WriteLineDebug($"Project '{path}' validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load the project on an STA thread, extract managed ExtractedProjData inside STA,
        /// and return that managed object to the caller. The returned object is never null.
        /// </summary>
        private ExtractedProjData LoadProjectAndExtractData(string projPath, int maxRetries, double initialRetrySeconds)
        {
            // Always prepare a non-null result object that will be returned to native caller
            ExtractedProjData result = new ExtractedProjData { _TypeGuid = "UNKNOWN", _ConfigsPlatforms = null };

            var thread = new System.Threading.Thread(() =>
            {
                MessageFilter.Register();
                try
                {
                    int attempt = 0;
                    double waitSeconds = initialRetryAfterClamp(initialRetrySeconds);

                    while (attempt < maxRetries)
                    {
                        attempt++;
                        try
                        {
                            Project proj = null;
                            try
                            {
                                proj = _dte.Solution.AddFromFile(projPath);
                            }
                            catch (COMException ex)
                            {
                                uint hr = (uint)ex.ErrorCode;
                                if (hr == 0x80004005)
                                {
                                    // E_FAIL: sometimes AddFromFile throws but a last project stub may be present in the solution.
                                    conlog.WriteLineWarn($"COMException E_FAIL loading '{projPath}' — will attempt to extract minimal data from solution if possible.");
                                    try
                                    {
                                        // attempt best-effort to pick the last added project in the solution (if any)
                                        if (_dte.Solution?.Projects != null && _dte.Solution.Projects.Count > 0)
                                        {
                                            proj = _dte.Solution.Projects.Item(_dte.Solution.Projects.Count);
                                        }
                                    }
                                    catch (Exception innerEx)
                                    {
                                        conlog.WriteLineDebug($"Fallback project lookup after E_FAIL failed: {innerEx.Message}");
                                        proj = null;
                                    }
                                }
                                else
                                {
                                    // rethrow other COMExceptions to outer handler
                                    throw;
                                }
                            }

                            if (proj == null)
                            {
                                throw new RetryableProjectLoadException($"Project object null after AddFromFile fallback for '{projPath}'.");
                            }

                            // Validate minimal usability (Kind must be present)
                            if (!ValidateProjectObject(proj, projPath))
                            {
                                throw new RetryableProjectLoadException($"Project '{projPath}' not yet usable (Kind missing).");
                            }

                            // Extract Type GUID safely (inside STA thread)
                            try
                            {
                                string kind = null;
                                try { kind = proj.Kind; } catch { kind = null; }
                                result._TypeGuid = !string.IsNullOrEmpty(kind) ? kind : "UNKNOWN";
                            }
                            catch { result._TypeGuid = "UNKNOWN"; }

                            // Try to extract configurations (best-effort)
                            try
                            {
                                var configMgr = proj.ConfigurationManager;
                                if (configMgr?.ConfigurationRowNames != null)
                                {
                                    foreach (object rowName in (object[])configMgr.ConfigurationRowNames)
                                    {
                                        try
                                        {
                                            var rows = configMgr.ConfigurationRow(rowName.ToString());
                                            foreach (Configuration cfg in rows)
                                            {
                                                result.AddConfigPlatform(cfg.ConfigurationName, cfg.PlatformName);
                                            }
                                        }
                                        catch (Exception rowEx)
                                        {
                                            conlog.WriteLineDebug($"Configuration row access for '{projPath}' failed: {rowEx.Message}");
                                        }
                                    }
                                }
                                else
                                {
                                    conlog.WriteLineDebug($"Project '{projPath}' has no ConfigurationManager or no rows (non-fatal).");
                                }
                            }
                            catch (Exception exCfg)
                            {
                                conlog.WriteLineDebug($"ConfigurationManager access for '{projPath}' failed (non-fatal): {exCfg.Message}");
                            }

                            // success: result populated
                            return;
                        }
                        catch (COMException ex) when ((uint)ex.ErrorCode == 0x8001010A || (uint)ex.ErrorCode == 0x80010001)
                        {
                            // transient RPC errors — retry
                            conlog.WriteLineWarn("Transient COM error loading '{0}' (0x{1:X8}) — retrying in {2:0.0}s...", projPath, ex.ErrorCode, waitSeconds);
                        }
                        catch (RetryableProjectLoadException rex)
                        {
                            conlog.WriteLineWarn("Failed to obtain usable project for '{0}' — attempt {1}/{2}: {3}", projPath, attempt, maxRetries, rex.Message);
                        }
                        catch (COMException ex)
                        {
                            uint hr = (uint)ex.ErrorCode;
                            if (hr == 0x80004005)
                            {
                                // E_FAIL: attempt fallback handled earlier; if still thrown here treat non-retryable
                                conlog.WriteLineWarn("COMException E_FAIL loading '{0}' — aborting further retries.", projPath);
                                break;
                            }
                            else
                            {
                                conlog.WriteLineWarn("COMException loading '{0}' (0x{1:X8}) — will retry in {2:0.0}s", projPath, ex.ErrorCode, waitSeconds);
                            }
                        }
                        catch (Exception ex)
                        {
                            conlog.WriteLineWarn("Unexpected exception loading '{0}': {1} — will retry in {2:0.0}s", projPath, ex.Message, waitSeconds);
                        }

                        // pump messages and wait before next attempt; cap wait
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(Convert.ToInt32(waitSeconds * 1000));
                        waitSeconds = Math.Min(waitSeconds * 2.0, 5.0);
                    } // attempts loop
                }
                finally
                {
                    MessageFilter.Revoke();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return result;
        }

        // Helper to clamp unrealistic tiny initial retry values
        private double initialRetryAfterClamp(double val)
        {
            if (val < 0.05) return 0.05;
            return val;
        }

        /// <summary>
        /// ExtractProjectData: orchestrates DTE instantiation (if required) and calls STA loader.
        /// Returns the extracted project data.
        /// </summary>
        public ExtractedProjData ExtractProjectData(string projPath)
        {
            try
            {
                if (!conlog.IsInitialized())
                {
                    conlog.InitLogging(_outModeLogging, Path.Combine(_strLogPath, $"{_timeStampPrefix}_{_assemblyName}.log"));
                }

                // instantiate DTE once
                if (!_bDteInstanciated)
                {
                    string progId = $"VisualStudio.DTE.{_VS_MajorVersion}.0";
                    _dte = Activator.CreateInstance(Type.GetTypeFromProgID(progId)) as DTE;

                    MessageFilter.Register();
                    _dte.MainWindow.Visible = _showVisualStudio;
                    _dte.SuppressUI = !_showVisualStudio;
                    _dte.UserControl = _showVisualStudio;

                    _dte.Solution.Create(Path.GetTempPath(), $"{_timeStampPrefix}_{_assemblyName}.sln");
                    _bDteInstanciated = true;

                    System.Threading.Thread.Sleep(Convert.ToInt32(1000 * _solutionSleepAfterCreate));
                }

                // perform STA load + extraction
                ExtractedProjData extracted = LoadProjectAndExtractData(projPath, _projRetriesCount, _projInitialRetryAfterSeconds);

                // always return a non-null ExtractedProjData
                extracted = extracted ?? new ExtractedProjData { _TypeGuid = "UNKNOWN", _ConfigsPlatforms = null };

                // log what we got
                conlog.WriteLineDebug("Loaded project '{0}' -> TypeGuid={1}, Configs={2}", projPath, extracted._TypeGuid, (extracted._ConfigsPlatforms?.Length ?? 0));

                return extracted;
            }
            catch (Exception ex)
            {
                conlog.WriteLineException(ex, $"occurred for project file '{projPath}' loaded in Visual Studio {_VS_MajorVersion}");
                return null;
            }
        }
    }
}

