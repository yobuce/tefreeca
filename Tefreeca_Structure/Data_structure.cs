using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;

namespace Tefreeca_Structure
{
    public class Data_structure
    {
        public static String hostName_Debug = "localhost";
        public static String hostName = "45.58.36.192";
        public static UInt16 hostPort = 20000;
        public static int MaxConnect_per_unit = 5; //각 유닛마다 접속할 수 있는 최대 연결 수
        public static bool is_debug = false;

        

        /// <summary>
        /// 바이트 배열 관리 솔루션
        /// </summary>
        /// <param name="source"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static byte[] Combine(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];

        System.Buffer.BlockCopy(a, 0, c, 0, a.Length);

            System.Buffer.BlockCopy(b, 0, c, a.Length, b.Length);

            return c;

        }
        public static byte[] SeparateAndGetLast(byte[] source, byte[] separator)
        {
            for (var i = 0; i < source.Length; ++i)
            {
                if (Equals(source, separator, i))
                {
                    var index = i + separator.Length;
                    var part = new byte[source.Length - index];
                    Array.Copy(source, index, part, 0, part.Length);
                    return part;
                }
            }
            throw new Exception("not found");
        }
        public static byte[][] Separate(byte[] source, byte[] separator)
        {
            var Parts = new List<byte[]>();
            var Index = 0;
            byte[] Part;
            for (var I = 0; I < source.Length; ++I)
            {
                if (Equals(source, separator, I))
                {
                    Part = new byte[I - Index];
                    Array.Copy(source, Index, Part, 0, Part.Length);
                    Parts.Add(Part);
                    Index = I + separator.Length;
                    I += separator.Length - 1;
                }
            }
            Part = new byte[source.Length - Index];
            Array.Copy(source, Index, Part, 0, Part.Length);
            Parts.Add(Part);
            return Parts.ToArray();
        }
        public static bool Equals(byte[] source, byte[] separator, int index)
        {
            for (int i = 0; i < separator.Length; ++i)
                if (index + i >= source.Length || source[index + i] != separator[i])
                    return false;
            return true;
        }


        
    }

    public static class GZipCompress
    {
        public static byte[] Compress(byte[] buffer)
        {
            MemoryStream ms = new MemoryStream();
            GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true);
            zip.Write(buffer, 0, buffer.Length);
            zip.Close();
            ms.Position = 0;

            MemoryStream outStream = new MemoryStream();

            byte[] compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return gzBuffer;
        }

        public static byte[] Decompress(byte[] gzBuffer)
        {
            MemoryStream ms = new MemoryStream();
            int msgLength = BitConverter.ToInt32(gzBuffer, 0);
            ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

            byte[] buffer = new byte[msgLength];

            ms.Position = 0;
            GZipStream zip = new GZipStream(ms, CompressionMode.Decompress);
            zip.Read(buffer, 0, buffer.Length);

            return buffer;
        }
    }

    
    // i 로 시작하는 클래스는 서버단에서 명령어 별로 처리를 하기 위한 데이터 구조 단위.
    public class i_Update_Proxy
    {
        public i_Update_Proxy(string host_, State_Proxy state_proxy_)
        {
            host = host_;
            state_proxy = state_proxy_;
        }
        public string host;
        public State_Proxy state_proxy;
    }
    public enum i_Updated_Connection_Type
    {
        State_Connect, //연결상태 변경 object타입은 State_Connect
        Login_Account_no, //Login_Account_no 추가 혹은 변경 object타입은 int no.
        State_Account_on_connected,  //Login_Account 상태 변경. object타입은 State_Account_on_connected

    }
    public class i_Updated_Connection
    {
        public i_Updated_Connection(int connect_No, i_Updated_Connection_Type Type, object data)
        {
            Connect_No = connect_No;
            Updated_Connection_Type = Type;
            Data = data;
        }
        public int Connect_No;
        public i_Updated_Connection_Type Updated_Connection_Type;
        public object Data;
    }
    public class i_State
    {
        public State_Unit State_unit;     //현재 유닛의 상태.
        public List<i_Updated_Connection> State_connect;     //같은 볼륨을 가지는 리스트.
    }

    public class i_Order
    {
        public i_Order(int no, int User_No_, int Max_Connect_, string URL_, Type_Order Type_order_)
        {
            No = no;
            User_No = User_No_;
            Max_Connect = Max_Connect_;
            URL = URL_;
            Type_order = Type_order_;
        }
        public int No;
        public int User_No;
        public int Max_Connect;
        public string URL;
        public Type_Order Type_order;
    }

    public class i_connect
    {
        public i_connect(int No_, int Order_No_, string URL_, Type_Order order_type_, string Proxy_)
        {
            No = No_;
            Order_No = Order_No_;
            URL = URL_;
            order_type = order_type_;
            Proxy = Proxy_;
        }
        public int No;
        public int Order_No;
        public string URL;
        public string Proxy;
        public Type_Order order_type;

    }
    public class i_Account_Set
    {
        public i_Account_Set(
            Type_Order Type_order_,
            int Connect_No_,
            int Account_No_,
            string ID_,
            string PW_,
            int Order_No_)
        {
            Account_No = Account_No_;
            Type_order = Type_order_;
            Connect_No = Connect_No_;
            ID = ID_;
            PW = PW_;
            Order_No = Order_No_;
        }

        public int Account_No;
        public int Connect_No;
        public int Order_No;
        public string ID;
        public string PW;
        public Type_Order Type_order;
    }
    public class i_Login
    {
        public i_Login(int Unit_No_, string id_, string pw_, Type_User type_)
        {
            Unit_No = Unit_No_;
            id = id_;
            pw = pw_;
            type = type_;
        }
        public int Unit_No;
        public string id;
        public string pw;
        public Type_User type;
    }

    public class i_Login_success
    {
        public i_Login_success(int User_No_, bool is_reseller_, string Session_, Type_User type_)
        {
            User_No = User_No_;
            is_reseller = is_reseller_;
            Session = Session_;
            Type_user = type_;
        }
        public int User_No;
        public bool is_reseller;
        public string Session;
        public Type_User Type_user;
    }

    public class i_Updated_User
    {
        public int No;
        public int Unit_No;
        public int Slot;
        public int Slot_Account;
        public int fee;
        public string id;
        public string phone;
        public string email;
        public string pay_type;
        public string session;
        public DateTime expire;
        public Type_User User_type;
        public bool is_Reseller;
        public int my_Reseller_No;
    }

    public class i_List_ServerInfo
    {
        public i_List_ServerInfo(int Command_Q_length_)
        {
            Command_Q_length = Command_Q_length_;
        }
        public int Command_Q_length;
    }

    public class i_List_Proxy
    {
        public int No;
        public string Host;
        public int Error_count;
        public State_Proxy State;
        public Type_Proxy Type;
    }

    public class i_List_User
    {
        public int No;
        public int Unit_No;
        public int Slot;
        public int Slot_Account;
        public int fee;
        public string id;
        public string phone;
        public string email;
        public string pay_type;
        public string session;
        public DateTime expire;
        public Type_User User_type;
        public bool is_Reseller;
        public int my_Reseller_No;
    }
    public class i_List_Account
    {
        public int No;
        public Type_Order Type_order_Site;
        public int User_No;
        public string ID;
        public string PW;
        public State_Account State;
    }
    public class i_List_Connect
    {
        public int No;
        public int Order_No;
        public int Unit_No;
        public string Connect_URL;
        public string Proxy_host;
        public Type_Order Type_order;
        public State_Connect State;
        public int Login_Account_no;
        public State_Account_on_connected State_Account_on_connected;
        public int Wait_count;
    }
    public class i_List_Order
    {
        public int No;
        public int User_No;
        public int Connected_count;
        public int Max_connect;
        public string URL;
        public Type_Order Type_order;
        public State_Order State;
    }

    public class i_List_Unit
    {
        public int No;
        public int Connected_count;
        public int Max_connect;
        public string Proxy;
        public State_Unit State;
        public int Wait_count;
    }

    public class i_User_info
    {
        public int No;
        public int Unit_No;
        public int slot;
        public string Id;
        public DateTime Expire;
        public Type_User type;
        public bool is_Reseller;
    }

    public class i_Set_MaxConnect
    {
        public i_Set_MaxConnect(int _User_No, int _Order_No, int _MaxConnect)
        {
            User_No = _User_No;
            Order_No = _Order_No;
            MaxConnect = _MaxConnect;
        }

        public int User_No;
        public int Order_No;
        public int MaxConnect;
    }
    public class i_Test_connect_order
    {
        public i_Test_connect_order(int Order_no_, int Unit_No_, int Connect_No_)
        {
            Order_no = Order_no_;
            Unit_No = Unit_No_;
            Connect_No = Connect_No_;
        }
        public i_Test_connect_order()
        {
            Order_no = 0;
            Unit_No = 0;
            Connect_No = 0;
        }
        public int Order_no;
        public int Unit_No;
        public int Connect_No;
    }

    public class i_Make_Login
    {
        public i_Make_Login(string id, string pw)
        {
            ID = id;
            PW = pw;
        }

        public string ID;
        public string PW;

    }

    public class i_Make_Connect
    {
        public i_Make_Connect(string _URL, int _Max_Connect)
        {
            Max_Connect = _Max_Connect;
            URL = _URL;
        }
        public string URL;
        public int Max_Connect;
    }

    public class i_Active_Order
    {
        public int Order_no;
    }

    public class i_Order_Info
    {
        public int Order_no;
        public int Connect_Count;
        public State_Order State;
    }

    public class i_R_Order_Set_MaxConnect
    {
        public i_R_Order_Set_MaxConnect(client_message message_, int Order_no_, int Max_Connect_)
        {
            message = message_;
            Order_no = Order_no_;
            Max_Connect = Max_Connect_;

        }
        public client_message message;
        public int Order_no;
        public int Max_Connect;
    }

    public class i_Updated_Connection_Account
    {
        public int Connect_No;
        public State_Account_on_connected State;
    }

    public enum i_Login_Failure_type
    {
        Invalid_login_information,      //올바르지 않은 로그인 정보
        Duplicate_Login,                //중복로그인
    }

    public class mini_database
    {
        private Hashtable hash = null;
        private List<int> keylist = null;
        private Hashtable StringKey = null;
        private index_generator index = null;

        private object table_lock = new object();

        public mini_database()
        {
            hash = new Hashtable();
            StringKey = new Hashtable(); //문자열로 키를 지정할 경우.(stringkey , intkey)
            keylist = new List<int>(); //키 리스트
            index = new index_generator();
        }

        public List<int> List_() //key에 대한 리스트.
        {
            return keylist;
            //return List.
        }

        public void Clear()
        {
            hash.Clear();
            keylist.Clear();
            StringKey.Clear();
            index = new index_generator();
        }
        /*
        private object make_key_lock = new object();
        public int make_key() //현재 리스트에 존재하지 않는 키를 새로 생성한다.
        {
            int key = -1;
            lock(make_key_lock)
            {
                bool in_new_key = false;

                while (!in_new_key) //중복되지 않는 키 생성.
                {
                    key = Convert.ToInt32(index.generate_index()); //생성하고
                    if (!keylist.Contains(key)) //검사한다.
                        in_new_key = true;                   //기존에 없는키가 확인이 되면, true로 만들어 탈출한다.
                }
                return key;
            }
        }
        */
        public int Count()
        {
            return hash.Count;
        }

        public bool Has_int_key(int key)
        {
            return hash.ContainsKey(key);
        }

        public bool Has_value(object value)
        {
            return hash.ContainsValue(value);
        }

        public bool Update(string key, object value)
        {
            if (StringKey.Contains(key))
            {
                int string_index = (int)StringKey[key];

                if (Update(string_index, value))
                    return true;
                else
                    throw new System.ArgumentException("엔트리 수정에 실패했습니다.");
            }
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 존재하지 않습니다.");
        }
        public bool Update(int key, object value)
        {
            lock (table_lock)
            {
                object inner_value = null;

                if (hash.ContainsKey(key))
                {
                    inner_value = hash[key]; //지울 오브젝트를 찾아

                    hash.Remove(key); //키로 지우고

                    hash.Add(key, value); //다시 추가.

                    return true;
                }
                else
                    throw new System.ArgumentException("테이블에 해당 키 값이 존재하지 않습니다.");
            }
        }

        public object Select_for(int key)
        {
            if (hash.ContainsKey(keylist[key]))
                return hash[keylist[key]];
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 존재하지 않습니다.");
        }

        public object Select_key(string key)
        {
            if (StringKey.ContainsKey(key))
            {
                int string_index = (int)StringKey[key];

                return Select_key(string_index);
            }
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 존재하지 않습니다.");
        }
        public object Select_key(int key)
        {
            if (hash.ContainsKey(key))
                return hash[key];
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 존재하지 않습니다.");
        }

        public int Getkey(object value)
        {
            if (hash.ContainsValue(value))
            {
                foreach (int key in keylist)
                {
                    if (hash[key] == value)
                    {
                        return key;
                    }
                }
            }
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 존재하지 않습니다.");
            return -1;
        }

        public int Add(string key, object value)
        {
            lock (table_lock)
            {
                int string_index = -1;
                if (!StringKey.Contains(key))
                {
                    string_index = Convert.ToInt32(index.generate_index(keylist));

                    StringKey.Add(key, string_index);

                    if (!Insert(string_index, value))
                        throw new System.ArgumentException("엔트리 생성에 실패했습니다.");
                }
                else
                    throw new System.ArgumentException("테이블에 해당 키 값이 이미 존재합니다.");
                return string_index;
            }
        }
        public bool Add(int key, object value)
        {
            lock (table_lock)
            {
                return Add_(key, value);
            }
        }
        public int Add(object value)
        {
            lock (table_lock)
            {
                return Add_(value);
            }
        }

        private bool Add_(string key, object value)
        {
            if (!StringKey.Contains(key))
            {
                int string_index = Convert.ToInt32(index.generate_index(keylist));

                StringKey.Add(key, string_index);

                if (Add_(string_index, value))
                    return true;
                else
                    throw new System.ArgumentException("엔트리 생성에 실패했습니다.");
            }
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 이미 존재합니다.");
        }
        private bool Add_(int key, object value)
        {
            if (!hash.ContainsKey(key)) //존재하지 않아야 한다.
            {
                keylist.Add(key);
                hash.Add(key, value); //다시 추가.

                return true;
            }
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 이미 존재합니다.");
        }
        private int Add_(object value)
        {
            int idx = Convert.ToInt32(index.generate_index(keylist));

            keylist.Add(idx);
            hash.Add(idx, value);

            return idx;
        }

        public bool Insert(string key, object value)
        {
            if (!StringKey.Contains(key))
            {
                int string_index = Convert.ToInt32(index.generate_index(keylist));

                StringKey.Add(key, string_index);

                if (Insert(string_index, value))
                    return true;
                else
                    throw new System.ArgumentException("엔트리 생성에 실패했습니다.");
            }
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 이미 존재합니다.");
        }
        public bool Insert(int key, object value)
        {
            lock (table_lock)
            {
                if (!hash.ContainsKey(key)) //존재하지 않아야 한다.
                {
                    keylist.Add(key);
                    hash.Add(key, value); //다시 추가.

                    return true;
                }
                else
                    throw new System.ArgumentException("테이블에 해당 키 값이 이미 존재합니다.");
            }
        }
        public int Insert(object value)
        {
            lock (table_lock)
            {
                int idx = Convert.ToInt32(index.generate_index());

                keylist.Add(idx);
                hash.Add(idx, value);

                return idx;
            }
        }

        public bool Remove(string key)
        {
            if (StringKey.Contains(key))
            {
                int string_index = (int)StringKey[key];

                if (Remove(string_index))
                {
                    StringKey.Remove(string_index);
                    return true;
                }
                else
                    throw new System.ArgumentException("엔트리 삭제에 실패했습니다.");
            }
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 존재하지 않습니다.");
        }
        public bool Remove(int key) //반환이 true면 삭제성공, false면 삭제 실패(해당 요소가 없거나 오류)
        {
            object value = null;

            if (hash.ContainsKey(key))
            {
                value = hash[key];
                lock (table_lock)
                {
                    keylist.Remove(key);
                    hash.Remove(key);
                }

                return true;
            }
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 존재하지 않습니다.");
        }

        public bool Remove(object value) //반환이 true면 삭제성공, false면 삭제 실패(해당 요소가 없거나 오류)
        {
            if (hash.ContainsValue(value))
            {
                foreach (int key in keylist)
                {
                    if (hash[key] == value)
                    {
                        lock (table_lock)
                        {
                            keylist.Remove(key);
                            hash.Remove(key);
                        }
                        break;
                    }
                }
                return true;
            }
            else
                throw new System.ArgumentException("테이블에 해당 키 값이 존재하지 않습니다.");
        }
    }
    public class index_generator //인덱스 생성기
    {
        private UInt32 index;
        private object index_lock = new object();

        public index_generator()
        {
            index = 0;
        }
        public index_generator(UInt32 default_value)
        {
            lock (index_lock)
            {
                index = default_value;
            }
        }

        public UInt32 generate_index(List<int> keyList)//기존에 키들이 파편화 되어 있을 경우 인덱스를 올바로 생성하기 위한 코드.
        {
            lock (index_lock)
            {
                UInt32 idx = 0;
                while (true)
                {
                    idx = index++;
                    if (!keyList.Contains(Convert.ToInt32(idx))) //없으면.
                        return idx;
                }
            }
        }
        public UInt32 generate_index()
        {
            lock (index_lock)
            {
                return index++;
            }
        }
    }
    public class hash_pro
    {
        public bool is_duple(string value)
        {
            return is_duple(value, 1);
        }

        public void reset() //모든 데이터를 초기화 시킨다. 처음상태로..
        {
            h_list.Clear();
            h_hash.Clear();
        }

        private ArrayList h_list = new ArrayList();
        private Hashtable h_hash = new Hashtable();

        private bool is_duple(string value, int index_length)//true면 중복이란 말이고 false 면 중복이 아닌 새로운것이란 말.
        {
            if (value == null) //값이 재대로 안들어오면 무조건 중복으로 간주, 버리도록 유도.
                return true;

            string key = value.Substring(0, index_length);

            if (h_hash.Contains(key)) //해당 키가 있으면, 해당 키의 해시에 이메일이 있는지 검사,
            {
                Hashtable table = (Hashtable)h_hash[key];
                if (table.ContainsValue(value)) //이메일을 찾으면 찾았다고 하고 탈출
                    return true;
                else //못찾으면, 사이즈 검사해서 더 깊이 찾아 들어갈지, 검사..
                    if (table.Count > 1000) //맥시멈 사이즈 이상이면, 또다른 테이블이 존재할 수 있으므로 검사.
                    return is_duple(value, index_length + 1);
                else //맥시멈이 아니면 더이상 다른 테이블이 없으므로 거기에 추가하고 종료
                {
                    try
                    {
                        table.Add(table.Count, value);
                    }
                    catch { }
                    return false;
                }
            }
            else //없으면, 처음 새롭게 만들어지는 테이블이다. 그러므로 테이블 만들고 이멜 추가하고 바로 종료.
            {
                h_hash.Add(key, new Hashtable());
                h_list.Add(h_hash[key]);

                Hashtable table = (Hashtable)h_hash[key];
                try
                {
                    table.Add(0, value);
                }
                catch { }

                return false;
            } //t 면 이메일을 찾았다는 말, f면 못찾았다는 말.
        }

        public bool has_element(string value) //단지 있는지 없는지만 계산
        {
            return has_element(value, 1);
        }

        private bool has_element(string value, int index_length)
        {
            if (value == null) //값이 재대로 안들어오면 무조건 중복으로 간주, 버리도록 유도.
                return true;

            string key = value.Substring(0, index_length);

            if (h_hash.Contains(key)) //해당 키가 있으면, 해당 키의 해시에 이메일이 있는지 검사,
            {
                Hashtable table = (Hashtable)h_hash[key];
                if (table.ContainsValue(value)) //이메일을 찾으면 찾았다고 하고 탈출
                    return true;
                else //못찾으면, 사이즈 검사해서 더 깊이 찾아 들어갈지, 검사..
                    if (table.Count > 1000) //맥시멈 사이즈 이상이면, 또다른 테이블이 존재할 수 있으므로 검사.
                    return has_element(value, index_length + 1);
                else //맥시멈이 아니면 더이상 다른 테이블이 없으므로 거기에 추가하고 종료
                {
                    return false;
                }
            }
            else //없으면, 처음 새롭게 만들어지는 테이블이다. 그러므로 테이블 만들고 이멜 추가하고 바로 종료.
            {
                return false;
            } //t 면 이메일을 찾았다는 말, f면 못찾았다는 말.
        }
        public bool remove_element(string value) //단지 있는지 없는지만 계산
        {
            return remove_element(value, 1);
        }

        private bool remove_element(string value, int index_length) //지우는데 성공하면 t 실패하면 f
        {
            if (value == null) //값이 재대로 안들어오면 무조건 중복으로 간주, 버리도록 유도.
                return false;

            string key = value.Substring(0, index_length);

            if (h_hash.Contains(key)) //해당 키가 있으면, 해당 키의 해시에 이메일이 있는지 검사,
            {
                Hashtable table = (Hashtable)h_hash[key];
                if (table.ContainsValue(value)) //이메일을 찾으면 찾았다고 하고 탈출
                {
                    for (int i = 0; i < table.Count; i++)
                    {
                        if (table[i].ToString() == value)
                        {
                            table.Remove(i);
                            break;
                        }
                    }
                    return true;
                }
                else //못찾으면, 사이즈 검사해서 더 깊이 찾아 들어갈지, 검사..
                    if (table.Count > 1000) //맥시멈 사이즈 이상이면, 또다른 테이블이 존재할 수 있으므로 검사.
                    return has_element(value, index_length + 1);
                else //맥시멈이 아니면 더이상 다른 테이블이 없으므로 거기에 추가하고 종료
                {
                    return false;
                }
            }
            else //없으면, 처음 새롭게 만들어지는 테이블이다. 그러므로 테이블 만들고 이멜 추가하고 바로 종료.
            {
                return false;
            } //t 면 이메일을 찾았다는 말, f면 못찾았다는 말.
        }
    }
    public class AsyncObject //기본 통신 단위
    {
        public Byte[] Buffer;
        public Socket WorkingSocket;
        public AsyncObject(Int32 bufferSize)
        {
            this.Buffer = new Byte[bufferSize];
        }
    }

    public class Tefreeca_client_EventArgs : EventArgs //사용자 정의 이벤트 클래스
    {
        public Unit_Event_Type type { get; set; }
        public int data_int { get; set; }
        public string data_string { get; set; }
        public List<Object> Data_object_list { get; set; }
    }


    public enum client_message
    {
        로그인_준비_완료,
        로그인_시도중,
        로그인_성공,
        로그인_실패,
        올바르지_않은_계정정보,
        사용기간_만료,
        URL_테스트중,
        테스트_실패,
        테스트_성공,
        방송_접속_활성,
        방송_접속_비활성,
        디버깅_모드,
        접속_명령_생성,
        오류,
        활성화_되었습니다,
        연결_해제중,
        연결_해제_성공,
        서버와_접속이_끊어졌습니다,
        사용자를_확인할_수_없습니다,
        사용기간이_만료되었습니다,
        같은_URL로_진행중인_연결이_있습니다,
        연결이_모두_종료된_후에_다시_시도하세요,
        할당된_연결보다_더_많은_연결을_생성할_수_없습니다,
        방송_접속수가_변경되었습니다,
        방송_접속수_변경중_오류,
        존재하지_않는_연결명령입니다,
    }

    public enum Unit_Event_Type //클라이언트 이벤트 발생 모델 Tefreeca_client.cs에 배당된 기능 이외에 유닛 전체의 기능을 이용하기 위해 통신단과 커뮤니팅 하기 위한 이벤트 구분자.
    {
        //unit
        Change_Client_state,            //클라이언트의 상태가 변하면 발생하는 이벤트.
        Check_End_broadcast,            //계속 방송중인지 종료되었는지 확인하는 이벤트.      
        Order_Connect,                  //상태: 연결중.. -> 연결완료 or 연결 실패
        Order_Disconnect,               //상태: -> 종료중.. -> 종료완료
        Failure_Proxy,                  //연결시도가 실패했을때,,
        Disconnect_All,                 //모든 연결 삭제.
        Disconnect_connect,             //하나의 연결 해제
        Connect_sideBreak,              //연결중인 상태에서 종료명령이 들어왔을때,
        Account_Login,                  //계정으로 로그인.
        Account_Check_Login,            //계정이 로긴 됬는지 채크.
        

        //Admin
        List_Order,
        List_Unit,
        List_Proxy,
        List_User,
        List_Connect,
        List_ServerInfo,
        List_Account,

        //client,
        Change_connect,                 //소켓 연결 상태가 변하면 발생.
        Login_success,                  //로그인 성공.
        Login_Failure,                  //로그인 실패
        User_info,                      //유저 정보 요청에 대한 정보가 오면
        Order_no,                       //오더번호 요청에 대한 정보가 오면.
        Active_Order,                   //오더를 보낸 후 오더가 활성되었다는 것을 확인한다면,
        Order_Info,                     //오더를 보낸 후 오더에 대한 주기적인 상태정보.
        Server_Connection_Broken,       //서버와 연결이 끊김.
        Order_Set_MaxConnect,           //최대 연결 개수 변경이 수락되어 응답이 왔을때.
        Error_Order,
        
    };

    public enum Command_Server //서버가 받을 명령어
    {
        New_connect,            //새 소캣이 접속하면, 어드민, 유닛, 클라이언트에 따라 새로운 객체를 생성하고 유닛이면 유닛번호임명
        Disconnected_Client,    //연결이 비정상적으로 끊어졌을 경우.(프록시를 재사용하게 되돌리고, 연결을 삭제하고, 유닛을 삭제해야면 삭제한다.

        Order_Connect,          //연결 주문
        Order_Disconnect,       //연결 해제 주문
        Order_Request_No,       //연결 오더를 위한 고유번호 요청.
        Order_Set_MaxConnect,   //오더의 최고 연결 수를 지정.
        //Request_Proxy,        //해당 유닛 상태를 wait_proxy로 변경 상태는 상태메니저로 부터 자동으로 받는다.
        Sucsses_Proxy,          //유닛으로 부터 받음, 프록시 상태 used로 변경.
        Failure_Proxy,          //프록시 연결 실패로 사용불가 처리 코드.
        Failure_Connect,        ///unit에서 연결을 5회 시도했으나 연결 실패했다는 신호. connected가 0인 상태에서 누적
                                ///10회되면 이 신호가 중첩되면 order를 종료한다.
        Managing_Order,         //오더 관리
        Managing_Proxy,         //프록시 관리 - faliue상태
        Managing_Tester,        //(make_thread)프록시 테스터
        Managing_Unit,         //유닛의 종료 재시작 관리, 및 유닛의 커넥션 갯수 카운팅.
        Managing_OrderInfo,     //각 오더를 오더내린 클라이언트에게 정보를 주기적으로 제공하기 위함.
        Managing_Login_Account, //로그인 가능한 연결을 찾아 account를 할당한다

        Update_Proxy_DB,        //(make_thread)디비로 부터 프록시,유저의 최신정보를 업데이트 하는 기능을 불러올 수 있도록 하는 메소드를 부르는 신호.
        Update_User_DB,         //(make_thread)는 새로운 스레드를 만들어 작업을 수행 한 뒤 결과를 커멘드 형테로 삽입한다.
        Update_Account_DB,      //(make_thread)디비로 부터 어카운트 유저의 최신정보를 업데이트

        Updated_User,           //스레드로 부터 받은 디비 조회 결과를 업데이트 한다.
        Updated_Proxy,          //스레드로 부터 받은 프록시 테스트 결과를 업데이트 한다.
        Updated_Connection,     //상태 메시지
        Updated_Unit,           //
        Updated_Order,
        State,

        //Afreeca
        End_Broadcasting,       //방송이 종료되었습니다. 서버에서 받으면 오더를 unactive로 바꾸어서 모든 커넥션을 닫는다.


        //관리자 명령
        Add_Proxy,          //프록시 서버추가
        List_Order,         //각각의 리스트를 구하는 프로그램
        List_Unit,
        List_Proxy,
        List_User,
        List_Connect,
        List_ServerInfo,
        List_Account,

        HeartBeat,              //이걸 받으면 하트비트를 보낸다.

        //클라이언트로 받을 명령어
        Login,
        User_Info,              //클라이언트 정보 요청.
    }

    public enum Command_Client //클라이언트가 받을 명령어
    {
        //Unit
        Set_UnitNo,             //유닛,고유번호 임명
        Set_Proxy,              //유닛,unit에게 프록시 서버 지정,
        order_connect,          //유닛,연결 명령 연결정보 포함.(struct connection) 
        Close_connect,          //유닛,연결 해지 연결정보 포함 개별적인 connect를 닫는다.
        Close_all,              //해당 유닛은 모든 연결을 강제종료한다.
        set_proxy_success_ok,   //프록시 설정 완료신호, 이걸 클라이언트가 받으면 자신의 상태를 enable로 바꾼다.
        Change_Proxy,           //프록시 서버를 변경하자는 명령어.
        Failure_Counter,        //연결 실패에 대한 카운팅.
        Set_Tester,             //테스터로 임명하는 코드.

        Account_Set,            //개별적인 연결에 로그인 계정을 임명하는 명령어.

        Managing_State,         //상태들을 취합해서 보내라는 신호. 그리고 유닛의 상태를 적절히 자동으로 변경한다.
        Managing_connecting,    //연결 시도중인 프로세스를 체킹하고 감시하라는 코드
        Managing_RequestList,   //어드민 전용. 현재 보고 있는 화면의 정보를 주기적으로 요청하는 명령어.
        Managing_State_client,  //클라이언트 전용, 연결 상태 정보를 취합해 보내라는 의미,
        Managing_Account,       //현재 로그인할수 있는 연결들을 관리하는 메니저.

        Updated_Connection_Account,     //연결의 어카운트 로긴 상태 업데이트 명령어
        Updated_Connection,     //연결 상태를 변경할떄,
        Updated_current_View_page,  //현재 보고 있는 화면, current_View_page 값을 바꾸는 명령어.
        HeartBeat,              //하트비트 받을때 코드

        //client
        Make_Login,             //유저 로그인 시도,
        Make_Connect,           //연결을 만든다.
        Login_success,          //로그인 성공
        Login_Failure,          //로그인 실패
        User_Info,              //클라이언트 정보 수신시
        Order_No,               //연결을 위한 오더넘버 요청에 대한 응답. 이걸 받으면 자동으로 연결 오더를 보낸다.
        Order_Info,             //요청한 접속 요청에 대한 주기적인 최신 상태 데이터를 받는다.
        Active_Order,           //접속요청이 올바르게 접수되어 오더가 등록되었다는 신호.
        Close_Order,            //폼 컨르롤로부터 연결 해지 명령을 받을때 사용, 이 명령을 받으면 현재 활성화된 오더를 닫는 명령을 서버로 보낸다.
        R_Order_Set_MaxConnect, //오더의 최고 연결 수를 지정에 대한 응답.
        Make_Order_Set_MaxConnect,// /오더의 최고 연결 수 만드는 명령어.

        //administrator
        List_Order,
        List_Unit,
        List_Proxy,
        List_User,
        List_Connect,
        List_ServerInfo,
        List_Account,

        Error_Order,                  //오더 명령중 애러 이게 발생하면 처음으로 되돌린다.
    }
    
    public enum Type_Proxy
    {
        Free,
        Paid
    };
    public enum Type_Order
    {
        Enter_Afreeca_Room
    }
    public enum Type_User
    {
        Administrator,  //최고 관리자
        Reseller,       //리셀러
        Gold,           //유료등급3
        Silver,         //유료등급2
        Bronze,         //유료등급1        
        Starter,        //가입하는 순간.(트라이얼 기간 포함)
        Tester_Unit,    //테스터,
    }
    public enum State_Unit
    {
        Starting,
        Wait_proxy,
        Enable,
        Full_connect,
        Ordering,
        Logged,          //로그인 된 상태, 별도의 명령을 받는다.
        Wait,           //유닛의 상태를 대기하는 코드 (약 1~10회의 카운드를 받고나면 마지막에 받은 상태로 변경된다.
                        //이는 유닛으로 부터 상태를 받을때는 대기를 하게 되지만, 서버에서 직접 변경하는 경우는 즉각적으로 변경된다.
    }
    public enum State_Connect
    {                           //실패가 정정되는 과정. 연결-시도중..-실패 - (서버)연결해제 명령 - 종료시도중 - 닫힘 - (서버에서 메니저가) 연결 삭제.- 
        Connected,  //정상적으로 연결 완료 됨.
        Connecting, //연결시도중..
        Closing,    //종료 시도중..
        Close,      //완전히 닫힘. 이 상태가 전달되면 서버측에서는 메니저가 돌면서 커넥션을 삭제한다.
        Failure,    //연결을 시도 했으나 시간초과, 프록시 응답 x 등으로 연결 실패
        No_answer,  //응답이 없음.
        Wait
    }
    public enum State_Proxy
    {
        New,
        Enable,         //사용 가능한 서버였으나 테스트는 되지 않은 상태.
        Passed_test,   //테스트에 통과된 상태
        Disable,        //테스트 실패, 혹은 오류로 인해 사용불능인 서버
        Wait,           //대기 상태 
        Used,           //할당된 상태.
    }
    public enum State_Order
    {
        Wait_Test,
        Testing,
        Passed_Test,
        Active,
        Closing,
        Close,
    }
    public enum State_User
    {
        Logged,         //로그인 됨.
    }
    public enum State_Account
    {
        Enable,
        Disable,
    }
    public enum State_Account_on_connected
    {
        NotSet,           //계정이 부여되지 않은 상태
        Setting,        //처음 계정이 부여되었을 때 초기 상태,
        Trying_Login,   //로그인 시도중
        Logged_Account, //로그인됨
        Failure_Login,  //로그인 실패

    }



    public struct command_data_server //클라이언트에서 서버로 가는 명령
    {
        public Int32 Sender;
        public Int32 Command_code;
        public object data;

        public command_data_server(Int32 sender, Command_Server command_code, object data)
        {
            this.Sender = sender;
            this.Command_code = (int)command_code;
            this.data = data;
        }
    }
    public struct command_data_client //서버에서 클라이언트로 가는 명령.
    {

        public Int32 Receiver; //생성시에 들어가는 값은 메시지를 받을 받을 클라이언트 번호.
        public Int32 Command_code;
        public object data; //Command_Server

        public command_data_client(Int32 Receiver_, Command_Client command_code, object data)
        {
            this.Receiver = Receiver_;
            this.Command_code = (int)command_code;
            this.data = data;
        }
    }
}
