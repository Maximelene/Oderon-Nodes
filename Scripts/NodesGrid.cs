using System.Collections.Generic;
using UnityEngine;

namespace OderonNodes
{
    public class NodesGrid : MonoBehaviour
    {
        [SerializeField] GameObject nodePrefab;

        public List<GameObject> nodesList = new List<GameObject>();
        public Dictionary<Vector3, GameObject> nodesAlreadyExisting = new Dictionary<Vector3, GameObject>();

        public GridParameters gridParameters = new GridParameters();

        /*public void InitializeGrid()
        {
            foreach (Node joint in GetComponentsInChildren<Node>())
            {

            }
        }*/


        void Start()
        {

            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, 0.2f, pos.z);
        }

        public void GenerateGrid()
        {
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
        }

        void SpawnHorizontalLine(int nodesCount, float xShift = 0, float zShift = 0, int startingNumber = 1)
        {
            for (int i = 0; i < nodesCount; i++)
            {
                // Calculate position
                Vector3 position = new Vector3(xShift, 0, (i * NodesManager.nodesLineDifference) + zShift);

                if (!nodesAlreadyExisting.ContainsKey(position))
                {
                    GameObject newNode = Instantiate(nodePrefab, transform);
                    newNode.transform.localPosition = position;
                    newNode.name = "Node " + (startingNumber + i);

                    nodesList.Add(newNode);
                    nodesAlreadyExisting[position] = newNode;
                }
            }
        }

        public void ClearGrid()
        {
            foreach (GameObject node in nodesList) { DestroyImmediate(node); }

            nodesList.Clear();
            nodesAlreadyExisting.Clear();

            foreach (Transform child in transform) { Destroy(child.gameObject); }
        }

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