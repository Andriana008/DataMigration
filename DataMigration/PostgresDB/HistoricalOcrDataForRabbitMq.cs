namespace DataMigration.PostgresDB
{
    public class HistoricalOcrDataForRabbitMq
    {
        public int TenantId { get; set; }

        public string FullFilePath { get; set; }

        public string ErrorMessage { get; set; }

        public int? StatusId { get; set; }

        public string Data { get; set; }

        public object CreatedAt { get; set; }

        public object UpdatedAt { get; set; }

        public long DocId { get; set; }
      
    }
}
