using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>提取目标的类型：本地目录或经 MTP 连接的安卓设备。</summary>
    public enum TargetKind
    {
        /// <summary>本地硬盘目录。</summary>
        Local,

        /// <summary>经 MTP 连接的安卓设备路径。</summary>
        Device,
    }

    /// <summary>
    /// 偏好数据：记录窗体中选择的路径、目标类型及上次执行导出时勾选的播放列表 Persistent ID。
    /// </summary>
    [DataContract]
    public class Preferences
    {
        [DataMember]
        public string LibraryFilePath { get; set; }

        [DataMember]
        public string RootPath { get; set; }

        [DataMember]
        public string TargetPath { get; set; }

        [DataMember]
        public List<string> CheckedPlaylistPersistentIds { get; set; }

        /// <summary>提取时是否覆盖目标路径中已存在的音乐文件（m3u 始终覆盖）。</summary>
        [DataMember]
        public bool OverwriteFiles { get; set; }

        /// <summary>默认日志输出级别名称（Fatal/Error/Warn/Info/Debug），未配置时为 Error。</summary>
        [DataMember]
        public string LogLevel { get; set; }

        /// <summary>目标路径类型（本地目录或设备路径），未配置时为本地目录。</summary>
        [DataMember]
        public TargetKind TargetKind { get; set; }

        /// <summary>目标为设备时记录的 DeviceId（WPD 设备标识）。</summary>
        [DataMember]
        public string DeviceId { get; set; }

        /// <summary>目标为设备时记录的设备路径友好字面量（仅用于显示，形如“Pixel内部共享存储空间\Music”）。</summary>
        [DataMember]
        public string DevicePath { get; set; }

        /// <summary>目标为设备时记录的原始设备路径（用于连接，形如“内部共享存储空间\Music”）。</summary>
        [DataMember]
        public string DeviceRawPath { get; set; }

        /// <summary>
        /// 同步失败中止阈值的两个参数（只读，不提供 UI）：
        /// 待同步数达到 MaxErrorRatio 比例的失败或累计 MaxErrorCount 个失败即中止，
        /// 取两者中较大的数作为阈值。写入偏好文件仅作存档用途。
        /// </summary>
        public const double MaxErrorRatio = 0.1;
        public const int MaxErrorCount = 10;
    }

    /// <summary>
    /// 在程序运行目录下加载/创建 preference.json。
    /// </summary>
    public static class PreferenceStore
    {
        public static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preference.json");

        /// <summary>尝试加载偏好文件；文件不存在或解析失败时返回 null。</summary>
        public static Preferences Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return null;
                using (var stream = File.OpenRead(FilePath))
                {
                    return (Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>创建或覆盖偏好文件。</summary>
        public static void Save(Preferences preferences)
        {
            using (var stream = File.Create(FilePath))
            {
                new DataContractJsonSerializer(typeof(Preferences)).WriteObject(stream, preferences);
            }
        }
    }
}
