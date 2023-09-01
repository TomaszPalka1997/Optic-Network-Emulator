using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using DataStructures;

namespace SubnetworkController
{
    class Program
    {

        private Socket asonSocket;
        private int asonPort = 4999;
        private IPAddress asonIpAddress = IPAddress.Parse("127.0.0.1");
        private ManualResetEvent allDone = new ManualResetEvent(false);
        private string name = "Subnetwork";

        static void Main(string[] args)
        {
            Program subnetwork = new Program(args[0]);
        }

        public Program(string name)
        {
            this.name = name;
            Console.Title = name;
            ConnectToASON();
        }

        private void ConnectToASON()
        {
            //Logs.ShowLog(LogType.INFO, "Connecting to ASON...");

            while (true)
            {
                asonSocket = new Socket(asonIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    asonSocket.Connect(new IPEndPoint(asonIpAddress, asonPort));

                }
                catch (Exception)
                {
                    //Logs.ShowLog(LogType.ERROR, "Couldn't connect to ASON.");
                    //Console.WriteLine("Reconnecting...");
                    Thread.Sleep(5000);
                    continue;
                }

                try
                {
                    //Logs.ShowLog(LogType.INFO, "Sending CONNECTED to ASON...");
                    string connectedMessage = "CONNECTED-SUB";
                    Package package = new Package(name, asonIpAddress.ToString(), asonPort, connectedMessage);
                    asonSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(package)));

                    byte[] buffer = new byte[1024];
                    int bytes = asonSocket.Receive(buffer);

                    var message = Encoding.ASCII.GetString(buffer, 0, bytes);

                    if (message.Contains("CONNECTED"))
                    {
                        Logs.ShowLog(LogType.CONNECTED, "Subnetwork active.");
                        Logs.ShowLog(LogType.LRM, "Sending Local Topology to RC.");
                        Logs.ShowLog(LogType.RC, "Received Local Topology from LRM.");
                        Logs.ShowLog(LogType.RC, "Sending Network Topology to Domain RC.");

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

        private void ReceiveMessagesFromASON()
        {
            byte[] buffer = new byte[2000];
            int bytes = asonSocket.Receive(buffer);
            var message = Encoding.ASCII.GetString(buffer, 0, bytes);
            string srcName;
            string destName;
            if (message.StartsWith("{"))
            {
                Package package = DeserializeFromJson(message);
                if (package.Message == "CONNECTION-ACCEPTED")
                {
                    //Console.WriteLine(message);
                    srcName = package.DestName;
                    destName = package.SourceName;


                    if((srcName == "H1" || srcName == "H2") && (destName == "H3" || destName == "H4"))
                    {
                        // idzie przez Sub1 i Sub2
                        if(name == "Subnetwork1")
                        {
                            StartKombajn(package.InOutsFromSubs[0], package.InOutsFromSubs[1], package.Slots, package.ShortestPath);
                        }
                            
                        else if (name == "Subnetwork2")
                        {
                            StartKombajn(package.InOutsFromSubs[2], package.InOutsFromSubs[3], package.Slots, package.ShortestPath);
                        }
                            
                    }
                    else if ((srcName == "H3" || srcName == "H4") && (destName == "H1" || destName == "H2"))
                    {
                        // idzie przez Sub2 i Sub1
                        if (name == "Subnetwork1")
                            StartKombajn(package.InOutsFromSubs[2], package.InOutsFromSubs[3], package.Slots, package.ShortestPath);
                        else if (name == "Subnetwork2")
                            StartKombajn(package.InOutsFromSubs[0], package.InOutsFromSubs[1], package.Slots, package.ShortestPath);
                    }
                    else
                    {
                        // idzie przez Sub1 lub Sub2
                    }

                }
                else if(package.Message == "WEIGHT-CHANGED")
                {
                    Logs.ShowLog(LogType.INFO, $"Changed connection weight between {package.DestName} and {package.SourceName}");
                    Logs.ShowLog(LogType.LRM, "Sending Local Topology to RC.");
                    Logs.ShowLog(LogType.RC, "Received Local Topology from LRM.");
                    Logs.ShowLog(LogType.RC, "Sending Network Topology to Domain RC.");
                }
                else if (package.Message == "CONNECTION-DELETED")
                {
                    Logs.ShowLog(LogType.INFO, $"Deleted connection between {package.DestName} and {package.SourceName}");
                    Logs.ShowLog(LogType.LRM, "Sending Local Topology to RC.");
                    Logs.ShowLog(LogType.RC, "Received Local Topology from LRM.");
                    Logs.ShowLog(LogType.RC, "Sending Network Topology to Domain RC.");
                }
                else
                {
                    Logs.ShowLog(LogType.ERROR, "Unknown message from Domain received: " + message);
                }
            }

        }

        private void StartKombajn(string inSub, string outSub, List<int> slots, List<string> nodes)
        {
            ConnectionController CC = new ConnectionController();
            //Logs.ShowLog(LogType.CC, "Sending Route Table Query to RC...");
            //Logs.ShowLog(LogType.RC, "Sending Route Table Query Response to CC...");
            CC.SendRouteTableQuery(inSub, outSub, slots, nodes);
            
            Logs.ShowLog(LogType.RC, "Sending Connection Request to Nodes...");
            Logs.ShowLog(LogType.CC, "Sending Connection Response to Domain CC...");
        }

        public string SerializeToJson(Package package)
        {
            string jsonString;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            jsonString = JsonSerializer.Serialize(package, options);

            return jsonString;
        }

        public Package DeserializeFromJson(string serializedString)
        {
            Package package = JsonSerializer.Deserialize<Package>(serializedString);
            return package;
        }
    }
}
