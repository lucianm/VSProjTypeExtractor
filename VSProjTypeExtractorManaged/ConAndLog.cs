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

        private string _filePath = "";
        private bool _FileIsOpen = false;
        private static readonly Lazy<ConAndLog> lazy = new Lazy<ConAndLog>(() => new ConAndLog());
        private OutMode _outMode = OutMode.OutConsole;
        public static ConAndLog Instance { get { return lazy.Value; } }
        public StreamWriter _SW { get; set; }
        public FileStream _ostrm { get; set; }
        private bool _IsInitialized = false;

        public bool IsInitialized() { return _IsInitialized; }

        private ConAndLog()
        {
        }

        ~ConAndLog()
        {
            DisposeStreamWriter();
        }

        public void WriteLine(string format, params Object[] args)
        {
            string strFmtMsg = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " - " + format;
            Debug.WriteLine(strFmtMsg, args);
        }

        public void WriteLineRethrow(Exception ex, string format, params Object[] args)
        {
            String MsgExc = string.Format(format, args);
            WriteLine(MsgExc + Environment.NewLine + "   EXCEPTION:" + ex.ToString() + Environment.NewLine);
            throw new ApplicationException(MsgExc, ex);
        }

        public void InitLogging(OutMode outMode = OutMode.OutConsole, string filePath = "")
        {
            _outMode = outMode;
            InstantiateStreamWriter(filePath);
            WriteLine("START logging configured for {0} ...", _outMode);
            _IsInitialized = true;
        }

        public void CloseLogging()
        {
            WriteLine("STOP logging ...");
            Debug.Flush();
            Debug.Listeners.Clear();
            _IsInitialized = false;
            try
            {
                if (_FileIsOpen)
                {
                    _SW.Flush();
                    _SW.Close();
                    _ostrm.Close();
                    _FileIsOpen = false;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException(string.Format("Access denied. Could not close StreamWriter using path: {0}.", _filePath), ex);
            }
        }

        private void DisposeStreamWriter()
        {
            if (_SW != null)
            {
                try
                {
                    _SW.Dispose();
                }
                catch (ObjectDisposedException) { } // object already disposed - ignore exception
            }
        }

        private void InstantiateStreamWriter(string filePath)
        {
            DisposeStreamWriter();
            try
            {
                if ((_outMode & OutMode.OutLogfile) == OutMode.OutLogfile)
                {
                    // if logfile is active
                    EnsureLogDirectoryExists(Directory.GetParent(filePath).ToString());
                    _ostrm = new FileStream(filePath, FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write);
                    _SW = new StreamWriter(_ostrm);
                    _SW.AutoFlush = true;
                    _filePath = filePath;
                    _FileIsOpen = true;

                    TextWriterTraceListener tr = new TextWriterTraceListener(_SW);
                    Debug.Listeners.Add(tr);
                }

                if ((_outMode & OutMode.OutConsole) == OutMode.OutConsole)
                {
                    // if console is active
                    TextWriterTraceListener tr = new TextWriterTraceListener(System.Console.Out);
                    Debug.Listeners.Add(tr);
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
