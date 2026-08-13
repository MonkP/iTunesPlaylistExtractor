namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.txtLibraryFilePath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnChooseLibFile = new System.Windows.Forms.Button();
            this.btnChooseRootPath = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtRootPath = new System.Windows.Forms.TextBox();
            this.btnChooseTargetPath = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtTargetBox = new System.Windows.Forms.TextBox();
            this.btnExtract = new System.Windows.Forms.Button();
            this.listViewPlaylists = new System.Windows.Forms.ListView();
            this.lblStatus = new System.Windows.Forms.Label();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // txtLibraryFilePath
            // 
            this.txtLibraryFilePath.AcceptsReturn = true;
            this.txtLibraryFilePath.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtLibraryFilePath.Location = new System.Drawing.Point(117, 12);
            this.txtLibraryFilePath.Name = "txtLibraryFilePath";
            this.txtLibraryFilePath.Size = new System.Drawing.Size(646, 23);
            this.txtLibraryFilePath.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Library XML file";
            // 
            // btnChooseLibFile
            // 
            this.btnChooseLibFile.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnChooseLibFile.Location = new System.Drawing.Point(766, 12);
            this.btnChooseLibFile.Margin = new System.Windows.Forms.Padding(0);
            this.btnChooseLibFile.Name = "btnChooseLibFile";
            this.btnChooseLibFile.Size = new System.Drawing.Size(25, 23);
            this.btnChooseLibFile.TabIndex = 2;
            this.btnChooseLibFile.Text = "...";
            this.btnChooseLibFile.UseVisualStyleBackColor = true;
            this.btnChooseLibFile.Click += new System.EventHandler(this.btnChooseLibFile_Click);
            // 
            // btnChooseRootPath
            // 
            this.btnChooseRootPath.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnChooseRootPath.Location = new System.Drawing.Point(766, 41);
            this.btnChooseRootPath.Margin = new System.Windows.Forms.Padding(0);
            this.btnChooseRootPath.Name = "btnChooseRootPath";
            this.btnChooseRootPath.Size = new System.Drawing.Size(25, 23);
            this.btnChooseRootPath.TabIndex = 5;
            this.btnChooseRootPath.Text = "...";
            this.btnChooseRootPath.UseVisualStyleBackColor = true;
            this.btnChooseRootPath.Click += new System.EventHandler(this.btnChooseRootPath_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(12, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "Root Path";
            // 
            // txtRootPath
            // 
            this.txtRootPath.AcceptsReturn = true;
            this.txtRootPath.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtRootPath.Location = new System.Drawing.Point(117, 41);
            this.txtRootPath.Name = "txtRootPath";
            this.txtRootPath.Size = new System.Drawing.Size(646, 23);
            this.txtRootPath.TabIndex = 3;
            // 
            // btnChooseTargetPath
            // 
            this.btnChooseTargetPath.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnChooseTargetPath.Location = new System.Drawing.Point(659, 591);
            this.btnChooseTargetPath.Margin = new System.Windows.Forms.Padding(0);
            this.btnChooseTargetPath.Name = "btnChooseTargetPath";
            this.btnChooseTargetPath.Size = new System.Drawing.Size(25, 23);
            this.btnChooseTargetPath.TabIndex = 8;
            this.btnChooseTargetPath.Text = "...";
            this.btnChooseTargetPath.UseVisualStyleBackColor = true;
            this.btnChooseTargetPath.Click += new System.EventHandler(this.btnChooseTargetPath_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(12, 594);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 17);
            this.label3.TabIndex = 7;
            this.label3.Text = "Target Path";
            // 
            // txtTargetBox
            // 
            this.txtTargetBox.AcceptsReturn = true;
            this.txtTargetBox.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtTargetBox.Location = new System.Drawing.Point(117, 591);
            this.txtTargetBox.Name = "txtTargetBox";
            this.txtTargetBox.Size = new System.Drawing.Size(539, 23);
            this.txtTargetBox.TabIndex = 6;
            // 
            // btnExtract
            // 
            this.btnExtract.Location = new System.Drawing.Point(716, 591);
            this.btnExtract.Name = "btnExtract";
            this.btnExtract.Size = new System.Drawing.Size(75, 23);
            this.btnExtract.TabIndex = 9;
            this.btnExtract.Text = "Extract";
            this.btnExtract.UseVisualStyleBackColor = true;
            // 
            // listViewPlaylists
            // 
            this.listViewPlaylists.CheckBoxes = true;
            this.listViewPlaylists.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colCount});
            this.listViewPlaylists.HideSelection = false;
            this.listViewPlaylists.Location = new System.Drawing.Point(15, 70);
            this.listViewPlaylists.Name = "listViewPlaylists";
            this.listViewPlaylists.Size = new System.Drawing.Size(776, 475);
            this.listViewPlaylists.TabIndex = 10;
            this.listViewPlaylists.UseCompatibleStateImageBehavior = false;
            this.listViewPlaylists.View = System.Windows.Forms.View.Details;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(13, 548);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(47, 12);
            this.lblStatus.TabIndex = 11;
            this.lblStatus.Text = "Standby";
            // 
            // colName
            // 
            this.colName.Text = "Playlist Name";
            this.colName.Width = 600;
            // 
            // colCount
            // 
            this.colCount.Text = "Item Count";
            this.colCount.Width = 150;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 626);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.listViewPlaylists);
            this.Controls.Add(this.btnExtract);
            this.Controls.Add(this.btnChooseTargetPath);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtTargetBox);
            this.Controls.Add(this.btnChooseRootPath);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtRootPath);
            this.Controls.Add(this.btnChooseLibFile);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtLibraryFilePath);
            this.Name = "Form1";
            this.Text = "iTunes Playlist Extractor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLibraryFilePath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnChooseLibFile;
        private System.Windows.Forms.Button btnChooseRootPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtRootPath;
        private System.Windows.Forms.Button btnChooseTargetPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtTargetBox;
        private System.Windows.Forms.Button btnExtract;
        private System.Windows.Forms.ListView listViewPlaylists;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colCount;
    }
}

