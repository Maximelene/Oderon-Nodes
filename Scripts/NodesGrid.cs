using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OderonNodes
{
    public class NodesGrid : MonoBehaviour
    {
        #region Variables
        #pragma warning disable 649
        [SerializeField] GameObject nodePrefab;
        #pragma warning restore 649

        List<GameObject> nodesToKeep = new List<GameObject>();
        public List<GameObject> nodesList = new List<GameObject>();
        Dictionary<Vector3, GameObject> nodesCoordinates = new Dictionary<Vector3, GameObject>();

        public GridParameters gridParameters = new GridParameters();
        #endregion

        /*public void InitializeGrid()
        {
            foreach (Node joint in GetComponentsInChildren<Node>())
            {

            }
        }*/

        void Start()
        {
            // Move the grid to a more "usable" in-game position 
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, 0.2f, pos.z);
        }

        #region Grid Generation
        public void GenerateGrid()
        {
            // Refresh the lists
            RefreshNodesList();
            nodesToKeep.Clear();

            // Generate the Nodes
            if (gridParameters.type == GridType.Vertical)
            {
                for (int i = 0; i < gridParameters.width + (Mathf.Floor(gridParameters.height / 2)); i++)
                {
                    // Calculate the Z Shift
                    float zShift = i * -0.9f;
                    if (i > gridParameters.width - 1)
                    {
                        zShift += (i - gridParameters.width + 1) * 3.6f;

                        SpawnHorizontalLine(gridParameters.height - ((i - gridParameters.width + 1) * 2), i * -NodesManager.nodesColumnDifference, zShift, (i * gridParameters.width) + 1);
                    }
                    else
                    {
                        SpawnHorizontalLine(Mathf.Clamp(i * 2 + 1, 1, gridParameters.height), i * -NodesManager.nodesColumnDifference, i * -0.9f, (i * gridParameters.width) + 1);
                    }
                }
            }
            else
            {
                for (int i = 0; i < gridParameters.height; i++)
                {
                    // Calculate the Z Shift
                    float zShift = 0;
                    if (i % 2 == 1) { zShift = 0.9f; }

                    SpawnHorizontalLine(gridParameters.width, i * -NodesManager.nodesColumnDifference, zShift, (i * gridParameters.width) + 1);
                }
            }

            // Destroy all the existing nodes that shouldn't be kept (when the grid size is reduced)
            List<GameObject> nodesToDestroy = new List<GameObject>();
            foreach (GameObject node in nodesList) { if (!nodesToKeep.Contains(node)) { nodesToDestroy.Add(node); } }
            if (nodesToDestroy.Count > 0) { foreach (GameObject nodeToDestroy in nodesToDestroy) { DestroyImmediate(nodeToDestroy); } }
        }

        void SpawnHorizontalLine(int nodesCount, float xShift = 0, float zShift = 0, int startingNumber = 1)
        {
            for (int i = 0; i < nodesCount; i++)
            {
                // Calculate position
                Vector3 position = new Vector3(xShift, 0, (i * NodesManager.nodesLineDifference) + zShift);

                if (!nodesCoordinates.ContainsKey(position))
                {
                    GameObject newNode = PrefabUtility.InstantiatePrefab(nodePrefab, transform) as GameObject;
                    newNode.transform.localPosition = position;
                    newNode.name = "Node " + (startingNumber + i);

                    nodesList.Add(newNode);
                    nodesCoordinates[position] = newNode;

                    // Make sure to keep that node
                    nodesToKeep.Add(newNode);
                }
                else
                {
                    // The node already exists: add it to the list of nodes to keep
                    nodesToKeep.Add(nodesCoordinates[position]);
                }
            }
        }

        void RefreshNodesList()
        {
            nodesList.Clear();
            nodesCoordinates.Clear();

            foreach (Node node in GetComponentsInChildren<Node>())
            {
                nodesList.Add(node.gameObject);
                nodesCoordinates[node.gameObject.transform.localPosition] = node.gameObject;
            }
        }

        public void ClearGrid()
        {
            RefreshNodesList();

            foreach (GameObject node in nodesList) { DestroyImmediate(node); }

            nodesList.Clear();
            nodesCoordinates.Clear();

            //foreach (Transform child in transform) { DestroyImmediate(child.gameObject); }
        }
        #endregion

        #region Classes & Enums
        [System.Serializable]
        public class GridParameters
        {
            public GridType type = GridType.Horizontal;
            public int width = 15;
            public int height = 15;
        }

        public enum GridType { Horizontal, Vertical }
        #endregion
    }
}