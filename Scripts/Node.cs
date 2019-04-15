using System.Collections.Generic;
using UnityEngine;

namespace OderonNodes
{
    public class Node : MonoBehaviour
    {
        #region Variables
        [Header("Parameters")]
        public NodeParameters parameters;

        [Header("Position")]
        [HideInInspector] public Coordinates coordinates;
        [HideInInspector] public CubeCoordinates cubeCoordinates;
        [HideInInspector] public bool isOnAShiftedColumn = false;
        [HideInInspector] public List<Node> neighbours = new List<Node>();

        [Header("Status")]
        [SerializeField] Entity occupant = null;
        public List<Entity> alteringEntities = new List<Entity>(); // List of entities "aletring" the Node (having an effet on it, that can be triggered by other entities entering the node)

        [Header("Meshes & Materials")]
        public NodeMeshes meshes;
        public NodeMaterials materials;

        // Components
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;

        // Parameters
        
        #endregion

        #region Common Methods
        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            HighlightNode(HighlightColors.Default);
        }

        void Start()
        {
            Initialize();
        }
        #endregion

        #region Initialize
        public void Initialize()
        {
            // If the node is impassable, hide it
            if (parameters.terrainType == TerrainType.Impassable)
            { meshRenderer.enabled = false; }

            // Apply the Border Mesh (the Full Mesh is more practical in the Edirot, the Border Mesh is better in-game)
            // TODO: let Player choose the Node mesh, and their alpha level
            meshFilter.mesh = meshes.bordersMesh;
        }
        #endregion

        #region Coordinates
        // Function calculating the coordinates of that node (based on an initial node)
        public CubeCoordinates CalculateNodeCoordinates(GameObject initialNode)
        {
            #region Offset coordinates
            // COLUMN
            // Get the difference in X axis between this node and the initial one
            float xDifference = gameObject.transform.localPosition.x - initialNode.transform.localPosition.x;
            coordinates.column = Mathf.RoundToInt(xDifference / NodesManager.nodesColumnDifference);

            // if the X coordinate is odd, the node is on a shifted column
            if (coordinates.column % 2 != 0)
            { isOnAShiftedColumn = true; }

            // LINE
            // Get the difference in Z axis between this node and the initial one
            float zDifference = gameObject.transform.localPosition.z - initialNode.transform.localPosition.z;
            // If the node is on a shifted row, substract half the difference, to simulate the node being on the same line
            if (isOnAShiftedColumn)
            { zDifference -= (NodesManager.nodesLineDifference / 2); }
            // Now, calculate the coordinate
            coordinates.row = Mathf.RoundToInt(zDifference / NodesManager.nodesLineDifference);
            #endregion

            #region Cube coordinates
            // X COORDINATE
            cubeCoordinates.x = coordinates.column;

            // Z COORDINATE
            cubeCoordinates.z = Mathf.RoundToInt(coordinates.row - ((coordinates.column - (Mathf.Abs(coordinates.column) % 2)) / 2));

            // Y COORDINATE
            cubeCoordinates.y = Mathf.RoundToInt(-cubeCoordinates.x - cubeCoordinates.z);
            #endregion

            return cubeCoordinates;
        }
        #endregion

        #region Occupying Node
        public void OccupyNode(GameObject newOccupant)
        {
            occupant = newOccupant.GetComponent<Entity>();
        }

        public void FreeNode()
        {
            occupant = null;
        }
        #endregion


        





        #region Movement
        // Function called by a character entering the node
        public void EnterNode(Entity entity)
        {
            foreach (Entity alteringEntity in alteringEntities)
            {
                alteringEntity.EntityEntersAlteredNode(entity);
            }
        }

        // Function called when a character finishes its turn on the node
        public void EndTurnOnNode(Entity entity)
        {
            foreach (Entity alteringEntity in alteringEntities)
            {
                alteringEntity.EntityFinishesTurnOnNode(entity);
            }
        }

        // Function determining the cost to cross that tile (meaning the cost to go from another tile to that one)
        public float CostToEnter()
        {
            // Create the variable
            float costToEnter = 1f;

            // If the node is occupied, it can't be crossed
            if (IsOccupied || parameters.terrainType == TerrainType.Impassable)
            { costToEnter = Mathf.Infinity; }
            // If not, it depends from the type of terrain
            else
            {
                switch (parameters.terrainType)
                {
                    case TerrainType.Medium:
                        costToEnter = 2f;
                        break;
                    case TerrainType.Hard:
                        costToEnter = 3f;
                        break;
                    case TerrainType.Impassable:
                        costToEnter = Mathf.Infinity;
                        break;
                    default:
                        costToEnter = 1f;
                        break;
                }
            }

            return costToEnter;
        }
        #endregion

        #region Highlights
        public void HighlightNode(HighlightColors color)
        {
            // Create the variables
            Material material = materials.defaultMaterial;

            // Select which material to use
            switch (color)
            {
                case HighlightColors.White:
                    material = materials.whiteMaterial;
                    break;
                case HighlightColors.Green:
                    material = materials.greenMaterial;
                    break;
                case HighlightColors.Yellow:
                    material = materials.yellowMaterial;
                    break;
                case HighlightColors.Red:
                    material = materials.redMaterial;
                    break;
                case HighlightColors.LightBlue:
                    material = materials.lightBlueMaterial;
                    break;
                case HighlightColors.Blue:
                    material = materials.blueMaterial;
                    break;
                case HighlightColors.Highlighted:
                    material = materials.highlightedMaterial;
                    break;
                default:
                    break;
            }

            // Apply the selected material
            meshRenderer.material = material;
        }

        public void UnHighlightNode()
        {
            if (meshRenderer && materials.defaultMaterial)
            { meshRenderer.sharedMaterial = materials.defaultMaterial; }
        }
        #endregion

        #region Getters & Setters
        public bool IsOccupied { get { if (occupant != null) { return true; } else { return false; } } }
        public Entity Occupant { get { return occupant; } }
        #endregion

        #region Classes & Enums
        [System.Serializable]
        public class Coordinates
        {
            public float column = 0;
            public float row = 0;
        }

        // Cube coordinates imagine that hexes are part of a cube. These coordinates make for easy calculations, and are calculated for each node during map generation
        [System.Serializable]
        public class CubeCoordinates
        {
            public float x = 0;
            public float y = 0;
            public float z = 0;
        }

        [System.Serializable]
        public class NodeParameters
        {
            public TerrainType terrainType = TerrainType.Normal;
            public bool blocksLineOfSight = false;
        }

        [System.Serializable]
        public class NodeMeshes
        {
            public Mesh fullMesh;
            public Mesh bordersMesh;
        }

        [System.Serializable]
        public class NodeMaterials
        {
            public Material defaultMaterial;
            public Material whiteMaterial;
            public Material greenMaterial;
            public Material yellowMaterial;
            public Material redMaterial;
            public Material lightBlueMaterial;
            public Material blueMaterial;
            public Material highlightedMaterial;
        }

        public enum TerrainType
        { Normal, Medium, Hard, Impassable }

        public enum HighlightColors
        { Default, White, Green, Yellow, Red, LightBlue, Blue, Highlighted }
        #endregion
    }
}