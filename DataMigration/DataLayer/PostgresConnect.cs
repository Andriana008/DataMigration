using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using DataMigration.PostgresDB;
using DataMigration.Logger;


namespace DataMigration.DataLayer
{
    public class PostgresConnect
    {
        private Logger.Logger _log = new Logger.Logger();

        public void StartLogConsole(Logger.Logger logger)
        {
            _log = new Logger.Logger(logger);
        }
        public List<string> GetAllHistoricPaths(string connectString,int limit)
        {
            var docs=new List<string>();
            try
            {
                _log.WriteLog(LogLevel.Info, $"Get {limit} historic paths (Postgres)\n");
                using (var conn = new NpgsqlConnection(connectString))
                {
                    conn.Open();
                    using (var command = GetCommand(conn, $"SELECT fullfilepath FROM historical_ocr_data LIMIT {limit}"))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                docs.Add(ReplaceUnsupportedCharacters(reader.GetString(0), "/", "\\"));
                            }
                        }
                    }
                }               
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Info, "Something wrong with executing command or with data in table \n");
                _log.WriteLog(LogLevel.Error, ex.Message + "\n" + ex.StackTrace);
            }
            return docs;
        }
        public NpgsqlCommand GetCommand(NpgsqlConnection connection, string commandText)
        {
            return new NpgsqlCommand
            {
                Connection = connection,
                CommandTimeout = 0,
                CommandType = CommandType.Text,
                CommandText = commandText
            };
        }
        public void InsertOcr(string connectString,List<HistoricalOcrData> historicData, List<VanguardDoc> docs)
        {
            if (historicData.Count == 0)
            {
                _log.WriteLog(LogLevel.Info, "No historicData to insert \n");
                return;
            }
            for (var i = 0; i < historicData.Count; i++)
            {
                historicData[i].Data = ReplaceUnsupportedCharacters(historicData[i].Data, "'", ".");
                InsertOcrData(connectString, historicData[i], docs[i]);
            }
        }

        public string ReplaceUnsupportedCharacters(string word,string oldChar,string newChar)
        {
            return word.Contains(oldChar) ? word.Replace(oldChar, newChar) : word;
        }

        public void InsertOcrData(string connectString, HistoricalOcrData histoticData, VanguardDoc docs)
        {
            const string newErrorMessage = "0";
            try
            {
                _log.WriteLog(LogLevel.Info, $"Insert ocrData with path <'{histoticData.FullFilePath}'> into (Postgres) database \n");
                using (var conn = new NpgsqlConnection(connectString))
                {
                    conn.Open();
                    using (var command = GetCommand(conn, "INSERT INTO \"public\".\"ocr_data\" (\"DocId\", \"TenantId\", \"ErrorMessage\", \"StatusId\"," +
                   $" \"Data\", \"createdAt\", \"updatedAt\") VALUES({docs.DocId}, {histoticData.TenantId},  '{newErrorMessage}', {histoticData.StatusId}, '{histoticData.Data}'," +
                   $" '{histoticData.CreatedAt}', '{histoticData.UpdatedAt}') " +
                   "ON CONFLICT ON CONSTRAINT ocr_data_pkey " +
                   $"DO UPDATE SET \"ErrorMessage\" = '{newErrorMessage}', \"StatusId\" = {histoticData.StatusId}," +
                   $"\"createdAt\" = '{histoticData.CreatedAt}', \"updatedAt\" = '{histoticData.UpdatedAt}'"))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Info, "Something wrong with executing command or with data in table \n");
                _log.WriteLog(LogLevel.Error, ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void RemoveHistoricalData(string connectString, List<HistoricalOcrData> historicData)
        {
            if (historicData.Count == 0)
            {
                _log.WriteLog(LogLevel.Info, "No historicData to remove \n");
                return;
            }
            foreach (var t in historicData)
            {
                RemoveHistoricalOcrData(connectString, t);
            }
        }

        public void RemoveHistoricalOcrData(string connectString, HistoricalOcrData historicData)
        {
            try
            {
                _log.WriteLog(LogLevel.Info, $"Remove historicalData with path <'{historicData.FullFilePath}'> from (Postgres) database \n");
                using (var conn = new NpgsqlConnection(connectString))
                {
                    conn.Open();
                    using (var command = GetCommand(conn,
                    $"DELETE FROM historical_ocr_data WHERE fullfilepath = '{historicData.FullFilePath}'"))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Info, "Something wrong with executing command or with data in table \n");
                _log.WriteLog(LogLevel.Error, ex.Message + "\n" + ex.StackTrace);
            }
        }
        public List<HistoricalOcrData> GetPostgresHistoricData(string connectString,List<VanguardDoc>vanguardDocs)
        {
            var historicData = new List<HistoricalOcrData>();
            if (vanguardDocs.Count == 0)
            {
                _log.WriteLog(LogLevel.Info, "No vanguard documents \n");
            }
            try
            {
                _log.WriteLog(LogLevel.Info, "Get historicalData from (Postgres) database \n");
                using (var conn = new NpgsqlConnection(connectString))
                {
                    var paths = vanguardDocs.Select(i => i.DocPath);
                    conn.Open();
                    using (var command = GetCommand(conn, "SELECT tenantid, fullfilepath, errormessage, statusid,Data, createdat, updatedat FROM historical_ocr_data  " +
                        $" where fullfilepath IN ('{string.Join("','", paths)}')"))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                historicData.Add(new HistoricalOcrData
                                {
                                    TenantId = Convert.ToInt32(reader["TenantId"]),
                                    FullFilePath = Convert.ToString(reader["FullFilePath"]),
                                    ErrorMessage = Convert.ToString(reader["ErrorMessage"]),
                                    StatusId = Convert.ToInt32(reader["StatusId"]),
                                    Data = Convert.ToString(reader["Data"]),
                                    CreatedAt = reader.Get​Provider​Specific​Value(reader.GetOrdinal("createdAt")),
                                    UpdatedAt = reader.Get​Provider​Specific​Value(reader.GetOrdinal("updatedAt"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Info, "Something wrong with executing command or with data in table \n");
                _log.WriteLog(LogLevel.Error, ex.Message + "\n" + ex.StackTrace);
            }
            return historicData;           
        }
    }
}
