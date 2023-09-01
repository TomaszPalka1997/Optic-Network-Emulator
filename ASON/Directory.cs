using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using DataStructures;

namespace ASON
{
    public class Directory
    {
        public Dictionary<string, string> IpTable { get; set; }
        
        public Directory(string configFilePath)
        {
            IpTable = new Dictionary<string, string>();
            LoadDirectory(configFilePath);
        }

        public string ReceiveDirectoryRequest(string name)
        {
            Logs.ShowLog(LogType.DIRECTORY, $"Sending Directory Response({IpTable[name]}) to NCC...");
            return IpTable[name];
        }

        private void LoadDirectory(string configFilePath)
        {
            foreach (var row in File.ReadAllLines(configFilePath))
            {
                var splitRow = row.Split("=");
                IpTable.Add(splitRow[0], splitRow[1]);
            }
        }
    }
}
