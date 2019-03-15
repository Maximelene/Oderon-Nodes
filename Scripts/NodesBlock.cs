using UnityEngine;

public class NodesBlock : MonoBehaviour
{
	void Start () {

        Vector3 pos = transform.position;
        Vector3 tmp = new Vector3(pos.x, 0.2f, pos.z);
        transform.position = tmp;
	}
}
