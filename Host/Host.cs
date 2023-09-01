using DataStructures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Host
{
    public class ObjectState
    {
        public Socket workSocket = null;
        public const int bufferSize = 1024;
        public byte[] buffer = new byte[bufferSize];
        public StringBuilder sb = new StringBuilder();
    }

    class Host
    {
        private Socket cableCloudSocket;
        private Socket asonSocket;
        private string hostName;
        private int hostPort;
        private IPAddress destAddress;
        private String destName;
        private IPAddress ipSourceAddress;
        private int h1Port;
        private IPAddress ipAddressH1;
        private int h2Port;
        private IPAddress ipAddressH2;
        private int h3Port;
        private IPAddress ipAddressH3;
        private int h4Port;
        private IPAddress ipAddressH4;
        private int cableCloudPort;
        private IPAddress asonIpAddress;
        private int asonPort;
        private int destinationPort;
        private IPAddress cableCloudIpAddress;
        private ManualResetEvent allDone = new ManualResetEvent(false);
        List<int> slots = new List<int>();
        //List<bool> connection = new List<bool>();
        bool connection = false;
        bool isYTyped = false;
        bool isNTyped = false;
        private CPCC Cpcc = new CPCC();

        public Host(string filePath)
        {
            LoadPropertiesFromFile(filePath);
            Console.Title = hostName;
            //var t = Task.Run(action: () => ConnectToASON());
            var t = Task.Run(action: () => ConnectToCableCloud());
            Thread.Sleep(1000);
            //ConnectToCableCloud();
            ConnectToASON();
        }

        public void StartHost(Socket cableCloudSocket)
        {
            Task.Run(action: () => ReceiveMessages());
            Task.Run(action: () => ReceiveMessagesFromASON());
            Thread.Sleep(1000);
            Console.WriteLine("Write '1'  if you want to send the message.");
            while (true)
            {
                //Thread.Sleep(1000);
                //Console.WriteLine("Write '1'  if you want to send the message.");

                //Clear the keyboard buffer.
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(false);
                }

                //Console.WriteLine("jestem tu: ");
                string decision = Console.ReadLine();
                //Console.WriteLine("tu nie jestem jeszcze: ");

                if (connection)
                {
                    Console.WriteLine("Type a message or 'exit' if you want to close connection");
                    //Console.WriteLine("Type a message: ");
                    var messageToCloud = Console.ReadLine();
                    if (messageToCloud == "exit")
                    {
                        connection = false;
                        string messageASON = "CALL-TEARDOWN";
                        SendToAson(asonSocket, hostName, destAddress.ToString(), messageASON, 0);
                        continue;
                    }
                    else if (messageToCloud == "")
                    {
                        
                        Logs.ShowLog(LogType.ERROR, "Connection lost");
                        connection = false;
                        string messageASON = "CALL-TEARDOWN";
                        SendToAson(asonSocket, hostName, destAddress.ToString(), messageASON, 0);
                        continue;
                    }
                    else
                    {
                        Send(cableCloudSocket, messageToCloud);
                        Console.WriteLine("Press ENTER if you want to send the message.");
                        continue;
                    }
                }
                else if (decision == "1")
                {
                    try
                    {
                        Console.WriteLine("\nChoose bandwidth. Write down an integer [Mb/s]:");
                                                
                        long choiceBandWidth = long.Parse(Console.ReadLine()) * 1000000;
                        //Console.WriteLine("Your bandwidth: " + choiceBandWidth);

                        Console.WriteLine("\nChoose which host you want to send the message to: \n    1. H1 \n    2. H2 \n    3. H3 \n    4. H4");
                        var choice = Console.ReadLine();
                        if (choice == "1")
                        {
                            string messageASON = "CALL-CPCC";
                            SendToAson(asonSocket, hostName, "H1", messageASON, choiceBandWidth);
                            Cpcc.SendCallRequest(hostName, "H1", choiceBandWidth);
                            destinationPort = h1Port;
                            destAddress = ipAddressH1;
                            continue;
                        }
                        else if (choice == "2")
                        {
                            string messageASON = "CALL-CPCC";
                            SendToAson(asonSocket, hostName, "H2", messageASON, choiceBandWidth);
                            Cpcc.SendCallRequest(hostName, "H2", choiceBandWidth);
                            destinationPort = h2Port;
                            destAddress = ipAddressH2;
                            continue;
                        }
                        else if (choice == "3")
                        {
                            string messageASON = "CALL-CPCC";
                            SendToAson(asonSocket, hostName, "H3", messageASON, choiceBandWidth);
                            Cpcc.SendCallRequest(hostName, "H3", choiceBandWidth);
                            destinationPort = h3Port;
                            destAddress = ipAddressH3;
                            continue;
                        }
                        else if (choice == "4")
                        {
                            string messageASON = "CALL-CPCC";
                            SendToAson(asonSocket, hostName, "H4", messageASON, choiceBandWidth);
                            Cpcc.SendCallRequest(hostName, "H4", choiceBandWidth);
                            destinationPort = h4Port;
                            destAddress = ipAddressH4;
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("\nNo such host connected. Try again.");
                            continue;
                        }

                        //Console.WriteLine("Write message");
                        //while (!connection)
                        //{
                        //    Thread.Sleep(500);
                        //    conti;
                        //}
                        //Console.WriteLine("Type a message: ");
                        //if (connection)
                        //{
                        //    string message = Console.ReadLine().ToString();
                        //    Send(cableCloudSocket, message);
                        //}
                        
                        //allDone.WaitOne();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    
                }
                else if (decision == "Y")
                {
                    //Thread.Sleep(10000);
                    isYTyped = true;
                    continue;
                }
                else if (decision == "N")
                {
                    //Thread.Sleep(10000);
                    isNTyped = true;
                    continue;
                }
                else
                {
                    Logs.ShowLog(LogType.ERROR, "You wrote something other than '1' ");
                    Console.WriteLine("Please try again.\n");
                }
            }
        }

        private void ConnectToASON()
        {
            //Logs.ShowLog(LogType.INFO, "Connecting to CPCC...");
            
            while (true)
            {
                asonSocket = new Socket(asonIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    asonSocket.Connect(new IPEndPoint(asonIpAddress, asonPort));

                }
                catch (Exception)
                {
                    //Logs.ShowLog(LogType.ERROR, "Couldn't connect to CPCC.");
                    //Console.WriteLine("Reconnecting...");
                    Thread.Sleep(5000);
                    continue;
                }

                try
                {
                    //Logs.ShowLog(LogType.INFO, "Sending CONNECTED to CPCC...");
                    string connectedMessage = "CONNECTED";
                    Package package = new Package(hostName, asonIpAddress.ToString(), asonPort, connectedMessage);
                    asonSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(package)));

                    byte[] buffer = new byte[1024];
                    int bytes = asonSocket.Receive(buffer);

                    var message = Encoding.ASCII.GetString(buffer, 0, bytes);

                    if (message.Contains("CONNECTED"))
                    {
                        //Logs.ShowLog(LogType.CONNECTED, "Connected to CPCC.");

                        while (true)
                        {
                            ReceiveMessagesFromASON();
                        }
                       
                    }
                }
                catch (Exception e)
                { 
                    Logs.ShowLog(LogType.ERROR, e.ToString());
                }

            }
        }
        private void ConnectToCableCloud()
        {
            //Logs.ShowLog(LogType.INFO, "Connecting to cable cloud...");
            while (true)
            {
                cableCloudSocket = new Socket(cableCloudIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    cableCloudSocket.Connect(new IPEndPoint(cableCloudIpAddress, cableCloudPort));

                }
                catch (Exception)
                {
                    //Logs.ShowLog(LogType.ERROR, "Couldn't connect to cable cloud.");
                    //Console.WriteLine("Reconnecting...");
                    Thread.Sleep(5000);
                    continue;
                }

                try
                {
                    //Logs.ShowLog(LogType.INFO, "Sending CONNECTED to cable cloud...");
                    string connectedMessage = "CONNECTED";
                    Package package = new Package(hostName, cableCloudIpAddress.ToString(), cableCloudPort, connectedMessage);
                    cableCloudSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(package)));

                    byte[] buffer = new byte[1024];
                    int bytes = cableCloudSocket.Receive(buffer);

                    var message = Encoding.ASCII.GetString(buffer, 0, bytes);

                    if (message.Contains("CONNECTED"))
                    {
                        //Logs.ShowLog(LogType.CONNECTED, "Connected to cable cloud.");                    
                        StartHost(cableCloudSocket);
                        break;

                    }
                }
                catch (Exception)
                {
                    Logs.ShowLog(LogType.ERROR, "Couldn't send hello to cable cloud.");
                }

            }
        }

        private void ReceiveMessagesFromASON()
        {

            byte[] buffer = new byte[2000];
            int bytes = asonSocket.Receive(buffer);
            var message = Encoding.ASCII.GetString(buffer, 0, bytes);
            if (message.StartsWith("{"))
            {
                //Console.WriteLine("Flaga: " + message);
                Package package = DeserializeFromJson(message);
                if (package.Message == "CONNECTION-ACCEPTED")
                {
                    Logs.ShowLog(LogType.INFO, "Connection established to " + package.SourceName);
                    Console.WriteLine("Press ENTER if you want to send the message.");
                    slots  =  package.Slots;
                    connection = true;
                    destName = package.SourceName;
                }
                else if (package.Message == "CONNECTION-REJECTED")
                {
                    Logs.ShowLog(LogType.INFO, "Connection rejected with " + package.DestName);
                    connection = false;
                }               
                else if(package.Message == "CALL-TEARDOWN")
                {
                    Logs.ShowLog(LogType.INFO, "Connection closed with " + package.SourceName);
                    connection = false;
                }
                else if (package.Message == "CONNECTION-CLOSED")
                {
                    Logs.ShowLog(LogType.INFO, $"Connection with {package.DestName} closed.");
                    Console.WriteLine("Write '1'  if you want to send the message.");
                }
                else if (package.Message == "CALL-ACCEPT")
                {
                    Console.WriteLine($"Do you want to accept connection from {package.SourceName}?");
                    Console.WriteLine("Type: Y/N");

                    //Clear the keyboard buffer.
                    while (Console.KeyAvailable)
                    {
                        Console.ReadKey(false);
                    }

                    //string answer = Console.ReadLine();

                    string answer = "";
                    while (true)
                    {
                        if (isYTyped || isNTyped)
                        {
                            if (isYTyped)
                            {
                                answer = "Y";
                            }
                            else
                            {
                                answer = "N";
                            }
                            break;
                        }

                    }

                    switch (answer)
                    {
                        case "Y":
                            package.Message = "CONNECTION-ACCEPTED";
                            Logs.ShowLog(LogType.INFO, "Connection accepted.");
                            Cpcc.SendCallAccept(package.DestName, package.SourceName);
                            SendToAson(asonSocket, package.DestName, package.SourceName, package.Message, package.Bandwidth);
                            break;
                        case "N":
                            package.Message = "CONNECTION-REJECTED";
                            //var packageNegative = SerializeToJson(package);
                            Logs.ShowLog(LogType.INFO, "Connection rejected.");
                            Cpcc.SendCallReject(package.DestName, package.SourceName);
                            SendToAson(asonSocket, package.DestName, package.SourceName, package.Message, package.Bandwidth);
                            break;
                        default:
                            Logs.ShowLog(LogType.ERROR, "You wrote something other than Y/N.");
                            break;
                    }
                    isYTyped = false;
                    isNTyped = false;
                }
            }
           
        }

        private void ReceiveMessages()
        {
            while (true)
            {
                byte[] buffer = new byte[1024];
                int bytes = cableCloudSocket.Receive(buffer);
                var message = Encoding.ASCII.GetString(buffer, 0, bytes);
                Package package = DeserializeFromJson(message);
                Console.WriteLine($"Received message from {package.SourceName}: " + package.Message);

            }
        }

        private void LoadPropertiesFromFile(string filePath)
        {
            var properties = new Dictionary<string, string>();
            foreach (var row in File.ReadAllLines(filePath))
            {
                properties.Add(row.Split('=')[0], row.Split('=')[1]);
            }
            hostName = properties["HOSTNAME"];
            hostPort = int.Parse(properties["HOSTPORT"]);
            ipSourceAddress = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
            cableCloudIpAddress = IPAddress.Parse(properties["CABLECLOUDIPADDRESS"]);
            cableCloudPort = int.Parse(properties["CABLECLOUDPORT"]);
            asonIpAddress = IPAddress.Parse(properties["ASONIPADDRESS"]);
            asonPort = int.Parse(properties["ASONPORT"]);
            if (hostName == "H1")
            {
                h1Port = int.Parse(properties["HOSTPORT"]);
                ipAddressH1 = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
                h2Port = int.Parse(properties["H2PORT"]);
                ipAddressH2 = IPAddress.Parse(properties["IPADDRESSH2"]);
                h3Port = int.Parse(properties["H3PORT"]);
                ipAddressH3 = IPAddress.Parse(properties["IPADDRESSH3"]);
                h4Port = int.Parse(properties["H4PORT"]);
                ipAddressH4 = IPAddress.Parse(properties["IPADDRESSH4"]);
            }
            else if (hostName == "H2")
            {
                h2Port = int.Parse(properties["HOSTPORT"]);
                ipAddressH2 = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
                h1Port = int.Parse(properties["H1PORT"]);
                ipAddressH1 = IPAddress.Parse(properties["IPADDRESSH1"]);
                h3Port = int.Parse(properties["H3PORT"]);
                ipAddressH3 = IPAddress.Parse(properties["IPADDRESSH3"]);
                h4Port = int.Parse(properties["H4PORT"]);
                ipAddressH4 = IPAddress.Parse(properties["IPADDRESSH4"]);
            }
            else if (hostName == "H3")
            {
                h3Port = int.Parse(properties["HOSTPORT"]);
                ipAddressH3 = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
                h1Port = int.Parse(properties["H1PORT"]);
                ipAddressH1 = IPAddress.Parse(properties["IPADDRESSH1"]);
                h2Port = int.Parse(properties["H2PORT"]);
                ipAddressH2 = IPAddress.Parse(properties["IPADDRESSH2"]);
                h4Port = int.Parse(properties["H4PORT"]);
                ipAddressH4 = IPAddress.Parse(properties["IPADDRESSH4"]);
            }
            else if (hostName == "H4")
            {
                h4Port = int.Parse(properties["HOSTPORT"]);
                ipAddressH4 = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
                h1Port = int.Parse(properties["H1PORT"]);
                ipAddressH1 = IPAddress.Parse(properties["IPADDRESSH1"]);
                h2Port = int.Parse(properties["H2PORT"]);
                ipAddressH2 = IPAddress.Parse(properties["IPADDRESSH2"]);
                h3Port = int.Parse(properties["H3PORT"]);
                ipAddressH3 = IPAddress.Parse(properties["IPADDRESSH3"]);
            }
        }

        private void Send(Socket hostSocket, string data)
        {
            Package package = new Package(hostName, hostPort, destAddress.ToString(), slots, destinationPort, data);
            string json = SerializeToJson(package);
            Logs.ShowLog(LogType.INFO, "Sending package to Cable Cloud.");
            byte[] byteData = Encoding.ASCII.GetBytes(json);
            //Console.WriteLine("Message has been sent.");
            hostSocket.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), hostSocket);
        }

        private void SendToAson(Socket hostSocket, string srcName, string destName, string data, long bandwidth)
        {
            Package package = new Package(srcName, destName, data, bandwidth);
            string json = SerializeToJson(package);
            //Logs.ShowLog(LogType.INFO, "Sending message to CPCC.");
            byte[] byteData = Encoding.ASCII.GetBytes(json);
            hostSocket.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), hostSocket);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket hostSocket = (Socket)ar.AsyncState;
                int byteSent = hostSocket.EndSend(ar);
                //Logs.ShowLog(LogType.INFO, $"Sent: {byteSent} bytes");                
                allDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static string SerializeToJson(Package package)
        {
            string jsonString;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            jsonString = System.Text.Json.JsonSerializer.Serialize(package, options);

            return jsonString;
        }

        public static Package DeserializeFromJson(string serializedString)
        {
            Package package = new Package();
            //Console.WriteLine("..." + serializedString);
            //package = JsonSerializer.Deserialize<Package>(serializedString);
            package = JsonConvert.DeserializeObject<Package>(serializedString);
            return package;
        }
    }

}