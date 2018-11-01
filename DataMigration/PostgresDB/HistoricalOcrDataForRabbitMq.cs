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

        public HistoricalOcrDataForRabbitMq(int tenantId, string fullFilePath, string errorMessage, int? statusId,
            string data, object createdAt, object updatedAt, long docId)
        {
            TenantId = tenantId;
            FullFilePath = fullFilePath;
            ErrorMessage = errorMessage;
            StatusId = statusId;
            Data = data;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            DocId = docId;
        }
    }
}
