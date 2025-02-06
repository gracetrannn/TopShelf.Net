// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
using System;
using System.Globalization;
using ElmahCore; // Ensure this is compatible with .NET 8
using Topshelf.Logging;

namespace Topshelf.Logging
{
    public class ElmahLogLevels
    {
        public bool IsDebugEnabled { get; set; } = true;
        public bool IsInfoEnabled { get; set; } = true;
        public bool IsWarnEnabled { get; set; } = true;
        public bool IsErrorEnabled { get; set; } = true;
        public bool IsFatalEnabled { get; set; } = true;
    }

    public enum ElmahLogLevelsEnum
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    public class ElmahLogWriter : LogWriter
    {
        private readonly ErrorLog _log;
        private readonly ElmahLogLevels _logLevels;

        // Change this constructor to instantiate your desired ErrorLog type
        public ElmahLogWriter()
            : this(new MemoryErrorLog()) //.
        { }

        public ElmahLogWriter(ErrorLog log)
            
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));//.
        }

        public ElmahLogWriter(ErrorLog log, ElmahLogLevels logLevels)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _logLevels = logLevels ?? new ElmahLogLevels();
        }

        private void WriteToElmah(ElmahLogLevelsEnum logLevel, LogWriterOutputProvider messageProvider)
        {
            if (messageProvider == null) return;
            WriteToElmah(logLevel, messageProvider().ToString());
        }

        private void WriteToElmah(ElmahLogLevelsEnum logLevel, string format, params object[] args)
        {
            WriteToElmah(logLevel, string.Format(format, args));
        }

        private void WriteToElmah(ElmahLogLevelsEnum logLevel, string message, Exception? exception = null)
        {
            var error = exception == null ? new Error() : new Error(exception)
            {
                Type = exception?.GetType().FullName ?? GetLogLevel(logLevel),
                Message = message,
                Time = DateTime.UtcNow, // Use UtcNow for consistency
                HostName = Environment.MachineName,
                Detail = exception == null ? message : exception.StackTrace
            };

            if (IsLogLevelEnabled(logLevel))
                _log.Log(error);
        }

        private string GetLogLevel(ElmahLogLevelsEnum logLevel) =>
            logLevel switch
            {
                ElmahLogLevelsEnum.Debug => "Debug",
                ElmahLogLevelsEnum.Info => "Info",
                ElmahLogLevelsEnum.Warn => "Warn",
                ElmahLogLevelsEnum.Error => "Error",
                ElmahLogLevelsEnum.Fatal => "Fatal",
                _ => "Unknown log level"
            };

        public void Debug(object message) => WriteToElmah(ElmahLogLevelsEnum.Debug, message.ToString());

        public void Debug(object message, Exception exception) => WriteToElmah(ElmahLogLevelsEnum.Debug, message.ToString(), exception);

        public void Debug(LogWriterOutputProvider messageProvider)
        {
            if (!IsDebugEnabled) return;
            WriteToElmah(ElmahLogLevelsEnum.Debug, messageProvider);
        }

        public void DebugFormat(string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Debug, format, args);

        public void DebugFormat(IFormatProvider provider, string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Debug, format, args);

        public void Info(object message) => WriteToElmah(ElmahLogLevelsEnum.Info, message.ToString());

        public void Info(object message, Exception exception) => WriteToElmah(ElmahLogLevelsEnum.Info, message.ToString(), exception);

        public void Info(LogWriterOutputProvider messageProvider)
        {
            if (!IsInfoEnabled) return;
            WriteToElmah(ElmahLogLevelsEnum.Info, messageProvider);
        }

        public void InfoFormat(string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Info, format, args);

        public void InfoFormat(IFormatProvider provider, string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Info, format, args);

        public void Warn(object message) => WriteToElmah(ElmahLogLevelsEnum.Warn, message.ToString());

        public void Warn(object message, Exception exception) => WriteToElmah(ElmahLogLevelsEnum.Warn, message.ToString(), exception);

        public void Warn(LogWriterOutputProvider messageProvider)
        {
            if (!IsWarnEnabled) return;
            WriteToElmah(ElmahLogLevelsEnum.Warn, messageProvider);
        }

        public void WarnFormat(string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Warn, format, args);

        public void WarnFormat(IFormatProvider provider, string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Warn, format, args);

        public void Error(object message) => WriteToElmah(ElmahLogLevelsEnum.Error, message.ToString());

        public void Error(object message, Exception exception) => WriteToElmah(ElmahLogLevelsEnum.Error, message.ToString(), exception);

        public void Error(LogWriterOutputProvider messageProvider)
        {
            if (!IsErrorEnabled) return;
            WriteToElmah(ElmahLogLevelsEnum.Error, messageProvider);
        }

        public void ErrorFormat(string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Error, format, args);

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Error, format, args);

        public void Fatal(object message) => WriteToElmah(ElmahLogLevelsEnum.Fatal, message.ToString());

        public void Fatal(object message, Exception exception) => WriteToElmah(ElmahLogLevelsEnum.Fatal, message.ToString(), exception);

        public void Fatal(LogWriterOutputProvider messageProvider)
        {
            if (!IsFatalEnabled) return;
            WriteToElmah(ElmahLogLevelsEnum.Fatal, messageProvider);
        }

        public void FatalFormat(string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Fatal, format, args);

        public void FatalFormat(IFormatProvider provider, string format, params object[] args) => WriteToElmah(ElmahLogLevelsEnum.Fatal, format, args);

        private bool IsLogLevelEnabled(ElmahLogLevelsEnum logLevel) => logLevel switch
        {
            ElmahLogLevelsEnum.Debug => IsDebugEnabled,
            ElmahLogLevelsEnum.Info => IsInfoEnabled,
            ElmahLogLevelsEnum.Warn => IsWarnEnabled,
            ElmahLogLevelsEnum.Error => IsErrorEnabled,
            ElmahLogLevelsEnum.Fatal => IsFatalEnabled,
            _ => true
        };

        public bool IsDebugEnabled => _logLevels.IsDebugEnabled;

        public bool IsInfoEnabled => _logLevels.IsInfoEnabled;

        public bool IsWarnEnabled => _logLevels.IsWarnEnabled;

        public bool IsErrorEnabled => _logLevels.IsErrorEnabled;

        public bool IsFatalEnabled => _logLevels.IsFatalEnabled;

        public void Log(LoggingLevel level, object obj)
        {
            switch (level)
            {
                case var l when l == LoggingLevel.Fatal:
                    Fatal(obj);
                    break;
                case var l when l == LoggingLevel.Error:
                    Error(obj);
                    break;
                case var l when l == LoggingLevel.Warn:
                    Warn(obj);
                    break;
                case var l when l == LoggingLevel.Info:
                    Info(obj);
                    break;
                case var l when l >= LoggingLevel.Debug:
                    Debug(obj);
                    break;
            }
        }

        public void Log(LoggingLevel level, object obj, Exception exception)
        {
            switch (level)
            {
                case var l when l == LoggingLevel.Fatal:
                    Fatal(obj, exception);
                    break;
                case var l when l == LoggingLevel.Error:
                    Error(obj, exception);
                    break;
                case var l when l == LoggingLevel.Warn:
                    Warn(obj, exception);
                    break;
                case var l when l == LoggingLevel.Info:
                    Info(obj, exception);
                    break;
                case var l when l >= LoggingLevel.Debug:
                    Debug(obj, exception);
                    break;
            }
        }

        public void Log(LoggingLevel level, LogWriterOutputProvider messageProvider)
        {
            switch (level)
            {
                case var l when l == LoggingLevel.Fatal:
                    Fatal(messageProvider);
                    break;
                case var l when l == LoggingLevel.Error:
                    Error(messageProvider);
                    break;
                case var l when l == LoggingLevel.Warn:
                    Warn(messageProvider);
                    break;
                case var l when l == LoggingLevel.Info:
                    Info(messageProvider);
                    break;
                case var l when l >= LoggingLevel.Debug:
                    Debug(messageProvider);
                    break;
            }
        }

        public void LogFormat(LoggingLevel level, string format, params object[] args)
        {
            switch (level)
            {
                case var l when l == LoggingLevel.Fatal:
                    FatalFormat(CultureInfo.InvariantCulture, format, args);
                    break;
                case var l when l == LoggingLevel.Error:
                    ErrorFormat(CultureInfo.InvariantCulture, format, args);
                    break;
                case var l when l == LoggingLevel.Warn:
                    WarnFormat(CultureInfo.InvariantCulture, format, args);
                    break;
                case var l when l == LoggingLevel.Info:
                    InfoFormat(CultureInfo.InvariantCulture, format, args);
                    break;
                case var l when l >= LoggingLevel.Debug:
                    DebugFormat(CultureInfo.InvariantCulture, format, args);
                    break;
            }
        }

        public void LogFormat(LoggingLevel level, IFormatProvider formatProvider, string format, params object[] args)
        {
            switch (level)
            {
                case var l when l == LoggingLevel.Fatal:
                    FatalFormat(formatProvider, format, args);
                    break;
                case var l when l == LoggingLevel.Error:
                    ErrorFormat(formatProvider, format, args);
                    break;
                case var l when l == LoggingLevel.Warn:
                    WarnFormat(formatProvider, format, args);
                    break;
                case var l when l == LoggingLevel.Info:
                    InfoFormat(formatProvider, format, args);
                    break;
                case var l when l >= LoggingLevel.Debug:
                    DebugFormat(formatProvider, format, args);
                    break;
            }
        }
    }
}
