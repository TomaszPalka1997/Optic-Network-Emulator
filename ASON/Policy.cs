using DataStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace ASON
{
    public class Policy
    {
        public bool IsConnectionAuthenticated { get; set; }

        public Policy() { }

        public bool ReceivePolicyRequest()
        {
            CheckConnectionAuthentication();
            Logs.ShowLog(LogType.POLICY, "Sending Policy Response(ConnectionAuthenticated) to NCC...");
            return IsConnectionAuthenticated;
        }

        private void CheckConnectionAuthentication()
        {
            //logi requesty
            IsConnectionAuthenticated = true;
        }
    }
}
