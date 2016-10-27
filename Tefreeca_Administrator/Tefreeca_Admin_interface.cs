using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using Tefreeca_Structure;

namespace Tefreeca_Administrator
{
    class Tefreeca_Admin_interface
    {
        #region Public Data --------------------
        public static event EventHandler Tefreeca_client_event;
        public static int Unit_No = new int();
        public static int User_No = new int();
        public static State_Unit Unit_State = new State_Unit();
        public static Boolean Flag_Restart = new Boolean();    //제시작 모드

        #endregion

        #region Private Data -------------------
        private static Socket m_ClientSocket = null;
        private static AsyncCallback m_fnReceiveHandler;
        private static AsyncCallback m_fnSendHandler;
        private static Boolean Connected = new Boolean();       //연결되었는지 플래그..
        private static string Session = "";
        private static Type_User Type_user = Type_User.Starter;
        private static Queue<command_data_client> Command_Q;            //명령어 큐 command_data_client
        private static Queue<command_data_client> Command_waitQ;        //명령어 대기 큐
        private static Queue<command_data_server> Responser_Q;            //응답 대기 큐.
        private static int current_View_page = 0;
        #endregion

        #region Public Method ------------------
        public Tefreeca_Admin_interface()
        {
            Unit_No = -1; //초기화.
            Connected = false;
            Flag_Restart = false;

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
            Console.WriteLine("Command] {0} : ", ((Command_Client)CMD.Command_code).ToString());
            switch (CMD.Command_code)
            {
                case (int)Command_Client.Set_UnitNo:
                    {
                        int No = Convert.ToInt32(CMD.data);

                        Unit_No = No;

                        Add_Response(
                            Command_Server.Login,
                            new i_Login(
                                Unit_No,
                                "Administrator", 
                                MD5HashFunc("tyn787ft"), 
                                Type_User.Administrator
                                )
                        );
                        Console.WriteLine("Login..");
                    }
                    break;
                case (int)Command_Client.Login_success:
                    {
                        i_Login_success ils = JsonConvert.DeserializeObject<i_Login_success>(CMD.data.ToString());

                        User_No = ils.User_No;
                        Unit_State = State_Unit.Logged;
                        Session = ils.Session;
                        Type_user = ils.Type_user;
                    }
                    break;
                case (int)Command_Client.Login_Failure: 
                    {
                      
                    }
                    break;
                case (int)Command_Client.List_ServerInfo:
                    {
                        i_List_ServerInfo DR = JsonConvert.DeserializeObject<i_List_ServerInfo>(CMD.data.ToString());
                        Make_Client_Event(Unit_Event_Type.List_ServerInfo, new List<object> { DR });
                    }
                    break;
                case (int)Command_Client.List_Connect:
                    {
                        List<i_List_Connect> DR = JsonConvert.DeserializeObject<List<i_List_Connect>>(CMD.data.ToString());
                        Make_Client_Event(Unit_Event_Type.List_Connect, new List<object> { DR });
                    }
                    break;
                case (int)Command_Client.List_Account:
                    {
                        List<i_List_Account> DR = JsonConvert.DeserializeObject<List<i_List_Account>>(CMD.data.ToString());
                        Make_Client_Event(Unit_Event_Type.List_Account, new List<object> { DR });
                    }
                    break;
                case (int)Command_Client.List_Order:
                    {
                        List<i_List_Order> DR = JsonConvert.DeserializeObject<List<i_List_Order>>(CMD.data.ToString());
                        Make_Client_Event(Unit_Event_Type.List_Order, new List<object> { DR });
                    }
                    break;
                case (int)Command_Client.List_Proxy:
                    {
                        List<i_List_Proxy> DR = JsonConvert.DeserializeObject<List<i_List_Proxy>>(CMD.data.ToString());
                        Make_Client_Event(Unit_Event_Type.List_Proxy, new List<object> { DR });
                    }
                    break;
                case (int)Command_Client.List_Unit:
                    {
                        List<i_List_Unit> DR = JsonConvert.DeserializeObject<List<i_List_Unit>>(CMD.data.ToString());
                         Make_Client_Event(Unit_Event_Type.List_Unit, new List<object> { DR });
                    }
                    break;
                case (int)Command_Client.List_User:
                    {
                        List<i_List_User> DR = JsonConvert.DeserializeObject<List<i_List_User>>(CMD.data.ToString());

                        Make_Client_Event(Unit_Event_Type.List_User, new List<object> { DR });
                    }
                    break;
                case (int)Command_Client.Managing_State: //일단 현재의 상태를 주기적으로 보내는 것으로 하자.
                    {
                        i_State istate = new i_State();

                        istate.State_unit = Unit_State;
                        istate.State_connect = new List<i_Updated_Connection>();
                        
                        Add_Response(Command_Server.State, istate);
                    }
                    break;
                case (int)Command_Client.Managing_RequestList: //현재 보고 있는 화면의 정보를 주기적으로 요청하는 명령어.
                    {
                        int[] Managing = new int[]{
                            (int)Command_Server.List_Order,
                            (int)Command_Server.List_User,
                            (int)Command_Server.List_Unit,
                            (int)Command_Server.List_Connect,
                            (int)Command_Server.List_Proxy,
                            (int)Command_Server.List_Account,
                            };

                        if (Managing.Length > current_View_page)
                            Add_Response((Command_Server)Managing[current_View_page], null);

                        Add_Response(Command_Server.List_ServerInfo, null);
                    }
                    break;
                case (int)Command_Client.Updated_current_View_page: //현재 보고 있는 화면의 정보를 주기적으로 요청하는 명령어.
                    {
                        current_View_page = (int)CMD.data;
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
                        //ao.Buffer = GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CMD)));
                        ao.Buffer = Data_structure.Combine(Encoding.Unicode.GetBytes("^^^"), GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CMD))));
                        ao.WorkingSocket = m_ClientSocket;

                        ao.WorkingSocket.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnSendHandler, ao);
                    }
                    catch (Exception ex)
                    {
                        Console.Write("Error Send Message [ Command : {0}, data : {1},  receiver]", CMD.Command_code.ToString(), CMD.data != null ? CMD.data.ToString() : "" , CMD.Sender.ToString());
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
                (int)Command_Client.Managing_RequestList
                    };
            int[] Term = new int[] { 5, 2  };//각 주기
            int[] T = new int[] { 0, 2};//카운터.


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

            try {
                // 문자열을 바이트 배열으로 변환
                //ao.Buffer = GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CD)));
                ao.Buffer = Data_structure.Combine(Encoding.Unicode.GetBytes("^^^"), GZipCompress.Compress(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(CD))));
                ao.WorkingSocket = m_ClientSocket;
            }
            catch(Exception ex)
            {
                Console.WriteLine("전송 준비중 오류!\n메세지: {0}", ex.Message);
                return;
            }

            try
            {
                
                // 전송 시작!
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
                byte[][] ss = Data_structure.Separate(msgByte, Encoding.Unicode.GetBytes("^^^"));

                for (int i = 0; i < ss.Length; i++)
                {
                    if (ss[i].Length < 5)
                        continue;
                    try
                    {
                        string msg = Encoding.Unicode.GetString(GZipCompress.Decompress(ss[i]));
                        if (msg.Trim() != "")
                            Received_Command(msg);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("수신 데이터 해석 중 오류.: {0}", ex.Message);

                    }
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
    }
}

