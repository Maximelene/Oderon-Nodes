using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace OderonNodes
{
    public class Entity : MonoBehaviour
    {
        #region Variables
        [Header("Position")]
        public Node node;

        [Header("Movement")]
        [SerializeField] int movementLeft = 3;
        MovementWaypoint nextWaypoint;

        // Components
        NavMeshAgent navMeshAgent;
        #endregion

        #region Common Methods
        void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        void Update()
        {
            ProcessMovement();
        }
        #endregion

        #region Nodes interaction
        // Function checking for all the nodes, and finding the nearest one
        public Node FindNearestNode()
        {
            // Create the variables
            Node nearestNode = null;
            float nearestNodeDistance = Mathf.Infinity;

            // Go through each node
            foreach (Node node in FindObjectsOfType<Node>())
            {
                // Make sure the node is not impassable
                if (node.parameters.terrainType != Node.TerrainType.Impassable)
                {
                    // Get the distance between the entity and the node
                    float distance = Vector3.Distance(gameObject.transform.position, node.transform.position);

                    // If that node is the nearest one found yet, memorize it
                    if (distance < nearestNodeDistance)
                    {
                        nearestNode = node;
                        nearestNodeDistance = distance;
                    }
                }
            }

            return nearestNode;
        }

        // Function warping the entity to a specific node
        public void WarpToNode(Node nodeToWarpTo = null, bool occupyNode = true)
        {
            // If no node was specified, use the one currently used by the entity (or the nearest one if no node is currently used)
            if (!nodeToWarpTo)
            {
                if (node)
                { nodeToWarpTo = node; }
                else
                { nodeToWarpTo = FindNearestNode(); }
            }

            // Register the node
            node = nodeToWarpTo;

            if (node != null)
            {
                // If the character is already there, don't do anything
                if (transform.position.x != nodeToWarpTo.transform.position.x || transform.position.z != nodeToWarpTo.transform.position.z)
                {
                    // Determine the position to warp to
                    Vector3 warpTo = new Vector3(nodeToWarpTo.transform.position.x, 0, nodeToWarpTo.transform.position.z);

                    // Get the NavMeshAgent component
                    NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();

                    // If the NavMeshAgent component exists, use the Warp function
                    if (navMeshAgent)
                    { navMeshAgent.Warp(warpTo); }
                    // If not, update the transform
                    else
                    { gameObject.transform.position = warpTo; }
                }
            }

            // Occupy the node
            if (occupyNode)
            { nodeToWarpTo.OccupyNode(gameObject); }
        }
        #endregion

        #region Movement
        // Make the character move towards a specific destination. If the character has not enough movement to reach it, it will get as near to the destination as possible
        public void MoveTo(Node destination, List<Node> path)
        {
            // Create the variables
            int createdWaypoints = 0;
            MovementWaypoint firstCreatedWaypoint = null;
            MovementWaypoint previousCreatedWaypoint = null; // Store the previously created waypoint (to update its informations after the next one is created)

            // Create the Movement Waypoints
            foreach (Node node2 in path)
            {
                // Make sure to not create a waypoint on the current node, and to not create more waypoints than required
                if (node != node2)
                {
                    // Create the Waypoint
                    MovementWaypoint createdMovementWaypoint = new GameObject().AddComponent<MovementWaypoint>();
                    createdMovementWaypoint.gameObject.transform.position = new Vector3(node2.transform.position.x, 0, node2.transform.position.z);

                    // Set the waypoint informations
                    createdMovementWaypoint.associatedCharacter = gameObject;
                    createdMovementWaypoint.node = node2;

                    // Set this newly created waypoint as the last waypoint's next waypoint
                    if (previousCreatedWaypoint)
                    { previousCreatedWaypoint.GetComponent<MovementWaypoint>().nextWaypoint = createdMovementWaypoint; }

                    // Store this waypoint as the new "previous" one
                    previousCreatedWaypoint = createdMovementWaypoint;

                    // If this is the first created waypoint, store it
                    if (!firstCreatedWaypoint)
                    { firstCreatedWaypoint = createdMovementWaypoint; }

                    // Increase the number of waypoints created (to limit movement to the desired number)
                    createdWaypoints++;
                }
            }

            // If at least a waypoint has been created, set the character destination, and reduce its movement left
            if (createdWaypoints > 0)
            {
                nextWaypoint = firstCreatedWaypoint;

                // Start moving
                MoveToWaypoint();
            }
        }

        public void MoveToWaypoint()
        {
            startMovingObservers?.Invoke();
            navMeshAgent.SetDestination(nextWaypoint.transform.position);
        }

        public void ProcessMovement()
        {
            if (nextWaypoint)
            {
                // Get the waypoint informations
                MovementWaypoint movementWaypoint = nextWaypoint.GetComponent<MovementWaypoint>();

                // Get the real distance between the character and its current node, and between the character and its next node
                float currentNodeDistance = Vector3.Distance(node.transform.position, transform.position);
                float nextWaypointDistance = Vector3.Distance(nextWaypoint.transform.position, transform.position);

                // If the character is nearer from its next node than from its current one, process the position change
                if (nextWaypoint.GetComponent<MovementWaypoint>().node != node && nextWaypointDistance <= currentNodeDistance)
                {
                    // Free the previous node
                    node.FreeNode();

                    // Update the character's position
                    node = movementWaypoint.node;

                    // Reduce movement left
                    movementLeft = Mathf.Clamp(movementLeft - Mathf.RoundToInt(movementWaypoint.node.CostToEnter()), 0, 100);

                    enterNewNodeObservers?.Invoke(movementWaypoint.node.CostToEnter());

                    // Occupy the node
                    node.OccupyNode(gameObject);
                }

                // If the character is near enough from the waypoint to "reconsider" it (see if it should go to the next one), do it
                if (nextWaypointDistance <= 0.3f)
                {
                    // Trigger the fact that the character entered a Node
                    nextWaypoint.GetComponent<MovementWaypoint>().node.EnterNode(this);

                    // Check to see if the character should move to another waypoint
                    if (movementWaypoint.nextWaypoint)
                    {
                        nextWaypoint = movementWaypoint.nextWaypoint;
                        MoveToWaypoint();
                    }
                    // This is the final waypoint
                    else
                    {
                        nextWaypoint = null;
                        stopMovingObservers?.Invoke();
                    }

                    // Destroy the waypoint
                    Destroy(movementWaypoint.gameObject);
                }
                else
                { MoveToWaypoint(); }
            }
        }
        #endregion

        #region Cursor hovering
        public void OnCursorEnter() { cursorEnterObservers?.Invoke(); }
        public void OnCursorExitMethod() { cursorExitObservers?.Invoke(); }
        #endregion

        #region Altering Nodes
        // Method called when an entity enters a node altered by this entity
        public void EntityEntersAlteredNode(Entity entity) { entityEntersNodeAlteredByThisEntity(entity); }

        // Method called when an entity finishes its turn on a node altered by this entity
        public void EntityFinishesTurnOnNode(Entity entity) { entityFinishesTurnOnNodeAlteredByThisEntity(entity); }
        #endregion

        #region Delegates
        public delegate void OnCursorEnterDelegate(); public OnCursorEnterDelegate cursorEnterObservers;
        public delegate void OnCursorExitDelegate(); public OnCursorExitDelegate cursorExitObservers;

        public delegate void OnStartMovingDelegate(); public OnStartMovingDelegate startMovingObservers;
        public delegate void OnStopMovingDelegate(); public OnStopMovingDelegate stopMovingObservers;

        public delegate void OnEnterNewNodeDelegate(float costToEnter); public OnEnterNewNodeDelegate enterNewNodeObservers;

        public delegate void OnEntityEntersNodeAlteredByThisEntityDelegate(Entity entity); public OnEntityEntersNodeAlteredByThisEntityDelegate entityEntersNodeAlteredByThisEntity;
        public delegate void OnEntityFinishesTurnOnNodeAlteredByThisEntityDelegate(Entity entity); public OnEntityFinishesTurnOnNodeAlteredByThisEntityDelegate entityFinishesTurnOnNodeAlteredByThisEntity;
        #endregion

        #region Getters & Setters
        public int MovementLeft { get { return movementLeft; } set { movementLeft = value; } }
        #endregion
    }
}