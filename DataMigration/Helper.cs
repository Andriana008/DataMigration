using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using DataMigration.Logger;
using DataMigration.PostgresDB;

namespace DataMigration
{
    public class Helper
    {
        private Logger.Logger _log = new Logger.Logger();

        public void StartLogConsole(Logger.Logger logger)
        {
            _log = new Logger.Logger(logger);
        }

        public string ReplaceUnsupportedCharacters(string word, string oldChar, string newChar)
        {
            return word.Contains(oldChar) ? word.Replace(oldChar, newChar) : word;
        }

        public string ConvertStringsToXml(List<string> docPaths)
        {
            var resultStr = "";
            if (docPaths.Count == 0)
            {
                _log.WriteLog(LogLevel.Info, "No documents to read \n");
            }
            else
            {
                _log.WriteLog(LogLevel.Info, "Write documents in xml format \n");
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (TextWriter streamWriter = new StreamWriter(memoryStream))
                        {
                            var xmlSerializer = new XmlSerializer(typeof(List<string>));
                            xmlSerializer.Serialize(streamWriter, docPaths);
                            resultStr = XElement.Parse(Encoding.ASCII.GetString(memoryStream.ToArray()))
                                .ToString(SaveOptions.OmitDuplicateNamespaces);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.WriteLog(LogLevel.Error, "Error while getting vanguard documents. Error details: \n" + ex.Message + "\n");
                }
            }
            return resultStr;
        }

        public List<HistoricalOcrDataForRabbitMq> ConvertHistoricOcrDataForRabbitConsuming(List<HistoricalOcrData> histData,List<VanguardDoc>vanguardDoc)
        {
            if (histData.Count == 0 || vanguardDoc.Count == 0)
            {
                _log.WriteLog(LogLevel.Info, "No data to convert \n");
            }
            _log.WriteLog(LogLevel.Info, "Converting data for Rabbit  \n");
            return histData.Select((t, i) => new HistoricalOcrDataForRabbitMq(t.TenantId, t.FullFilePath, t.ErrorMessage, t.StatusId, ReplaceUnsupportedCharacters(t.Data, "'", "."), t.CreatedAt, t.UpdatedAt, vanguardDoc[i].DocId)).ToList();
        }
    }
}
