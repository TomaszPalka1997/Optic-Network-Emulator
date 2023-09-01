using System;
using System.Text;

namespace DataStructures
{
    public enum LogType {CONNECTED, ERROR, INFO, CC, CPCC, DIRECTORY, LRM, NCC, POLICY, RC }
    static public class Logs
    {
        public static void ShowLog(LogType type, string message)
        {
            StringBuilder sb = new StringBuilder();
            switch (type)
            {
                case LogType.CONNECTED:
                    sb.Append("CONNECTED :: ");
                    break;
                case LogType.ERROR:
                    sb.Append("ERROR :: ");
                    break;
                case LogType.INFO:
                    sb.Append("INFO :: ");
                    break;
                case LogType.CC:
                    sb.Append("CC :: ");
                    break;
                case LogType.CPCC:
                    sb.Append("CPCC :: ");
                    break;
                case LogType.DIRECTORY:
                    sb.Append("DIRECTORY :: ");
                    break;
                case LogType.LRM:
                    sb.Append("LRM :: ");
                    break;
                case LogType.NCC:
                    sb.Append("NCC :: ");
                    break;
                case LogType.POLICY:
                    sb.Append("POLICY :: ");
                    break;
                case LogType.RC:
                    sb.Append("RC :: ");
                    break;
            }
            sb.Append(DateTime.Now.ToString());
            sb.Append(" :: ");
            sb.Append(message);
            //sb.Append(Environment.NewLine);
            Console.WriteLine(sb.ToString());
        }
    }
}
