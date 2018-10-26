using System;
using System.Data.SqlClient;
using System.Windows;
using System.Configuration;
using System.Linq;
using Npgsql;
using DataMigration.DataLayer;
using DataMigration.Logger;


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
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.Error,
                    "Error while getting limit value from configuration. Error details: \n" + ex.Message + "\n");
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
                    _postgresConnect.RemoveHistoricalData(npgsqlLocalConnection, historicalOcrData);
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
        private void CheckAllConnections()
        {
            _logger.WriteLog(LogLevel.Info, "Check all connections \n");
            try
            {
                using (var sqlConnection = new SqlConnection(SqlConnectionString)) { sqlConnection.Open(); }
                using (var npgsqlConnection = new NpgsqlConnection(LocalPostgreConnectionString)) { npgsqlConnection.Open(); }
                using (var npgsqlConnection = new NpgsqlConnection(DockerPostgresConnectionString)) { npgsqlConnection.Open(); }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.Error, "Error while trying to open connection strings. Error details: \n" + ex.Message + "\n");
            }
        }
       
    }
}

