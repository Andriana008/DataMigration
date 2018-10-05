using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Configuration;
using Npgsql;
using DataMigration.PostgresDB;
using DataMigration.DataLayer;
using DataMigration.Loggers;

namespace DataMigration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Logger _logger = new Logger();
        private PostgresConnect _postgresConnect = new PostgresConnect();
        private SQLConnect _sqlConnect = new SQLConnect();


        private List<HistoricalOcrData> _historicalOcrDatas = new List<HistoricalOcrData>();
        private List<string> _docPathes = new List<string>();
        private List<string> _updateDocPathes = new List<string>();
        private List<VanguardDoc> _vanguardDocsData = new List<VanguardDoc>();

        public string VanguardSqlConnection { get; set; }
        public string LocalPostgre { get; set; }
        public string DockerPostgres { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            VanguardSqlConnection = ConfigurationManager.ConnectionStrings["sql_connection"].ConnectionString;
            LocalPostgre = ConfigurationManager.ConnectionStrings["local"].ConnectionString;
            DockerPostgres = ConfigurationManager.ConnectionStrings["postgres_from_docker"].ConnectionString;
            StartLogConsole();
            _logger.WriteLog(LogLevel.INFO, "Start \n");
        }

        public void StartLogConsole()
        {
            MainWindow main = this;
            _logger = new Logger(main);
            _postgresConnect.StartLogConsole(_logger);
            _sqlConnect.StartLogConsole(_logger);
            _logger.WriteLog(LogLevel.INFO, "Start logger \n");
        }

        private void Matching_Data_Click(object sender, RoutedEventArgs e)
        {
            _logger.WriteLog(LogLevel.INFO, "Show matching data click \n");

            _postgresConnect.GetAllHisroricPaths(LocalPostgre, _docPathes, _updateDocPathes);

            _vanguardDocsData = _sqlConnect.GetVanguardDocuments(VanguardSqlConnection, _updateDocPathes);

            IEnumerable<string> docs = _vanguardDocsData.Select(i => i.DocPath);

            ShowAllPaths(docs);
        }

        private void Show_Ocr_Data_Click(object sender, RoutedEventArgs e)
        {
            _logger.WriteLog(LogLevel.INFO, "Show ocr_data click \n");

            _historicalOcrDatas = _postgresConnect.GetPostgresHistoricData(LocalPostgre, _vanguardDocsData);

            _sqlConnect.UpdateDM_OCR_PROCESS(VanguardSqlConnection, _vanguardDocsData);

            _postgresConnect.InsertOcr(DockerPostgres, _historicalOcrDatas, _vanguardDocsData);

            ShowtPostgresHistoricData(_vanguardDocsData);
        }


        private void Delete_From_Historical_Click(object sender, RoutedEventArgs e)
        {
            _logger.WriteLog(LogLevel.INFO, "Delete from historical click \n");

            _postgresConnect.RemoveHistoricalData(LocalPostgre, _historicalOcrDatas);
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            _logger.WriteLog(LogLevel.INFO, "Check all connections click \n");
            try
            {
                using (SqlConnection connection = new SqlConnection(VanguardSqlConnection)) { connection.Open(); }
                using (NpgsqlConnection conn = new NpgsqlConnection(LocalPostgre)) { conn.Open(); }
                using (NpgsqlConnection con = new NpgsqlConnection(DockerPostgres)) { con.Open(); }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.ERROR, ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void ShowtPostgresHistoricData(List<VanguardDoc> vanguards)
        {
            try
            {
                _logger.WriteLog(LogLevel.INFO, "Check historical_ocr_data \n");
                using (NpgsqlConnection conn = new NpgsqlConnection(LocalPostgre))
                {
                    IEnumerable<string> pathes = vanguards.Select(i => i.DocPath);
                    NpgsqlDataAdapter adapter = new NpgsqlDataAdapter($"SELECT tenantid, fullfilepath, errormessage, statusid,data, createdat, updatedat FROM historical_ocr_data  " +
                        $" where fullfilepath IN ('{string.Join("','", pathes)}')", conn);
                    DataTable table = new DataTable("Data");
                    adapter.Fill(table);
                    data.ItemsSource = table.DefaultView;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.ERROR, ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void ShowAllPaths(IEnumerable<string> docs)
        {
            try
            {
                _logger.WriteLog(LogLevel.INFO, " Show all selected paths \n");
                using (NpgsqlConnection conn = new NpgsqlConnection(LocalPostgre))
                {
                    NpgsqlDataAdapter da = new NpgsqlDataAdapter($"SELECT fullfilepath FROM historical_ocr_data LIMIT 10", conn);
                    DataTable dt = new DataTable("Data");
                    da.Fill(dt);
                    data.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(LogLevel.ERROR, ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}

