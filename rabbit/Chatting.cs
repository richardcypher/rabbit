using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Threading;

namespace rabbit
{
    class Chatting
    {
        private string name, ip;
        private IModel channel;

        public Chatting(string name, string ip, string selfname)
        {
            this.name = name;
            this.ip = ip;

            Thread thread = new Thread(startServer);
            thread.Start();
            sendMessage(selfname + " get in the room");
            string enter;
            do
            {
                enter = Console.ReadLine();
                if (!enter.Equals("exit"))
                    sendMessage(selfname + ":" + enter);
            }while (!enter.Equals("exit"));
            //close thread connection
            sendMessage(selfname + " has left the room");
            channel.Close();
            try
            {
                thread.Abort();
            }
            catch (ThreadAbortException e)
            {
            }
        }
        void startServer()
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.HostName = ip;
            IConnection connection = factory.CreateConnection();
            channel = connection.CreateModel();
            connection.AutoClose = true;

            string queuename = channel.QueueDeclare().QueueName;
            channel.ExchangeDeclare("MyChat", "direct");
            channel.QueueBind(queuename, "MyChat", name);

            QueueingBasicConsumer consumer = new QueueingBasicConsumer(channel);
            string tag = channel.BasicConsume(queuename, true, consumer);

            while (true)
            {
                try
                {
                    RabbitMQ.Client.Events.BasicDeliverEventArgs e = (RabbitMQ.Client.Events.BasicDeliverEventArgs)consumer.Queue.Dequeue();
                    byte[] body = e.Body;
                    string message = System.Text.Encoding.UTF8.GetString(body);
                    Console.WriteLine(message);
                }
                catch (System.IO.EndOfStreamException ex) 
                {
                    break;
                }
            }
        }

        void sendMessage(string message)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.HostName = ip;
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();

            channel.ExchangeDeclare("MyChat", "direct");
            channel.BasicPublish("MyChat", name, null, System.Text.Encoding.UTF8.GetBytes(message));

            channel.Close();
            connection.Close();
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("the command line arguments are lost..");
                return;
            }
            string name;
            string ip;
            string pattern;

            Console.WriteLine("please enter your name to log in");
            name = Console.ReadLine();
            do
            {
                Console.WriteLine("please enter the server ip");
                ip = Console.ReadLine();
                pattern = "\\b((?!\\d\\d\\d)\\d+|1\\d\\d|2[0-4]\\d|25[0-5])\\." +
                                "((?!\\d\\d\\d)\\d+|1\\d\\d|2[0-4]\\d|25[0-5])\\." +
                                "((?!\\d\\d\\d)\\d+|1\\d\\d|2[0-4]\\d|25[0-5])\\." +
                                "((?!\\d\\d\\d)\\d+|1\\d\\d|2[0-4]\\d|25[0-5])\\b";
                if (!System.Text.RegularExpressions.Regex.IsMatch(ip, pattern))
                    Console.WriteLine("the format of ip is not correct, please enter again");
            }
            while (!System.Text.RegularExpressions.Regex.IsMatch(ip, pattern));
          
            string roomname;
            if (args[0] == "create")
                Console.WriteLine("please enter the room name you want to create ");
            else if (args[0] == "join")
                Console.WriteLine("please enter the room name you want to join");
            roomname = Console.ReadLine();
            new Chatting(roomname, ip, name);
        }
    }
}