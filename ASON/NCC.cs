using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using DataStructures;

namespace ASON
{
    public class NCC
    {
        public string SourceName { get; set; }
        public string DestName { get; set; }
        public long Bandwidth { get; set; }
        public Directory Dir { get; set; }
        public Policy Pol { get; set; }
        public ConnectionController CC { get; set; }
        public Dictionary<string, string> IpTableFinal { get; set; }
        int i = 0;
        public NCC()
        {
            if (i < 1)
            {
                IpTableFinal = new Dictionary<string, string>();
                string workingDirectory = Environment.CurrentDirectory;
                string path = Path.Combine(workingDirectory, @"DataStructures", "Directory.properties");
                Dir = new Directory(path);
                Pol = new Policy();
                CC = new ConnectionController();
            }
            i++;
        }

        public void ReceiveCallRequest(string sourceName, string destName, long bandwidth)
        {
            SourceName = sourceName;
            DestName = destName;
            Bandwidth = bandwidth;
            SendDirectoryRequest(SourceName);
            //delay
            SendDirectoryRequest(DestName);
            //delay
            SendPolicyRequest();
            //delay
            SendConnectionRequest();
        }
        public void ReceiveCallTeardown(string sourceName, string destName)
        {
            CC.ClearConnectionResources(IpTableFinal[sourceName], IpTableFinal[destName]);
            i--;
        }
        private void SendDirectoryRequest(string name)
        {
            Logs.ShowLog(LogType.NCC, $"Sending Directory Request({name}) to Directory...");
            Thread.Sleep(500);
            string ipAddress = Dir.ReceiveDirectoryRequest(name);
            if (!IpTableFinal.ContainsKey(name))
            {
                IpTableFinal.Add(name, ipAddress);
            }
                
        }


        private void SendPolicyRequest()
        {
            Logs.ShowLog(LogType.NCC, "Sending Policy Request to Policy...");
            Thread.Sleep(500);
            bool isAuthenticated = Pol.ReceivePolicyRequest();
            if (isAuthenticated == true)
            {
                Logs.ShowLog(LogType.NCC, "Connection is authenticated.");
            }
        }

        private void SendConnectionRequest()
        {
            Logs.ShowLog(LogType.NCC, "Sending Connection Request to Connection Controller.");
            CC.ReceiveConnectionRequest(IpTableFinal[SourceName], IpTableFinal[DestName], Bandwidth);
        }

        public void SendCallAcceptRequest(string srcName, string destName, long bandwidth)
        {

        }
    }
}
