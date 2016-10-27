using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using Tefreeca_server;
using System.Data;
using System.Net;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using Tefreeca_Structure;

namespace Tefreeca
{
    class Program
    {
        #region Layer [자료구조]
        private static AsyncCallback m_fnReceiveHandler;
        private static AsyncCallback m_fnSendHandler;
        private static AsyncCallback m_fnAcceptHandler;

        private static Socket m_ServerSocket = null;

        private static mini_database m_ConnectedClientList;    //(Unit_num (A.I), Socket)      소캣관리 디비
        private static mini_database Faliure_counting;          //order_no가 키, connect_no 가 값.
        private static mini_database Testing_List;              //테스터 유닛용 테스트 목록 디비 order_no가 키, i_Test_connect_order 가 값,
        private static Queue<command_data_server> Command_Q;            //명령어 큐
        private static Queue<command_data_server> Command_waitQ;        //명령어 대기 큐
        private static Queue<command_data_client> Responser_Q;            //응답 대기 큐.
        private static bool Running = false;
        private static Random Rand = new Random();
        #endregion

        #region Layer [스레드]
        private static void Thread_Responser_()
        {
            command_data_client CMD;
            
            while (Running)
            {
                while (Responser_Q.Count > 0)
                {
                    lock (Responser_Q)
                    {
                        CMD = (command_data_client)Responser_Q.Dequeue();
                    }
                    try
                    {
                        AsyncObject ao = new AsyncObject(1);
                        // 문자열을 바이트 배열으로 변환
                        try {

                            ao.Buffer = Data_structure.Combine(Encoding.Unicode.GetBytes("^^^"), GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CMD))));
                            //ao.Buffer = GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CMD)));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("송신 데이터 준비중 오류: {0}", ex.Message);
                            continue;
                        }
                        //ao.Buffer = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CMD));
                        ao.WorkingSocket = (Socket)m_ConnectedClientList.Select_key(CMD.Receiver);
                        ao.WorkingSocket.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnSendHandler, ao);
                    }
                    catch (Exception ex)
                    {
                        if (ex.HResult == -2147467259) //소캣 연결이 끊긴 경우.
                        {
                            Console.WriteLine("Disconnected_Client U[{0}]", CMD.Receiver);
                            Add_Command(CMD.Receiver, Command_Server.Disconnected_Client, CMD.Receiver);
                        }
                        else
                        {
                            Console.Write("Error Send Message [ Command : {0}, data : {1},  receiver]", ((Command_Client)CMD.Command_code).ToString(), CMD.data != null ? CMD.data.ToString() : "", CMD.Receiver.ToString());
                            Console.Write("Error String: {0}", ex);
                        }
                        //애러를 클라이언트에게 보내주는 코드가 필요할지도 모른다. 1.25
                    }
                }
                Thread.Sleep(100);
            }
        }
        private static void Thread_Commander_()
        {
            command_data_server CMD;

            while (Running)
            {
                lock (Command_waitQ) //락걸고 들어가.
                {
                    lock(Command_Q)
                    {
                        for (int i = 0; i < Command_waitQ.Count; i++)
                        {
                            Command_Q.Enqueue(Command_waitQ.Dequeue());
                        }
                    }
                }
                while (Command_Q.Count > 0)
                {
                    lock (Command_Q)
                    {
                        CMD = (command_data_server)Command_Q.Dequeue();
                    }

                    if (Data_structure.is_debug)
                        Commander(CMD); //명령어 수행
                    else
                    {
                        try
                        {
                            Commander(CMD); //명령어 수행
                        }
                        catch (Exception ex)
                        {
                            Console.Write("Error [ Command : {0}, Sender : {1}, data : {2}]", ((Command_Server)CMD.Command_code).ToString(), CMD.Sender.ToString(), CMD.data != null ? CMD.data.ToString() : "");
                            Console.Write("Error String: {0}", ex);

                            //애러를 클라이언트에게 보내주는 코드가 필요할지도 모른다. 1.25
                        }
                    }
                }
                Thread.Sleep(100);
            }
        }
        private static void Thread_Daemon_() //각종 메니저를 처리하는 스레드.
        {
            //프록시 메니저 프록시 대기상태의 유닛들에게 프록시 배분
            //프록시 테스터 테스트 안된 프록시 서버 테스팅
            //오더 메니저.. 엑티브 상태의 주문을가져와 새로운 커넥트 생성. 한번에 10개 미만으로..

            int[] Managing = new int[]{
                (int)Command_Server.Managing_Order, 
                (int)Command_Server.Managing_Proxy, 
                (int)Command_Server.Managing_Tester,
                (int)Command_Server.HeartBeat,
                (int)Command_Server.Update_Proxy_DB,
                (int)Command_Server.Update_User_DB,
                (int)Command_Server.Update_Account_DB,
                (int)Command_Server.Managing_Unit,
                (int)Command_Server.Managing_OrderInfo,
                (int)Command_Server.Managing_Login_Account,
                //Managiing_Unit
                    };
            int[] Term = new int[]    {   1,  5,  7, 20,    7200,   300,    300,    3 , 3, 3};//각 주기
            int[] T = new int[]       {   5,  1,  2, 0,     0,      5,      5,      3 , 3, 10};//시작 카운터. 0이면 즉시 실행
             
            while (Running)
            {
                Thread.Sleep(1000);

                for (int i = 0; i < Managing.Length; i++)
                {
                    if (T[i] <= 0)
                    {
                        Add_Command((Command_Server)Managing[i], null);
                        T[i] = Term[i];
                    }
                    else
                        T[i] = T[i] - 1;
                }
            }
        }
        #endregion
        
        private static void Commander(command_data_server CMD)
        {
            /*
            string SPEAKER = "";
            if (CMD.Sender == -1)
                SPEAKER = "Server";
            else
                SPEAKER = "C - " + CMD.Sender.ToString();
            Console.WriteLine("[" + SPEAKER + "] " + ((Command_Server)CMD.Command_code).ToString());
            */
            switch (CMD.Command_code)
            {
                case (int)Command_Server.New_connect:
                    {
                        Socket S = (Socket)CMD.data; //소캣을 받아
                        int Unit_No = Convert.ToInt32(DB.Newindex.generate_index()); //고유 유닛번호를 생성하고
                        DB.Unit.Rows.Add(Unit_No, 0, 5, "", State_Unit.Starting, 0); //디비에 저장한다음
                        m_ConnectedClientList.Insert(Unit_No, S);
                        Add_Response(Unit_No, Command_Client.Set_UnitNo, Unit_No);   //고유번호를 응답한다.
                    }
                    break;
                case (int)Command_Server.Disconnected_Client: //클라이언트 소캣이 끊어지면 이 명령이 들어온다.
                    {
                        #region Command_Server.Disconnected_Client

                        int Unit_no = (int)CMD.Sender;
                        
                        DataRow[] user = DB.User.Select("Unit_No = " + Unit_no);
                        if (user.Length > 0)
                        {
                            int user_no = (int)user[0]["No"];
                            foreach (DataRow O in DB.Order.Select("User_No = " + user_no))
                            {
                                //DB.Order.Rows.Remove(O); //그냥 삭제하는게 아니라.
                                Add_Command(Command_Server.Order_Disconnect, (int)O["No"]); //연결 해제 명령을 내려 정상적으로 삭제되게 한다.
                                delete_Faliure_counting((int)O["No"]); //Faliure_counting에서도 삭제해 주어야 한다..
                            }
                            DB.User.Rows.Remove(user[0]); //삭제..
                        }

                        DataRow[] Connection = DB.User.Select("Unit_No = " + Unit_no.ToString());
                        if (Connection.Length > 0)
                            for (int i = 0; i < Connection.Length; i++)
                            {
                                Add_Response((int)Connection[i]["Unit_No"], Command_Client.Close_connect, (int)Connection[i]["No"]);
                                //서버에서 삭제하고 유닛에는 남아잇으면안되므로 닫기 명령을 내보낸다.
                                //DB.Connection.Rows.Remove(Connection[i]);
                            }
                        
                        DataRow[] unit = DB.Unit.Select("No = " + Unit_no); //유닛을 맨 마지막에 삭제한다.
                        if (unit.Length == 1) //어드민이거나 유저면 , 오더를 삭제해야 한다.
                        {   //프록시 상태를 바꿈.
                            Add_Command(Command_Server.Updated_Proxy, new i_Update_Proxy((string)unit[0]["Proxy"], State_Proxy.Enable));
                            DB.Unit.Rows.Remove(unit[0]);
                        }
                        else
                        {
                            Console.WriteLine("Error] Disconnected_Client Error.");
                        }
                        
                        #endregion


                        #region Command_Server.Disconnected_Client backup
                        /*
                        int Unit_no = (int)CMD.Sender;

                        DataRow[] Unit__ = DB.Unit.Select("No = " + Unit_no);
                        if (Unit__.Length > 0)
                            Add_Command(Command_Server.Updated_Proxy, new i_Update_Proxy((string)Unit__[0]["Proxy"], State_Proxy.Enable));
                        //프록시 상태를 바꿈.

                        DataRow[] user = DB.User.Select("Unit_No = " + Unit_no);
                        if (user.Length > 0)
                        {
                            DataRow[] Connection = DB.User.Select("Unit_No = " + ((int)user[0]["Unit_No"]).ToString());
                            if (Connection.Length > 0)
                                for (int i = 0; i < Connection.Length; i++)
                                {
                                    Add_Response((int)Connection[i]["Unit_No"], Command_Client.Close_connect, (int)Connection[i]["No"]);
                                    //서버에서 삭제하고 유닛에는 남아잇으면안되므로 닫기 명령을 내보낸다.
                                    //DB.Connection.Rows.Remove(Connection[i]);
                                }
                        }
                        if (user.Length > 0) //어드민이거나 유저면 , 오더를 삭제해야 한다.
                        {
                            int user_no = (int)user[0]["No"];
                            foreach (DataRow O in DB.Order.Select("User_No = " + user_no))
                            {
                                //DB.Order.Rows.Remove(O); //그냥 삭제하는게 아니라.
                                Add_Command(Command_Server.Order_Disconnect, (int)O["No"]); //연결 해제 명령을 내려 정상적으로 삭제되게 한다.
                                delete_Faliure_counting((int)O["No"]); //Faliure_counting에서도 삭제해 주어야 한다..
                            }

                            DB.User.Rows.Remove(user[0]); //삭제..
                        }

                        DataRow[] unit = DB.Unit.Select("No = " + Unit_no); //유닛을 맨 마지막에 삭제한다.
                        if (unit.Length == 1) //어드민이거나 유저면 , 오더를 삭제해야 한다.
                        {
                            DB.Unit.Rows.Remove(unit[0]);
                        }
                        else
                        {
                            Console.WriteLine("Error] Disconnected_Client Error!!");
                        }

                        */
                        #endregion
                    }
                    break;
                case (int)Command_Server.Order_Connect: //유저의 유효기간, url중복 검사.
                    {
                        #region Command_Server.Order_Connect
                        i_Order iOrder = JsonConvert.DeserializeObject<i_Order>(CMD.data.ToString());

                        ///유저 유효기간 검사.

                        DataRow[] Users = DB.User.Select("No = " + iOrder.User_No);

                        if (Users.Length != 1)
                        {
                            Add_Response(CMD.Sender, Command_Client.Error_Order, client_message.사용자를_확인할_수_없습니다);
                            return;
                        }
                        if ((int)Users[0]["slot"] < iOrder.Max_Connect)
                        {
                            Add_Response(CMD.Sender, Command_Client.Error_Order, client_message.할당된_연결보다_더_많은_연결을_생성할_수_없습니다);
                            return;
                        }
                        DataRow User = Users[0];
                        DateTime expire = (DateTime)User["expire"];
                        TimeSpan turm = DateTime.Now - expire;
                        if (turm.TotalSeconds > 0)
                        {
                            Add_Response(CMD.Sender, Command_Client.Error_Order, client_message.사용기간이_만료되었습니다);
                            return;
                        }

                        ///URL중복 검사를 디비를 조회해 처리한다.
                        DataRow[] orders = DB.Order.Select("URL = '" + iOrder.URL + "' and User_No = " + iOrder.User_No);
                        if (orders.Length > 0)
                        {
                            Add_Response(CMD.Sender, Command_Client.Error_Order, client_message.같은_URL로_진행중인_연결이_있습니다);
                            Add_Response(CMD.Sender, Command_Client.Error_Order, client_message.연결이_모두_종료된_후에_다시_시도하세요);
                            return;
                        }


                        DB.Order.Rows.Add(
                            iOrder.No,
                            iOrder.User_No,
                            0,
                            iOrder.Max_Connect,
                            iOrder.URL,
                            iOrder.Type_order,
                            State_Order.Wait_Test); //테스트 대기로 시작.

                        Add_Response(CMD.Sender, Command_Client.Active_Order, iOrder.No); //올바른 접속이라고 알림
                        #endregion
                    }
                    break;
                case (int)Command_Server.Order_Request_No://단운 오더 넘버만 생성한것을 요청한다.
                    {
                        #region Command_Server.Order_Connect

                        int Order_No = Convert.ToInt32(DB.Newindex.generate_index());

                        Add_Response(CMD.Sender, Command_Client.Order_No, Order_No);

                        #endregion
                    }
                    break;
                case (int)Command_Server.Order_Disconnect:
                    {
                        int order_no = int.Parse(CMD.data.ToString());
                        delete_Faliure_counting(order_no); //삭제해 주어야 한다..

                        DataRow[] R = DB.Order.Select("No = " + order_no);

                        if (R.Length == 1)
                        {
                            R[0]["State"] = State_Order.Closing; //종료 상태로 바꾼다.
                        }
                        else
                        {
                            throw new Exception("Can't Find Order no." + order_no + " On DB.");
                        }
                    }
                    break;
                case (int)Command_Server.Order_Set_MaxConnect:
                    {
                        i_Set_MaxConnect Set_MaxConnect = JsonConvert.DeserializeObject<i_Set_MaxConnect>(CMD.data.ToString());

                        DataRow[] Users = DB.User.Select("No = " + Set_MaxConnect.User_No);
                        DataRow[] Orders = DB.Order.Select("No = " + Set_MaxConnect.Order_No);

                        if (Orders.Length > 1)
                        {
                            Add_Response(CMD.Sender, Command_Client.R_Order_Set_MaxConnect, new i_R_Order_Set_MaxConnect(client_message.존재하지_않는_연결명령입니다, Set_MaxConnect.Order_No, Set_MaxConnect.User_No));
                            return;
                        }
                        else if (Orders.Length == 0)
                        {
                            i_R_Order_Set_MaxConnect ir = new i_R_Order_Set_MaxConnect(client_message.방송_접속수가_변경되었습니다, Set_MaxConnect.Order_No, Set_MaxConnect.MaxConnect);
                            Add_Response(CMD.Sender, Command_Client.R_Order_Set_MaxConnect, ir);
                            return;
                        }

                        if (Users.Length == 1)
                        {
                            if ((int)Users[0]["slot"] < Set_MaxConnect.MaxConnect)
                            {
                                Add_Response(CMD.Sender, Command_Client.R_Order_Set_MaxConnect, new i_R_Order_Set_MaxConnect(client_message.할당된_연결보다_더_많은_연결을_생성할_수_없습니다, Set_MaxConnect.Order_No, Set_MaxConnect.User_No));
                                //Add_Response(CMD.Sender, Command_Client.R_Order_Set_MaxConnect, client_message.할당된_연결보다_더_많은_연결을_생성할_수_없습니다);
                                return;
                            }
                            else if ((int)Orders[0]["Max_connect"] == Set_MaxConnect.MaxConnect)
                            {
                                return;
                            }
                            try
                            {
                                Orders[0]["Max_connect"] = Set_MaxConnect.MaxConnect;
                                i_R_Order_Set_MaxConnect ir = new i_R_Order_Set_MaxConnect(client_message.방송_접속수가_변경되었습니다, Set_MaxConnect.Order_No, (int)Orders[0]["Max_connect"]);
                                Add_Response(CMD.Sender, Command_Client.R_Order_Set_MaxConnect, ir);
                            }
                            catch
                            {
                                i_R_Order_Set_MaxConnect ir = new i_R_Order_Set_MaxConnect(client_message.방송_접속수_변경중_오류, Set_MaxConnect.Order_No, 0);
                                Add_Response(CMD.Sender, Command_Client.R_Order_Set_MaxConnect, ir);
                            }
                        }
                        else
                        {
                            Add_Response(CMD.Sender, Command_Client.R_Order_Set_MaxConnect, new i_R_Order_Set_MaxConnect(client_message.사용자를_확인할_수_없습니다, Set_MaxConnect.Order_No, Set_MaxConnect.User_No));
                           // Add_Response(CMD.Sender, Command_Client.R_Order_Set_MaxConnect, client_message.사용자를_확인할_수_없습니다);
                            return;
                        }
                    }
                    break;
                case (int)Command_Server.Sucsses_Proxy: //유닛이 프록시 설정에 성공했다는 신호.
                    {                                   //해당 프록시를 사용중으로 바꾼다.
                        string host_server = (string)CMD.data;

                        DataRow[] R = DB.Unit.Select("No = " + CMD.Sender);

                        if (R.Length == 1)
                        {
                            DataRow Unit_ = R[0];
                            Unit_["Proxy"] = host_server;

                            Add_Command(Command_Server.Updated_Proxy, new i_Update_Proxy(host_server, State_Proxy.Used));
                            Add_Response(CMD.Sender, Command_Client.set_proxy_success_ok, host_server);
                        }
                        else
                        {
                            Console.WriteLine("Unit DB error");
                        }
                    }
                    break;
                case (int)Command_Server.Failure_Proxy: //단순히 프록시 상태만 업데이트 하는 용도,
                    {       //프록시 재할당은 상태가 업데이트 하면서 자동처리.
                        string host_server = (string)CMD.data;
                        Add_Command(Command_Server.Updated_Proxy, new i_Update_Proxy(host_server, State_Proxy.Disable));
                    }
                    break;
                case (int)Command_Server.Failure_Connect: //연결 5회 실패시..
                    {
                        int order_no = int.Parse(CMD.data.ToString());
                        ///order의 connected_count의 값을 확인해 0이상이면
                        ///Faliure_counting에서 해당 order을 패기하고 
                        ///아니면 카운터를 올린다. Faliure_counting에 없으면 추가.
                        /// 
                        ///카운터가 10이상이 되면 오더를 종료한다.
                        ///그러면서 클라이언트에게 메시지를 보낸다. 비정상종료라고,
                        /// 
                        /// Faliure_counting에서 삭제하는 조건은
                        /// 클라이언트 로그아웃,
                        ///  Order_Disconnect 명령.
                        /// 비정상적인 종료 + 

                        DataRow[] OR = DB.Order.Select("No = " + order_no);

                        if (OR.Length != 1)
                        {
                            throw new Exception("Don't Find Order no." + order_no);
                        }

                        int C_count = (int)OR[0]["Connected_count"];

                        if (C_count > 0) //0이상이면 패기.
                            delete_Faliure_counting(order_no);
                        else
                        {
                            if (Faliure_counting.Has_int_key(order_no)) //키가 존재하면, +1한 카운트로 업데이트 하고,
                                Faliure_counting.Update(order_no, (int)Faliure_counting.Select_key(order_no) + 1);
                            else //없으면
                                Faliure_counting.Insert(order_no, 1); //새 테이블을 생성.

                            if (Faliure_counting.Has_int_key(order_no)) //키가 존재하면, +1한 카운트로 업데이트 하고,
                                if ((int)Faliure_counting.Select_key(order_no) >= 10) //5가
                                {
                                    //클라이언트에게 "비정상 종료"라고 메시지를 보내는 코드 필요

                                    //오더 종료
                                    Add_Command(Command_Server.Order_Disconnect, order_no);
                                }
                        }
                    }
                    break;
                case (int)Command_Server.Managing_Order: //유닛에게 연결 명령 배분
                    {
                        #region Command_Server.Managing_Order

                        //새롭게 오더가 들어와서 테스트를 대기중인 오더를 찾아     
                        if (Data_structure.is_debug)//디버그 모드이면, 테스팅 안함.
                        {
                            foreach (DataRow WaitOrder in DB.Order.Select("State = " + (int)State_Order.Wait_Test))
                            {
                                WaitOrder["State"] = State_Order.Active; //상태를 바꾼다.
                            }
                        }
                        else
                        {
                            foreach (DataRow WaitOrder in DB.Order.Select("State = " + (int)State_Order.Wait_Test))
                            {
                                int order_no = (int)WaitOrder["No"];
                                List<i_Test_connect_order> M;

                                if (Testing_List.Has_int_key(order_no)) //있으면 넣구
                                {
                                    M = (List<i_Test_connect_order>)Testing_List.Select_key(order_no);

                                    Test_connect_Order(order_no, M);
                                }
                                else
                                {
                                    M = new List<i_Test_connect_order> { };

                                    Test_connect_Order(order_no, M);
                                }

                                WaitOrder["State"] = State_Order.Testing; //상태를 바꾼다.
                            }
                        }
                        foreach (DataRow TestingOrder in DB.Order.Select("State = " + (int)State_Order.Testing))
                        {
                            int orderno = (int)TestingOrder["No"];
                            if (Testing_List.Has_int_key(orderno))
                            {
                                List<i_Test_connect_order> it = (List<i_Test_connect_order>)Testing_List.Select_key(orderno);

                                if (it.Count >= 5) //테스트를 5회 이상 했다면.. 그리고 연결시도가 모두 실패라면, 삭제,
                                {
                                    string Query = "";
                                    for (int i = 0; i < it.Count; i++) //i_Test_connect_order의 해당 오더에 담긴 모든 테스트 항목을 가져와
                                        if (i == 0)
                                            Query += "No = " + it[i].Connect_No;
                                        else
                                            Query += " or No = " + it[i].Connect_No;

                                    DataRow[] testconnect = DB.Connection.Select("(" + Query + ") and State =" + (int)State_Connect.Failure);

                                    if (testconnect.Length >= it.Count) //아무도 성공하지 못했다면, 삭제하자.
                                    {
                                        Testing_List.Remove(orderno);
                                        TestingOrder["State"] = State_Order.Closing; //상태를 바꾼다.
                                        Console.WriteLine("UnPassed Test Order : {0}.", orderno);
                                        continue;
                                    }
                                }
                                else if (it.Count <= 0) //테스트 한적이 없으면, 테스트를 진행하고 그 결과물을 저장.
                                {
                                    Test_connect_Order(orderno, (List<i_Test_connect_order>)Testing_List.Select_key(orderno));
                                    continue;
                                }
                                bool is_connected = false;
                                //Console.WriteLine("★★★★★★testing count [ {0} ]", it.Count );
                                foreach (i_Test_connect_order connect in it) //테스트의 결과를 검사.
                                {

                                    DataRow[] Cnt = DB.Connection.Select("No = " + connect.Connect_No + " and State = " + (int)State_Connect.Connected);

                                    //디버그
                                    // DataRow[] Cnt_debug = DB.Connection.Select("No = " + connect.Connect_No);
                                    //if (Cnt_debug.Length > 0)
                                    //    Console.WriteLine("★★★★★★connect.Connect_No [ {0} ], state [{1}] ", connect.Connect_No, Cnt_debug[0]["State"].ToString());

                                    if (Cnt.Length > 0) //테스트 진행한 연결 중 연결된게 하나라도 있으면 
                                    {
                                        is_connected = true; //종료 신호를 보낸다.
                                        break;
                                    }
                                }
                                if (is_connected) //연결되면 테스트 관련 디비와 미니디비를 지우고 상태를 엑티브로 바꾼다.
                                {
                                    string Query = "";
                                    for (int i = 0; i < it.Count; i++) //i_Test_connect_order의 해당 오더에 담긴 모든 테스트 항목을 가져와
                                        if (i == 0)
                                            Query += "No = " + it[i].Connect_No;
                                        else
                                            Query += " or No = " + it[i].Connect_No;

                                    DataRow[] testconnect = DB.Connection.Select(Query);

                                    if (testconnect.Length > 0) //그것을 실제 디비에 있는지 찾는다.
                                        foreach (DataRow Rtc in testconnect) //찾아서 실제 디비에 있는 것들을 모아 전부 접속 해제 명령을 내린다.
                                            Add_Response((int)Rtc["Unit_No"], Command_Client.Close_connect, (int)Rtc["No"]);

                                    Testing_List.Remove(orderno);

                                    TestingOrder["State"] = State_Order.Active; //상태를 바꾼다.
                                }
                                else //성공한 연결이 없으면 또다른 테스트 유닛을 찾아 테스트 진행.
                                {
                                    if (it.Count < 2) //5개 이상은 안된다.
                                        Test_connect_Order(orderno, (List<i_Test_connect_order>)Testing_List.Select_key(orderno));
                                }

                            }
                            else // 테스트 할 List<i_Test_connect_order> 정보가 없는 것이므로, 새로 만든다.
                            {
                                Testing_List.Add(orderno, new List<i_Test_connect_order> { });
                            }
                            //커넥션들을 검사해서 사라진게 있으면(접속 실패 이므로.), 다시 테스트 시도,최대 재시도 횟수는 5회.
                            //연결된게 있으면 해당 테스트 카운트를 삭제하고 
                            //상태를 액티브로 바꾼다.
                        }


                        //필요한 연결을 계산해 새 연결명령을 내리는 코드.
                        List<int> Order_unit_list = new List<int>(); //루프가 돌면서 같은 유닛에게 동시 서로 다른 명령을 안내리기 위해..
                        foreach (DataRow AcitiveOrder in DB.Order.Select("State = " + (int)State_Order.Active))
                        {
                            try
                            {
                                string query =
                                    "Order_No = " + (int)AcitiveOrder["No"] +
                                    " and (State = " + (int)State_Connect.Connected +
                                    " or State = " + (int)State_Connect.Connecting +
                                    " or State = " + (int)State_Connect.Closing + ")";

                                int Connected_count = DB.Connection.Select(query).Length;
                                int need_connect = (int)AcitiveOrder["Max_connect"] - Connected_count + 8;


                                string Query = "Connected_count < " + Data_structure.MaxConnect_per_unit + " and State = " + (int)State_Unit.Enable;

                                foreach (int no in Order_unit_list)
                                {
                                    Query = Query + " and NOT No = " + no.ToString();
                                }

                                DataRow[] Tester_User = DB.User.Select("User_type = " + (int)Type_User.Tester_Unit);
                                foreach (DataRow TUser in Tester_User) //테스터 유닛은 사용 가능한 유닛에서 제외시킨다.
                                {
                                    Query += " and not No = " + (int)TUser["Unit_No"];
                                }

                                DataRow[] Enable_Unit = DB.Unit.Select(Query);
                                
                                AcitiveOrder["Connected_count"] = Connected_count;

                                //가장 작은 값을 이번에 연결할 사이즈로 삼음.
                                int Volume = Math.Min(Math.Min(1, Enable_Unit.Length), need_connect);

                                for (int j = 0; j < Volume; j++)
                                {
                                    int Connect_No = Convert.ToInt32(DB.Newindex.generate_index()); //고유 유닛번호를 생성하고
                                    int Rand_no = Rand.Next(Enable_Unit.Length);

                                    int Unit_no = (int)Enable_Unit[Rand_no]["No"];

                                    DB.Connection.Rows.Add(
                                        Connect_No,
                                        AcitiveOrder["No"],
                                        Enable_Unit[Rand_no]["No"], //랜덤하게 유닛 매칭
                                        AcitiveOrder["URL"],
                                        Enable_Unit[Rand_no]["Proxy"],
                                        AcitiveOrder["Type_order"],
                                        State_Connect.Connecting,
                                        -1,
                                        State_Account_on_connected.NotSet,
                                        0
                                        );
                                    //디비에 새 커넥트를 만들고
                                    //오더명령.
                                    i_connect iconnect = new i_connect(
                                        Connect_No,
                                        (int)AcitiveOrder["No"],
                                        (string)AcitiveOrder["URL"],
                                        (Type_Order)AcitiveOrder["Type_order"],
                                        (string)Enable_Unit[Rand_no]["Proxy"]
                                        );
                                    Order_unit_list.Add(Unit_no);
                                    Add_Response(Unit_no, Command_Client.order_connect, iconnect);// 연결명령을 보낸다
                                    Add_Command(Unit_no, Command_Server.Updated_Unit, State_Unit.Wait); //즉시 상태를 대기로 바꾼다.
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("ERROR Managing_Order.Acitive_Order Step 0 : {0}", ex.ToString());
                            }
                        }

                        //필요한 연결 수와 현재 접속된 카운트를 비교해 삭제해야할 접속을 찾아 삭제 명령을 내림.
                        //List<int> Order_unit_list = new List<int>(); //루프가 돌면서 같은 유닛에게 동시 서로 다른 명령을 안내리기 위해..
                        foreach (DataRow AcitiveOrder in DB.Order.Select("State = " + (int)State_Order.Active))
                        {
                            try
                            {
                                string query = //단지 갯수만 알아내기 위한 쿼리.
                                    "Order_No = " + (int)AcitiveOrder["No"] +
                                    " and (State = " + (int)State_Connect.Connected +
                                    " or State = " + (int)State_Connect.Connecting +
                                    " or State = " + (int)State_Connect.Closing + ")";

                                int Active_count = DB.Connection.Select(query).Length; //현재 활성화된 모든 연결 수 
                                int need_Disconnect = Active_count - (int)AcitiveOrder["Max_connect"];

                                if (need_Disconnect < 12) //삭제 할게 없으면 넘어간다.
                                    continue;

                                string query1 = //상태가 Connected인 연결중에서 삭제하기 위해 Connected상태인 연결을 뽑는다.
                                    "Order_No = " + (int)AcitiveOrder["No"] +
                                    " and State = " + (int)State_Connect.Connected;
                                DataRow[] Connected_ROW = DB.Connection.Select(query1);

                                //가장 작은 값을 이번에 연결할 사이즈로 삼음 한번에 최대 5개씩 삭제,
                                int Volume = Math.Min(Math.Min(5, need_Disconnect), Connected_ROW.Length);
                                List<int> Deletable = new List<int>();

                                for (int j = 0; Deletable.Count <= Volume; j++)
                                {
                                    int Rand_no = Rand.Next(Connected_ROW.Length);

                                    if (!Deletable.Contains(Rand_no))
                                        Deletable.Add(Rand_no); //기존에 없으면 추가.
                                }
                                
                                for (int j = 0; j < Deletable.Count; j++)
                                {
                                    Add_Response(
                                        (int)Connected_ROW[Deletable[j]]["Unit_No"], 
                                        Command_Client.Close_connect, 
                                        (int)Connected_ROW[Deletable[j]]["No"]
                                        ); // 연결해제 명령을 보낸다
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("ERROR Managing_Order.Acitive_Order Step 889 : {0}", ex.ToString());
                            }
                        }


                        DataRow[] Closing_Order = DB.Order.Select("State = " + (int)State_Order.Closing);
                        //닫혀있지 않은 연결을 찾아 닫음
                        //모두 닫히면 close로 변경.
                        for (int i = 0; i < Closing_Order.Length; i++)
                        {
                            try
                            {   //각 오더에 대해 연결상태가 State_Connect.connected 인 연결을 찾아 닫는 신호를 보낸다.
                                DataRow[] yet_closing_connect = DB.Connection.Select("Order_No = " + (int)Closing_Order[i]["No"] + " and State = " + (int)State_Connect.Connected + "");

                                foreach (DataRow DR in yet_closing_connect)
                                {
                                    i_Updated_Connection iuc = new i_Updated_Connection((int)DR["No"], i_Updated_Connection_Type.State_Connect, State_Connect.Wait);
                                    Add_Command(Command_Server.Updated_Connection, iuc);
                                    //대기로 바꾸고,

                                    Add_Response((int)DR["Unit_No"], Command_Client.Close_connect, iuc.Connect_No);
                                    //바로 종료 신호.

                                    //유닛에서 받으면 그 즉시 클로징 상태,
                                    //그리고 닫기 수행 시작.
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("ERROR Managing_Order.Closing_Order Step 1 : {0}", ex.ToString());
                            }

                            try
                            {   //모두 닫혔는지 확인하고 닫혔으면 오더를 close 상태로 바꾼다.
                                DataRow[] all_connect = DB.Connection.Select("Order_No = " + (int)Closing_Order[i]["No"] + "");

                                if (all_connect.Length == 0)
                                    Add_Command((int)Closing_Order[i]["No"], Command_Server.Updated_Order, State_Order.Close);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("ERROR Managing_Order.Closing_Order Step 2 : {0}", ex.ToString());
                            }
                        }
                            
                        DataRow[] Close_Order = DB.Order.Select("State = " + (int)State_Order.Close);
                        //삭제.
                        //삭제하기전엔 반드시 로그인 정보를 보낸다.
                        command_data_server cmd = new command_data_server(-1, Command_Server.Managing_OrderInfo, null);
                        Commander(cmd);

                        for (int i = 0; i < Close_Order.Length; i++)
                        {
                            try
                            {
                                DB.Order.Rows.Remove(Close_Order[i]);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("ERROR Managing_Order.Close_Order Step 3 : {0}", ex.ToString());
                            }
                        }

                        DataRow[] Close_Connection = DB.Connection.Select("State = " + (int)State_Connect.Close);
                        //삭제.
                        for (int i = 0; i < Close_Connection.Length; i++)
                        {
                            try
                            {
                                DB.Connection.Rows.Remove(Close_Connection[i]);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("ERROR Managing_Order.Close_Order Step 4 : {0}", ex.ToString());
                            }
                        }

                        #endregion
                    }
                    break;
                case (int)Command_Server.Managing_Proxy: //유닛에게 프록시 배분
                    {
                        #region Command_Server.Managing_Proxy
                        DataRow[] P = DB.Proxy.Select("State = " + (int)State_Proxy.Passed_test);
                        DataRow[] U = DB.Unit.Select("State = " + (int)State_Unit.Wait_proxy);

                        if (P.Length > 0 && U.Length > 0) //둘다 먼가 잇어야지
                        {
                            foreach (DataRow Unit_ in U)
                            {
                                P = DB.Proxy.Select("State = " + (int)State_Proxy.Passed_test); //테스트를 통과한 항목 새로 찾음.

                                if (P.Length > 0)
                                {
                                    int Sel = Rand.Next(P.Length);

                                    string host_server = P[Sel]["Host"].ToString(); //매칭

                                    //이 코드를 쓰면 안된다. 즉시 바꿔야 한다.
                                    //Add_Command(Command_Server.Updated_Proxy, new i_Update_Proxy(host_server, State_Proxy.Wait));
                                    P[Sel]["State"] = State_Proxy.Wait;

                                    Add_Command((int)Unit_["No"], Command_Server.Updated_Unit, State_Unit.Wait);
                                    //해당 유닛의 상태를 대기상태로 바꾸어야 한다.
                                    Add_Response((int)Unit_["No"], Command_Client.Set_Proxy, host_server);   //고유번호를 응답한다.
                                }
                                else
                                    break;
                            }
                        }
                        #endregion
                    }
                    break;
                case (int)Command_Server.Managing_Unit:
                    {
                        foreach (DataRow U in DB.Unit.Rows)
                        {
                            int count = DB.Connection.Select("Unit_No = " + (int)U["No"]).Length;
                            
                            U["Connected_count"] = count;
                        }
                    }
                    break;
                case (int)Command_Server.Managing_Tester:
                    {
                        Thread Thread_Managing_Tester = new Thread(new ParameterizedThreadStart(Thread_Managing_Tester_));
                        Thread_Managing_Tester.SetApartmentState(ApartmentState.STA);
                        Thread_Managing_Tester.Start(DB.Proxy.Select("State = " + (int)State_Proxy.New + " or State = " + (int)State_Proxy.Enable)); //응답을 처리하는 스레드.
                    }
                    break;
                case (int)Command_Server.Managing_OrderInfo: //클라이언트에게 오더의 상태 정보를 전송함
                    {
                        foreach (DataRow R in DB.Order.Rows) //모든 행을 가져온다.
                        {
                            DataRow[] UserL = DB.User.Select("No = " + R["User_No"].ToString());
                            if (UserL.Length != 1)
                                continue;
                            int Unit_no_on_Order = (int)UserL[0]["Unit_No"];

                            i_Order_Info orderinfo = new i_Order_Info(); //상태정보를 담을 클래스 생성.
                            orderinfo.Order_no = (int)R["No"];
                            orderinfo.Connect_Count = (int)R["Connected_count"];
                            orderinfo.State = (State_Order)R["State"];

                            Add_Response(Unit_no_on_Order, Command_Client.Order_Info, orderinfo);
                        }
                    }
                    break;
                case (int)Command_Server.Managing_Login_Account: //로그인이 가능하면 로그인 처리를 하는 코드.
                    {
                        mini_database AccountHash = new mini_database(); // Account No 가 키, i_List_Account가 값.
                        //DataRowCollection Order = DB.Order.Rows;
                        
                        foreach (DataRow A in DB.Account.Rows) //모든 행을 가져온다.
                        {
                            i_List_Account L = new i_List_Account();
                            L.ID = (string)A["ID"];
                            L.PW = (string)A["PW"];
                            L.No = (int)A["No"];
                            L.User_No = (int)A["User_No"];
                            L.Type_order_Site = (Type_Order)A["Site"];
                            L.State = (State_Account)A["State"];

                            AccountHash.Add(L.No, L);
                        }

                        foreach (DataRow R in DB.Order.Rows) //모든 행을 가져온다.
                        {
                            for (int i = 0; i < AccountHash.Count(); i++)
                            {
                                i_List_Account il = (i_List_Account)AccountHash.Select_for(i);

                                DataRow[] C = DB.Connection.Select("Order_No =" + ((int)R["No"]).ToString() + " and Login_Account_no = " + il.No.ToString() );

                                if (C.Length == 0) //없다는 것은 이 계정이 현재 오더에서는 쓰이지 않았다는 뜻이므로,
                                {
                                    DataRow[] candidate = DB.Connection.Select("Order_No =" + ((int)R["No"]).ToString() + "and State = " + (int)State_Connect.Connected + " and State_Account_on_connected = " + (int)State_Account_on_connected.NotSet);
                                    //먼저 해당 오더에서 아직 어카운트가 세팅되지 않은 연결을 모두 찾아낸다.
                                    if (candidate.Length > 0)
                                    {
                                        int candidate_No = Rand.Next(candidate.Length); //랜덤으로 수를 하나 뽑은 뒤.

                                        candidate[candidate_No]["Login_Account_no"] = il.No;
                                        candidate[candidate_No]["State_Account_on_connected"] = State_Account_on_connected.Setting;
                                        candidate[candidate_No]["Wait_count"] = 0;// 카운팅 대기를 한다.

                                        i_Account_Set ias = new i_Account_Set(
                                            il.Type_order_Site,
                                            (int)candidate[candidate_No]["No"],
                                            il.No, il.ID, il.PW,
                                            (int)candidate[candidate_No]["Order_No"]);

                                        Add_Response((int)candidate[candidate_No]["Unit_No"], Command_Client.Account_Set, ias);

                                    }
                                    else
                                        break; //임명가능한 연결이 전여 없기때문에 해당 오더는 패스.
                                    //임명
                                }

                            }
                        }
                            /// 모든 오더를 가져온다
                            /// 각 오더의 커넥션중 연결이 된 connected 상태의 연결을 가져온다
                            /// 이 중, 어카운트 리스트에서 사용되지 않은 아이디 패스워드를 찾아내어
                            /// connected 중 로그인 시도가 되지 않은 목록을 찾아 내어 매칭 시킨다.
                            /// 
                            ///(로그인 명령을 내린다) 
                    }
                    break;
                case (int)Command_Server.Updated_Order: //sender가 order_no, data가 type
                    {

                        DataRow[]  order = DB.Order.Select("No = " + (int)CMD.Sender + "");
                        if (order.Length == 1)
                        {
                            if ((State_Order)order[0]["State"] != (State_Order)CMD.data) //같지 않아야만 새로운 값으로 대치..
                            {
                                order[0]["State"] = (State_Order)CMD.data;
                                Console.WriteLine("Updated_Order. [{0} : {1}]", ((int)order[0]["No"]).ToString(), ((State_Order)CMD.data).ToString());
                            }
                        }
                        else
                            Console.WriteLine("Error Command_Server.Updated_Order");
                    }
                    break;
                case (int)Command_Server.Update_User_DB: //디비에서 유저 리스트를 가져와 추가하는 코드
                    {
                        Thread Thread_Update_User_DB = new Thread(new ThreadStart(Thread_Update_User_DB_));
                        Thread_Update_User_DB.SetApartmentState(ApartmentState.STA);
                        Thread_Update_User_DB.Start(); //응답을 처리하는 스레드.
                    }
                    break;
                case (int)Command_Server.Update_Proxy_DB: //디비에서 프록시 리스트를 가져와 추가하는 코드
                    {
                        Thread Thread_Update_Proxy_DB = new Thread(new ThreadStart(Thread_Update_Proxy_DB_));
                        Thread_Update_Proxy_DB.SetApartmentState(ApartmentState.STA);
                        Thread_Update_Proxy_DB.Start(); //응답을 처리하는 스레드.
                    }
                    break;
                case (int)Command_Server.Update_Account_DB: //디비에서 어카운트 리스트를 가져와 추가하는 코드
                    {
                        Thread Thread_Update_Account_DB = new Thread(new ThreadStart(Thread_Update_Account_DB_));
                        Thread_Update_Account_DB.SetApartmentState(ApartmentState.STA);
                        Thread_Update_Account_DB.Start(); //응답을 처리하는 스레드.
                    }
                    break;
                case (int)Command_Server.Updated_User:
                    {
                        i_Updated_User iuu = (i_Updated_User)CMD.data;

                        //내부 디비에 유저 정보를 저장한다.

                        DB.User.Rows.Add(
                            iuu.No,
                            iuu.Unit_No,
                            iuu.Slot,
                            iuu.Slot_Account,
                            iuu.fee,
                            iuu.id,
                            iuu.phone,
                            iuu.email,
                            iuu.pay_type,
                            iuu.session,
                            iuu.expire,
                            iuu.User_type,
                            iuu.is_Reseller,
                            iuu.my_Reseller_No
                            );

                        Console.WriteLine("U[{0}] Login.", iuu.id);

                    }
                    break;
                case (int)Command_Server.Updated_Proxy:
                    {
                        #region Command_Server.Updated_Proxy
                        i_Update_Proxy iup = (i_Update_Proxy)CMD.data;
                        if (iup.host.Length <= 5) //너무 짧으면 아니다.
                            return;

                        DataRow[] E = DB.Proxy.Select("Host = '" + iup.host + "'");
                        if (E.Length == 1)
                        {
                            E[0]["State"] = iup.state_proxy;
                            Console.WriteLine("Updated proxy [{0} - {1}]", iup.host, iup.state_proxy.ToString());
                        }                            
                        else
                            Console.WriteLine("Error Update_Proxy.");
                        #endregion
                    }
                    break;
                case (int)Command_Server.Updated_Unit:
                    {
                        #region Command_Server.Updated_Unit
                        DataRow[] U = DB.Unit.Select("No = " + CMD.Sender + "");
                        State_Unit state_unit = (State_Unit)CMD.data;

                        if (U.Length == 1)
                        {
                            int STATE_DB = (int)U[0]["State"]; //DB애서 가져온 상태
                            int STATE_NEW = (int)state_unit;
                            
                            if (STATE_DB == STATE_NEW)
                                return;
                            else
                            {
                                if (STATE_DB == (int)State_Unit.Wait && (int)U[0]["Wait_count"] > 0)
                                {
                                    U[0]["Wait_count"] = (int)U[0]["Wait_count"] - 1;
                                    Console.WriteLine("Wait Update Unit [{0} - (Wait){1}]", CMD.Sender, state_unit.ToString());
                                    return; //바로 종료
                                }
                                else if (state_unit == State_Unit.Wait)
                                    U[0]["Wait_count"] = 2;

                                U[0]["State"] = state_unit;
                                Console.WriteLine("Updated Unit [{0} - {1}]", CMD.Sender, state_unit.ToString());
                            }
                        }
                        else
                            Console.WriteLine("Error Updated_Unit.");
                        #endregion
                    }
                    break;
                case (int)Command_Server.Updated_Connection: //연결 상태를 변경시키는 메소드
                    {
                        #region Command_Server.Updated_Connection
                        i_Updated_Connection iuc = (i_Updated_Connection)CMD.data;

                        if (iuc.Updated_Connection_Type == i_Updated_Connection_Type.State_Connect)
                        {
                            State_Connect State_connect = (State_Connect)Convert.ToInt32(iuc.Data);

                            DataRow[] U = DB.Connection.Select("No = " + iuc.Connect_No + "");

                            if (U.Length == 1)
                            {
                                if ((int)U[0]["State"] == (int)State_connect) //변동이 없으면 그냥 리턴
                                    return;
                                else if (State_connect == State_Connect.Close) //만약 종료되었음을 알리는 신호라면,
                                {
                                    DB.Connection.Rows.Remove(U[0]); //정말 지워지는지 검증필요
                                    Console.WriteLine("Deleted Connection [U[{0}] C[{1}]", CMD.Sender, iuc.Connect_No);
                                    return;
                                }
                                else if (State_connect == State_Connect.Wait) //새롭게 대기가 되었다면 2로 카운팅 초기화
                                    U[0]["Wait_count"] = 2;
                                else if ((int)U[0]["State"] == (int)State_Connect.Wait && (int)U[0]["Wait_count"] > 0)
                                {
                                    U[0]["Wait_count"] = (int)U[0]["Wait_count"] - 1;
                                    Console.WriteLine("Wait Update Connect [{0} - (Wait){1}]", iuc.Connect_No, State_connect.ToString());
                                    return; //바로 종료
                                }

                                U[0]["State"] = (int)State_connect;
                                Console.WriteLine("Updated Connection [U[{0}] C[{1}] {2}]", CMD.Sender, iuc.Connect_No, State_connect.ToString());
                            }
                            else
                                Console.WriteLine("Error Updated_Connection.");
                        }
                        if (iuc.Updated_Connection_Type == i_Updated_Connection_Type.State_Account_on_connected)
                        {
                            State_Account_on_connected State_Account = (State_Account_on_connected)Convert.ToInt32(iuc.Data);

                            DataRow[] U = DB.Connection.Select("No = " + iuc.Connect_No + "");

                            if (U.Length == 1)
                            {
                                U[0]["State_Account_on_connected"] = (int)State_Account;
                            }
                        }
                        if (iuc.Updated_Connection_Type == i_Updated_Connection_Type.Login_Account_no)
                        {
                            int Account_no = (int)Convert.ToInt32(iuc.Data);

                            DataRow[] U = DB.Connection.Select("No = " + iuc.Connect_No + "");

                            if (U.Length == 1)
                            {
                                U[0]["Login_Account_no"] = Account_no;
                            }
                        }
                        #endregion
                    }
                    break;
                case (int)Command_Server.State:
                    {
                        #region Command_Server.State
                        i_State istate = JsonConvert.DeserializeObject<i_State>(CMD.data.ToString());

                        Add_Command(CMD.Sender, Command_Server.Updated_Unit, istate.State_unit); //유닛상태 업데이트.

                        foreach (i_Updated_Connection iuc in istate.State_connect)
                            Add_Command(CMD.Sender, Command_Server.Updated_Connection, iuc); //연결상태 업데이트.
                        #endregion
                    }
                    break;
                case (int)Command_Server.HeartBeat:
                    {
                        #region Command_Server.HeartBeat
                        
                        foreach (DataRow UL in DB.Unit.Rows)
                            Add_Response((int)UL["No"], Command_Client.HeartBeat, null);
                        #endregion
                    }
                    break;
                case (int)Command_Server.Add_Proxy:
                    {
                        //직접 추가하는 방식은 임시적으로 추가하는 방식임.
                        //서버가 재부팅 후에도 프록시에 보존되려면 디비에 추가해야 한다.
                    }
                    break;
                case (int)Command_Server.List_Order:
                    {
                        List<i_List_Order> LL_Order = new List<i_List_Order>();
                        foreach (DataRow R in DB.Order.Rows)
                        {
                            i_List_Order LOrder = new i_List_Order();
                            LOrder.No = (int)R["No"];
                            LOrder.User_No = (int)R["User_No"];
                            LOrder.Connected_count = (int)R["Connected_count"];
                            LOrder.Max_connect = (int)R["Max_connect"];
                            LOrder.URL = (string)R["URL"];
                            LOrder.Type_order = (Type_Order)R["Type_order"];
                            LOrder.State = (State_Order)R["State"];

                            LL_Order.Add(LOrder);
                        }

                        Add_Response(CMD.Sender, Command_Client.List_Order, LL_Order);
                    }
                    break;
                case (int)Command_Server.List_Account:
                    {
                        List<i_List_Account> LL_Account = new List<i_List_Account>();
                        foreach (DataRow R in DB.Account.Rows)
                        {
                            i_List_Account LAcc = new i_List_Account();
                            LAcc.No = (int)R["No"];
                            LAcc.Type_order_Site = (Type_Order)R["Site"];
                            LAcc.User_No = (int)R["User_No"];
                            LAcc.ID = (string)R["ID"];
                            LAcc.PW = (string)R["PW"];
                            LAcc.State = (State_Account)R["State"];

                            LL_Account.Add(LAcc);
                        }

                        Add_Response(CMD.Sender, Command_Client.List_Account, LL_Account);
                    }
                    break;
                case (int)Command_Server.List_Unit:
                    {
                        List<i_List_Unit> LL_Unit = new List<i_List_Unit>();
                        foreach (DataRow R in DB.Unit.Rows)
                        {
                            i_List_Unit LUnit = new i_List_Unit();
                            LUnit.No = (int)R["No"];
                            LUnit.Connected_count = (int)R["Connected_count"];
                            LUnit.Max_connect = (int)R["Max_connect"];
                            LUnit.Proxy = (string)R["Proxy"];
                            if (LUnit.Proxy == null)
                                LUnit.Proxy = "";
                            LUnit.State = (State_Unit)R["State"];
                            LUnit.Wait_count = (int)R["Wait_count"];

                            LL_Unit.Add(LUnit);
                        }

                        Add_Response(CMD.Sender, Command_Client.List_Unit, LL_Unit);
                    }
                    break;
                case (int)Command_Server.List_Proxy:
                    {
                        List<i_List_Proxy> LL_Proxy = new List<i_List_Proxy>();
                        foreach (DataRow R in DB.Proxy.Rows)
                        {
                            i_List_Proxy LProxy = new i_List_Proxy();
                            LProxy.No = (int)R["No"];
                            LProxy.Host = (string)R["Host"];
                            LProxy.Error_count = (int)R["Error_count"];
                            LProxy.State = (State_Proxy)R["State"];
                            LProxy.Type = (Type_Proxy)R["Type"];

                            LL_Proxy.Add(LProxy);
                        }

                        Add_Response(CMD.Sender, Command_Client.List_Proxy, LL_Proxy);
                    }
                    break;
                case (int)Command_Server.List_User:
                    {
                        List<i_List_User> LL_Unit = new List<i_List_User>();
                        foreach (DataRow R in DB.User.Rows)
                        {
                            i_List_User LUser = new i_List_User();
                            LUser.No = (int)R["No"];
                            LUser.Unit_No = (int)R["Unit_No"];
                            LUser.Slot = (int)R["slot"];
                            LUser.Slot_Account = (int)R["slot_account"];
                            LUser.fee = (int)R["fee"];
                            LUser.id = (string)R["id"];
                            LUser.phone = (string)R["phone"];
                            LUser.email = (string)R["email"];
                            LUser.pay_type = (string)R["pay_type"];
                            LUser.session = (string)R["session"];
                            LUser.expire = (DateTime)R["expire"];
                            LUser.User_type = (Type_User)R["User_type"];
                            LUser.is_Reseller = (bool)R["is_Reseller"];
                            LUser.my_Reseller_No = (int)R["my_Reseller"];

                            LL_Unit.Add(LUser);
                        }

                        Add_Response(CMD.Sender, Command_Client.List_User, LL_Unit);
                    }
                    break;
                case (int)Command_Server.List_Connect:
                    {
                        List<i_List_Connect> LL_Connect = new List<i_List_Connect>();
                        foreach (DataRow R in DB.Connection.Rows)
                        {
                            i_List_Connect LConnect = new i_List_Connect();
                            LConnect.No = (int)R["No"];
                            LConnect.Order_No = (int)R["Order_No"];
                            LConnect.Unit_No = (int)R["Unit_No"];
                            LConnect.Connect_URL = (string)R["Connect_URL"];
                            LConnect.Proxy_host = (string)R["Proxy_host"];
                            LConnect.Type_order = (Type_Order)R["Type_order"];
                            LConnect.State = (State_Connect)R["State"];
                            LConnect.Login_Account_no = (int)R["Login_Account_no"];
                            LConnect.State_Account_on_connected = (State_Account_on_connected)R["State_Account_on_connected"];
                            LConnect.Wait_count = (int)R["Wait_count"];

                            LL_Connect.Add(LConnect);
                        }
                        Add_Response(CMD.Sender, Command_Client.List_Connect, LL_Connect);
                    }
                    break;
                case (int)Command_Server.List_ServerInfo:
                    {
                        Add_Response(
                            CMD.Sender,
                            Command_Client.List_ServerInfo,
                            new i_List_ServerInfo(Command_Q.Count)
                            );
                    }
                    break;
                case (int)Command_Server.End_Broadcasting:
                    {
                        int order_no = int.Parse(CMD.data.ToString());

                        Add_Command(order_no, Command_Server.Updated_Order, State_Order.Closing);
                    }
                    break;
                case (int)Command_Server.User_Info:
                    {
                        DataRow[] Users = DB.User.Select("Unit_No = " + CMD.Sender);
                        if (Users.Length == 1)
                        {
                            DataRow User = Users[0];
                            i_User_info i_user = new i_User_info();
                            i_user.No = (int)User["No"];
                            i_user.Unit_No = (int)User["Unit_No"];
                            i_user.slot = (int)User["slot"];
                            i_user.Id = (string)User["id"];
                            i_user.Expire = (DateTime)User["expire"];
                            i_user.type = (Type_User)User["User_type"];
                            i_user.is_Reseller = (bool)User["is_Reseller"];

                            Add_Response(CMD.Sender, Command_Client.User_Info, i_user);
                        }
                        else
                        {
                            throw new Exception("not exist User entry");
                        }
                    }
                    break;
                case (int)Command_Server.Login: //즉시 디비로 가서 로그인 정보를 가져온다. //스레드로 분리할 수도 있을것.
                    {
                        i_Login Lo = new i_Login(-1, "", "", Type_User.Starter);
                        try
                        {
                            Lo = JsonConvert.DeserializeObject<i_Login>(CMD.data.ToString());
                            string Session_ = make_session(); //로그인 세션을 만든다.
                                                              ///그냥 테스터 일경우는 빠른 로긴,

                            if (Lo.type == Type_User.Tester_Unit) //테스터 유닛으로 로긴시.
                            {
                                i_Updated_User iuu = new i_Updated_User();

                                int User_No = 0;
                                do {//-1,-2,-3 순서로 계속 생성하여 없는 번호가 나올 때 까지,
                                    User_No--;
                                    iuu.No = User_No;
                                } while (DB.User.Select("No = " + User_No).Length != 0);
                                iuu.Unit_No = Lo.Unit_No;
                                iuu.phone = " ";
                                iuu.expire = (DateTime)DateTime.Now;
                                iuu.Slot = 5;
                                iuu.Slot_Account = 0;
                                iuu.id = Lo.id;
                                iuu.pay_type = " ";
                                iuu.fee = 0;
                                iuu.email = " ";
                                iuu.User_type = Lo.type;
                                iuu.is_Reseller = false;
                                iuu.my_Reseller_No = 0;
                                iuu.session = Session_;

                                Add_Command(Command_Server.Updated_User, iuu);
                                //테스터는 유닛상태가 logged를 가지지 않는다. 명령을 테스트 해야 하기 때문.

                                return;
                            }

                            MYSQL mysql = new MYSQL();

                            mysql.DB_SELECT("member");
                            mysql.DB_SELECT_WHERE("id", Lo.id);
                            mysql.DB_SELECT_WHERE("pw", Lo.pw);

                            MySqlDataReader reader = mysql.DB_SELECT_RUN(); 

                            if (MYSQL.is_readable(reader))
                            {
                                i_Updated_User iuu = new i_Updated_User();
                                iuu.No = (int)reader["no"];
                                iuu.Unit_No = Lo.Unit_No;
                                iuu.phone = (string)reader["phone"];
                                iuu.expire = (DateTime)reader["expire"];
                                iuu.Slot = (int)reader["slot"];
                                iuu.Slot_Account = (int)reader["slot_account"];
                                iuu.id = Lo.id;
                                iuu.pay_type = (string)reader["pay_type"];
                                iuu.fee = (int)reader["fee"];
                                iuu.email = (string)reader["email"];
                                iuu.User_type = (Type_User)((int)reader["User_type"]);
                                iuu.is_Reseller = (bool)reader["is_Reseller"];
                                iuu.my_Reseller_No = (int)reader["my_Reseller"];
                                iuu.session = Session_;

                                DataRow[] Logged_user = DB.User.Select("No = " + iuu.No);

                                if (Logged_user.Length > 0)
                                {
                                    Add_Response(Lo.Unit_No, Command_Client.Login_Failure, i_Login_Failure_type.Duplicate_Login); //중복 로그인 실패 알림.
                                    return;

                                }
                                else //로그인 성공이면.
                                {
                                    Add_Command(Command_Server.Updated_User, iuu);
                                    Add_Response(
                                        Lo.Unit_No,
                                        Command_Client.Login_success,
                                        new i_Login_success(
                                            iuu.No,
                                            iuu.is_Reseller,
                                            Session_,
                                            iuu.User_type
                                            )
                                        );
                                }
                            }
                            else
                            {
                                Add_Response(Lo.Unit_No, Command_Client.Login_Failure, i_Login_Failure_type.Invalid_login_information); //로그인 실패 알림.
                            }

                            if (reader != null)
                            {
                                reader.Close();
                                reader = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Login Error : {0}", ex.ToString());
                            if(Lo.Unit_No != -1)
                                Add_Response(Lo.Unit_No, Command_Client.Login_Failure, i_Login_Failure_type.Invalid_login_information); //로그인 실패 알림.
                        }
                    }
                    break;
                default:
                    {
                        Command_Server S = (Command_Server)CMD.Command_code;
                        Console.WriteLine("Unknown Command : {0}", S.ToString());
                    }
                    break;
            }
        }
        
        #region Layer 1 (Commander 메소드)
        

        private static i_Test_connect_order Test_connect_Order(int OrderNO, List<i_Test_connect_order> i_testConnectOrder)
        {
            
            //해당 오더넘버의 테스트 연결 신호를 보내는 메소드
            ///i_testConnectOrder는 이미 테스트를 진행하기로 한 유닛 리스트이다, 한 유닛에 여러개의 테스트가 들어가는걸 막기 위해. 

            string query = "User_type = " + (int)Type_User.Tester_Unit;
            if (i_testConnectOrder != null)
            {
                foreach (i_Test_connect_order it in i_testConnectOrder)
                {
                    query += " and not Unit_No = " + it.Unit_No;
                }
            }
            DataRow[] Enable_User = DB.User.Select(query);
            DataRow[] Order = DB.Order.Select("No = " + OrderNO);
            DataRow[] Enable_Unit = new DataRow[] { };

            if (Enable_User.Length > 0) //테스터 유닛이 하나라도 있을때,
            {
                string Q = "State = " + (int)State_Unit.Enable + " and ( ";
                for (int i = 0; i < Enable_User.Length; i++)
                    if (i == 0)
                        Q += "No = " + (int)Enable_User[i]["Unit_No"];
                    else
                        Q += "or No = " + (int)Enable_User[i]["Unit_No"];
                Q += " )";
                Enable_Unit = DB.Unit.Select(Q);

                if (Enable_Unit.Length <= 0) //없으면..? 임명코드
                {
                    Set_Tester_Unit();
                    //테스터 임명 코드 필요
                    return null;
                }
                else if (Order.Length == 1) //하나라도 있으면. 글고 Order가 존재하면,
                {
                    int Connect_No = Convert.ToInt32(DB.Newindex.generate_index()); //고유 유닛번호를 생성하고
                    int Rand_no = Rand.Next(Enable_Unit.Length);

                    int Unit_no = (int)Enable_Unit[Rand_no]["No"];
                    DB.Connection.Rows.Add(
                        Connect_No,
                        OrderNO,
                        Unit_no, //랜덤하게 유닛 매칭
                        (string)Order[0]["URL"],
                        "",
                        (Type_Order)Order[0]["Type_order"],
                        State_Connect.Connecting,
                        -1,
                        State_Account_on_connected.NotSet,
                        0
                        );
                    //디비에 새 커넥트를 만들고
                    //오더명령.
                    i_connect iconnect = new i_connect(
                        Connect_No,
                        OrderNO,
                        (string)Order[0]["URL"],
                        (Type_Order)Order[0]["Type_order"],
                        ""
                        );

                    if (Testing_List.Has_int_key(OrderNO)) //잇으면 해당 항목에 테스트 커넥션을 추가하여 업데이트하고,
                    {
                        List<i_Test_connect_order> it = (List<i_Test_connect_order>)Testing_List.Select_key(OrderNO);

                        it.Add(new i_Test_connect_order(OrderNO, Unit_no, Connect_No));

                        Testing_List.Update(OrderNO, it);
                    }
                    else //없으면 새로 만들어 넣는다.
                    {
                        Testing_List.Add(OrderNO, new List<i_Test_connect_order> { new i_Test_connect_order(OrderNO, Unit_no, Connect_No) });
                    }
                    Add_Response(Unit_no, Command_Client.order_connect, iconnect);// 연결명령을 보낸다
                    Add_Command(Unit_no, Command_Server.Updated_Unit, State_Unit.Wait); //즉시 상태를 대기로 바꾼다. 
                    return new i_Test_connect_order(OrderNO, Unit_no, Connect_No);
                }
                else
                {
                    throw new Exception("Error Test_connect_Order");
                }
            }
            else //테스트 가능한 유닛이 하나도 없으므로.. 임명.
            {
                Set_Tester_Unit();
                Console.WriteLine("Set Command Tester Uint.");
                return null;
                //throw new Exception("Error Test_connect_Order");
            }
        }

        private static bool Set_Tester_Unit() //랜덤하게 테스트 유닛을 지정한다.
        {
            Random R1 = new Random();
            Random R2 = new Random();
            int unit_no = 0;

            //테스트 유닛, 관리자는 테스터로 임명하지 않도록 쿼리를 만들기 위해 디비에서 테스터, 관리자.
            DataRow[] U = DB.User.Select("User_type = " + (int)Type_User.Tester_Unit + " or User_type = " + (int)Type_User.Administrator);
            string Query = "State = " + (int)State_Unit.Enable;

            if (U.Length > 0)
            {
                foreach (DataRow ul in U)
                {
                    Query += " and not No = " + (int)ul["Unit_No"];
                }
            }

            int loop = R1.Next(10);
            DataRow[] R = DB.Unit.Select(Query);

            if (R.Length > 0)
            {
                for (int i = 0; i < loop; i++)
                    unit_no = (int)R[R2.Next(R.Length)]["No"];
                
                Add_Response(unit_no, Command_Client.Set_Tester, null);
                return true;
            }
            else
                return false;
        }

        private static bool delete_Faliure_counting(int order_no)
        {
            if (Faliure_counting.Has_int_key(order_no))
                Faliure_counting.Remove(order_no);
            else
                return false;
            return true;            
        }

        private static string make_session()
        {
            return MD5HashFunc(Rand.Next().ToString());
        }

        private static string MD5HashFunc(string str)
        {
            StringBuilder MD5Str = new StringBuilder();
            byte[] byteArr = Encoding.ASCII.GetBytes(str);
            byte[] resultArr = (new MD5CryptoServiceProvider()).ComputeHash(byteArr);

            //for (int cnti = 1; cnti < resultArr.Length; cnti++) (2010.06.27)
            for (int cnti = 0; cnti < resultArr.Length; cnti++)
            {
                MD5Str.Append(resultArr[cnti].ToString("X2"));
            }
            return MD5Str.ToString();
        }

        private static void Thread_Update_User_DB_() 
        {
            //현재 사용하지 않는다 디비에 실시간으로 접근해 로그인 계정인지 아닌지 판단한다.

        }

        private static void Thread_Update_Account_DB_()
        {
            try
            {
                int count = 0;
                MYSQL db = new MYSQL();

                db.DB_SELECT("account");
                db.DB_SELECT_WHERE("State", (int)State_Account.Enable); //disable인 계정은 안가져온다.

                MySqlDataReader reader = db.DB_SELECT_RUN();
                while (MYSQL.is_readable(reader))
                {
                    int Site = int.Parse(reader["Site"].ToString());

                    DataRow[] accrow = DB.Account.Select("No = " + int.Parse(reader["No"].ToString()) + " and ID = '" + reader["ID"].ToString() + "'"); //서버 스레드 외에 디비이 접속하게 되는 부분

                    if (accrow.Length <= 0) //없으면 추가한다.
                    {
                        int no = int.Parse(reader["No"].ToString());
                        int Order_no = int.Parse(reader["Order_No"].ToString());
                        string ID = reader["ID"].ToString();
                        string PW = reader["PW"].ToString();

                        DB.Account.Rows.Add(no, Site, Order_no, ID, PW, (int)State_Account.Enable); //서버 스레드 외에 디비에 접속하게 된는 부분. (수정가능)
                        count++;
                    }
                    else //있으면 상태를 보고 변경되었으면 수정해준다.
                    {
                        bool on_count = false;
                        if ((int)accrow[0]["No"] != int.Parse(reader["No"].ToString()))
                        {
                            accrow[0]["No"] = int.Parse(reader["No"].ToString());
                            on_count = true;
                        }
                        if ((string)accrow[0]["ID"] != reader["ID"].ToString())
                        {
                            accrow[0]["ID"] = reader["ID"].ToString();
                        }
                        if ((string)accrow[0]["PW"] != reader["PW"].ToString())
                        {
                            accrow[0]["PW"] = reader["PW"].ToString();
                        }
                        if ((State_Account)accrow[0]["State"] != (State_Account)(int.Parse(reader["State"].ToString())))
                        {
                            accrow[0]["State"] = (State_Account)(int.Parse(reader["State"].ToString()));
                        }

                        if (on_count)
                            count++;
                    }
                }
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
                Console.WriteLine("Updated {0} Login Account.", count);

            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR : Thread_Update_Account_DB : {0}", ex.ToString());
            }
        }

        private static void Thread_Update_Proxy_DB_()
        {
            try
            { 
                int count = 0;
                MYSQL db = new MYSQL();

                db.DB_SELECT("proxyhost");
                db.DB_SELECT_WHERE("state", (int)State_Proxy.Disable, "!="); //disable인 프록시는 안가져온다.

                MySqlDataReader reader = db.DB_SELECT_RUN();
                while (MYSQL.is_readable(reader))
                {
                    string host_ = reader["Host"].ToString();

                    DataRow[] custRows = DB.Proxy.Select("Host = '" + host_ + "'"); //서버 스레드 외에 디비이 접속하게 되는 부분

                    if (custRows.Length <= 0) //없어야 추가..
                    {
                        int no = int.Parse(reader["no"].ToString());
                        string host = reader["Host"].ToString();
                        State_Proxy sp = (State_Proxy)int.Parse(reader["State"].ToString());
                        Type_Proxy tp = (bool)reader["Type"] ? Type_Proxy.Paid : Type_Proxy.Free;
                        DB.Proxy.Rows.Add(no, host, 0, sp, tp); //서버 스레드 외에 디비에 접속하게 된는 부분. (수정가능)
                        count++;
                    }
                }
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
                Console.WriteLine("Updated {0} Proxy Host.", count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR : Thread_Update_Proxy_DB : {0}", ex.ToString());
            }
        }

        private static void Thread_Managing_Tester_(object ProxyList)
        {
            DataRow[] custRows = (DataRow[])ProxyList;
            Queue Proxy_Q_root = new Queue();

            try
            {
                if (custRows.Length > 0)
                {
                    Proxy_Q_root = new Queue();
                    foreach (DataRow R in custRows)
                    {
                        if (Proxy_Q_root.Count >= 10)
                            break;
                        Proxy_Q_root.Enqueue(R["Host"].ToString()); //모든 PL을 넣고..
                    }


                    int Thread_volume = Proxy_Q_root.Count;
                    if (Proxy_Q_root.Count > 0)
                    {
                        Thread[] ThreadList = new Thread[10];
                        for (int i = 0; i < Thread_volume; i++) //10개의 스레드를 사용.
                        {
                            ThreadList[i] = new Thread(new ParameterizedThreadStart(Thread_testing));
                            (ThreadList[i]).Start(Proxy_Q_root.Dequeue());
                        }
                        //for (int i = 0; i < Thread_volume; i++)
                        //    ThreadList[i].Join();
                    }
                }
            }
            catch { }
        }

        private static void Thread_testing(object host_)
        {
            string host = (string)host_;

            Add_Command(Command_Server.Updated_Proxy, new i_Update_Proxy(host, State_Proxy.Wait));

            if (Test_proxy(host)) //테스트 하여..
            {                      
                Add_Command(Command_Server.Updated_Proxy, new i_Update_Proxy(host, State_Proxy.Passed_test));
            }
            else
            {
                Add_Command(Command_Server.Updated_Proxy, new i_Update_Proxy(host, State_Proxy.Disable));
            }
        }

        private static bool Test_proxy(string host)
        {
            //Console.WriteLine("proxy test : {0}", host);
            return Test_proxy(host.Split(':')[0], int.Parse(host.Split(':')[1]));
        }
        private static bool Test_proxy(string host, int port)
        {
            var is_success = false;
            try
            {
                var connsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connsock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 200);
                System.Threading.Thread.Sleep(500);
                var hip = IPAddress.Parse(host);
                var ipep = new IPEndPoint(hip, port);
                connsock.Connect(ipep);
                if (connsock.Connected)
                {
                    is_success = true;
                }
                connsock.Close();
            }
            catch (Exception)
            {
                is_success = false;
            }
            //Console.WriteLine("proxy test : {0}:{1} = {2}", host, port, is_success.ToString());
            return is_success;
        }
        #endregion

        #region Layer 0 (베이스 메소드)

        static void Main(string[] args)
        {
            Set_flag(args);
            Start_Server(20000);
            BlockExit();
        }
        private static void Set_flag(string[] arg) //배열로 부터 설정을 받아 세팅한다.
        {
            foreach (string ar in arg)
            {
                if (ar.StartsWith("-"))
                {
                    switch (ar.ToLower().Trim())
                    {
                        case "-noneproxy": //프록시를 받지 않고 자신이 실행한 운영체제의 아이피를 그냥 쓴다.
                            {
                                //none_proxy = true;
                                Console.WriteLine("None Proxy Mode.");
                            }
                            break;
                        case "-tester": //테스트모드
                            {
                                //tester = true;

                                Console.WriteLine("Tester Mode.");
                            }
                            break;
                        case "-debug": //디버그 모드
                            {
                                //tester = true;
                                Data_structure.is_debug = true;
                                Console.WriteLine("Debug Mode.");
                            }
                            break;
                        default:
                            {
                                Console.WriteLine("Option Error : {0}", ar);
                                return;
                            }
                    }
                }
                else
                {
                    Console.WriteLine("Option Error : {0}", ar);
                    return;
                }
            }
        }
        private static void Start_Server(UInt16 port) //순서가 매우 중요
        {
            DB.DB_init(); //디비 초기화
            Command_Q = new Queue<command_data_server>();
            Command_waitQ = new Queue<command_data_server>();
            Responser_Q = new Queue<command_data_client>();
            m_ConnectedClientList = new mini_database();
            Faliure_counting = new mini_database();
            Testing_List = new mini_database();
            // 비동기 작업에 사용될 대리자를 초기화합니다.
            m_fnReceiveHandler = new AsyncCallback(handleDataReceive);
            m_fnSendHandler = new AsyncCallback(handleDataSend);
            m_fnAcceptHandler = new AsyncCallback(handleClientConnectionRequest);

            Running = true;

            Thread Thread_Commander = new Thread(new ThreadStart(Thread_Commander_));
            Thread_Commander.SetApartmentState(ApartmentState.STA);
            Thread_Commander.Start(); //명령어를 가져와 처리하는 스레드.

            Thread Thread_Responser = new Thread(new ThreadStart(Thread_Responser_));
            Thread_Responser.SetApartmentState(ApartmentState.STA);
            Thread_Responser.Start(); //응답을 처리하는 스레드.

            Thread Thread_Daemon = new Thread(new ThreadStart(Thread_Daemon_));
            Thread_Daemon.SetApartmentState(ApartmentState.STA);
            Thread_Daemon.Start(); //각종 메니저를 처리하는 스레드.
            

            // TCP 통신을 위한 소켓을 생성합니다.
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            // 특정 포트에서 모든 주소로부터 들어오는 연결을 받기 위해 포트를 바인딩합니다.
            m_ServerSocket.Bind(new IPEndPoint(IPAddress.Any, port));

            // 연결 요청을 받기 시작합니다.
            m_ServerSocket.Listen(5); //연결 보류 할 수 있는 최대 클라이언트 개수 대기수가 5를 넘기면 연결 실패로 돌립니다.

            // BeginAccept 메서드를 이용해 들어오는 연결 요청을 비동기적으로 처리합니다.
            // 연결 요청을 처리하는 함수는 handleClientConnectionRequest 입니다.
            m_ServerSocket.BeginAccept(m_fnAcceptHandler, null);
            Console.WriteLine("Listen Client...");
        }

        private static void Received_Command(string data)
        {
            data = data.Replace("}{", "}\r\n{");
            string[] cmd = data.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string data_ in cmd)
            {
                try
                {
                    if (data_.Trim().Length < 5)
                        continue;
                    command_data_server CD = JsonConvert.DeserializeObject<command_data_server>(data_.Trim());
                    
                    lock (Command_waitQ)
                    {
                        Command_waitQ.Enqueue(CD); //추가.
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR Received_command : {0} \r\n EOORO DATA : {1} \r\n data_Length : {2}", ex.ToString(), data_, data_.Length);
                }
            }
        }

        private static void Add_Command(Command_Server command_code, object data)
        {
            lock (Command_Q)
            {
                Command_Q.Enqueue(new command_data_server(-1, command_code, data));
            }
        }
        private static void Add_Command(int sender, Command_Server command_code, object data)
        {
            lock (Command_Q)
            {
                Command_Q.Enqueue(new command_data_server(sender, command_code, data));
            }
        }
        private static void Add_Response(int receiver, Command_Client command_code, object data)
        {
            lock (Responser_Q)
            {
                Responser_Q.Enqueue(new command_data_client(receiver, command_code, data));
            }
        }

        private static void handleClientConnectionRequest(IAsyncResult ar) //연결요청이 들어오면..?
        {
            Socket sockClient = null;
            try
            {
                // 클라이언트의 연결 요청을 수락합니다.
                sockClient = m_ServerSocket.EndAccept(ar);
            }
            catch (Exception ex)
            {
                throw (new Exception("연결 수락 도중 오류 발생! 메세지:" + ex.Message));
            }

            // 4096 바이트의 크기를 갖는 바이트 배열을 가진 AsyncObject 클래스 생성
            AsyncObject ao = new AsyncObject(4096);

            // 작업 중인 소켓을 저장하기 위해 sockClient 할당
            ao.WorkingSocket = sockClient;
            // 클라이언트 소켓 저장

            Add_Command(0, Command_Server.New_connect, sockClient);   //여기엔 새로운 클라이언트 소캣이 파라미터로 넘어가 저장될 것이다.

            try
            {
                // 비동기적으로 들어오는 자료를 수신하기 위해 BeginReceive 메서드 사용!
                sockClient.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnReceiveHandler, ao);
            }
            catch (Exception ex)
            {
                // 예외가 발생하면 예외 정보 출력 후 함수를 종료한다.
                Console.WriteLine("Data Receiving Error 7887:{0}", ex.Message);
            }
            m_ServerSocket.BeginAccept(m_fnAcceptHandler, null);
        }
        private static void handleDataReceive(IAsyncResult ar)
        {

            // 넘겨진 추가 정보를 가져옵니다.
            // AsyncState 속성의 자료형은 Object 형식이기 때문에 형 변환이 필요합니다~!
            AsyncObject ao = (AsyncObject)ar.AsyncState;

            // 받은 바이트 수 저장할 변수 선언
            Int32 recvBytes;

            try
            {
                // 자료를 수신하고, 수신받은 바이트를 가져옵니다.
                recvBytes = ao.WorkingSocket.EndReceive(ar);
            }
            catch
            {
                // 예외가 발생하면 함수 종료!
                return;
            }

            // 수신받은 자료의 크기가 1 이상일 때에만 자료 처리
            if (recvBytes > 0)
            {
                // 공백 문자들이 많이 발생할 수 있으므로, 받은 바이트 수 만큼 배열을 선언하고 복사한다.
                Byte[] msgByte = new Byte[recvBytes];
                Array.Copy(ao.Buffer, msgByte, recvBytes);

                // 받은 메세지를 출력 (여기서 서버의 동작이 실행된다.)
                //Console.WriteLine("Receive code: {0}",Encoding.Unicode.GetString(msgByte));
                //string msg = Encoding.Unicode.GetString(msgByte);
                

                byte[][] ss = Data_structure.Separate(msgByte, Encoding.Unicode.GetBytes("^^^"));

                for (int i = 0; i < ss.Length; i++)
                {
                    if (ss[i].Length < 5)
                        continue;
                    try
                    {
                        string msg = Encoding.Unicode.GetString(GZipCompress.Decompress(ss[i]));
                        if (msg.Trim() != "")
                            Received_Command(msg.Trim());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR 922 Data convert Error: {0}", ex.Message);
                        continue;
                    }
                }
            }

            try
            {
                // 비동기적으로 들어오는 자료를 수신하기 위해 BeginReceive 메서드 사용!
                ao.WorkingSocket.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnReceiveHandler, ao);
            }
            catch (Exception ex)
            {
                // 예외가 발생하면 예외 정보 출력 후 함수를 종료한다.
                Console.WriteLine("자료 수신 대기 도중 오류 발생! 메세지: {0}", ex.Message);
                return;
            }
        }
        private static void handleDataSend(IAsyncResult ar)
        {
            // 넘겨진 추가 정보를 가져옵니다.
            AsyncObject ao = (AsyncObject)ar.AsyncState;

            // 보낸 바이트 수를 저장할 변수 선언
            Int32 sentBytes;

            try
            {
                // 자료를 전송하고, 전송한 바이트를 가져옵니다.
                sentBytes = ao.WorkingSocket.EndSend(ar);
            }
            catch (Exception ex)
            {
                // 예외가 발생하면 예외 정보 출력 후 함수를 종료한다.
                Console.WriteLine("자료 송신 도중 오류 발생! 메세지: {0}", ex.Message);
                return;
            }

            if (sentBytes > 0)
            {
                // 여기도 마찬가지로 보낸 바이트 수 만큼 배열 선언 후 복사한다.
                Byte[] msgByte = new Byte[sentBytes];
                Array.Copy(ao.Buffer, msgByte, sentBytes);

                //Console.WriteLine("메세지 보냄: {0}", Encoding.Unicode.GetString(msgByte));
            }
        }
        private static void BlockExit() //프로그램이 종료되지 않게 한다.
        {
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
        #endregion
    }
}
