
using System.Collections.Generic;
using System.Linq;

namespace DataStructures
{
    public class RoutingTableEntry
    {
        public int OutPort { get; set; }
        public List<int> Slots { get; set; }

        public RoutingTableEntry(int outPort, List<int> slots)
        {
            OutPort = outPort;
            Slots = slots;
        }
      
        public void ClearList(int outPort, List<int> slotsToDelete)
        {
            
        }
    }

}