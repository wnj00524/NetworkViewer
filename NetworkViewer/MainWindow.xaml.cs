using GraphX.Common.Enums;
using GraphX.Logic.Models;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetworkViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NetworkGraph _graph;

        public MainWindow()
        {
            InitializeComponent();

            // It's best practice to wait for the UI to load before drawing the graph
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetupGraph();
        }

        private void SetupGraph()
        {
            _graph = new NetworkGraph();

            // 1. Create test nodes
            var nodeA = new NetworkNode { Name = "Web Server" };
            var nodeB = new NetworkNode { Name = "Firewall" };
            var nodeC = new NetworkNode { Name = "Database" };
            var nodeD = new NetworkNode { Name = "Client" };

            // Add nodes to the graph
            _graph.AddVertex(nodeA);
            _graph.AddVertex(nodeB);
            _graph.AddVertex(nodeC);
            _graph.AddVertex(nodeD);

            // 2. Create connections (Edges)
            // The capacity property is ready for your future network flow logic!
            var edge1 = new NetworkEdge(nodeA, nodeB) { Capacity = 100 };
            var edge2 = new NetworkEdge(nodeB, nodeC) { Capacity = 500 };
            var edge3 = new NetworkEdge(nodeD, nodeA) { Capacity = 50 };

            // Add edges to the graph
            _graph.AddEdge(edge1);
            _graph.AddEdge(edge2);
            _graph.AddEdge(edge3);

            // 3. Configure the Logic Core
            var logicCore = new GXLogicCore<NetworkNode, NetworkEdge, NetworkGraph>(_graph);

            // Tell GraphX how to automatically position the nodes
            // KK (Kamada-Kawai) is a good default algorithm for general networks
            logicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
            logicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            logicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;

            Area.LogicCore = logicCore;

            // 4. Enable Interactions
            Area.SetVerticesDrag(true, true); // Allows the user to click and drag nodes

            // 5. Render the Graph
            Area.GenerateGraph(true, true);

            // Automatically zoom the view to fit the newly generated network
            zoomControl.ZoomToFill();
        }
    }
}