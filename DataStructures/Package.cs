using System.Collections.Generic;

namespace DataStructures
{
    public class Package
    {
        public string SourceName { get; set; }
        public string DestAddress { get; set; }
        public int IncomingPort { get; set; }
        public string RoutingTable { get; set; }
        public long Bandwidth { get; set; }
        public string DestName { get; set; }
        public int DestPort { get; set; }
        public string Message { get; set; }
        public List<int> Slots { get; set; }
        public List<string> ShortestPath { get; set; }
        public List<string> InOutsFromSubs { get; set; }

        public Package()
        {
            SourceName = "";
            DestAddress = "";
            IncomingPort = 0;
            RoutingTable = "";
            DestPort = 0;
            Message = "";
            Slots = new List<int>();
            ShortestPath = new List<string>();
        }

        public Package(string sourceName, string destAddress, int destPort, string message, string routingTable)
        {
            SourceName = sourceName;
            DestAddress = destAddress;
            IncomingPort = 0;
            DestPort = destPort;
            Message = message;
            RoutingTable = routingTable;
        }

        public Package(string sourceName, string destAddress, int destPort, string message)
        {
            SourceName = sourceName;
            DestAddress = destAddress;
            IncomingPort = 0;
            DestPort = destPort;
            Message = message;
        }
        public Package(string sourceName, string destName, string message, long bandwidth)
        {
            SourceName = sourceName;
            DestName = destName;
            Bandwidth = bandwidth;
            Message = message;
        }

        public Package(string sourceName, int incomingPort, string destAddress, List<int> slots, int destPort, string message)
        {
            SourceName = sourceName;
            IncomingPort = incomingPort;
            DestAddress = destAddress;
            Slots = slots;
            DestPort = destPort;
            Message = message;
        }

        public Package(string sourceName, string destName, string message, int delPort)
        {
            SourceName = sourceName;
            Message = message;
            DestPort = delPort;
            DestName = destName;
        }
    }
}
