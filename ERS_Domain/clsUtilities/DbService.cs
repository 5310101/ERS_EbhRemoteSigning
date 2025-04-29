
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ERS_Domain.clsUtilities
{

    public class DbService
    {
		private string _connStr;

		public string ConnStr
		{
			get 
			{
				if (string.IsNullOrEmpty(_connStr))
				{
					_connStr = ConfigurationManager.ConnectionStrings["CONNECTION_STRING"].ConnectionString;
				}
				return _connStr; 
			}
		}

        private SqlConnection GetConnection(string connStr)
        {
            SqlConnection conn = new SqlConnection(connStr);
            if (conn.State != ConnectionState.Open)
            {
                 conn.Open();
            }
            return conn;
        }

        public DataTable GetDataTable(string TSQL, string connectionString = "", SqlParameter[] sqlParams = null)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = ConnStr;
                }
                using (SqlConnection conn = GetConnection(connectionString))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(TSQL, conn))
                    {
                        if (sqlParams != null && sqlParams?.Length > 0)
                        {
                            adapter.SelectCommand.Parameters.AddRange(sqlParams);
                        }
                        using (DataTable dt = new DataTable())
                        {
                            adapter.SelectCommand.CommandType = CommandType.Text;
                            adapter.SelectCommand.CommandTimeout = 600;
                            adapter.Fill(dt);
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "GetDataTable");
                return null;
            }
        }
        public bool ExecQuery(string TSQL, string connectionString = "", SqlParameter[] sqlParams = null)
        {
            bool isExecuted = false;
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = ConnStr;
                }
                using (SqlConnection conn = GetConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(TSQL, conn))
                    {
                        if (sqlParams != null && sqlParams?.Length > 0)
                        {
                            command.Parameters.AddRange(sqlParams);
                        }
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = 600;
                        int rowEffected = command.ExecuteNonQuery();
                        if (rowEffected > 0)
                        {
                            isExecuted = true;
                        }
                    }
                }
                return isExecuted;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "ExecQuery");
                return false;
            }
        }
        public bool ExecQuery_Tran(string TSQL, string connectionString = "", SqlParameter[] sqlParams = null)
        {
            bool isExecuted = false;
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = ConnStr;
                }
                using (SqlConnection conn = GetConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(TSQL, conn))
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        if (sqlParams != null && sqlParams?.Length > 0)
                        {
                            command.Parameters.AddRange(sqlParams);
                        }
                        command.Transaction = trans;
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = 600;
                        try
                        {
                            int rowEffected = command.ExecuteNonQuery();
                            if (rowEffected > 0)
                            {
                                trans.Commit();
                                isExecuted = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Utilities.logger.ErrorLog(ex, "Rollback Transaction");
                            trans.Rollback();
                        }
                    }
                }
                return isExecuted;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "ExecQuery");
                return false;
            }
        }

        private async Task<SqlConnection> GetConnectionAsync(string connStr)
		{
			SqlConnection conn = new SqlConnection(connStr);
			if(conn.State != ConnectionState.Open)
			{
				await conn.OpenAsync();
			}
			return conn;
		}
        public async Task<DataTable> GetDataTableAsync(string TSQL, string connectionString = "", SqlParameter[] sqlParams = null)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = ConnStr;
                }
                using (SqlConnection conn = await GetConnectionAsync(connectionString))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(TSQL, conn))
                    {
                        if (sqlParams != null && sqlParams?.Length > 0)
                        {
                            adapter.SelectCommand.Parameters.AddRange(sqlParams);
                        }
                        using (DataTable dt = new DataTable())
                        {
                            adapter.SelectCommand.CommandType = CommandType.Text;
                            adapter.SelectCommand.CommandTimeout = 600;
                            await Task.Run(() => adapter.Fill(dt));
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "GetDataTable");
                return null;
            }
        }
        public async Task<bool> ExecQueryAsync(string TSQL, string connectionString = "", SqlParameter[] sqlParams = null)
        {
            bool isExecuted = false;
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = ConnStr;
                }
                using (SqlConnection conn = await GetConnectionAsync(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(TSQL, conn))
                    {
                        if (sqlParams != null && sqlParams?.Length > 0)
                        {
                            command.Parameters.AddRange(sqlParams);
                        }
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = 600;
                        int rowEffected = await command.ExecuteNonQueryAsync();
                        if (rowEffected > 0)
                        {
                            isExecuted = true;
                        }
                    }
                }
                return isExecuted;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "ExecQuery");
                return false;
            }
        }
        public async Task<bool> ExecQuery_TranAsync(string TSQL, string connectionString = "", SqlParameter[] sqlParams = null)
        {
            bool isExecuted = false;
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = ConnStr;
                }
                using (SqlConnection conn = await GetConnectionAsync(connectionString))
                {
                    using (SqlTransaction trans = conn.BeginTransaction())
                    using (SqlCommand command = new SqlCommand(TSQL, conn))
                    {
                        if (sqlParams != null && sqlParams?.Length > 0)
                        {
                            command.Parameters.AddRange(sqlParams);
                        }
                        command.Transaction = trans;
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = 600;
                        try
                        {
                            int rowEffected = await command.ExecuteNonQueryAsync();
                            if (rowEffected > 0)
                            {
                                trans.Commit();
                                isExecuted = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Utilities.logger.ErrorLog(ex, "Rollback Transaction");
                            trans.Rollback();
                        }
                    }
                }
                return isExecuted;
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "ExecQuery");
                return false;
            }
        }
    }
}