using System;
using System.Data;
using System.Data.SqlClient;

namespace Common.Class
{
    public class AppDao
    {
        public DataTable GetTable(string TSQL, System.Data.SqlClient.SqlParameter[] myParamArr = null, SqlConnection myConn = null)
        {
            try
            {
                if (myConn == null)
                {
                    using (SqlConnection conn = new SqlConnection(Utilities.Setting.DBConnStr))
                    {
                        return GetTable(TSQL, myParamArr, conn);
                    }
                }

                if (myConn.State != ConnectionState.Open) { myConn.Open(); }

                using (SqlDataAdapter da = new SqlDataAdapter(TSQL, myConn))
                {
                    using (DataTable dt = new DataTable("DataTable"))
                    {
                        if (myParamArr != null && myParamArr.Length > 0)
                        {
                            da.SelectCommand.Parameters.AddRange(myParamArr);
                        }
                        da.SelectCommand.CommandType = CommandType.Text;
                        da.SelectCommand.CommandTimeout = 600;
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log.GhiLog("GetTable", ex, TSQL, myParamArr);
                throw ex;
            }
            finally
            {
                if (myConn != null) { myConn.Close(); }
            }
        }
        public DataTable GetTableForConfig(string TSQL, System.Data.SqlClient.SqlParameter[] myParamArr = null, SqlConnection myConn = null)
        {
            try
            {
                if (myConn == null)
                {
                    using (SqlConnection conn = new SqlConnection(Utilities.Setting.DBConnStrConFig))
                    {
                        return GetTable(TSQL, myParamArr, conn);
                    }
                }

                if (myConn.State != ConnectionState.Open) { myConn.Open(); }

                using (SqlDataAdapter da = new SqlDataAdapter(TSQL, myConn))
                {
                    using (DataTable dt = new DataTable("DataTable"))
                    {
                        if (myParamArr != null && myParamArr.Length > 0)
                        {
                            da.SelectCommand.Parameters.AddRange(myParamArr);
                        }
                        da.SelectCommand.CommandType = CommandType.Text;
                        da.SelectCommand.CommandTimeout = 600;
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log.GhiLog("GetTable", ex, TSQL, myParamArr);
                throw ex;
            }
            finally
            {
                if (myConn != null) { myConn.Close(); }
            }
        }

        public int ExecSQLNonTrans(string TSQL, SqlParameter[] myParamArr = null, SqlConnection myConn = null)
        {
            try
            {
                if (myConn == null)
                {
                    using (SqlConnection conn = new SqlConnection(Utilities.Setting.DBConnStr))
                    {
                        return ExecSQL(TSQL, myParamArr, conn);
                    }
                }

                if (myConn.State != ConnectionState.Open) { myConn.Open(); }

                using (SqlCommand cmd = new SqlCommand(TSQL, myConn))
                {
                    if (myParamArr != null && myParamArr.Length > 0)
                    {
                        cmd.Parameters.AddRange(myParamArr);
                    }
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 600;
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Utilities.Log.GhiLog("ExecSQL", ex, TSQL, myParamArr);
                throw ex;
            }
            finally
            {
                if (myConn != null) { myConn.Close(); }
            }
        }

        public int ExecSQL(string TSQL, SqlParameter[] myParamArr = null, SqlConnection myConn = null, SqlTransaction myTrans = null)
        {
            try
            {
                if (myConn == null)
                {
                    using (SqlConnection conn = new SqlConnection(Utilities.Setting.DBConnStr))
                    {
                        return ExecSQL(TSQL, myParamArr, conn, myTrans);
                    }
                }

                if (myConn.State != ConnectionState.Open) { myConn.Open(); }

                using (SqlCommand cmd = new SqlCommand(TSQL, myConn))
                {
                    if (myParamArr != null && myParamArr.Length > 0)
                    {
                        cmd.Parameters.AddRange(myParamArr);
                    }
                    cmd.Transaction = myTrans;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 600;
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Utilities.Log.GhiLog("ExecSQL", ex, TSQL, myParamArr);
                throw ex;
            }
            finally
            {
                if (myConn != null)
                {
                    if (myTrans == null)
                    {
                        myConn.Close();

                    }
                }
            }
        }




        public DataTable GetTable(string TSQL, string DBConnStr, SqlParameter[] myParamArr = null)
        {
            try
            {
                using (SqlConnection myConn = new SqlConnection(DBConnStr))
                {
                    if (myConn.State != ConnectionState.Open) { myConn.Open(); }

                    using (SqlDataAdapter da = new SqlDataAdapter(TSQL, myConn))
                    {
                        using (DataTable dt = new DataTable("DataTable"))
                        {
                            if (myParamArr != null && myParamArr.Length > 0)
                            {
                                da.SelectCommand.Parameters.AddRange(myParamArr);
                            }
                            da.SelectCommand.CommandType = CommandType.Text;
                            da.SelectCommand.CommandTimeout = 600;
                            da.Fill(dt);
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log.GhiLog("GetTable", ex, TSQL, myParamArr);
                throw ex;
            }
        }

        public int ExecSQLNonTrans(string TSQL, string DBConnStr, SqlParameter[] myParamArr = null)
        {
            try
            {
                using (SqlConnection myConn = new SqlConnection(DBConnStr))
                {
                    if (myConn.State != ConnectionState.Open) { myConn.Open(); }

                    using (SqlCommand cmd = new SqlCommand(TSQL, myConn))
                    {
                        if (myParamArr != null && myParamArr.Length > 0)
                        {
                            cmd.Parameters.AddRange(myParamArr);
                        }
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandTimeout = 600;
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log.GhiLog("ExecSQL", ex, TSQL, myParamArr);
                throw ex;
            }
        }

        public int ExecSQLGetId(string TSQL, SqlParameter[] myParamArr = null, SqlConnection myConn = null, SqlTransaction myTrans = null)
        {
            try
            {
                if (myConn == null)
                {
                    using (SqlConnection conn = new SqlConnection(Utilities.Setting.DBConnStr))
                    {
                        return ExecSQLGetId(TSQL, myParamArr, conn, myTrans);
                    }
                }

                if (myConn.State != ConnectionState.Open) { myConn.Open(); }

                TSQL = TSQL + " SELECT SCOPE_IDENTITY()";
                using (SqlCommand cmd = new SqlCommand(TSQL, myConn))
                {
                    if (myParamArr != null && myParamArr.Length > 0)
                    {
                        cmd.Parameters.AddRange(myParamArr);
                    }
                    cmd.Transaction = myTrans;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 600;
                    return int.Parse(cmd.ExecuteScalar().ToString());
                }
            }
            catch (Exception ex)
            {
                Utilities.Log.GhiLog("ExecSQLGetId", ex, TSQL, myParamArr);
                throw ex;
            }
            finally
            {
                if (myConn != null)
                {
                    if (myTrans == null)
                    {
                        myConn.Close();
                    }
                }
            }
        }
    }
}
