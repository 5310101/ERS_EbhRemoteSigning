using ERS_Domain;
using ERS_Domain.CAService;
using ERS_Domain.clsUtilities;
using ERS_Domain.Dtos;
using ERS_Domain.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace IntrustCA_Winservice.Services
{
    public class CoreService
    {
        private readonly DbService _dbService;

        public CoreService()
        {
            _dbService = new DbService();
        }
        
        public bool UpdateHS(UpdateHoSoDto hsUpdate)
        {
            try
            {
                List<SqlParameter> sqlparams = new List<SqlParameter>();
                if (hsUpdate.ListId.Any() == false)
                {
                    throw new Exception("ListId cannot be empty");
                }
                string[] paramNames = hsUpdate.ListId.Select(id => $"@p{id}").ToArray();
                string inClause = string.Join(",", paramNames);
                foreach (string pname in paramNames)
                {
                    SqlParameter param = new SqlParameter(pname, pname.Replace("@p", ""));
                    sqlparams.Add(param);
                }

                string tSql = $"UPDATE HoSo_RS SET TrangThai=@TrangThai, LastGet=@LastGet, ErrMsg=@ErrMsg, FilePath=@FilePath WHERE Guid IN ({inClause})";
                sqlparams.Add(new SqlParameter("@TrangThai", (int)hsUpdate.TrangThai));
                sqlparams.Add(new SqlParameter("@LastGet", hsUpdate.LastGet));
                sqlparams.Add(new SqlParameter("@ErrMsg", hsUpdate.ErrMsg));
                sqlparams.Add(new SqlParameter("@FilePath", hsUpdate.FilePath));
                return _dbService.ExecQuery_Tran(tSql, "", sqlparams.ToArray());
            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "UpdateHS");
                return false;
            }
        }

        public DataTable GetHS(RemoteSigningProvider provider, TrangThaiHoso trangThai, int numberOfHS)
        {
            SqlParameter[] sqlParams = new SqlParameter[]
            {
                new SqlParameter("@CAProvider",SqlDbType.Int){Value = (int)provider},
                new SqlParameter("@TrangThai", SqlDbType.Int){Value = (int)trangThai}
            };
            return _dbService.GetDataTable($"SELECT TOP {numberOfHS} uid,Guid,SerialNumber,typeDK FROM HoSo_RS WITH (UPDLOCK, READPAST, ROWLOCK) WHERE CAProvider = @CAProvider AND TrangThai = @TrangThai ORDER BY NgayGui", "" , sqlParams);
        }

        public DataTable GetToKhai(string GuidHS)
        {
            string tsql = "SELECT * FROM ToKhai_RS WITH (UPDLOCK, READPAST, ROWLOCK) WHERE GuidHS=@GuidHS AND TrangThai = 6 AND typeDK<>1";
            return _dbService.GetDataTable(tsql, "", new SqlParameter[]
            {
                new SqlParameter("@GuidHS", GuidHS)
            });
        }

        public bool UpdateToKhai(UpdateToKhaiDto tkUpdate)
        {
            string tsql = "UPDATE ToKhai_RS SET TrangThai=@TrangThai, ErrMsg=@ErrMsg, LastGet=@LastGet, FilePath=@FilePath WHERE id=@Id";
            SqlParameter[] sqlParams = new SqlParameter[]
            {
                new SqlParameter("@Id", SqlDbType.Int){Value =  tkUpdate.Id},
                new SqlParameter("@TrangThai", SqlDbType.Int){Value = (int)tkUpdate.TrangThai},
                new SqlParameter("@ErrMsg", tkUpdate.ErrMsg),
                new SqlParameter("@LastGet", tkUpdate.LastGet),
                new SqlParameter("@FilePath", tkUpdate.FilePath)
            };
            return _dbService.ExecQuery_Tran(tsql,"", sqlParams);
        }
    }
}
