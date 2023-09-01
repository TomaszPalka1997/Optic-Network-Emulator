using DataStructures;
using System.Collections.Specialized;

namespace Host
{
    class CPCC
    {
        public CPCC() { }

        public void SendCallRequest(string hostName, string destName, long choiceBandWidth)
        {
            Logs.ShowLog(LogType.CPCC, $"Sending Call Request({hostName}, {destName}, {choiceBandWidth}) to NCC...");
        }

        public void SendCallAccept(string destName, string sourceName)
        {
            Logs.ShowLog(LogType.CPCC, $"Sending Call Accept({destName}, {sourceName}, Confirmed) to NCC.");
        }

        public void SendCallReject(string destName, string sourceName)
        {
            Logs.ShowLog(LogType.CPCC, $"Sending Call Accept({destName}, {sourceName}, Rejected) to NCC.");
        }
    }
}