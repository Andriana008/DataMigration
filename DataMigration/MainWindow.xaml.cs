using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Configuration;
using System.Linq;
using System.Text;
using Npgsql;
using DataMigration.DataLayer;
using DataMigration.Logger;
using DataMigration.PostgresDB;
using DataMigration.Rabbit;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace DataMigration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly int _limit;

        private Logger.Logger _logger = new Logger.Logger();
        private readonly PostgresConnect _postgresConnect = new PostgresConnect();
        private readonly SqlConnect _sqlConnect = new SqlConnect();
        private readonly Helper _help = new Helper();
        private readonly RabbitMqConsumer _rabbitMqConsumer=new RabbitMqConsumer();
        private readonly RabbitMqProducer _rabbitMqProducer=new RabbitMqProducer();
        private readonly List<HistoricalOcrData> _rabbitHistoricalData=new List<HistoricalOcrData>();

        public readonly string SqlConnectionString;
        public readonly string LocalPostgreConnectionString;
        public readonly string DockerPostgresConnectionString;
        

        public MainWindow()
        {
            InitializeComponent();          
            StartLogConsole();
            try
            {
                _limit = Convert.ToInt32(ConfigurationManager.AppSettings["limit"]);
                var queueName = ConfigurationManager.AppSettings["queueName"];                
                _rabbitMqConsumer =new RabbitMqConsumer(ConfigurationManager.AppSettings["rabbitMQHost"],ConfigurationManager.AppSettings["rabbitVhost"],queueName);
                _rabbitMqProducer = new RabbitMqProducer(ConfigurationManager.AppSettings["rabbitMQHost"],ConfigurationManager.AppSettings["rabbitVhost"], queueName);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.Error,
                    "Error while getting data from configuration. Error details: \n" + ex.Message + "\n");
            }
            try
            {              
               
                SqlConnectionString = ConfigurationManager.ConnectionStrings["vanguard_database"].ConnectionString;
                LocalPostgreConnectionString = ConfigurationManager.ConnectionStrings["local_postgres"].ConnectionString;
                DockerPostgresConnectionString =ConfigurationManager.ConnectionStrings["postgres_from_docker"].ConnectionString;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.Error, "Error while getting connection strings from configuration. Error details: \n" + ex.Message + "\n");
            }
            _rabbitMqProducer.StartLogConsole(_logger);
            _rabbitMqConsumer.StartLogConsole(_logger);
            CheckAllConnections();
            _logger.WriteLog(LogLevel.Info, "Start app \n");
        }
        public void StartLogConsole()
        {
            var main = this;
            _logger = new Logger.Logger(main);
            _postgresConnect.StartLogConsole(_logger);
            _sqlConnect.StartLogConsole(_logger);
            _help.StartLogConsole(_logger);
            _logger.WriteLog(LogLevel.Info, "Start logger \n");
        }
        private void Data_Migration_Click(object sender, RoutedEventArgs e)
        {
            var npgsqlLocalConnection = new NpgsqlConnection(LocalPostgreConnectionString);
            var vanguardConnection = new SqlConnection(SqlConnectionString);
            var npgsqlDockerConnection = new NpgsqlConnection(DockerPostgresConnectionString);
            try
            {               
                npgsqlLocalConnection.Open();               
                vanguardConnection.Open();              
                npgsqlDockerConnection.Open();
                var totalCount = _postgresConnect.GetCountOfHistoricData(npgsqlLocalConnection);
                while (totalCount > 0)
                {
                    _logger.WriteLog(LogLevel.Info, "Get historic data from (Postgres) \n");
                    var historicalOcrData = _postgresConnect.GetPostgresHistoricData(npgsqlLocalConnection, _limit);
                    _logger.WriteLog(LogLevel.Info, "Get matching documents paths \n");
                    var historicalDocsToMigrate = historicalOcrData
                        .Select(i => _help.ReplaceUnsupportedCharacters(i.FullFilePath, "/", "\\")).ToList();
                    var resultXmlString = _help.ConvertStringsToXml(historicalDocsToMigrate);
                    var vanguardDocsData = _sqlConnect.GetVanguardDocuments(vanguardConnection, resultXmlString);
                    _logger.WriteLog(LogLevel.Info, "Update documents in (Vanguard) \n");
                    _sqlConnect.UpdateDM_OCR_PROCESS(vanguardConnection, vanguardDocsData);
                    _logger.WriteLog(LogLevel.Info, "Insert data in (Postgres) \n");
                    historicalOcrData =_postgresConnect.RemoveUnmatchingHistoricData(historicalOcrData, vanguardDocsData);
                    _postgresConnect.InsertOcr(npgsqlDockerConnection, historicalOcrData, vanguardDocsData);
                    _logger.WriteLog(LogLevel.Info, "Delete data from historical_ocr (Postgres) \n");
                    //_postgresConnect.RemoveHistoricalData(npgsqlLocalConnection, historicalOcrData);
                    totalCount -= _limit;
                    _logger.WriteLog(LogLevel.Info, $"{totalCount} left \n");
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.Error,
                    "Error while trying to open connection strings. Error details: \n" + ex.Message + "\n");
            }
            finally
            {
                npgsqlLocalConnection.Close();
                npgsqlDockerConnection.Close();
                vanguardConnection.Close();
            }
        }

        private void Rabbit_Migration_Click(object sender, RoutedEventArgs e)
        {
            var npgsqlLocalConnection = new NpgsqlConnection(LocalPostgreConnectionString);
            var vanguardConnection = new SqlConnection(SqlConnectionString);
            try
            {
                npgsqlLocalConnection.Open();
                vanguardConnection.Open();
               // var totalCount = _postgresConnect.GetCountOfHistoricData(npgsqlLocalConnection);
                var totalCount = 2;
                while (totalCount > 0)
                {
                   
                    _logger.WriteLog(LogLevel.Info, $"{totalCount} left \n");

                    _logger.WriteLog(LogLevel.Info, "Get historic data from (Postgres) \n");
                    var historicalOcrData = _postgresConnect.GetPostgresHistoricData(npgsqlLocalConnection, _limit);

                    _logger.WriteLog(LogLevel.Info, "Send data into (Rabbit) \n");

                    _rabbitMqProducer.SentAllData(historicalOcrData);
                    _logger.WriteLog(LogLevel.Info, "Get data from (Rabbit) \n");

                    _rabbitMqConsumer.Receive(ReceiveMessage);
                    
                    _logger.WriteLog(LogLevel.Info, "Insert data in (Postgres) \n");
                    
                    _postgresConnect.InsertOcrRabbit(npgsqlLocalConnection, _rabbitHistoricalData);
                    _logger.WriteLog(LogLevel.Info, "Delete data from historical_ocr (Postgres) \n");
                    //_postgresConnect.RemoveHistoricalData(npgsqlLocalConnection, rabbitHistoricalData);

                    totalCount -= _limit;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.Error,
                    "Error while trying to open connection strings. Error details: \n" + ex.Message + "\n");
            }
            finally
            {
                npgsqlLocalConnection.Close();
                vanguardConnection.Close();
            }
        }
        private void CheckAllConnections()
        {
            _logger.WriteLog(LogLevel.Info, "Check all connections \n");
            try
            {
                using (var sqlConnection = new SqlConnection(SqlConnectionString)) { sqlConnection.Open(); }
                using (var npgsqlConnection = new NpgsqlConnection(LocalPostgreConnectionString)) { npgsqlConnection.Open(); }
                //using (var npgsqlConnection = new NpgsqlConnection(DockerPostgresConnectionString)) { npgsqlConnection.Open(); }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.Error, "Error while trying to open connection strings. Error details: \n" + ex.Message + "\n");
            }
        }
        private void ReceiveMessage(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var body = e.Body;
                var message = Encoding.UTF8.GetString(body);
                var histOcrData = JsonConvert.DeserializeObject<HistoricalOcrData>(message);
                _rabbitHistoricalData.Add(histOcrData);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.Error,
                    "Error while receiving messages. Error details: \n" + ex.Message + "\n");
            }
        }
    }
}

