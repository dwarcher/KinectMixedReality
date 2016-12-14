using UnityEngine;
using System.Collections;

public class CircleAround : MonoBehaviour {
    public float ht = 1.5f;
    public float rate = 180f;
    public float radius = 1.0f;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = Quaternion.Euler(0, Time.time * rate, 0) * (Vector3.forward * radius) + Vector3.up * ht;
	}
}
