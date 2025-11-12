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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

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

        private static readonly int _MaxLevelNameLength = Enum.GetNames(typeof(LogLevel)).Max(name => name.Length);

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

        public void WriteLine(LogLevel level, string format, params object[] args)
        {
            if (level < m_currentLevel)
                return;

            string timestamp = DateTimeOffset.Now.ToString("o");
            string levelStr = level.ToString().PadRight(_MaxLevelNameLength);
            string strFmtMsg = $"{timestamp} - [{levelStr}]: {format}";

            lock (m_lock)
            {
                Trace.WriteLine(string.Format(strFmtMsg, args));
            }
        }

        public void WriteLineDebug(string format, params Object[] args) {WriteLine(LogLevel.DEBUG, format, args);}

        public void WriteLineInfo(string format, params Object[] args) { WriteLine(LogLevel.INFO, format, args); }

        public void WriteLineWarn(string format, params Object[] args) { WriteLine(LogLevel.WARN, format, args); }

        public void WriteLineError(string format, params Object[] args) { WriteLine(LogLevel.ERROR, format, args); }

        public void WriteLineFatal(string format, params Object[] args) { WriteLine(LogLevel.FATAL, format, args); }

        public void WriteLineException(Exception ex, string format, params object[] args)
        {
            var sb = new StringBuilder();

            // User-provided context
            sb.AppendLine(string.Format(format, args) + ":");

            int depth = 0;
            Exception current = ex;

            while (current != null)
            {
                string indent = new string(' ', (depth + 1) * 3);

                sb.AppendLine($"{indent}EXCEPTION ({depth}): {current.GetType().FullName}");
                sb.AppendLine($"{indent}Message: {current.Message}");

                // Indent and format stack trace lines
                if (!string.IsNullOrEmpty(current.StackTrace))
                {
                    sb.AppendLine($"{indent}StackTrace:");
                    foreach (var line in current.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    {
                        sb.AppendLine($"{indent}   {line}");
                    }
                }

                current = current.InnerException;
                depth++;

                // Add a separator only if another inner exception exists
                if (current != null)
                    sb.AppendLine();
            }

            // Remove trailing whitespace/newlines for clean log output
            WriteLineFatal(sb.ToString().TrimEnd());
        }


        public void WriteLineRethrow(Exception ex, string format, params Object[] args)
        {
            WriteLineException(ex, format, args);
            throw new ApplicationException(string.Format(format, args), ex);
        }

        public void InitLogging(OutMode outMode = OutMode.OutConsole, string filePath = "")
        {
            _outMode = outMode;

            try
            {
                if ((_outMode & OutMode.OutConsole) == OutMode.OutConsole)
                {
                    // if console is active, configure it first
                    Trace.Listeners.Add(new ConsoleTraceListener());
                }

                if ((_outMode & OutMode.OutLogfile) == OutMode.OutLogfile)
                {
                    // if logfile is active
                    string logDir = Directory.GetParent(filePath).ToString();

                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }

                    Trace.Listeners.Add(new TextWriterTraceListener(filePath));
                    _filePath = filePath;
                    _FileIsOpen = true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteLineRethrow(ex, "Access denied. Could not instantiate StreamWriter using path: {0}.", filePath);
            }

            WriteLineDebug("START logging configured for {0} ...", _outMode);
            _IsInitialized = true;
        }

        public void CloseLogging()
        {
            WriteLineDebug("STOP logging ...");
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
    }
}
