namespace Tefreeca_Client
{
    partial class TefreecaClient
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TefreecaClient));
            this.tbx_ID = new System.Windows.Forms.TextBox();
            this.tbx_PW = new System.Windows.Forms.TextBox();
            this.btn_Login = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel_control = new System.Windows.Forms.Panel();
            this.btn_set_Maxconnect = new System.Windows.Forms.Button();
            this.numericUpDown_MaxConnect = new System.Windows.Forms.NumericUpDown();
            this.lb_usergrade = new System.Windows.Forms.Label();
            this.btn_connect = new System.Windows.Forms.Button();
            this.trackBar_order_maxconnect = new System.Windows.Forms.TrackBar();
            this.lb_url = new System.Windows.Forms.Label();
            this.listBox_state_log = new System.Windows.Forms.ListBox();
            this.lb_expire = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lb_connect_state = new System.Windows.Forms.Label();
            this.lb_userName = new System.Windows.Forms.Label();
            this.tbx_BJID = new System.Windows.Forms.TextBox();
            this.panel_login = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel_control.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaxConnect)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_order_maxconnect)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.panel_login.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // tbx_ID
            // 
            this.tbx_ID.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.tbx_ID, "tbx_ID");
            this.tbx_ID.Name = "tbx_ID";
            // 
            // tbx_PW
            // 
            this.tbx_PW.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.tbx_PW, "tbx_PW");
            this.tbx_PW.Name = "tbx_PW";
            this.tbx_PW.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbx_PW_KeyDown);
            // 
            // btn_Login
            // 
            resources.ApplyResources(this.btn_Login, "btn_Login");
            this.btn_Login.Name = "btn_Login";
            this.btn_Login.UseVisualStyleBackColor = true;
            this.btn_Login.Click += new System.EventHandler(this.btn_Login_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // panel_control
            // 
            this.panel_control.Controls.Add(this.btn_set_Maxconnect);
            this.panel_control.Controls.Add(this.numericUpDown_MaxConnect);
            this.panel_control.Controls.Add(this.lb_usergrade);
            this.panel_control.Controls.Add(this.btn_connect);
            this.panel_control.Controls.Add(this.trackBar_order_maxconnect);
            this.panel_control.Controls.Add(this.lb_url);
            this.panel_control.Controls.Add(this.listBox_state_log);
            this.panel_control.Controls.Add(this.lb_expire);
            this.panel_control.Controls.Add(this.groupBox1);
            this.panel_control.Controls.Add(this.lb_userName);
            this.panel_control.Controls.Add(this.tbx_BJID);
            resources.ApplyResources(this.panel_control, "panel_control");
            this.panel_control.Name = "panel_control";
            // 
            // btn_set_Maxconnect
            // 
            resources.ApplyResources(this.btn_set_Maxconnect, "btn_set_Maxconnect");
            this.btn_set_Maxconnect.Name = "btn_set_Maxconnect";
            this.btn_set_Maxconnect.UseVisualStyleBackColor = true;
            this.btn_set_Maxconnect.Click += new System.EventHandler(this.btn_set_Maxconnect_Click);
            // 
            // numericUpDown_MaxConnect
            // 
            this.numericUpDown_MaxConnect.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.numericUpDown_MaxConnect, "numericUpDown_MaxConnect");
            this.numericUpDown_MaxConnect.Name = "numericUpDown_MaxConnect";
            this.numericUpDown_MaxConnect.ValueChanged += new System.EventHandler(this.numericUpDown_MaxConnect_ValueChanged);
            // 
            // lb_usergrade
            // 
            resources.ApplyResources(this.lb_usergrade, "lb_usergrade");
            this.lb_usergrade.Name = "lb_usergrade";
            // 
            // btn_connect
            // 
            this.btn_connect.BackColor = System.Drawing.SystemColors.InactiveCaption;
            resources.ApplyResources(this.btn_connect, "btn_connect");
            this.btn_connect.Name = "btn_connect";
            this.btn_connect.UseVisualStyleBackColor = false;
            this.btn_connect.Click += new System.EventHandler(this.btn_connect_Click);
            // 
            // trackBar_order_maxconnect
            // 
            resources.ApplyResources(this.trackBar_order_maxconnect, "trackBar_order_maxconnect");
            this.trackBar_order_maxconnect.Maximum = 50;
            this.trackBar_order_maxconnect.Name = "trackBar_order_maxconnect";
            this.trackBar_order_maxconnect.SmallChange = 5;
            this.trackBar_order_maxconnect.Value = 1;
            this.trackBar_order_maxconnect.Scroll += new System.EventHandler(this.trackBar_order_maxconnect_Scroll);
            // 
            // lb_url
            // 
            resources.ApplyResources(this.lb_url, "lb_url");
            this.lb_url.Name = "lb_url";
            // 
            // listBox_state_log
            // 
            this.listBox_state_log.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listBox_state_log.FormattingEnabled = true;
            resources.ApplyResources(this.listBox_state_log, "listBox_state_log");
            this.listBox_state_log.Name = "listBox_state_log";
            // 
            // lb_expire
            // 
            resources.ApplyResources(this.lb_expire, "lb_expire");
            this.lb_expire.ForeColor = System.Drawing.Color.DarkRed;
            this.lb_expire.Name = "lb_expire";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lb_connect_state);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // lb_connect_state
            // 
            resources.ApplyResources(this.lb_connect_state, "lb_connect_state");
            this.lb_connect_state.Name = "lb_connect_state";
            // 
            // lb_userName
            // 
            resources.ApplyResources(this.lb_userName, "lb_userName");
            this.lb_userName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.lb_userName.Name = "lb_userName";
            // 
            // tbx_BJID
            // 
            this.tbx_BJID.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.tbx_BJID, "tbx_BJID");
            this.tbx_BJID.Name = "tbx_BJID";
            // 
            // panel_login
            // 
            this.panel_login.Controls.Add(this.pictureBox1);
            this.panel_login.Controls.Add(this.btn_Login);
            this.panel_login.Controls.Add(this.tbx_ID);
            this.panel_login.Controls.Add(this.label2);
            this.panel_login.Controls.Add(this.tbx_PW);
            this.panel_login.Controls.Add(this.label1);
            resources.ApplyResources(this.panel_login, "panel_login");
            this.panel_login.Name = "panel_login";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Tefreeca_Client.Properties.Resources.ci2;
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // TefreecaClient
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel_login);
            this.Controls.Add(this.panel_control);
            this.Name = "TefreecaClient";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TefreecaClient_FormClosing);
            this.panel_control.ResumeLayout(false);
            this.panel_control.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaxConnect)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_order_maxconnect)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panel_login.ResumeLayout(false);
            this.panel_login.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox tbx_ID;
        private System.Windows.Forms.TextBox tbx_PW;
        private System.Windows.Forms.Button btn_Login;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel_control;
        private System.Windows.Forms.Button btn_connect;
        private System.Windows.Forms.Label lb_url;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox listBox_state_log;
        private System.Windows.Forms.Label lb_expire;
        private System.Windows.Forms.Label lb_userName;
        private System.Windows.Forms.TextBox tbx_BJID;
        private System.Windows.Forms.Label lb_connect_state;
        private System.Windows.Forms.Panel panel_login;
        private System.Windows.Forms.Label lb_usergrade;
        private System.Windows.Forms.TrackBar trackBar_order_maxconnect;
        private System.Windows.Forms.NumericUpDown numericUpDown_MaxConnect;
        private System.Windows.Forms.Button btn_set_Maxconnect;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}

