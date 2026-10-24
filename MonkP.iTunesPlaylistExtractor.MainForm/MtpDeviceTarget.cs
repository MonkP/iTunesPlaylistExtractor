using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediaDevices;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// 通过 MTP 连接的安卓设备目标：基于 MediaDevices（WPD）实现与本地目录相同的同步语义。
    /// 设备路径形如“内部共享存储空间\Music”。
    /// 覆盖语义：MTP 不支持原地覆盖，执行“删除旧对象后重新上传”；
    /// 智能同步：未启用覆盖时，已存在且大小相同的文件直接跳过。
    /// </summary>
    internal sealed class MtpDeviceTarget : IExtractTarget, IDisposable
    {
        private readonly MediaDevice _device;
        private readonly string _basePath;
        private readonly string _basePathWithSeparator;

        private MtpDeviceTarget(MediaDevice device, string basePath)
        {
            _device = device;
            _basePath = basePath;
            _basePathWithSeparator = basePath + "\\";
        }

        /// <summary>
        /// 按 DeviceId 定位并连接设备，校验基础路径存在后返回目标实例；失败返回 null。
        /// </summary>
        internal static MtpDeviceTarget Connect(string deviceId, string basePath)
        {
            MediaDevice device = null;
            try
            {
                device = MediaDevice.GetDevices().FirstOrDefault(d => d.DeviceId == deviceId);
                if (device == null)
                {
                    LogHelper.WriteLog(LogHelper.LogLevel.Warn, $"MTP device not found: {deviceId}");
                    return null;
                }
                device.Connect();
                if (!device.DirectoryExists(basePath))
                {
                    LogHelper.WriteLog(LogHelper.LogLevel.Warn,
                        $"Path \"{basePath}\" does not exist on device \"{device.FriendlyName}\".");
                    device.Disconnect();
                    return null;
                }
                return new MtpDeviceTarget(device, basePath);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Warn, "Failed to connect the MTP device.", ex);
                device?.Dispose();
                return null;
            }
        }

        public string Description => $"{_device.FriendlyName}: {_basePath}";

        public bool ExistsFile(string relPath)
        {
            try
            {
                return _device.FileExists(GetFullPath(relPath));
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Warn, $"Failed to check \"{relPath}\" on device.", ex);
                return false;
            }
        }

        public void CopyLocalFile(string relPath, string sourcePath, bool overwriteFiles)
        {
            var fullPath = GetFullPath(relPath);
            var existing = TryGetFileInfo(fullPath);
            if (existing != null)
            {
                if (!overwriteFiles && existing.Length == (ulong)new FileInfo(sourcePath).Length)
                    return;
                // MTP 不支持原地覆盖：删除旧对象后重新上传
                _device.DeleteFile(fullPath);
            }
            EnsureDirectories(relPath);
            using (var stream = File.OpenRead(sourcePath))
            {
                _device.UploadFile(stream, fullPath);
            }
        }

        public void WriteText(string relPath, string content)
        {
            var fullPath = GetFullPath(relPath);
            // m3u 始终覆盖：删除旧对象后重新上传
            if (_device.FileExists(fullPath))
                _device.DeleteFile(fullPath);
            EnsureDirectories(relPath);
            // m3u 使用不带 BOM 的 UTF-8，兼容大多数播放器
            using (var stream = new MemoryStream(new UTF8Encoding(false).GetBytes(content)))
            {
                _device.UploadFile(stream, fullPath);
            }
        }

        public IEnumerable<string> EnumerateRelativeFiles()
        {
            foreach (var file in _device.EnumerateFiles(_basePath, "*", SearchOption.AllDirectories))
            {
                var rel = file.Replace('/', '\\');
                if (rel.StartsWith(_basePathWithSeparator, StringComparison.OrdinalIgnoreCase))
                    rel = rel.Substring(_basePathWithSeparator.Length);
                yield return rel;
            }
        }

        public void DeleteFile(string relPath) => _device.DeleteFile(GetFullPath(relPath));

        public void Dispose()
        {
            try
            {
                _device.Dispose();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Warn, "Failed to dispose the MTP device.", ex);
            }
        }

        private string GetFullPath(string relPath) => _basePath + "\\" + relPath;

        private MediaFileInfo TryGetFileInfo(string fullPath)
        {
            try
            {
                // GetFileInfo 对不存在的对象会抛异常，此处以异常判定不存在；
                // 注意不能依赖返回对象上的存在性属性（本库未提供）
                return _device.GetFileInfo(fullPath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>按文件的相对路径逐级创建缺失的父目录（WPD 要求自父向子逐级创建）。</summary>
        private void EnsureDirectories(string relPath)
        {
            var dir = relPath.Contains("\\")
                ? relPath.Substring(0, relPath.LastIndexOf('\\'))
                : null;
            if (string.IsNullOrEmpty(dir))
                return;

            // 自底向上找出第一个不存在的目录，再自该目录起逐级向下创建
            var missing = new List<string>();
            var current = dir;
            while (!string.IsNullOrEmpty(current) && !_device.DirectoryExists(_basePath + "\\" + current))
            {
                missing.Add(current);
                var index = current.LastIndexOf('\\');
                current = index < 0 ? null : current.Substring(0, index);
            }
            for (var i = missing.Count - 1; i >= 0; i--)
                _device.CreateDirectory(_basePath + "\\" + missing[i]);
        }
    }
}
