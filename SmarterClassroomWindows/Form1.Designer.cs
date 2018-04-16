namespace SmarterClassroomWindows
{
    partial class IndexForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IndexForm));
            this.btn_Debug = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Debug
            // 
            this.btn_Debug.Location = new System.Drawing.Point(830, 12);
            this.btn_Debug.Name = "btn_Debug";
            this.btn_Debug.Size = new System.Drawing.Size(75, 23);
            this.btn_Debug.TabIndex = 0;
            this.btn_Debug.Text = "Debug";
            this.btn_Debug.UseVisualStyleBackColor = true;
            this.btn_Debug.Click += new System.EventHandler(this.btn_Debug_Click);
            // 
            // IndexForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(917, 539);
            this.Controls.Add(this.btn_Debug);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "IndexForm";
            this.Text = "智慧教室";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.IndexForm_FormClosing);
            this.Load += new System.EventHandler(this.IndexForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_Debug;
    }
}

