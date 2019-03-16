using UnityEngine;
using OderonNodes;

public class MovementWaypoint : MonoBehaviour
{
    #region Variables
    public GameObject associatedCharacter = null;
    public MovementWaypoint nextWaypoint = null;

    [Header("Position")]
    public Node node;
    #endregion
}