using ceTe.DynamicPDF.Printing;
using Microsoft.VisualBasic;
using QuickGraph;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Swanson
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public partial class MainWindow : Window
    {
        //http://www.itdevspace.com/2010/11/graph-wpf-graph-layout-framework.html?m=1 

        string path = "C:\\Users\\Omer\\source\\repos\\Swanson\\Swanson\\"; // CHANGE PATH HERE <-----------------------------------------------------------------
        SqlConnector SqlConnector;
        string addCode;
        string deleteCode;
        string dfsCode;
        string bfsCode;
        string delVertexCode;
        string editVertexCode;
        public MainWindow()
        {
            SqlConnector = new SqlConnector(path);
            InitializeComponent();
            LoadText();
            DataContext = this;
            LoadGraphNames();
            cbbLoad.ItemsSource = graphNames;
        }
        List<SampleVertex> verticesList = new List<SampleVertex>(); // Main list of vertices 
        SampleVertex vertexTemp; // Keeps track of vertex that is being added or removed
        SampleVertex vertexSearchRoot; // Keeps track of the root node used for searching
        string graphName = ""; // Name of graph
        ObservableCollection<string> graphNames = new ObservableCollection<string>(); // Keeps track of all graph names from DataBase
        bool dfsOn = false; // Flag that tells you if DFS search is running
        bool bfsOn = false; // Flag that tells you if BFS search is running
        bool pause = false; // Flag that can pause DFS and BFS algorithms mid execution
        List<string> searchOutput = new List<string>(); // List in order of how the graph is traversed
        List<string> stackOutput = new List<string>(); // List that contains stack behavior during one of the searches
        string elapsedTimeDFS = ""; // Stores Elapsed Time for DFS algorithm
        string elapsedTimeBFS = ""; // Stores Elapsed Time for DFS algorithm
        //bool bidiretional = false; // Keeps track if cbBidirectional is checked

        private readonly BidirectionalGraph<object, IEdge<object>> graph = new BidirectionalGraph<object, IEdge<object>>(); // Graph meant for display only
        public IBidirectionalGraph<object, IEdge<object>> Graph
        {
            get { return graph; }
        }
        private string _layoutAlgorithm = "EfficientSugiyama";
        public string LayoutAlgorithm
        {
            get { return _layoutAlgorithm; }
            set
            {
                if (value != _layoutAlgorithm)
                {
                    _layoutAlgorithm = value;
                }
            }
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e) // Function for editing the vertex that you right clicked on 
        {
            if (!dfsOn && !bfsOn)
            {
                tbCodeSnippet.Text = editVertexCode;
                var menuItem = sender as MenuItem;
                var temp = menuItem.Tag as SampleVertex;
                foreach (SampleVertex v in verticesList)
                {
                    if (temp.Text == v.Text)
                    {
                        vertexTemp = v;
                    }
                }

                string newText = Interaction.InputBox($"Here you can change the value of the selected vertex:\n\n\n(If you want to quit click ok cancel doesnt work...)", $"Vertex {vertexTemp.Text} settings", $"{vertexTemp.Text}", -1, -1);

                while (!CheckIfUnique(newText) || newText == "")
                {
                    if (vertexTemp.Text == newText)
                    {
                        break;
                    }
                    newText = Interaction.InputBox($"The previous value is already taken, please enter a new one:\n\n\n(If you want to quit click ok cancel doesnt work...)", $"Vertex {vertexTemp.Text} settings", $"{vertexTemp.Text}", -1, -1);
                }

                vertexTemp.Text = newText;
                vertexTemp.NotifyChanged("Text"); 
            }
        }

        private void MenuItemDeleteVertex_Click(object sender, RoutedEventArgs e) // Function for deleting the vertex that you right clicked on 
        {
            if (!dfsOn && !bfsOn)
            {
                tbCodeSnippet.Text = delVertexCode;
                var menuItem = sender as MenuItem;
                var temp = menuItem.Tag as SampleVertex;
                foreach (SampleVertex v in verticesList)
                {
                    if (temp.Text == v.Text)
                    {
                        vertexTemp = v;
                    }
                }

                foreach (SampleVertex v in verticesList.ToList())
                {
                    if (v.adjacencyList.Count > 0)
                    {
                        foreach (SampleVertex adj in v.adjacencyList.ToList())
                        {
                            if (graph.ContainsVertex(vertexTemp))
                            {
                                if (adj.Text == vertexTemp.Text)
                                {
                                    DeleteEdge(v, vertexTemp);
                                }

                                if (v.Text == vertexTemp.Text)
                                {
                                    DeleteEdge(vertexTemp, adj);
                                }

                                if (!graph.ContainsVertex(vertexTemp))
                                {
                                    break;
                                }

                                if (v.adjacencyList.Count == 0 || !graph.ContainsVertex(v))
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if (!graph.ContainsVertex(vertexTemp))
                    {
                        break;
                    }
                }

                if (graph.ContainsVertex(vertexTemp))
                {
                    graph.RemoveVertex(vertexTemp);
                }

                if (verticesList.Contains(vertexTemp))
                {
                    verticesList.Remove(vertexTemp);
                } 
            }
        }

        private bool CheckIfUnique(string name) // Checks if the entered vertex already exists 
        {
            if (GetVertex(name) == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CheckIfVertexIsUsed(SampleVertex vertex) // Checks if the vertex is idle i.e. if there is any edge involving it 
        {
            bool flagFrom = true; // checks if the vertex is pointing to something
            bool flagTo = false; // checks if something is pointing to the vertex
            if (vertex.adjacencyList.Count == 0)
            {
                flagFrom = false;
            }

            foreach (SampleVertex v in verticesList)
            {
                foreach (SampleVertex adj in v.adjacencyList)
                {
                    if (adj.Text == vertex.Text)
                    {
                        flagTo = true;
                    }
                }
            }

            if (flagTo || flagFrom)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private SampleVertex GetVertex(string name) // Returns the exact vertex from main list
        {
            SampleVertex vertex = null;
            foreach (SampleVertex v in verticesList)
            {
                if (v.Text == name)
                {
                    vertex = v;
                    return vertex;
                }
            }
            return vertex;
        }

        private void DeleteEdge(SampleVertex from, SampleVertex to) // Deletes an edge, given source and destination
        {
            foreach (SampleVertex v in verticesList)
            {
                if (v.Active)
                {
                    v.Change();
                }
            }

            IEdge<object> edge;

            foreach (SampleVertex v in verticesList)
            {
                foreach (SampleVertex adj in v.adjacencyList)
                {
                    if (from == v && to == adj)
                    {
                        from.Change();
                        to.Change();
                        v.adjacencyList.Remove(adj);
                        v.NotifyChanged("Adjecent");
                        graph.TryGetEdge(v, adj, out edge);
                        graph.RemoveEdge(edge);
                        if (!CheckIfVertexIsUsed(to))
                        {
                            graph.RemoveVertex(to);
                            verticesList.Remove(to);
                            if (graph.VertexCount == 1)
                            {
                                graph.RemoveVertex(from);
                                verticesList.Remove(from);
                                graph.Clear();
                            }
                        }

                        if (!CheckIfVertexIsUsed(from))
                        {
                            graph.RemoveVertex(from);
                            verticesList.Remove(from);
                            if (graph.VertexCount == 1)
                            {
                                graph.RemoveVertex(to);
                                verticesList.Remove(to);
                                graph.Clear();
                            }
                        }
                        tbFrom.Text = "";
                        tbTo.Text = "";
                        return;
                    }
                }
            }
        }

        private void Liberate() // Deletes all edges and therefore clears the graph
        {
            foreach (SampleVertex v in verticesList.ToList())
            {
                if (v.adjacencyList.Count > 0)
                {
                    foreach (SampleVertex adj in v.adjacencyList.ToList())
                    {
                        DeleteEdge(v, adj);
                    } 
                }
            }

            tbCodeSnippet.Text = "";
        }

        private void Button_Click(object sender, RoutedEventArgs e) // Executes edge adding procedure
        {
            tbCodeSnippet.Text = addCode;
            if (tbFrom.Text != tbTo.Text && !dfsOn && !bfsOn && tbTo.Text != "")
            {
                foreach (SampleVertex v in verticesList)
                {
                    if (v.Active)
                    {
                        v.Change();
                    }
                }
                SampleVertex from = new SampleVertex(tbFrom.Text);
                SampleVertex to = new SampleVertex(tbTo.Text);

                AddEdge(from, to);

                tbFrom.Text = "";
                tbTo.Text = "";
                tbFrom.Focus();
            }
            else
            {
                if (tbFrom.Text != tbTo.Text)
                {
                    MessageBox.Show("You have entered the same name in both fields");
                }
                tbFrom.Text = "";
                tbTo.Text = "";
                tbFrom.Focus();
            }
        }

        private void AddEdge(SampleVertex from, SampleVertex to) // Function for adding new edges and vertices
        {
            bool fromFlag = true;
            bool toFlag = true;
            bool edgeFlag = true;

            foreach (SampleVertex v in verticesList)
            {
                if (v.Text == from.Text)
                {
                    from = v;
                    fromFlag = false;
                }

                if (v.Text == to.Text)
                {
                    to = v;
                    toFlag = false;
                }
            }

            if (fromFlag)
            {
                verticesList.Add(from);
                graph.AddVertex(from);
            }

            if (toFlag)
            {
                verticesList.Add(to);
                graph.AddVertex(to);
            }

            IEdge<object> edge = new Edge<object>(from, to);

            foreach (SampleVertex v in verticesList)
            {
                foreach (SampleVertex adj in v.adjacencyList)
                {
                    if (v.Text == from.Text && adj.Text == to.Text)
                    {
                        edgeFlag = false;
                    }
                }
            }

            if (edgeFlag)
            {
                graph.AddEdge(edge);
                if (!from.adjacencyList.Contains(to))
                {
                    from.adjacencyList.Add(to);
                }
                from.NotifyChanged("Adjecent");
                to.Change();
                from.Change();
                if (cbBidirectional.IsChecked == true)
                {
                    edge = new Edge<object>(to, from);
                    graph.AddEdge(edge);
                    if (!to.adjacencyList.Contains(from))
                    {
                        to.adjacencyList.Add(from);
                    }
                    to.NotifyChanged("Adjecent");
                }
            }
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)// KeyDown functions
        {
            if (e.Key == Key.N) 
            {
                if (!dfsOn && !bfsOn)
                {
                    foreach (SampleVertex v in verticesList)
                    {
                        if (!v.Active)
                        {
                            v.Change();
                            break;
                        }
                    } 
                }
            }

            if (e.Key == Key.C)
            {
                if (!dfsOn && !bfsOn && MessageBox.Show($"Are you sure you want to clear the graph: {graphName}", "Clear Graph", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Liberate();
                }
            }  

            if (e.Key == Key.Escape)
            {
                if (graphNames.Contains(graphName) && !bfsOn && !dfsOn)
                {
                    if (MessageBox.Show($"Are you sure you want to delete the graph: {graphName}", "Graph Deletion", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Liberate();
                        graphName = tbGraphName.Text;
                        if (tbGraphName.Text != "")
                        {
                            SqlConnector.Delete(graphName);
                        }
                        tbGraphName.Text = "";
                        graphNames.Clear();
                        LoadGraphNames();
                        cbbLoad.ItemsSource = null;
                        cbbLoad.ItemsSource = graphNames;
                        tbSearch.Text = "";
                        lSearchOutput.Content = "";
                        tbElapsedDFS.Text = "";
                        tbElapsedBFS.Text = "";
                    } 
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e) // Function deletes a certain edge 
        {
            tbCodeSnippet.Text = deleteCode;
            if (tbFrom.Text != "" && tbTo.Text != "" && !dfsOn && !bfsOn)
            {
                SampleVertex from = GetVertex(tbFrom.Text);
                SampleVertex to = GetVertex(tbTo.Text);

                if (from == null || to == null)
                {
                    tbFrom.Text = "";
                    tbTo.Text = "";
                    tbFrom.Focus();
                    MessageBox.Show("This edge does not exist.");
                    return;
                }

                DeleteEdge(from, to);
            }
        }

        private bool AllActive() // Checks if all vertices are active i.e. turned black in color
        {
            bool flag = true;
            foreach (SampleVertex v in verticesList)
            {
                if (!v.Active)
                {
                    flag = false;
                }
            }

            return flag;
        }       

        private async Task DFSAsync(SampleVertex vertex) // Depth First Search algorithm code
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            btnPause.Visibility = Visibility.Visible;
            btnPause.Background = Brushes.Black;
            btnPause.Foreground = Brushes.White;
            btnPause.Content = "Pause";
            pause = false;
            bool paused = false;

            dfsOn = true;
            List<SampleVertex> stack = vertex.adjacencyList.ToList();
            stack.Reverse();
            stack.Add(vertex);

            btnDFS.Background = Brushes.White;
            btnDFS.Foreground = Brushes.Black;

            while (stack.Count > 0)
            {
                if (pause)
                {
                    btnPause.Content = "Resume";
                    btnPause.Background = Brushes.White;
                    btnPause.Foreground = Brushes.Black;

                    paused = true;

                    while (pause)
                    {
                        await Task.Delay(10); 
                    }

                    btnPause.Content = "Pause";
                    btnPause.Background = Brushes.Black;
                    btnPause.Foreground = Brushes.White;
                }

                List<SampleVertex> help = new List<SampleVertex>();
                SampleVertex temp = stack[stack.Count - 1];
                temp.Change();
                temp.Visit();

                int i = 0;
                stackOutput.Clear();
                foreach (SampleVertex v in stack)
                {
                    stackOutput.Add($"{v.Text}[{i++}]");
                }
                stackOutput.Reverse();
                lStackOutput.Content = string.Join("\n", stackOutput);
                stackOutput.Reverse();

                await Task.Delay(1000);
                
                searchOutput.Add(temp.Text);
                lSearchOutput.Content = string.Join("->", searchOutput);
                if (searchOutput.Count == graph.VertexCount)
                {
                    lStackOutput.Content = "";
                }

                temp.Visit();
                stack.Remove(temp);

                foreach (SampleVertex v in temp.adjacencyList.ToList())
                {
                    if (!v.Active && !stack.Contains(v))
                    {
                        help.Add(v);
                    }
                }

                if (help.Count != 0)
                {
                    help.Reverse();
                    stack.AddRange(help);
                    help.Clear();
                }

                if (AllActive())
                {
                    break;
                }
            }

            stackOutput.Clear();
            searchOutput.Clear();

            btnDFS.Background = Brushes.Black;
            btnDFS.Foreground = Brushes.White;

            btnPause.Visibility = Visibility.Collapsed;

            dfsOn = false;

            stopwatch.Stop();
            
            tbElapsedDFS.Text = "DFS Elapsed Time: " + (stopwatch.ElapsedMilliseconds).ToString() + "ms";
            if (!paused)
            {
                elapsedTimeDFS = (stopwatch.ElapsedMilliseconds).ToString() + "ms";
            }
            else
            {
                tbElapsedDFS.Text = "DFS Elapsed Time: " + (stopwatch.ElapsedMilliseconds).ToString() + "ms" + " (with pauses)";
            }

            return;
        }

        private async Task BFSAsync(SampleVertex vertex) // Breadth First Search algorithm code
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            btnPause.Visibility = Visibility.Visible;
            btnPause.Background = Brushes.Black;
            btnPause.Foreground = Brushes.White;
            btnPause.Content = "Pause";
            pause = false;
            bool paused = false;

            btnPause.Visibility = Visibility.Visible;

            bfsOn = true;
            List<SampleVertex> queue = vertex.adjacencyList.ToList();
            queue.Reverse();
            queue.Add(vertex);

            btnBFS.Background = Brushes.White;
            btnBFS.Foreground = Brushes.Black;

            while (queue.Count > 0)
            {
                if (pause)
                {
                    btnPause.Content = "Resume";
                    btnPause.Background = Brushes.White;
                    btnPause.Foreground = Brushes.Black;

                    paused = true;

                    while (pause)
                    {
                        await Task.Delay(10);
                    }

                    btnPause.Content = "Pause";
                    btnPause.Background = Brushes.Black;
                    btnPause.Foreground = Brushes.White;
                }

                List<SampleVertex> help = new List<SampleVertex>();
                SampleVertex temp = queue[queue.Count - 1];
                temp.Change();
                temp.Visit();

                int i = queue.Count - 1;
                stackOutput.Clear();
                foreach (SampleVertex v in queue)
                {
                    stackOutput.Add($"{v.Text}[{i--}]");
                }
                stackOutput.Reverse();
                lStackOutput.Content = string.Join("\n", stackOutput);
                stackOutput.Reverse();

                await Task.Delay(1000);

                searchOutput.Add(temp.Text);
                lSearchOutput.Content = string.Join("->", searchOutput);
                if (searchOutput.Count == graph.VertexCount)
                {
                    lStackOutput.Content = "";
                }

                temp.Visit();
                queue.Remove(temp);
                queue.Reverse();

                foreach (SampleVertex v in temp.adjacencyList.ToList())
                {
                    if (!v.Active && !queue.Contains(v))
                    {
                        help.Add(v);
                    }
                }

                if (help.Count != 0)
                {
                    queue.AddRange(help);
                    help.Clear();
                }

                queue.Reverse();

                if (AllActive())
                {
                    break;
                }
            }

            stackOutput.Clear();
            searchOutput.Clear();

            btnBFS.Background = Brushes.Black;
            btnBFS.Foreground = Brushes.White;

            btnPause.Visibility = Visibility.Collapsed;

            bfsOn = false;

            stopwatch.Stop(); 
            
            tbElapsedBFS.Text = "BFS Elapsed Time: " + (stopwatch.ElapsedMilliseconds).ToString() + "ms";
            if (!paused)
            {
                elapsedTimeBFS = (stopwatch.ElapsedMilliseconds).ToString() + "ms";
            }
            else
            {
                tbElapsedBFS.Text = "BFS Elapsed Time: " + (stopwatch.ElapsedMilliseconds).ToString() + "ms" + " (with pauses)";
            }

            return;
        }

        private void btnDFS_Click(object sender, RoutedEventArgs e) // Button for activating DFS procedure
        {
            if (!bfsOn && !dfsOn && tbSearch.Text != "" && GetVertex(tbSearch.Text) != null)
            {
                tbCodeSnippet.Text = dfsCode;
                lSearchOutput.Content = "";
                foreach (SampleVertex v in verticesList)
                {
                    if (v.Active)
                    {
                        v.Change();
                    }
                }
                vertexSearchRoot = GetVertex(tbSearch.Text);

                DFSAsync(vertexSearchRoot);
            }
        }

        private void cbBidirectional_Checked(object sender, RoutedEventArgs e) // Changes state of bidirectional flag
        {
            //if (graph.EdgeCount == 0)
            //{
            //    bidiretional = !bidiretional;
            //}
        }

        private void btnBFS_Click(object sender, RoutedEventArgs e) // Button for activating BFS procedure
        {
            if (!dfsOn && !bfsOn && tbSearch.Text != "" && GetVertex(tbSearch.Text) != null)
            {
                tbCodeSnippet.Text = bfsCode;
                lSearchOutput.Content = "";
                foreach (SampleVertex v in verticesList)
                {
                    if (v.Active)
                    {
                        v.Change();
                    }
                }
                vertexSearchRoot = GetVertex(tbSearch.Text);

                BFSAsync(vertexSearchRoot);
            }
        }

        private void LoadText() // Function that loads text into text blocks
        {
            lSearchOutput.Content = "";
            lStackOutput.Content = "";
            tbElapsedDFS.Text = "";
            tbElapsedBFS.Text = "";
            tbControlPanel.Text = "Add new vertices by\nconnecting them with edges.\nNo idle vertices allowed.\n" +
                "Upon deletion of edge\nevery vertex is checked\nand if needed removed.";
            tbSearchAlgorithms.Text = "Depth First Search (DFS):\n" +
                "The algorithm starts at the root node and explores as far as possible along each branch before backtracking.\n\n" +
                "Breadth First Search (BFS):\n" +
                "It starts at the tree root and explores all nodes at the present depth prior to moving on to the nodes at the next depth level.";


            addCode = "private void AddEdge(SampleVertex from, SampleVertex to)\n" +
"        {	\n" +
"            bool fromFlag = true;\n" +
"            bool toFlag = true;	\n" +
"            bool edgeFlag = true;\n" +
"\n" +
"            foreach (SampleVertex v in verticesList)\n" +
"            {\n" +
"                if (v.Text == from.Text)\n" +
"                {\n" +
"                    from = v;\n" +
"                    fromFlag = false;\n" +
"                }\n" +
"\n" +
"                if (v.Text == to.Text)\n" +
"                {\n" +
"                    to = v;\n" +
"                    toFlag = false;\n" +
"                }\n" +
"            }\n" +
"\n" +
"\n" +
"            if (fromFlag)\n" +
"            {\n" +
"                verticesList.Add(from); \n" +
"            }\n" +
"\n" +
"            if (toFlag)\n" +
"            {\n" +
"                verticesList.Add(to);\n" +
"            }\n" +
"\n" +
"            foreach (SampleVertex v in verticesList)\n" +
"            {\n" +
"                foreach (SampleVertex adj in v.adjacencyList)\n" +
"                {\n" +
"                    if (v.Text == from.Text && adj.Text == to.Text)\n" +
"                    {\n" +
"                        edgeFlag = false;\n" +
"                    }\n" +
"                }\n" +
"            }\n" +
"\n" +
"            if (edgeFlag)\n" +
"            {\n" +
"                if (!from.adjacencyList.Contains(to))\n" +
"                {\n" +
"                    from.adjacencyList.Add(to);\n" +
"                }\n" +
"\n" +
"                if (cbBidirectional.IsChecked == true)\n" +
"                {\n" +
"                    if (!to.adjacencyList.Contains(from))\n" +
"                    {\n" +
"                        to.adjacencyList.Add(from);\n" +
"                    }\n" +
"                }\n" +
"            }\n" +
"        }\n" +
"\n";

            deleteCode = "private void DeleteEdge(SampleVertex from, SampleVertex to) \n" +
"        {\n" +
"            foreach (SampleVertex v in verticesList)\n" +
"            {\n" +
"                foreach (SampleVertex adj in v.adjacencyList)\n" +
"                {\n" +
"                    if (from == v && to == adj)\n" +
"                    {\n" +
"                        v.adjacencyList.Remove(adj);\n" +
"                        if (!CheckIfVertexIsUsed(to))\n" +
"                        {\n" +
"                            verticesList.Remove(to);\n" +
"                            if (verticesList.Count == 1)\n" +
"                            {\n" +
"                                verticesList.Remove(from);\n" +
"                            }\n" +
"                        }\n" +
"\n" +
"                        if (!CheckIfVertexIsUsed(from))\n" +
"                        {\n" +
"                            verticesList.Remove(from);\n" +
"                            if (verticesList.Count == 1)\n" +
"                            {\n" +
"                                verticesList.Remove(to);\n" +
"                            }\n" +
"                        }\n" +
"                        return;\n" +
"                    }\n" +
"                }\n" +
"            }\n" +
"        }\n" +
"\n";

            dfsCode = "private async Task DFSAsync(SampleVertex vertex) \n" +
"        {\n" +
"            dfsOn = true;\n" +
"            List<SampleVertex> stack = vertex.adjacencyList.ToList();\n" +
"            stack.Reverse();\n" +
"            stack.Add(vertex);\n" +
"\n" +
"            while (stack.Count > 0)\n" +
"            {\n" +
"                List<SampleVertex> help = new List<SampleVertex>();\n" +
"                SampleVertex temp = stack[stack.Count - 1];\n" +
"\n" +
"                temp.Active = true;\n" +
"                stack.Remove(temp);\n" +
"\n" +
"                foreach (SampleVertex v in temp.adjacencyList.ToList())\n" +
"                {\n" +
"                    if (!v.Active && !stack.Contains(v))\n" +
"                    {\n" +
"                        help.Add(v);\n" +
"                    }\n" +
"                }\n" +
"\n" +
"                if (help.Count != 0)\n" +
"                {\n" +
"                    help.Reverse();\n" +
"                    stack.AddRange(help);\n" +
"                    help.Clear();\n" +
"                }\n" +
"                if (AllActive())\n" +
"                {\n" +
"                    break;\n" +
"                }\n" +
"            }\n" +
"\n" +
"            dfsOn = false;\n" +
"\n" +
"            return;\n" +
"        }\n" +
"\n";

            bfsCode = "private async Task BFSAsync(SampleVertex vertex)\n" +
"        {\n" +
"\n" +
"            bfsOn = true;\n" +
"            List<SampleVertex> queue = vertex.adjacencyList.ToList();\n" +
"            queue.Reverse();\n" +
"            queue.Add(vertex);\n" +
"\n" +
"            while (queue.Count > 0)\n" +
"            {\n" +
"\n" +
"                List<SampleVertex> help = new List<SampleVertex>();\n" +
"                SampleVertex temp = queue[queue.Count - 1];\n" +
"                temp.Active = true;\n" +
"\n" +
"                \n" +
"                temp.Visit();\n" +
"                queue.Remove(temp);\n" +
"                queue.Reverse();\n" +
"\n" +
"                foreach (SampleVertex v in temp.adjacencyList.ToList())\n" +
"                {\n" +
"                    if (!v.Active && !queue.Contains(v))\n" +
"                    {\n" +
"                        help.Add(v);\n" +
"                    }\n" +
"                }\n" +
"\n" +
"                if (help.Count != 0)\n" +
"                {\n" +
"                    queue.AddRange(help);\n" +
"                    help.Clear();\n" +
"                }\n" +
"\n" +
"                queue.Reverse();\n" +
"\n" +
"                if (AllActive())\n" +
"                {\n" +
"                    break;\n" +
"                }\n" +
"            }\n" +
"\n" +
"            bfsOn = false; \n" +
"\n" +
"            return;\n" +
"        }\n" +
"\n";

            delVertexCode = "foreach (SampleVertex v in verticesList)\n" +
"            {\n" +
"                if (temp.Text == v.Text)\n" +
"                {\n" +
"                    vertexTemp = v;\n" +
"                }\n" +
"            }\n" +
"\n" +
"            foreach (SampleVertex v in verticesList.ToList())\n" +
"            {\n" +
"                if (v.adjacencyList.Count > 0)\n" +
"                {\n" +
"                    foreach (SampleVertex adj in v.adjacencyList.ToList())\n" +
"                    {\n" +
"                        if (verticesList.Contanis())\n" +
"                        {\n" +
"                            if (adj.Text == vertexTemp.Text)\n" +
"                            {\n" +
"                                DeleteEdge(v, vertexTemp);\n" +
"                            }\n" +
"\n" +
"                            if (v.Text == vertexTemp.Text)\n" +
"                            {\n" +
"                                DeleteEdge(vertexTemp, adj);\n" +
"                            }\n" +
"\n" +
"                            if (!verticesList.Contains (vertexTemp))\n" +
"                            {\n" +
"                                break;\n" +
"                            }\n" +
"\n" +
"if (v.adjacencyList.Count == 0 ||  !verticesList.Contains (v))\n" +
"                            {\n" +
"                                break;\n" +
"                            }\n" +
"                        }\n" +
"                    }\n" +
"                }\n" +
"                if (!verticesList.Contains(vertexTemp))\n" +
"                {\n" +
"                    break;\n" +
"                }\n" +
"            }\n" +
"\n" +
"            if (verticesList.Contains(vertexTemp))\n" +
"            {\n" +
"                verticesList.Remove(vertexTemp);\n" +
"            }\n" +
"\n";

            editVertexCode = "foreach (SampleVertex v in verticesList)\n" +
"            {\n" +
"                if (temp.Text == v.Text)\n" +
"                {\n" +
"                    vertexTemp = v;\n" +
"                }\n" +
"            }\n" +
"\n" +
"            string newText = Interaction.InputBox(message,title, \n" +
"{vertexTemp.Text}, -1, -1);\n" +
"\n" +
"            while (!CheckIfUnique(newText) || newText == )\n" +
"            {\n" +
"                if (vertexTemp.Text == newText)\n" +
"                {\n" +
"                    break;\n" +
"                }\n" +
"                newText = Interaction.InputBox(message,title, \n" +
"{vertexTemp.Text}, -1, -1);\n" +
"            }\n" +
"\n" +
"            vertexTemp.Text = newText;\n" +
"            vertexTemp.NotifyChanged(Text);\n" +
"\n";
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            pause = !pause;
        }

        private void LoadGraphNames()
        {
            DataTable dt = SqlConnector.ReadGraphs();

            foreach (DataRow dr in dt.Rows)
            {
                graphNames.Add(dr["GraphName"].ToString());
            }
        }

        private void btnLoadGraph_Click(object sender, RoutedEventArgs e)
        {
            if (cbbLoad.SelectedItem != null && !bfsOn && !dfsOn)
            {
                Liberate();
                graphName = cbbLoad.SelectedItem.ToString();
                tbGraphName.Text = graphName;
                DataTable dt = SqlConnector.ReadVertices(graphName);

                foreach (DataRow dr in dt.Rows)
                {
                    SampleVertex vertex = new SampleVertex(dr["Vertex"].ToString());
                    string adjecencies = dr["Edges"].ToString();
                    List<string> adjecenciesList = adjecencies?.Split(", ").ToList();

                    foreach (string adj in adjecenciesList)
                    {
                        if (adj != "")
                        {
                            AddEdge(vertex, new SampleVertex(adj)); 
                        }
                    }
                }

                dt = SqlConnector.ReadGraphTimes(graphName);
                foreach (DataRow dr in dt.Rows)
                {
                    tbElapsedDFS.Text = "DFS Elapsed Time: " + dr["DFSTime"].ToString();
                    tbElapsedBFS.Text = "BFS Elapsed Time: " + dr["BFSTime"].ToString();

                    elapsedTimeDFS = dr["DFSTime"].ToString();
                    elapsedTimeBFS = dr["BFSTime"].ToString();
                }

                foreach (SampleVertex v in verticesList)
                {
                    if (v.Active)
                    {
                        v.Change();
                    }
                }

                lSearchOutput.Content = "";
            }
        }

        private void btnSaveGraph_Click(object sender, RoutedEventArgs e)
        {
            graphName = tbGraphName.Text;
            if (graphNames.Contains(tbGraphName.Text) && graph.EdgeCount != 0 && MessageBox.Show($"Are you sure you want to update the graph: {graphName}", "Graph Update", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SqlConnector.Delete(graphName);
                SqlConnector.Write(verticesList, graphName, elapsedTimeDFS, elapsedTimeBFS);
                MessageBox.Show($"Graph {graphName} has been successfully Updated");
            }
            else if (tbGraphName.Text != "" && graph.EdgeCount != 0)
            {
                SqlConnector.Write(verticesList, graphName, elapsedTimeDFS, elapsedTimeBFS);
                graphNames.Clear();
                LoadGraphNames();
                cbbLoad.ItemsSource = null;
                cbbLoad.ItemsSource = graphNames;
                MessageBox.Show($"Graph {graphName} has been successfully Saved");
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"The documentation will be printed immediately to your default printer", "Printing", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string filePath = $"{path}GraphDocumentation.pdf";
                PrintJob printJob = new PrintJob(Printer.Default, filePath);
                printJob.Print(); 
            }  
        }
    }
}

