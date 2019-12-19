using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace rpc_client
{
    class Program
    {
        static void Main(string[] args)
        {
            var rpcClient = new RPCClient();

            Console.WriteLine(" [x] Requesting fib(30)");
            var response = rpcClient.Call("30");
            Console.WriteLine(" [.] Got '{0}'", response);

            rpcClient.Close();
        }

        static void TcpClient()
        {
            try
            {
                TcpClient Tcpclient = new TcpClient();
                Console.WriteLine("Connecting..");
                Tcpclient.Connect("127.0.0.1", 5000);
                Console.WriteLine("Connected");
                Console.WriteLine("Ente the String you want to send ");
                string str = Console.ReadLine();
                Stream stm = Tcpclient.GetStream();
                ASCIIEncoding ascnd = new ASCIIEncoding();
                byte[] ba = ascnd.GetBytes(str);
                Console.WriteLine("Sending..");
                stm.Write(ba, 0, ba.Length);
                byte[] bb = new byte[100];
                int k = stm.Read(bb, 0, 100);
                for (int i = 0; i < k; i++)
                {
                    Console.Write(Convert.ToChar(bb[i]));
                }

                Tcpclient.Close();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error" + ex.StackTrace);
            }
        }
    }

    class RPCClient
    {
        private IConnection connection;
        private IModel channel;
        private string replyQueueName;
        private QueueingBasicConsumer consumer;

        public RPCClient()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };
            //factory.Uri = new Uri("amqp://user:pass@hostName:port/vhost");

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            replyQueueName = channel.QueueDeclare();
            consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(replyQueueName, true, consumer);
        }

        public string Call(string message)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = channel.CreateBasicProperties();
            props.ReplyTo = replyQueueName;
            props.CorrelationId = corrId;

            var messageBytes = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish("", "rpc_queue", props, messageBytes);

            while (true)
            {
                var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                if (ea.BasicProperties.CorrelationId == corrId)
                {
                    return Encoding.UTF8.GetString(ea.Body);
                }
            }
        }

        public void Close()
        {
            connection.Close();
        }
    }
}