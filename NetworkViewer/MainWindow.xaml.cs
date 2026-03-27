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
        private int _nodeCounter = 1;
        private GraphX.Controls.VertexControl _dragSource;
        private System.Windows.Shapes.Line _previewLine;

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
            /*
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
            */

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

            //Subscribe to mouse events for interactivity
            zoomControl.MouseDoubleClick += ZoomControl_MouseDoubleClick;
            zoomControl.MouseMove += ZoomControl_MouseMove;
            zoomControl.PreviewMouseRightButtonUp += ZoomControl_PreviewMouseRightButtonUp;


            foreach (var vc in Area.VertexList.Values)
            {
                vc.PreviewMouseRightButtonDown += Vertex_RightMouseDown;
                vc.PreviewMouseRightButtonUp += Vertex_RightMouseUp;
            }
        }

        private void ZoomControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 1. Get the exact coordinates of the mouse relative to the GraphArea
            var clickPosition = e.GetPosition(Area);

            // 2. Create the underlying data model
            var newNode = new NetworkNode { Name = $"New Node {_nodeCounter++}" };
            _graph.AddVertex(newNode);

            // 3. Create the visual UI representation (VertexControl)
            var vertexControl = new GraphX.Controls.VertexControl(newNode);

            // 4. Position the visual control exactly where the user clicked
            vertexControl.SetPosition(clickPosition.X, clickPosition.Y);

            // Attach edge drawing events to the dynamically created node
            vertexControl.PreviewMouseRightButtonDown += Vertex_RightMouseDown;
            vertexControl.PreviewMouseRightButtonUp += Vertex_RightMouseUp;

            // 5. Inject it into the visual canvas (true = generate label automatically)
            Area.AddVertexAndData(newNode, vertexControl, true);
        }

        private void Vertex_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _dragSource = sender as GraphX.Controls.VertexControl;

            // 1. Create a temporary dashed line
            _previewLine = new System.Windows.Shapes.Line
            {
                Stroke = System.Windows.Media.Brushes.DarkGray,
                StrokeThickness = 2,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 }
            };

            // 2. Set the starting point to the center of the source node
            var startPos = _dragSource.GetPosition();
            _previewLine.X1 = startPos.X + (_dragSource.ActualWidth / 2);
            _previewLine.Y1 = startPos.Y + (_dragSource.ActualHeight / 2);
            _previewLine.X2 = _previewLine.X1;
            _previewLine.Y2 = _previewLine.Y1;

            // 3. Add the line to the canvas
            Area.Children.Add(_previewLine);
        }

        private void ZoomControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Update the end of the line to follow the mouse cursor
            if (_dragSource != null && _previewLine != null)
            {
                var pos = e.GetPosition(Area);
                _previewLine.X2 = pos.X;
                _previewLine.Y2 = pos.Y;
            }
        }

        private void Vertex_RightMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_dragSource != null)
            {
                var target = sender as GraphX.Controls.VertexControl;

                // Ensure we dropped it on a valid target, and not on the same node we started from
                if (target != null && target != _dragSource)
                {
                    var sourceData = _dragSource.Vertex as NetworkNode;
                    var targetData = target.Vertex as NetworkNode;

                    // 1. Create the new Edge Data Model
                    var newEdge = new NetworkEdge(sourceData, targetData) { Capacity = 100 };
                    _graph.AddEdge(newEdge);

                    // 2. Create the visual representation and inject it into the UI
                    var edgeControl = new GraphX.Controls.EdgeControl(_dragSource, target, newEdge);
                    Area.AddEdgeAndData(newEdge, edgeControl, true);
                }
            }
        }

        private void ZoomControl_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Always clean up the temporary line when the mouse is released, 
            // regardless of whether it was dropped on a node or empty space.
            if (_previewLine != null)
            {
                Area.Children.Remove(_previewLine);
                _previewLine = null;
                _dragSource = null;
            }
        }
    }
}