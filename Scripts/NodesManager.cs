using System.Collections.Generic;
using UnityEngine;

namespace OderonNodes
{
    public class NodesManager : MonoBehaviour
    {
        // References: https://www.redblobgames.com/grids/hexagons
        // System used: odd-q (Offset coordinates)

        #region Variables
        // Lists
        List<Node> nodesList = new List<Node>(); // List of all the nodes (without any other information)
        Dictionary<Node.CubeCoordinates, Node> nodesCubeCoordinates = new Dictionary<Node.CubeCoordinates, Node>(); // List of all the cube coordinates, with the associated node

        // Status
        public Node currentlySelectedNode = null;
        #endregion

        #region Nodes Initialization
        // Function calculating the coordinates of every node
        public void InitializeNodes()
        {
            // Create the variables
            GameObject initialNode = null;
            int i = 1;

            // Go through each Node
            foreach (Node node in FindObjectsOfType<Node>())
            {
                // If no initial Node has been determined, use this one
                if (!initialNode)
                {
                    // Set the node as initial
                    initialNode = node.gameObject;

                    // Set the coordinates at 0
                    node.coordinates.column = 0;
                    node.coordinates.row = 0;
                }

                // Calculate the node's coordinates
                Node.CubeCoordinates cubeCoordinates = node.CalculateNodeCoordinates(initialNode);

                // Rename the node
                node.gameObject.name = "Node " + i;
                i++;

                // Register the node
                nodesList.Add(node);
                nodesCubeCoordinates[cubeCoordinates] = node;
            }

            // Build the neighbour's graph
            BuildNeighboursGraph();
        }
        #endregion

        #region Highlighting
        // Function highlighting a specific list of nodes
        public void HighlightNodes(List<Node> nodes, Node.HighlightColors color, Node nodeToExclude = null)
        {
            foreach (Node node in nodes)
            {
                if (node != nodeToExclude)
                { node.HighlightNode(color); }
            }
        }

        // Function un-highlighting every node
        public void UnHighlightAllNodes()
        {
            foreach (Node node in nodesList)
            { node.UnHighlightNode(); }
        }
        #endregion

        #region Movement Methods
        // Calculate the best path to move from a node to another. Returns a Path class, including the path itself, and its cost
        public Path MovementPath(Node sourceNode, Node targetNode)
        {
            // Using the Dijkstra algorithm (tutorial used: https://www.youtube.com/watch?v=QhaKb5N3Hj8&list=PLbghT7MmckI55gwJLrDz0UtNfo9oC0K1Q&index=5)
            // Create the return Path
            Path returnPath = new Path();

            // Create the dictionaries listing the distance to the nodes, and the previous node used
            Dictionary<Node, float> dist = new Dictionary<Node, float>();
            Dictionary<Node, Node> prev = new Dictionary<Node, Node>();

            // Set up the list of Nodes not checked yet
            List<Node> unvisited = new List<Node>();

            // Set the source node informations
            dist[sourceNode] = 0;
            prev[sourceNode] = null;

            // Initialize every node to have an infinite distance
            foreach (Node node in nodesList)
            {
                if (node != sourceNode)
                {
                    dist[node] = Mathf.Infinity;
                    prev[node] = null;
                }

                unvisited.Add(node);
            }

            while (unvisited.Count > 0)
            {
                // Check the node with the current smallest distance (the source node at first)
                // Commented here is another way to do it, probably slower
                // Node nodeToCheck = unvisited.OrderBy(n => dist[n]).First();
                Node nodeToCheck = null;
                foreach (Node possibleNodeToCheck in unvisited)
                {
                    if (nodeToCheck == null || dist[possibleNodeToCheck] < dist[nodeToCheck])
                    { nodeToCheck = possibleNodeToCheck; }
                }

                if (nodeToCheck == targetNode)
                { break; }

                // Now that we're visiting this node, remove it from the "unvisited" list
                unvisited.Remove(nodeToCheck);

                // Check all the neighbors of that node
                foreach (Node neighbour in nodeToCheck.neighbours)
                {
                    // Create the variables
                    float alt = dist[nodeToCheck];

                    // Calculate the distance to get to that tile
                    alt += neighbour.CostToEnter();

                    // The path we calculated to reach that node is shorter than others already found: register it at the new shorter path
                    if (alt < dist[neighbour])
                    {
                        dist[neighbour] = alt;
                        prev[neighbour] = nodeToCheck;
                    }
                }
            }

            // We found a path to the target node
            if (dist[targetNode] != Mathf.Infinity)
            {
                List<Node> currentPath = new List<Node>();

                // From the target node, step through the "prev" chain, adding each node to the path, essentially constructing it backwards
                Node currentNode = targetNode;
                while (currentNode != null)
                {
                    currentPath.Add(currentNode);
                    currentNode = prev[currentNode];
                }

                // Reverse the Nodes list
                currentPath.Reverse();

                // Set the Return Path informations
                returnPath.path = currentPath;
                returnPath.cost = dist[targetNode];
            }
            // No path was found
            else
            {
                // Set the Return Path informations
                returnPath.path = null;
                returnPath.cost = Mathf.Infinity;
            }

            return returnPath;
        }

        // List all the accessible tiles from a specific tile, with a defined movement range
        public List<Node> ListAccessibleTiles(Node source, float range)
        {
            // Create the variables
            List<Node> returnList = new List<Node>();
            Dictionary<Node, float> accessibleNodes = new Dictionary<Node, float>();
            bool noPathFound = false; // Once no more paths are found, this variable will be turned to "true", stopping the analysis

            // Add the source node to the list, with a distance of 0
            accessibleNodes[source] = 0;

            // Create the list of nodes to check (starting with only the source)
            List<Node> nodesToCheck = new List<Node>();
            nodesToCheck.Add(source);

            while (noPathFound == false)
            {
                // Set "no path found as true". If no path is found, it will stay "true".
                noPathFound = true;

                // List the nodes to add to the "nodes to check" list
                List<Node> nodesToAdd = new List<Node>();

                foreach (Node analyzedNode in nodesToCheck)
                {
                    // Get this node's neighbours
                    foreach (Node neighbour in analyzedNode.neighbours)
                    {
                        // Calculate the total cost to get to this neighbour
                        float cost = accessibleNodes[analyzedNode] + neighbour.CostToEnter();

                        // Make sure the node can be accessed
                        if (cost != Mathf.Infinity)
                        {
                            // If the cost is under the range (and is a better path than already found), register this node as accessible, and add it to the nodes to check in a next pass
                            if (cost <= range && !accessibleNodes.ContainsKey(neighbour) || cost <= range && cost < accessibleNodes[neighbour])
                            {
                                accessibleNodes[neighbour] = cost;
                                nodesToAdd.Add(neighbour);

                                // A pass has been found
                                noPathFound = false;
                            }
                        }
                    }
                }

                // Clear the "nodes to check" list, since we checked them
                nodesToCheck.Clear();

                // Add the new nodes to check to the list
                if (nodesToAdd.Count > 0)
                {
                    foreach (Node node in nodesToAdd)
                    { nodesToCheck.Add(node); }
                    nodesToAdd.Clear();
                }
                else
                { noPathFound = true; }
            }

            // Go through each node deemed accessible, and add it to the list
            foreach (KeyValuePair<Node, float> accessibleNode in accessibleNodes)
            { returnList.Add(accessibleNode.Key); }

            return returnList;
        }

        // Highlight the tiles that are accessible from a specific tile, with a defined movement range
        public void DisplayMovementRange(Node source, float range)
        {
            // Generate the list of accessible tiles
            List<Node> accessibleTiles = ListAccessibleTiles(source, range);

            // Highlight these nodes
            HighlightNodes(accessibleTiles, Node.HighlightColors.White, source);
        }

        // Function finding the most accessible tile from a list of tiles
        // TODO: unused?
        public void MostAccessibleTile(Node sourceNode, List<Node> sampleList, out Node returnNode, out float returnCost)
        {
            // Create the variables
            Node selectedNode = null;
            float selectedNodeCostToReach = Mathf.Infinity;

            // Go through each node from the sample
            foreach (Node node in sampleList)
            {
                // Make sure that node can be entered
                if (node.CostToEnter() != Mathf.Infinity)
                {
                    // Generate the path to that node
                    float cost = MovementPath(sourceNode, node).cost;

                    // If the cost to reach that node is less than the currently isolated node, keep that node
                    if (cost < selectedNodeCostToReach)
                    {
                        selectedNode = node;
                        selectedNodeCostToReach = cost;
                    }
                }
            }

            // If a node has been selected, return it. If not, return nothing.
            if (selectedNode && selectedNodeCostToReach < Mathf.Infinity)
            {
                returnNode = selectedNode;
                returnCost = selectedNodeCostToReach;
            }
            else
            {
                returnNode = null;
                returnCost = Mathf.Infinity;
            }
        }
        #endregion

        #region Distance & Range Methods
        // Calculate the distance between two nodes, using Cube Coordinates (the distance between a node and one of its neighbours is 1) - https://www.redblobgames.com/grids/hexagons/#distances
        public static int Distance(Node sourceNode, Node targetNode)
        {
            // Get the nodes' coordinates
            Node.CubeCoordinates sourceCoordinates = sourceNode.cubeCoordinates;
            Node.CubeCoordinates targetCoordinates = targetNode.cubeCoordinates;

            // Calculate the distance
            return Mathf.RoundToInt((Mathf.Abs(sourceCoordinates.x - targetCoordinates.x) + Mathf.Abs(sourceCoordinates.y - targetCoordinates.y) + Mathf.Abs(sourceCoordinates.z - targetCoordinates.z)) / 2);
        }

        // List all the nodes in a specific range of a specific node
        public List<Node> NodesInRange(Node source, float minRange, float maxRange)
        {
            // Create the variable
            List<Node> nodesInRange = new List<Node>();

            // Go through each node
            foreach (Node node in nodesList)
            {
                // Calculate the distance
                float distance = Distance(source, node);

                if (distance <= maxRange && distance >= minRange)
                { nodesInRange.Add(node); }
            }

            return nodesInRange;
        }
        #endregion

        #region Line of Sight Methods
        // Function testing if there is a clear line of sight between two nodes
        public static bool IsTargetNodeVisible(Node sourceNode, Node targetNode)
        {
            if (!targetNode.parameters.blocksLineOfSight)
            {
                // get the Nodes list
                List<Node> nodesList = GetNodesManager().NodesList;

                // Create the variables
                bool foundNodeBlockingSight = false;

                // Calculate the distance between the two nodes
                float distance = Distance(sourceNode, targetNode);

                // If the target node and the source node are neighbours, they are automatically visible from each other
                if (distance == 1)
                { return true; }
                // If not, trace a line between the two nodes, checking the nodes entered to get from one to another. If at least one of these nodes block line of sight, the target node isn't visible.
                else
                {
                    // Calculate the fraction of that Vector where to locate each point
                    float fraction = 1 / distance;
                    float currentFraction = fraction;
                    while (currentFraction < 1)
                    {
                        // Calculate the point's location
                        Vector3 point = Vector3.Lerp(sourceNode.transform.position, targetNode.transform.position, currentFraction);

                        // Find the nearest node to that point
                        #region Nearest node
                        // Create the variables
                        Node nearestNode = null;
                        float nearestNodeDistance = Mathf.Infinity;

                        // Go through each node
                        foreach (Node node in nodesList)
                        {
                            // Get the distance between the point and the node
                            float distanceToPoint = Vector3.Distance(point, node.transform.position);

                            // If the node blocks line of sight, include a variance
                            if (node.parameters.blocksLineOfSight && distanceToPoint < (nearestNodeDistance * 1.1f))
                            {
                                nearestNode = node;
                                nearestNodeDistance = distanceToPoint;
                            }
                            // If the node doesn't block line of sight, it just has to be the nearest
                            else if (distanceToPoint < nearestNodeDistance)
                            {
                                nearestNode = node;
                                nearestNodeDistance = distanceToPoint;
                            }
                        }
                        #endregion

                        // If that node blocks the line of sight
                        if (nearestNode.parameters.blocksLineOfSight)
                        {
                            foundNodeBlockingSight = true;
                            break;
                        }

                        currentFraction += fraction;
                    }

                    return !foundNodeBlockingSight;
                }
            }
            // If the node blocks line of sight, it isn't visible by itself
            else
            { return false; }
        }
        #endregion

        #region Attack Methods
        // List all the nodes that can be attacked from a specific node, with a defined attack range
        public List<Node> AttackableNodes(Node source, float minRange, float maxRange)
        {
            // Create the list
            List<Node> attackableNodes = new List<Node>();

            // List all the nodes in range
            List<Node> nodesInRange = NodesInRange(source, minRange, maxRange);

            // Check each node for line of sight
            foreach (Node node in nodesInRange)
            {
                // Make sure the node isn't Impassable (impassable nodes can't be entered, so they will never need to be attacked)
                if (node.parameters.terrainType != Node.TerrainType.Impassable)
                {
                    // Check for Line of Sight
                    if (IsTargetNodeVisible(source, node))
                    { attackableNodes.Add(node); }
                }
            }

            return attackableNodes;
        }
        #endregion

        public void BuildNeighboursGraph()
        {
            // Go through each node
            foreach (Node initialNode in nodesList)
            {
                // Go through each other node, to find the neighbours
                foreach (Node node in nodesList)
                {
                    if (node != initialNode)
                    {
                        // Calculate the differences in each axis
                        float xDifference = node.coordinates.column - initialNode.coordinates.column;
                        float yDifference = node.coordinates.row - initialNode.coordinates.row;

                        // The node is on the same row, with only a difference of 1 in its line
                        if (xDifference == 0 && yDifference == 1 || xDifference == 0 && yDifference == -1)
                        { initialNode.neighbours.Add(node); }
                        // The node is in a neigbouring row, but on the same line
                        else if (xDifference == 1 && yDifference == 0 || xDifference == -1 && yDifference == 0)
                        { initialNode.neighbours.Add(node); }
                        // The node is in a neighbouring row, but on the previous line (for tiles that aren't shifted)
                        else if (xDifference == -1 && yDifference == -1 && initialNode.isOnAShiftedColumn == false || xDifference == 1 && yDifference == -1 && initialNode.isOnAShiftedColumn == false)
                        { initialNode.neighbours.Add(node); }
                        // The node is in a neighbouring row, but on the next line (for tiles that are shifted)
                        else if (xDifference == -1 && yDifference == 1 && initialNode.isOnAShiftedColumn == true || xDifference == 1 && yDifference == 1 && initialNode.isOnAShiftedColumn == true)
                        { initialNode.neighbours.Add(node); }
                    }
                }
            }
        }

        // Method getting the current active Nodes Manager
        public static NodesManager GetNodesManager()
        {
            var controllers = FindObjectsOfType<NodesManager>();

            if (controllers.Length > 1) { Debug.LogError("WARNING: You have more than one Node Managers active"); }

            return controllers[0];
        }

        #region getters & Setters
        public List<Node> NodesList { get { return nodesList; } }
        #endregion
    }

    #region Classes & Enums
    // A path is determined by the list of nodes to cross, and its total cost
    public class Path
    {
        public List<Node> path = new List<Node>();
        public float cost = Mathf.Infinity;
    }
    #endregion
}