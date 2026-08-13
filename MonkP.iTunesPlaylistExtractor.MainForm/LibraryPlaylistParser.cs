using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// 解析 iTunes Library XML 顶层 dict 中的 Playlists 数组，
    /// 提取播放列表的层级元数据（Persistent ID、Parent Persistent ID、Folder 等）。
    /// 这些字段是 ITunesLibraryParser 包的 Playlist 类未提供的，
    /// 用于还原播放列表文件夹的嵌套结构。
    /// </summary>
    public static class LibraryPlaylistParser
    {
        /// <summary>
        /// 解析指定路径的 iTunes Library XML，提取所有播放列表的层级元数据。
        /// </summary>
        /// <param name="xmlLibraryPath">iTunes Library XML 文件的完整路径。</param>
        /// <returns>包含所有播放列表（含系统播放列表）的 <see cref="LibraryPlaylist"/> 列表。</returns>
        public static List<LibraryPlaylist> Parse(string xmlLibraryPath)
        {
            var result = new List<LibraryPlaylist>();
            var doc = XDocument.Load(xmlLibraryPath);
            // 定位 plist > dict 顶层节点
            var rootDict = doc.Element("plist")?.Element("dict");
            // 取顶层 dict 中 key="Playlists" 对应的 array 元素
            var playlistsArray = GetValueByKey(rootDict, "Playlists");
            if (playlistsArray == null)
                return result;

            // 遍历 array 中每个 dict，解析为一条 LibraryPlaylist 记录
            foreach (var playlistElement in playlistsArray.Elements("dict"))
            {
                result.Add(CreatePlaylist(playlistElement));
            }
            return result;
        }

        /// <summary>
        /// 从单个播放列表 dict 元素中提取各字段，构建 <see cref="LibraryPlaylist"/> 实例。
        /// </summary>
        private static LibraryPlaylist CreatePlaylist(XElement playlistElement)
        {
            return new LibraryPlaylist
            {
                PlaylistId = int.Parse(ParseStringValue(playlistElement, "Playlist ID")),
                PlaylistPersistentId = ParseStringValue(playlistElement, "Playlist Persistent ID"),
                ParentPersistentId = ParseStringValue(playlistElement, "Parent Persistent ID"),
                Name = ParseStringValue(playlistElement, "Name"),
                IsFolder = ParseBooleanValue(playlistElement, "Folder"),
                IsMaster = ParseBooleanValue(playlistElement, "Master"),
                DistinguishedKind = ParseNullableIntValue(playlistElement, "Distinguished Kind"),
            };
        }

        /// <summary>
        /// 按 plist 的 key/value 交替结构，取 dict 直接子级中指定 key 对应的值元素。
        /// 只遍历直接子级，避免误取嵌套节点中的同名 key。
        /// </summary>
        private static XElement GetValueByKey(XElement dict, string keyName)
        {
            if (dict == null)
                return null;
            foreach (var keyElement in dict.Elements("key"))
            {
                if (keyElement.Value == keyName)
                    return keyElement.ElementsAfterSelf().FirstOrDefault();
            }
            return null;
        }

        /// <summary>从 dict 中读取指定 key 对应的 string 值元素，不存在时返回 null。</summary>
        private static string ParseStringValue(XElement dict, string keyName)
        {
            return GetValueByKey(dict, keyName)?.Value;
        }

        /// <summary>
        /// 从 dict 中读取指定 key 对应的布尔值；
        /// plist 中布尔值以 &lt;true/&gt; 或 &lt;false/&gt; 元素表示，通过元素名判断。
        /// </summary>
        private static bool ParseBooleanValue(XElement dict, string keyName)
        {
            return GetValueByKey(dict, keyName)?.Name.LocalName == "true";
        }

        /// <summary>从 dict 中读取指定 key 对应的整数值；若 key 不存在则返回 null。</summary>
        private static int? ParseNullableIntValue(XElement dict, string keyName)
        {
            var value = ParseStringValue(dict, keyName);
            return string.IsNullOrEmpty(value) ? (int?)null : int.Parse(value);
        }
    }
}
