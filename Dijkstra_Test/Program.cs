using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dijkstra
{
    public class Program
    {
        public Dictionary<string, int> nodes = new Dictionary<string, int>();
        string workingDirectory = Environment.CurrentDirectory;
       
        Graph graph = new Graph();
        public Program()
        {
            string path = Path.Combine(workingDirectory, @"DataStructures", "RoutingController.properties");
            //graph.AddNode("H1");
            //graph.AddNode("H3");
            graph.AddNode("S1");
            graph.AddNode("S2");
            graph.AddNode("S3");
            graph.AddNode("S4");
            //graph.AddNode("S5");
            //graph.AddNode("S6");
            //graph.AddNode("S7");

            //PrintEntries();
            LoadTableFromFile(path);
            var calculator = new DistanceCalculator();
            var distances = calculator.CalculateDistances(graph, "S1", "S2");  // Start from "G"

            foreach (var d in distances)
            {
                //if (d.Key.Contains("H"))
                //{
                Console.WriteLine("{0}, {1}", d.Key, d.Value); 
                //}

            }
        }
        public void LoadTableFromFile(string configFilePath)
        {
            foreach (var row in File.ReadAllLines(configFilePath))
            {
                var splitRow = row.Split(", ");
                if (splitRow[0] == "#" || splitRow.Length > 3)
                {
                    continue;
                }
                int value = int.Parse(splitRow[2]);
                var key = splitRow[0] + ", " + splitRow[1];
                foreach(var node in graph.Nodes.Keys)
                {
                    foreach(var node2 in graph.Nodes.Keys)
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
                nodes.Add(key, value);

            }
        }
        public void PrintEntries()
        {
            int i = 1;
            Console.WriteLine("Nodes, Length");
            foreach (KeyValuePair<string, int> kvp in nodes)
            {
                Console.WriteLine(i + ". {0}, {1}", kvp.Key, kvp.Value);
                i++;
            }
        }
        static void Main()
        {
            
            Program program = new Program();
            

           
        }
        

    }
}
