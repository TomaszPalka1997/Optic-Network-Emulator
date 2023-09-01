using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dijkstra
{
    public class DistanceCalculator
    {

        List<string> nodes = new List<string>();
        List<string> possiblePaths = new List<string>();
        string previousNode = "";
        string path = "";
        public IDictionary<string, double> CalculateDistances(Graph graph, string startingNode, string destNode)
        {
            if (!graph.Nodes.Any(n => n.Key == startingNode))
                throw new ArgumentException("Starting node must be in graph.");

            InitialiseGraph(graph, startingNode);
            ProcessGraph(graph, startingNode, destNode);
            foreach(var row in nodes)
            {
                Console.WriteLine(row);
            }
            return ExtractDistances(graph);
        }

        private void InitialiseGraph(Graph graph, string startingNode)
        {
            foreach (Node node in graph.Nodes.Values)
                node.DistanceFromStart = double.PositiveInfinity;
            graph.Nodes[startingNode].DistanceFromStart = 0;
        }

        private void ProcessGraph(Graph graph, string startingNode, string destNode)
        {
            bool finished = false;
            var queue = graph.Nodes.Values.ToList();
            while (!finished)
            {
                Node nextNode = queue.OrderBy(n => n.DistanceFromStart).FirstOrDefault(n => !double.IsPositiveInfinity(n.DistanceFromStart));
                if (nextNode != null)
                {
                    ProcessNode(nextNode, queue, destNode);
                    queue.Remove(nextNode);
                }
                else
                {
                    finished = true;
                }
            }
        }

        private void ProcessNode(Node node, List<Node> queue, string destNode)
        {
            var connections = node.Connections.Where(c => queue.Contains(c.Target));
            
            foreach (var connection in connections)
            {
                Console.WriteLine("Current Node: " + node.Name);
                Console.WriteLine("Target Node: " + connection.Target.Name);

                    double distance = node.DistanceFromStart + connection.Distance;
                if (distance < connection.Target.DistanceFromStart)
                {
                    connection.Target.DistanceFromStart = distance;
                    if (!nodes.Contains(node.Name))
                    {
                        nodes.Add(node.Name);
                    }
                    if (connection.Target.Name == destNode && nodes.Contains(destNode))
                    {
                        nodes.Remove(destNode);
                    }
                    if (connection.Target.Name == destNode)
                    {
                        nodes.Add(connection.Target.Name);
                    }

                }
                
            }
        }

        private IDictionary<string, double> ExtractDistances(Graph graph)
        {
            return graph.Nodes.ToDictionary(n => n.Key, n => n.Value.DistanceFromStart);
        }
    }
}
