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

namespace ManagementSystem
{
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

    class ManagementSystem
    {
        private Socket msSocket;
        private int port = 5000;
        private IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        private ManualResetEvent allDone = new ManualResetEvent(false);
        private Dictionary<string, Socket> connectedSockets = new Dictionary<string, Socket>();
       

       
        public ManagementSystem()
        {
            Console.Title = "Management System";
           
        }

        public void Start()
        {
            var t = Task.Run(action: () => ListenForConnections());
            Thread.Sleep(1000);
            HandleInput();
            Console.ReadLine();
        }

        private void HandleInput()
        {
            while (true)
            {
                if (connectedSockets.Count == 0)
                {
                    continue;
                }

                // Clear the keyboard buffer.
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(false);
                }

                Console.WriteLine("\nChoose which switch you want to manage: \n[1] S1 \n[2] S2 \n[3] S3 \n[4] S4 \n[5] S5 \n[6] S6 \n[7] S7 \n[8] if you want to add new connection or change weight of existing connection");
                var choice = Console.ReadLine();
                
                if (choice == "1")
                {
                    SendTableRequestTo("S1");
                    
                }
                else if (choice == "2")
                {
                    SendTableRequestTo("S2");
                }
                else if (choice == "3")
                {
                    SendTableRequestTo("S3");
                }
                else if (choice == "4")
                {
                    SendTableRequestTo("S4");
                }
                else if (choice == "5")
                {
                    SendTableRequestTo("S5");
                }
                else if (choice == "6")
                {
                    SendTableRequestTo("S6");
                }
                else if (choice == "7")
                {
                    SendTableRequestTo("S7");
                }
                else if (choice == "8")
                {
                    ChangeConnection();
                }
                else
                {
                    Console.WriteLine("\nNo such switch connected. Try again.");
                    continue;
                }
            }
        }

        private void ChangeConnection()
        {
            var properties = new List<Connection>();

            string workingDirectory = Environment.CurrentDirectory;
            String configFilePath = Path.Combine(workingDirectory, @"DataStructures", "RoutingController.properties");
            //String configFilePath2 = Path.Combine(workingDirectory, @"DataStructures", "RoutingControllerRemoved.properties");

            Console.WriteLine("Write first switch name");
            string swtch1 = Console.ReadLine();
            Console.WriteLine("Write second switch name");
            string swtch2 = Console.ReadLine();
            Console.WriteLine("Write weight of this connection");
            int weight = int.Parse(Console.ReadLine());
            


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
                if ((properties[i].incomingObject == swtch1 && properties[i].destinationObject == swtch2) || (properties[i].incomingObject == swtch2 && properties[i].destinationObject == swtch1))
                {
                    //Console.WriteLine($"Deleted connection: {properties[i].incomingObject} {properties[i].destinationObject} {properties[i].weight}");
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
                outputFile.WriteLine($"{swtch1}, {swtch2}, {weight}");
            }
            Package package = new Package();
            package.Message = "WEIGHT-CHANGED";
            package.SourceName = swtch1;
            package.DestName = swtch2;
            connectedSockets[swtch1].Send(Encoding.ASCII.GetBytes(SerializeToJson(package)));
        }

        private void SendTableRequestTo(string switchName)
       {
            Package package = new Package();
            package.Message = "REQUEST-TABLE";
            SendMessage(switchName, package);
       }


        private void ListenForConnections()
        {
            //Logs.ShowLog(LogType.INFO, "Awaiting connection...");
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
                if (receivedPackage.Message == "CONNECTED")
                {
                    // Send response message.
                    //Logs.ShowLog(LogType.CONNECTED, $"Connection with {receivedPackage.SourceName} established.");
                    try
                    {
                        connectedSockets.Add(receivedPackage.SourceName, handler);
                    }
                    catch(Exception)
                    {
                        //Logs.ShowLog(LogType.CONNECTED, $"Router {receivedPackage.SourceName} reconnected.");
                    }
                    SendResponse(handler, content);
                    
                }
                else if (receivedPackage.Message == "SENDING-TABLES")
                {
                    Console.WriteLine(receivedPackage.RoutingTable);

                }
                else
                {
                    //Logs.ShowLog(LogType.ERROR, $"Unknown message received: {content}");
                }
                state.sb.Clear();
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            catch (Exception)
            {
                var myKey = connectedSockets.FirstOrDefault(x => x.Value == handler).Key;
                //Logs.ShowLog(LogType.ERROR, $"Connection with {myKey} lost.");
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
        }

        private void SendResponse(Socket handler, string data)
        {
            byte[] responseMessage = Encoding.ASCII.GetBytes(data);
            var myKey = connectedSockets.FirstOrDefault(x => x.Value == handler).Key;
            Logs.ShowLog(LogType.INFO, $"Sending {responseMessage.Length} bytes to {myKey}.");
            handler.Send(responseMessage);
        }

        private void SendMessage(string switchName, Package package)
        {
            try
            {
                var handler = connectedSockets[switchName];
                string json = SerializeToJson(package);
                byte[] byteData = Encoding.ASCII.GetBytes(json);
                //Logs.ShowLog(LogType.INFO, $"Sending {byteData.Length} bytes to {switchName}.");
                Console.WriteLine(json);
                handler.Send(byteData);
            }
            catch (KeyNotFoundException)
            {
                //Logs.ShowLog(LogType.ERROR, $"Switch {switchName} is not connected.");
            }
            catch
            {
                Logs.ShowLog(LogType.ERROR, $"Couldn't send message.");
            }
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

        /*private void SendTables(string destination)
        {
            var tablesFile = new List<string>();
            switch (destination)
            {
                case "S1":
                    foreach (var row in File.ReadAllLines(S1TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package1 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("S1", package1);
                    break;
                case "S2":
                    foreach (var row in File.ReadAllLines(S2TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package2 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("S2", package2);
                    break;
                case "S3":
                    foreach (var row in File.ReadAllLines(S3TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package3 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("S3", package3);
                    break;
                case "S4":
                    foreach (var row in File.ReadAllLines(S4TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package4 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("S4", package4);
                    break;
                case "S5":
                    foreach (var row in File.ReadAllLines(S5TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package5 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("S5", package5);
                    break;
                case "S6":
                    foreach (var row in File.ReadAllLines(S6TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package6 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("S6", package6);
                    break;
                case "S7":
                    foreach (var row in File.ReadAllLines(S7TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package7 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("S7", package7);
                    break;

            }
        }*/
    }
}
