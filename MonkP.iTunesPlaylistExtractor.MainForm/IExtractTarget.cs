using System.Collections.Generic;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// 提取的目标存储抽象：本地目录或经 MTP 连接的安卓设备路径。
    /// 方法参数中的相对路径均相对目标根路径，分隔符为反斜杠。
    /// </summary>
    internal interface IExtractTarget
    {
        /// <summary>目标的可读描述（本地目录绝对路径或“设备: 路径”），用于日志。</summary>
        string Description { get; }

        /// <summary>判断目标中是否已存在指定相对路径的文件。</summary>
        bool ExistsFile(string relPath);

        /// <summary>
        /// 将本地文件写入目标。
        /// overwriteFiles 为 true 时覆盖已有文件；为 false 时跳过已存在的文件。
        /// 设备目标在跳过前会比较文件大小，大小不同视为需要更新而执行上传。
        /// </summary>
        void CopyLocalFile(string relPath, string sourcePath, bool overwriteFiles);

        /// <summary>将文本内容（m3u）写入目标，始终覆盖已有文件。</summary>
        void WriteText(string relPath, string content);

        /// <summary>递归枚举目标中全部文件的相对路径（分隔符统一为反斜杠）。</summary>
        IEnumerable<string> EnumerateRelativeFiles();

        /// <summary>删除目标中指定相对路径的文件。</summary>
        void DeleteFile(string relPath);
    }
}
