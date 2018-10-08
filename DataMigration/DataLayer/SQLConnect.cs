using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using DataMigration.PostgresDB;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using DataMigration.Logger;

namespace DataMigration.DataLayer
{
    public class SqlConnect
    {
        private Logger.Logger _log=new Logger.Logger();

        public void StartLogConsole(Logger.Logger logger)
        {
            _log = new Logger.Logger(logger);
        }
        public List<VanguardDoc> GetVanguardDocuments(string connectString,List<string> docPaths)
        {
            var vanguardDocs = new List<VanguardDoc>();
            if (docPaths.Count == 0)
            {
                _log.WriteLog(LogLevel.Info, "No documents to read \n");
                return vanguardDocs;
            }
            try
            {
                _log.WriteLog(LogLevel.Info, "Write documents in xml to make temporaly table in order to simplify detting data from DM_CONTENT table \n");
                string resultStr;
                using (var memoryStream = new MemoryStream())
                {
                    using (TextWriter streamWriter = new StreamWriter(memoryStream))
                    {
                        var xmlSerializer = new XmlSerializer(typeof(List<string>));
                        xmlSerializer.Serialize(streamWriter, docPaths);
                        resultStr = XElement.Parse(Encoding.ASCII.GetString(memoryStream.ToArray())).ToString(SaveOptions.OmitDuplicateNamespaces);
                    }
                }
                using (var connection = new SqlConnection(connectString))
                {
                    connection.Open();
                    try
                    {
                        using (var command = new SqlCommand
                        {
                            CommandType = CommandType.Text,
                            Connection = connection,
                            CommandTimeout = 0,
                            CommandText = (@"
                        declare @doc table (DocPath varchar(max)) 
                        insert into @doc select tbl.col.value('.[1]', 'varchar(max)') from @XML.nodes('ArrayOfString/string') tbl(col) 
                        select dmc.DM_ID, dmc.DMC_ID, dmc.DMC_PATH, dm.DEPT_ID
                        from VG48215.DOC_MASTER dm
                        join VG48215.DM_CONTENT dmc on dmc.DM_ID = dm.DM_ID
                        join VG48215.DM_OCR_PROCESS ocr on ocr.DMC_ID = dmc.DMC_ID
                        join @doc d on d.DocPath = dmc.DMC_PATH")
                        })
                        {
                            command.Parameters.Add("@XML", SqlDbType.Xml);
                            command.Parameters["@XML"].Value = resultStr;

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    vanguardDocs.Add(new VanguardDoc
                                    {
                                        DocId = Convert.ToInt64(reader["DM_ID"]),
                                        DmcId = Convert.ToInt64(reader["DMC_ID"]),
                                        DocPath = Convert.ToString(reader["DMC_PATH"]).Replace("\\", "/"),
                                        DeptId = Convert.ToInt32(reader["DEPT_ID"]),
                                    });
                                }
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Info, "Something wrong with executing command or with data in table \n");
                _log.WriteLog(LogLevel.Error, ex.Message + "\n" + ex.StackTrace);
            }
            return vanguardDocs;
        }

        public void UpdateDM_OCR_PROCESS(string connectString,List<VanguardDoc> vanguardDocs)
        {
            const int newStatus = 2;
            const int newErrorCount = 0;
            if (vanguardDocs.Count==0)
            {
                _log.WriteLog(LogLevel.Info, "No data to update \n");
                return;
            }
            IEnumerable<long> docIds = vanguardDocs.Select(i => i.DmcId).ToArray();
            try
            {
                _log.WriteLog(LogLevel.Info, $"Update DM_OCR_PROCESS (VanguardDb) with ids-->({string.Join(",", docIds)})\n");
                using (var conn = new SqlConnection(connectString))
                {
                    conn.Open();
                    var command =
                        new SqlCommand(
                            $"UPDATE [VG48215].[DM_OCR_PROCESS] SET STATUS={newStatus} ,ERROR_COUNT={newErrorCount} WHERE DMC_ID IN " +
                            $"({string.Join(",", docIds)})") {Connection = conn};
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Info, "Something wrong with executing command or with data in table \n");
                _log.WriteLog(LogLevel.Error, ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
