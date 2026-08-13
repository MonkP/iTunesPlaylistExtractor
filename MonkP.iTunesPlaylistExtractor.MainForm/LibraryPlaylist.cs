using ITunesLibraryParser;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// 从 iTunes Library XML 的 Playlists 节点解析出的播放列表（或播放列表文件夹）元数据。
    /// 播放列表之间的父子嵌套关系通过 <see cref="PlaylistPersistentId"/> 与
    /// <see cref="ParentPersistentId"/> 关联。
    /// </summary>
    public class LibraryPlaylist
    {
        public int PlaylistId { get; set; }

        public string PlaylistPersistentId { get; set; }

        public string ParentPersistentId { get; set; }

        public string Name { get; set; }

        /// <summary>是否为播放列表文件夹。</summary>
        public bool IsFolder { get; set; }

        /// <summary>是否为主资料库（资料库/Master）。</summary>
        public bool IsMaster { get; set; }

        /// <summary>iTunes 内置的系统播放列表类别（如 音乐/影片/播客 等），非用户创建。</summary>
        public int? DistinguishedKind { get; set; }

        /// <summary>由 ITunesLibraryParser 解析出的播放列表对象，Tracks 已解析为 Track 引用。</summary>
        public Playlist Playlist { get; set; }

        /// <summary>用于显示的曲目数量：普通列表为自身曲目数；文件夹为其下所有列表曲目去重后的总数。</summary>
        public int DisplayTrackCount { get; set; }

        /// <summary>在树形结构中的层级深度，0 为顶层，用于名称缩进显示。</summary>
        public int IndentLevel { get; set; }
    }
}
