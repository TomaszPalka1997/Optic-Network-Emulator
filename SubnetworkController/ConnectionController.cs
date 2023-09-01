using DataStructures;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SubnetworkController
{
    class ConnectionController
    {

        public ConnectionController() { }

        public void SendRouteTableQuery(string inSub, string outSub, List<int> slots, List<string> nodes)
        {
            Logs.ShowLog(LogType.CC, "Sending Route Table Query to RC...");
            RoutingController RC = new RoutingController();
            List<string> shortestPath = RC.ReceiveRouteTableQuery(nodes);
            //foreach (var row in shortestPath)
            //{
            //    Logs.ShowLog(LogType.CC, $"Sending SNP LinkConnectionRequest to LRM A({row}) ...");
            //}
            LRM Lrm = new LRM();
            Lrm.ReceiveLinkConnectionRequest(inSub, outSub, slots, shortestPath);
            Logs.ShowLog(LogType.LRM, "Sending Local Topology to RC ...");
            Logs.ShowLog(LogType.RC, "Received Local Topology from LRM...");


        }
    }
}
