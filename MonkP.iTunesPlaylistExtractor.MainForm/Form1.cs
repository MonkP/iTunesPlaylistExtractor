using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ITunesLibraryParser;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    public partial class Form1 : Form
    {
        /// <summary>按显示顺序展开后的播放列表（含文件夹），Tag 中保存 <see cref="LibraryPlaylist"/>。</summary>
        private readonly List<LibraryPlaylist> _displayedPlaylists = new List<LibraryPlaylist>();

        private bool _isLoading;

        /// <summary>从偏好文件恢复的勾选列表 Persistent ID，在播放列表加载完成后应用一次。</summary>
        private HashSet<string> _pendingCheckedIds;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var preferences = PreferenceStore.Load();
            if (preferences == null)
                return;

            txtLibraryFilePath.Text = preferences.LibraryFilePath ?? string.Empty;
            txtRootPath.Text = preferences.RootPath ?? string.Empty;
            txtTargetBox.Text = preferences.TargetPath ?? string.Empty;
            if (Enum.TryParse(preferences.LogLevel, true, out LogHelper.LogLevel logLevel))
                LogHelper.DefaultLevel = logLevel;
            _pendingCheckedIds = new HashSet<string>(
                preferences.CheckedPlaylistPersistentIds ?? Enumerable.Empty<string>());

            if (File.Exists(txtLibraryFilePath.Text))
                LoadLibraryAsync();
        }

        private void btnChooseLibFile_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose your iTunes Library XML file";
                dialog.Filter = "iTunes Library XML (*.xml)|*.xml|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                txtLibraryFilePath.Text = dialog.FileName;
            }
            LoadLibraryAsync();
        }

        private void btnChooseRootPath_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose the root path of your music library";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                txtRootPath.Text = dialog.SelectedPath;
            }
        }

        private void btnChooseTargetPath_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose the target path to extract playlists to";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                txtTargetBox.Text = dialog.SelectedPath;
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            SavePreferences();
            // TODO: 实现音乐文件提取与 .m3u 生成（Goal Features 6、7）
        }

        /// <summary>
        /// 将窗体当前选择的路径与勾选的播放列表 Persistent ID 写入运行目录下的 preference.json。
        /// </summary>
        private void SavePreferences()
        {
            var checkedIds = new List<string>();
            foreach (ListViewItem item in listViewPlaylists.Items)
            {
                if (item.Checked && item.Tag is LibraryPlaylist playlist)
                    checkedIds.Add(playlist.PlaylistPersistentId);
            }

            try
            {
                PreferenceStore.Save(new Preferences
                {
                    LibraryFilePath = txtLibraryFilePath.Text?.Trim(),
                    RootPath = txtRootPath.Text?.Trim(),
                    TargetPath = txtTargetBox.Text?.Trim(),
                    CheckedPlaylistPersistentIds = checkedIds,
                    LogLevel = LogHelper.DefaultLevel.ToString(),
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Error, $"Failed to save preference.json: {PreferenceStore.FilePath}", ex);
                MessageBox.Show(this, $"Failed to save preference.json:{Environment.NewLine}{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LoadLibraryAsync()
        {
            var xmlPath = txtLibraryFilePath.Text?.Trim();
            if (string.IsNullOrEmpty(xmlPath) || !File.Exists(xmlPath))
            {
                MessageBox.Show(this, "The library XML file does not exist.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (_isLoading)
                return;

            _isLoading = true;
            lblStatus.Text = "Loading library...";
            try
            {
                var playlists = await Task.Run(() => BuildPlaylistHierarchy(xmlPath));
                PopulatePlaylistListView(playlists);
                lblStatus.Text = $"Loaded {playlists.Count(p => !p.IsFolder)} playlists" +
                    $" in {playlists.Count(p => p.IsFolder)} folders.";
                LogHelper.WriteLog(LogHelper.LogLevel.Info, $"Library loaded: {xmlPath}, " +
                    $"{playlists.Count(p => !p.IsFolder)} playlists in {playlists.Count(p => p.IsFolder)} folders.");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Failed to load library.";
                LogHelper.WriteLog(LogHelper.LogLevel.Error, $"Failed to load the library XML file: {xmlPath}", ex);
                MessageBox.Show(this, $"Failed to load the library XML file:{Environment.NewLine}{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// 解析 Library XML，构建播放列表层级并按显示顺序展开：
        /// 曲目数据由 ITunesLibraryParser 解析；层级元数据由 <see cref="LibraryPlaylistParser"/> 补充；
        /// 过滤掉主资料库与 iTunes 系统播放列表后，按父子关系展开为平铺列表。
        /// </summary>
        private static List<LibraryPlaylist> BuildPlaylistHierarchy(string xmlPath)
        {
            var library = new ITunesLibrary(xmlPath);
            var packagePlaylists = library.Playlists.ToDictionary(p => p.PlaylistId);

            var allPlaylists = LibraryPlaylistParser.Parse(xmlPath);
            foreach (var playlist in allPlaylists)
            {
                if (packagePlaylists.TryGetValue(playlist.PlaylistId, out var packagePlaylist))
                    playlist.Playlist = packagePlaylist;
            }

            // 过滤掉主资料库（Master）和 iTunes 内置系统播放列表（Distinguished Kind）
            var userPlaylists = allPlaylists
                .Where(p => !p.IsMaster && p.DistinguishedKind == null)
                .ToList();

            var childrenByParentId = new Dictionary<string, List<LibraryPlaylist>>();
            var roots = new List<LibraryPlaylist>();
            var persistentIds = new HashSet<string>(userPlaylists.Select(p => p.PlaylistPersistentId));
            foreach (var playlist in userPlaylists)
            {
                if (playlist.ParentPersistentId != null && persistentIds.Contains(playlist.ParentPersistentId))
                {
                    if (!childrenByParentId.TryGetValue(playlist.ParentPersistentId, out var children))
                    {
                        children = new List<LibraryPlaylist>();
                        childrenByParentId[playlist.ParentPersistentId] = children;
                    }
                    children.Add(playlist);
                }
                else
                {
                    roots.Add(playlist);
                }
            }

            var flattened = new List<LibraryPlaylist>(userPlaylists.Count);
            foreach (var root in roots)
            {
                FlattenNode(root, 0, childrenByParentId, flattened);
            }
            return flattened;
        }

        /// <summary>
        /// 深度优先展开节点，子节点紧跟父节点之后；
        /// 同时自底向上统计曲目数——文件夹显示其下所有列表曲目去重后的总数。
        /// </summary>
        private static HashSet<int> FlattenNode(LibraryPlaylist node, int indentLevel,
            Dictionary<string, List<LibraryPlaylist>> childrenByParentId, List<LibraryPlaylist> flattened)
        {
            node.IndentLevel = indentLevel;
            flattened.Add(node);

            var trackIds = new HashSet<int>(
                (node.Playlist?.Tracks ?? Enumerable.Empty<Track>()).Select(t => t.TrackId));
            if (childrenByParentId.TryGetValue(node.PlaylistPersistentId, out var children))
            {
                foreach (var child in children)
                {
                    trackIds.UnionWith(FlattenNode(child, indentLevel + 1, childrenByParentId, flattened));
                }
            }
            node.DisplayTrackCount = trackIds.Count;
            return trackIds;
        }

        private void PopulatePlaylistListView(List<LibraryPlaylist> playlists)
        {
            _displayedPlaylists.Clear();
            _displayedPlaylists.AddRange(playlists);

            listViewPlaylists.BeginUpdate();
            try
            {
                listViewPlaylists.Items.Clear();
                foreach (var playlist in playlists)
                {
                    var item = new ListViewItem(new string(' ', playlist.IndentLevel * 2) + playlist.Name);
                    item.SubItems.Add(playlist.DisplayTrackCount.ToString());
                    item.Tag = playlist;
                    listViewPlaylists.Items.Add(item);
                }

                // 恢复偏好文件中记录的勾选状态，仅应用一次
                if (_pendingCheckedIds != null)
                {
                    foreach (ListViewItem item in listViewPlaylists.Items)
                    {
                        if (item.Tag is LibraryPlaylist playlist &&
                            _pendingCheckedIds.Contains(playlist.PlaylistPersistentId))
                            item.Checked = true;
                    }
                    _pendingCheckedIds = null;
                }
            }
            finally
            {
                listViewPlaylists.EndUpdate();
            }
        }
    }
}
