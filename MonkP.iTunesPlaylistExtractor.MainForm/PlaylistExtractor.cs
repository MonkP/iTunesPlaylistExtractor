using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ITunesLibraryParser;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// 一个被勾选待提取的播放列表节点：含 m3u 文件名与其曲目列表（保持列表原有顺序）。
    /// 文件夹节点的曲目为其全部子孙列表曲目去重后的合集。
    /// </summary>
    internal class ExtractPlaylist
    {
        public string Name { get; set; }

        public string M3uFileName { get; set; }

        public List<Track> Tracks { get; set; } = new List<Track>();
    }

    /// <summary>提取结果统计，用于状态栏与弹窗的汇总信息。</summary>
    internal class ExtractResult
    {
        /// <summary>勾选的播放列表（含文件夹）数量。</summary>
        public int SelectedPlaylists { get; set; }

        /// <summary>去重后待复制的曲目文件总数。</summary>
        public int TotalTracks { get; set; }

        /// <summary>成功复制的曲目文件数。</summary>
        public int CopiedTracks { get; set; }

        /// <summary>被跳过的曲目数（无本地文件路径或不在根目录下）。</summary>
        public int SkippedTracks { get; set; }

        /// <summary>复制失败的曲目文件数。</summary>
        public int FailedCopies { get; set; }

        /// <summary>成功创建的 m3u 文件数。</summary>
        public int CreatedM3u { get; set; }

        /// <summary>创建失败的 m3u 文件数。</summary>
        public int FailedM3u { get; set; }

        /// <summary>提取后预期存在的曲目文件相对路径（相对目标路径），用于扫描多余文件。</summary>
        public List<string> ExpectedTrackRelPaths { get; } = new List<string>();

        /// <summary>过程中是否出现过需要查看日志的问题。</summary>
        public bool HasIssues => SkippedTracks > 0 || FailedCopies > 0 || FailedM3u > 0;

        /// <summary>构建汇总文本，状态栏与弹窗显示一致的内容。</summary>
        public string BuildSummary()
        {
            var summary = $"Done: selected {SelectedPlaylists} playlists / {TotalTracks} tracks; " +
                $"copied {CopiedTracks} tracks, created {CreatedM3u} m3u files.";
            if (SkippedTracks > 0)
                summary += $" Skipped {SkippedTracks} tracks.";
            if (FailedCopies > 0)
                summary += $" {FailedCopies} copy failures.";
            if (FailedM3u > 0)
                summary += $" {FailedM3u} m3u failures.";
            return summary;
        }
    }

    /// <summary>
    /// 提取引擎（功能 6、7）：将勾选播放列表中的音乐文件按相对根目录的原始路径结构复制到目标路径，
    /// 并在目标路径生成 .m3u 播放列表文件（条目为相对路径）。
    /// 先汇总去重全部待复制文件再执行复制；m3u 文件始终覆盖，
    /// 音乐文件是否覆盖由 overwriteFiles 决定；
    /// 单个文件复制异常不中止过程，仅记录日志。
    /// </summary>
    internal static class PlaylistExtractor
    {
        /// <summary>父级路径名压缩时每一级保留的最大字符数。</summary>
        private const int MaxParentNameLength = 6;

        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// 执行提取：汇总去重待复制文件 → 逐个复制（带进度）→ 逐个生成 m3u（带进度）。
        /// 在后台线程调用，进度通过 <paramref name="progress"/> 回报到 UI 线程。
        /// </summary>
        /// <param name="overwriteFiles">true 时覆盖目标已存在的音乐文件；false 时保留已有文件不再复制。m3u 文件始终覆盖。</param>
        internal static ExtractResult Extract(string rootPath, string targetPath,
            IReadOnlyList<ExtractPlaylist> playlists, bool overwriteFiles, IProgress<string> progress)
        {
            var result = new ExtractResult { SelectedPlaylists = playlists.Count };
            LogHelper.WriteLog(LogHelper.LogLevel.Info,
                $"Extract started: root \"{rootPath}\", target \"{targetPath}\", " +
                $"{playlists.Count} playlists selected, overwrite files: {overwriteFiles}.");
            Directory.CreateDirectory(targetPath);

            var rootFull = Path.GetFullPath(rootPath);
            if (!rootFull.EndsWith(Path.DirectorySeparatorChar.ToString()))
                rootFull += Path.DirectorySeparatorChar;

            // 汇总所有待复制文件并按相对路径去重；trackRelPaths 记录每个曲目对应的相对路径（供 m3u 使用）
            var copyPlan = new List<KeyValuePair<string, string>>();
            var plannedSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var trackRelPaths = new Dictionary<int, string>();
            var processedTrackIds = new HashSet<int>();
            foreach (var playlist in playlists)
            {
                foreach (var track in playlist.Tracks)
                {
                    // 同一曲目在多个列表中重复出现时只解析一次，保证跳过计数不重复
                    if (!processedTrackIds.Add(track.TrackId))
                        continue;

                    var sourcePath = ResolveSourcePath(track);
                    if (sourcePath == null)
                    {
                        result.SkippedTracks++;
                        continue;
                    }
                    if (!sourcePath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                    {
                        result.SkippedTracks++;
                        LogHelper.WriteLog(LogHelper.LogLevel.Warn,
                            $"Track {track.TrackId} \"{track.Name}\" is outside the root path, skipped: {sourcePath}");
                        continue;
                    }

                    var relPath = sourcePath.Substring(rootFull.Length);
                    if (plannedSources.TryGetValue(relPath, out var existingSource))
                    {
                        if (!string.Equals(existingSource, sourcePath, StringComparison.OrdinalIgnoreCase))
                        {
                            LogHelper.WriteLog(LogHelper.LogLevel.Warn,
                                $"Relative path conflict \"{relPath}\": keeping \"{existingSource}\", ignoring \"{sourcePath}\".");
                        }
                    }
                    else
                    {
                        plannedSources[relPath] = sourcePath;
                        copyPlan.Add(new KeyValuePair<string, string>(relPath, sourcePath));
                    }
                    // m3u 条目统一使用正斜杠分隔，便于跨平台播放器识别
                    trackRelPaths[track.TrackId] = relPath.Replace('\\', '/');
                }
            }
            result.TotalTracks = copyPlan.Count;
            result.ExpectedTrackRelPaths.AddRange(plannedSources.Keys);

            // 逐个复制，异常不中止，仅记录日志
            for (var i = 0; i < copyPlan.Count; i++)
            {
                var file = copyPlan[i];
                progress?.Report($"Copying ({i + 1}/{copyPlan.Count})");
                var destPath = Path.Combine(targetPath, file.Key);
                try
                {
                    if (!overwriteFiles && File.Exists(destPath))
                    {
                        // 未启用覆盖：目标文件已存在则直接保留，视为目标中已就绪
                        result.CopiedTracks++;
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    File.Copy(file.Value, destPath, true);
                    result.CopiedTracks++;
                }
                catch (Exception ex)
                {
                    result.FailedCopies++;
                    LogHelper.WriteLog(LogHelper.LogLevel.Error,
                        $"Failed to copy \"{file.Value}\" to \"{destPath}\".", ex);
                }
            }

            // 逐个生成 m3u 文件
            var usedM3uNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < playlists.Count; i++)
            {
                var playlist = playlists[i];
                progress?.Report($"Creating m3u ({i + 1}/{playlists.Count})");
                if (!usedM3uNames.Add(playlist.M3uFileName))
                {
                    LogHelper.WriteLog(LogHelper.LogLevel.Warn,
                        $"Duplicate m3u name \"{playlist.M3uFileName}\", the earlier one will be overwritten.");
                }
                var m3uPath = Path.Combine(targetPath, playlist.M3uFileName);
                try
                {
                    var itemCount = 0;
                    // m3u 使用不带 BOM 的 UTF-8，兼容大多数播放器
                    using (var writer = new StreamWriter(m3uPath, false, new UTF8Encoding(false)))
                    {
                        writer.WriteLine("#EXTM3U");
                        foreach (var track in playlist.Tracks)
                        {
                            // 解析失败被跳过的曲目不会出现在 m3u 中
                            if (trackRelPaths.TryGetValue(track.TrackId, out var relPath))
                            {
                                writer.WriteLine(relPath);
                                itemCount++;
                            }
                        }
                    }
                    result.CreatedM3u++;
                    LogHelper.WriteLog(LogHelper.LogLevel.Info, $"Created m3u \"{m3uPath}\" with {itemCount} items.");
                }
                catch (Exception ex)
                {
                    result.FailedM3u++;
                    LogHelper.WriteLog(LogHelper.LogLevel.Error, $"Failed to create m3u \"{m3uPath}\".", ex);
                }
            }

            var summary = result.BuildSummary();
            LogHelper.WriteLog(result.HasIssues ? LogHelper.LogLevel.Warn : LogHelper.LogLevel.Info,
                $"Extract finished. {summary}");
            return result;
        }

        /// <summary>
        /// 扫描目标路径下不属于本次提取结果的文件：
        /// 未被本次勾选列表包含的曲目文件，以及本次未生成的 m3u 播放列表。
        /// 返回多余文件的全路径列表；目标路径不存在时返回空列表。
        /// </summary>
        /// <param name="expectedRelPaths">本次提取的曲目文件相对路径（分隔符不限，大小写不敏感）。</param>
        /// <param name="expectedM3uNames">本次生成的 m3u 文件名（位于目标路径根下）。</param>
        internal static List<string> FindExtraFiles(string targetPath,
            IReadOnlyCollection<string> expectedRelPaths, IReadOnlyCollection<string> expectedM3uNames)
        {
            if (!Directory.Exists(targetPath))
                return new List<string>();

            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var relPath in expectedRelPaths ?? Enumerable.Empty<string>())
                expected.Add(NormalizeRelPath(relPath));
            foreach (var m3uName in expectedM3uNames ?? Enumerable.Empty<string>())
                expected.Add(m3uName);

            var targetFull = Path.GetFullPath(targetPath);
            if (!targetFull.EndsWith(Path.DirectorySeparatorChar.ToString()))
                targetFull += Path.DirectorySeparatorChar;

            var extras = new List<string>();
            foreach (var file in Directory.EnumerateFiles(targetPath, "*", SearchOption.AllDirectories))
            {
                if (!expected.Contains(NormalizeRelPath(file.Substring(targetFull.Length))))
                    extras.Add(file);
            }
            return extras;
        }

        /// <summary>
        /// 逐个删除文件，单个文件删除异常不中止，仅记录日志；返回成功删除的数量。
        /// </summary>
        internal static int DeleteFiles(IReadOnlyList<string> filePaths)
        {
            var deleted = 0;
            foreach (var file in filePaths)
            {
                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLog(LogHelper.LogLevel.Error, $"Failed to delete \"{file}\".", ex);
                }
            }
            return deleted;
        }

        /// <summary>将相对路径统一为反斜杠形式，便于大小写不敏感比较。</summary>
        private static string NormalizeRelPath(string relPath) => relPath.Replace('/', '\\');

        /// <summary>
        /// 按“各父级文件夹到当前节点”的全路径构建 m3u 文件名：
        /// 各级之间用“-”分隔；父级名称压缩为最多前 6 个字符，当前节点保留全名；
        /// 文件路径非法字符替换为“_”。
        /// </summary>
        internal static string BuildM3uFileName(IReadOnlyList<string> pathNames)
        {
            var segments = new List<string>(pathNames.Count);
            for (var i = 0; i < pathNames.Count; i++)
            {
                var name = pathNames[i] ?? string.Empty;
                if (i < pathNames.Count - 1 && name.Length > MaxParentNameLength)
                    name = name.Substring(0, MaxParentNameLength);
                segments.Add(SanitizeFileName(name));
            }
            return string.Join("-", segments) + ".m3u";
        }

        /// <summary>将文件名中的非法字符替换为“_”。</summary>
        private static string SanitizeFileName(string name)
        {
            var chars = new char[name.Length];
            for (var i = 0; i < name.Length; i++)
                chars[i] = Array.IndexOf(InvalidFileNameChars, name[i]) >= 0 ? '_' : name[i];
            return new string(chars);
        }

        /// <summary>
        /// 将 Track 的 Location（file:// URL）解析为本地绝对路径；
        /// 无法解析（无 Location、非本地文件等）时记录日志并返回 null。
        /// </summary>
        private static string ResolveSourcePath(Track track)
        {
            if (string.IsNullOrEmpty(track.Location))
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Warn,
                    $"Track {track.TrackId} \"{track.Name}\" has no location, skipped.");
                return null;
            }
            if (!Uri.TryCreate(track.Location, UriKind.Absolute, out var uri) || !uri.IsFile)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Warn,
                    $"Track {track.TrackId} \"{track.Name}\" location is not a local file, skipped: {track.Location}");
                return null;
            }
            try
            {
                return Path.GetFullPath(GetLocalFilePath(uri));
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Warn,
                    $"Track {track.TrackId} \"{track.Name}\" has an invalid location, skipped: {track.Location}", ex);
                return null;
            }
        }

        /// <summary>
        /// 从 file:// URL 取得本地文件路径。
        /// iTunes 导出的 URL 带 localhost 主机名，部分系统上 <see cref="Uri.LocalPath"/> 会返回
        /// 形如 \\localhost\F:\… 的 UNC 形式（前缀导致 Path.GetFullPath 报错），
        /// 因此对 localhost 或空主机名的 URL 改为解码 AbsolutePath 还原盘符路径；
        /// 其他主机名（网络共享）仍使用 LocalPath 的结果。
        /// </summary>
        private static string GetLocalFilePath(Uri uri)
        {
            if (string.IsNullOrEmpty(uri.Host) ||
                uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                // AbsolutePath 形如 "/F:/音乐/xxx.mp3"（仍为转义形式），解码并去掉开头的斜杠即为盘符路径
                return Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
            }
            return uri.LocalPath;
        }
    }
}
