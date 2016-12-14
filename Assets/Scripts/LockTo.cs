using UnityEngine;
using System.Collections;

public class LockTo : MonoBehaviour {
    public GameObject target;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = target.transform.position;
        transform.rotation = target.transform.rotation;
	}
}
