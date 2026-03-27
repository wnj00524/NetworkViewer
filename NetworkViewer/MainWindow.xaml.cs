using GraphX.Common.Enums;
using GraphX.Logic.Models;
using GraphX.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Media;

namespace NetworkViewer
{
    public partial class MainWindow : Window
    {
        private NetworkGraph _graph;

        // State variables for interactions
        private int _nodeCounter = 1;
        private VertexControl _dragSource;
        private Line _previewLine;

        public MainWindow()
        {
            InitializeComponent();
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
            var edge1 = new NetworkEdge(nodeA, nodeB) { Capacity = 100 };
            var edge2 = new NetworkEdge(nodeB, nodeC) { Capacity = 500 };
            var edge3 = new NetworkEdge(nodeD, nodeA) { Capacity = 50 };

            // Add edges to the graph
            _graph.AddEdge(edge1);
            _graph.AddEdge(edge2);
            _graph.AddEdge(edge3);

            // 3. Configure the Logic Core
            var logicCore = new GXLogicCore<NetworkNode, NetworkEdge, NetworkGraph>(_graph);
            logicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
            logicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            logicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;

            Area.LogicCore = logicCore;

            // 4. Enable Interactions
            Area.SetVerticesDrag(true, true);

            // 5. Render the Graph
            Area.GenerateGraph(true, true);
            zoomControl.ZoomToFill();
            Area.ShowAllEdgesLabels(false);

            // 6. Wire up global events for drawing and adding nodes
            zoomControl.MouseDoubleClick += ZoomControl_MouseDoubleClick;
            zoomControl.MouseMove += ZoomControl_MouseMove;
            zoomControl.PreviewMouseRightButtonUp += ZoomControl_PreviewMouseRightButtonUp;

            // Wire up right-click edge drawing events for the initial test nodes
            foreach (var vc in Area.VertexList.Values)
            {
                // We only need the MouseDown event here now
                vc.PreviewMouseRightButtonDown += Vertex_RightMouseDown;
            }
        }

        // --- INTERACTION EVENT HANDLERS ---

        // Spawns a new node on double click
        private void ZoomControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var clickPosition = e.GetPosition(Area);
            var newNode = new NetworkNode { Name = $"New Node {_nodeCounter++}" };
            _graph.AddVertex(newNode);

            var vertexControl = new VertexControl(newNode);
            vertexControl.SetPosition(clickPosition.X, clickPosition.Y);

            // Attach edge drawing start event to the dynamically created node
            vertexControl.PreviewMouseRightButtonDown += Vertex_RightMouseDown;

            Area.AddVertexAndData(newNode, vertexControl, true);
        }

        // Starts drawing a preview line on right-click
        private void Vertex_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragSource = sender as VertexControl;

            _previewLine = new Line
            {
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };

            var startPos = _dragSource.GetPosition();
            _previewLine.X1 = startPos.X + (_dragSource.ActualWidth / 2);
            _previewLine.Y1 = startPos.Y + (_dragSource.ActualHeight / 2);
            _previewLine.X2 = _previewLine.X1;
            _previewLine.Y2 = _previewLine.Y1;

            Area.Children.Add(_previewLine);
        }

        // Updates the preview line to follow the mouse cursor
        private void ZoomControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragSource != null && _previewLine != null)
            {
                var pos = e.GetPosition(Area);
                _previewLine.X2 = pos.X;
                _previewLine.Y2 = pos.Y;
            }
        }


        // Deletes nodes or edges when the user holds ALT and clicks them
        private void ZoomControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only trigger deletion if the ALT key is currently held down
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                DependencyObject hitObject = e.OriginalSource as DependencyObject;

                // Walk up the visual tree to see what we clicked on
                while (hitObject != null && hitObject != zoomControl)
                {
                    if (hitObject is VertexControl vc)
                    {
                        var node = vc.Vertex as NetworkNode;

                        // 1. Remove from UI (GraphX automatically removes attached visual edges too)
                        Area.RemoveVertexAndEdges(node);

                        // 2. Remove from the underlying data model
                        _graph.RemoveVertex(node);

                        // Stop the click from panning the camera
                        e.Handled = true;
                        return;
                    }
                    else if (hitObject is EdgeControl ec)
                    {
                        var edge = ec.Edge as NetworkEdge;

                        // 1. Remove from UI
                        Area.RemoveEdge(edge, true);

                        // 2. Remove from the underlying data model
                        _graph.RemoveEdge(edge);

                        e.Handled = true;
                        return;
                    }
                    hitObject = VisualTreeHelper.GetParent(hitObject);
                }
            }
        }

        // Handles both the snapping of the edge and the cleanup in one place
        private void ZoomControl_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragSource != null)
            {
                // 1. Use WPF Hit Testing to find out what UI element we dropped the line on
                DependencyObject hitObject = e.OriginalSource as DependencyObject;
                VertexControl targetVertex = null;

                // Walk up the visual tree to see if the hit object is part of a VertexControl
                while (hitObject != null)
                {
                    if (hitObject is VertexControl vc)
                    {
                        targetVertex = vc;
                        break;
                    }
                    hitObject = VisualTreeHelper.GetParent(hitObject);
                }

                // 2. If we found a valid target that isn't the node we started from
                if (targetVertex != null && targetVertex != _dragSource)
                {
                    var sourceData = _dragSource.Vertex as NetworkNode;
                    var targetData = targetVertex.Vertex as NetworkNode;

                    var newEdge = new NetworkEdge(sourceData, targetData) { Capacity = 100 };
                    _graph.AddEdge(newEdge);

                    var edgeControl = new EdgeControl(_dragSource, targetVertex, newEdge);
                    Area.AddEdgeAndData(newEdge, edgeControl, true);
                }

                // 3. Always clean up the preview line
                if (_previewLine != null)
                {
                    Area.Children.Remove(_previewLine);
                }

                _previewLine = null;
                _dragSource = null;
            }
        }
    }
}