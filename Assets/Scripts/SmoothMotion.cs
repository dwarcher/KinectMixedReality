using UnityEngine;
using System.Collections;

public class SmoothMotion : MonoBehaviour {
    public Transform trackTo;
	// Use this for initialization
	void Start () {
        //_oldParent = transform.parent;

        //transform.parent = null;
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = Vector3.Lerp(transform.position, trackTo.position, Time.deltaTime * 7.0f);
        transform.rotation = Quaternion.Lerp(transform.rotation, trackTo.rotation, Time.deltaTime * 7.0f);
    }
}
