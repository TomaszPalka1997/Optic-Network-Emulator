using System;
using System.Collections.Generic;

namespace DataStructures
{
    public class RoutingTable
    {
        public List<RoutingTableEntry> Entries { get; set; }

        public RoutingTable()
        {
            Entries = new List<RoutingTableEntry>();
        }

    }

}