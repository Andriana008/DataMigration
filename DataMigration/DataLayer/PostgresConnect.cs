using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using DataMigration.PostgresDB;
using DataMigration.Loggers;


namespace DataMigration.DataLayer
{
    public class PostgresConnect
    {
        Logger Log = new Logger();

        public void StartLogConsole(Logger logger)
        {
            Log = new Logger(logger);
        }
        public void GetAllHisroricPaths(string connectString, List<string> docPathes, List<string> updateDocPathes)
        {
            try
            {
                Log.WriteLog(LogLevel.INFO, $"Get all historic paths (Postgres)\n");
                using (NpgsqlConnection conn = new NpgsqlConnection(connectString))
                {
                    conn.Open();
                    using (var command = GetCommand(conn, "SELECT fullfilepath FROM historical_ocr_data LIMIT 10"))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                docPathes.Add(reader.GetString(0));
                                updateDocPathes.Add(reader.GetString(0).Replace("/", "\\"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog(LogLevel.ERROR, ex.Message + "\n" + ex.StackTrace);
            }
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
                Log.WriteLog(LogLevel.INFO, $"No data to insert \n");
                return;
            }
            else
            {
                for (int i = 0; i < historicData.Count; i++)
                {
                    if (historicData[i].Data.Contains("'"))
                    {
                        historicData[i].Data = historicData[i].Data.Replace("'", ".");
                    }
                    InsertOcrData(connectString, historicData[i], docs[i]);
                }
            }
        }
        public void InsertOcrData(string connectString, HistoricalOcrData histData, VanguardDoc docs)
        {
            try
            {
                Log.WriteLog(LogLevel.INFO, $"Insert ocrData with path <'{histData.FullFilePath}'> into (Postgres) database \n");
                using (NpgsqlConnection conn = new NpgsqlConnection(connectString))
                {
                    conn.Open();
                    using (var command = GetCommand(conn, $"INSERT INTO \"public\".\"ocr_data\" (\"DocId\", \"TenantId\", \"ErrorMessage\", \"StatusId\"," +
                   $" \"Data\", \"createdAt\", \"updatedAt\") VALUES({docs.DocId}, {histData.TenantId}, '0', {histData.StatusId}, '{histData.Data}'," +
                   $" '{histData.CreatedAt}', '{histData.UpdatedAt}') " +
                   "ON CONFLICT ON CONSTRAINT ocr_data_pkey " +
                   $"DO UPDATE SET \"ErrorMessage\" = '0', \"StatusId\" = {histData.StatusId}," +
                   $"\"createdAt\" = '{histData.CreatedAt}', \"updatedAt\" = '{histData.UpdatedAt}'"))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog(LogLevel.ERROR, ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void RemoveHistoricalData(string connectString, List<HistoricalOcrData> historicData)
        {
            if (historicData.Count == 0)
            {
                Log.WriteLog(LogLevel.INFO, $"No data to remove \n");
                return;
            }
            else
            {
                for (int i = 0; i < historicData.Count; i++)
                {
                    RemoveHistoricalOcrData(connectString, historicData[i]);
                }
            }
        }

        public void RemoveHistoricalOcrData(string connectString, HistoricalOcrData data)
        {
            try
            {
                Log.WriteLog(LogLevel.INFO, $"Remove historicalData with path <'{data.FullFilePath}'> from (Postgres) database \n");
                using (NpgsqlConnection conn = new NpgsqlConnection(connectString))
                {
                    conn.Open();
                    using (var command = GetCommand(conn,
                    $"DELETE FROM historical_ocr_data WHERE fullfilepath = '{data.FullFilePath}'"))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog(LogLevel.ERROR, ex.Message + "\n" + ex.StackTrace);
            }
        }
        public List<HistoricalOcrData> GetPostgresHistoricData(string connectString,List<VanguardDoc>vanguardDocs)
        {
            List<HistoricalOcrData> historicalOcrDatas = new List<HistoricalOcrData>();
            try
            {
                Log.WriteLog(LogLevel.INFO, $"Get historicalData from (Postgres) database \n");
                using (NpgsqlConnection conn = new NpgsqlConnection(connectString))
                {
                    IEnumerable<string> pathes = vanguardDocs.Select(i => i.DocPath);
                    conn.Open();
                    using (var command = GetCommand(conn, $"SELECT tenantid, fullfilepath, errormessage, statusid,data, createdat, updatedat FROM historical_ocr_data  " +
                        $" where fullfilepath IN ('{string.Join("','", pathes)}')"))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                historicalOcrDatas.Add(new HistoricalOcrData
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
                Log.WriteLog(LogLevel.ERROR, ex.Message + "\n" + ex.StackTrace);
            }
            return historicalOcrDatas;           
        }
    }
}
