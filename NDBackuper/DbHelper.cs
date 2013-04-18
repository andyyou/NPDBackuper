using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace NDBackuper
{
    public static class DbHelper
    {
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
                        sbc.BatchSize       = 10000; // 批次寫入的數量
                        sbc.BulkCopyTimeout = 60;    // 逾時時間
                        //處理完後丟出一個事件,或是說處理幾筆後就丟出事件 
                        sbc.NotifyAfter = dt.Rows.Count;
                        sbc.SqlRowsCopied += new SqlRowsCopiedEventHandler(mySbc_SqlRowsCopied);
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
