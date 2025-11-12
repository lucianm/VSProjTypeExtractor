/*
    VSProjTypeExtractor - Visual Studio project type GUID extractor
    ConAndLog.cs - Console and file logger
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
using System.IO;
using System.Diagnostics;

namespace VSProjTypeExtractorManaged
{
    public sealed class ConAndLog
    {
        [Flags]
        public enum OutMode {
            OutNone = 0x0,
            OutConsole = 0x01,
            OutLogfile = 0x02
        };
        public enum LogLevel
        {
            DEBUG,
            INFO,
            WARN,
            ERROR,
            FATAL
        }

        private static readonly Lazy<ConAndLog> lazy = new Lazy<ConAndLog>(() => new ConAndLog());
        public static ConAndLog Instance => lazy.Value;

        private LogLevel m_currentLevel = LogLevel.DEBUG; // default
        private readonly object m_lock = new object();

        private string _filePath = "";
        private bool _FileIsOpen = false;
        private OutMode _outMode = OutMode.OutConsole;
        private bool _IsInitialized = false;

        public bool IsInitialized() { return _IsInitialized; }

        private ConAndLog()
        {
            Trace.AutoFlush = true;
        }

        ~ConAndLog()
        {
        }

        public void SetLogLevel(string levelName)
        {
            if (Enum.TryParse<LogLevel>(levelName, true, out var level))
            {
                m_currentLevel = level;
            }
            else
            {
                throw new ArgumentException($"Invalid log level: {levelName}");
            }
        }

        public void WriteLine(LogLevel level, string format, params Object[] args)
        {
            lock (m_lock)
            {
                if (level < m_currentLevel)
                    return; // filtered out
            }

            string timestamp = DateTimeOffset.Now.ToString("o");
            string strFmtMsg = $"{timestamp} - [{level}]: {format}";
            Trace.WriteLine(string.Format(strFmtMsg, args));
        }

        public void WriteLineDebug(string format, params Object[] args) {WriteLine(LogLevel.DEBUG, format, args);}
        public void WriteLineInfo(string format, params Object[] args) { WriteLine(LogLevel.INFO, format, args); }
        public void WriteLineWarn(string format, params Object[] args) { WriteLine(LogLevel.WARN, format, args); }
        public void WriteLineError(string format, params Object[] args) { WriteLine(LogLevel.ERROR, format, args); }
        public void WriteLineFatal(string format, params Object[] args) { WriteLine(LogLevel.FATAL, format, args); }

        public void WriteLineRethrow(Exception ex, string format, params Object[] args)
        {
            String MsgExc = string.Format(format, args);
            WriteLineFatal(MsgExc + Environment.NewLine + "   EXCEPTION:" + ex.ToString() + Environment.NewLine);
            throw new ApplicationException(MsgExc, ex);
        }

        public void InitLogging(OutMode outMode = OutMode.OutConsole, string filePath = "")
        {
            _outMode = outMode;
            InstantiateStreamWriter(filePath);
            WriteLineInfo("START logging configured for {0} ...", _outMode);
            _IsInitialized = true;
        }

        public void CloseLogging()
        {
            WriteLineInfo("STOP logging ...");
            Trace.Flush();
            Trace.Listeners.Clear();
            _IsInitialized = false;
            try
            {
                if (_FileIsOpen)
                {
                    _FileIsOpen = false;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException(string.Format("Access denied. Could not close StreamWriter using path: {0}.", _filePath), ex);
            }
        }

        private void InstantiateStreamWriter(string filePath)
        {
            try
            {
                if ((_outMode & OutMode.OutLogfile) == OutMode.OutLogfile)
                {
                    // if logfile is active
                    EnsureLogDirectoryExists(Directory.GetParent(filePath).ToString());
                    Trace.Listeners.Add(new TextWriterTraceListener(filePath));
                    _filePath = filePath;
                    _FileIsOpen = true;
                }

                if ((_outMode & OutMode.OutConsole) == OutMode.OutConsole)
                {
                    // if console is active
                    Trace.Listeners.Add(new ConsoleTraceListener());
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine("Cannot open '" + filePath + "' for writing");
                throw new ApplicationException(string.Format("Access denied. Could not instantiate StreamWriter using path: {0}.", filePath), ex);
            }
        }

        private void EnsureLogDirectoryExists(string logDir)
        {
            if (!Directory.Exists(logDir))
            {
                try
                {
                    Directory.CreateDirectory(logDir);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new ApplicationException(string.Format("Access denied. Could not create log directory using path: {0}.", logDir), ex);
                }
            }
        }
    }
}
