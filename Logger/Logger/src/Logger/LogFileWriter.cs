using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace h.mu.Logger
{
    public class LogFileWriter
    {
        /// <summary>
        /// 書込みで利用するバッファサイズ.
        /// </summary>
        public static readonly int BUFFER_SIZE = 256;

        #region property
        public LogLevel logLevel
        {
            set;
            get;
        }
        #endregion

        #region field
        /// <summary>
        /// 循環形式のライタ
        /// </summary>
        private RollingFileWriter writer;

        private Object locker = new Object();
        #endregion

        public LogFileWriter(LogLevel logLevel, String dir, String name, String ext, int fileSize, int fileCount) 
        {
            this.logLevel = logLevel;
            this.writer = new RollingFileWriter(dir, name, ext, fileSize, fileCount);
        }

        public LogFileWriter(System.Configuration.ApplicationSettingsBase setting, String prefix)
        {
            this.logLevel = (LogLevel)setting[prefix + "logLevel"];
            this.writer = new RollingFileWriter(setting, prefix);
        }

        public void writeLog(DateTime dateTime, LogLevel logLevel, String callerMethod, String message, params Object[] objs)
        {
            String logText = makeLogText(dateTime, logLevel, callerMethod, message, objs);

            lock (locker)
            {
                try
                {
                    this.writer.open();
                    this.writer.writeLine(logText);
                    this.writer.close();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceError("writeLog:" + e.Message + "," + e.StackTrace);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void log(LogLevel logLevel, String message, params Object[] objs)
        {
            if (!isOutput(logLevel))
            {
                return;
            }
            const int callerFrameIndex = 2;
            System.Diagnostics.StackFrame callerFrame = new System.Diagnostics.StackFrame(callerFrameIndex);
            System.Reflection.MethodBase callerMethod = callerFrame.GetMethod();
            this.writeLog(DateTime.Now, logLevel, callerMethod.Name, message, objs);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void debug(String message, params Object[] objs)
        {
            log(LogLevel.DEBUG, message, objs);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void info(String message, params Object[] objs)
        {
            log(LogLevel.INFO, message, objs);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void warn(String message, params Object[] objs)
        {
            log(LogLevel.WARNING, message, objs);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void error(String message, params Object[] objs)
        {
            log(LogLevel.ERROR, message, objs);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void error(String message, Exception e)
        {
            log(LogLevel.ERROR, message + "[message:"  + e.Message + "][stack:" + e.StackTrace + "]");
        }

        private bool isOutput(LogLevel logLevel)
        {
            return this.logLevel <= logLevel;
        }

        private String makeLogText(DateTime dateTime, LogLevel logLevel, String callerMethod, String message, params Object[] objs)
        {
            StringBuilder builder = new StringBuilder(BUFFER_SIZE);
            builder.Append(dateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"));
            builder.Append(",");
            builder.Append(logLevelToStr(logLevel));
            builder.Append(",");
            builder.Append(System.Threading.Thread.CurrentThread.ManagedThreadId);
            builder.Append(",");
            builder.Append(callerMethod);
            builder.Append(",");
            if (objs != null)
            {
                builder.Append(String.Format(message, objs));
            }
            else
            {
                builder.Append(message);
            }
            return builder.ToString();
        }

        private String logLevelToStr(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.DEBUG:
                    return "D";
                case LogLevel.INFO:
                    return "I";
                case LogLevel.WARNING:
                    return "W";
                case LogLevel.ERROR:
                    return "E";
                default:
                    return "?";
            }
        }
                 
    }
}
