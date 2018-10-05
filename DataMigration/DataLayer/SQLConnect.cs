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
using DataMigration.Loggers;

namespace DataMigration.DataLayer
{
    public class SQLConnect
    {
        Logger Log=new Logger();

        public void StartLogConsole(Logger logger)
        {
            Log = new Logger(logger);
        }
        public List<VanguardDoc> GetVanguardDocuments(string connectString,List<string> docPathes)
        {
            var docs = new List<VanguardDoc>();
            if (docPathes.Count == 0)
            {
                Log.WriteLog(LogLevel.INFO, $"No documents to read \n");
                return docs;
            }
            try
            {
                Log.WriteLog(LogLevel.INFO, $"Write documents in xml to make temporaly table in order to simplify detting data from DM_CONTENT table \n");
                string resultStr;
                using (var memoryStream = new MemoryStream())
                {
                    using (TextWriter streamWriter = new StreamWriter(memoryStream))
                    {
                        var xmlSerializer = new XmlSerializer(typeof(List<string>));
                        xmlSerializer.Serialize(streamWriter, docPathes);
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
                            CommandText = string.Format(@"
                        declare @doc table (DocPath varchar(max)) 
                        insert into @doc select tbl.col.value('.[1]', 'varchar(max)') from @XML.nodes('ArrayOfString/string') tbl(col) 
                        select dmc.DM_ID, dmc.DMC_ID, dmc.DMC_PATH, dm.DEPT_ID
                        from VG{0}.DOC_MASTER dm
                        join VG{0}.DM_CONTENT dmc on dmc.DM_ID = dm.DM_ID
                        join VG{0}.DM_OCR_PROCESS ocr on ocr.DMC_ID = dmc.DMC_ID
                        join @doc d on d.DocPath = dmc.DMC_PATH", 48215)
                        })
                        {
                            command.Parameters.Add("@XML", SqlDbType.Xml);
                            command.Parameters["@XML"].Value = resultStr;

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    docs.Add(new VanguardDoc
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
                Log.WriteLog(LogLevel.ERROR, ex.Message + "\n" + ex.StackTrace);
            }
            return docs;
        }

        public void UpdateDM_OCR_PROCESS(string connectString,List<VanguardDoc> vanguardDocs)
        {
            IEnumerable<long> docIds = vanguardDocs.Select(i => i.DmcId);
            try
            {
                Log.WriteLog(LogLevel.INFO, $"Update DM_OCR_PROCESS (VanguardDb) with ids-->({string.Join(",", docIds)})\n");
                using (SqlConnection conn = new SqlConnection(connectString))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand("UPDATE [VG48215].[DM_OCR_PROCESS] SET STATUS=2 ,ERROR_COUNT=0 WHERE DMC_ID IN " + $"({string.Join(",", docIds)})");
                    command.Connection = conn;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog(LogLevel.ERROR, ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
