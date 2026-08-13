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
        public static List<LibraryPlaylist> Parse(string xmlLibraryPath)
        {
            var result = new List<LibraryPlaylist>();
            var doc = XDocument.Load(xmlLibraryPath);
            var rootDict = doc.Element("plist")?.Element("dict");
            var playlistsArray = GetValueByKey(rootDict, "Playlists");
            if (playlistsArray == null)
                return result;

            foreach (var playlistElement in playlistsArray.Elements("dict"))
            {
                result.Add(CreatePlaylist(playlistElement));
            }
            return result;
        }

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

        private static string ParseStringValue(XElement dict, string keyName)
        {
            return GetValueByKey(dict, keyName)?.Value;
        }

        private static bool ParseBooleanValue(XElement dict, string keyName)
        {
            return GetValueByKey(dict, keyName)?.Name.LocalName == "true";
        }

        private static int? ParseNullableIntValue(XElement dict, string keyName)
        {
            var value = ParseStringValue(dict, keyName);
            return string.IsNullOrEmpty(value) ? (int?)null : int.Parse(value);
        }
    }
}
