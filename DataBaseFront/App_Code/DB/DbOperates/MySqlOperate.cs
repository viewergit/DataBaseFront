﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DataBaseFront.DB.DbParams;
using MySql.Data.MySqlClient;

namespace DataBaseFront.DB.DbOperates
{
    public class MySqlOperate : IDbOperate
    {
        public IDbParam DBParam { get; set; }
        public string DbName { get; set; }
        public string ConnectionString { get; set; }
        public char KeyWordStart { get { return '`'; } }
        public char KeyWordEnd { get { return '`'; } }
        public void FilterKeyWord(ref string sql)
        {
            sql = sql.Replace('【', KeyWordStart);
            sql = sql.Replace('】', KeyWordEnd);
        }

        public bool Exists(string sql)
        {
            object obj = GetSingle(sql);
            if (obj == null || obj == DBNull.Value)
                return false;
            else
                return true;
        }

        public bool IsExistColumn(string table_name, string column_name)
        {
            return GetColumnNames(table_name).Contains(column_name);
        }

        public bool IsExistTable(string table_name)
        {
            object obj = GetSingle(string.Format("SELECT COUNT(1) C FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{0}'", table_name));
            if (obj == null || obj == DBNull.Value)
                return false;
            else
                return Convert.ToInt32(obj) > 0;
        }

        public int GetMaxID(string table_name, string field_name)
        {
            object obj = GetSingle(string.Format("SELECT MAX(【{0}】)+1 FROM 【{1}】", field_name, table_name));
            if (obj == null || obj == DBNull.Value)
                return 1;
            else
                return Convert.ToInt32(obj);
        }

        public bool TryConnect(out string error)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    error = string.Empty;
                    return true;
                }
                catch (MySqlException ex)
                {
                    error = ex.Message;
                    return false;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
        }

        public int ExecuteSql(string sql)
        {
            this.FilterKeyWord(ref sql);

            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(sql, connection))
                {
                    try
                    {
                        cmd.CommandTimeout = 45;
                        connection.Open();
                        int rows = cmd.ExecuteNonQuery();
                        return rows;
                    }
                    catch (MySqlException ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }

        public object GetSingle(string sql)
        {
            this.FilterKeyWord(ref sql);

            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(sql, connection))
                {
                    try
                    {
                        connection.Open();
                        object obj = cmd.ExecuteScalar();
                        return obj;
                    }
                    catch (MySqlException ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }

        public DataTable GetDataTable(string sql)
        {
            this.FilterKeyWord(ref sql);

            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                DataTable dt = new DataTable();
                try
                {
                    connection.Open();
                    MySqlDataAdapter command = new MySqlDataAdapter(sql, connection);
                    command.Fill(dt);
                }
                catch (MySqlException ex)
                {
                    throw ex;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
                return dt;
            }
        }

        public string BuildConnectionString(IDbParam dbParam)
        {
            MySqlParam mysqlParam = dbParam as MySqlParam;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Server={0}", mysqlParam.ServerName);
            sb.AppendFormat(";Port={0}", mysqlParam.Port);
            if (!string.IsNullOrEmpty(mysqlParam.DbName))
                sb.AppendFormat(";DataBase={0}", mysqlParam.DbName);
            sb.AppendFormat(";Persist Security Info={0}", "false");
            sb.AppendFormat(";User ID={0}", mysqlParam.UserID);
            sb.AppendFormat(";Password={0}", mysqlParam.UserPass);
            sb.AppendFormat(";Allow Zero Datetime={0}", "true");
            sb.AppendFormat(";Character Set={0}", "utf8");

            DbName = mysqlParam.DbName;
            ConnectionString = sb.ToString();
            sb = null;
            return ConnectionString;
        }

        public string BuildSelectTableSql(string table_name)
        {
            string result = string.Empty;
            StringBuilder sb = new StringBuilder();
            IList<string> columns = GetColumnNames(table_name);
            if (columns != null && columns.Count > 0)
            {
                foreach (string column in columns)
                {
                    sb.AppendFormat("【{0}】,", column);
                }
                sb = sb.Remove(sb.Length - 1, 1);

                result = string.Format("SELECT {0} FROM 【{1}】;", sb.ToString(), table_name);
            }

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildSelectTableSql(string table_name, int top)
        {
            string result = string.Empty;
            StringBuilder sb = new StringBuilder();
            IList<string> columns = GetColumnNames(table_name);
            if (columns != null && columns.Count > 0)
            {
                foreach (string column in columns)
                {
                    sb.AppendFormat("【{0}】,", column);
                }
                sb = sb.Remove(sb.Length - 1, 1);

                result = string.Format("SELECT {0} FROM 【{1}】 LIMIT {2};", sb.ToString(), table_name, top);
            }

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildCreateTableSql(string table_name)
        {
            string result = string.Empty;
            DataTable dt = GetDataTable(string.Format("SHOW CREATE TABLE {0};", table_name));
            if (dt != null && dt.Rows.Count > 0)
                result = dt.Rows[0]["Create Table"].ToString();
            return result;
        }

        public string BuildCreateViewSql(string view_name)
        {
            string result = string.Empty;
            DataTable dt = GetDataTable(string.Format("SHOW CREATE VIEW {0};", view_name));
            if (dt != null && dt.Rows.Count > 0)
                result = dt.Rows[0]["Create View"].ToString();
            return result;
        }

        public string BuildCreateProcedureSql(string procedure_name)
        {
            string result = string.Empty;
            DataTable dt = GetDataTable(string.Format("SHOW CREATE PROCEDURE {0};", procedure_name));
            if (dt != null && dt.Rows.Count > 0)
                result = dt.Rows[0]["Create Procedure"].ToString();
            return result;
        }

        public string BuildDropTableSql(string table_name)
        {
            string result = string.Empty;
            result = string.Format("DROP TABLE IF EXISTS 【{0}】;", table_name);

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildDropViewSql(string view_name)
        {
            string result = string.Empty;
            result = string.Format("DROP VIEW IF EXISTS 【{0}】;", view_name);

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildDropProcedureSql(string procdure_name)
        {
            string result = string.Empty;
            result = string.Format("DROP PROCEDURE IF EXISTS 【{0}】;", procdure_name);

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildInsertSql(string table_name)
        {
            string result = string.Empty;
            IList<MGColumn> columnNames = GetColumns(table_name);

            if (columnNames != null && columnNames.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("INSERT INTO 【{0}】", table_name);
                sb.Append("(");
                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.AppendFormat("【{0}】", columnNames[i].Name);
                }
                sb.Append(")");
                sb.Append(" VALUES ");
                sb.Append("(");
                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(DBTypeUtil.FormatColumnValue("", columnNames[i].DbType));
                }
                sb.Append(")");
                result = sb.ToString();
            }

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildInsertSqlWidthParam(string table_name)
        {
            string result = string.Empty;
            IList<string> columnNames = GetColumnNames(table_name);

            if (columnNames != null && columnNames.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("INSERT INTO 【{0}】", table_name);
                sb.Append("(");
                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.AppendFormat("【{0}】", columnNames[i]);
                }
                sb.Append(")");
                sb.Append(" VALUES ");
                sb.Append("(");
                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.AppendFormat("@{0}", columnNames[i]);
                }
                sb.Append(")");
                result = sb.ToString();
            }

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildDeleteSql(string table_name)
        {
            string result = string.Empty;
            IList<MGColumn> columnNames = GetColumns(table_name);

            if (columnNames != null && columnNames.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("DELETE FROM 【{0}】 WHERE 【{1}】 = {2}", table_name, columnNames[0].Name, DBTypeUtil.FormatColumnValue("", columnNames[0].DbType));

                result = sb.ToString();
            }

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildDeleteSqlWidthParam(string table_name)
        {
            string result = string.Empty;
            IList<string> columnNames = GetColumnNames(table_name);

            if (columnNames != null && columnNames.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("DELETE FROM 【{0}】 WHERE 【{1}】 = @{1}", table_name, columnNames[0]);

                result = sb.ToString();
            }

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildUpdateSql(string table_name)
        {
            string result = string.Empty;
            IList<MGColumn> columnNames = GetColumns(table_name);

            if (columnNames != null && columnNames.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("UPDATE 【{0}】", table_name);
                sb.Append(" SET ");
                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.AppendFormat("【{0}】 = {1}", columnNames[i].Name, DBTypeUtil.FormatColumnValue("", columnNames[i].DbType));
                }
                sb.AppendFormat(" WHERE 【{0}】 = {1}", columnNames[0].Name, DBTypeUtil.FormatColumnValue("", columnNames[0].DbType));

                result = sb.ToString();
            }

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildUpdateSqlWidthParam(string table_name)
        {
            string result = string.Empty;
            IList<string> columnNames = GetColumnNames(table_name);

            if (columnNames != null && columnNames.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("UPDATE 【{0}】", table_name);
                sb.Append(" SET ");
                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.AppendFormat("【{0}】 = @{0}", columnNames[i]);
                }
                sb.AppendFormat(" WHERE 【{0}】 = @{0}", columnNames[0]);

                result = sb.ToString();
            }

            this.FilterKeyWord(ref result);
            return result;
        }

        public string BuildInsertColumnSql(string table_name, string column_name, string column_dbType, int length, int numericScale, bool IsPrimaryKey)
        {
            return string.Format("{0}:{1},{2},{3},{4},{5},{6}", "BuildInsertColumnSql", table_name, column_name, column_dbType, length, numericScale, IsPrimaryKey);
        }

        public string BuildUpdateColumnNameSql(string table_name, string column_name, string column_new_name)
        {
            return string.Format("{0}:{1},{2},{3}", "BuildInsertColumnSql", table_name, column_name, column_new_name);
        }

        public string BuildUpdateColumnDbTypeSql(string table_name, string column_name, string column_dbType, int length, int numericScale)
        {
            return string.Format("{0}:{1},{2},{3},{4},{5}", "BuildUpdateColumnDbTypeSql", table_name, column_name, column_dbType, length, numericScale);
        }

        public string BuildUpdateColumnPrimaryKeySql(string table_name, string column_name, bool IsPrimaryKey)
        {
            return string.Format("{0}:{1},{2},{3}", "BuildUpdateColumnPrimaryKeySql", table_name, column_name, IsPrimaryKey);
        }

        public string BuildDeleteColumnSql(string table_name, string column_name)
        {
            return string.Format("{0}:{1},{2}", "BuildDeleteColumnSql", table_name, column_name);
        }

        public IList<MGDataBase> GetDataBaseInfo()
        {
            IList<MGDataBase> list = new List<MGDataBase>();
            DataTable dt = GetDataTable("SHOW DATABASES;");
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MGDataBase()
                    {
                        Name = dr["Database"].ToString()
                    });
                }
            }
            return list.OrderBy(o => o.Name).ToList();
        }

        public IList<MGTable> GetTables()
        {
            IList<MGTable> list = new List<MGTable>();
            DataTable dt = GetDataTable(string.Format("SHOW FULL TABLES FROM {0} WHERE 【Table_type】 = 'BASE TABLE';", DbName));
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MGTable()
                    {
                        Name = dr[0].ToString()
                    });
                }
            }
            return list.OrderBy(o => o.Name).ToList();
        }

        public IList<string> GetColumnNames(string table_name)
        {
            IList<string> list = new List<string>();
            DataTable dt = GetDataTable(string.Format("SHOW COLUMNS FROM 【{0}】 FROM 【{1}】;", table_name, DbName));

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(dr["Field"].ToString());
                }
            }
            return list;
        }

        public IList<MGColumn> GetColumns(string table_name)
        {
            DataTable dt = GetDataTable(string.Format("SHOW FULL COLUMNS FROM 【{0}】 FROM 【{1}】;", table_name, DbName));
            if (dt != null && dt.Rows.Count > 0)
            {
                string tmpType;
                string _type;
                string _typeLength;
                IList<MGColumn> list = new List<MGColumn>();
                MGColumn columnInfo;
                int index = 1;
                foreach (DataRow dr in dt.Rows)
                {
                    tmpType = dr["Type"].ToString().Split(' ')[0];
                    if (tmpType.Contains('('))
                    {
                        _type = tmpType.Split('(')[0];
                        _typeLength = tmpType.Split('(')[1].TrimEnd(')');
                    }
                    else
                    {
                        _type = tmpType;
                        _typeLength = "0";
                    }

                    columnInfo = new MGColumn();

                    columnInfo.Index = index;
                    columnInfo.Name = Convert.ToString(dr["Field"]);
                    columnInfo.Remark = Convert.ToString(dr["Comment"]);
                    columnInfo.AutoIncrement = Convert.ToBoolean(dr["Extra"].ToString() == "auto_increment");
                    columnInfo.IsPrimaryKey = Convert.ToBoolean(dr["Key"].ToString() == "PRI");
                    columnInfo.DbType = _type.ToLower();
                    if (_typeLength.Contains(','))
                    {
                        columnInfo.Length = Convert.ToInt32(_typeLength.Split(',')[0]);
                        columnInfo.NumericPrecision = columnInfo.Length;
                        columnInfo.NumericScale = Convert.ToInt32(_typeLength.Split(',')[1]);
                    }
                    else
                    {
                        columnInfo.Length = Convert.ToInt32(_typeLength);
                        columnInfo.NumericPrecision = columnInfo.Length;
                        columnInfo.NumericScale = 0;
                    }
                    columnInfo.AllowNull = Convert.ToBoolean(dr["Null"].ToString() == "YES");
                    columnInfo.DefaultValue = Convert.ToString(dr["Default"]);

                    list.Add(columnInfo);

                    index++;
                }
                return list;
            }
            else
            {
                return null;
            }
        }

        public IList<MGProcedure> GetProcedures()
        {
            IList<MGProcedure> list = new List<MGProcedure>();
            DataTable dt = GetDataTable(string.Format("SELECT 【name】 FROM mysql.proc where 【db】 = '{0}' and 【type】= 'PROCEDURE';", DbName));
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MGProcedure()
                    {
                        Name = dr["name"].ToString()
                    });
                }
            }
            return list.OrderBy(o => o.Name).ToList();
        }

        public IList<MGView> GetViews()
        {
            IList<MGView> list = new List<MGView>();
            DataTable dt = GetDataTable("SELECT 【TABLE_NAME】 FROM information_schema.VIEWS;");

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MGView()
                    {
                        Name = dr["TABLE_NAME"].ToString()
                    });
                }
            }
            return list.OrderBy(o => o.Name).ToList();
        }

        public IList<MGTableInfo> GetTableInfos()
        {
            return GetTableInfos(GetTables());
        }

        public IList<MGTableInfo> GetTableInfos(IList<MGTable> tables)
        {
            if (tables == null) return null;

            var source_tables = tables;
            var target_tables = new List<MGTableInfo>();
            foreach (var table in source_tables)
            {
                var columns = GetColumns(table.Name);
                target_tables.Add(new MGTableInfo() { Table = table, Columns = columns });
            }
            return target_tables;
        }

        public IList<Entity.TypeEntity> GetDBTypes()
        {
            return DBTypeUtil.GetTypeEntity("MySqlDbType");
        }
    }
}
