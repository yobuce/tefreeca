using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Diagnostics;
using System.Threading;
using Tefreeca_Structure;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Tefreeca_Unit
{
    class TefreecaUnit
    {
        #region <프로세스 컨트롤을 위한 dll로드 코드>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string strClassName, string StrWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern void SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_SHOWNORMAL = 1; //활성화
        private const int SW_HIDENORMAL = 6; //최소화
        #endregion

        private static TefreecaUnit_interface TC;
        private static mini_database Ldriver = new mini_database(); //cno가 키 iwebdriver가 객체.
        private static bool none_proxy = false;
        private static bool tester = false;
        private static int timeout;
        private static int SideBreak_ConnectNo = -1; //유닛에서 연결중인 상태에서 접속 취소가 나오면 사이드브레이크가 발동되어 -1에서 해당 고유 유닛 번호로 바뀐다.
        static void Main(string[] args)
        {
            init_Server();

            Set_flag(args);
            Set_flag(Read_satting());
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
                                none_proxy = true;
                                Console.WriteLine("None Proxy Mode.");
                            }
                            break;
                        case "-tester": //테스터모드 //유닛이 초기 접속 테스터가 된다.
                            {
                                tester = true;
                                Console.WriteLine("Tester Mode.");
                            }
                            break;
                        case "-debug": //디버그 모드
                            {
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
        private static string[] Read_satting() //@"C:\satting.txt 파일로 부터 설정을 읽는다.
        {
            if (System.IO.File.Exists(@"C:\satting.txt"))
            {
                Console.WriteLine("Exists Setting file." + @"C:\satting.txt");
                string[] lines = System.IO.File.ReadAllLines(@"C:\satting.txt");
                return lines;
            }
            else
            {
                Console.WriteLine("Not Exists Setting file." + @"C:\satting.txt");
                return new string[] { };
            }
        }
        
        private static void Shutdown_ALL() //모든 크롬드라이버와 브라우저를 종료한다.
        {
            foreach (Process P in Process.GetProcesses())
            {
                if (P.ProcessName.ToLower().Contains("chrome"))
                {
                    P.Kill();
                }
            }
        }

        private static void BlockExit() //프로그램이 종료되지 않게 한다.
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (TC != null)
                {
                    if (TC.is_Flag_Restart()) //재시작 사이클
                    {
                        Console.Clear();
                        Shutdown_ALL(); //모두 종료.
                        StartServer(Data_structure.is_debug ? Data_structure.hostName_Debug : Data_structure.hostName, Data_structure.hostPort, none_proxy, tester);
                    }
                }
                else //첫 사이클은 여기서 시작.
                    StartServer(Data_structure.is_debug ? Data_structure.hostName_Debug : Data_structure.hostName, Data_structure.hostPort, none_proxy, tester);
            }
        }

        private static void init_Server() //서버의 각종 자료구조 초기화.
        {
            timeout = 20;
            try
            {
                TefreecaUnit_interface.Tefreeca_client_event -= Eventer_Tefreeca_client_event;
            }
            catch { }
            Ldriver.Clear();
        }

        private static void StartServer(String hostName, UInt16 hostPort, bool noneproxy, bool tester_)
        {
            init_Server();
            TC = null;
            TC = new TefreecaUnit_interface(noneproxy, tester_);
            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Connecting Server [{0}:{1}]", hostName, hostPort);

                TC.ConnectToServer(hostName, hostPort);
                if (!TC.is_connected())
                {
                    Console.WriteLine("Failure Connect, Reconnecting..");
                    continue;
                }
                else
                {
                    TefreecaUnit_interface.Tefreeca_client_event += Eventer_Tefreeca_client_event;
                    break;
                }
            }
        }

        public static void Eventer_Tefreeca_client_event(object sender, EventArgs e)
        {
            try
            {
                Tefreeca_client_EventArgs RecieveEventArg = e as Tefreeca_client_EventArgs;
                if (RecieveEventArg == null)
                    return;
                Unit_Event_Type type = RecieveEventArg.type;
                
                switch (type)
                {
                    case Unit_Event_Type.Account_Check_Login:
                        {
                            int connect_no = RecieveEventArg.data_int;

                            //실질적인 로그인 시도.
                            if (!Ldriver.Has_int_key(connect_no)) //드라이브가 없으면 바로 삭제한것으로 간주
                            {
                                return;
                            }
                            IWebDriver drv = (IWebDriver)Ldriver.Select_key(connect_no);

                            if (login_check(drv))
                            {
                                i_Updated_Connection_Account iua = new i_Updated_Connection_Account();
                                iua.Connect_No = connect_no;
                                iua.State = State_Account_on_connected.Logged_Account;
                                TefreecaUnit_interface.Add_Command(Command_Client.Updated_Connection_Account, iua);
                            }
                            else //로긴 상태가 아니라면, 로긴을 해야하는데.. 그래서 다시 setting 로 바꿔서 로긴시도하게끔 만듬.
                            {
                                i_Updated_Connection_Account iua = new i_Updated_Connection_Account();
                                iua.Connect_No = connect_no;
                                iua.State = State_Account_on_connected.Setting;
                                TefreecaUnit_interface.Add_Command(Command_Client.Updated_Connection_Account, iua);
                            }
                        }
                        break;
                    case Unit_Event_Type.Account_Login: //ㄹ
                        {
                            i_Account_Set iac = (i_Account_Set)RecieveEventArg.Data_object_list[0];

                            //실질적인 로그인 시도.
                            if (!Ldriver.Has_int_key(iac.Connect_No)) //드라이브가 없으면 바로 삭제한것으로 간주
                            {
                                return;
                            }
                            IWebDriver drv = (IWebDriver)Ldriver.Select_key(iac.Connect_No);

                            login_afreeca(drv, iac.ID, iac.PW); //여기선 로긴만 한다.
                        }
                        break;
                    case Unit_Event_Type.Check_End_broadcast: //방송이 진행중에 종료되었는지 확인하는 코드
                        {
                            foreach(i_connect C in RecieveEventArg.Data_object_list)
                            {
                                Thread th = new Thread(new ParameterizedThreadStart(Thread_check_End_Broadcasting));
                                th.Start(C); //웹브라우저 객체를 매게변수로 넘긴다.
                            }
                        }
                        break;
                    case Unit_Event_Type.Disconnect_All:
                        {
                            Shutdown_ALL();
                        }
                        break;
                    case Unit_Event_Type.Disconnect_connect://하나의 연결 해제.
                        {
                            i_connect C = (i_connect)RecieveEventArg.Data_object_list[0];

                            if (!Ldriver.Has_int_key(C.No)) //드라이브가 없으면 바로 삭제한것으로 간주
                            {
                                TefreecaUnit_interface.Add_Command( //상태를 업데이트 하도록 한다.
                                Command_Client.Updated_Connection,
                                new i_Updated_Connection(C.No, i_Updated_Connection_Type.State_Connect, State_Connect.Close));
                                return;
                            }

                            TefreecaUnit_interface.Add_Command( //상태를 업데이트 하도록 한다.
                                Command_Client.Updated_Connection,
                                new i_Updated_Connection(C.No, i_Updated_Connection_Type.State_Connect, State_Connect.Closing));
                            
                            Thread th = new Thread(new ParameterizedThreadStart(Close_Driver));
                            th.Start(Ldriver.Select_key(C.No)); //웹브라우저 객체를 매게변수로 넘긴다.

                            th.Join();

                            TefreecaUnit_interface.Add_Command( //상태를 업데이트 하도록 한다.
                                Command_Client.Updated_Connection,
                                new i_Updated_Connection(C.No, i_Updated_Connection_Type.State_Connect, State_Connect.Close));
                            try
                            {
                                Ldriver.Remove(C.No);
                            }
                            catch
                            {
                                Console.WriteLine("Already remove webdriver : {0}", C.No);
                            }
                        }
                        break;
                    case Unit_Event_Type.Order_Connect:
                        {
                            i_connect C = JsonConvert.DeserializeObject<i_connect>(RecieveEventArg.data_string);
                            Command_Connect(C);
                        }
                        break;
                    case Unit_Event_Type.Order_Disconnect:
                        {
                            
                        }
                        break;
                    case Unit_Event_Type.Connect_sideBreak:
                        {
                            SideBreak_ConnectNo = RecieveEventArg.data_int;
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
                //throw;
           }
        }

        private static void Command_Connect(i_connect C)
        {
            switch (C.order_type)
            {
                case Type_Order.Enter_Afreeca_Room:
                    {
                        Thread th = new Thread(new ParameterizedThreadStart(Thread__Enter_Afreeca_Room));
                        th.Start(C);
                    }
                    break;
                default:
                    { }
                    break;
            }
        }

        private static void Thread_check_End_Broadcasting(object i_connect_)
        {
            i_connect C = (i_connect)i_connect_;
            int endtimeout = 2;
            bool is_end = false;

            if (!Ldriver.Has_int_key(C.No)) //현재 해당 연결이 없거나 삭제되었으면 확인하지 않는다.
                return;

            IWebDriver dr_ = (IWebDriver)Ldriver.Select_key(C.No);

            while(endtimeout-- > 0)
            {
                try
                {
                    IWebElement nn = dr_.FindElement(By.Id("layerAirOff"));
                    string ddds = nn.Text;
                    if (ddds.Contains("방송을 종료하였습니다"))
                    {
                        is_end = true;
                        break;
                    }
                }
                catch { }
                Thread.Sleep(1000);
            }

            if (is_end)
            {
                TefreecaUnit_interface.Add_Command( //상태를 업데이트 하도록 한다.
                                Command_Client.Updated_Connection,
                                new i_Updated_Connection(C.No, i_Updated_Connection_Type.State_Connect, State_Connect.Failure));

                TefreecaUnit_interface.Add_Response(Command_Server.End_Broadcasting, C.Order_No);
                Console.WriteLine("Ended Broadcasting. ");
            }
        }
        private static Random RAND = new Random();
        private static bool Enter_Afreeca_Room_to_Mobile()
        {
            //if (RAND.Next(0, 100) > 40)
            //    return true;
            return false;
        }
        private static void Thread__Enter_Afreeca_Room(object connection_)
        {
            i_connect C = (i_connect)connection_;
            if (Enter_Afreeca_Room_to_Mobile() && (!tester)) //테스터면 절대 안됨.
            {
                C.URL = C.URL.Replace("play.afreecatv.com", "m.afreecatv.com/#/player");
            }
            try
            {
                int timeout_ = timeout;
                bool connected = false;

                if (C.Proxy == "")
                {
                    Console.WriteLine("ERROR: Can't connect. \r\n Not set proxy Server.");
                    return;
                }
                
                var chromeOptions = new ChromeOptions();
                if (!none_proxy)
                {
                    string server = Convert.ToString(C.Proxy);
                    var proxy = new Proxy();
                    proxy.HttpProxy = server;
                    chromeOptions.Proxy = proxy;
                }
                //시크릿 모드로.. 섹션 분리
                chromeOptions.AddArgument("--incognito");
                chromeOptions.AddArgument("--silent");
                IWebDriver Driver = new ChromeDriver(@".\", chromeOptions);
                lock (Ldriver)
                {
                    Ldriver.Insert(C.No, Driver);
                }
                Driver.Navigate().GoToUrl(C.URL);

                if (C.URL.Contains("m.afreecatv.com")) //모바일 =========================================================
                {
                    while (true)
                    {
                        Console.Write("X{0}.", timeout_);
                        try
                        {
                            IWebElement btn_normal_q = WebDriverExtensions.FindElement(Driver, By.ClassName("low"), 5);
                            IWebElement startBtnGroup = WebDriverExtensions.FindElement(Driver, By.Id("startBtnGroup"), 5);
                            //startBtnGroup
                            if (btn_normal_q != null)
                            {
                                string disp = startBtnGroup.GetCssValue("display");
                                if (disp.Contains("none")) //없으면 접속된거
                                {
                                    ReadOnlyCollection<IWebElement> in_broadcasting = WebDriverExtensions.FindElements(Driver, By.TagName("dd"), 5);

                                    bool is_inbroad = false;
                                    foreach (IWebElement ele in in_broadcasting)
                                    {
                                        if (is_방송에_입장하셨습니다(ele.Text))
                                        {
                                            is_inbroad = true;
                                            break;
                                        }
                                    }

                                    if (is_inbroad)
                                    {
                                        connected = true;

                                        TefreecaUnit_interface.Add_Command( //상태를 업데이트 하도록 한다.
                                        Command_Client.Updated_Connection,
                                        new i_Updated_Connection(C.No, i_Updated_Connection_Type.State_Connect, State_Connect.Connected));

                                        process_do_Low(); //모든 크롬 창 최소화.
                                        break;
                                    }
                                }
                                else //있으면 접속안된거..
                                {
                                    Thread.Sleep(2000);
                                    WebDriverExtensions.element_click(Driver, btn_normal_q);
                                }
                            }
                        }
                        catch
                        { }
                        if (timeout_-- <= 0) //0이하면 탈출..
                            break;
                        Thread.Sleep(1000);
                    }
                    if (!connected)
                    {
                        if (!none_proxy && timeout > 100)
                        {
                            TefreecaUnit_interface.Add_Command(Command_Client.Change_Proxy, null);
                        }
                        else
                        {
                            if (timeout <= 100)
                                timeout += 20;
                            TefreecaUnit_interface.Add_Command( //상태를 업데이트 하도록 한다.
                                        Command_Client.Updated_Connection,
                                        new i_Updated_Connection(C.No, i_Updated_Connection_Type.State_Connect, State_Connect.Failure));
                        }
                    }
                }//============================================================================================
                else //pc
                {
                    while (true)
                    {
                        try
                        {
                            Console.Write("X{0}.", timeout_);
                            IWebElement nn = Driver.FindElement(By.XPath("//*[@id=\"broad_info\"]/dl/dt"));
                            if (!is_방송을_불러오고_있습니다(nn.Text.ToString())) //연결되면
                            {
                                connected = true;

                                TefreecaUnit_interface.Add_Command( //상태를 업데이트 하도록 한다.
                                    Command_Client.Updated_Connection,
                                    new i_Updated_Connection(C.No, i_Updated_Connection_Type.State_Connect, State_Connect.Connected));

                                process_do_Low(); //모든 크롬 창 최소화.

                                break;
                            }
                        }
                        catch { }


                        if (SideBreak_ConnectNo != -1) //사이드 브레이크 발동
                        {
                            //if (SideBreak_ConnectNo == C.No)
                            //{
                                SideBreak_ConnectNo = -1;
                                break;
                            //}
                        }

                        if (timeout_-- <= 0) //0이하면 탈출..
                            break;
                        Thread.Sleep(1000);

                        // 접속을 검사함.. 계속 계속.. 다 될때 까지 다되면 끗.
                    }
                    if (!connected)
                    {
                        if (!none_proxy && timeout > 100)
                        {
                            TefreecaUnit_interface.Add_Command(Command_Client.Change_Proxy, null);
                        }
                        else
                        {
                            if (timeout <= 100)
                                timeout += 20;
                            TefreecaUnit_interface.Add_Command( //상태를 업데이트 하도록 한다.
                                        Command_Client.Updated_Connection,
                                        new i_Updated_Connection(C.No, i_Updated_Connection_Type.State_Connect, State_Connect.Failure));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AFREECA ERROR: " + ex.ToString());
                TefreecaUnit_interface.Add_Command( //상태를 업데이트 하도록 한다.
                                Command_Client.Updated_Connection,
                                new i_Updated_Connection(C.No, i_Updated_Connection_Type.State_Connect, State_Connect.Failure)); //연결 실패로 바꾼다.
            }
        }

        private static bool is_방송에_입장하셨습니다(string txt)
        {
            if (txt == "")
                return false;

            int i = 0;

            if (txt.Contains("방송에"))
                i++;
            if (txt.Contains("입장하셨습니다"))
                i++;

            if (i > 1)
                return true;
            return false;
        }

        private static bool is_방송을_불러오고_있습니다(string txt)
        {
            if (txt == "")
                return true;

            int i = 0;

            if (txt.Contains("방송을"))
                i++;
            if (txt.Contains("불러오고"))
                i++;
            if (txt.Contains("있습니다"))
                i++;

            if (i > 1)
                return true;
            return false;
        }

        private static bool login_check(IWebDriver Drv) //로그인 체크
        {
            IWebElement WD = WebDriverExtensions.FindElement(Drv, By.Id("topmenu_name"), 5);
            if (WD != null)
            {
                string display = WD.Text;
                if (display.Length > 3) //디스플레이 되 있으면 로그인 된 것으로 간주.
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false; //5초간 확인 불가라면 로그인 실패로 간주.
            }
        }

        /// <summary>
        /// 방에 들어와 있는 상태에서 로그인 시도,
        /// </summary>
        /// <param name="Drv"></param>
        private static void login_afreeca(IWebDriver Drv, string ID, string PW)
        {
            try
            {
                //IWebElement login_button = Drv.FindElement(By.LinkText("로그인"));

                IWebElement login_button = WebDriverExtensions.FindElement(Drv, By.XPath("//*[@id=\"topbox\"]/div/ul/li[1]/a/span"), 5);
                Drv.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 5));
                if (login_button != null)
                {
                    click_element(Drv, login_button);
                    Drv.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 5));
                    Thread.Sleep(500);
                    click_element(Drv, login_button);
                    WebDriverWait wait2 = new WebDriverWait(Drv, new TimeSpan(0, 0, 5));
                    wait2.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.Id("szUid")));

                    IWebElement tbox_id = Drv.FindElement(By.Id("szUid"));
                    tbox_id.SendKeys(ID);
                    IWebElement tbox_pw = Drv.FindElement(By.Id("szPassword"));
                    tbox_pw.SendKeys(PW);
                    IWebElement btn_login = Drv.FindElement(By.ClassName("btn_login"));
                    btn_login.Click();
                }
            }
            catch (NoSuchElementException e)
            {
                Console.WriteLine("로그인 실패 : {0}", e.ToString());
            }
            catch
            {

            }
        }
        
        private static void click_element(IWebDriver Driver, IWebElement ELE)
        {
            Actions A = new Actions(Driver);
            A.Click(ELE).Build().Perform();
        }
        private static void process_do_Low() //모든 크롬 관련 프로세스 창을 최소화 하고 우선순위를 최하로 낮춪다.
        {
            try
            {
                Process[] Pchromedriver = Process.GetProcessesByName("chromedriver");
                Process[] PChrome = Process.GetProcessesByName("Chrome");

                foreach (Process P in Process.GetProcesses())
                {
                    if (P.ProcessName.ToLower().Contains("chrome"))
                    {
                        // 윈도우 핸들러
                        IntPtr procHandler = FindWindow(null, P.MainWindowTitle);
                        // 활성화
                        P.PriorityClass = ProcessPriorityClass.Idle; //우선순위 하락.
                        ShowWindow(procHandler, SW_HIDENORMAL); //창 최소화.
                                                                //P.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("process_do_Low() ERROR : {0}}", ex.ToString());
            }
        }

        private static void Close_Driver(object iwebdriver)
        {
            IWebDriver dr_ = (IWebDriver)iwebdriver;
            try
            {
                dr_.Close();
                dr_.Quit();
            }
            catch {
                try {
                    dr_.Quit();
                }catch {
                    
                }
            }
        }
    }
    class Mouse_hock
    {
        [DllImport("user32.dll")]
        static extern void keybd_event(byte vk, byte scan, int flags, ref int extrainfo);
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEMOVE = 0x0001;   // 마우스 이동
        private const uint ABSOLUTEMOVE = 0x8000;   // 전역 위치
        private const uint LBUTTONDOWN = 0x0002;   // 왼쪽 마우스 버튼 눌림
        private const uint LBUTTONUP = 0x0004;   // 왼쪽 마우스 버튼 떼어짐

        public void MouseClick()
        {
            mouse_event(LBUTTONDOWN, 0, 0, 0, 0);
            mouse_event(LBUTTONUP, 0, 0, 0, 0);
        }
    }
}
