using UnityEngine;
using System.Collections;

public class FPSController : MonoBehaviour {

	public float movementSpeed = 5.0f;
	public float rotationSpeed = 5.0f;
	public float rotationRange = 70.0f;
	public float jumpSpeed = 5.0f;

	float velVert = 0;

	// Use this for initialization
	void Start () {
		Screen.lockCursor = true;
	}
	
	// Update is called once per frame
	void Update () {
		CharacterController cc = GetComponent<CharacterController> ();

		// Rotation
		float rotHorDelta = Input.GetAxis ("Mouse X") * rotationSpeed;
		transform.Rotate(0, rotHorDelta, 0);

		float rotVertDelta = Input.GetAxis ("Mouse Y") * rotationSpeed;
		float rotVertCurrent = Camera.main.transform.eulerAngles.x;
		if (rotVertCurrent > 180.0f)
		{
			rotVertCurrent -= 360.0f;
		}
		float rotVertNew = Mathf.Clamp(rotVertCurrent + rotVertDelta, -rotationRange, rotationRange);
		Camera.main.transform.localRotation = Quaternion.Euler(rotVertNew, 0, 0);

		// Movement
		if (cc.isGrounded)
		{
			velVert = 0;

			if (Input.GetButtonDown ("Jump"))
			{
				velVert = jumpSpeed;
			}
		}
		else
		{
			velVert += Physics.gravity.y * Time.deltaTime;
		}

		Vector3 movDelta = new Vector3 (Input.GetAxis ("Horizontal") * movementSpeed, velVert, Input.GetAxis ("Vertical") * movementSpeed);
		cc.Move (transform.rotation * movDelta * Time.deltaTime);
	}
}
