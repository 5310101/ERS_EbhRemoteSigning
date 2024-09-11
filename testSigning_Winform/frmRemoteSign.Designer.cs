namespace testSigning_Winform
{
    partial class frmRemoteSign
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnLayKetQua = new System.Windows.Forms.Button();
            this.btnThoat = new System.Windows.Forms.Button();
            this.lblTimeLeft = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDocId = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblTrangThai = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtFolder = new System.Windows.Forms.TextBox();
            this.btnChonFolder = new System.Windows.Forms.Button();
            this.panelToKhai = new System.Windows.Forms.FlowLayoutPanel();
            this.button2 = new System.Windows.Forms.Button();
            this.btnKyToKhai = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnKyToKhai);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.panelToKhai);
            this.panel1.Controls.Add(this.btnChonFolder);
            this.panel1.Controls.Add(this.txtFolder);
            this.panel1.Location = new System.Drawing.Point(12, 93);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(672, 440);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.lblTrangThai);
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.txtDocId);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.lblTimeLeft);
            this.panel2.Controls.Add(this.btnThoat);
            this.panel2.Controls.Add(this.btnLayKetQua);
            this.panel2.Location = new System.Drawing.Point(690, 93);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(378, 440);
            this.panel2.TabIndex = 1;
            // 
            // btnLayKetQua
            // 
            this.btnLayKetQua.Location = new System.Drawing.Point(80, 375);
            this.btnLayKetQua.Name = "btnLayKetQua";
            this.btnLayKetQua.Size = new System.Drawing.Size(104, 41);
            this.btnLayKetQua.TabIndex = 0;
            this.btnLayKetQua.Text = "Lấy kết quả";
            this.btnLayKetQua.UseVisualStyleBackColor = true;
            // 
            // btnThoat
            // 
            this.btnThoat.Location = new System.Drawing.Point(190, 375);
            this.btnThoat.Name = "btnThoat";
            this.btnThoat.Size = new System.Drawing.Size(104, 41);
            this.btnThoat.TabIndex = 1;
            this.btnThoat.Text = "Thoát";
            this.btnThoat.UseVisualStyleBackColor = true;
            // 
            // lblTimeLeft
            // 
            this.lblTimeLeft.AutoSize = true;
            this.lblTimeLeft.BackColor = System.Drawing.Color.ForestGreen;
            this.lblTimeLeft.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTimeLeft.ForeColor = System.Drawing.Color.White;
            this.lblTimeLeft.Location = new System.Drawing.Point(142, 249);
            this.lblTimeLeft.Name = "lblTimeLeft";
            this.lblTimeLeft.Size = new System.Drawing.Size(86, 46);
            this.lblTimeLeft.TabIndex = 2;
            this.lblTimeLeft.Text = "300";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.LightGray;
            this.label2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label2.Location = new System.Drawing.Point(51, 315);
            this.label2.Name = "label2";
            this.label2.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.label2.Size = new System.Drawing.Size(292, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "File chỉ có thời hạn tồn tại trong 300 giây";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.LightGray;
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.Location = new System.Drawing.Point(25, 335);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(350, 20);
            this.label3.TabIndex = 4;
            this.label3.Text = "Hết thời gian file sẽ bị xóa trên server SMARTCA";
            // 
            // txtDocId
            // 
            this.txtDocId.AutoSize = true;
            this.txtDocId.BackColor = System.Drawing.Color.PaleGreen;
            this.txtDocId.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDocId.Location = new System.Drawing.Point(242, 32);
            this.txtDocId.Name = "txtDocId";
            this.txtDocId.Size = new System.Drawing.Size(64, 25);
            this.txtDocId.TabIndex = 5;
            this.txtDocId.Text = "doc id";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(24, 29);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(192, 29);
            this.label5.TabIndex = 6;
            this.label5.Text = "Trạng thái hồ sơ:";
            // 
            // lblTrangThai
            // 
            this.lblTrangThai.AutoSize = true;
            this.lblTrangThai.BackColor = System.Drawing.Color.PaleGreen;
            this.lblTrangThai.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTrangThai.Location = new System.Drawing.Point(24, 87);
            this.lblTrangThai.Name = "lblTrangThai";
            this.lblTrangThai.Size = new System.Drawing.Size(333, 25);
            this.lblTrangThai.TabIndex = 7;
            this.lblTrangThai.Text = "Trạng thái hồ sơ trên server SmartCa";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(24, 196);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(324, 29);
            this.label1.TabIndex = 8;
            this.label1.Text = "Thời gian còn lại để xác nhận";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(12, 32);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(176, 29);
            this.label4.TabIndex = 9;
            this.label4.Text = "Ký Demo hồ sơ";
            // 
            // txtFolder
            // 
            this.txtFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFolder.Location = new System.Drawing.Point(22, 32);
            this.txtFolder.Name = "txtFolder";
            this.txtFolder.Size = new System.Drawing.Size(358, 30);
            this.txtFolder.TabIndex = 0;
            // 
            // btnChonFolder
            // 
            this.btnChonFolder.Location = new System.Drawing.Point(386, 32);
            this.btnChonFolder.Name = "btnChonFolder";
            this.btnChonFolder.Size = new System.Drawing.Size(104, 30);
            this.btnChonFolder.TabIndex = 9;
            this.btnChonFolder.Text = "Chọn";
            this.btnChonFolder.UseVisualStyleBackColor = true;
            this.btnChonFolder.Click += new System.EventHandler(this.btnChonFolder_Click);
            // 
            // panelToKhai
            // 
            this.panelToKhai.Location = new System.Drawing.Point(22, 78);
            this.panelToKhai.Name = "panelToKhai";
            this.panelToKhai.Size = new System.Drawing.Size(628, 277);
            this.panelToKhai.TabIndex = 10;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(244, 375);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(153, 41);
            this.button2.TabIndex = 11;
            this.button2.Text = "Tạo và ký hồ sơ";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // btnKyToKhai
            // 
            this.btnKyToKhai.Location = new System.Drawing.Point(496, 32);
            this.btnKyToKhai.Name = "btnKyToKhai";
            this.btnKyToKhai.Size = new System.Drawing.Size(109, 30);
            this.btnKyToKhai.TabIndex = 12;
            this.btnKyToKhai.Text = "Ký tờ khai";
            this.btnKyToKhai.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1080, 556);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Remote Signing SMART CA";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lblTimeLeft;
        private System.Windows.Forms.Button btnThoat;
        private System.Windows.Forms.Button btnLayKetQua;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label txtDocId;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblTrangThai;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnChonFolder;
        private System.Windows.Forms.TextBox txtFolder;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.FlowLayoutPanel panelToKhai;
        private System.Windows.Forms.Button btnKyToKhai;
    }
}

