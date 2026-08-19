using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ITunesLibraryParser;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// 应用程序主窗体，负责 iTunes 资料库的加载、播放列表展示、偏好设置保存与提取操作的入口。
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>按显示顺序展开后的播放列表（含文件夹），Tag 中保存 <see cref="LibraryPlaylist"/>。</summary>
        private readonly List<LibraryPlaylist> _displayedPlaylists = new List<LibraryPlaylist>();

        /// <summary>标记当前是否正在异步加载资料库，防止重复触发。</summary>
        private bool _isLoading;

        /// <summary>从偏好文件恢复的勾选列表 Persistent ID，在播放列表加载完成后应用一次。</summary>
        private HashSet<string> _pendingCheckedIds;

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 窗体加载时调用：从偏好文件恢复上次的路径、日志级别、覆盖开关及勾选状态；
        /// 若资料库 XML 文件存在则自动触发加载。
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            // 读取持久化偏好（路径、日志级别、勾选 ID 等）
            var preferences = PreferenceStore.Load();
            if (preferences == null)
                return;

            // 将偏好填充到对应文本框
            txtLibraryFilePath.Text = preferences.LibraryFilePath ?? string.Empty;
            txtRootPath.Text = preferences.RootPath ?? string.Empty;
            txtTargetBox.Text = preferences.TargetPath ?? string.Empty;
            ckbOverWrite.Checked = preferences.OverwriteFiles;
            if (Enum.TryParse(preferences.LogLevel, true, out LogHelper.LogLevel logLevel))
                LogHelper.DefaultLevel = logLevel;

            // 暂存勾选 ID，待播放列表加载完成后统一恢复
            _pendingCheckedIds = new HashSet<string>(
                preferences.CheckedPlaylistPersistentIds ?? Enumerable.Empty<string>());

            // 若资料库文件有效则自动加载
            if (File.Exists(txtLibraryFilePath.Text))
                LoadLibraryAsync();
        }

        /// <summary>
        /// 弹出文件选择对话框，让用户选择 iTunes Library XML 文件，选择后自动加载资料库。
        /// </summary>
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

        /// <summary>
        /// 弹出文件夹选择对话框，让用户指定音乐库的根目录。
        /// </summary>
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

        /// <summary>
        /// 弹出文件夹选择对话框，让用户指定播放列表提取的目标路径。
        /// </summary>
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

        /// <summary>
        /// 「Extract」按钮点击处理：先保存偏好，然后校验前置条件，
        /// 在后台线程执行提取（复制曲目文件、生成 m3u），
        /// 然后询问是否删除目标路径中不属于本次提取结果的文件；
        /// 完成后在状态栏和弹窗显示汇总；若过程有错误，关闭弹窗后自动打开当日日志文件。
        /// </summary>
        private async void btnExtract_Click(object sender, EventArgs e)
        {
            SavePreferences();

            // 校验前置条件
            if (listViewPlaylists.Items.Count == 0)
            {
                MessageBox.Show(this, "Please load the iTunes library first.", "Extract",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var playlists = CollectCheckedPlaylists();
            if (playlists.Count == 0)
            {
                MessageBox.Show(this, "Please check at least one playlist to extract.", "Extract",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var rootPath = txtRootPath.Text?.Trim();
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                MessageBox.Show(this, "The root path of the music library is invalid.", "Extract",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var targetPath = txtTargetBox.Text?.Trim();
            if (string.IsNullOrEmpty(targetPath))
            {
                MessageBox.Show(this, "Please choose a target path.", "Extract",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnExtract.Enabled = false;
            // Progress 在 UI 线程创建，自动将进度文本封送回 UI 线程
            var progress = new Progress<string>(status => lblStatus.Text = status);
            try
            {
                var result = await Task.Run(() => PlaylistExtractor.Extract(
                    rootPath, targetPath, playlists, ckbOverWrite.Checked, progress));
                var summary = result.BuildSummary();

                // 无论是否启用覆盖，都检查目标路径中不属于本次提取结果的文件
                // （本次未勾选列表的 m3u，或勾选列表未包含的曲目文件）
                var extraFiles = PlaylistExtractor.FindExtraFiles(targetPath,
                    result.ExpectedTrackRelPaths,
                    playlists.Select(p => p.M3uFileName).ToList());
                if (extraFiles.Count > 0)
                {
                    var choice = MessageBox.Show(this,
                        "Target Path contains files that not included in playlists choosen this time, would you like to delete them?",
                        "Extract", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (choice == DialogResult.Yes)
                    {
                        var deleted = PlaylistExtractor.DeleteFiles(extraFiles);
                        LogHelper.WriteLog(LogHelper.LogLevel.Info,
                            $"Deleted {deleted} extra files from \"{targetPath}\".");
                        summary += $" Deleted {deleted} extra files.";
                    }
                }

                lblStatus.Text = summary;
                MessageBox.Show(this, summary, "Extract", MessageBoxButtons.OK,
                    result.HasIssues ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                // 过程有错误时，用户关闭弹窗后自动打开当日日志文件
                if (result.HasIssues)
                {
                    // 先确保错误日志已落盘再打开
                    LogHelper.Flush(TimeSpan.FromSeconds(5));
                    var logFile = LogHelper.GetLogFile(DateTime.Now);
                    if (File.Exists(logFile))
                        Process.Start(logFile);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Extract failed.";
                LogHelper.WriteLog(LogHelper.LogLevel.Error, "Extract failed.", ex);
                MessageBox.Show(this, $"Extract failed:{Environment.NewLine}{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExtract.Enabled = true;
            }
        }

        /// <summary>
        /// 收集勾选的待提取播放列表：普通列表取自身曲目；
        /// 文件夹取全部子孙列表曲目按 Track ID 去重后的合集；
        /// m3u 文件名由祖先文件夹路径加当前节点名构建。
        /// </summary>
        private List<ExtractPlaylist> CollectCheckedPlaylists()
        {
            var items = listViewPlaylists.Items.Cast<ListViewItem>().ToList();
            var selected = new List<ExtractPlaylist>();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.Checked || !(item.Tag is LibraryPlaylist node))
                    continue;

                var tracks = new List<Track>();
                if (node.IsFolder)
                {
                    // 平铺列表中，文件夹的子孙紧跟其后且缩进更深，借此区间收集去重
                    var seenTrackIds = new HashSet<int>();
                    for (var j = i + 1; j < items.Count; j++)
                    {
                        var child = (LibraryPlaylist)items[j].Tag;
                        if (child.IndentLevel <= node.IndentLevel)
                            break;
                        foreach (var track in child.Playlist?.Tracks ?? Enumerable.Empty<Track>())
                        {
                            if (seenTrackIds.Add(track.TrackId))
                                tracks.Add(track);
                        }
                    }
                }
                else
                {
                    tracks.AddRange(node.Playlist?.Tracks ?? Enumerable.Empty<Track>());
                }

                selected.Add(new ExtractPlaylist
                {
                    Name = node.Name,
                    M3uFileName = PlaylistExtractor.BuildM3uFileName(GetAncestorPath(items, i)),
                    Tracks = tracks,
                });
            }
            return selected;
        }

        /// <summary>取节点的完整路径名称（从顶层文件夹到自身），用于构建 m3u 文件名。</summary>
        private static List<string> GetAncestorPath(List<ListViewItem> items, int index)
        {
            var node = (LibraryPlaylist)items[index].Tag;
            var path = new List<string> { node.Name };
            var indent = node.IndentLevel;
            for (var j = index - 1; j >= 0 && indent > 0; j--)
            {
                var previous = (LibraryPlaylist)items[j].Tag;
                if (previous.IndentLevel == indent - 1)
                {
                    path.Insert(0, previous.Name);
                    indent--;
                }
            }
            return path;
        }

        /// <summary>
        /// 将窗体当前选择的路径、覆盖开关与勾选的播放列表 Persistent ID 写入运行目录下的 preference.json。
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
                    OverwriteFiles = ckbOverWrite.Checked,
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

        /// <summary>
        /// 异步加载 iTunes 资料库：在后台线程解析 XML 并构建播放列表层级，
        /// 完成后回到 UI 线程刷新列表视图与状态栏。
        /// 通过 <see cref="_isLoading"/> 标志防止并发重复加载。
        /// </summary>
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
                // 在后台线程执行耗时的 XML 解析
                var playlists = await Task.Run(() => BuildPlaylistHierarchy(xmlPath));
                // 回到 UI 线程更新列表视图
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

        /// <summary>
        /// 将播放列表数据填充到 ListView，并根据缩进层级显示树形结构；
        /// 若有待恢复的勾选状态（来自偏好文件），则在此处一次性应用后清空。
        /// </summary>
        /// <param name="playlists">按显示顺序排列的播放列表（已通过 Flatten 展开）。</param>
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
                    // 通过前导空格数模拟缩进，每个层级 2 个空格
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
