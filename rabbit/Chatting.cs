using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Threading;
//to send a private message use "private(a) message" send to a there is a blank between message and private thing
//to get the users of the room use "+showuser slefname"
namespace rabbit
{
    class Chatting
    {
        private string name, ip, selfname;
        private IModel channel;

        public Chatting(string name, string ip, string selfname)
        {
            this.name = name;
            this.ip = ip;
            this.selfname = selfname;

            Thread thread = new Thread(startServer);
            thread.Start();
            sendMessage(selfname + " enter the room", true, "");
            string enter;
            do
            {
                enter = Console.ReadLine();
                if (enter.Equals("+showuser " + selfname))
                {
                    sendMessage(enter, true, "");
                }
                else if (!enter.Equals("exit"))
                {
                    if (enter.StartsWith("private"))
                    {
                        int indexl = enter.IndexOf("("), indexr = enter.IndexOf(")");
                        int length = indexr - indexl - 1;
                        string to = enter.Substring(indexl + 1, length);
                        sendMessage(selfname + ":" + enter.Substring(enter.IndexOf(" ") + 1), false, to);
                    }
                    else
                        sendMessage(selfname + ":" + enter, true, "");
                }
            }while (!enter.Equals("exit"));
            //close thread connection
            sendMessage(selfname + " leave the room", true, "");
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
            channel.ExchangeDeclare("MyZone", "direct");
            channel.QueueBind(queuename, "MyZone", name);
            channel.QueueBind(queuename, "MyZone", name + "." + selfname);

            QueueingBasicConsumer consumer = new QueueingBasicConsumer(channel);
            string tag = channel.BasicConsume(queuename, true, consumer);

            while (true)
            {
                try
                {
                    RabbitMQ.Client.Events.BasicDeliverEventArgs e = 
                        (RabbitMQ.Client.Events.BasicDeliverEventArgs)consumer.Queue.Dequeue();
                    byte[] body = e.Body;
                    string message = System.Text.Encoding.UTF8.GetString(body);
                    if (message.Trim().StartsWith("+showuser"))
                    {
                        int index = message.IndexOf(" ");
                        string to = message.Substring(index + 1);
                        sendMessage("-" + selfname, false, to);
                    }
                    else if (message.Trim().StartsWith("update"));
                    else if (message.EndsWith("enter the room"))
                    {
                        int index = message.IndexOf(" ");
                        string to = message.Substring(0, index);
                        sendMessage("update " + selfname, false, to);
                        Console.WriteLine(message);
                    }
                    else
                        Console.WriteLine(message);
                }
                catch (System.IO.EndOfStreamException ex) 
                {
                    break;
                }
            }
        }

        void sendMessage(string message, bool broad, string to)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.HostName = ip;
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();

            channel.ExchangeDeclare("MyZone", "direct");
            if (broad)
                channel.BasicPublish("MyZone", name, null, System.Text.Encoding.UTF8.GetBytes(message));
            else
                channel.BasicPublish("MyZone", name + "." + to, null, System.Text.Encoding.UTF8.GetBytes(message));

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
            else
                return;
            roomname = Console.ReadLine();
            new Chatting(roomname, ip, name);
        }
    }
}