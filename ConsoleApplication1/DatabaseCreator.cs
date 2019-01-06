using System;
using System.Text;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.IO;

namespace DatabaseCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Init
            Console.WriteLine("建立数据库...");
            OleDbDataReader reader;
            string dConnS = "Data Source=(local)\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True";
            bool flag = true;
            SqlConnection dbConn;
            flag:
            dbConn = new SqlConnection(dConnS);
            try
            {
                dbConn.Open();
            }
            catch (SqlException exp)
            {
                if (flag)
                {
                    dConnS = "Data Source=(local);Initial Catalog=master;Integrated Security=True";
                    flag = false;
                }
                else
                {
                    Console.WriteLine("无法自动连接数据库，请手动输入DataSource。\n例如(local)\\SQLEXPRESS");
                    dConnS = "Data Source=" + Console.ReadLine() + ";Initial Catalog=master;Integrated Security=True";
                }
                goto flag;
            }
            SqlDataReader re;
            re = new SqlCommand("select count(*) from sys.databases where name='plent' or name ='insect' or name ='users'", dbConn).ExecuteReader();
            re.Read();
            if (int.Parse(re.GetValue(0).ToString()) > 0)
            {
                re.Close();
                Console.WriteLine("检测到已存在数据库plent或insect或users。\n即将删库并重新建立。输入9确认，按任意键结束程序。");
                if (Console.ReadKey().KeyChar == 57)
                    try
                    {
                        new SqlCommand("drop database plent,insect,users", dbConn).ExecuteNonQuery();
                        Console.WriteLine("删库完成");
                    }
                    catch (Exception exp) { }
                else
                {
                    dbConn.Close();
                    return;
                }
            }
            else re.Close(); 
            new SqlCommand("create database insect\ncreate database plent\ncreate database users", dbConn).ExecuteNonQuery();
            dbConn.Close();
            Console.WriteLine("建库完成");
            #endregion

            #region users
            Console.WriteLine("用户库建表...");
            dbConn = new SqlConnection("Data Source=(local)"+(flag?"\\SQLEXPRESS":"")+";Initial Catalog=users;Integrated Security=True");
            dbConn.Open();
            new SqlCommand(File.ReadAllText(".\\sqlusers.txt", Encoding.Default), dbConn).ExecuteNonQuery();
            dbConn.Close();
            #endregion

            #region insect
            Console.WriteLine("昆虫库建表...");
            dbConn = new SqlConnection("Data Source=(local)" + (flag ? "\\SQLEXPRESS" : "") + ";Initial Catalog=insect;Integrated Security=True");
            dbConn.Open();
            new SqlCommand(File.ReadAllText(".\\sqlinsect.txt",Encoding.Default), dbConn).ExecuteNonQuery();
            string eConnS = "Provider=Microsoft.Ace.OleDb.12.0;Data Source=.\\insect.xlsx;Extended Properties='Excel 8.0;HDR=yes;IMEX=1'";
            OleDbConnection OleConn = new OleDbConnection(eConnS);
            dbConn.Close();

            string insert;

            Console.WriteLine("填充目表...");
            OleConn.Open();
            reader = new OleDbCommand("SELECT * FROM  [目$]", OleConn).ExecuteReader();
            insert = "INSERT INTO orders VALUES ";
            if(reader.Read())
                insert += "('" + reader.GetValue(0) + "','" + reader.GetValue(1) + "','" + reader.GetValue(2) + "')";
            while (reader.Read())
                insert += ",('" + reader.GetValue(0) + "','" + reader.GetValue(1) + "','" + reader.GetValue(2) + "')";
            OleConn.Close();
            dbConn.Open();
            new SqlCommand(insert, dbConn).ExecuteNonQuery();
            dbConn.Close();

            Console.WriteLine("填充科表...");
            OleConn.Open();
            reader = new OleDbCommand("SELECT * FROM  [科$]", OleConn).ExecuteReader();
            insert = "INSERT INTO family VALUES ";
            if (reader.Read())
                insert += "('" + reader.GetValue(0) + "','" + reader.GetValue(1) + "','" + reader.GetValue(2) + "',(select oname from orders where c_oname='" + reader.GetValue(3) + "'))";
            while (reader.Read())
                insert += ",('" + reader.GetValue(0) + "','" + reader.GetValue(1) + "','" + reader.GetValue(2) + "',(select oname from orders where c_oname='" + reader.GetValue(3) + "'))";
            OleConn.Close();
            dbConn.Open();
            new SqlCommand(insert, dbConn).ExecuteNonQuery();
            dbConn.Close();

            Console.WriteLine("填充种表...");
            OleConn.Open();
            reader = new OleDbCommand("SELECT * FROM  [种$]", OleConn).ExecuteReader();
            insert = "INSERT INTO species VALUES ";
            if (reader.Read())
                insert += "(" + reader.GetValue(0) + ",'" + reader.GetValue(1) + "','" + reader.GetValue(2) + "',(select fname from family where c_fname='" + reader.GetValue(3) + "'))";
            while (reader.Read())
                insert += ",(" + reader.GetValue(0) + ",'" + reader.GetValue(1) + "','" + reader.GetValue(2) + "',(select fname from family where c_fname='" + reader.GetValue(3) + "'))";
            OleConn.Close();
            dbConn.Open();
            new SqlCommand(insert, dbConn).ExecuteNonQuery();
            dbConn.Close();

            Console.WriteLine("填充特征表...");
            OleConn.Open();
            reader = new OleDbCommand("SELECT * FROM  [特征$]", OleConn).ExecuteReader();
            insert = "INSERT INTO features VALUES ";
            if (reader.Read())
                insert += "(" + reader.GetValue(0) + ",'" + reader.GetValue(1) + "','" + reader.GetValue(2) + "','" + reader.GetValue(3) + "','" + reader.GetValue(4) + "'," + (reader.GetValue(5).ToString())[0] + ")";
            while (reader.Read())
                try { insert += ",(" + reader.GetValue(0) + ",'" + reader.GetValue(1) + "','" + reader.GetValue(2) + "','" + reader.GetValue(3) + "','" + reader.GetValue(4) + "'," + (reader.GetValue(5).ToString())[0] + ")"; }
                catch (Exception exp) { }
            OleConn.Close();
            dbConn.Open();
            new SqlCommand(insert, dbConn).ExecuteNonQuery();
            dbConn.Close();

            Console.WriteLine("昆虫库添加存储过程");
            dbConn.Open();
            string si = File.ReadAllText(".\\insect.txt", Encoding.Default);
            int leni = si.Length;
            int lasti = 0;
            for (int i = 0; i < leni; i++)
                if (si[i] == ';')
                {
                    new SqlCommand(Extract(si, lasti, i), dbConn).ExecuteNonQuery();
                    lasti = i + 1;
                }
            Console.WriteLine("昆虫库完成");
            #endregion
            
            #region plent
            Console.WriteLine("植物库建表...");
            dConnS = "Data Source=(local)" + (flag ? "\\SQLEXPRESS" : "") + ";Initial Catalog=plent;Integrated Security=True";
            eConnS = "Provider=Microsoft.Ace.OleDb.12.0;Data Source=.\\plent.xlsx;Extended Properties='Excel 8.0;HDR=yes;IMEX=1'";
            OleConn = new OleDbConnection(eConnS);
            dbConn = new SqlConnection(dConnS);
            dbConn.Open();
            new SqlCommand(File.ReadAllText(".\\sqlplent.txt", Encoding.Default), dbConn).ExecuteNonQuery();
            dbConn.Close();
            
            Console.WriteLine("填充纲表...");
            OleConn.Open();
            reader = new OleDbCommand("SELECT * FROM  [纲$]", OleConn).ExecuteReader();
            insert = "INSERT INTO class VALUES ";
            if (reader.Read())
                insert += "('" + reader.GetValue(0) + "','" + reader.GetValue(1) + "','" + reader.GetValue(2) + "')";
            while (reader.Read())
                insert += ",('" + reader.GetValue(0) + "','" + reader.GetValue(1) + "','" + reader.GetValue(2) + "')";
            OleConn.Close();
            dbConn.Open();
            new SqlCommand(insert, dbConn).ExecuteNonQuery();
            dbConn.Close();
            
            Console.WriteLine("填充科表...");
            OleConn.Open();
            reader = new OleDbCommand("SELECT * FROM  [科$]", OleConn).ExecuteReader();
            while (reader.Read())
                insert += "INSERT INTO family VALUES " + "('" + reader.GetValue(0) + "','" + reader.GetValue(1) + "','" + reader.GetValue(2) + "',(select cname from class where c_cname='" + reader.GetValue(3) + "'))";
            OleConn.Close();
            dbConn.Open();
            try { new SqlCommand(insert, dbConn).ExecuteNonQuery(); }
            catch (Exception exp) { }
            dbConn.Close();

            Console.WriteLine("填充种表...");
            OleConn.Open();
            reader = new OleDbCommand("SELECT * FROM  [种$]", OleConn).ExecuteReader();
            insert = "INSERT INTO species VALUES ";
            if (reader.Read())
                insert += "(" + reader.GetValue(0) + ",'" + reader.GetValue(1) + "','" + reader.GetValue(2) + "',(select fname from family where c_fname='" + reader.GetValue(3) + "'))";
            while (reader.Read())
                if (reader.GetValue(0).ToString() == "") break;
                else
                    insert += ",(" + reader.GetValue(0) + ",'" + reader.GetValue(1) + "','" + reader.GetValue(2) + "',(select fname from family where c_fname='" + reader.GetValue(3) + "'))";
            OleConn.Close();
            dbConn.Open();
            new SqlCommand(insert, dbConn).ExecuteNonQuery();
            dbConn.Close();

            Console.WriteLine("填充特征表...");
            OleConn.Open();
            reader = new OleDbCommand("SELECT * FROM  [特征$]", OleConn).ExecuteReader();
            insert = "INSERT INTO features VALUES ";
            if (reader.Read())
                insert += "(" + reader.GetValue(0) + ",'" + reader.GetValue(1) + "','" + reader.GetValue(2) + "','" + reader.GetValue(3) + "','" + reader.GetValue(4) + "','" + reader.GetValue(5) + "','" + reader.GetValue(6) + "')";
            while (reader.Read())
                insert += ",(" + reader.GetValue(0) + ",'" + reader.GetValue(1) + "','" + reader.GetValue(2) + "','" + reader.GetValue(3) + "','" + reader.GetValue(4) + "','" + reader.GetValue(5) + "','" + reader.GetValue(6) + "')";
            OleConn.Close();
            dbConn.Open();
            new SqlCommand(insert, dbConn).ExecuteNonQuery();
            dbConn.Close();

            Console.WriteLine("植物库填充存储过程...");
            dbConn = new SqlConnection(dConnS);
            dbConn.Open();
            string sp = File.ReadAllText(".\\plent.txt", Encoding.Default);
            int len=sp.Length;
            int lastp = 0;
            for(int i=0;i<len;i++)
                if (sp[i] == ';')
                {
                    new SqlCommand(Extract(sp, lastp, i), dbConn).ExecuteNonQuery();
                    lastp = i + 1;
                }
            dbConn.Close();
            #endregion
            Console.WriteLine("植物库完成\n建立数据库完成\n按任意键退出");
            Console.ReadKey();
        }

        public static string Extract(string init, int head, int end)
        {
            string temp = "";
            for (int i = head; i < end; i++)
                temp += init[i];
            return temp;
        }
    }
}
