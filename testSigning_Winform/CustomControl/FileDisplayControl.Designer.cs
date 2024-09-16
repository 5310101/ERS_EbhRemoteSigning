namespace testSigning_Winform.CustomControl
{
    partial class FileDisplayControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTrangThai = new System.Windows.Forms.Label();
            this.txtFileName = new System.Windows.Forms.TextBox();
            this.lblFileTime = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblTrangThai
            // 
            this.lblTrangThai.AutoSize = true;
            this.lblTrangThai.BackColor = System.Drawing.Color.Chartreuse;
            this.lblTrangThai.Location = new System.Drawing.Point(384, 11);
            this.lblTrangThai.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblTrangThai.Name = "lblTrangThai";
            this.lblTrangThai.Size = new System.Drawing.Size(75, 20);
            this.lblTrangThai.TabIndex = 1;
            this.lblTrangThai.Text = "StatusTK";
            // 
            // txtFileName
            // 
            this.txtFileName.Location = new System.Drawing.Point(3, 6);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.Size = new System.Drawing.Size(366, 26);
            this.txtFileName.TabIndex = 2;
            // 
            // lblFileTime
            // 
            this.lblFileTime.AutoSize = true;
            this.lblFileTime.BackColor = System.Drawing.Color.Orange;
            this.lblFileTime.Location = new System.Drawing.Point(469, 11);
            this.lblFileTime.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblFileTime.Name = "lblFileTime";
            this.lblFileTime.Size = new System.Drawing.Size(36, 20);
            this.lblFileTime.TabIndex = 3;
            this.lblFileTime.Text = "300";
            // 
            // FileDisplayControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblFileTime);
            this.Controls.Add(this.txtFileName);
            this.Controls.Add(this.lblTrangThai);
            this.Name = "FileDisplayControl";
            this.Size = new System.Drawing.Size(534, 42);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTrangThai;
        private System.Windows.Forms.TextBox txtFileName;
        private System.Windows.Forms.Label lblFileTime;
    }
}
