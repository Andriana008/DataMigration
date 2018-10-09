using System;
using System.Data.SqlClient;
using System.Windows;
using System.Configuration;
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

        public readonly string SqlConnectionString;
        public readonly string LocalPostgreConnectionString;
        public readonly string DockerPostgresConnectionString;

        public MainWindow()
        {
            InitializeComponent();          
            StartLogConsole();
            try
            {
                _limit= Convert.ToInt32(ConfigurationManager.AppSettings["Limit"]);
                SqlConnectionString = ConfigurationManager.ConnectionStrings["sql_connection"].ConnectionString;
                LocalPostgreConnectionString = ConfigurationManager.ConnectionStrings["local"].ConnectionString;
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
            _logger.WriteLog(LogLevel.Info, "Start logger \n");
        }

        private void Data_Migration_Click(object sender, RoutedEventArgs e)
        {
            var totalCount = _postgresConnect.GetCountOfHistoricData(LocalPostgreConnectionString);
            while (totalCount > 0)
            {
                _logger.WriteLog(LogLevel.Info, $"Get {_limit} documents paths from (Postgres) \n");

                var updatedDocPaths = _postgresConnect.GetAllHistoricPaths(LocalPostgreConnectionString, _limit);

                _logger.WriteLog(LogLevel.Info, "Get matching documents paths \n");

                var resultXmlString = _sqlConnect.GetStringsIntoXmlFormat(updatedDocPaths);

                var vanguardDocsData = _sqlConnect.GetVanguardDocuments(SqlConnectionString, resultXmlString);

                _logger.WriteLog(LogLevel.Info, "Get historical documents from (Postgres) \n");

                var historicalOcrData = _postgresConnect.GetPostgresHistoricData(LocalPostgreConnectionString, vanguardDocsData);

                _logger.WriteLog(LogLevel.Info, "Update documents in (Vanguard) \n");

                _sqlConnect.UpdateDM_OCR_PROCESS(SqlConnectionString, vanguardDocsData);

                _logger.WriteLog(LogLevel.Info, "Insert data in (Postgres) \n");

                _postgresConnect.InsertOcr(DockerPostgresConnectionString, historicalOcrData, vanguardDocsData);

                _logger.WriteLog(LogLevel.Info, "Delete data from historical_ocr (Postgres) \n");

                _postgresConnect.RemoveHistoricalData(LocalPostgreConnectionString, historicalOcrData);

                totalCount -=_limit;

                _logger.WriteLog(LogLevel.Info, $"{totalCount} left \n");
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

