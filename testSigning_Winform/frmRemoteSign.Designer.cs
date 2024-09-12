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
            this.btnKyToKhai = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.panelToKhai = new System.Windows.Forms.FlowLayoutPanel();
            this.btnChonFolder = new System.Windows.Forms.Button();
            this.txtFolder = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.lblTrangThai = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtDocId = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblTimeLeft = new System.Windows.Forms.Label();
            this.btnThoat = new System.Windows.Forms.Button();
            this.btnLayKetQua = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnKyToKhai);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.panelToKhai);
            this.panel1.Controls.Add(this.btnChonFolder);
            this.panel1.Controls.Add(this.txtFolder);
            this.panel1.Location = new System.Drawing.Point(8, 60);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(448, 286);
            this.panel1.TabIndex = 0;
            // 
            // btnKyToKhai
            // 
            this.btnKyToKhai.Location = new System.Drawing.Point(331, 21);
            this.btnKyToKhai.Margin = new System.Windows.Forms.Padding(2);
            this.btnKyToKhai.Name = "btnKyToKhai";
            this.btnKyToKhai.Size = new System.Drawing.Size(73, 23);
            this.btnKyToKhai.TabIndex = 12;
            this.btnKyToKhai.Text = "Ký tờ khai";
            this.btnKyToKhai.UseVisualStyleBackColor = true;
            this.btnKyToKhai.Click += new System.EventHandler(this.btnKyToKhai_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(163, 244);
            this.button2.Margin = new System.Windows.Forms.Padding(2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(102, 27);
            this.button2.TabIndex = 11;
            this.button2.Text = "Tạo và ký hồ sơ";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // panelToKhai
            // 
            this.panelToKhai.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelToKhai.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.panelToKhai.Location = new System.Drawing.Point(15, 51);
            this.panelToKhai.Margin = new System.Windows.Forms.Padding(2);
            this.panelToKhai.Name = "panelToKhai";
            this.panelToKhai.Padding = new System.Windows.Forms.Padding(5);
            this.panelToKhai.Size = new System.Drawing.Size(419, 180);
            this.panelToKhai.TabIndex = 10;
            // 
            // btnChonFolder
            // 
            this.btnChonFolder.Location = new System.Drawing.Point(258, 21);
            this.btnChonFolder.Margin = new System.Windows.Forms.Padding(2);
            this.btnChonFolder.Name = "btnChonFolder";
            this.btnChonFolder.Size = new System.Drawing.Size(69, 23);
            this.btnChonFolder.TabIndex = 9;
            this.btnChonFolder.Text = "Chọn";
            this.btnChonFolder.UseVisualStyleBackColor = true;
            this.btnChonFolder.Click += new System.EventHandler(this.btnChonFolder_Click);
            // 
            // txtFolder
            // 
            this.txtFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFolder.Location = new System.Drawing.Point(15, 21);
            this.txtFolder.Margin = new System.Windows.Forms.Padding(2);
            this.txtFolder.Name = "txtFolder";
            this.txtFolder.Size = new System.Drawing.Size(240, 23);
            this.txtFolder.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.lblTrangThai);
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.txtDocId);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.lblTimeLeft);
            this.panel2.Controls.Add(this.btnThoat);
            this.panel2.Controls.Add(this.btnLayKetQua);
            this.panel2.Location = new System.Drawing.Point(460, 60);
            this.panel2.Margin = new System.Windows.Forms.Padding(2);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(252, 286);
            this.panel2.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 127);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(212, 20);
            this.label1.TabIndex = 8;
            this.label1.Text = "Thời gian còn lại để xác nhận";
            // 
            // lblTrangThai
            // 
            this.lblTrangThai.AutoSize = true;
            this.lblTrangThai.BackColor = System.Drawing.Color.PaleGreen;
            this.lblTrangThai.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTrangThai.Location = new System.Drawing.Point(16, 57);
            this.lblTrangThai.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblTrangThai.Name = "lblTrangThai";
            this.lblTrangThai.Size = new System.Drawing.Size(243, 17);
            this.lblTrangThai.TabIndex = 7;
            this.lblTrangThai.Text = "Trạng thái hồ sơ trên server SmartCa";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(16, 19);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(127, 20);
            this.label5.TabIndex = 6;
            this.label5.Text = "Trạng thái hồ sơ:";
            // 
            // txtDocId
            // 
            this.txtDocId.AutoSize = true;
            this.txtDocId.BackColor = System.Drawing.Color.PaleGreen;
            this.txtDocId.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDocId.Location = new System.Drawing.Point(161, 21);
            this.txtDocId.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.txtDocId.Name = "txtDocId";
            this.txtDocId.Size = new System.Drawing.Size(46, 17);
            this.txtDocId.TabIndex = 5;
            this.txtDocId.Text = "doc id";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.LightGray;
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.Location = new System.Drawing.Point(17, 218);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(236, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Hết thời gian file sẽ bị xóa trên server SMARTCA";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.LightGray;
            this.label2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label2.Location = new System.Drawing.Point(34, 205);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.label2.Size = new System.Drawing.Size(198, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "File chỉ có thời hạn tồn tại trong 300 giây";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTimeLeft
            // 
            this.lblTimeLeft.AutoSize = true;
            this.lblTimeLeft.BackColor = System.Drawing.Color.ForestGreen;
            this.lblTimeLeft.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTimeLeft.ForeColor = System.Drawing.Color.White;
            this.lblTimeLeft.Location = new System.Drawing.Point(95, 162);
            this.lblTimeLeft.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblTimeLeft.Name = "lblTimeLeft";
            this.lblTimeLeft.Size = new System.Drawing.Size(59, 31);
            this.lblTimeLeft.TabIndex = 2;
            this.lblTimeLeft.Text = "300";
            // 
            // btnThoat
            // 
            this.btnThoat.Location = new System.Drawing.Point(127, 244);
            this.btnThoat.Margin = new System.Windows.Forms.Padding(2);
            this.btnThoat.Name = "btnThoat";
            this.btnThoat.Size = new System.Drawing.Size(80, 27);
            this.btnThoat.TabIndex = 1;
            this.btnThoat.Text = "Thoát";
            this.btnThoat.UseVisualStyleBackColor = true;
            // 
            // btnLayKetQua
            // 
            this.btnLayKetQua.Location = new System.Drawing.Point(49, 244);
            this.btnLayKetQua.Margin = new System.Windows.Forms.Padding(2);
            this.btnLayKetQua.Name = "btnLayKetQua";
            this.btnLayKetQua.Size = new System.Drawing.Size(73, 27);
            this.btnLayKetQua.TabIndex = 0;
            this.btnLayKetQua.Text = "Lấy kết quả";
            this.btnLayKetQua.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(8, 21);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(116, 20);
            this.label4.TabIndex = 9;
            this.label4.Text = "Ký Demo hồ sơ";
            // 
            // frmRemoteSign
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(720, 373);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "frmRemoteSign";
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

        public System.Windows.Forms.Panel panel1;
        public System.Windows.Forms.Panel panel2;
        public System.Windows.Forms.Label lblTimeLeft;
        public System.Windows.Forms.Button btnThoat;
        public System.Windows.Forms.Button btnLayKetQua;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.Label label3;
        public System.Windows.Forms.Label label5;
        public System.Windows.Forms.Label txtDocId;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Label lblTrangThai;
        public System.Windows.Forms.Label label4;
        public System.Windows.Forms.Button btnChonFolder;
        public System.Windows.Forms.TextBox txtFolder;
        public System.Windows.Forms.Button button2;
        public System.Windows.Forms.FlowLayoutPanel panelToKhai;
        public System.Windows.Forms.Button btnKyToKhai;
    }
}

