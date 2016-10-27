using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Tefreeca_Structure;

namespace Tefreeca_Unit
{
    class TefreecaUnit_interface
    {
        #region Public Data --------------------
        public static event EventHandler Tefreeca_client_event;
        public static int Unit_No = new int();
        public static int User_No = new int();
        public static string Proxy_host = "";
        public static State_Unit Unit_State = new State_Unit();
        public static Boolean Flag_Restart = new Boolean();    //제시작 모드

        #endregion

        #region Private Data -------------------
        private static bool Setted_proxy = false;
        private static bool None_proxy = false;
        private static bool Tester = false;
        private static string Session = "";
        private static Type_User Type_user = Type_User.Starter;
        private static Socket m_ClientSocket = null;
        private static AsyncCallback m_fnReceiveHandler;
        private static AsyncCallback m_fnSendHandler;
        private static Boolean Connected = new Boolean();       //연결되었는지 플래그..
        private static DataTable DB_Connection;
        private static mini_database Faliure_counting; //order_no가 키, count가 값

        private static Queue<command_data_client> Command_Q;            //명령어 큐 command_data_client
        private static Queue<command_data_client> Command_waitQ;        //명령어 대기 큐
        private static Queue<command_data_server> Responser_Q;            //응답 대기 큐.
        #endregion

        #region Public Method ------------------
        public TefreecaUnit_interface(bool Noneproxy, bool tester)
        {
            m_ClientSocket = null;
            DB_Connection = null;
            DB.DB_init_(ref DB_Connection);
            Setted_proxy = false;
            None_proxy = false;
            Unit_No = -1; //초기화.
            User_No = -1;
            Unit_State = State_Unit.Starting;
            Tester = tester;
            None_proxy = Noneproxy;
            Connected = false;
            Flag_Restart = false;
            Type_user = Type_User.Starter;
            Session = "";
            
            Faliure_counting = new mini_database();
            Command_Q = new Queue<command_data_client>();
            Command_waitQ = new Queue<command_data_client>();
            Responser_Q = new Queue<command_data_server>();

            m_fnReceiveHandler = new AsyncCallback(handleDataReceive);
            m_fnSendHandler = new AsyncCallback(handleDataSend);
        }
        public bool is_connected()
        {
            return Connected;
        }
        public bool is_Flag_Restart()
        {
            return Flag_Restart;
        }
        public void StopClient()
        {
            // 가차없이 클라이언트 소켓을 닫습니다.
            m_ClientSocket.Close();
        }
        public void ConnectToServer(String hostName, UInt16 hostPort)
        {
            // TCP 통신을 위한 소켓을 생성합니다.
            m_ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            Boolean isConnected = false;
            try
            {
                // 연결 시도
                m_ClientSocket.Connect(hostName, hostPort);

                // 연결 성공
                isConnected = true;
            }
            catch
            {
                // 연결 실패 (연결 도중 오류가 발생함)
                isConnected = false;
            }
            Connected = isConnected;

            if (isConnected)
            {
                // 4096 바이트의 크기를 갖는 바이트 배열을 가진 AsyncObject 클래스 생성
                AsyncObject ao = new AsyncObject(4096);

                // 작업 중인 소켓을 저장하기 위해 sockClient 할당
                ao.WorkingSocket = m_ClientSocket;

                // 비동기적으로 들어오는 자료를 수신하기 위해 BeginReceive 메서드 사용!
                m_ClientSocket.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnReceiveHandler, ao);
                Start_Thread();
                Console.WriteLine("Successful connection!");
            }
            else
            {
                Console.WriteLine("Connection Failed..");
            }
        }
        #endregion

        private void Start_Thread()
        {
            Thread Thread_Commander = new Thread(new ThreadStart(Thread_Commander_));
            Thread_Commander.SetApartmentState(ApartmentState.STA);
            Thread_Commander.Start(); //명령어를 가져와 처리하는 스레드.

            Thread Thread_Responser = new Thread(new ThreadStart(Thread_Responser_));
            Thread_Responser.SetApartmentState(ApartmentState.STA);
            Thread_Responser.Start(); //응답을 처리하는 스레드.

            Thread Thread_Daemon = new Thread(new ThreadStart(Thread_Daemon_));
            Thread_Daemon.SetApartmentState(ApartmentState.STA);
            Thread_Daemon.Start(); //각종 메니저를 처리하는 스레드.
        }
        private static void Commander(command_data_client CMD)
        {
            //Console.WriteLine("Command] {0} : ", ((Command_Client)CMD.Command_code).ToString());
            switch (CMD.Command_code)
            {
                case (int)Command_Client.Updated_Connection_Account: //현재 로그인할수 있는 연결들을 관리하는 메니저.
                    {
                        i_Updated_Connection_Account iuca = (i_Updated_Connection_Account)CMD.data; //내부에서 호출하기 땜에 시리얼라이즈하지 않는다.

                        DataRow[] C = DB_Connection.Select("No = " + iuca.Connect_No);
                        if (C.Length == 1)
                        {
                            C[0]["State_Account_on_connected"] = iuca.State;
                        }
                    }
                    break;
                case (int)Command_Client.Managing_Account: //현재 로그인할수 있는 연결들을 관리하는 메니저.
                    {
                        //Trying_Login 상태인 연결들을 찾아 로그인 체크,
                        DataRow[] C = DB_Connection.Select("State_Account_on_connected =" + (int)State_Account_on_connected.Trying_Login);
                        if (C.Length > 0)
                        {
                            foreach (DataRow Cnt in C)
                            {
                                Make_Client_Event(Unit_Event_Type.Account_Check_Login, (int)Cnt["No"]);
                            }
                        }

                        //상태가 setting 인 연결을 찾아 새로그인을 시킨다.
                        C = DB_Connection.Select("State_Account_on_connected =" + (int)State_Account_on_connected.Setting);
                        if (C.Length > 0)
                        {
                            foreach (DataRow Cnt in C)
                            {
                                i_Account_Set IAS = new i_Account_Set(
                                    (Type_Order)Cnt["Type_order"],
                                    (int)Cnt["No"],
                                    (int)Cnt["Login_Account_no"],
                                    (string)Cnt["Account_ID"],
                                    (string)Cnt["Account_PW"],
                                    (int)Cnt["Order_No"]
                                    );
                                Cnt["State_Account_on_connected"] = State_Account_on_connected.Trying_Login;
                                Make_Client_Event(Unit_Event_Type.Account_Login, new List<object> { IAS });
                            }
                        }
                        //Failure_Login 인 연결은 글쌔..
                    }
                    break;
                case (int)Command_Client.Account_Set: //개별적인 연결에 로그인 계정을 임명하는 명령어. 
                    {
                        //Add_Response((int)candidate[candidate_No]["Unit_No"], Command_Client.Account_Set, ias);
                        i_Account_Set ils = JsonConvert.DeserializeObject<i_Account_Set>(CMD.data.ToString());

                        DataRow[] C = DB_Connection.Select("No =" + ils.Connect_No);

                        if (C.Length != 1)
                            return;

                        C[0]["Login_Account_no"] = ils.Account_No;
                        C[0]["Account_ID"] = ils.ID;
                        C[0]["Account_PW"] = ils.PW;
                        C[0]["State_Account_on_connected"] = State_Account_on_connected.Setting; //첫 세팅.

                        //
                        //여기서 직접 로긴 명령을 내리는것이 아니라 메니저에서 별도로 로긴명령을 내린다.
                        //디비에 정보를 저장하고
                        //이벤트로 크롬드라이브에 로그인 명령을 내린다
                    }
                    break;
                case (int)Command_Client.Close_connect:
                    {
                        int connect_no = int.Parse(CMD.data.ToString());

                        DataRow[] CC = DB_Connection.Select(
                            "No = " + connect_no + " and not State = " + (int)State_Connect.Closing +
                            " and not State = " + (int)State_Connect.Close
                            ); //이미 닫혔거나 닫고 있지 않은경우 실행한다.
                        if (CC.Length == 1)
                        {
                            Make_Client_Event(
                                    Unit_Event_Type.Disconnect_connect,
                                    new List<object> {new i_connect(
                                            (int)CC[0]["No"],
                                            (int)CC[0]["Order_No"],
                                            (string)CC[0]["Connect_URL"],
                                            (Type_Order)CC[0]["Type_order"],
                                            (string)CC[0]["Proxy_host"]) }
                                    );
                        }
                    }
                    break;
                case (int)Command_Client.Set_UnitNo:
                    {
                        int No = Convert.ToInt32(CMD.data);
                        
                        Unit_No = No;
                        Console.WriteLine("Set Unit No] {0}.", Unit_No.ToString());

                        if (None_proxy) //자신의 아이피를 프록시 서버 호스트로 지정.
                            Add_Command(Command_Client.Set_Proxy, Client_IP);
                        else
                            Unit_State = State_Unit.Wait_proxy;

                        Console.WriteLine("Updated State_Unit [{0}]", Unit_State.ToString());
                    }
                    break;
                case (int)Command_Client.Login_success:
                    {
                        i_Login_success ils = JsonConvert.DeserializeObject<i_Login_success>(CMD.data.ToString());

                        User_No = ils.User_No;
                        //Unit_State = State_Unit.Logged;
                        Session = ils.Session;
                        Type_user = ils.Type_user;
                    }
                    break;
                case (int)Command_Client.Set_Proxy: //서버로 부터 프록시 배정 신호가 온다.
                    {
                        string host = (string)CMD.data;

                        if (None_proxy) //다이렉트 아이피 모드
                        {
                            Proxy_host = host;
                            Setted_proxy = true;
                            Console.WriteLine("Setted Direct ip : {0}.", Proxy_host);
                        }
                        else if (host.Length > 8 && Test_proxy(host))
                        {
                            Proxy_host = host;
                            Setted_proxy = true;
                            Console.WriteLine("Setted Proxy : {0}.", Proxy_host);
                            Add_Response(Command_Server.Sucsses_Proxy, host);
                        }
                        else
                        {
                            Setted_proxy = false;
                            Unit_State = State_Unit.Wait_proxy;
                            Add_Response(Command_Server.Failure_Proxy, host);
                            Console.WriteLine("Error Set_Proxy : {0}.", host);
                        }
                    }
                    break;
                case (int)Command_Client.Managing_State: //일단 현재의 상태를 주기적으로 보내는 것으로 하자.
                    {
                        ///=====================================================
                        /// 유닛의 상태를 자동으로 결정하는코드
                        if (Unit_No < 0)
                            return;


                        if (Setted_proxy)
                        {
                            DataRow[] C = DB_Connection.Select("State = " + (int)State_Connect.Connecting
                            + " or State = " + (int)State_Connect.Wait + "");
                            if(Unit_State != State_Unit.Logged && Proxy_host != "")
                                if (C.Length > 0)
                                    Unit_State = State_Unit.Ordering; //연결 상태를 오더 처리중으로 변경.
                                else
                                    Unit_State = State_Unit.Enable; //연결 상태를 오더 처리중으로 변경.
                        }
                        
                        //==========================================
                        //현재의 상태를 주기적으로 보내는 코드.

                        i_State istate = new i_State();

                        istate.State_unit = Unit_State;
                        istate.State_connect = new List<i_Updated_Connection>();

                        foreach (DataRow R in DB_Connection.Rows) //모든 내용.
                        {
                            i_Updated_Connection iuc = new i_Updated_Connection((int)R["No"], i_Updated_Connection_Type.State_Connect, (State_Connect)R["State"]);
                            istate.State_connect.Add(iuc); //모든 커넥션을 추가.

                            if (R["State_Account_on_connected"] != null)
                            {
                                i_Updated_Connection iuc2 = new i_Updated_Connection((int)R["No"], i_Updated_Connection_Type.State_Account_on_connected, (State_Account_on_connected)R["State_Account_on_connected"]);
                                istate.State_connect.Add(iuc2); //모든 커넥션을 추가.
                            }

                            if (R["Login_Account_no"] != null)
                            {
                                i_Updated_Connection iuc3 = new i_Updated_Connection((int)R["No"], i_Updated_Connection_Type.Login_Account_no, (int)R["Login_Account_no"]);
                                istate.State_connect.Add(iuc3); //모든 커넥션을 추가.
                            }

                            //반으시 닫힘을 알리고 삭제되어야 하므로Managing_connecting에서 별도 처리 하기 보다 한번에 여기서 순서에 맞춰 먼저 보내고 삭제,
                            if ((int)R["State"] == (int)State_Connect.Close) //만약 종료되었음을 알리는 신호라면,
                            {
                                Console.WriteLine("Deleted Connection [C[{0}]", iuc.Connect_No);
                                DB_Connection.Rows.Remove(R); //정말 지워지는지 검증필요
                                break;
                            }

                        }
                        Add_Response(Command_Server.State, istate);
                    }
                    break;
                case (int)Command_Client.Managing_connecting:
                    {
                        //연결되어 있는 브라우저의 방송이 종료되었는지 체크하여
                        //종료되었으면 방송을 닫는다.

                        DataRow[] Cr = DB_Connection.Select("State = " + (int)State_Connect.Connected);
                        List<object> i_connect_list = new List<object>();

                        foreach (DataRow R in Cr) //연결된 상태에 있는 커넥션들을 가져와서 이벤트를 발생시킨다.
                        {
                            i_connect ic = new i_connect(
                                            (int)R["No"],
                                            (int)R["Order_No"],
                                            (string)R["Connect_URL"],
                                            (Type_Order)R["Type_order"],
                                            (string)R["Proxy_host"]);
                            i_connect_list.Add(ic);
                        }

                        try
                        {
                            Make_Client_Event(Unit_Event_Type.Check_End_broadcast, i_connect_list);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        //실패한 연결을 닫는 명령을 내린다.
                        foreach (DataRow C in DB_Connection.Rows)
                        {
                            if((State_Connect)C["State"] == State_Connect.Failure)
                            {
                                Make_Client_Event(
                                    Unit_Event_Type.Disconnect_connect, 
                                    new List<object> {new i_connect(
                                            (int)C["No"],
                                            (int)C["Order_No"],
                                            (string)C["Connect_URL"],
                                            (Type_Order)C["Type_order"],
                                            (string)C["Proxy_host"]) }
                                    );

                            }
                        }

                    }
                    break;
                case (int)Command_Client.set_proxy_success_ok: //일단 현재의 상태를 주기적으로 보내는 것으로 하자.
                    {
                        if (Tester)
                        {
                            Add_Response(Command_Server.Login, new i_Login(
                                Unit_No,
                                "Test_" + Unit_No,
                                "",
                                Type_User.Tester_Unit
                                ));
                        }
                        
                        Unit_State = State_Unit.Enable; //사용가능 상태로 바꿈.
                    }
                    break;
                case (int)Command_Client.order_connect: //연결 명령.
                    {
                        i_connect C = JsonConvert.DeserializeObject<i_connect>(CMD.data.ToString());

                        //Unit_State = State_Unit.Ordering; //연결 상태를 오더 처리중으로 변경.

                        DB_Connection.Rows.Add(C.No, C.Order_No, C.URL, Proxy_host, C.order_type, State_Connect.Connecting, -1,"","", State_Account_on_connected.NotSet);
                        //디비에 등록.
                        try
                        {
                            if (Tefreeca_client_event != null)
                            {
                                Tefreeca_client_EventArgs TC_EvtArgs = new Tefreeca_client_EventArgs();
                                TC_EvtArgs.type = Unit_Event_Type.Order_Connect;

                                i_connect iC = JsonConvert.DeserializeObject<i_connect>(CMD.data.ToString());

                                if(iC.Proxy.Length < 6) //프록시가 지정되어 있지 않은 채 명령이 들어오면 유닛에 지정된 ip를 사용한다.
                                    iC.Proxy = Proxy_host;

                                TC_EvtArgs.data_string = JsonConvert.SerializeObject(iC);
                                
                                // TC_EvtArgs.Data_object_list = new List<object>(new object[] { C }); //connect가 들어있는 리스트를 매개변수로 넘김.
                                Tefreeca_client_event(null, TC_EvtArgs);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                    break;
                case (int)Command_Client.Updated_Connection:
                    {
                        i_Updated_Connection iuc = (i_Updated_Connection)CMD.data;

                        switch (iuc.Updated_Connection_Type)
                        {
                            case i_Updated_Connection_Type.State_Connect:
                                {
                                    State_Connect State_connect = (State_Connect)iuc.Data;
                                    DataRow[] C = DB_Connection.Select("No = " + iuc.Connect_No + "");

                                    if (C.Length == 1)
                                    {
                                        if ((int)C[0]["State"] == (int)State_connect)
                                            return; //변동없으면 패스.

                                        if ((int)C[0]["State"] == (int)State_Connect.Connecting && //연결상태가 연결중에서 종료쪽으로 바뀌면,
                                            (   State_connect == State_Connect.Failure  ||
                                                State_connect == State_Connect.Close    ||
                                                State_connect == State_Connect.Closing  ))
                                        {
                                            Make_Client_Event(Unit_Event_Type.Connect_sideBreak, iuc.Connect_No);
                                        }

                                        C[0]["State"] = (int)State_connect;
                                        Console.WriteLine("Updated Connection [C[{0}] {1}]", iuc.Connect_No, State_connect.ToString());

                                        if (State_connect == State_Connect.Failure) //연결 실패로 바꾸는 경우.
                                        {
                                            Add_Command(Command_Client.Failure_Counter, (int)C[0]["Order_No"]); //실패 카운트함.
                                        }
                                    }
                                    else
                                        Console.WriteLine("Error Updated_Connection.");
                                }
                                break;
                            case i_Updated_Connection_Type.Login_Account_no:
                                {
                                }
                                break;
                            case i_Updated_Connection_Type.State_Account_on_connected:
                                {
                                }
                                break;

                        }
                    }
                    break;
                case (int)Command_Client.Failure_Counter:
                    {
                        int order_no = (int)CMD.data;

                        ///order에 해당하는 엔트리가 있는지 Faliure_counting에서 검사해 
                        ///Faliure_counting에 있으면
                        ///발생한 애러의 order를 가지고 Faliure_counting에서 조회해 해당 카운트를 올린다.
                        ///없으면 새로 생성,
                        ///Faliure_counting를 관리하는데.. 
                        ///connected인 상태가 하나라도 있으면 삭제,
                        ///
                        ///10을 넘어가는 카운트가 있으면
                        ///실패신호를 보내게 된다. 그리고나면 삭제.
                        ///그리고 실패신호를 저장해두었다가  10회가 넘으면 해당 order를 종료시킨다.

                        if (Faliure_counting.Has_int_key(order_no)) //키가 존재하면, +1한 카운트로 업데이트 하고,
                            Faliure_counting.Update(order_no, (int)Faliure_counting.Select_key(order_no) + 1);
                        else //없으면
                            Faliure_counting.Insert(order_no, 1); //새 테이블을 생성.

                        DataRow[] R = DB_Connection.Select("Order_No = " + order_no + " and State = " + (int)State_Connect.Connected);
                        //해당 order중 연결된 상태의 커넥션이 있다면
                        if (R.Length > 0)
                            if (Faliure_counting.Has_int_key(order_no)) //키가 존재하면, 
                                Faliure_counting.Remove(order_no);  //삭제.

                        if (Faliure_counting.Has_int_key(order_no)) //키가 존재하면, +1한 카운트로 업데이트 하고,
                            if ((int)Faliure_counting.Select_key(order_no) >= 5) //5가 되면,
                                Add_Response(Command_Server.Failure_Connect, order_no);
                    }
                    break;
                case (int)Command_Client.Change_Proxy: //연결실패로 프록시를 바꾸자 하면..
                    {
                        ///모든 연결을 닫고, 그럴 필요 없다. 이미 접속된 연결을 그대로 두고,
                        /// 실패한 접속은 자동으로 닫힐테고,
                        /// 프록시 실패 명령을 보내면 서버는 그 프록시를 사용불가 처리 할 것이다
                        /// 그리고 상태를 wait_proxy로 바꾸면 새로운 프록시가 배정된다.
                        /// 그리고 다시 돌아갈 수 있다.

                        if(Proxy_host != "")
                            Add_Response(Command_Server.Failure_Proxy, Proxy_host);
                        else
                            Console.WriteLine("Error Command_Client.Change_Proxy, host:{0}", Proxy_host.ToString());


                        Unit_State = State_Unit.Wait_proxy;
                    }
                    break;
                case (int)Command_Client.Set_Tester: //연결실패로 프록시를 바꾸자 하면..
                    {
                        Console.WriteLine("Set_Tester_Unit.");
                        if (!Tester)
                        {
                            Tester = true;
                            Add_Response(Command_Server.Login, new i_Login(
                                    Unit_No,
                                    "Test_" + Unit_No,
                                    "",
                                    Type_User.Tester_Unit
                                    ));
                            //모든 연결을 닫는 코드 필요.
                        }
                    }
                    break;
                case (int)Command_Client.Order_Info: //연결실패로 프록시를 바꾸자 하면..
                    {

                    }
                    break;
                case (int)Command_Client.HeartBeat: //연결실패로 프록시를 바꾸자 하면..
                    {
                        
                    }
                    break;
                default:
                    {
                        Command_Client S = (Command_Client)CMD.Command_code;
                        Console.WriteLine("Unknown Command : {0}", S.ToString());
                    }
                    break;
            }
        }
        
        #region Thread --------------------
        private static void Thread_Responser_()
        {
            command_data_server CMD;


            while (Connected && !Flag_Restart)
            {
                while (Responser_Q.Count > 0)
                {
                    lock (Responser_Q)
                    {
                        CMD = (command_data_server)Responser_Q.Dequeue();
                    }
                    try
                    {
                        AsyncObject ao = new AsyncObject(1);
                        // 문자열을 바이트 배열으로 변환
                        ao.Buffer = Data_structure.Combine(Encoding.Unicode.GetBytes("^^^"), GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CMD))));
                        //ao.Buffer = GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CMD)));

                        ao.WorkingSocket = m_ClientSocket;
                        
                        ao.WorkingSocket.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnSendHandler, ao);

                    }
                    catch (Exception ex)
                    {
                        Flag_Restart = true;
                        Console.Write("Error Send Message [ Command : {0}, data : {1},  receiver]", CMD.Command_code.ToString(), CMD.data.ToString(), CMD.Sender.ToString());
                        Console.Write("Error String: {0}", ex);
                    }
                }
                Thread.Sleep(100);
            }
        }
        private static void Thread_Commander_()
        {
            command_data_client CMD;

            while (Connected && !Flag_Restart)
            {
                lock (Command_waitQ) //락걸고 들어가.
                {
                    lock (Command_Q)
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
                        CMD = (command_data_client)Command_Q.Dequeue();
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
                            Console.Write("Error [ Command : {0}, Sender : {1}, data : {2}]", ((Command_Client)CMD.Command_code).ToString(), CMD.Receiver.ToString(), CMD.data != null ? CMD.data.ToString() : "");
                            Console.Write("Error String: {0}", ex);

                            //애러를 클라이언트에게 보내주는 코드가 필요할지도 모른다. 1.25
                        }
                    }
                }
                Thread.Sleep(100);
            }
        }
        private static void Thread_Daemon_() //상태 변경을 처리하는 스레드
        {
            //주기적으로 상태를 체크, 변동사항을 보내는 코드.
            //실제로 보내는게 아니라 그일을 수행하도록 명령어를 클라이언트로 집어넣는다.
            //연결상태를 관리하는 메니저를 부르는 코드를 집어넣는다.
            int[] Managing = new int[]{
                (int)Command_Client.Managing_State,
                (int)Command_Client.Managing_connecting,
                (int)Command_Client.Managing_Account
                    };
            int[] Term = new int[] { 5, 5, 5};//각 주기
            int[] T = new int[] { 10, 10, 10};//카운터.


            while (Connected)
            {
                Thread.Sleep(1000);

                for (int i = 0; i < Managing.Length; i++)
                {
                    if (T[i] <= 0)
                    {
                        Add_Command((Command_Client)Managing[i], null);
                        T[i] = Term[i];
                    }
                    else
                        T[i] = T[i] - 1;
                }
            }
        }
        #endregion

        #region Layer 0.-------------------
        private static bool Make_Client_Event(Unit_Event_Type Type, List<object> Data)
        {
            if (Tefreeca_client_event != null) //이벤트 구독자가 있어야만 발생..
            {
                Tefreeca_client_EventArgs TC_EvtArgs = new Tefreeca_client_EventArgs();
                TC_EvtArgs.type = Type;
                TC_EvtArgs.Data_object_list = Data;
                Tefreeca_client_event(null, TC_EvtArgs);
                return true;
            }
            else
                return false;
        }
        private static bool Make_Client_Event(Unit_Event_Type Type, int Data)
        {
            if (Tefreeca_client_event != null) //이벤트 구독자가 있어야만 발생..
            {
                Tefreeca_client_EventArgs TC_EvtArgs = new Tefreeca_client_EventArgs();
                TC_EvtArgs.type = Type;
                TC_EvtArgs.data_int = Data;
                Tefreeca_client_event(null, TC_EvtArgs);
                return true;
            }
            else
                return false;
        }
        private static bool Make_Client_Event(Unit_Event_Type Type, string Data)
        {
            if (Tefreeca_client_event != null) //이벤트 구독자가 있어야만 발생..
            {
                Tefreeca_client_EventArgs TC_EvtArgs = new Tefreeca_client_EventArgs();
                TC_EvtArgs.type = Type;
                TC_EvtArgs.data_string = Data;
                Tefreeca_client_event(null, TC_EvtArgs);
                return true;
            }
            else
                return false;
        }
        private static void Add_Command(int sender, Command_Client command_code_to_client, object data) 
        {
            lock (Command_Q)
            {
                Command_Q.Enqueue(new command_data_client(sender, command_code_to_client, data));
            }
        }
        public static void Add_Command(Command_Client command_code_to_client, object data)
        {
            lock (Command_Q)
            {
                Command_Q.Enqueue(new command_data_client(0, command_code_to_client, data));
            }
        }
        private static void Add_Response(int receiver, Command_Server command_code_to_server, object data)
        {
            lock (Responser_Q)
            {
                Responser_Q.Enqueue(new command_data_server(receiver, command_code_to_server, data));
            }
        }
        public static void Add_Response(Command_Server command_code_to_server, object data)
        {
            lock (Responser_Q)
            {
                Responser_Q.Enqueue(new command_data_server(Unit_No, command_code_to_server, data));
            }
        }

        private void send_command(Command_Server command_code, object data) //즉시 보낸다.
        {
            if (Unit_No == -1)
                return;
            AsyncObject ao = new AsyncObject(1);

            command_data_server CD = new command_data_server(Unit_No, command_code, data);

            // 문자열을 바이트 배열으로 변환
            ao.Buffer = Data_structure.Combine(Encoding.Unicode.GetBytes("^^^"), GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CD))));
            //ao.Buffer = GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CD)));
            ao.WorkingSocket = m_ClientSocket;
            // 전송 시작!
            try
            {
                m_ClientSocket.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnSendHandler, ao);
            }
            catch (Exception ex)
            {
                Console.WriteLine("전송 중 오류 발생!\n메세지: {0}", ex.Message);
                Flag_Restart = true;
            }

        }
        private void Received_Command(string data)
        {
            data = data.Replace("}{", "}\r\n{");
            string[] cmd = data.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string data_ in cmd)
            {
                try
                {
                    if (data_.Trim().Length < 5)
                        continue;
                    command_data_client CD = JsonConvert.DeserializeObject<command_data_client>(data_);
                    
                    Add_Command((Command_Client)CD.Command_code, CD.data); //명령어를 하나씩 추가한다.
                }
                catch (Exception ex)
                {
                    Console.Write("명령어 해석 애러: {0}", ex);
                }
            }
        }
        private void handleDataReceive(IAsyncResult ar)
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
                //string msg = Encoding.Unicode.GetString(msgByte);
                //string msg = Encoding.Unicode.GetString(GZipCompress.Decompress(msgByte));
                // 받은 메세지를 출력

                /////
                byte[][] ss = Data_structure.Separate(msgByte, Encoding.Unicode.GetBytes("^^^"));

                for (int i = 0; i < ss.Length; i++)
                {
                    if (ss[i].Length < 5)
                        continue;
                    Received_Command(Encoding.Unicode.GetString(GZipCompress.Decompress(ss[i])));
                }
                ////
                //Received_Command(msg);
            }

            try
            {
                // 자료 처리가 끝났으면~
                // 이제 다시 데이터를 수신받기 위해서 수신 대기를 해야 합니다.
                // Begin~~ 메서드를 이용해 비동기적으로 작업을 대기했다면
                // 반드시 대리자 함수에서 End~~ 메서드를 이용해 비동기 작업이 끝났다고 알려줘야 합니다!
                ao.WorkingSocket.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnReceiveHandler, ao);
            }
            catch (Exception ex)
            {
                // 예외가 발생하면 예외 정보 출력 후 함수를 종료한다.
                Console.WriteLine("자료 수신 대기 도중 오류 발생! 메세지: {0}", ex.Message);
                return;
            }
        }
        
        private void handleDataSend(IAsyncResult ar)
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
        private static string Client_IP //내 아이피 알아내기.
        {
            get
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                string ClientIP = string.Empty;
                for (int i = 0; i < host.AddressList.Length; i++)
                {
                    if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        ClientIP = host.AddressList[i].ToString();
                    }
                }
                return ClientIP;
            }
        }
        #endregion
    }
}
