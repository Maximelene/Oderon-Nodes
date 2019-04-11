using UnityEngine;

public class NodesBlock : MonoBehaviour
{
	void Start () {

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, 0.2f, pos.z);
	}
}