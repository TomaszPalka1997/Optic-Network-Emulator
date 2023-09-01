using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASON
{
    public class ConnectionController
    {
        public RoutingController RC { get; set; }
        public LRM Lrm { get; set; }
        public List<string> InOuts { get; set; }
        public List<int> SlotsToCcSubnetwork { get; set; }
        public List<string> ShortestPathSub1 { get; set; }
        public List<string> ShortestPathSub2 { get; set; }
        public bool IsSubnetwork1 { get; set; }
        public bool IsSubnetwork2 { get; set; }
        public bool AreBothSubnetworks { get; set; }
        int i = 0;
        public ConnectionController()
        {
            InOuts = new List<string>();
            if (i < 1)
            {
                RC = new RoutingController();
                Lrm = new LRM();
                
                SlotsToCcSubnetwork = new List<int>();
                ShortestPathSub1 = new List<string>();
                ShortestPathSub2 = new List<string>();
                
            }
            i++;
        }

        public void ReceiveConnectionRequest(string sourceIp, string destIp, long bandwidth)
        {
            SendRouteTableQuery(sourceIp, destIp, bandwidth);
            if (InOuts.Count() == 2)
            {
                if (InOuts.Contains("10"))
                {
                    IsSubnetwork1 = true;
                    List<string> subnetwork1 = new List<string> { "S1", "S2", "S3", "S4" };
                    ShortestPathSub1 = SubnetworkConnectionController(subnetwork1, InOuts[0], InOuts[1], SlotsToCcSubnetwork);
                    Logs.ShowLog(LogType.CC, "Sending Connection Request to Subnetwork CC...");
                    //Logs.ShowLog(LogType.CC, "Sending Route Table Query to Subnetwork RC...");
                }
                else if (InOuts.Contains("60"))
                {
                    List<string> subnetwork2 = new List<string> { "S5", "S6", "S7" };
                    ShortestPathSub2 = SubnetworkConnectionController(subnetwork2, InOuts[0], InOuts[1], SlotsToCcSubnetwork);
                    Logs.ShowLog(LogType.CC, "Sending Connection Request to Subnetwork CC...");
                    //Logs.ShowLog(LogType.CC, "Sending Route Table Query to Subnetwork RC...");
                    IsSubnetwork2 = true;
                }
                
            }
            else if (InOuts.Count() == 4)
            {
                List<string> subnetwork1 = new List<string> { "S1", "S2", "S3", "S4" };
                List<string> subnetwork2 = new List<string> { "S5", "S6", "S7" };
                Logs.ShowLog(LogType.CC, "Sending Connection Request to Subnetwork CC...");
                Logs.ShowLog(LogType.CC, "Sending Connection Request to Subnetwork CC...");
                //Logs.ShowLog(LogType.CC, "Sending Route Table Query to Subnetwork RC...");
                if (InOuts[0] == "10" || InOuts[0] == "40")
                {
                    ShortestPathSub1 = SubnetworkConnectionController(subnetwork1, InOuts[0], InOuts[1], SlotsToCcSubnetwork);
                    ShortestPathSub2 = SubnetworkConnectionController(subnetwork2, InOuts[2], InOuts[3], SlotsToCcSubnetwork);
                }
                else if(InOuts[0] == "70" || InOuts[0] == "60")
                {
                    ShortestPathSub1 = SubnetworkConnectionController(subnetwork1, InOuts[2], InOuts[3], SlotsToCcSubnetwork);
                    ShortestPathSub2 = SubnetworkConnectionController(subnetwork2, InOuts[0], InOuts[1], SlotsToCcSubnetwork);

                }
                
                AreBothSubnetworks = true;
            }
            
        }

        public void ClearConnectionResources(string sourceIP, string destIP)
        {
            
            InOuts.Clear();
            ShortestPathSub1.Clear();
            ShortestPathSub2.Clear();
            
            RC.ClearConnectionResourcesRC(sourceIP, destIP);
            i--;

        }

        private void SendRouteTableQuery(string sourceIp, string destIp, long bandwidth)
        {
            Logs.ShowLog(LogType.CC, "Sending Route Table Query to RC...");
            
            (List<string> inOuts, Dictionary<string, List<int>> allocatedConnections) = RC.ReceiveRouteTableQuery(sourceIp, destIp, bandwidth);
            string key = sourceIp + ", " + destIp;
            InOuts = inOuts;
            SlotsToCcSubnetwork = allocatedConnections[key];
            int counter = 0;
            List<int> tempList = new List<int>();
            Logs.ShowLog(LogType.RC, "Sending Route Table Query Response to Domain CC...");
            foreach (var row in inOuts)
            {
                if(row == "25" || row == "52")
                {
                    tempList.Add(Convert.ToInt32(row));
                    counter++;
                }
                if(counter == 2)
                {
                    Logs.ShowLog(LogType.CC, "Sending SNP Link Connection Request to LRM...");
                    List<string> snp = Lrm.ReceiveLinkConnectionRequest(tempList[0], tempList[1], allocatedConnections[key]);
                    break;
                }
            }
            
        }

        private List<string> SubnetworkConnectionController(List<string> subnetwork, string srcPort, string destPort, List<int> slots)
        {
            //Log informujacy ze CC podsieciowe dostaly ConnectionRequest(List<string> subnetwork, string srcPort, string destPort, List<int> slots)
            List<string> nodes = new List<string>();
            nodes = RC.RouteTableQuerySubnetwork(subnetwork, srcPort, destPort);
            
            return nodes;
        }
    }
}
