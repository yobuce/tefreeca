using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using Tefreeca_Structure;

namespace Tefreeca_Client
{
    class Tefreeca_client_interface
    {
        #region Public Data --------------------
        public static event EventHandler Tefreeca_client_event;
        public static int Unit_No = new int();
        public static int User_No = new int();
        public static State_Unit Unit_State = new State_Unit();

        #endregion

        #region Private Data -------------------
        private static Socket m_ClientSocket = null;
        private static AsyncCallback m_fnReceiveHandler;
        private static AsyncCallback m_fnSendHandler;
        private static Boolean Connected = new Boolean();               //서버와 연결되었는지 확인하는 플래그 
        private static string Session = "";
        private static Type_User Type_user = Type_User.Starter;
        private static Queue<command_data_client> Command_Q;            //명령어 큐 command_data_client
        private static Queue<command_data_client> Command_waitQ;        //명령어 대기 큐
        private static Queue<command_data_server> Responser_Q;          //응답 대기 큐.

        //오더명령 과정에서 필요한 데이터.
        //private static string target_URL = "";
        private static i_Make_Connect this_order;
        private static int this_order_No;
        private static i_User_info User_info;
        #endregion



        private static void Commander(command_data_client CMD)
        {
            Console.WriteLine("Command] {0} : ", ((Command_Client)CMD.Command_code).ToString());
            switch (CMD.Command_code)
            {
                case (int)Command_Client.Set_UnitNo:
                    {
                        Unit_No = Convert.ToInt32(CMD.data);
                        Make_Client_Event(Unit_Event_Type.Change_connect, new List<object> { true });

                        
                    }
                    break;
                case (int)Command_Client.Make_Login:
                    {
                        i_Make_Login login_info = (i_Make_Login)CMD.data;
                        
                        Tefreeca_client_interface.Add_Response(
                            Command_Server.Login,
                            new i_Login(
                                Unit_No,
                                login_info.ID,
                                MD5HashFunc(login_info.PW),
                                Type_User.Starter       //기본값은 스타터이나 로그인하면서 실제 회원등급을 전달받는다.
                                )
                        );
                    }
                    break;
                case (int)Command_Client.Login_success:
                    {
                        i_Login_success ils = JsonConvert.DeserializeObject<i_Login_success>(CMD.data.ToString());

                        User_No = ils.User_No;
                        Unit_State = State_Unit.Logged; //로그드를 사용하지 않고 일반 유닛처럼 운영.
                        Session = ils.Session;
                        Type_user = ils.Type_user;

                        Add_Response(Command_Server.User_Info, null);

                        Make_Client_Event(Unit_Event_Type.Login_success, new List<object> { });
                    }
                    break;
                case (int)Command_Client.Login_Failure:
                    {
                        i_Login_Failure_type Failure_info = JsonConvert.DeserializeObject<i_Login_Failure_type>(CMD.data.ToString());

                        Make_Client_Event(Unit_Event_Type.Login_Failure, new List<object> { Failure_info });
                    }
                    break;
                case (int)Command_Client.User_Info:
                    {
                        User_info = JsonConvert.DeserializeObject<i_User_info>(CMD.data.ToString());

                        Make_Client_Event(Unit_Event_Type.User_info, new List<object> { User_info });
                        //로그인 정보 가져오는 코드.
                        ///그래서 받으면 폼에다 업데이트 할 수 있게 이벤트로 넘겨줌1
                    }
                    break;
                case (int)Command_Client.Order_No:
                    {
                        this_order_No = JsonConvert.DeserializeObject<int>(CMD.data.ToString());

                        Make_Client_Event(Unit_Event_Type.Order_no, this_order_No); //이벤트 발생시키고
                        
                        i_Order io = new i_Order(this_order_No, User_No, this_order.Max_Connect, this_order.URL, Type_Order.Enter_Afreeca_Room); //실제 명령을 만든 후, 

                        Add_Response(Command_Server.Order_Connect, io); //서버로 보낸다.
                    }
                    break;
                case (int)Command_Client.Error_Order:
                    {
                        client_message error_info = JsonConvert.DeserializeObject<client_message>(CMD.data.ToString());
                        
                        Make_Client_Event(Unit_Event_Type.Error_Order, new List<object> { error_info });
                        //로그인 정보 가져오는 코드.
                        ///그래서 받으면 폼에다 업데이트 할 수 있게 이벤트로 넘겨줌1
                    }
                    break;
                case (int)Command_Client.Make_Connect:
                    {
                        i_Make_Connect connect_info = (i_Make_Connect)CMD.data;
                        this_order = connect_info;
                        Add_Response(Command_Server.Order_Request_No, null); //오더 리퀘스트 실행. 그러면 번호를 서버에서 생성하여 Order_No 응답이 온다.
                    }
                    break;
                case (int)Command_Client.Order_Info:
                    {
                        i_Order_Info order_info = JsonConvert.DeserializeObject<i_Order_Info>(CMD.data.ToString());

                        Make_Client_Event(Unit_Event_Type.Order_Info, new List<object> { order_info });
                    }
                    break;
                case (int)Command_Client.Active_Order:
                    {
                        int order_no = JsonConvert.DeserializeObject<int>(CMD.data.ToString());
                        if(this_order_No == order_no)
                            Make_Client_Event(Unit_Event_Type.Active_Order, order_no);
                    }
                    break;
                case (int)Command_Client.Close_Order: //폼 컨르롤로부터 연결 해지 명령을 받을때 사용, 이 명령을 받으면 현재 활성화된 오더를 닫는 명령을 서버로 보낸다.
                    {
                        Add_Response(Command_Server.Order_Disconnect, this_order_No);
                    }
                    break;
                case (int)Command_Client.Make_Order_Set_MaxConnect:
                    {
                        int set_maxConnect = JsonConvert.DeserializeObject<int>(CMD.data.ToString());

                        i_Set_MaxConnect iset = new i_Set_MaxConnect(User_No, this_order_No, set_maxConnect);

                        Add_Response(Command_Server.Order_Set_MaxConnect, iset);
                    }
                    break;
                case (int)Command_Client.R_Order_Set_MaxConnect:
                    {
                        i_R_Order_Set_MaxConnect ir = JsonConvert.DeserializeObject<i_R_Order_Set_MaxConnect>(CMD.data.ToString());
                        Make_Client_Event(Unit_Event_Type.Order_Set_MaxConnect, new List<object> { ir });
                    }
                    break;
                case (int)Command_Client.HeartBeat:
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

        #region Public Method ------------------
        public static void Server_Disconnect()
        {
            m_ClientSocket.Close();
        }

        public Tefreeca_client_interface()
        {
            Unit_No = -1; //초기화.
            Connected = false;

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

        #region Thread --------------------
        private static void Thread_Responser_()
        {
            command_data_server CMD;


            while (Connected)
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
                        //ao.Buffer = GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CMD)));
                        ao.Buffer = Data_structure.Combine(Encoding.Unicode.GetBytes("^^^"), GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CMD))));
                        ao.WorkingSocket = m_ClientSocket;

                        ao.WorkingSocket.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnSendHandler, ao);
                    }
                    catch (Exception ex)
                    {
                        Console.Write("Error Send Message [ Command : {0}, data : {1},  receiver]", CMD.Command_code.ToString(), CMD.data != null ? CMD.data.ToString() : "", CMD.Sender.ToString());
                        Console.Write("Error String: {0}", ex);
                    }
                }
                Thread.Sleep(100);
            }
        }
        private static void Thread_Commander_()
        {
            command_data_client CMD;

            while (Connected)
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
                //(int)Command_Client.Managing_State,
                //(int)Command_Client.Managing_RequestList
                    };
            int[] Term = new int[] { 5, 2 };//각 주기
            int[] T = new int[] { 0, 2 };//카운터.


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
                Command_Q.Enqueue(new command_data_client(-1, command_code_to_client, data));
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

        private void send_command(Command_Server command_code, object data)
        {
            if (Unit_No == -1)
                return;
            AsyncObject ao = new AsyncObject(1);

            command_data_server CD = new command_data_server(Unit_No, command_code, data);

            // 문자열을 바이트 배열으로 변환

            //ao.Buffer = GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CD)));
            ao.Buffer = Data_structure.Combine(Encoding.Unicode.GetBytes("^^^"), GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CD))));

            ao.WorkingSocket = m_ClientSocket;
            // 전송 시작!
            try
            {
                m_ClientSocket.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnSendHandler, ao);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SENDING ERROR: {0}", ex.Message);
                // 서버와 연결이 끊기면 여기서 문제가 생긴다.
                Make_Client_Event(Unit_Event_Type.Server_Connection_Broken, 0);
                Connected = false;
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
                byte[][] ss = Data_structure.Separate(msgByte, Encoding.Unicode.GetBytes("^^^"));

                for (int i = 0; i < ss.Length; i++)
                {
                    if (ss[i].Length < 5)
                        continue;
                    Received_Command(Encoding.Unicode.GetString(GZipCompress.Decompress(ss[i])));
                }

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
        #endregion
    }
}
