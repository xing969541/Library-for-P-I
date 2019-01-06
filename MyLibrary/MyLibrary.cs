using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Security.Cryptography;
using System.Diagnostics;

namespace MyLibrary
{
    /// <summary>
    /// 父子节点对，用于存储成对出现的数据。
    /// </summary>
    public class FSPair
    {
        private int father;
        private int son;

        /// <summary>
        /// 父子节点对 构造函数
        /// </summary>
        /// <param name="father">父节点名称</param>
        /// <param name="son">子节点名称</param>
        public FSPair(int father, int son)
        {
            this.father = father;
            this.son = son;
        }

        /// <summary>
        /// 读取或修改父值。
        /// </summary>
        public int Father
        {
            get { return father; }
            set { father = value; }
        }

        /// <summary>
        /// 读取或修改子值。
        /// </summary>
        public int Son
        {
            get { return son; }
            set { son = value; }
        }
    }

    namespace DataBaseOperator
    {
        /// <summary>
        /// 本类用于C#与数据库间的交互，为抽象类。
        /// 为适用于不同结构的数据库，应从本类继承后进行扩展。
        /// </summary>
        public abstract class DataBaseOperator
        {
            protected SqlConnection conn;
            private DataSet ds = new DataSet();
            protected SqlDataReader reader;
            protected byte flag = 0;
            protected string[] dataSource = { "\\SQLEXPRESS", "" };

            #region Constructor and Destructor
            /// <summary>
            /// 无参构造函数，自动连接至本地服务器master数据库。
            /// </summary>
            public DataBaseOperator()
            {
            e:
                conn = new SqlConnection("Data Source=(local)" + dataSource[flag] + ";Initial Catalog=master;Integrated Security=True");
                try
                {
                    conn.Open();
                }
                catch (SqlException exp)
                {
                    flag++;
                    goto e;
                }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 构造函数。初始化与数据库的连接。
            /// </summary>
            /// <param name="connectString">数据库连接信息，用于初始化SqlConnection类。</param>
            public DataBaseOperator(string connectString)
            {
                conn = new SqlConnection(connectString);
            }

            /// <summary>
            /// 构造函数。初始化与数据库的连接。
            /// </summary>
            /// <param name="dataSource">服务器名</param>
            /// <param name="initalCatalog">数据库名</param>
            public DataBaseOperator(string dataSource, string initalCatalog)
            {
                string connectString = @"Data Source=" + dataSource + ";Initial Catalog=" + initalCatalog + ";Integrated Security=True";
                conn = new SqlConnection(connectString);
            }

            /// <summary>
            /// 析构函数。用于确保数据库连接的关闭，避免未知错误。
            /// </summary>
            ~DataBaseOperator()
            {
                conn.Close();
            }
            #endregion

            /// <summary>
            /// 获取当前数据库连接字符串
            /// </summary>
            /// <returns></returns>
            public string GetDataSource()
            {
                return conn.DataSource;
            }

            /// <summary>
            /// 在数据库中执行任意SQL语句。返回受影响的行数。
            /// </summary>
            /// <param name="SQLsentence">需执行的SQL语句。</param>
            /// <returns>返回受影响的行数。</returns>
            public virtual int SqlExecute(string SQLsentence)
            {
                try
                {
                    conn.Open();
                    return new SqlCommand(SQLsentence, conn).ExecuteNonQuery();
                }
                catch (Exception exp) { throw exp; }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 获取某张表内所有数据。返回一个数据集。查询所得表索引为0。
            /// </summary>
            /// <param name="SQLsentence">所需表的名字。</param>
            /// <returns>返回包含所需表的数据集。表在其中索引为表名。</returns>
            public virtual DataSet SqlSelect(string SQLsentence)
            {
                ds.Clear();
                conn.Open();
                try
                {
                    new SqlDataAdapter(SQLsentence, conn).Fill(ds);
                    return ds;
                }
                catch (Exception exp) { throw exp; }
                finally { conn.Close(); }
            }

        }

        /*
        /// <summary>
        /// 本类用于C#与树形表间的交互。
        /// 继承自DataBaseOperator。
        /// 树形表命名要求如下：
        ///     主键: tnum;左右标识: tleft,tright(NOT NULL)
        ///     表名: tree
        /// 本类已封闭，无法继承。
        /// </summary>
        public sealed class TreeDataBaseOperator : DataBaseOperator
        {
            private static int count;//统计表中行数（节点数）

            #region Constructor
            /// <summary>
            /// 构造函数。初始化与数据库的连接。
            /// </summary>
            /// <param name="connectString">数据库连接信息，用于初始化SqlConnection类。</param>
            public TreeDataBaseOperator(string connectString)
                : base(connectString)
            {
                string countTable = "select count(*) from tree";
                da = new SqlDataAdapter(countTable, conn);
                da.Fill(ds, "count");
                count = int.Parse(ds.Tables["count"].Rows[0].ItemArray[0].ToString());//记录表中行数
                ds.Clear();
                conn.Open();
            }

            /// <summary>
            /// 构造函数。初始化与数据库的连接。
            /// </summary>
            /// <param name="dataSource">服务器名</param>
            /// <param name="initalCatalog">数据库名</param>
            public TreeDataBaseOperator(string dataSource, string initalCatalog)
                : base(dataSource, initalCatalog)
            {
                string countTable = "select count(*) from tree";
                da = new SqlDataAdapter(countTable, conn);
                da.Fill(ds, "count");
                count = int.Parse(ds.Tables["count"].Rows[0].ItemArray[0].ToString());//记录表中行数
                ds.Clear();
            }
            #endregion

            #region Select
            /// <summary>
            /// 返回表中行数
            /// </summary>
            public int ElementNum
            {
                get { return count; }
            }

            /// <summary>
            /// 检查序号是否在表中，返回布尔值。
            /// </summary>
            /// <param name="num">序号</param>
            /// <returns>返回值为True则该序号在表中。</returns>
            public bool exist(int num)
            {
                string sql = "select count(*) from tree where tnum='" + num + "'";
                da = new SqlDataAdapter(sql, conn);
                da.Fill(ds, "a");
                try
                {
                    return ds.Tables["a"].Rows[0].ItemArray[0].ToString() == "1";
                }
                finally { ds.Clear(); }
            }

            /// <summary>
            /// 获取某序号在数据库中的左右值。返回父子节点对。
            /// </summary>
            /// <param name="num">学名</param>
            /// <returns>父为左值,子为右值。若该学名不存在将返回两个-1。</returns>
            private FSPair getLR(int num)
            {
                if (exist(num))
                {
                    string sql = "select tleft,tright from tree where tnum='" + num + "'";
                    da = new SqlDataAdapter(sql, conn);
                    da.Fill(ds, "a");
                    try
                    {
                        return new FSPair(int.Parse(ds.Tables["a"].Rows[0].ItemArray[1].ToString()), int.Parse(ds.Tables["a"].Rows[0].ItemArray[2].ToString()));
                    }
                    finally { ds.Clear(); }
                }
                else
                    return new FSPair(-1, -1);
            }

            /// <summary>
            /// 检查节点是否为叶子节点。当本表完整时，可用于判断该序号是否代表表中存在的种名（或亚种名）。
            /// </summary>
            /// <param name="num">所需查询的节点名。</param>
            /// <returns>当本节点为叶子节点时返回True，不是叶子节点或节点不存在时返回False。</returns>
            public bool isLeaf(int num)
            {
                FSPair temp = getLR(num);
                return temp.Son - temp.Father == 1;
            }

            /// <summary>
            /// 查询从目标节点至根节点的路径。返回字符串数组。
            /// </summary>
            /// <param name="num">目标节点。</param>
            /// <param name="order">不输入或输入True时返回顺序为从根节点至目标节点。
            /// 输入False时返回顺序为从目标节点至根节点。</param>
            /// <returns></returns>
            public int[] toRoot(int num, bool order = true)
            {
                FSPair temp = getLR(num);
                if (temp.Father == -1)
                    return null;
                string sql = "select tnum from tree where tleft<=" + temp.Father + " and tright >=" + temp.Son + " order by tleft " + (order ? "asc" : "desc");
                da = new SqlDataAdapter(sql, conn);
                da.Fill(ds, "a");
                try
                {
                    int[] s = new int[ds.Tables["a"].Rows.Count];
                    int len = ds.Tables["a"].Rows.Count;
                    for (int i = 0; i < len; i++)
                        s[i] = int.Parse(ds.Tables["a"].Rows[i].ItemArray[3].ToString());
                    return s;
                }
                finally { ds.Clear(); }
            }//存在未解决问题

            /// <summary>
            /// 查询目标节点的所有孩子节点。返回数组。
            /// </summary>
            /// <param name="num">目标节点。</param>
            /// <returns>数组内为目标节点所有孩子节点。</returns>
            public int[] getAllChild(int num)
            {
                FSPair temp = getLR(num);
                if (temp.Father == -1)
                    return null;
                string sql = "select tnum from tree where tleft>" + temp.Father + " and tright <" + temp.Son + " order by tleft asc";
                da = new SqlDataAdapter(sql, conn);
                da.Fill(ds, "a");
                try
                {
                    int[] s = new int[ds.Tables["a"].Rows.Count];
                    for (int i = 0; i < ds.Tables["a"].Rows.Count; i++)
                        s[i] = int.Parse(ds.Tables["a"].Rows[i].ItemArray[3].ToString());
                    return s;
                }
                finally { ds.Clear(); }
            }//存在同上问题
            #endregion

            //private int[][] getAllInfo(int num)
            //public int[] getAllSon(int num)
            //{
            //    //FSPair temp = getLR(num);
            //    //if (temp.Father == -1)
            //    //    return null;
            //    //int[] allChild = getAllChildren(num);
            //    //if (allChildren == null)
            //    //    return null;
            //}
            //public int[] getAllBrother(int num)
            //public int[] getAllLeaf(int num)

            #region  Insert
            /// <summary>
            /// 用于插入父子节点对至表中。
            /// 除表为空外，只可在父节点存在时插入其子节点，或插入目前根节点的父节点。
            /// 理论上可通过多次遍历，从任一节点开始构建整棵树。
            /// 返回布尔值。
            /// </summary>
            /// <param name="pair">所需插入的父子节点对</param>
            /// <returns>插入成功则返回值为True。</returns>
            public bool insert(FSPair pair)
            {
                conn.Open();
                try
                {
                    int temp;
                    if (count == 0)
                    {
                        string sql = "insert into tree values('" + pair.Father + "',1,4),('" + pair.Son + "',2,3)";
                        comm = new SqlCommand(sql, conn);
                        temp = comm.ExecuteNonQuery();
                        count = 2;
                        return true;
                    }
                    else
                    {
                        if (exist(pair.Father))
                        {
                            string sql = "insert into tree values('" + pair.Son + "',(select tleft from tree where tnum='" + pair.Father + "')+1,(select tleft from tree where tnum='" + pair.Father + "')+2) ";
                            comm = new SqlCommand(sql, conn);
                            temp = comm.ExecuteNonQuery();
                            sql = "update tree set tleft+=2 where tleft>=(select tleft from tree where tnum='" + pair.Son + "') and tnum!='" + pair.Son + "' " +
                                "update tree set tright+=2 where tright>=(select tleft from tree where tnum='" + pair.Son + "') and tnum!='" + pair.Son + "'";
                            comm = new SqlCommand(sql, conn);
                            comm.ExecuteNonQuery();
                            if (temp == 0)
                                return false;
                            else
                            {
                                count++;
                                return true;
                            }
                        }
                        else
                        {
                            if (getLR(pair.Son).Father == 1)
                            {
                                string sql = "insert into tree values ('" + pair.Father + "',1,(select tright from tree where tnum='" + pair.Son + "')+2) update tree set tleft+=1 , tright+=1 where tnum!='" + pair.Father + "'";
                                comm = new SqlCommand(sql, conn);
                                temp = comm.ExecuteNonQuery();
                                if (temp == 0)
                                    return false;
                                else
                                {
                                    count++;
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 用于批量插入父子节点对至表中。
            /// 除表为空外，只可在父节点存在时插入其子节点，或插入目前根节点的父节点。
            /// 理论上可通过多次遍历，从任一节点开始构建整棵树。
            /// 返回成功插入的父子节点对数量。
            /// </summary>
            /// <param name="pair">需要插入的父子节点对。</param>
            /// <returns>返回成功插入的父子节点对数量。</returns>
            public int insert(FSPair[] pair)
            {
                int flag = 0;
                for (int i = 0; i < pair.Length; i++)
                    try
                    {
                        if (insert(pair[i])) flag++;
                    }
                    catch (Exception exp) { }
                return flag;
            }
            #endregion

            #region  Update
            /// <summary>
            /// 修改表中序号为新序号。返回布尔值。
            /// </summary>
            /// <param name="num">需替换的序号</param>
            /// <param name="newnum">替换后的新序号</param>
            /// <returns>修改成功将返回True。</returns>
            public bool update(int num, int newnum)
            {
                string sql = "update tree set tnum ='" + newnum + "' where tnum='" + num + "'";
                conn.Open();
                try
                {
                    comm = new SqlCommand(sql, conn);
                    return comm.ExecuteNonQuery() == 1;
                }
                catch (SqlException sqlExp) { throw sqlExp; }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 读入父子节点对，修改表中序号。用子值替换父值。返回布尔值。
            /// </summary>
            /// <param name="pair">请确保父为旧值，子为新值。</param>
            /// <returns>修改成功将返回True。</returns>
            public bool update(FSPair pair)
            {
                try { return update(pair.Father, pair.Son); }
                catch (SqlException sqlExp) { throw sqlExp; }
            }
            #endregion

            #region Delete
            /// <summary>
            /// 删除节点，同时销毁所有子节点。返回删除的节点数。
            /// 可用本函数删库。请慎重使用本函数，并在执行前再次确认。
            /// </summary>
            /// <param name="num">所需删除的节点名称。</param>
            /// <returns>返回删除的节点数。节点不存在时返回-1。</returns>
            public int delete(int num)
            {
                FSPair p = getLR(num);
                if (p.Father != -1)
                {
                    string sql = "delete from tree where tleft>=" + p.Father + " and tright<= " + p.Son;
                    conn.Open();
                    try
                    {
                        comm = new SqlCommand(sql, conn);
                        int temp = comm.ExecuteNonQuery();
                        count -= temp;
                        if (p.Son - p.Father + 1 != temp * 2)
                            throw (new Exception("删除数据时发生未知错误，请检查数据库情况。"));
                        sql = "update tree set tleft-=" + temp * 2 + " where tleft>" + p.Father + " update tree set tright-=" + temp * 2 + " where tright>" + p.Son;
                        comm = new SqlCommand(sql, conn);
                        comm.ExecuteNonQuery();
                        return temp;
                    }
                    catch (Exception exp) { throw exp; }
                    finally { conn.Close(); }
                }
                else return -1;
            }
            #endregion

        }
        */

        /// <summary>
        /// 本类用于操纵植物数据库。继承自DataBaseOperator。
        /// 已密封，无法继承。
        /// </summary>
        public sealed class PlentDataBaseOperator : DataBaseOperator
        {
            /// <summary>
            /// 茎
            /// </summary>
            public enum Stem
            {
                木本,
                藤本,
                草本,
                无茎
            }

            /// <summary>
            /// 叶
            /// </summary>
            public enum Leaf
            {
                单叶,
                复叶
            }

            /// <summary>
            /// 果
            /// </summary>
            public enum Fruit
            {
                核果,
                浆果,
                坚果,
                蒴果,
                瘦果,
                聚合果,
                蓇葖果,
                聚花果,
                球果
            }

            /// <summary>
            /// 花
            /// </summary>
            public enum Flower
            {
                聚伞花序,
                伞形花序,
                轮伞花序,
                圆锥花序,
                柔荑花序,
                穗状花序,
                头状花序,
                伞房花序,
                复伞形花序,
                总状花序
            }

            /// <summary>
            /// 无参构造函数，自动建立与本地服务器植物数据库的连接。
            /// </summary>
            public PlentDataBaseOperator()
            {
            e:
                conn = new SqlConnection("Data Source=(local)" + dataSource[flag] + ";Initial Catalog=plent;Integrated Security=True");
                try
                {
                    conn.Open();
                }
                catch (SqlException exp)
                {
                    flag++;
                    goto e;
                }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 传入dataSource进行连接。
            /// </summary>
            /// <param name="dataSource">数据库名</param>
            public PlentDataBaseOperator(string dataSource)
            {
                conn = new SqlConnection("Data Source=" + dataSource + ";Initial Catalog=plent;Integrated Security=True");
                try
                {
                    conn.Open();
                }
                catch (SqlException exp) { throw exp; }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 获取某一物种分类信息。顺序为纲、纲拉丁名、科、科拉丁名、中文名、学名。
            /// 输入不存在时返回null
            /// </summary>
            /// <param name="name">物种名称。拉丁名或中文名皆可。</param>
            /// <returns>返回字符串数组。顺序为纲、纲拉丁名、科、科拉丁名、中文名、学名。
            /// 输入不存在时返回null</returns>
            public string[] GetClassify(string name)
            {
                string[] classify = new string[6];
                conn.Open();
                try
                {
                    reader = new SqlCommand("select c_cname,class.cname,c_fname,family.fname,c_sname,sname from class,family,species "
                    + "where family.cname=class.cname and family.fname=species.fname and (c_sname='" + name + "' or sname='" + name + "')", conn).ExecuteReader();
                    if (!reader.Read())
                        return null;
                    else
                        for (int i = 0; i < 6; i++)
                            classify[i] = reader.GetValue(i).ToString();
                    return classify;
                }
                catch (Exception exp) { throw exp; }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 获取某一物种特征。顺序为中文名、学名、花序、叶形、叶形态、茎质地、果实、分布。
            /// </summary>
            /// <param name="name">物种名称。拉丁名或中文名皆可。</param>
            /// <returns>返回字符串数组。顺序为中文名、学名、花序、叶形、叶形态、茎质地、果实、分布。</returns>
            public string[] GetInfo(string name)
            {
                string[] classify = new string[8];
                try
                {
                    conn.Open();
                    reader = new SqlCommand("select c_sname,sname,inflorescence,leaf_type,leaf_shape,stem,fruit,geog from features,species " +
                    "where species.snum=features.snum and (c_sname='" + name + "' or sname='" + name + "')", conn).ExecuteReader();
                    if (!reader.Read())
                        return null;
                    else
                        for (int i = 0; i < 8; i++)
                            classify[i] = reader.GetValue(i).ToString();
                    return classify;
                }
                catch (Exception exp) { throw exp; }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 获取某物种在数据库中的序号。
            /// </summary>
            /// <param name="name">物种名称。拉丁名或中文名皆可。</param>
            /// <returns>物种序号</returns>
            public int GetNum(string name)
            {
                conn.Open();
                reader = new SqlCommand("select snum from species where c_sname='" + name + "' or sname='" + name + "'", conn).ExecuteReader();
                try
                {
                    if (reader.Read())
                        return int.Parse(reader.GetValue(0).ToString());
                    else return -1;
                }
                catch (Exception exp) { throw exp; }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 删除某一物种，输入学名或中文名皆可。
            /// </summary>
            /// <param name="name">需要删除的学名或中文名</param>
            /// <returns>删除成功返回true。</returns>
            public bool DeleteSpecies(string name)
            {
                return SqlExecute("exec delete_species '" + name + "'") == 2;
            }

            /// <summary>
            /// 对植物某科进行统计，返回一个DataSet（表索引为0）。
            /// </summary>
            /// <param name="name">需统计的科中文名或学名</param>
            /// <returns>需统计的科中文名或学名</returns>
            public DataSet ClassStatistics(string name)
            {
                return SqlSelect("select species.snum as 编号,c_sname as 中文名,sname as 学名 from features,species,family where family.fname=species.fname and species.snum=features.snum and (c_fname='" + name + "' or family.fname='" + name + "')");
            }

            /// <summary>
            /// 统计某一科内物种数量。
            /// </summary>
            /// <param name="name">需统计的科中文名或学名</param>
            /// <returns></returns>
            public int CountSpeciesInClass(string name)
            {
                return ClassStatistics(name).Tables[0].Rows.Count;
            }

            /// <summary>
            /// 模糊查询。叶型及茎质地为精确。
            /// </summary>
            /// <param name="stem">茎质地</param>
            /// <param name="leaf">叶型</param>
            /// <param name="fruit">果实</param>
            /// <param name="flower">花序</param>
            /// <returns></returns>
            public DataSet FuzzySearch(Stem stem, Leaf leaf, Fruit fruit, Flower flower)
            {
                return SqlSelect("select snum as 编号,c_sname as 中文名,sname as 学名 from species where snum in(select snum from features where (inflorescence like '%" + flower.ToString() + "%' or inflorescence='' or fruit='' or fruit like '%" + fruit.ToString() + "%') and (leaf_type like '%" + leaf.ToString() + "%' or leaf_type='') and stem like '%" + stem.ToString() + "%')");
            }
        }

        /// <summary>
        /// 本类用于操纵昆虫数据库。继承自DataBaseOperator。
        /// 已密封，无法继承。
        /// </summary>
        public sealed class InsectDataBaseOperator : DataBaseOperator
        {
            /// <summary>
            /// 口器
            /// </summary>
            public enum Mouth
            {
                虹吸式,
                咀嚼式,
                刺吸式
            }

            /// <summary>
            /// 触须
            /// </summary>
            public enum Tentacle
            {
                球杆状,
                羽毛状,
                锯齿状,
                丝状,
                鳃叶状,
                肘状,
                鞭状,
                锤状,
                须状,
                圆筒状
            }

            /// <summary>
            /// 足
            /// </summary>
            public enum Foot
            {
                游泳足,
                步行足,
                开掘足,
                跳跃足
            }

            /// <summary>
            /// 翅质地
            /// </summary>
            public enum Wing
            {
                膜质,
                鞘质,
                革质
            }

            /// <summary>
            /// 无参构造函数，自动建立与本地服务器昆虫数据库的连接。
            /// </summary>
            public InsectDataBaseOperator()
            {
            e:
                conn = new SqlConnection("Data Source=(local)" + dataSource[flag] + ";Initial Catalog=insect;Integrated Security=True");
                try
                {
                    conn.Open();
                }
                catch (SqlException exp)
                {
                    flag++;
                    goto e;
                }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 传入dataSource进行连接。
            /// </summary>
            /// <param name="dataSource">数据库名</param>
            public InsectDataBaseOperator(string dataSource)
            {
                conn = new SqlConnection("Data Source=" + dataSource + ";Initial Catalog=insect;Integrated Security=True");
                try
                {
                    conn.Open();
                }
                catch (SqlException exp) { throw exp; }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 获取某一物种分类信息。
            /// </summary>
            /// <param name="name">物种名称。拉丁名或中文名皆可。</param>
            /// <returns>返回字符串数组。顺序为目、目拉丁名、科、科拉丁名、中文名、学名。
            /// 输入不存在时返回null</returns>
            public string[] GetClassify(string name)
            {
                string[] classify = new string[6];
                conn.Open();
                try
                {
                    reader = new SqlCommand("select c_oname,orders.oname,c_fname,family.fname,c_sname,sname from orders,family,species "
                    + "where family.oname=orders.oname and family.fname=species.fname and (c_sname='" + name + "' or sname='" + name + "')", conn).ExecuteReader();
                    if (!reader.Read())
                        return null;
                    else
                        for (int i = 0; i < 6; i++)
                            classify[i] = reader.GetValue(i).ToString();
                    return classify;
                }
                catch (Exception exp) { throw exp; }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 获取某一物种特征。
            /// </summary>
            /// <param name="name">物种名称。拉丁名或中文名皆可。</param>
            /// <returns>返回字符串数组。顺序为中文名、学名、口器、触角、足、翅类型、翅对数。</returns>
            public string[] GetInfo(string name)
            {
                string[] classify = new string[7];
                conn.Open();
                try
                {
                    reader = new SqlCommand("select c_sname,sname,mouthparts,tentacle,foot,wing_texture1,wing_num from features,species " +
                    "where species.snum=features.snum and (c_sname='" + name + "' or sname='" + name + "')", conn).ExecuteReader();
                    if (!reader.Read())
                        return null;
                    else
                        for (int i = 0; i < 7; i++)
                            classify[i] = reader.GetValue(i).ToString();
                    return classify;
                }
                catch (Exception exp) { throw exp; }
                finally { conn.Close(); }

            }

            /// <summary>
            /// 获取某物种在数据库中的序号。
            /// </summary>
            /// <param name="name">物种名称。拉丁名或中文名皆可。</param>
            /// <returns>物种序号</returns>
            public int GetNum(string name)
            {
                conn.Open();
                reader = new SqlCommand("select snum from species where c_sname='" + name + "' or sname='" + name + "'", conn).ExecuteReader();
                try
                {
                    if (reader.Read())
                        return int.Parse(reader.GetValue(0).ToString());
                    else return -1;
                }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 对植物某科进行统计，返回一个DataSet（表索引为0）。
            /// </summary>
            /// <param name="name">需统计的科中文名或学名</param>
            /// <returns>需统计的科中文名或学名</returns>
            public DataSet ClassStatistics(string name)
            {
                return SqlSelect("select species.snum as 序号,c_sname as 中文名,sname as 学名,mouthparts as 口器,tentacle as 触角,foot as 足,wing_texture1 as 翅质地,wing_num as 翅数量 from family,species,features where family.fname=species.fname and species.snum=features.snum and (c_fname='" + name + "' or family.fname='" + name + "')");
            }

            /// <summary>
            /// 统计某一科内物种数量。
            /// </summary>
            /// <param name="name">需统计的科中文名或学名</param>
            /// <returns></returns>
            public int CountSpeciesInClass(string name)
            {
                return ClassStatistics(name).Tables[0].Rows.Count;
            }

            /// <summary>
            /// 昆虫库模糊检索
            /// </summary>
            /// <param name="mouth">口器</param>
            /// <param name="tentacle">触须</param>
            /// <param name="foot">足</param>
            /// <param name="wing">翅质地</param>
            /// <returns></returns>
            public DataSet FuzzySearch(Mouth mouth, Tentacle tentacle, Foot foot, Wing wing)
            {
                return SqlSelect("select snum as 编号,c_sname as 中文名,sname as 学名 from species where snum in(select snum from features where (mouthparts like '%" + mouth.ToString() + "%' and (tentacle='' or tentacle like '%" + tentacle.ToString() + "%') and foot like '%" + foot.ToString() + "%' and wing_texture1 like '%" + wing.ToString() + "%'))");
            }
        }

        /// <summary>
        /// 本类用于操纵用户数据库。继承自DataBaseOperator。
        /// 已密封，无法继承。
        /// </summary>
        public sealed class UsersDatabaseOperator : DataBaseOperator
        {
            /// <summary>
            /// 无参构造函数。自动建立与数据库的连接。
            /// </summary>
            public UsersDatabaseOperator()
            {
            e:
                conn = new SqlConnection("Data Source=(local)" + dataSource[flag] + ";Initial Catalog=users;Integrated Security=True");
                try
                {
                    conn.Open();
                }
                catch (SqlException exp)
                {
                    flag++;
                    goto e;
                }
                finally { conn.Close(); }

            }

            /// <summary>
            /// 传入dataSource进行连接。
            /// </summary>
            /// <param name="dataSource">数据库名</param>
            public UsersDatabaseOperator(string dataSource)
            {
                conn = new SqlConnection("Data Source=" + dataSource + ";Initial Catalog=users;Integrated Security=True");
                try
                {
                    conn.Open();
                }
                catch (SqlException exp) { throw exp; }
                finally { conn.Close(); }
            }

            /// <summary>
            /// 用于存储与传递用户信息。
            /// </summary>
            public struct UserInfo
            {

                /// <summary>
                /// 用户名
                /// </summary>
                public string user_name;
                /// <summary>
                /// 密码。明文传入密文传出。
                /// </summary>
                public string password;
                /// <summary>
                /// 昵称
                /// </summary>
                public string nickname;
                /// <summary>
                /// 性别。"男"、"女"或""
                /// </summary>
                public string sex;
                /// <summary>
                /// 生日。格式为年.月.日
                /// </summary>
                public string birthday;
                /// <summary>
                /// 邮箱
                /// </summary>
                public string email;
                /// <summary>
                /// 密保问题
                /// </summary>
                public string question;
                /// <summary>
                /// 密保回答
                /// </summary>
                public string answer;
            }

            /// <summary>
            /// 用于传递留言信息
            /// </summary>
            public struct MessageInfo
            {
                /// <summary>
                /// 用户昵称
                /// </summary>
                public string nickname;
                /// <summary>
                /// 留言内容
                /// </summary>
                public string message;
                /// <summary>
                /// 留言时间
                /// </summary>
                public string leavingTime;
            }

            /// <summary>
            /// 传入UserInfo进行注册。仅会返回是否注册成功，所以使用前请先检查插入数据的合法性。例如用户名、昵称重复。
            /// </summary>
            /// <param name="info">用户信息。</param>
            /// <returns>返回插入是否成功。</returns>
            public bool Register(UserInfo info)
            {
                DateTime dt = DateTime.Now;
                return SqlExecute("insert into users values('" + info.user_name + "','" + Encrypt(info.password, dt.ToString("u").Replace("Z", ""))
                    + "','" + info.nickname + "','" + info.sex + "','" + info.birthday + "','" + info.email + "','" + info.question + "','"
                    + Encrypt(info.answer, dt.ToString("u").Replace("Z", "")) + "',0,'" + dt.ToString("u").Replace("Z", "") + "',';',';')") == 1;
            }

            /// <summary>
            /// 判断用户名是否已存在。
            /// </summary>
            /// <param name="user_name">输入用户名</param>
            /// <returns>存在该用户名则返回true。</returns>
            public bool User_nameExist(string user_name)
            {
                conn.Open();
                reader = new SqlCommand("select * from users where user_name='" + user_name + "'", conn).ExecuteReader();
                try
                {
                    return reader.Read();
                }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 判断昵称是否已存在。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <returns>存在该昵称则返回true。</returns>
            public bool NicknameExist(string nickname)
            {
                conn.Open();
                reader = new SqlCommand("select * from users where nickname='" + nickname + "'", conn).ExecuteReader();
                try
                {
                    return reader.Read();
                }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 获取用户权限值。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <returns>用户权限值,不存在用户时返回-1</returns>
            public int GetAuthority(string nickname)
            {
                try
                {
                    conn.Open();
                    reader = new SqlCommand("select authority from users where nickname='" + nickname + "'", conn).ExecuteReader();
                    if (reader.Read())
                        return int.Parse(reader.GetValue(0).ToString());
                    else return -1;
                }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 登陆时验证身份。返回内容为空时说明用户名密码不正确。
            /// </summary>
            /// <param name="user_name">用户名</param>
            /// <param name="password">密码</param>
            /// <returns>登录成功返回用户信息，其余返回空UserInfo。</returns>
            public UserInfo Login(string user_name, string password)
            {
                try
                {
                    conn.Open();
                    reader = new SqlCommand("select * from users where user_name='" + user_name + "'", conn).ExecuteReader();
                    if (!reader.Read())
                        return new UserInfo();
                    //string text = reader.GetValue(9).ToString();
                    if (reader.GetValue(1).ToString() != Encrypt(password, Fit(reader.GetValue(9).ToString())))
                        return new UserInfo();
                    UserInfo temp = new UserInfo();
                    temp.user_name = reader.GetValue(0).ToString();
                    temp.nickname = reader.GetValue(2).ToString();
                    temp.sex = reader.GetValue(3).ToString();
                    temp.birthday = reader.GetValue(4).ToString();
                    temp.email = reader.GetValue(5).ToString();
                    temp.question = reader.GetValue(6).ToString();
                    return temp;
                }
                finally
                {
                    reader.Close();
                    conn.Close();
                }
            }

            /// <summary>
            /// 检验密保问题及答案是否正确
            /// </summary>
            /// <param name="name">昵称或用户名</param>
            /// <param name="question">密保问题</param>
            /// <param name="answer">密保答案</param>
            /// <returns>验证通过返回true</returns>
            public bool CheckQA(string name, string question, string answer)
            {
                try
                {
                    conn.Open();
                    reader = new SqlCommand("select * from users where user_name='" + name + "' and question='" + question + "'", conn).ExecuteReader();
                    if (!reader.Read())
                        return false;
                    return reader.GetValue(7).ToString() == Encrypt(answer, Fit(reader.GetValue(9).ToString()));
                }
                finally
                {
                    reader.Close();
                    conn.Close();
                }
            }

            /// <summary>
            /// 获取某用户的密保问题。需输入用户名。
            /// </summary>
            /// <param name="name">用户名</param>
            /// <returns>密保问题</returns>
            public string GetQuestion(string name)
            {
                try
                {
                    conn.Open();
                    reader = new SqlCommand("select * from users where user_name='" + name + "'", conn).ExecuteReader();
                    if (!reader.Read())
                        return null;
                    return reader.GetValue(6).ToString();
                }
                finally
                {
                    reader.Close();
                    conn.Close();
                }

            }

            /// <summary>
            /// 重新设置密码
            /// </summary>
            /// <param name="name">用户名</param>
            /// <param name="new_password">新密码</param>
            /// <returns>修改成功返回true</returns>
            public bool ResetPassword(string name, string new_password)
            {
                string temp = GetCreateTime(name);
                return SqlExecute("update users set password='" + Encrypt(new_password, temp) + "' where user_name='" + name + "'") == 1;
            }

            /// <summary>
            /// 获取用户创建用户时间。
            /// </summary>
            /// <param name="name">用户名</param>
            /// <returns>用户创建时间</returns>
            public string GetCreateTime(string name)
            {
                try
                {
                    conn.Open();
                    reader = new SqlCommand("select * from users where user_name='" + name + "'", conn).ExecuteReader();
                    if (reader.Read())
                        return Fit(reader.GetValue(9).ToString());
                    else return null;
                }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 通过昵称获取用户创建时间。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <returns></returns>
            public string NicknameGetCreateTime(string nickname)
            {
                try
                {
                    conn.Open();
                    reader = new SqlCommand("select * from users where nickname='" + nickname + "'", conn).ExecuteReader();
                    if (reader.Read())
                        return Fit(reader.GetValue(9).ToString());
                    else return null;
                }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 重新设置密保问题与答案
            /// </summary>
            /// <param name="nickname">用户名</param>
            /// <param name="new_question">新问题</param>
            /// <param name="new_answer">新答案</param>
            /// <returns></returns>
            public bool ResetQA(string nickname, string new_question, string new_answer)
            {
                string temp = NicknameGetCreateTime(nickname);
                return SqlExecute("update users set answer='" + Encrypt(new_answer, temp) + "',question='" + new_question + "' where nickname='" + nickname + "'") == 1;

            }

            /// <summary>
            /// 用于添加留言。添加成功返回true。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="message">留言内容</param>
            /// <returns>添加成果返回true</returns>
            public bool AddMessage(string nickname, string message)
            {
                return SqlExecute("insert into _message values('" + nickname + "','" + message + "',getdate())") == 1;
            }

            /// <summary>
            /// 重新设置个人信息
            /// </summary>
            /// <param name="name">用户昵称或者用户名</param>
            /// <param name="new_nickname">新昵称</param>
            /// <param name="new_birthday">新生日</param>
            /// <param name="new_sex">新性别</param>
            /// <param name="new_email">新邮箱</param>
            /// <returns>重设个人信息成功返回true</returns>
            public bool ResetInfo(string name, string new_nickname, string new_birthday,
                string new_sex, string new_email)
            {
                return SqlExecute("update users set nickname='" + new_nickname +
                       "',birthday='" + new_birthday +
                       "',sex='" + new_sex +
                       "',email='" + new_email + "' " +
                       " where nickname='" + name + "'or user_name = '" + name + "'") == 1;
            }

            /// <summary>
            /// 修改用户权限。权限仅能被设为0-9。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="authouity">修改后权限值</param>
            /// <returns>修改成功返回true。</returns>
            public bool ResetAuthority(string nickname, int authouity)
            {
                return (authouity >= 0 && authouity <= 9) && SqlExecute("update users set authority=" + authouity + " where nickname='" + nickname + "'") == 1;
            }

            /// <summary>
            /// 获取数据库中所有留言。
            /// </summary>
            /// <param name="order">排序为true时返回从老到新的留言。</param>
            /// <returns>返回留言信息数组</returns>
            public MessageInfo[] GetAllMessage(bool order = true)
            {
                conn.Open();
                try
                {
                    List<MessageInfo> list = new List<MessageInfo>();
                    var temp = new MessageInfo();
                    reader = new SqlCommand("select * from _message", conn).ExecuteReader();
                    while (reader.Read())
                    {
                        temp.nickname = reader.GetValue(0).ToString();
                        temp.message = reader.GetValue(1).ToString();
                        temp.leavingTime = reader.GetValue(2).ToString();
                        list.Add(temp);
                    }
                    int i = list.Count;
                    if (i == 0) return null;
                    var s = new MessageInfo[i];
                    for (int j = 0; j < i; j++)
                        s[j] = list[j];
                    return s;
                }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 获取某用户的所有留言。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="order">排序为true时返回从老到新的留言。</param>
            /// <returns>该用户留言数组</returns>
            public MessageInfo[] GetUserMessage(string nickname, bool order = true)
            {
                List<MessageInfo> list = new List<MessageInfo>();
                var temp = new MessageInfo();
                temp.nickname = nickname;
                conn.Open();
                try
                {
                    reader = new SqlCommand("select * from _message where nickname='" + nickname + "' order by leaving_time " + (order ? "asc" : "desc"), conn).ExecuteReader();
                    while (reader.Read())
                    {
                        temp.message = reader.GetValue(1).ToString();
                        temp.leavingTime = reader.GetValue(2).ToString();
                        list.Add(temp);
                    }
                    int i = list.Count;
                    var s = new MessageInfo[i];
                    for (int j = 0; j < i; j++)
                        s[j] = list[j];
                    return s;
                }
                finally
                {
                    reader.Close();
                    conn.Close();
                }
            }

            /// <summary>
            /// 获取某一时间点之前或之后的留言。默认为该时间点之后。
            /// </summary>
            /// <param name="time">时间点</param>
            /// <param name="after">true为时间点前，false为时间点后</param>
            /// <param name="order">排序为true时返回从老到新的留言。</param>
            /// <returns>时间点前后留言数组</returns>
            public MessageInfo[] GetTimeMessage(string time, bool after = true, bool order = true)
            {
                List<MessageInfo> list = new List<MessageInfo>();
                var temp = new MessageInfo();
                conn.Open();
                try
                {
                    reader = new SqlCommand("select * from _message where leaving_time" + (after ? ">" : "<") + "'" + time + "' order by leaving_time " + (order ? "asc" : "desc"), conn).ExecuteReader();
                    while (reader.Read())
                    {
                        temp.nickname = reader.GetValue(0).ToString();
                        temp.message = reader.GetValue(1).ToString();
                        temp.leavingTime = reader.GetValue(2).ToString();
                        list.Add(temp);
                    }
                    int i = list.Count;
                    var s = new MessageInfo[i];
                    for (int j = 0; j < i; j++)
                        s[j] = list[j];
                    return s;
                }
                finally
                {
                    reader.Close();
                    conn.Close();
                }
            }

            /// <summary>
            /// 获取某用户某一时间点之前或之后的留言。默认为该时间点之后。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="time">时间点</param>
            /// <param name="after">true为时间点前，false为时间点后</param>
            /// <param name="order">排序为true时返回从老到新的留言。</param>
            /// <returns>该用户时间点前后留言数组</returns>
            public MessageInfo[] GetUserTimeMessage(string nickname, string time, bool after = true, bool order = true)
            {
                List<MessageInfo> list = new List<MessageInfo>();
                var temp = new MessageInfo();
                conn.Open();
                try
                {
                    reader = new SqlCommand("select * from _message where leaving_time" + (after ? ">" : "<") +
                        "'" + time + "' and nickname='" + nickname + "' order by leaving_time " + (order ? "asc" : "desc"), conn).ExecuteReader();
                    while (reader.Read())
                    {
                        temp.nickname = reader.GetValue(0).ToString();
                        temp.message = reader.GetValue(1).ToString();
                        temp.leavingTime = reader.GetValue(2).ToString();
                        list.Add(temp);
                    }
                    int i = list.Count;
                    var s = new MessageInfo[i];
                    for (int j = 0; j < i; j++)
                        s[j] = list[j];
                    return s;
                }
                finally
                {
                    reader.Close();
                    conn.Close();
                }
            }

            /// <summary>
            /// 删除某用户某时间的消息。删除成功返回true
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="time">留言时间</param>
            /// <returns></returns>
            public bool DeleteMessage(string nickname, string time)
            {
                return SqlExecute("delete from _message where leaving_time='" + time + "' and nickname='" + nickname + "'") == 1;
            }

            /// <summary>
            /// 删除某用户某时间的消息。删除成功返回true
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <returns></returns>
            public bool DeleteMessage(string nickname)
            {
                return SqlExecute("delete from _message where nickname='" + nickname + "'") == 1;
            }

            /// <summary>
            /// 向用户收藏夹中添加收藏。添加成功返回true，重复添加、昵称不存在等情况下添加失败返回false。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="num">收藏编号</param>
            /// <param name="table">物种类型('p'或'i')</param>
            /// <returns>添加成功返回true，重复添加、昵称不存在等情况下添加失败返回false。</returns>
            public bool AddFavour(string nickname, int num, char table)
            {
                int[] temp = GetFavour(nickname, table);
                conn.Open();
                try
                {
                    if (temp != null)
                    {
                        int len = temp.Length;
                        for (int i = 0; i < len; i++)
                            if (temp[i] == num)
                                return false;
                    }
                }
                finally { conn.Close(); }
                return SqlExecute("update users set favour" + table + "+='" + num + ";' where nickname='" + nickname + "'") == 1;
            }

            /// <summary>
            /// 获取用户收藏内容，返回整型数组。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="table">物种类型('p'或'i')</param>
            /// <returns>用户收藏物种的序号</returns>
            public int[] GetFavour(string nickname, char table)
            {
                conn.Open();
                reader = new SqlCommand("select favour" + table + " from users where nickname='" + nickname + "'", conn).ExecuteReader();
                if (!reader.Read())
                    return null;
                string temp = reader.GetValue(0).ToString();
                conn.Close();
                reader.Close();
                int len = temp.Length;
                int last = 1;
                List<int> list = new List<int>();
                for (int i = 1; i < len; i++)
                    if (temp[i] == ';')
                    {
                        list.Add(int.Parse(Extract(temp, last, i)));
                        last = i + 1;
                    }
                len = list.Count;
                int[] output = new int[len];
                for (int i = 0; i < len; i++)
                    output[i] = list[i];
                return output;
            }

            /// <summary>
            /// 删除用户收藏夹内内容。删除成功返回true。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="num">需删除的收藏编号</param>
            /// <param name="table">物种类型('p'或'i')</param>
            /// <returns>删除成功返回true。</returns>
            public bool DeleteFavour(string nickname, int num, char table)
            {
                conn.Open();
                reader = new SqlCommand("select favour" + table + " from users where nickname='" + nickname + "'", conn).ExecuteReader();
                if (!reader.Read())
                    return false;
                string temp = reader.GetValue(0).ToString();
                reader.Close();
                try
                {
                    return new SqlCommand("update users set favour" + table + "='" + Delete(temp, num.ToString())
                        + "' where nickname='" + nickname + "'", conn).ExecuteNonQuery() == 1;
                }
                finally
                {
                    conn.Close();
                }
            }

            /// <summary>
            /// 批量删除用户收藏夹内容，删除成功返回true。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="num">需删除的收藏编号数组</param>
            /// <param name="table">物种类型('p'或'i')</param>
            /// <returns>删除成功返回true。</returns> 
            public bool DeleteFavour(string nickname, int[] num, char table)
            {
                conn.Open();
                try
                {
                    reader = new SqlCommand("select favour" + table + " from users where nickname='" + nickname + "'", conn).ExecuteReader();
                    if (!reader.Read())
                        return false;
                    string temp = reader.GetValue(0).ToString();
                    int len = num.Length;
                    for (int i = 0; i < len; i++)
                        temp = Delete(temp, num[i].ToString());
                    return new SqlCommand("update users set favour" + table + "='" + temp + "' where nickname='" + nickname + "'", conn).ExecuteNonQuery() == 1;
                }
                finally
                {
                    conn.Close();
                    reader.Close();
                }
            }

            /// <summary>
            /// 清空用户收藏夹。
            /// </summary>
            /// <param name="nickname">用户昵称</param>
            /// <param name="table">物种类型('p'或'i')</param>
            /// <returns>清除成功返回true。</returns>
            public bool EmptyFavour(string nickname, char table)
            {
                return SqlExecute("update users set favour" + table + "=';' where nickname='" + nickname + "'") == 1;
            }

            /// <summary>
            /// 获取字符串经过SHA256算法加密后得到的字符串。
            /// </summary>
            /// <param name="source">需加密的字符串</param>
            /// <returns>加密后的字符串</returns>
            public static string GetStringSHA256(string source)
            {
                return BitConverter.ToString(
                    new SHA256CryptoServiceProvider().ComputeHash(
                        Encoding.UTF8.GetBytes(source))).Replace("-", "");
            }

            private static string Mix(string s1, string s2)
            {
                string mixed = "";
                for (int i = 0; i < 64; i++)
                    mixed += s1[i] + s2[i];
                return mixed;
            }

            private static string Encrypt(string s1, string s2)
            {
                return GetStringSHA256(Mix(GetStringSHA256(s1), GetStringSHA256(s2)));
            }

            private string Fit(string s)
            {
                const string DAY = "一二三四五六日";
                for (int i = 0; i < 7; i++)
                    s = s.Replace("/周" + DAY[i], "");
                s = s.Replace('/', '-');
                if (s.Length == 18)
                    s = s.Insert(11, "0");
                return s;
            }

            /// <summary>
            /// 截取从head开始，end结束（不包括end）的字符串
            /// </summary>
            /// <param name="init">被截取的字符串</param>
            /// <param name="head">截取起始点</param>
            /// <param name="end">截取终止点（不包括）</param>
            /// <returns></returns>
            public string Extract(string init, int head, int end)
            {
                string temp = "";
                for (int i = head; i < end; i++)
                    temp += init[i];
                return temp;
            }

            private string Delete(string init, string toDelete)
            {
                int m = init.IndexOf(";" + toDelete + ";");
                if (m == -1) return init;
                return Extract(init, 0, m + 1) + Extract(init, m + toDelete.Length + 2, init.Length);
            }

        }
    }
}