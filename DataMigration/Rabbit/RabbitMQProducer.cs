using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using DataMigration.Logger;
using DataMigration.PostgresDB;
using Newtonsoft.Json;

namespace DataMigration.Rabbit
{
    public class RabbitMqProducer
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly string _queueName;
        private IConnection _connection;
        private IModel _consumingChannel;
        private IConnection Connection => _connection ?? (_connection = _connectionFactory.CreateConnection());
        private IModel ConsumingChannel => _consumingChannel ?? (_consumingChannel = Connection.CreateModel());

        private Logger.Logger _log = new Logger.Logger();

        public void StartLogConsole(Logger.Logger logger)
        {
            _log = new Logger.Logger(logger);
        }

        public RabbitMqProducer() {}

        public RabbitMqProducer(string url, string vhost,string name)
        {
            _connectionFactory = new ConnectionFactory()
            {
                Uri = new Uri(url),
                VirtualHost = vhost
            };
            _queueName = name;
        }

        public void Send(HistoricalOcrDataForRabbitMq historicalOcrData)
        {
            try
            {
                ConsumingChannel.QueueDeclare(queue: $"{_queueName}",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                string message = JsonConvert.SerializeObject(historicalOcrData);
                var body = Encoding.UTF8.GetBytes(message);

                ConsumingChannel.BasicPublish(exchange: "",
                    routingKey: $"{_queueName}",
                    basicProperties: null,
                    body: body);

                _log.WriteLog(LogLevel.Info, "[x] Sent message to Rabbit \n");
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Error,
                    "Error while receiving messages from Rabbit. Error details: \n" + ex.Message + "\n");
            }
        }
        public void SentAllData(List<HistoricalOcrDataForRabbitMq> historicalOcrData)
        {
            if (historicalOcrData.Count == 0)
            {
                _log.WriteLog(LogLevel.Info, "No data to sent to Rabbit \n");
            }
            foreach (var item in historicalOcrData)
            {
                Send(item);
            }
        }
    }
}
