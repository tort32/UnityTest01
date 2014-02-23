using UnityEngine;
using System.Collections;

public class ProceduralMesh : MonoBehaviour
{

	public float radius = 2;
	public int slices = 16;

	// Use this for initialization
	void Start ()
	{
		MeshFilter mf = GetComponent<MeshFilter> ();
		Mesh mesh = new Mesh ();
		mf.mesh = mesh;

		// Generate vertices
		Vector3[] vertices = new Vector3[slices * slices];
		Vector3[] normals = new Vector3[slices * slices];
		Vector2[] uv = new Vector2[slices * slices];
		for (int j = 0; j < slices; ++j)
		{
			float v = (float)j / (float)slices;
			float theta = Mathf.PI * v;
			for (int i=0; i<slices; ++i)
			{
				int index = j * slices + i;
				float u = (float)i / (float)slices;
				float phi = 2.0f * Mathf.PI * u;
				Vector3 point = new Vector3 (
						Mathf.Sin (theta) * Mathf.Cos (phi),
						Mathf.Sin (theta) * Mathf.Sin (phi),
						Mathf.Cos (theta)
					);
				vertices[index] = point * radius;
				normals[index] = point;
				uv[index] = new Vector2(u,v);

				Debug.Log(string.Format("{0}: {1},{2},{3}", index, point.x, point.y, point.z));
			}
		}
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uv;

		// Generate triangles
		int[] tri = new int[ (slices - 1) * (slices) * 6 ];
		for (int j = 0; j < slices - 1; ++j)
		{ 
			for (int i = 0; i < slices; ++i)
			{
				int vertIndex = j * slices + i;
				int vertIndexNext = j * slices + ((i == slices - 1) ? 0 : i + 1);

				int triIndex = (j * (slices - 1) + i) * 6;

				tri[triIndex++] = vertIndex;
				tri[triIndex++] = vertIndex + slices;
				tri[triIndex++] = vertIndexNext;


				tri[triIndex++] = vertIndex + slices;
				tri[triIndex++] = vertIndexNext + slices;
				tri[triIndex++] = vertIndexNext;
			}
		}
		mesh.triangles = tri;
	}

	// Update is called once per frame
	void Update ()
	{

	}
}
