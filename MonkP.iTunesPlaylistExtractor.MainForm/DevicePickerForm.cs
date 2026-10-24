using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MediaDevices;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    /// <summary>
    /// MTP 设备路径选择对话框：左侧列出已连接的便携设备，右侧以树形懒加载方式浏览设备目录。
    /// 确认后通过 SelectedDeviceId / SelectedPath 返回选择结果。
    /// </summary>
    internal sealed class DevicePickerForm : Form
    {
        private const string PlaceholderKey = "__placeholder__";

        private ListView _deviceList;
        private TreeView _folderTree;
        private Button _btnOk;
        private Label _lblHint;

        /// <summary>所选设备的 DeviceId（未确认时为 null）。</summary>
        public string SelectedDeviceId { get; private set; }

        /// <summary>所选设备路径的友好字面量（形如“Pixel内部共享存储空间\Music”，仅用于显示，未确认时为 null）。</summary>
        public string SelectedPath { get; private set; }

        /// <summary>所选设备的原始设备路径（形如“内部共享存储空间\Music”，用于连接，未确认时为 null）。</summary>
        public string SelectedDevicePath { get; private set; }

        public DevicePickerForm()
        {
            InitializeComponentInternal();
            PopulateDevices();
        }

        private void InitializeComponentInternal()
        {
            Text = "Choose Path on Device";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 460);
            Font = new Font("微软雅黑", 9F);

            var lblDevices = new Label
            {
                Text = "Devices",
                Location = new Point(12, 9),
                AutoSize = true
            };

            _deviceList = new ListView
            {
                Location = new Point(12, 30),
                Size = new Size(200, 340),
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false
            };
            _deviceList.Columns.Add("Device", 180);
            _deviceList.SelectedIndexChanged += DeviceList_SelectedIndexChanged;

            var lblFolders = new Label
            {
                Text = "Folders",
                Location = new Point(224, 9),
                AutoSize = true
            };

            _folderTree = new TreeView
            {
                Location = new Point(224, 30),
                Size = new Size(404, 340),
                HideSelection = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _folderTree.BeforeExpand += FolderTree_BeforeExpand;
            _folderTree.AfterSelect += (s, e) => UpdateOkState();

            _lblHint = new Label
            {
                Text = "Select a device, then choose a folder. " +
                       "Keep the device unlocked and in file-transfer mode.",
                Location = new Point(12, 380),
                Size = new Size(616, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(88, 30),
                Location = new Point(440, 408),
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(88, 30),
                Location = new Point(540, 408),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            AcceptButton = _btnOk;
            CancelButton = btnCancel;
            Controls.Add(lblDevices);
            Controls.Add(_deviceList);
            Controls.Add(lblFolders);
            Controls.Add(_folderTree);
            Controls.Add(_lblHint);
            Controls.Add(_btnOk);
            Controls.Add(btnCancel);
        }

        private void PopulateDevices()
        {
            _deviceList.Items.Clear();
            _folderTree.Nodes.Clear();
            try
            {
                foreach (var device in MediaDevice.GetDevices())
                {
                    string deviceName = getDeviceDisplayName(device);
                    var item = new ListViewItem(deviceName)
                    {
                        Tag = device
                    };
                    _deviceList.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Warn, "Failed to enumerate MTP devices.", ex);
            }
            if (_deviceList.Items.Count == 0)
            {
                _lblHint.Text = "No portable device found. Connect your Android device, " +
                                "unlock it and set the USB mode to File Transfer (MTP).";
            }
        }

        private static string getDeviceDisplayName(MediaDevice device)
        {
            var deviceName = device.FriendlyName;
            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = $"{device.Description}({device.Manufacturer})";
            }
            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = device.DeviceId;
            }

            return deviceName;
        }

        private void DeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            _folderTree.Nodes.Clear();
            UpdateOkState();
            if (!(_deviceList.SelectedItems.Count > 0 && _deviceList.SelectedItems[0].Tag is MediaDevice device))
                return;

            try
            {
                // 每次切换设备时重连，确保设备状态为最新
                device.Disconnect();
                device.Connect();
                foreach (var drive in device.GetDrives())
                {
                    // MediaDriveInfo.Name 即存储卷根目录名称（形如“内部共享存储空间”）
                    var node = new TreeNode(drive.Name ?? "Storage")
                    {
                        Tag = new NodeState(device, drive.RootDirectory?.FullName ?? drive.Name)
                    };
                    node.Nodes.Add(new TreeNode { Name = PlaceholderKey });
                    _folderTree.Nodes.Add(node);
                }
                if (_folderTree.Nodes.Count > 0)
                    _folderTree.Nodes[0].Expand();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Warn, "Failed to enumerate device storage.", ex);
                _folderTree.Nodes.Add("(Failed to read the device: " + ex.Message + ")");
            }
        }

        private void FolderTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            if (!(node.Tag is NodeState state))
                return;

            var hasPlaceholder = node.Nodes.ContainsKey(PlaceholderKey);
            if (!hasPlaceholder)
                return;

            node.Nodes.Clear();
            try
            {
                // MediaDevice.EnumerateDirectories 返回子目录的完整设备路径，需自行取末段作为显示名
                foreach (var dirPath in state.Device.EnumerateDirectories(state.Path))
                {
                    var separatorIndex = dirPath.LastIndexOf('\\');
                    var dirName = separatorIndex >= 0 ? dirPath.Substring(separatorIndex + 1) : dirPath;
                    var child = new TreeNode(dirName)
                    {
                        Tag = new NodeState(state.Device, dirPath)
                    };
                    // 先挂占位节点，展开时再懒加载子目录
                    child.Nodes.Add(new TreeNode { Name = PlaceholderKey });
                    node.Nodes.Add(child);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Warn,
                    $"Failed to enumerate \"{state.Path}\".", ex);
            }
        }

        private void UpdateOkState()
        {
            var selected = _folderTree.SelectedNode;
            _btnOk.Enabled = selected != null && selected.Tag is NodeState;
        }

        private void ConfirmSelection()
        {
            if (_folderTree.SelectedNode?.Tag is NodeState state)
            {
                SelectedDeviceId = state.Device.DeviceId;
                SelectedDevicePath = state.Path;
                SelectedPath = getDeviceDisplayName(state.Device) + state.Path;
                DialogResult = DialogResult.OK;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 仅在用户确认时设置选择结果，关闭前统一断开浏览用的连接
            if (DialogResult == DialogResult.OK)
                ConfirmSelection();
            foreach (ListViewItem item in _deviceList.Items)
            {
                if (item.Tag is MediaDevice device)
                {
                    try
                    {
                        device.Disconnect();
                    }
                    catch
                    {
                        // 浏览连接断开失败不影响主流程
                    }
                }
            }
            base.OnFormClosing(e);
        }

        /// <summary>树节点的载荷：设备实例与该节点对应的设备路径。</summary>
        private sealed class NodeState
        {
            public MediaDevice Device { get; }
            public string Path { get; }

            public NodeState(MediaDevice device, string path)
            {
                Device = device;
                Path = path;
            }
        }
    }
}
