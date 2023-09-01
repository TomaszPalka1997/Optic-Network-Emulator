using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DataStructures;

namespace Switch
{
    class Switch
    {
        private Socket managementSystemSocket;
        private Socket cableCloudSocket;
        private Socket asonSocket;

        public string switchName;
        private IPAddress cloudAddress;
        private int cloudPort;
        private IPAddress managementSystemAddress;
        private int managementSystemPort;
        private IPAddress asonAddress;
        private int asonPort;

        private RoutingTable routingTable;
        private RoutingTableEntry routingTableEntry;

        public Switch(string switchConfigFilePath)
        {
            LoadPropertiesFromFile(switchConfigFilePath);
            Console.Title = $"{switchName}";
            routingTable = new RoutingTable();
        }

        public void Start()
        {
            Task.Run(action: () => ConnectToASON());
            Task.Run(action: () => ConnectToManagementSystem());
            ConnectToCloud();
        }

        private void ConnectToCloud()
        {
            while (true)
            {
                Logs.ShowLog(LogType.INFO, "Connecting to cable cloud...");
                cableCloudSocket = new Socket(cloudAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    cableCloudSocket.Connect(new IPEndPoint(cloudAddress, cloudPort));

                }
                catch (Exception)
                {
                    Logs.ShowLog(LogType.ERROR, "Couldn't connect to cable cloud.");
                    Logs.ShowLog(LogType.INFO, "Retrying...");
                    Thread.Sleep(5000);
                    continue;
                }

                try
                {
                    Logs.ShowLog(LogType.INFO, "Sending CONNECTED to cable cloud...");
                    string connectedMessage = "CONNECTED";
                    Package connectedCheckPackage = new Package(switchName, cloudAddress.ToString(), cloudPort, connectedMessage);
                    cableCloudSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(connectedCheckPackage)));
                    while (true)
                    {
                        HandleMessageFromCloud();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Logs.ShowLog(LogType.INFO, "Connection to cable cloud lost.");
                }
            }
        }

        private void ConnectToManagementSystem()
        {
            while (true)
            {
                Logs.ShowLog(LogType.INFO, "Connecting to management system...");
                managementSystemSocket = new Socket(managementSystemAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    managementSystemSocket.Connect(new IPEndPoint(managementSystemAddress, managementSystemPort));
                   
                }
                catch (Exception)
                {
                    Logs.ShowLog(LogType.ERROR, "Couldn't connect to management system.");
                    Logs.ShowLog(LogType.INFO, "Retrying...");
                    Thread.Sleep(5000);
                    continue;
                }

                try
                {
                    Logs.ShowLog(LogType.INFO, "Sending CONNECTED to management system...");
                    string connectedMessage = "CONNECTED";
                    Package connectedCheckPackage = new Package(switchName, managementSystemAddress.ToString(), managementSystemPort, connectedMessage);
                    managementSystemSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(connectedCheckPackage)));
                    while (true)
                    {
                        HandleResponseFromMS();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Logs.ShowLog(LogType.INFO, "Connection to management system lost.");
                }
            }
        }

        private void ConnectToASON()
        {
            while (true)
            {
                Logs.ShowLog(LogType.INFO, "Connecting to Subnetwork Controller...");
                asonSocket = new Socket(asonAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    asonSocket.Connect(new IPEndPoint(asonAddress, asonPort));

                }
                catch (Exception)
                {
                    Logs.ShowLog(LogType.ERROR, "Couldn't connect to Subnetwork Controller.");
                    Logs.ShowLog(LogType.INFO, "Retrying...");
                    Thread.Sleep(5000);
                    continue;
                }

                try
                {
                    Logs.ShowLog(LogType.INFO, "Sending CONNECTED to Subnetwork Controller...");
                    string connectedMessage = "CONNECTED";
                    Package connectedCheckPackage = new Package(switchName, asonAddress.ToString(), asonPort, connectedMessage);
                    asonSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(connectedCheckPackage)));
                    while (true)
                    {
                        HandleMessageFromASON();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Logs.ShowLog(LogType.INFO, "Connection to Subnetwork Controller.");
                }
            }
        }

        private void HandleMessageFromASON()
        {
            Package receivedPackage = ReceiveMessageFrom(asonSocket);
            //Console.WriteLine(SerializeToJson(receivedPackage));

            if (receivedPackage.Message == "CONNECTED")
            {
                Logs.ShowLog(LogType.CONNECTED, "Connected to Subnetwork Controller.");
            }
            else if (receivedPackage.Message == "ADD-ENTRY")
            {
                Logs.ShowLog(LogType.INFO, "Received routing table entry.");
                routingTableEntry = new RoutingTableEntry(receivedPackage.IncomingPort, receivedPackage.Slots);
                routingTable.Entries.Add(routingTableEntry);
                //foreach(var row in routingTable.Entries)
                //{
                //    Console.WriteLine(row.OutPort);
                //}
                //foreach(var row in routingTableEntry.Slots)
                //{
                //    Console.WriteLine("Route table slots: " + row);
                //}
                //Console.WriteLine(receivedPackage.IncomingPort);
            }
            else if(receivedPackage.Message == "DELETE-ENTRY")
            {
                foreach(var slotsToDelete in receivedPackage.Slots)
                {
                    if (routingTableEntry.Slots.Contains(slotsToDelete))
                    {
                        //Console.WriteLine("Deleting slot: " + slotsToDelete);
                        routingTableEntry.Slots.Remove(slotsToDelete);
                        Logs.ShowLog(LogType.INFO, "Slots have been removed");

                    }
                    routingTableEntry.OutPort = 0;
                    //Console.WriteLine(routingTableEntry.Slots.Count());

                    //foreach(var row in routingTable.Entries.)
                    //{
                    //    Console.WriteLine(row);
                    //}
                    //routingTable.Entries[1].Slots.Remove(slotsToDelete);
                    //foreach (var entries in routingTable.Entries)
                    //{
                    //    Console.WriteLine(receivedPackage.IncomingPort);

                    //    foreach(var slots in entries.Slots)
                    //    {   

                    //        if (slotsToDelete == slots)
                    //        {
                    //            //entries.Slots.RemoveAt(0);
                    //        }
                            
                    //    }

                    //}
                }
                //foreach (var entries in routingTable.Entries)
                //{
                //    foreach (var slots in entries.Slots)
                //    {
                //        Console.WriteLine("Usunięte sloty: " + slots);
                //    }
                //}
            }
            else
            {
                try
                {
                  //code is going to be there ASAP
                }
                catch (Exception)
                {
                   
                }
            }
        }
        private void HandleMessageFromCloud()
        {
            Package receivedPackage = ReceiveMessageFrom(cableCloudSocket);

            if (receivedPackage.Message == "CONNECTED")
            {
                Logs.ShowLog(LogType.CONNECTED, "Connected to cable cloud.");
            }
            else if (receivedPackage.Message == "CONNECTION-DELETED")
            {
                //Console.WriteLine("received: " + receivedPackage.Message);
                //Console.WriteLine("deleted port: " + receivedPackage.DestPort);
                asonSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(receivedPackage)));
                //receivedPackage.Message = "CALL-TEARDOWN";
                //asonSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(receivedPackage)));
            }
            else
            {
                try
                {
                    Logs.ShowLog(LogType.INFO, "Received a package from cable cloud:");
                    Console.WriteLine(SerializeToJson(receivedPackage));
                    Route(receivedPackage);
                    //SendPackageToCloud(receivedPackage);
                    Logs.ShowLog(LogType.INFO, "Sent routed package to cable cloud.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    //Logs.ShowLog(LogType.ERROR, "Couldn't perform routing.");
                }
            }

        }

        private void HandleResponseFromMS()
        {
            Package receivedPackage = ReceiveMessageFrom(managementSystemSocket);

            if (receivedPackage.Message == "CONNECTED")
            {
                Logs.ShowLog(LogType.CONNECTED, "Connected to management system.");
            }
            else if (receivedPackage.Message == "SENDING-TABLES")
            {
                Logs.ShowLog(LogType.INFO, "Received tables from MS.");

            }
            else if (receivedPackage.Message == "REQUEST-TABLE")
            {
                Logs.ShowLog(LogType.INFO, "Received table request from MS.");
                SendTableToMS();
            }
            else if (receivedPackage.Message == "WEIGHT-CHANGED")
            {
                asonSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(receivedPackage)));
            }
            else
            {
                Logs.ShowLog(LogType.ERROR, "Received unknown command from MS.");
            }
        }

        private Package ReceiveMessageFrom(Socket socket)
        {
            byte[] buffer = new byte[5120];
            int bytes = socket.Receive(buffer);
            var message = Encoding.ASCII.GetString(buffer, 0, bytes);
            Package receivedPackage = DeserializeFromJson(message);
            return receivedPackage;
        }

        private void LoadPropertiesFromFile(string configFilePath)
        {
            var properties = new Dictionary<string, string>();
            foreach (var row in File.ReadAllLines(configFilePath))
            {
                properties.Add(row.Split('=')[0], row.Split('=')[1]);
            }
            switchName = properties["SWITCHNAME"];
            managementSystemAddress = IPAddress.Parse(properties["MANAGEMENTSYSTEMADDRESS"]);
            cloudAddress = IPAddress.Parse(properties["CLOUDADDRESS"]);
            managementSystemPort = int.Parse(properties["MANAGEMENTSYSTEMPORT"]);
            cloudPort = int.Parse(properties["CLOUDPORT"]);
            asonPort = int.Parse(properties["ASONPORT"]);
            asonAddress = IPAddress.Parse(properties["ASONADDRESS"]);
        }

       
        private string SerializeToJson(Package package)
        {
            string jsonString;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            jsonString = JsonSerializer.Serialize(package, options);

            return jsonString;
        }

        private Package DeserializeFromJson(string serializedString)
        {

            Package package = JsonSerializer.Deserialize<Package>(serializedString);
            return package;

        }


        public void Route(Package package)
        {
            foreach(var row in routingTable.Entries)
            {
                //Console.WriteLine(row);
                if (!row.Slots.SequenceEqual(package.Slots))
                {
                    //Console.WriteLine("Flaga");
                    continue;
                }
                else
                {
                    //Console.WriteLine("OutPort: " + routingTableEntry.OutPort);
                    package.IncomingPort = row.OutPort;
                    SendPackageToCloud(package);
                    break;
                }

            }
        }

            private void SendPackageToCloud(Package package)
        {
            try
            {
                cableCloudSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(package)));
            }
            catch (Exception)
            {
                Logs.ShowLog(LogType.ERROR, "Couldn't send package to cable cloud.");
            }
        }

        private void SendTableToMS()
        {   
            
            try
            {
                //foreach (var row in routingTable.Entries)
                //{
                //    Console.WriteLine(row.InPort);
                //    Console.WriteLine(row.OutPort);
                //    foreach (var row2 in row.Slots)
                //    {
                //        Console.WriteLine(row2);
                //    }

                //}
                Package tablePackage = new Package();
                //tablePackage.RoutingTable = routingTable;
                string jsonString;
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };

                jsonString = JsonSerializer.Serialize(routingTable.Entries, options);
                //Console.WriteLine(jsonString);
                tablePackage.RoutingTable = jsonString;
                tablePackage.Message = "SENDING-TABLES";
                string json = SerializeToJson(tablePackage);
               
                managementSystemSocket.Send(Encoding.ASCII.GetBytes(json));
            }
            catch (Exception)
            {
                Logs.ShowLog(LogType.ERROR, "Couldn't send package to MS.");
            }
        }
    }
}