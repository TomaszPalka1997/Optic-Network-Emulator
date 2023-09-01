using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataStructures;
using System.Text;

namespace ASON
{
    public class RoutingController
    {
        public string SourceIp { get; set; }
        public string DestIp { get; set; }
        public long Bandwidth { get; set; }
        public Dictionary<string, string> RouteDictionary { get; set; }
        public Dictionary<string, int> DistanceDictionary { get; set; }
        public Dictionary<string, List<int>> SlotsDictionary { get; set; }

        public Dictionary<string, int> nodes = new Dictionary<string, int>();

        string workingDirectory = Environment.CurrentDirectory;

        string path;

        bool areBothSubnetworks = false;

        List<string> inOutList;

        Dictionary<string, List<int>> allocatedConnections;


        public RoutingController()
        {
            RouteDictionary = new Dictionary<string, string>();
            DistanceDictionary = new Dictionary<string, int>();
            SlotsDictionary = new Dictionary<string, List<int>>();

            LoadRouteDictionary();
            LoadDistanceDictionary();
            path = Path.Combine(workingDirectory, @"DataStructures", "RoutingController.properties");

        }

        private void LoadDistanceDictionary()
        {
            // H1-H2
            DistanceDictionary.Add("190.98.50.1, 190.98.50.2", 150);
            DistanceDictionary.Add("190.98.50.2, 190.98.50.1", 150);
            // H1-H3
            DistanceDictionary.Add("190.98.50.1, 155.87.30.3", 250);
            DistanceDictionary.Add("155.87.30.3, 190.98.50.1", 250);
            // H1-H4
            DistanceDictionary.Add("190.98.50.1, 155.87.30.4", 320);
            DistanceDictionary.Add("155.87.30.4, 190.98.50.1", 320);
            // H2-H3
            DistanceDictionary.Add("190.98.50.2, 155.87.30.3", 130);
            DistanceDictionary.Add("155.87.30.3, 190.98.50.2", 130);
            // H2-H4
            DistanceDictionary.Add("190.98.50.2, 155.87.30.4", 270);
            DistanceDictionary.Add("155.87.30.4, 190.98.50.2", 270);
            // H3-H4
            DistanceDictionary.Add("155.87.30.3, 155.87.30.4", 100);
            DistanceDictionary.Add("155.87.30.4, 155.87.30.3", 100);
        }

        private int GetModulationForDistance(int distance)
        {
            // zwraca potege dwojki dla danej modulacji, np. 32-QAM -> 2^5 -> zwroci 5
            if (distance > 0 && distance <= 100)
            {
                // 64-QAM
                return 6;
            }
            else if (distance > 100 && distance <= 200)
            {
                // 32-QAM
                return 5;
            }
            else if (distance > 200 && distance <= 300)
            {
                // 16-QAM
                return 4;
            }
            else if (distance > 300 && distance <= 400)
            {
                // 8-QAM
                return 3;
            }
            else if (distance > 400 && distance <= 500)
            {
                // 4-QAM
                return 2;
            }
            else if (distance > 500 && distance <= 600)
            {
                // BPSK
                return 1;
            }
            else
                // nieznany przypadek
                return -1;
        }

        private void LoadRouteDictionary()
        {
            string workingDirectory = Environment.CurrentDirectory;
            string configFilePath = Path.Combine(workingDirectory, @"DataStructures", "RC.properties");

            foreach (string row in File.ReadAllLines(configFilePath))
            {
                var splitRow = row.Split("; ");
                RouteDictionary.Add(splitRow[0], splitRow[1]);
            }
        }

        public void ClearConnectionResourcesRC(string sourceIP, string destIP)
        {
            SlotsDictionary.Remove(sourceIP + ", " + destIP);
            nodes.Clear();
            inOutList.Clear();
            allocatedConnections.Remove(sourceIP + ", " + destIP);
        }

        public (List<string>, Dictionary<string, List<int>>) ReceiveRouteTableQuery(string sourceIp, string destIp, long bandwidth)
        {
            SourceIp = sourceIp;
            DestIp = destIp;
            Bandwidth = bandwidth;
            if (SourceIp == "190.98.50.1" && DestIp == "155.87.30.3" || SourceIp == "155.87.30.3" && DestIp == "190.98.50.1" || SourceIp == "190.98.50.1" && DestIp == "155.87.30.4" 
                || SourceIp == "155.87.30.4" && DestIp == "190.98.50.1" || SourceIp == "190.98.50.2" && DestIp == "155.87.30.4" || SourceIp == "155.87.30.4" && DestIp == "190.98.50.2" 
                || SourceIp == "190.98.50.2" && DestIp == "155.87.30.3" || SourceIp == "155.87.30.3" && DestIp == "190.98.50.2")
            {
                areBothSubnetworks = true;
            }

            string key = SourceIp + ", " + DestIp;
            inOutList = FindEntryInRouteDictionary(key);
            allocatedConnections = CalculateSlots();
            string inOutToAppend = "";
            foreach (var row in inOutList)
            {
                if (row.Equals(inOutList.Last()))
                {
                    inOutToAppend += row;
                }
                else
                {
                    inOutToAppend += row + ", ";
                }

            }
            string allocatedToAppend = "";
            foreach (var row in allocatedConnections)
            {
                if (row.Key.Equals(allocatedConnections.Last().Key))
                {
                    foreach (var row2 in row.Value)
                    {
                        if (row2.Equals(row.Value.Last()))
                        {
                            allocatedToAppend += row2;
                        }
                        else
                        {
                            allocatedToAppend += row2 + ", ";
                        }
                    }
                }
                    
                

            }
            //Logs.ShowLog(LogType.RC, $"Sending Route Table Query Respone({inOutToAppend}; [{allocatedToAppend}]) to CC...");
            return (inOutList, allocatedConnections);
        }

        private List<string> FindEntryInRouteDictionary(string key)
        {
            List<string> inOut = new List<string>();
            var values = RouteDictionary[key].Split(", ");
            foreach (string value in values)
            {
                inOut.Add(value);
            }
            return inOut;
        }

        private Dictionary<string, List<int>> CalculateSlots()
        {
            string key = SourceIp + ", " + DestIp;
            int modulation = GetModulationForDistance(DistanceDictionary[key]);
            double band = Math.Ceiling((Convert.ToDouble(Bandwidth) / modulation) + 10);
            double slotsNumber = Math.Ceiling(band / 12500000000);
            int slots = Convert.ToInt32(slotsNumber);
            List<int> slotsList = new List<int>();
            if (!SlotsDictionary.Any())
            {
                for (int i = 0; i < slots; i++)
                {
                    slotsList.Add(i);
                }
                SlotsDictionary.Add(key, slotsList);
            }
            else
            {
                foreach(KeyValuePair<string, List<int>> entry in SlotsDictionary)
                {
                    if (entry.Key.Contains(SourceIp) || entry.Key.Contains(DestIp))
                    {
                        for (int i = entry.Value.Last(); i < slots + entry.Value.Last(); i++)
                        {
                            slotsList.Add(i+1);
                        }
                        SlotsDictionary.Add(key, slotsList);
                        break;
                    }
                    else if (areBothSubnetworks)
                    {
                        for (int i = entry.Value.Last(); i < slots + entry.Value.Last(); i++)
                        {
                            slotsList.Add(i + 1);
                        }
                        SlotsDictionary.Add(key, slotsList);
                        break;
                    }
                    else
                    {
                        for (int i = 0; i < slots; i++)
                        {
                            slotsList.Add(i);
                        }
                        SlotsDictionary.Add(key, slotsList);
                        break;
                    }
                }
            }
          
            return SlotsDictionary;
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
            var distances = calculator.CalculateDistances(graph, GetSwitchNameFor(srcPort), GetSwitchNameFor(destPort));
            
            var nodes = calculator.nodes;
            //Logs.ShowLog(LogType.RC, "Sending Route Table Query Respone to Subnetwork CC...");
            Logs.ShowLog(LogType.CC, "Received Connection Response from Subnetwork CC...");
            return nodes;
        }

        private string GetSwitchNameFor(string port)
        {
            if (port == "10")
                return "S1";
            else if (port == "40")
                return "S4";
            else if (port == "25")
                return "S2";
            else if (port == "52")
                return "S5";
            else if (port == "70")
                return "S7";
            else if (port == "60")
                return "S6";
            else
            {
                Logs.ShowLog(LogType.ERROR, "Wrong port.");
                return "log";
            }
                

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
                if(!nodes.Any())
                {
                    nodes.Add(key, value);
                }
                

            }
        }

    }
}
