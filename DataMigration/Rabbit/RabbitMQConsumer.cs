using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DataMigration.Logger;


namespace DataMigration.Rabbit
{
    public class RabbitMqConsumer
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

        public RabbitMqConsumer() { }
        public RabbitMqConsumer(string url, string vhost,string name)
        {
            _connectionFactory = new ConnectionFactory()
            {
                Uri = new Uri(url),
                VirtualHost = vhost
            };
            _queueName = name;
        }

        public async void Receive(EventHandler<BasicDeliverEventArgs> handler)
        {
            try
            {
                await Task.Run(() =>
                {
                    ConsumingChannel.QueueDeclare(queue: $"{_queueName}",
                        durable: false,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    var consumer = new EventingBasicConsumer(ConsumingChannel);

                    consumer.Received += handler;

                    var messageCount = ConsumingChannel.MessageCount(_queueName);

                    _log.WriteLog(LogLevel.Info, $"Message Received {messageCount} \n");

                    ConsumingChannel.BasicConsume(queue: $"{_queueName}",
                        autoAck: true,
                        consumer: consumer);

                });
            }
            catch (Exception ex)
            {
                _log.WriteLog(LogLevel.Error,
                    "Error while receiving messages from Rabbit. Error details: \n" + ex.Message + "\n");
            }

        }
    }
}

