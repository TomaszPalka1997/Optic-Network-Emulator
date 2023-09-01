using DataStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace ASON
{
    public class StartAsonCompilation
    {
        public string SourceName { get; set; }
        public string DestName { get; set; }
        public long Bandwidth { get; set; }
        public NCC Ncc { get; set; }
        int i = 0;
        public StartAsonCompilation() 
        { 
            if(i < 1)
                Ncc = new NCC();
            i++;
        }

        public void NewConnection(string sourceName, string destName, long bandwidth, int i)
        {

            SendCallRequest(sourceName, destName, bandwidth, Ncc);
        }
        private void SendCallRequest(string sourceName, string destName, long bandwidth, NCC ncc)
        {
            //Logs.ShowLog(LogType.CPCC, "Sending Call Request to NCC...");
            ncc.ReceiveCallRequest(sourceName, destName, bandwidth);
        }

        public void SendCallTeardown(string sourceName, string destName)
        {
            Logs.ShowLog(LogType.NCC, $"Received Call Teardown({sourceName}, {destName}) from CPCC...");
            Ncc.ReceiveCallTeardown(sourceName, destName);
            i--;
        }

        public void ReceiveCallResponse()
        {

        }
    }
}
