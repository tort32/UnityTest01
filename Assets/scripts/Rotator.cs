using UnityEngine;
using System.Collections;

public class Rotator : MonoBehaviour {

	public Vector3 axis = new Vector3(0,1,0);
	public float rotationSpeed = 10.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
	{
		transform.Rotate(axis, rotationSpeed*Time.deltaTime);
	}
}
