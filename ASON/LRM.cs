using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ASON
{
    public class LRM
    {
        public LRM()
        {

        }

        public List<string> ReceiveLinkConnectionRequest(int firstSubnetworkPort, int secondSubnetworkPort, List<int> slots)
        {
            string firstSubPrt = firstSubnetworkPort.ToString();
            string secondSubPrt = secondSubnetworkPort.ToString();
            string slotsToAppend = "";
            foreach(var slot in slots)
            {
                if(slot.Equals(slots.Last()))
                {
                    slotsToAppend += slot;
                }
                else
                {
                    slotsToAppend += slot + ", ";
                }
            }
            Logs.ShowLog(LogType.LRM, $"LRM A is sending SNP Negotiation Request([{slotsToAppend}]) to LRM Z...");
            Thread.Sleep(300);
            List<string> snp = new List<string>();

            snp.Add(firstSubPrt + ": " + new Random().Next(1, 500));
            snp.Add(secondSubPrt + ": " + new Random().Next(1, 500));
            Logs.ShowLog(LogType.LRM, $"LRM Z is sending SNP Negotiation Response({snp[1]}, Confirmed) to LRM A...");
            Logs.ShowLog(LogType.LRM, $"LRM A is sending SNP Link Connection Response({snp[0]}, {snp[1]}) to CC...");
            Logs.ShowLog(LogType.LRM, "Sending Local Topology to Domain RC ...");
            Logs.ShowLog(LogType.RC, "Received Local Topology from Domain LRM...");
            return snp;
        }

    }
}
