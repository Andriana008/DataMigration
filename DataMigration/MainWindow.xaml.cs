using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Configuration;
using Npgsql;
using DataMigration.PostgresDB;
using DataMigration.DataLayer;
using DataMigration.Logger;


namespace DataMigration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        private const int Limit =10;
        private int _totalCount;

        private Logger.Logger _logger = new Logger.Logger();
        private readonly PostgresConnect _postgresConnect = new PostgresConnect();
        private readonly SqlConnect _sqlConnect = new SqlConnect();


        private List<HistoricalOcrData> _historicalOcrData = new List<HistoricalOcrData>();
        private List<string> _updatedDocPaths = new List<string>();
        private List<VanguardDoc> _vanguardDocsData = new List<VanguardDoc>();

        public string SqlConnectionString { get; set; }
        public string LocalPostgreConnectionString { get; set; }
        public string DockerPostgresConnectionString { get; set; }

        public MainWindow()
        {
            InitializeComponent();          
            StartLogConsole();
            CheckAllConnections();
            _logger.WriteLog(LogLevel.Info, "Start \n");
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
            _totalCount = _postgresConnect.GetCountOfHistoricData(LocalPostgreConnectionString);
            while (_totalCount > 0)
            {
                _logger.WriteLog(LogLevel.Info, $"Get {Limit} documents paths from (Postgres) \n");

                _updatedDocPaths = _postgresConnect.GetAllHistoricPaths(LocalPostgreConnectionString, Limit);

                _logger.WriteLog(LogLevel.Info, "Get matching documents paths \n");

                _vanguardDocsData = _sqlConnect.GetVanguardDocuments(SqlConnectionString, _updatedDocPaths);

                _logger.WriteLog(LogLevel.Info, "Get historical documents from (Postgres) \n");

                _historicalOcrData =
                    _postgresConnect.GetPostgresHistoricData(LocalPostgreConnectionString, _vanguardDocsData);

                _logger.WriteLog(LogLevel.Info, "Update documents in (Vanguard) \n");

                _sqlConnect.UpdateDM_OCR_PROCESS(SqlConnectionString, _vanguardDocsData);

                _logger.WriteLog(LogLevel.Info, "Insert data in (Postgres) \n");

                _postgresConnect.InsertOcr(DockerPostgresConnectionString, _historicalOcrData, _vanguardDocsData);


                _logger.WriteLog(LogLevel.Info, "Delete data from historical_ocr (Postgres) \n");

                //_postgresConnect.RemoveHistoricalData(LocalPostgreConnectionString, _historicalOcrData);

                _totalCount -=Limit;

                _logger.WriteLog(LogLevel.Info, $"{_totalCount} left \n");
            }
        }

        private void CheckAllConnections()
        {
            _logger.WriteLog(LogLevel.Info, "Check all connections \n");
            try
            {
                SqlConnectionString = ConfigurationManager.ConnectionStrings["sql_connection"].ConnectionString;
                LocalPostgreConnectionString = ConfigurationManager.ConnectionStrings["local"].ConnectionString;
                DockerPostgresConnectionString = ConfigurationManager.ConnectionStrings["postgres_from_docker"].ConnectionString;
                using (var sqlConnection = new SqlConnection(SqlConnectionString)) { sqlConnection.Open(); }
                using (var npgsqlConnection = new NpgsqlConnection(LocalPostgreConnectionString)) { npgsqlConnection.Open(); }
                using (var npgsqlConnection = new NpgsqlConnection(DockerPostgresConnectionString)) { npgsqlConnection.Open(); }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.Error, ex.Message + "\n" + ex.StackTrace);
            }
        }
       
    }
}

