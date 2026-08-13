using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// 日志输出工具类。日志写入运行目录下 log 文件夹中按日期命名的文件（yyyy-MM-dd.log）。
    /// WriteLog 只负责格式化并入队，由独立的后台线程从队列中读取并实际写文件；
    /// 程序正常退出前应调用 <see cref="Shutdown"/> 确保队列中的日志写完。
    /// </summary>
    internal static class LogHelper
    {
        /// <summary>日志级别，数值越大越严重，level &gt;= 默认级别时才写入日志文件。</summary>
        internal enum LogLevel
        {
            Fatal = 4,
            Error = 3,
            Warn = 2,
            Info = 1,
            Debug = 0,
        }

        /// <summary>默认输出日志级别，从 preference.json 加载，未配置时为 Error。</summary>
        internal static volatile LogLevel DefaultLevel = LogLevel.Error;

        private const int StdOutputHandle = -11;

        private static readonly BlockingCollection<LogEntry> LogQueue = new BlockingCollection<LogEntry>();
        private static readonly Thread WriterThread;
        private static readonly bool HasConsole;
        private static int _shutdownRequested;

        static LogHelper()
        {
            HasConsole = GetStdHandle(StdOutputHandle) != IntPtr.Zero;
            WriterThread = new Thread(WriterLoop) { IsBackground = true, Name = "LogWriter" };
            WriterThread.Start();
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        /// <summary>
        /// 格式化一条日志：级别达到默认级别时入队等待写入日志文件；
        /// 有可用的调试控制台时始终输出到控制台。返回格式化后的日志文本。
        /// </summary>
        internal static string WriteLog(LogLevel level, string content, Exception ex = null)
        {
            var message = FormatMessage(level, content, ex);
            if (HasConsole)
            {
                Console.Write(message);
            }
            if (level >= DefaultLevel)
            {
                LogQueue.Add(new LogEntry { LocalDate = DateTime.Now, Text = message });
            }
            return message;
        }

        /// <summary>停止接收新日志，等待队列中的日志全部写完。</summary>
        internal static void Shutdown()
        {
            if (Interlocked.CompareExchange(ref _shutdownRequested, 1, 0) != 0)
                return;
            LogQueue.CompleteAdding();
            WriterThread.Join(TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// 日志格式：UTC 毫秒时间戳:级别 / content / 序列化的 ex / 空行，
        /// 各部分之间以当前环境换行符分隔。
        /// </summary>
        private static string FormatMessage(LogLevel level, string content, Exception ex)
        {
            var newLine = Environment.NewLine;
            var builder = new StringBuilder();
            builder.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffK")).Append(':')
                .Append(level).Append(newLine);
            builder.Append(content ?? string.Empty).Append(newLine);
            if (ex != null)
            {
                builder.Append(SerializeException(ex)).Append(newLine);
            }
            builder.Append(newLine);
            return builder.ToString();
        }

        /// <summary>将异常（含 InnerException 链）序列化为 JSON，包含 Type、Message、StackTrace。</summary>
        private static string SerializeException(Exception ex)
        {
            try
            {
                var builder = new StringBuilder();
                AppendExceptionJson(builder, ex);
                return builder.ToString();
            }
            catch
            {
                return $"Type: {ex.GetType().FullName}, Message: {ex.Message}, StackTrace: {ex.StackTrace}";
            }
        }

        private static void AppendExceptionJson(StringBuilder builder, Exception ex)
        {
            builder.Append('{');
            builder.Append("\"Type\":").Append(ToJsonString(ex.GetType().FullName)).Append(',');
            builder.Append("\"Message\":").Append(ToJsonString(ex.Message)).Append(',');
            builder.Append("\"StackTrace\":").Append(ToJsonString(ex.StackTrace));
            if (ex.InnerException != null)
            {
                builder.Append(",\"InnerException\":");
                AppendExceptionJson(builder, ex.InnerException);
            }
            builder.Append('}');
        }

        private static string ToJsonString(string value)
        {
            if (value == null)
                return "null";
            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (ch < ' ')
                            builder.Append("\\u").Append(((int)ch).ToString("x4"));
                        else
                            builder.Append(ch);
                        break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }

        private static void WriterLoop()
        {
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            StreamWriter writer = null;
            DateTime currentDate = DateTime.MinValue;
            try
            {
                foreach (var entry in LogQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        if (writer == null || entry.LocalDate.Date != currentDate)
                        {
                            writer?.Dispose();
                            Directory.CreateDirectory(logDirectory);
                            var logFile = Path.Combine(logDirectory, $"{entry.LocalDate:yyyy-MM-dd}.log");
                            writer = new StreamWriter(logFile, true, new UTF8Encoding(true));
                            currentDate = entry.LocalDate.Date;
                        }
                        writer.Write(entry.Text);
                        writer.Flush();
                    }
                    catch
                    {
                        // 日志写入失败不应影响程序运行
                    }
                }
            }
            finally
            {
                writer?.Dispose();
            }
        }

        private class LogEntry
        {
            public DateTime LocalDate { get; set; }
            public string Text { get; set; }
        }
    }
}
