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

        private readonly Helper _help = new Helper();

        public void StartLogConsole(Logger.Logger logger)
        {
            _log = new Logger.Logger(logger);
        }

        public int GetCountOfHistoricData(NpgsqlConnection connection)
        {
            var countOfHistoricData = 0;
            try
            {
                using (var command = GetCommand(connection, "select count(*) from historical_ocr_data"))
                {
                    countOfHistoricData = Convert.ToInt32(command.ExecuteScalar());
                }

            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Error,
                    "Error while getting historic paths. Error details: \n" + ex.Message + "\n");
            }
            return countOfHistoricData;
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

        public List<HistoricalOcrData> RemoveUnmatchingHistoricData(List<HistoricalOcrData> historicData, List<VanguardDoc> docs)
        {
            var docsPaths = docs.Select(i => i.DocPath);
            var historicPaths = historicData.Select(i => i.FullFilePath).ToList();
            foreach (var item in docsPaths)
            {
                if (historicPaths.Contains(item)) continue;
                _log.WriteLog(LogLevel.Info, "Files paths don't match,delete data \n");
                historicData.RemoveAll(i => i.FullFilePath == item);
            }
            return historicData;
        }

        public void InsertOcr(NpgsqlConnection connection, List<HistoricalOcrData> historicData, List<VanguardDoc> docs)
        {
            if (historicData.Count == 0)
            {
                _log.WriteLog(LogLevel.Info, "No historicData to insert \n");
                return;
            }
            try
            {
                for (var i = 0; i < historicData.Count; i++)
                {
                    historicData[i].Data = _help.ReplaceUnsupportedCharacters(historicData[i].Data, "'", ".");
                    InsertOcrData(connection, historicData[i], docs[i]);
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Error, "Error while inserting ocr data. Error details: \n" + ex.Message + "\n");
            }
        }
        public void InsertOcrData(NpgsqlConnection connection, HistoricalOcrData histoticData, VanguardDoc docs)
        {
            try
            {
                _log.WriteLog(LogLevel.Info,
                    $"Insert ocrData with path <'{histoticData.FullFilePath}'> and docId <{docs.DocId}> into (Postgres) database \n");
                using (var command = GetCommand(connection,
                    "INSERT INTO \"public\".\"ocr_data\" (\"docid\", \"errormessage\", \"statusid\"," +
                    $" \"data\", \"createdat\", \"updatedat\") VALUES({docs.DocId},  '{null}', {histoticData.StatusId}, '{histoticData.Data}'," +
                    $" '{histoticData.CreatedAt}', '{histoticData.UpdatedAt}') " +
                    "ON CONFLICT ON CONSTRAINT ocr_data_pkey " +
                    $"DO UPDATE SET \"errormessage\" = '{null}', \"statusid\" = {histoticData.StatusId}," +
                    $"\"createdat\" = '{histoticData.CreatedAt}', \"updatedat\" = '{histoticData.UpdatedAt}'"))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Error, "Error while inserting ocr data. Error details: \n" + ex.Message + "\n");
            }
        }
        public void InsertOcrDataRabbit(NpgsqlConnection connection, HistoricalOcrDataForRabbitMq histoticData)
        {
            try
            {
                _log.WriteLog(LogLevel.Info,
                    $"Insert ocrData with path <'{histoticData.FullFilePath}'> and docId <{histoticData.DocId}> into (Postgres) database \n");
                using (var command = GetCommand(connection,
                    "INSERT INTO \"public\".\"ocr_data\" (\"docid\", \"errormessage\", \"statusid\"," +
                    $" \"data\") VALUES({histoticData.DocId},  '{null}', {histoticData.StatusId}, '{histoticData.Data}')" +
                    "ON CONFLICT ON CONSTRAINT ocr_data_pkey " +
                    $"DO UPDATE SET \"errormessage\" = '{null}', \"statusid\" = {histoticData.StatusId}"))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Error, "Error while inserting ocr data. Error details: \n" + ex.Message + "\n");
            }
        }
        public void RemoveHistoricalData(NpgsqlConnection connection, List<HistoricalOcrData> historicData)
        {
            if (historicData.Count == 0)
            {
                _log.WriteLog(LogLevel.Info, "No historicData to remove \n");
                return;
            }
            try
            {
                foreach (var t in historicData)
                {
                    RemoveHistoricalOcrData(connection, t);
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Error,
                    "Error while removing historic data. Error details: \n" + ex.Message + "\n");
            }
        }

        public void RemoveHistoricalOcrData(NpgsqlConnection connection, HistoricalOcrData historicData)
        {
            try
            {
                _log.WriteLog(LogLevel.Info,
                    $"Remove historicalData with path <'{historicData.FullFilePath}'> from (Postgres) database \n");
                using (var command = GetCommand(connection,
                    $"DELETE FROM historical_ocr_data WHERE fullfilepath = '{historicData.FullFilePath}'"))
                {
                    command.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Error,
                    "Error while removing historic data. Error details: \n" + ex.Message + "\n");
            }
        }
        public List<HistoricalOcrData> GetPostgresHistoricData(NpgsqlConnection connection, int limit)
        {
            var historicData = new List<HistoricalOcrData>();
            try
            {
                _log.WriteLog(LogLevel.Info, "Get historicalData from (Postgres) database \n");
                using (var command = GetCommand(connection,
                    $"SELECT tenantid, fullfilepath, errormessage, statusid,Data, createdat, updatedat FROM historical_ocr_data limit {limit}")
                )
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
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Error,
                    "Error while getting historic data. Error details: \n" + ex.Message + "\n");
            }
            return historicData;
        }
    }
}
