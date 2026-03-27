using GraphX.Common.Models;
using GraphX.Controls;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkViewer
{
    // A custom node (Vertex)
    public class NetworkNode : VertexBase
    {
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    // A custom connection (Edge)
    public class NetworkEdge : EdgeBase<NetworkNode>
    {
        public double Capacity { get; set; } // Ready for network flow later
        public NetworkEdge(NetworkNode source, NetworkNode target, double weight = 1)
            : base(source, target, weight) { }
    }

    // The Graph logic container
    public class NetworkGraph : BidirectionalGraph<NetworkNode, NetworkEdge> { }

    public class NetworkGraphArea : GraphArea<NetworkNode, NetworkEdge, NetworkGraph>
    {
    }


}
