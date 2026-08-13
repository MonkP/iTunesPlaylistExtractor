using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// 偏好数据：记录窗体中选择的路径及上次执行导出时勾选的播放列表 Persistent ID。
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

        /// <summary>默认日志输出级别名称（Fatal/Error/Warn/Info/Debug），未配置时为 Error。</summary>
        [DataMember]
        public string LogLevel { get; set; }
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
