using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dijkstra
{
    [DebuggerDisplay("-> {Target.Name} ({Distance})")]
    internal class NodeConnection
    {
        internal Node Target { get; private set; }
        internal double Distance { get; private set; }

        internal NodeConnection(Node target, double distance)
        {
            Target = target;
            Distance = distance;
        }
    }
}
