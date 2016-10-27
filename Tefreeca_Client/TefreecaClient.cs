using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Tefreeca_Structure;
using System.Threading;

namespace Tefreeca_Client
{
    public partial class TefreecaClient : Form
    {
        private ui_control UI;
        Tefreeca_client_interface tai;
        private State_Order Old_State = State_Order.Close;
        private int Max_Connect = 50;       //계정이 할수있는 최대의 연결
        private int Set_MaxConnect = 0;     //현재 계정에서 선택한 최대연결.

        public TefreecaClient()
        {
            InitializeComponent();

            panel_login.Location = new Point(4, 3);
            this.Size = new Size(336, 318);
            UI = new ui_control(this);

            Tefreeca_client_interface.Tefreeca_client_event += Eventer_Tefreeca_client_event;
            Thread SST = new Thread(new ThreadStart(Start_server_Thread));
            SST.Start();
        }

        private void Start_server_Thread()
        {
            Thread.Sleep(500);
            // StartServer(Data_structure.hostName, Data_structure.hostPort);
            StartServer(Data_structure.is_debug ? Data_structure.hostName_Debug : Data_structure.hostName, Data_structure.hostPort);
        }

        private void StartServer(String hostName, UInt16 hostPort)
        {
            tai = new Tefreeca_client_interface();
            while (true)
            {
                Console.WriteLine("try connecting...");
                UI.set_listBox_delegate(listBox_state_log, new string[] { "서버 연결중.." }, Option_listBox.add);
                tai.ConnectToServer(hostName, hostPort);
                if (!tai.is_connected())
                {
                    Console.WriteLine("Failure, Re Connecting..");
                    UI.set_listBox_delegate(listBox_state_log, new string[] { "서버 연결 실패." }, Option_listBox.add);
                    continue;
                }
                else
                {
                    Console.WriteLine("Connected!");
                    UI.set_listBox_delegate(listBox_state_log, new string[] { "서버 연결 성공." }, Option_listBox.add);
                    break;
                }
            }
        }

        private void btn_Login_Click(object sender, EventArgs e)
        {
            btn_Login.Enabled = false;
            Tefreeca_client_interface.Add_Command(Command_Client.Make_Login, new i_Make_Login(tbx_ID.Text, tbx_PW.Text)); //로그인 시도.
            UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.로그인_시도중.ToString().Replace("_", " ") }, Option_listBox.add);
            Console.WriteLine("Login..");
        }

        public void Eventer_Tefreeca_client_event(object sender, EventArgs e)
        {
            try
            {
                Tefreeca_client_EventArgs RecieveEventArg = e as Tefreeca_client_EventArgs;
                if (RecieveEventArg == null)
                    return;
                Unit_Event_Type type = RecieveEventArg.type;

                switch (type)
                {
                    case Unit_Event_Type.Change_connect: //연결 상태가 변하면,
                        {
                            bool connected = (bool)RecieveEventArg.Data_object_list[0];
                            UI.set_button_delegate(btn_Login, connected, Option_button.enable);
                            if (connected)
                            {
                                UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.로그인_준비_완료.ToString().Replace("_"," ")}, Option_listBox.add);
                                if(Data_structure.is_debug)
                                    UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.디버깅_모드.ToString().Replace("_", " ") }, Option_listBox.add);
                            }
                        }
                        break;
                    case Unit_Event_Type.Login_success:
                        {
                            UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.로그인_성공.ToString().Replace("_", " ") }, Option_listBox.add);

                            //판낼 정보를 업데이트가 먼저 되어야 한다.                            
                            

                            UI.set_PanelVisible_delegate(panel_login, false);
                            UI.set_button_delegate(btn_Login, true, Option_button.enable);
                        }
                        break;
                    case Unit_Event_Type.Login_Failure:// 로그인 실패시
                        {
                            //Login_Failure
                            string s = "";
                            i_Login_Failure_type Failure_info = (i_Login_Failure_type)RecieveEventArg.Data_object_list[0];
                            if (Failure_info == i_Login_Failure_type.Duplicate_Login)
                                s = "중복로그인 불가.";
                            else if (Failure_info == i_Login_Failure_type.Invalid_login_information)
                                s = "잘못된 로그인 정보";

                            UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.로그인_실패.ToString().Replace("_", " ") + " : " + s }, Option_listBox.add);
                            UI.set_button_delegate(btn_Login, true, Option_button.enable);
                        }
                        break;
                    case Unit_Event_Type.User_info:
                        {
                            i_User_info User_info = (i_User_info)RecieveEventArg.Data_object_list[0];
                            TimeSpan remind = User_info.Expire - DateTime.Now;

                            Max_Connect = User_info.slot; //최대 연결 가능 슬롯을 세팅해준다.
                            UI.NumericUpDowndelegate(numericUpDown_MaxConnect, Max_Connect, Option_NumericUpDown.Maximum); //최대치로 세팅
                            UI.NumericUpDowndelegate(numericUpDown_MaxConnect, Max_Connect, Option_NumericUpDown.value);   //최대치로 세팅
                            UI.TrackBardelegate(trackBar_order_maxconnect, 50, Option_TrackBar.value); //맥시멈으로..
                            Set_MaxConnect = Max_Connect;
                            UI.set_label_delegate(lb_userName, User_info.Id + "님");
                            UI.set_label_delegate(lb_expire, "expire : " + User_info.Expire.ToShortDateString() + " (" + ((int)remind.TotalDays).ToString() + " days remained.)");
                            UI.set_label_delegate(lb_usergrade, User_info.type.ToString() + " 등급");
                        }
                        break;
                    case Unit_Event_Type.Order_no:
                        {
                            //int order_no = RecieveEventArg.data_int;
                            UI.set_button_delegate(btn_connect, "해제", Option_button.text);
                            //UI.set_listBox_delegate(listBox_state_log, new string[] { "접속 명령 생성." }, Option_listBox.add);
                            UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.접속_명령_생성.ToString().Replace("_", " ") }, Option_listBox.add);
                        }
                        break;
                    case Unit_Event_Type.Error_Order:
                        {
                            client_message error_info = (client_message)RecieveEventArg.Data_object_list[0];
                            UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.오류.ToString().Replace("_", " ") + " : " + error_info.ToString().Replace("_", " ") }, Option_listBox.add);
                            //UI.set_listBox_delegate(listBox_state_log, new string[] { "[오류] : " + error_info }, Option_listBox.add);
                            UI.set_TextBox_delegate(tbx_BJID, true, Option_TextBox.enable);
                            UI.set_button_delegate(btn_connect, "접속", Option_button.text);
                            UI.set_button_delegate(btn_connect, true, Option_button.enable);
                        }
                        break;
                    case Unit_Event_Type.Active_Order: //주문이 활성화 되면 이것을 취소할 수 있게 해준다.
                        {
                            //int order_no = RecieveEventArg.data_int;
                            mode = btn_mode.disconnect;
                            UI.set_button_delegate(btn_connect, true, Option_button.enable);
                            UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.활성화_되었습니다.ToString().Replace("_", " ") }, Option_listBox.add);
                            //UI.set_listBox_delegate(listBox_state_log, new string[] { "활성화 되었습니다." }, Option_listBox.add);
                            //UI.set_listBox_delegate(listBox_state_log, new string[] { "[" + order_no.ToString() + "]활성화." }, Option_listBox.add);
                        }
                        break;
                    case Unit_Event_Type.Order_Info:
                        {
                            i_Order_Info Order_info = (i_Order_Info)RecieveEventArg.Data_object_list[0];

                            string State = Order_info.State.ToString().Replace("_", " ");

                            UI.set_label_delegate(lb_connect_state, State);
                            /*
                            if (Order_info.State == State_Order.Close || Order_info.State == State_Order.Closing) //클로징, 클로즈땐 카운트 표시 안함.
                                UI.set_label_delegate(lb_connect_state, State);
                            else
                                UI.set_label_delegate(lb_connect_state, State + " (" + Order_info.Connect_Count + ")");
                                */
                            if (Old_State == State_Order.Active && Order_info.State == State_Order.Closing) //활성화 되었다가 클로징으로 바뀌면.
                            {
                                //UI.set_listBox_delegate(listBox_state_log, new string[] { "연결 해제중.." }, Option_listBox.add);
                                UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.연결_해제중.ToString().Replace("_", " ") }, Option_listBox.add);
                                UI.set_TextBox_delegate(tbx_BJID, true, Option_TextBox.enable);
                            }

                            if (Order_info.State == State_Order.Close) //활성화 되었다가 클로징으로 바뀌면.
                            {
                                mode = btn_mode.connect;
                                UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.연결_해제_성공.ToString().Replace("_", " ") }, Option_listBox.add);
                                //UI.set_listBox_delegate(listBox_state_log, new string[] { "연결 해제 성공." }, Option_listBox.add);
                                UI.set_TextBox_delegate(tbx_BJID, true, Option_TextBox.enable);
                                UI.set_button_delegate(btn_connect, "접속", Option_button.text);
                                UI.set_button_delegate(btn_connect, true, Option_button.enable);
                            }
                            Old_State = Order_info.State;
                        }
                        break;
                    case Unit_Event_Type.Server_Connection_Broken: //서버와 연결이 끊김.
                        {
                            //UI.set_listBox_delegate(listBox_state_log, new string[] { "서버와의 접속이 끊어졌습니다." }, Option_listBox.add);
                            UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.서버와_접속이_끊어졌습니다.ToString().Replace("_", " ") }, Option_listBox.add);
                        }
                        break;
                    case Unit_Event_Type.Order_Set_MaxConnect: //최대 연결 수가 변경 수락되면..
                        {
                            i_R_Order_Set_MaxConnect ir = (i_R_Order_Set_MaxConnect)RecieveEventArg.Data_object_list[0];

                            if (ir.message == client_message.방송_접속수가_변경되었습니다)
                            {
                                UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.방송_접속수가_변경되었습니다.ToString().Replace("_", " ") + "[" + ir.Max_Connect + "]" }, Option_listBox.add);

                                Set_MaxConnect = ir.Max_Connect;

                                if (int.Parse(numericUpDown_MaxConnect.Value.ToString()) == Set_MaxConnect)
                                {
                                    UI.set_button_delegate(btn_set_Maxconnect, false, Option_button.enable);
                                }
                                else
                                {
                                    UI.set_button_delegate(btn_set_Maxconnect, true, Option_button.enable);
                                }
                            }
                            else if (ir.message == client_message.방송_접속수_변경중_오류)
                            {
                                UI.set_listBox_delegate(listBox_state_log, new string[] { client_message.방송_접속수_변경중_오류.ToString().Replace("_", " ") }, Option_listBox.add);
                            }
                            if (int.Parse(numericUpDown_MaxConnect.Value.ToString()) == Set_MaxConnect)
                            {
                                UI.set_button_delegate(btn_set_Maxconnect, false, Option_button.enable);
                            }
                            else
                            {
                                UI.set_button_delegate(btn_set_Maxconnect, true, Option_button.enable);
                            }

                            //UI.set_listBox_delegate(listBox_state_log, new string[] { "서버와의 접속이 끊어졌습니다." }, Option_listBox.add);

                        }
                        break;
                    default:
                        { }
                        break;
                }
           }
           catch (Exception ex)
           {
                Console.WriteLine("Error : {0}", ex.ToString());
                throw;
           }
        }
        private enum btn_mode { connect, disconnect};
        private btn_mode mode = btn_mode.connect;
        private void btn_connect_Click(object sender, EventArgs e)
        {
            if (mode == btn_mode.connect)
            {
                //주소길이 검사,
                if (tbx_BJID.Text.Trim().Length < 3)
                {
                    UI.set_listBox_delegate(listBox_state_log, new string[] { "BJ ID가 너무 짧습니다."}, Option_listBox.add);
                    return;
                }
                i_Make_Connect make_connect = new i_Make_Connect("http://play.afreecatv.com/" + tbx_BJID.Text.Trim(), Set_MaxConnect);
                
                Tefreeca_client_interface.Add_Command(Command_Client.Make_Connect, make_connect); //연결을 만든다.
                tbx_BJID.Enabled = false;
            }
            else if(mode == btn_mode.disconnect)
            {
                UI.set_button_delegate(btn_connect, false, Option_button.enable); //버튼 비활성화.
                Tefreeca_client_interface.Add_Command(Command_Client.Close_Order, null); 
            }
        }

        private void tbx_PW_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btn_Login_Click(sender, e);
            }
        }

        private void TefreecaClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            Tefreeca_client_interface.Server_Disconnect();
            Application.Exit();
        }

        
        private void trackBar_order_maxconnect_Scroll(object sender, EventArgs e)
        {
            TrackBar T = (TrackBar)sender;
            numericUpDown_MaxConnect.Value = Max_Connect * T.Value / 50;
        }

        private void btn_set_Maxconnect_Click(object sender, EventArgs e)
        {
            Tefreeca_client_interface.Add_Command(Command_Client.Make_Order_Set_MaxConnect, int.Parse(numericUpDown_MaxConnect.Value.ToString()));
            UI.set_listBox_delegate(listBox_state_log, new string[] { "최대 연결 수 변경 요청" }, Option_listBox.add);
        }

        private void numericUpDown_MaxConnect_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown N = (NumericUpDown)sender;
            if (N.Value > Max_Connect)
                N.Value = Max_Connect;

            if (int.Parse(numericUpDown_MaxConnect.Value.ToString()) == Set_MaxConnect)
            {
                UI.set_button_delegate(btn_set_Maxconnect, false, Option_button.enable);
            }
            else
            {
                UI.set_button_delegate(btn_set_Maxconnect, true, Option_button.enable);
            }
            //trackBar_order_maxconnect.Value = int.Parse(N.Value.ToString()) / Max_Connect * 50;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //trackBar_order_maxconnect.Value = 44;
        }
    }
}
