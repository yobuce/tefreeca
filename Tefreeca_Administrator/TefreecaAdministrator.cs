using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tefreeca_Structure;

namespace Tefreeca_Administrator
{
    public partial class TefreecaAdministrator : Form
    {
        private ui_control UI;
        Tefreeca_Admin_interface tai;
        public TefreecaAdministrator()
        {
            InitializeComponent();
            UI = new ui_control(this);
            StartServer(Data_structure.is_debug ? Data_structure.hostName_Debug : Data_structure.hostName, Data_structure.hostPort);
        }
        
        private void StartServer(String hostName, UInt16 hostPort)
        {
            tai = new Tefreeca_Admin_interface();
            while (true)
            {
                Console.WriteLine("접속 중...");

                tai.ConnectToServer(hostName, hostPort);
                if (!tai.is_connected())
                {
                    Console.WriteLine("접속 실패, 재시도합니다.");
                    continue;
                }
                else
                {
                    UI.set_label_delegate(lb_ip, hostName + " : " + hostPort.ToString());
                    Tefreeca_Admin_interface.Tefreeca_client_event += Eventer_Tefreeca_client_event;
                    break;
                }
            }
        }
        
        public void Eventer_Tefreeca_client_event(object sender, EventArgs e)
        {
            //try
            //{
                Tefreeca_client_EventArgs RecieveEventArg = e as Tefreeca_client_EventArgs;
                if (RecieveEventArg == null)
                    return;
                Unit_Event_Type type = RecieveEventArg.type;

                switch (type)
                {
                    case Unit_Event_Type.List_Account:
                        {
                            List<i_List_Account> DR = (List<i_List_Account>)(RecieveEventArg.Data_object_list[0]);
                            List<string[]> L = new List<string[]>();

                            foreach (i_List_Account R in DR)
                            {
                                L.Add(new String[] {
                                    R.No.ToString(),
                                    R.Type_order_Site.ToString(),
                                    R.User_No.ToString(),
                                    R.ID.ToString(),
                                    R.PW.ToString(),
                                    R.State.ToString()
                                    });
                            }
                            UI.set_Autolistview_delegate(listView_Account, L, Option_Audo_listview.Auto_display);
                        }
                        break;
                    case Unit_Event_Type.List_Connect:
                        {
                            List<i_List_Connect> DR = (List<i_List_Connect>)(RecieveEventArg.Data_object_list[0]);
                            List<string[]> L = new List<string[]>();

                            foreach (i_List_Connect R in DR)
                            {
                                L.Add(new String[] {
                                    R.No.ToString(),
                                    R.Order_No.ToString(),
                                    R.Unit_No.ToString(),
                                    R.Connect_URL.Replace("http://play.afreecatv.com/","").ToString(),
                                    R.Proxy_host.ToString(),
                                    R.Type_order.ToString(),
                                    R.State.ToString(),
                                    R.Login_Account_no == -1 ? "" : R.Login_Account_no.ToString(),
                                    R.State_Account_on_connected.ToString(),
                                    R.Wait_count.ToString()
                                    });
                            }
                            UI.set_Autolistview_delegate(listView_Connection, L, Option_Audo_listview.Auto_display);
                        }
                        break;
                    case Unit_Event_Type.List_Order:
                        {
                            List<i_List_Order> DR = (List<i_List_Order>)(RecieveEventArg.Data_object_list[0]);
                            List<string[]> L = new List<string[]>();

                            foreach (i_List_Order R in DR)
                            {
                                L.Add(
                                new String[] {
                                    R.No.ToString(),
                                    R.User_No.ToString(),
                                    R.Connected_count.ToString(),
                                    R.Max_connect.ToString(),
                                    R.State.ToString(),
                                    R.URL.ToString(),
                                    R.Type_order.ToString(),
                                });
                            }
                            UI.set_Autolistview_delegate(listView_Order, L, Option_Audo_listview.Auto_display);
                        }
                        break;
                    case Unit_Event_Type.List_Proxy:
                        {
                            List<i_List_Proxy> DR = (List<i_List_Proxy>)(RecieveEventArg.Data_object_list[0]);
                            List<string[]> L = new List<string[]>();
                            
                            foreach (i_List_Proxy R in DR)
                            {
                                 L.Add(
                                 new String[] {
                                    R.No.ToString(),
                                    R.Host.ToString(),
                                    R.Error_count.ToString(),
                                    R.State.ToString(),
                                    R.Type.ToString(),
                                });
                            }
                            UI.set_Autolistview_delegate(listView_ProxyOnServer, L, Option_Audo_listview.Auto_display);
                        }
                        break;
                    case Unit_Event_Type.List_Unit:
                        {
                            List<i_List_Unit> DR = (List<i_List_Unit>)(RecieveEventArg.Data_object_list[0]);
                            List<string[]> L = new List<string[]>();

                            foreach (i_List_Unit R in DR)
                            {
                                L.Add(
                                new String[] {
                                    R.No.ToString(),
                                    R.Connected_count.ToString(),
                                    R.Max_connect.ToString(),
                                    R.Proxy.ToString(),
                                    R.State.ToString(),
                                    R.Wait_count.ToString()
                                });
                            }
                            UI.set_Autolistview_delegate(listView_Unit, L, Option_Audo_listview.Auto_display);
                        }
                        break;
                    case Unit_Event_Type.List_User:
                        {
                            List<i_List_User> DR = (List<i_List_User>)(RecieveEventArg.Data_object_list[0]);
                            List<string[]> L = new List<string[]>();
                            foreach (i_List_User R in DR)
                            {
                                L.Add(
                                new String[] {
                                    R.No.ToString(),
                                    R.Unit_No.ToString(),
                                    R.id.ToString(),
                                    R.Slot.ToString(),
                                    R.Slot_Account.ToString(),
                                    R.pay_type.ToString(),
                                    R.expire.ToString(),
                                    R.User_type.ToString(),
                                    R.is_Reseller.ToString(),
                                    R.my_Reseller_No.ToString()
                                });
                            }
                            UI.set_Autolistview_delegate(listView_LoginUser, L, Option_Audo_listview.Auto_display);
                        }
                        break;
                    case Unit_Event_Type.List_ServerInfo:
                        {
                            i_List_ServerInfo DR = (i_List_ServerInfo)(RecieveEventArg.Data_object_list[0]);

                            set_label_delegate(lb_mqc, DR.Command_Q_length.ToString());
                        }
                        break;
                    default:
                        { }
                        break;
                }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Error : {0}", ex.ToString());
            //    throw;
            //}
        }

        delegate void SetLabelCallback(Label target, string value);
        public void set_label_delegate(Label target, string value)
        {
            if (target.InvokeRequired)
            {
                SetLabelCallback d = new SetLabelCallback(set_label_delegate);
                this.Invoke(d, new object[] { target, value });
            }
            else
            {
                target.Text = value;
                target.Visible = true;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            Tefreeca_Admin_interface.Add_Response(Command_Server.Update_Proxy_DB, null);
        }

        private void Tab_menu_SelectedIndexChanged(object sender, EventArgs e) //탭 메뉴가 바뀔때 마다 그부분을 저장한다.
        {
            Tefreeca_Admin_interface.Add_Command(Command_Client.Updated_current_View_page, ((TabControl)sender).SelectedIndex);
        }

        private void btn_Account_Click(object sender, EventArgs e)
        {
            Tefreeca_Admin_interface.Add_Response(Command_Server.Update_Account_DB, null);
        }

        private void btn_TEST_Click(object sender, EventArgs e)
        {
            List<string[]> L = new List<string[]>();
            L.Add(new String[] { "6", "35", "2", "ffwefwef", "112.21.342.211", "old", "wait", "2", "wait", "0" });
            UI.set_Autolistview_delegate(listView_Connection, L, Option_Audo_listview.Auto_display);
        }
    }
}
