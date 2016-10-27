using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Tefreeca_Structure
{
    public static class DB
    {
        public static void DB_init()
        {
            //Order
            Order.Columns.Add(new DataColumn("No", typeof(int)));
            Order.Columns.Add(new DataColumn("User_No", typeof(int)));
            Order.Columns.Add(new DataColumn("Connected_count", typeof(int)));
            Order.Columns.Add(new DataColumn("Max_connect", typeof(int)));
            Order.Columns.Add(new DataColumn("URL", typeof(string)));
            Order.Columns.Add(new DataColumn("Type_order", typeof(Type_Order)));
            Order.Columns.Add(new DataColumn("State", typeof(State_Order)));

            Order.PrimaryKey = new DataColumn[] { Order.Columns["No"] };


            Unit.Columns.Add(new DataColumn("No", typeof(int)));
            Unit.Columns.Add(new DataColumn("Connected_count", typeof(int)));
            Unit.Columns.Add(new DataColumn("Max_connect", typeof(int)));
            Unit.Columns.Add(new DataColumn("Proxy", typeof(string)));
            Unit.Columns.Add(new DataColumn("State", typeof(State_Unit)));
            Unit.Columns.Add(new DataColumn("Wait_count", typeof(int)));

            Unit.PrimaryKey = new DataColumn[] { Unit.Columns["No"] };


            Proxy.Columns.Add(new DataColumn("No", typeof(int)));
            Proxy.Columns.Add(new DataColumn("Host", typeof(string)));
            Proxy.Columns.Add(new DataColumn("Error_count", typeof(int)));
            Proxy.Columns.Add(new DataColumn("State", typeof(State_Proxy)));
            Proxy.Columns.Add(new DataColumn("Type", typeof(Type_Proxy)));

            Proxy.PrimaryKey = new DataColumn[] { Proxy.Columns["No"] };


            User.Columns.Add(new DataColumn("No", typeof(int)));
            User.Columns.Add(new DataColumn("Unit_No", typeof(int)));
            User.Columns.Add(new DataColumn("slot", typeof(int)));
            User.Columns.Add(new DataColumn("slot_account", typeof(int)));
            User.Columns.Add(new DataColumn("fee", typeof(int)));
            User.Columns.Add(new DataColumn("id", typeof(string)));
            User.Columns.Add(new DataColumn("phone", typeof(string)));
            User.Columns.Add(new DataColumn("email", typeof(string)));
            User.Columns.Add(new DataColumn("pay_type", typeof(string)));
            User.Columns.Add(new DataColumn("session", typeof(string)));
            User.Columns.Add(new DataColumn("expire", typeof(DateTime)));
            User.Columns.Add(new DataColumn("User_type", typeof(Type_User)));
            User.Columns.Add(new DataColumn("is_Reseller", typeof(bool)));
            User.Columns.Add(new DataColumn("my_Reseller", typeof(int)));

            User.PrimaryKey = new DataColumn[] { User.Columns["No"] };


            Connection.Columns.Add(new DataColumn("No", typeof(int)));
            Connection.Columns.Add(new DataColumn("Order_No", typeof(int)));
            Connection.Columns.Add(new DataColumn("Unit_No", typeof(int)));
            Connection.Columns.Add(new DataColumn("Connect_URL", typeof(string)));
            Connection.Columns.Add(new DataColumn("Proxy_host", typeof(string)));
            Connection.Columns.Add(new DataColumn("Type_order", typeof(Type_Order)));
            Connection.Columns.Add(new DataColumn("State", typeof(State_Connect)));
            Connection.Columns.Add(new DataColumn("Login_Account_no", typeof(int)));
            Connection.Columns.Add(new DataColumn("State_Account_on_connected", typeof(State_Account_on_connected)));
            Connection.Columns.Add(new DataColumn("Wait_count", typeof(int)));
            
            Connection.PrimaryKey = new DataColumn[] { Connection.Columns["No"] };
            

            Account.Columns.Add(new DataColumn("No", typeof(int)));
            Account.Columns.Add(new DataColumn("Site", typeof(Type_Order)));
            Account.Columns.Add(new DataColumn("User_No", typeof(int)));
            Account.Columns.Add(new DataColumn("ID", typeof(string)));
            Account.Columns.Add(new DataColumn("PW", typeof(string)));
            Account.Columns.Add(new DataColumn("State", typeof(State_Account)));
            
            //소유한 유저의 정보 0이면 공용 소유자가 있으면 그 사용자의 주문에만 배정

            Account.PrimaryKey = new DataColumn[] { Account.Columns["No"] };
        }

        public static void DB_init_(ref DataTable DB_Connection)
        {
            DB_Connection = new DataTable("Connection");
            /*
            DB_Connection.Columns.Add(new DataColumn("No", typeof(int)));
            DB_Connection.Columns.Add(new DataColumn("Order_No", typeof(int)));
            DB_Connection.Columns.Add(new DataColumn("Connect_URL", typeof(string)));
            DB_Connection.Columns.Add(new DataColumn("Proxy_host", typeof(string)));
            DB_Connection.Columns.Add(new DataColumn("State", typeof(State_Connect)));
            DB_Connection.Columns.Add(new DataColumn("Type_order", typeof(Type_Order)));
            */

            DB_Connection.Columns.Add(new DataColumn("No", typeof(int)));
            DB_Connection.Columns.Add(new DataColumn("Order_No", typeof(int)));
            DB_Connection.Columns.Add(new DataColumn("Connect_URL", typeof(string)));
            DB_Connection.Columns.Add(new DataColumn("Proxy_host", typeof(string)));
            DB_Connection.Columns.Add(new DataColumn("Type_order", typeof(Type_Order)));
            DB_Connection.Columns.Add(new DataColumn("State", typeof(State_Connect)));
            DB_Connection.Columns.Add(new DataColumn("Login_Account_no", typeof(int)));
            DB_Connection.Columns.Add(new DataColumn("Account_ID", typeof(string)));
            DB_Connection.Columns.Add(new DataColumn("Account_PW", typeof(string)));
            DB_Connection.Columns.Add(new DataColumn("State_Account_on_connected", typeof(State_Account_on_connected)));


            DB_Connection.PrimaryKey = new DataColumn[] { DB_Connection.Columns["No"] };
        }

        public static DataTable Order = new DataTable("Order");
        public static DataTable Unit = new DataTable("Unit");
        public static DataTable Proxy = new DataTable("Proxy");
        public static DataTable User = new DataTable("User");
        public static DataTable Connection = new DataTable("Connection");
        public static DataTable Account = new DataTable("Account");

        public static index_generator Newindex = new index_generator();

    }
}
