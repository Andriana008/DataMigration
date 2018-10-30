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
        private ConnectionFactory _connectionFactory;
        private string _queueName;
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

        public void Send(HistoricalOcrData historicalOcrData)
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

            _log.WriteLog(LogLevel.Info, $"[x] Sent message to rabbit :");

        }

        public void SentAllData(List<HistoricalOcrData> historicalOcrData)
        {

                foreach (var item in historicalOcrData)
                {
                    Send(item);
                }

        }
    }
}
