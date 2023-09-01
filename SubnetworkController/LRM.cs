using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DataStructures;

namespace SubnetworkController
{
    class LRM
    {
        public LRM() { }

        public void ReceiveLinkConnectionRequest(string inSub, string outSub, List<int> slots, List<string> shortestPath)
        {
            foreach (var row in shortestPath)
            {
                if (shortestPath.IndexOf(row) + 1 < shortestPath.Count())
                {
                    string slotsToAppend = "";
                    foreach (var slot in slots)
                    {
                        if (slot.Equals(slots.Last()))
                        {
                            slotsToAppend += slot;
                        }
                        else
                        {
                            slotsToAppend += slot + ", ";
                        }
                    }
                    Logs.ShowLog(LogType.CC, $"Sending SNP LinkConnectionRequest to LRM A({row}) ...");
                    Logs.ShowLog(LogType.LRM, $" LRM A({row}) is sending SNP Negotiation Request({slotsToAppend}) to LRM Z({shortestPath[shortestPath.IndexOf(row) + 1]}) ...");
                    List<string> snp = new List<string>();

                    snp.Add(inSub + ": " + new Random().Next(1, 500));
                    snp.Add(outSub + ": " + new Random().Next(1, 500));
                    Logs.ShowLog(LogType.LRM, $"LRM Z({shortestPath[shortestPath.IndexOf(row) + 1]}) is sending SNP Negotiation Response({snp[1]}, Confirmed) to LRM A({row})...");
                    Logs.ShowLog(LogType.LRM, $"LRM A({row}) is sending SNP Link Connection Response({snp[0]}, {snp[1]}) to CC...");
                }

            }
        }
    }
}
