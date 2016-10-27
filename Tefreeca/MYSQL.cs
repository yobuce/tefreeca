using System;
using MySql.Data.MySqlClient;
using System.Data;

namespace Tefreeca_server
{
    class MYSQL
    {
        private static string myConnectionString = "server=tefreeca.com;Port=3306;uid=afreeca;pwd=tyn787ft;database=afreeca;CHARSET=utf8"; // 
        //private static string myConnectionString = "server=molis.iptime.org;Port=33060;uid=afreeca;pwd=t5y6u7I*;database=afreeca;CHARSET=utf8"; // 
        private static MySqlConnection conn;
        private static MySqlCommand Com_svr;
        private static int max_db_connection = 10;
        private static MySqlConnection[] mconn = new MySqlConnection[max_db_connection];
        private static MySqlCommand[] mCom_svr = new MySqlCommand[max_db_connection];
        private static MySqlDataReader[] mdataReader = new MySqlDataReader[max_db_connection];

        public static string DATABASE = "afreeca";

        public static bool is_readable(MySqlDataReader reader)
        {
            if (reader != null)
            {
                if (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void init()
        {
            Console.WriteLine("[Debug]connecting DB...");
            conn = new MySqlConnection(myConnectionString);
            conn.Open();
        }
        private MySqlConnection init_sub()
        {
            MySqlConnection conn2 = new MySqlConnection(myConnectionString);
            try
            {
                conn2.Open();
            }
            catch
            {
                return null;
            }

            return conn2;
        }


        public static void init(int db_num)
        {
            mconn[db_num] = new MySqlConnection(myConnectionString);
            mconn[db_num].Open();
        }

        public static MySqlDataReader query_select(string query)
        {
            if (conn == null) //현재 연결이 비어 있으면?
            {
                init();
            }
            Com_svr = new MySqlCommand(query, conn);

            return Com_svr.ExecuteReader();
        }

        public static MySqlDataReader query_select(string query, int sub_db) //쿼리 독립성을 위한 코드.
        {
            if (mconn[sub_db] == null || mconn[sub_db].State == ConnectionState.Closed) //현재 연결이 비어 있으면?
            {
                init(sub_db);
            }

            if (mconn[sub_db].State == ConnectionState.Open) //사용가능한 상태로 열려있으면?
            {
                //try
                //{
                    mCom_svr[sub_db] = new MySqlCommand(query, mconn[sub_db]);
                    mCom_svr[sub_db].CommandType = CommandType.Text;

                    MySqlDataReader R = mCom_svr[sub_db].ExecuteReader();
                    return R;
                //}
                //catch (Exception ex) //문제가 생기면..
                //{ } 
                //    Console.WriteLine("DB ERROR : {0}", ex.ToString());
                //    try
                 //   {
                //        mconn[sub_db].Close();
                //    }
                 //   catch { }
                 //   mconn[sub_db] = null; //연결을 지우고,,
                 //
                  //  return query_select(query, sub_db + 1);
                //}
            }
            else //열려 있으나 사용불가상태면..
            {
                return query_select(query, sub_db + 1);
            }
        }

        public static void query_N(string query, int recount) // 반환없는 인서트. 리카운트는 몇번 재시도 할지.
        {
            try
            {
                if (conn == null)
                    init();
                else if (conn.State == ConnectionState.Closed)
                    init();

                Com_svr = new MySqlCommand();
                Com_svr.Connection = conn;
                Com_svr.CommandType = CommandType.Text;
                Com_svr.CommandText = query;
                Com_svr.ExecuteNonQuery();

                //Console.WriteLine("ExecuteNonQuery : {0}", Com_svr.LastInsertedId);
            }
            catch
            {
                //Console.WriteLine("DB 애러 발생: ");
                if (recount > 0)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                    conn = null; //연결을 지우고,,

                    query_N(query, --recount); //재시도, 연결이 지워졌으므로, 아마도 새로 연결을 만들 것이다.
                }
            }
        }
        public static void query_N(string query) // 반환없는 인서트. 리카운트는 몇번 재시도 할지.
        {
            query_N(query, 1);
        }
        public static long query_R(string query) //마지막 인서트 한 엔트리의 프라머리 키 값을 반환다.
        {
            return query_R(query, 1);
        }
        public static long query_R(string query, int recount) //마지막 인서트 한 엔트리의 프라머리 키 값을 반환다.
        {
            long R = 0;
            try
            {
                if (conn == null)
                    init();
                else if (conn.State == ConnectionState.Closed)
                    init();

                Com_svr = new MySqlCommand();
                Com_svr.Connection = conn;
                Com_svr.CommandType = CommandType.Text;
                Com_svr.CommandText = query;
                Com_svr.ExecuteNonQuery();
                R = Com_svr.LastInsertedId;
                //Console.WriteLine("ExecuteNonQuery : {0}", R);
            }
            catch 
            {
                //Console.WriteLine("DB 애러 발생: ");
                if (recount > 0)
                {
                    try
                    {
                        conn.Close();
                    }
                    catch { }
                    conn = null; //연결을 지우고,,
                    return query_R(query, --recount); //재시도, 연결이 지워졌으므로, 아마도 새로 연결을 만들 것이다.
                }
            }
            return R;
        }
        private string ch_qt(string value)
        {
            if (value != null)
                return value.Replace("'", "").Replace("\"", "");
            else
                return "";
        }

        public static void close()
        {
            conn.Close();
        }

        private long run_query(string query) //select가 아닌 쿼리문에서만 적용.
        {
            return query_R(query);
        }
        /**********************************/
        /*
        private Queue sql_command = new Queue(); // 저장을 위한 큐,.

        public void DB_COM_COLLECTION() //DB 컬렉션 초기화
        {
            sql_command.Clear(); // 큐 초기화.
        }

        public void SAVE_INSERT_COM()
        {
            if (DB_INST_FIELD != "" && DB_INST_VALUE != "" && DB_INST_table_name != "")
            {
                sql_command.Enqueue("INSERT INTO `" + DATABASE + "`.`" + DB_INST_table_name + "` (" + DB_INST_FIELD + ") VALUES (" + DB_INST_VALUE + ")");
            }
            DB_INST_table_name = ""; //지정한 테이블을 초기화
            DB_INST_FIELD = "";
            DB_INST_VALUE = "";
        }

        public void SAVE_UPDATE_COM()
        {
            if (DB_UPDT_WHERE != "" && DB_UPDT_SET != "" && DB_UPDT_table_name != "") //둘다 비어있지 않아야.
            {
                sql_command.Enqueue("UPDATE `" + DATABASE + "`.`" + DB_UPDT_table_name + "` SET " + DB_UPDT_SET + " WHERE " + DB_UPDT_WHERE + "");
            }
            DB_UPDT_table_name = ""; //실행 후 초기화
            DB_UPDT_SET = "";
            DB_UPDT_WHERE = "";
        }

        public void SAVE_DELETE_COM()
        {
            SAVE_DELETE_COM(1);
        }

        public void SAVE_DELETE_COM(int LIMIT)
        {
            string str_limit = "";
            if (LIMIT == 0)
            {
                str_limit = "";
            }
            else
            {
                str_limit = " LIMIT " + LIMIT.ToString();
            }

            if (DB_DLTE_WHERE != "" && DB_DLTE_table_name != "")
            {
                sql_command.Enqueue("DELETE FROM `" + DB_DLTE_table_name + "` WHERE " + DB_DLTE_WHERE + str_limit + "");
            }
            DB_DLTE_WHERE = "";
            DB_DLTE_table_name = "";
        }
        */
        //foreach 를 사용해 쿼리를 뽑은담에 한번에 전송.
        /*
        public void EXECUTE_COM()
        {
            string command_list = "";
            if (sql_command.Count < 1)
            {
                return;
            }
            foreach (Object obj in sql_command)
            {
                command_list += (string)obj + "|"; //줄바꿈 문장포함.안하고 해보든가하자-
            }
            run_query(command_list); // 모아둔거 한방에 실행...

            command_list = "";//초기화
        }
        */
        /*  MySql SELECT 명령어 컨트롤    */

        private string DB_SLT_table_name = "";
        private string DB_SLT_FIELD_tmp = "";
        private string DB_SLT_WHERE_tmp = "";
        private string DB_SLT_ORDER_SC = "";
        private string DB_SLT_ORDER_DESC = "";

        public void DB_SELECT(string table_name) //가져올 테이블을 지정.
        {
            DB_SLT_table_name = table_name;
            DB_SLT_FIELD_tmp = "*"; //DB_SELECT_FIELD 함수를 사용하지 않는다면, 별도로 지정한 필드가 없으므로 모든 필드를 가져온다.
            DB_SLT_WHERE_tmp = "";
        }
        public void DB_SELECT_FIELD(string field)
        {
            if (DB_SLT_FIELD_tmp == "*") //첨이라면,?
            {
                DB_SLT_FIELD_tmp = " `" + field + "`";
            }
            else //두번째 이상부턴 쉼표를 사용해 추가
            {
                DB_SLT_FIELD_tmp += " , `" + field + "`";
            }
        }
        public void DB_SELECT_MAX_FIELD(string field) //최대값을 구하는 함수사용.
        {
            DB_SLT_FIELD_tmp = "MAX(" + field + ")";
        }
        public void DB_SELECT_WHERE(string field, string value)
        {
            if (DB_SLT_WHERE_tmp == "") //처음 where 필드를 지정하였다면,
            {
                DB_SLT_WHERE_tmp = "`" + field + "`" + "LIKE '" + value + "'";
            }
            else
            {
                DB_SLT_WHERE_tmp += "AND `" + field + "` LIKE '" + value + "'"; //두번째 부터는 AND를 사용하여 연결.
            }
        }

        public void DB_SELECT_WHERE(string field, int value)
        {
            DB_SELECT_WHERE(field, value.ToString());
        }

        public void DB_SELECT_WHERE(string field, int value, string Operator)
        {
            DB_SELECT_WHERE(field, value.ToString(), Operator);
        }

        public void DB_SELECT_WHERE(string field, string value, string Operator)
        {
            value = ch_qt(value);
            if (DB_SLT_WHERE_tmp == "") //처음 where 필드를 지정하였다면,
            {
                DB_SLT_WHERE_tmp = "`" + field + "`" + Operator + "'" + value + "'";
            }
            else
            {
                DB_SLT_WHERE_tmp += "AND `" + field + "` " + Operator + " '" + value + "'"; //두번째 부터는 AND를 사용하여 연결.
            }
        }

        public void DB_SELECT_WHERE_OR(string field, int value)
        {
            DB_SELECT_WHERE_OR(field, value.ToString());
        }
        public void DB_SELECT_WHERE_OR(string field, string value) //or연산자. 여러 조건을 한번에 보기 위해서.
        {
            value = ch_qt(value);
            if (DB_SLT_WHERE_tmp == "") //처음 where 필드를 지정하였다면,
            {
                DB_SLT_WHERE_tmp = "`" + field + "`" + "LIKE '" + value + "'";
            }
            else
            {
                DB_SLT_WHERE_tmp += "OR `" + field + "` LIKE '" + value + "'"; //두번째 부터는 OR를 사용하여 연결.
            }
        }
        public void DB_SELECT_WHERE_OR(string field, int value, string Operator)
        {
            DB_SELECT_WHERE_OR(field, value.ToString(), Operator);
        }
        public void DB_SELECT_WHERE_OR(string field, string value, string Operator) //or연산자. 여러 조건을 한번에 보기 위해서.
        {
            value = ch_qt(value);
            if (DB_SLT_WHERE_tmp == "") //처음 where 필드를 지정하였다면,
            {
                DB_SLT_WHERE_tmp = "`" + field + "`" + Operator + "'" + value + "'";
            }
            else
            {
                DB_SLT_WHERE_tmp += "OR `" + field + "` " + Operator + " '" + value + "'"; //두번째 부터는 AND를 사용하여 연결.
            }
        }

        public MySqlDataReader DB_SELECT_RUN() //별도로 지정하지않으면 맨 첨부터 30개의 레코드를 가져옴; //문제발생의 여지가 있어.. 60개로 증강. 혹 안되면.. 0으로.
        {
            return DB_SELECT_RUN(0);
        }

        public void DB_SELECT_ORDER_DESC(string field) // 역순 옵션 넣기,
        {
            DB_SLT_ORDER_SC = "ORDER BY `" + field + "` DESC ";
        }
        public void DB_SELECT_ORDER_ASC(string field) // 정순 옵션 넣기,
        {
            DB_SLT_ORDER_SC = "ORDER BY `" + field + "` ASC ";
        }

        public MySqlDataReader DB_SELECT_RUN(string query)
        {
            MySqlDataReader R = null;

            DB_SLT_FIELD_tmp = "*";
            DB_SLT_WHERE_tmp = "";
            DB_SLT_ORDER_SC = "";
            DB_SLT_table_name = ""; //지정 테이블 초기화
            try
            {
                R = query_select(query);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Error : ", ex);
            }
            return R;
        }
        public MySqlDataReader DB_SELECT_RUN(int sub_db)
        {
            MySqlDataReader R = null;

            if (DB_SLT_table_name != "")
            {
                try
                {
                    if (DB_SLT_WHERE_tmp == "")
                    {
                        R = query_select("SELECT " + DB_SLT_FIELD_tmp + " FROM `" + DB_SLT_table_name + "`" + DB_SLT_ORDER_SC, sub_db);
                    }
                    else
                    {
                        R = query_select("SELECT " + DB_SLT_FIELD_tmp + " FROM `" + DB_SLT_table_name + "` WHERE " + DB_SLT_WHERE_tmp + DB_SLT_ORDER_SC, sub_db);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DB Error : ", ex);
                }

            }
            DB_SLT_FIELD_tmp = "*";
            DB_SLT_WHERE_tmp = "";
            DB_SLT_ORDER_SC = "";
            DB_SLT_table_name = ""; //지정 테이블 초기화

            return R;
        }
        /*
        public MySqlDataReader DB_SELECT_RUN(int LIMIT)
        {
            MySqlDataReader R = null;
            string str_limit = "";

            if (LIMIT == 0)
            {
                str_limit = "";
            }
            else
            {
                str_limit = " LIMIT " + LIMIT.ToString();
            }

            if (DB_SLT_table_name != "")
            {
                if (DB_SLT_WHERE_tmp == "")
                {
                    R = query_select("SELECT " + DB_SLT_FIELD_tmp + " FROM `" + DB_SLT_table_name + "`" + DB_SLT_ORDER_SC + str_limit);
                }
                else
                {
                    R = query_select("SELECT " + DB_SLT_FIELD_tmp + " FROM `" + DB_SLT_table_name + "` WHERE " + DB_SLT_WHERE_tmp + DB_SLT_ORDER_SC + str_limit);
                }
            }
            DB_SLT_FIELD_tmp = "*";
            DB_SLT_WHERE_tmp = "";
            DB_SLT_ORDER_SC = "";
            DB_SLT_table_name = ""; //지정 테이블 초기화

            return R;
        }
        public void DB_SELECT_RUN(int LIMIT_A, int LIMIT_B)
        {
            string str_limit = "";

            str_limit = " LIMIT " + LIMIT_A.ToString() + ", " + LIMIT_B.ToString();

            if (DB_SLT_table_name != "")
            {
                if (DB_SLT_WHERE_tmp == "")
                {
                    run_query("SELECT " + DB_SLT_FIELD_tmp + " FROM `" + DB_SLT_table_name + "`" + DB_SLT_ORDER_SC + str_limit);
                }
                else
                {
                    run_query("SELECT " + DB_SLT_FIELD_tmp + " FROM `" + DB_SLT_table_name + "` WHERE " + DB_SLT_WHERE_tmp + DB_SLT_ORDER_SC + str_limit);
                }
            }
            DB_SLT_FIELD_tmp = "*";
            DB_SLT_WHERE_tmp = "";
            DB_SLT_ORDER_SC = "";
            DB_SLT_table_name = ""; //지정 테이블 초기화
        }
        */
        public MySqlDataReader DB_SELECT_RUN_COUNT()
        {
            MySqlDataReader R = null;

            if (DB_SLT_table_name != "")
            {
                try
                {
                    if (DB_SLT_WHERE_tmp == "")
                    {
                        R = query_select("SELECT " + "count( * )" + " FROM `" + DB_SLT_table_name + "`" + DB_SLT_ORDER_DESC);
                    }
                    else
                    {
                        R = query_select("SELECT " + "count( * )" + " FROM `" + DB_SLT_table_name + "` WHERE " + DB_SLT_WHERE_tmp + DB_SLT_ORDER_DESC);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DB Error : ", ex);
                }
            }
            DB_SLT_FIELD_tmp = "*";
            DB_SLT_WHERE_tmp = "";
            DB_SLT_ORDER_DESC = "";
            DB_SLT_table_name = ""; //지정 테이블 초기화 

            return R;
        }
        /****************************************/

        /*  MySql INSERT 명령어 컨트롤    */

        private string DB_INST_table_name = "";
        private string DB_INST_FIELD = "";
        private string DB_INST_VALUE = "";

        public void DB_INSERT(string table_name)
        {
            DB_INST_table_name = table_name;
            DB_INST_VALUE = "";
            DB_INST_FIELD = "";
        }

        public void DB_INSERT_FIELD(string field, string value)
        {
            value = ch_qt(value);
            if (DB_INST_FIELD == "") //처음 필드를 추가할때면
            {
                DB_INST_FIELD = "`" + field + "`";
                DB_INST_VALUE = "'" + value + "'";
            }
            else
            {
                DB_INST_FIELD += ", `" + field + "`";
                DB_INST_VALUE += ", '" + value + "'";
            }
        }
        public void DB_INSERT_FIELD(string field, int value)
        {
            DB_INSERT_FIELD(field, value.ToString());
        }
        public long DB_INSERT_RUN()
        {
            if (DB_INST_FIELD != "" && DB_INST_VALUE != "" && DB_INST_table_name != "")
            {
                return run_query("INSERT INTO `" + DATABASE + "`.`" + DB_INST_table_name + "` (" + DB_INST_FIELD + ") VALUES (" + DB_INST_VALUE + ");");
            }
            else
            {
                DB_INST_table_name = ""; //지정한 테이블을 초기화
                return 0;
            }

        }


        /**********************************/

        /*  MySql UPDATE 명령어 컨트롤    */

        private string DB_UPDT_table_name = "";
        private string DB_UPDT_SET = "";
        private string DB_UPDT_WHERE = "";

        public void DB_UPDATE(string table_name)
        {
            DB_UPDT_table_name = table_name;
            DB_UPDT_WHERE = "";
            DB_UPDT_SET = "";
        }
        public void DB_UPDATE_SET(string field, string value)
        {
            value = ch_qt(value);
            if (DB_UPDT_SET == "")
            {
                DB_UPDT_SET = "`" + field + "` = '" + value + "'";
            }
            else
            {
                DB_UPDT_SET += ", `" + field + "` = '" + value + "'";
            }
        }
        public void DB_UPDATE_SET_count(string field, int value)
        {
            if (DB_UPDT_SET == "")
            {
                DB_UPDT_SET = "`" + field + "` = " + field + " + " + value;
            }
            else
            {
                DB_UPDT_SET += ", `" + field + "` = " + field + " + " + value;
            }
        }
        public void DB_UPDATE_SET(string field, int value)
        {
            DB_UPDATE_SET(field, value.ToString());
        }
        public void DB_UPDATE_WHERE(string field, string value)
        {
            value = ch_qt(value);
            if (DB_UPDT_WHERE == "")
            {
                DB_UPDT_WHERE = "`" + field + "`" + " LIKE '" + value + "'";
            }
            else
            {
                DB_UPDT_WHERE += "AND `" + field + "`" + " LIKE '" + value + "'"; //두번째 부터는 AND를 사용하여 연결.
            }
        }
        public void DB_UPDATE_WHERE(string field, int value)
        {
            DB_UPDATE_WHERE(field, value.ToString());
        }
        public void DB_UPDATE_RUN()
        {
            if (DB_UPDT_WHERE != "" && DB_UPDT_SET != "" && DB_UPDT_table_name != "") //둘다 비어있지 않아야.
            {
                MySqlDataReader RD;
                int i = 0;
                try
                {
                    RD = query_select("UPDATE `" + DATABASE + "`.`" + DB_UPDT_table_name + "` SET " + DB_UPDT_SET + " WHERE " + DB_UPDT_WHERE);
                }
                catch
                {
                    RD = query_select("UPDATE `" + DATABASE + "`.`" + DB_UPDT_table_name + "` SET " + DB_UPDT_SET + " WHERE " + DB_UPDT_WHERE, ++i);
                }

                RD.Close();
            }
            DB_UPDT_table_name = ""; //실행 후 초기화
        }
        /**********************************/


        /*  MySql DELETE 명령어 컨트롤    */

        private string DB_DLTE_table_name = "";
        private string DB_DLTE_WHERE = "";

        public void DB_DELETE(string table_name)
        {
            DB_DLTE_table_name = table_name;
            DB_DLTE_WHERE = "";
        }

        public void DB_DELETE_WHERE(string field, string value)
        {
            value = ch_qt(value);
            if (DB_DLTE_WHERE == "")
            {
                DB_DLTE_WHERE = "`" + field + "` = '" + value + "'";
            }
            else
            {
                DB_DLTE_WHERE += "AND `" + field + "` LIKE '" + value + "'"; //and를 사용하여 연결. 
            }
        }
        public void DB_DELETE_WHERE(string field, int value)
        {
            DB_DELETE_WHERE(field, value.ToString());
        }
        public void DB_DELETE_RUN()
        {
            DB_DELETE_RUN(1);
        }
        public void DB_DELETE_RUN(int LIMIT) //LIMIT를 지정을 하는부분, 지정을 하지 않고 그냥 실행하면 하나만삭제(LIMIT 1), 0이면 무제한, 숫자면 숫자만큼^^
        {
            string str_limit = "";
            if (LIMIT == 0)
            {
                str_limit = "";
            }
            else
            {
                str_limit = " LIMIT " + LIMIT.ToString();
            }

            if (DB_DLTE_WHERE != "" && DB_DLTE_table_name != "")
            {
                run_query("DELETE FROM `" + DB_DLTE_table_name + "` WHERE " + DB_DLTE_WHERE + str_limit);
            }
            DB_DLTE_WHERE = "";
            DB_DLTE_table_name = "";
        }
    }
}
