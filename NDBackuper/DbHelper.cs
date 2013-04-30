using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace NDBackuper
{
    public static class DbHelper
    {
        public const string SQL_TABLELIST = "SELECT name FROM sys.tables WHERE is_ms_shipped = 0";
        public const string SQL_LIST_CONSTRAINTS = @"SELECT
                                                     K_Table = FK.TABLE_NAME,
                                                     FK_Column = CU.COLUMN_NAME,
                                                     PK_Table = PK.TABLE_NAME,
                                                     PK_Column = PT.COLUMN_NAME,
                                                     Constraint_Name = C.CONSTRAINT_NAME
                                                     FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C
                                                     INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK 
                                                           ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
                                                     INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK 
                                                           ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME
                                                     INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU 
                                                           ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME
                                                     INNER JOIN (
                                                           SELECT i1.TABLE_NAME, i2.COLUMN_NAME
                                                           FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1
                                                     INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 
                                                           ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME
                                                     WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY'
                                                     ) PT ON PT.TABLE_NAME = PK.TABLE_NAME";
      
        public static List<string> SqlBulkLog = new List<string>();
      
        
        public static object ReadOne(string conn, string sql, params SqlParameter[] parms)
        {
            using (SqlConnection connection = new SqlConnection(conn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, connection, null, sql, parms);
                        object obj = cmd.ExecuteScalar();
                        cmd.Parameters.Clear();
                        if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
                        {
                            return null;
                        }
                        else
                        {
                            return obj;
                        }
                        }
                    catch (System.Data.SqlClient.SqlException e)
                    {
                        throw e;
                    }
                }
            }
        }
        public static string PrimaryKeyColumn(string conn, string table)
        {
            using (SqlConnection connection = new SqlConnection(conn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        if (connection.State != ConnectionState.Open)
                            connection.Open();
                        cmd.Connection = connection;
                        string format = @"select c.name 
                                          from sys.index_columns ic 
                                            join sys.indexes i on ic.index_id=i.index_id
                                            join sys.columns c on c.column_id=ic.column_id
                                          where 
                                            i.[object_id] = object_id('{0}') and 
                                            ic.[object_id] = object_id('{0}') and 
                                            c.[object_id] = object_id('{0}') and
                                            is_primary_key = 1";
                        string sql = string.Format(format, table);

                        cmd.CommandText = sql;
                        cmd.CommandType = CommandType.Text;
                        
                        SqlDataReader reader = cmd.ExecuteReader();
                        reader.Read();
                        cmd.Parameters.Clear();
                        return reader[0].ToString();
                        
                    }
                    catch (System.Data.SqlClient.SqlException e)
                    {
                        throw e;
                    }
                }
            }
        }
        public static int ExecuteSql(string cn, string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(cn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, conn, null, sql, parameters);
                        int rows = cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                        return rows;
                    }
                    catch (System.Data.SqlClient.SqlException e)
                    {
                        throw e;
                    }
                }
            }
        }
        public static bool ExecuteScript(string cn, string scriptPath)
        {
            try
            {
                System.IO.FileInfo file = new System.IO.FileInfo(scriptPath);
                string script = file.OpenText().ReadToEnd();
                file.OpenText().Close();

                using (SqlConnection conn = new SqlConnection(cn))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        Server server = new Server(new ServerConnection(conn));
                        server.ConnectionContext.ExecuteNonQuery(script);
                    }
                }
                
                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public static void ExecuteSqlBulk(string conn, DataTable dt)
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Suppress))
            {
                // 大量寫入
                // Solve Somethiing
                // http://msdn.microsoft.com/zh-tw/library/aa561924%28v=bts.10%29.aspx
                // http://www.died.tw/2009/04/msdtc.html
                // 
                using (SqlConnection connection = new SqlConnection(conn))
                {
                    using (SqlBulkCopy sbc = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity))
                    {
                        //設定
                        sbc.BatchSize       = 2000; // 批次寫入的數量
                        sbc.BulkCopyTimeout = 600;    // 逾時時間
                        //設定 NotifyAfter 屬性，以便在每複製 dt.Rows.Count 個資料列至資料表後，呼叫事件處理常式。

                        if (dt.Rows.Count < 1)
                        {
                            SqlBulkLog.Add(dt.TableName + " has no records.");
                        }
                        else
                        {
                            sbc.NotifyAfter = dt.Rows.Count;
                            sbc.SqlRowsCopied += new SqlRowsCopiedEventHandler(OnSqlBulkCopied);
                        }
                        // 更新哪個資料表
                        sbc.DestinationTableName = dt.TableName;
                        foreach (var item in dt.Columns.Cast<DataColumn>())
                        {
                            sbc.ColumnMappings.Add(item.ColumnName, item.ColumnName);
                        }
                        //開始寫入
                        sbc.WriteToServer(dt);
                        //完成交易
                        scope.Complete();
                    }
                }
            }
        }
        public static void OnSqlBulkCopied(object sender, SqlRowsCopiedEventArgs e)
        {
            string table = ((SqlBulkCopy)sender).DestinationTableName;
            string msg = string.Format("Copied {0} records from {1}", e.RowsCopied, table);
            SqlBulkLog.Add(msg);
        }
        public static DataSet CopySechmaFromDatabase(string conn)
        {
            #region Get Tables of conn database
            List<string> tables = new List<string>();
            using (SqlConnection connection = new SqlConnection(conn))
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = SQL_TABLELIST;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    tables.Add(dr[0].ToString());
                }
            }
            #endregion

            #region Copy Table schema into DataSet(ds)
            DataSet ds = new DataSet();
            ds.EnforceConstraints = true;
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(conn);
            ds.DataSetName = builder["Database"].ToString();
            
            foreach (var table in tables)
            {
                string sql = string.Format("Select * From {0}", table);
                using (SqlDataAdapter adapter = new SqlDataAdapter(sql,conn))
                {
                    adapter.FillSchema(ds, SchemaType.Source, table);
                }
            }
            #endregion

            using (SqlConnection connection = new SqlConnection(conn))
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = SQL_LIST_CONSTRAINTS;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    string fkTable        = dr[0].ToString();
                    string fkColumn       = dr[1].ToString();
                    string pkTable        = dr[2].ToString();
                    string pkColumn       = dr[3].ToString();
                    string constraintName = dr[4].ToString();
                    ForeignKeyConstraint fk = new ForeignKeyConstraint(
                                                        constraintName,
                                                        ds.Tables[pkTable].Columns[pkColumn],
                                                        ds.Tables[fkTable].Columns[fkColumn]);
                    fk.DeleteRule = System.Data.Rule.Cascade;
                    fk.UpdateRule = System.Data.Rule.Cascade;
                    ds.Tables[fkTable].Constraints.Add(fk);
                }
            }
            return ds;
        }
        public static List<string> Fill(string conn, DataSet ds, List<string> tables)
        {
            List<string> order = new List<string>();
            foreach (var tbl in tables)
            {
               DbHelper.Recursive(ds, tbl, ref order);
            }

            foreach (var tbl in order)
            {
                string sql = string.Format("Select * From {0}", tbl);
                using (SqlDataAdapter adapter = new SqlDataAdapter(sql, conn))
                {
                    adapter.Fill(ds.Tables[tbl]);
                }
            }
            return order;
        }
        public static void Recursive(DataSet ds, string tablename, ref List<string> created)
        {
            List<ForeignKeyConstraint> fks = new List<ForeignKeyConstraint>();
            foreach(var constraint in ds.Tables[tablename].Constraints)
            {
                ForeignKeyConstraint fk = constraint as ForeignKeyConstraint;
                if (fk == null) continue;

                fks.Add(fk);
            }
            foreach (ForeignKeyConstraint f in fks)
            {
                if(!created.Contains(tablename))
                    Recursive(ds, f.RelatedTable.ToString(),ref  created);
            }
            if (!created.Contains(tablename))
                created.Add(tablename);
            
        }
        public static bool IsTableExists(string conn, string table)
        {
            using (SqlConnection connection = new SqlConnection(conn))
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = SQL_TABLELIST;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    if (dr[0].ToString() == table)
                        return true;
                }
                return false;
            }
        }

        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, string sql, SqlParameter[] parms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = sql;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = CommandType.Text;
            if (parms != null)
            {
                foreach (SqlParameter parameter in parms)
                {
                    if ((parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Input) &&
                        (parameter.Value == null))
                    {
                        parameter.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(parameter);
                }
            }
        }  


    }
}
