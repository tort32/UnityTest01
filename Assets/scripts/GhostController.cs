﻿using UnityEngine;
using System.Collections;

public class GhostController : MonoBehaviour {

	public float movementSpeed = 5.0f;
	public float rotationSpeed = 5.0f;
	public float rotationRange = 70.0f;

	Vector3 dirUp = new Vector3 (0, 1, 0);

	// Use this for initialization
	void Start () {
		Screen.lockCursor = true;
	}
	
	// Update is called once per frame
	void Update () {
		Camera c = Camera.main;
		// Rotation
		float rotHorDelta = Input.GetAxis ("Mouse X") * rotationSpeed;
		transform.Rotate(0, rotHorDelta, 0);
		
		float rotVertDelta = Input.GetAxis ("Mouse Y") * rotationSpeed;
		float rotVertCurrent = c.transform.eulerAngles.x;
		if (rotVertCurrent > 180.0f)
		{
			rotVertCurrent -= 360.0f;
		}
		float rotVertNew = Mathf.Clamp(rotVertCurrent + rotVertDelta, -rotationRange, rotationRange);
		c.transform.localRotation = Quaternion.Euler(rotVertNew, 0, 0);
		
		// Movement
		Vector3 movDelta = new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical"));
		float floatDelta = Input.GetAxis ("UpDown");
		transform.position += (c.transform.rotation * movDelta + dirUp * floatDelta) * movementSpeed * Time.deltaTime;
	}
}
