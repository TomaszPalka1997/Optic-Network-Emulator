using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ASON;
using DataStructures;

namespace SubnetworkController
{
    class RoutingController
    {

        string workingDirectory = Environment.CurrentDirectory;
        string path;
        public Dictionary<string, int> nodes = new Dictionary<string, int>();

        public RoutingController() { path = Path.Combine(workingDirectory, @"DataStructures", "RoutingController.properties"); }

        public List<string> ReceiveRouteTableQuery(List<string> nodes)
        {
            SendRouteTableQueryResponse();

            return nodes;
        }

        private void SendRouteTableQueryResponse()
        {
            Logs.ShowLog(LogType.RC, "Sending Route Table Query Response to CC...");
        }

        public List<string> RouteTableQuerySubnetwork(List<string> subnetwork, string srcPort, string destPort)
        {
            Graph graph = new Graph();
            foreach (var row in subnetwork)
            {
                graph.AddNode(row);
            }
            LoadTableFromFile(path, graph);
            var calculator = new DistanceCalculator();
            var distances = calculator.CalculateDistances(graph, srcPort, destPort);

            var nodes = calculator.nodes;
            //Logs.ShowLog(LogType.RC, "Sending Route Table Query Respone to Subnetwork CC...");
            Logs.ShowLog(LogType.CC, "Received Connection Response from Subnetwork CC...");
            return nodes;
        }

        public void LoadTableFromFile(string configFilePath, Graph graph)
        {

            foreach (var row in File.ReadAllLines(configFilePath))
            {

                var splitRow = row.Split(", ");
                if (splitRow[0] == "#X" || splitRow.Length > 3)
                {
                    continue;
                }


                int value = int.Parse(splitRow[2]);
                var key = splitRow[0] + ", " + splitRow[1];
                foreach (var node in graph.Nodes.Keys)
                {
                    foreach (var node2 in graph.Nodes.Keys)
                    {
                        if (node == splitRow[0] && node2 == splitRow[1])
                        {
                            graph.AddConnection(splitRow[0], splitRow[1], int.Parse(splitRow[2]), true);
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                }
                if (!nodes.Any())
                {
                    nodes.Add(key, value);
                }


            }
        }

    }
}
