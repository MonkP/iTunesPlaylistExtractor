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

        /// <summary>Windows API 常量：标准输出句柄。</summary>
        private const int StdOutputHandle = -11;

        /// <summary>日志队列，调用方写入、后台线程消费。</summary>
        private static readonly BlockingCollection<LogEntry> LogQueue = new BlockingCollection<LogEntry>();
        /// <summary>后台日志写入线程，负责从队列中取出日志并写入文件。</summary>
        private static readonly Thread WriterThread;
        /// <summary>当前进程是否拥有可用的控制台窗口（用于决定是否输出到控制台）。</summary>
        private static readonly bool HasConsole;
        /// <summary>关闭标志：0=未关闭，1=已请求关闭，通过 CAS 保证只设置一次。</summary>
        private static int _shutdownRequested;

        /// <summary>
        /// 静态构造函数：检测控制台可用性并启动后台日志写入线程。
        /// </summary>
        static LogHelper()
        {
            // 通过标准输出句柄判断当前进程是否附带控制台
            HasConsole = GetStdHandle(StdOutputHandle) != IntPtr.Zero;
            WriterThread = new Thread(WriterLoop) { IsBackground = true, Name = "LogWriter" };
            WriterThread.Start();
        }

        /// <summary>Windows API：获取指定标准设备（如标准输出）的句柄，用于检测控制台是否存在。</summary>
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

        /// <summary>
        /// 将异常（含 InnerException 链）递归序列化为 JSON 字符串，包含 Type、Message、StackTrace。
        /// 序列化失败时回退为简单的文本格式。
        /// </summary>
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

        /// <summary>
        /// 递归将异常对象追加为 JSON 格式，包含 Type、Message、StackTrace 三个字段；
        /// 若存在 InnerException 则递归追加为嵌套的 "InnerException" 字段。
        /// </summary>
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

        /// <summary>
        /// 将字符串转换为 JSON 格式的带引号字符串，对特殊字符进行转义
        /// （双引号、反斜杠、换行、制表符及控制字符）；null 输入返回 "null"。
        /// </summary>
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
                        // 其他控制字符使用 \uXXXX 转义
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

        /// <summary>
        /// 后台写入线程的主循环：从 <see cref="LogQueue"/> 中消费日志条目，
        /// 按日期分文件写入运行目录下的 log 文件夹（yyyy-MM-dd.log）；
        /// 跨天时自动切换文件，每条写入后立即 Flush。
        /// </summary>
        private static void WriterLoop()
        {
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            StreamWriter writer = null;
            DateTime currentDate = DateTime.MinValue;
            try
            {
                // 阻塞式消费队列，直到 CompleteAdding 被调用
                foreach (var entry in LogQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        // 跨天或首次写入时创建/切换日志文件
                        if (writer == null || entry.LocalDate.Date != currentDate)
                        {
                            writer?.Dispose();
                            Directory.CreateDirectory(logDirectory);
                            var logFile = Path.Combine(logDirectory, $"{entry.LocalDate:yyyy-MM-dd}.log");
                            // 以追加模式、带 BOM 的 UTF-8 编码打开
                            writer = new StreamWriter(logFile, true, new UTF8Encoding(true));
                            currentDate = entry.LocalDate.Date;
                        }
                        writer.Write(entry.Text);
                        writer.Flush();
                    }
                    catch
                    {
                        // 日志写入失败不应影响程序运行，静默忽略
                    }
                }
            }
            finally
            {
                writer?.Dispose();
            }
        }

        /// <summary>日志队列中的单条记录，包含本地日期（用于分文件）和格式化后的日志文本。</summary>
        private class LogEntry
        {
            /// <summary>日志产生的本地日期，用于按天分文件。</summary>
            public DateTime LocalDate { get; set; }
            /// <summary>已格式化的日志文本。</summary>
            public string Text { get; set; }
        }
    }
}
