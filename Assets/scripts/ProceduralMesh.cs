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

		float[] heightMap = new float[slices * slices];
		for(int j = 0; j < slices; ++j)
		{
			for(int i = 0; i < slices; ++i)
			{
				int index = j * slices + i;
				heightMap[index] = Random.Range(0.91f,1.01f);
			}
		}

		// Generate vertices
		int vertCountI = slices + 1;
		int vertCountJ = slices;
		int vertCount = vertCountI * vertCountJ;
		Vector3[] vertices = new Vector3[vertCount];
		Vector3[] normals = new Vector3[vertCount];
		Vector2[] uv = new Vector2[vertCount];
		for (int j = 0; j < vertCountJ; ++j)
		{
			float v = (float)j / (float)(vertCountJ - 1); // [0..1]
			//float z = 1.0f - v * 2.0f;
			//float theta = Mathf.Acos (z);
			float theta = Mathf.PI * v;
			for (int i=0; i < vertCountI; ++i)
			{
				int mapIndex = j * slices + (i == slices ? 0 : i);
				int index = j * vertCountI + i;
				float u = (float)i / (float)(vertCountI - 1); // [0..1]
				float phi = 2.0f * Mathf.PI * u;
				Vector3 point = new Vector3 (
						Mathf.Sin (theta) * Mathf.Sin (phi),
						Mathf.Cos (theta),
						Mathf.Sin (theta) * Mathf.Cos (phi)
					);
				vertices[index] = point * radius * heightMap[mapIndex];
				normals[index] = point;
				uv[index] = new Vector2(1.0f - u, 1.0f - v);

				// Polar texture coords fix
				if(j == 0) uv[index].x -= 0.5f/(float)(vertCountI - 1);
				if(j == vertCountJ - 1) uv[index].x += 0.5f/(float)(vertCountI - 1);
			}
		}
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uv;

		// Generate triangles
		int triCountI = vertCountI - 1;
		int triCountJ = vertCountJ - 1;
		int triCount = triCountI * triCountJ;
		int[] tri = new int[ triCount * 6 ];
		for (int j = 0; j < triCountJ - 2; ++j)
		{ 
			for (int i = 0; i < triCountI; ++i)
			{
				int vertIndex = (j + 1) * vertCountI + i;
				int vertIndexNextI = (j + 1) * vertCountI + (i + 1);
				int vertIndexNextJ = (j + 2) * vertCountI + i;
				int vertIndexNextIJ = (j + 2) * vertCountI + (i + 1);

				int triIndex = (j * triCountI + i) * 6;

				tri[triIndex++] = vertIndex;
				tri[triIndex++] = vertIndexNextJ;
				tri[triIndex++] = vertIndexNextI;

				tri[triIndex++] = vertIndexNextJ;
				tri[triIndex++] = vertIndexNextIJ;
				tri[triIndex++] = vertIndexNextI;
			}
		}
		// Polar tiangle fans
		for (int i = 0; i < triCountI; ++i)
		{
			int vertIndex = (vertCountJ - 2) * vertCountI + i;
			int vertIndexNextI = (vertCountJ - 2) * vertCountI + (i + 1);
			int vertIndexNextJ = (vertCountJ - 1) * vertCountI + i;
			int vertIndexNextIJ = (vertCountJ - 1) * vertCountI + (i + 1);
			
			int triIndex = ((triCountJ - 2) * triCountI + i) * 6;
			
			tri[triIndex++] = vertIndex;
			tri[triIndex++] = vertIndexNextJ;
			tri[triIndex++] = vertIndexNextI;

			vertIndex = i;
			vertIndexNextI = (i + 1);
			vertIndexNextJ = 1 * vertCountI + i;
			vertIndexNextIJ = 1 * vertCountI + (i + 1);
			
			tri[triIndex++] = vertIndexNextJ;
			tri[triIndex++] = vertIndexNextIJ;
			tri[triIndex++] = vertIndexNextI;
		}
		mesh.triangles = tri;
	}

	// Update is called once per frame
	void Update ()
	{

	}
}
