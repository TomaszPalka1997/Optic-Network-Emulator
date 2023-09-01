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
using DataStructures;

namespace ASON
{
    //OBIEKTY ZROBIĆ

    class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 2048;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }

    class Connection
    {

        public string destinationObject;
        public string incomingObject;
        public int weight;
        public Connection(string destinationObject, string incomingObject, int weight)
        {

            this.destinationObject = destinationObject;
            this.incomingObject = incomingObject;
            this.weight = weight;
        }

    }

    class Program
    {
        private Socket msSocket;
        private int port = 4999;
        private IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        private ManualResetEvent allDone = new ManualResetEvent(false);
        private Dictionary<string, Socket> connectedSockets = new Dictionary<string, Socket>();
        StartAsonCompilation cpcc;
        private bool deleting = true;

        int i = 0;
        static void Main(string[] args)
        {

            Program programASON = new Program();
        }

        public Program()
        {
            cpcc = new StartAsonCompilation();
            Console.Title = "Domain";
            //var t = Task.Run(action: () => ListenForConnections());
            //Thread.Sleep(1000);
            ListenForConnections();
        }
        private void ListenForConnections()
        {
            Logs.ShowLog(LogType.INFO, "Domain active.");
            Logs.ShowLog(LogType.LRM, "Sending Local Topology to Domain RC.");
            Logs.ShowLog(LogType.RC, "Received Local Topology from Domain LRM.");

            msSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                msSocket.Bind(new IPEndPoint(ipAddress, port));
                msSocket.Listen(100);
                while (true)
                {
                    allDone.Reset();
                    msSocket.BeginAccept(new AsyncCallback(AcceptCallback), msSocket);
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject
            {
                workSocket = handler
            };
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = null;
            handler = state.workSocket;
            Package receivedPackage = new Package();

            try
            {
                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);

                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                var content = state.sb.ToString();
                
                receivedPackage = DeserializeFromJson(content);
                //Console.WriteLine("Received message: " + receivedPackage.Message);
                if (receivedPackage.Message == "CONNECTED")
                {
                    // Send response message.
                    //Logs.ShowLog(LogType.CONNECTED, $"Connection with {receivedPackage.SourceName} established.");
                    Thread.Sleep(100);
                    try
                    {
                        connectedSockets.Add(receivedPackage.SourceName, handler);
                    }
                    catch (Exception)
                    {
                        //Logs.ShowLog(LogType.CONNECTED, $"Switch {receivedPackage.SourceName} reconnected.");
                    }
                    SendResponse(handler, content);
                    //SendTables(receivedPackage.SourceName);
                }
                else if(receivedPackage.Message == "CONNECTED-SUB")
                {
                    //Thread.Sleep(1765);
                    Logs.ShowLog(LogType.RC, $"Received Network Topology from {receivedPackage.SourceName} RC.");
                    //Thread.Sleep(1746);
                    
                    // Send response message.
                    //Logs.ShowLog(LogType.CONNECTED, $"Connection with {receivedPackage.SourceName} established.");
                    Thread.Sleep(100);
                    try
                    {
                        connectedSockets.Add(receivedPackage.SourceName, handler);
                    }
                    catch (Exception)
                    {
                        //Logs.ShowLog(LogType.CONNECTED, $"Switch {receivedPackage.SourceName} reconnected.");
                    }
                    SendResponse(handler, content);
                    //SendTables(receivedPackage.SourceName);
                }
                else if (receivedPackage.Message == "CALL-CPCC")
                {
                    Logs.ShowLog(LogType.NCC, $"Received Call Request CPCC from {receivedPackage.SourceName}.");
                    Thread.Sleep(100);
                    Logs.ShowLog(LogType.NCC, $"Sending Call Accept Request to CPCC {receivedPackage.DestName}.");
                    receivedPackage.Message = "CALL-ACCEPT";
                    var package =  SerializeToJson(receivedPackage);
                    SendResponse(connectedSockets[receivedPackage.DestName], package);
                    
                }
                else if (receivedPackage.Message == "CONNECTION-ACCEPTED")
                {
                    Logs.ShowLog(LogType.NCC, "Received Call Accept Response from CPCC...");
                    Logs.ShowLog(LogType.NCC, $"Received CONNECTION-ACCEPTED from {receivedPackage.SourceName}.");
         
                    cpcc.NewConnection(receivedPackage.DestName, receivedPackage.SourceName, receivedPackage.Bandwidth, i);
                    
                    receivedPackage.Slots = cpcc.Ncc.CC.SlotsToCcSubnetwork;
                    var package = SerializeToJson(receivedPackage);

                    if (cpcc.Ncc.CC.AreBothSubnetworks)
                    {
                        receivedPackage.InOutsFromSubs = cpcc.Ncc.CC.InOuts;
                        receivedPackage.ShortestPath = cpcc.Ncc.CC.ShortestPathSub1;
                        var package1 = SerializeToJson(receivedPackage);
                        SendResponse(connectedSockets["Subnetwork1"], package1);
                        receivedPackage.ShortestPath = cpcc.Ncc.CC.ShortestPathSub2;
                        var package2 = SerializeToJson(receivedPackage);
                        SendResponse(connectedSockets["Subnetwork2"], package2);
                    }
                    else if (cpcc.Ncc.CC.IsSubnetwork1)
                    {
                        receivedPackage.InOutsFromSubs = cpcc.Ncc.CC.InOuts;
                        receivedPackage.ShortestPath = cpcc.Ncc.CC.ShortestPathSub1;
                        var package1 = SerializeToJson(receivedPackage);
                        SendResponse(connectedSockets["Subnetwork1"], package1);
                    }
                    else if (cpcc.Ncc.CC.IsSubnetwork2)
                    {
                        receivedPackage.InOutsFromSubs = cpcc.Ncc.CC.InOuts;
                        receivedPackage.ShortestPath = cpcc.Ncc.CC.ShortestPathSub2;
                        var package2 = SerializeToJson(receivedPackage);
                        SendResponse(connectedSockets["Subnetwork2"], package2);
                    }
                    SendShortestPath(receivedPackage.SourceName, receivedPackage.DestName, cpcc.Ncc.CC);
                    SendResponse(connectedSockets[receivedPackage.DestName], package);
                    i++;
                }
                else if (receivedPackage.Message == "CONNECTION-REJECTED")
                {
                    Logs.ShowLog(LogType.INFO, $"Received CONNECTION-REJECTED from {receivedPackage.SourceName}.");
                    var package = SerializeToJson(receivedPackage);
                    SendResponse(connectedSockets[receivedPackage.DestName], package);
                }
                else if (receivedPackage.Message == "CONNECTION-DELETED")
                {

                    if (deleting)
                        DeleteConnection(receivedPackage.SourceName, receivedPackage.DestName);

                    

                    //SendResponse(connectedSockets[receivedPackage.DestName], SerializeToJson(receivedPackage));
                    //SendResponse(connectedSockets[receivedPackage.SourceName], SerializeToJson(receivedPackage));

                }
                else if (receivedPackage.Message == "SENDING-TABLES")
                {
                    Console.WriteLine(receivedPackage.RoutingTable);

                }
                else if (receivedPackage.Message == "WEIGHT-CHANGED")
                {
                    string subNetworkName;
                    if (receivedPackage.SourceName == "S1" || receivedPackage.SourceName == "S2" || receivedPackage.SourceName == "S3" || receivedPackage.SourceName == "S4")
                    {
                        subNetworkName = "Subnetwork1";
                    }
                    else
                    {
                        subNetworkName = "Subnetwork2";
                    }

                    connectedSockets[subNetworkName].Send(Encoding.ASCII.GetBytes(SerializeToJson(receivedPackage)));
                    Thread.Sleep(500);
                    Logs.ShowLog(LogType.RC, $"Received Network Topology from {subNetworkName} RC.");
                }
                else if(receivedPackage.Message == "CALL-TEARDOWN")
                {
                   
                    if (receivedPackage.DestName == "190.98.50.1")
                    {
                        receivedPackage.Message = "CALL-TEARDOWN";
                        receivedPackage.DestName = "H1";
                        if (receivedPackage.SourceName == "H3" || receivedPackage.SourceName == "H4")
                            cpcc.Ncc.CC.AreBothSubnetworks = true;
                        else
                            cpcc.Ncc.CC.IsSubnetwork1 = true;
                        var package = SerializeToJson(receivedPackage);
                        ClearPath(receivedPackage.SourceName, receivedPackage.DestName, cpcc.Ncc.CC);
                        cpcc.SendCallTeardown(receivedPackage.SourceName, receivedPackage.DestName);
                        SendResponse(connectedSockets[receivedPackage.DestName], package);
                        receivedPackage.Message = "CONNECTION-CLOSED";
                        var package2 = SerializeToJson(receivedPackage);
                        SendResponse(connectedSockets[receivedPackage.SourceName], package2);

                    }
                    else if (receivedPackage.DestName == "190.98.50.2")
                    {
                        receivedPackage.Message = "CALL-TEARDOWN";
                        receivedPackage.DestName = "H2";
                        if (receivedPackage.SourceName == "H3" || receivedPackage.SourceName == "H4")
                            cpcc.Ncc.CC.AreBothSubnetworks = true;
                        else
                            cpcc.Ncc.CC.IsSubnetwork1 = true;
                        var package = SerializeToJson(receivedPackage);
                        ClearPath(receivedPackage.SourceName, receivedPackage.DestName, cpcc.Ncc.CC);
                        cpcc.SendCallTeardown(receivedPackage.SourceName, receivedPackage.DestName);
                        SendResponse(connectedSockets[receivedPackage.DestName], package);
                        receivedPackage.Message = "CONNECTION-CLOSED";
                        var package2 = SerializeToJson(receivedPackage);
                        SendResponse(connectedSockets[receivedPackage.SourceName], package2);
                    }
                    else if (receivedPackage.DestName == "155.87.30.3")
                    {
                        receivedPackage.Message = "CALL-TEARDOWN";
                        receivedPackage.DestName = "H3";
                        if (receivedPackage.SourceName == "H1" || receivedPackage.SourceName == "H2")
                            cpcc.Ncc.CC.AreBothSubnetworks = true;
                        else
                            cpcc.Ncc.CC.IsSubnetwork2 = true;
                        var package = SerializeToJson(receivedPackage);
                        ClearPath(receivedPackage.SourceName, receivedPackage.DestName, cpcc.Ncc.CC);

                        cpcc.SendCallTeardown(receivedPackage.SourceName, receivedPackage.DestName);

                        SendResponse(connectedSockets[receivedPackage.DestName], package);
                        receivedPackage.Message = "CONNECTION-CLOSED";
                        var package2 = SerializeToJson(receivedPackage);
                        SendResponse(connectedSockets[receivedPackage.SourceName], package2);
                    }
                    else if (receivedPackage.DestName == "155.87.30.4")
                    {
                        receivedPackage.Message = "CALL-TEARDOWN";
                        receivedPackage.DestName = "H4";
                        if (receivedPackage.SourceName == "H1" || receivedPackage.SourceName == "H2")
                            cpcc.Ncc.CC.AreBothSubnetworks = true;
                        else
                            cpcc.Ncc.CC.IsSubnetwork2 = true;
                        var package = SerializeToJson(receivedPackage);
                        ClearPath(receivedPackage.SourceName, receivedPackage.DestName, cpcc.Ncc.CC);
                        cpcc.SendCallTeardown(receivedPackage.SourceName, receivedPackage.DestName);
                        SendResponse(connectedSockets[receivedPackage.DestName], package);
                        receivedPackage.Message = "CONNECTION-CLOSED";
                        var package2 = SerializeToJson(receivedPackage);
                        SendResponse(connectedSockets[receivedPackage.SourceName], package2);

                    }
                    else
                    {
                        Logs.ShowLog(LogType.NCC, "There is no such host connected...");
                    }
                }                
                else
                {
                    Logs.ShowLog(LogType.ERROR, $"Unknown message received: {content}");
                }
                state.sb.Clear();
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                var myKey = connectedSockets.FirstOrDefault(x => x.Value == handler).Key;
                Console.WriteLine(e.ToString());
                Logs.ShowLog(LogType.ERROR, $"Connection with {myKey} lost.");
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
        }

        private void DeleteConnection(string SourceName, string DestName)
        {
            deleting = false;
            var properties = new List<Connection>();

            string workingDirectory = Environment.CurrentDirectory;
            String configFilePath = Path.Combine(workingDirectory, @"DataStructures", "RoutingController.properties");
            String configFilePath2 = Path.Combine(workingDirectory, @"DataStructures", "RoutingControllerRemoved.properties");

            //Dictionary<int, Tunnel> tunnels = new Dictionary<int, Tunnel>();

            foreach (var row in File.ReadAllLines(configFilePath))
            {

                var splitRow = row.Split(", ");
                if (splitRow[0] == "#X")
                {
                    continue;
                }
                properties.Add(new Connection(splitRow[1], splitRow[0], int.Parse(splitRow[2])));
            }

            for (int i = 0; i < properties.Count; i++)
            {
                if ((properties[i].incomingObject == SourceName && properties[i].destinationObject == DestName) || (properties[i].incomingObject == DestName && properties[i].destinationObject == SourceName))
                {
                    //Console.WriteLine($"Deleted {properties[i].incomingObject} {properties[i].destinationObject} {properties[i].weight}");
                    //Logs.ShowLog(LogType.INFO, $"Deleted {properties[i].incomingObject} {properties[i].destinationObject} {properties[i].weight}");
                    string subNetworkName;
                    if (SourceName == "S1" || SourceName == "S2" || SourceName == "S3" || SourceName == "S4")
                    {
                        subNetworkName = "Subnetwork1";
                    }
                    else
                    {
                        subNetworkName = "Subnetwork2";
                    }
                    Package package = new Package();
                    package.Message = "CONNECTION-DELETED";
                    package.SourceName = SourceName;
                    package.DestName = DestName;
                    connectedSockets[subNetworkName].Send(Encoding.ASCII.GetBytes(SerializeToJson(package)));
                    Thread.Sleep(500);
                    Logs.ShowLog(LogType.RC, $"Received Network Topology from {subNetworkName} RC.");

                    properties.RemoveAt(i);
                }
            }

            using (StreamWriter outputFile = new StreamWriter(configFilePath))
            {
                outputFile.WriteLine("#X, Y, Z - węzeł A, węzeł B, waga");
                for (int i = 0; i < properties.Count; i++)
                {
                    String conne = $"{properties[i].incomingObject}, {properties[i].destinationObject}, {properties[i].weight}";
                    outputFile.WriteLine(conne);
                }
            }
            deleting = true;
        }


        private void SendResponse(Socket handler, string data)
        {
            byte[] responseMessage = Encoding.ASCII.GetBytes(data);
            handler.Send(responseMessage);
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

        private void SendShortestPath(string srcName, string destName, ConnectionController CC)
        {
            List<int> slots = CC.SlotsToCcSubnetwork;
            Package package = new Package();
            package.Slots = slots;
            package.Message = "ADD-ENTRY";
            if (CC.AreBothSubnetworks)
            {
                foreach (var switchName in CC.ShortestPathSub1)
                {
                    Socket handler = connectedSockets[switchName];
                    
                    if (CC.ShortestPathSub1.IndexOf(switchName) + 1 < CC.ShortestPathSub1.Count())
                    {
                        int outPort = GetOutPort(switchName, CC.ShortestPathSub1[CC.ShortestPathSub1.IndexOf(switchName) + 1]);
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                        continue;
                    }
                    if (switchName == "S2")
                    {
                        int outPort = GetOutPort(switchName, "S5");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                        
                    }
                    else if (switchName == "S4")
                    {
                        int outPort = GetOutPort(switchName, "H2");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                        
                    }
                    else if (switchName == "S1")
                    {
                        int outPort = GetOutPort(switchName, "H1");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                }

                foreach (var switchName in CC.ShortestPathSub2)
                {
                    Socket handler = connectedSockets[switchName];
                    if (CC.ShortestPathSub2.IndexOf(switchName) + 1 < CC.ShortestPathSub2.Count())
                    {
                        int outPort = GetOutPort(switchName, CC.ShortestPathSub2[CC.ShortestPathSub2.IndexOf(switchName) + 1]);
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                        
                        continue;
                    }
                    if (switchName == "S5")
                    {
                        int outPort = GetOutPort(switchName, "S2");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                    else if (switchName == "S7")
                    {
                        int outPort = GetOutPort(switchName, "H3");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                    else if (switchName == "S6")
                    {
                        int outPort = GetOutPort(switchName, "H4");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                }
                CC.AreBothSubnetworks = false;
            }
            else if (CC.IsSubnetwork1)
            {
                foreach (var switchName in CC.ShortestPathSub1)
                {
                    Socket handler = connectedSockets[switchName];
                    if (CC.ShortestPathSub1.IndexOf(switchName) + 1 < CC.ShortestPathSub1.Count())
                    {
                        int outPort = GetOutPort(switchName, CC.ShortestPathSub1[CC.ShortestPathSub1.IndexOf(switchName) + 1]);
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                        continue;
                    }
                    if (switchName == "S4")
                    {
                        int outPort = GetOutPort(switchName, "H2");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                    else if (switchName == "S1")
                    {
                        int outPort = GetOutPort(switchName, "H1");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                }
                package.ShortestPath = CC.ShortestPathSub1;
                CC.IsSubnetwork1 = false;
                
            }
            else if (CC.IsSubnetwork2)
            {
                foreach (var switchName in CC.ShortestPathSub2)
                {
                    Socket handler = connectedSockets[switchName];
                    if (CC.ShortestPathSub2.IndexOf(switchName) + 1 < CC.ShortestPathSub2.Count())
                    {
                        int outPort = GetOutPort(switchName, CC.ShortestPathSub2[CC.ShortestPathSub2.IndexOf(switchName) + 1]);
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                        
                        continue;
                    }
                    if (switchName == "S7")
                    {
                        int outPort = GetOutPort(switchName, "H3");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                        
                    }
                    else if (switchName == "S6")
                    {
                        int outPort = GetOutPort(switchName, "H4");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                }
                CC.IsSubnetwork2 = false;
            }
            else
            {
                Logs.ShowLog(LogType.ERROR, "All subnetwork flags are false.");
            }
            Logs.ShowLog(LogType.CC, "Sending Connection Response to NCC...");
            Logs.ShowLog(LogType.NCC, "Sending Call Response to CPCC...");
        } 

        private void ClearPath(string srcName, string destName, ConnectionController CC)
        {
            List<int> slots = CC.SlotsToCcSubnetwork;
            Package package = new Package();
            package.Slots = slots;
            package.Message = "DELETE-ENTRY";
            //string packageToNodes = SerializeToJson(package);
            Logs.ShowLog(LogType.RC, "Sending Connection Request to Nodes...");
            if (CC.AreBothSubnetworks)
            {
                foreach (var switchName in CC.ShortestPathSub1)
                {

                    Socket handler = connectedSockets[switchName];
                    
                    if (CC.ShortestPathSub1.IndexOf(switchName) + 1 < CC.ShortestPathSub1.Count())
                    {
                        int outPort = GetOutPort(switchName, CC.ShortestPathSub1[CC.ShortestPathSub1.IndexOf(switchName) + 1]);
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                        continue;
                    }
                    if (switchName == "S2")
                    {
                        int outPort = GetOutPort(switchName, "S5");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                    else if (switchName == "S4")
                    {
                        int outPort = GetOutPort(switchName, "H2");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                    else if (switchName == "S1")
                    {
                        int outPort = GetOutPort(switchName, "H1");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                }

                foreach (var switchName in CC.ShortestPathSub2)
                {
                    Socket handler = connectedSockets[switchName];
                    if (CC.ShortestPathSub2.IndexOf(switchName) + 1 < CC.ShortestPathSub2.Count())
                    {
                        int outPort = GetOutPort(switchName, CC.ShortestPathSub2[CC.ShortestPathSub2.IndexOf(switchName) + 1]);
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                        continue;
                    }
                    if (switchName == "S5")
                    {
                        int outPort = GetOutPort(switchName, "S2");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                    else if (switchName == "S7")
                    {
                        int outPort = GetOutPort(switchName, "H3");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                    else if (switchName == "S6")
                    {
                        int outPort = GetOutPort(switchName, "H4");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        SendResponse(handler, packageToNodes);
                    }
                }
                Socket srcHost = connectedSockets[destName];
                //SendResponse(srcHost, );
            }
            else if (CC.IsSubnetwork1)
            {
                foreach (var switchName in CC.ShortestPathSub1)
                {
                    Socket handler = connectedSockets[switchName];
                    if (CC.ShortestPathSub1.IndexOf(switchName) + 1 < CC.ShortestPathSub1.Count())
                    {
                        int outPort = GetOutPort(switchName, CC.ShortestPathSub1[CC.ShortestPathSub1.IndexOf(switchName) + 1]);
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        //Console.WriteLine(packageToNodes);
                        SendResponse(handler, packageToNodes);
                        continue;
                    }
                    if (switchName == "S4")
                    {
                        int outPort = GetOutPort(switchName, "H2");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        //Console.WriteLine(packageToNodes);
                        SendResponse(handler, packageToNodes);
                    }
                    else if (switchName == "S1")
                    {
                        int outPort = GetOutPort(switchName, "H1");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        // Console.WriteLine(packageToNodes);
                        SendResponse(handler, packageToNodes);
                    }
                }
            }
            else if (CC.IsSubnetwork2)
            {
                foreach (var switchName in CC.ShortestPathSub2)
                {
                    Socket handler = connectedSockets[switchName];
                    if (CC.ShortestPathSub2.IndexOf(switchName) + 1 < CC.ShortestPathSub2.Count())
                    {
                        int outPort = GetOutPort(switchName, CC.ShortestPathSub2[CC.ShortestPathSub2.IndexOf(switchName) + 1]);
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        //Console.WriteLine(packageToNodes);
                        SendResponse(handler, packageToNodes);
                        continue;
                    }
                    if (switchName == "S7")
                    {
                        int outPort = GetOutPort(switchName, "H3");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        //Console.WriteLine(packageToNodes);
                        SendResponse(handler, packageToNodes);
                    }
                    else if (switchName == "S6")
                    {
                        int outPort = GetOutPort(switchName, "H4");
                        package.IncomingPort = outPort;
                        string packageToNodes = SerializeToJson(package);
                        //Console.WriteLine(packageToNodes);
                        SendResponse(handler, packageToNodes);
                    }
                }
            }
            else
            {
                Logs.ShowLog(LogType.ERROR, "All subnetwork flags are false.");
            }
            Logs.ShowLog(LogType.CC, "Sending Connection Response to Domain CC...");
            Logs.ShowLog(LogType.CC, "Sending Connection Response to NCC...");

        }
        private int GetOutPort(string first, string second)
        {
            int outPort = 0;
            string workingDirectory = Environment.CurrentDirectory;
            string path = Path.Combine(workingDirectory, @"DataStructures", "CableCloud.properties");
            
            foreach (var row in File.ReadAllLines(path))
            {
                var splitRow = row.Split(", ");
                if (splitRow[1] == first && splitRow[3] == second)
                {
                    outPort = Convert.ToInt32(splitRow[0]);
                    break;
                }
            }
            return outPort;
        }
    }
}
