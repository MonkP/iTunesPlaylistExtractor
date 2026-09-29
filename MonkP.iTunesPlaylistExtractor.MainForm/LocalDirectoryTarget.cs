using System.Collections.Generic;
using System.IO;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// 本地目录目标：封装原有的文件系统同步逻辑，行为与 MTP 支持引入前完全一致。
    /// </summary>
    internal sealed class LocalDirectoryTarget : IExtractTarget
    {
        private readonly string _targetPath;

        public LocalDirectoryTarget(string targetPath)
        {
            _targetPath = targetPath;
            Directory.CreateDirectory(_targetPath);
        }

        public string Description => _targetPath;

        public bool ExistsFile(string relPath) => File.Exists(GetFullPath(relPath));

        public void CopyLocalFile(string relPath, string sourcePath, bool overwriteFiles)
        {
            var destPath = GetFullPath(relPath);
            if (!overwriteFiles && File.Exists(destPath))
                return;
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            File.Copy(sourcePath, destPath, true);
        }

        public void WriteText(string relPath, string content)
        {
            var destPath = GetFullPath(relPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            // m3u 使用不带 BOM 的 UTF-8，兼容大多数播放器
            File.WriteAllText(destPath, content, new System.Text.UTF8Encoding(false));
        }

        public IEnumerable<string> EnumerateRelativeFiles()
        {
            var rootFull = Path.GetFullPath(_targetPath);
            if (!rootFull.EndsWith(Path.DirectorySeparatorChar.ToString()))
                rootFull += Path.DirectorySeparatorChar;
            foreach (var file in Directory.EnumerateFiles(_targetPath, "*", SearchOption.AllDirectories))
                yield return file.Substring(rootFull.Length);
        }

        public void DeleteFile(string relPath) => File.Delete(GetFullPath(relPath));

        private string GetFullPath(string relPath) => Path.Combine(_targetPath, relPath);
    }
}
