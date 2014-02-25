using UnityEngine;

public class ProceduralMesh : MonoBehaviour
{

	public float radius = 2;
	public int slices = 16;
	public float displacementMagnitude = 0.2f;

	// Use this for initialization
	void Start ()
	{
		MeshFilter mf = GetComponent<MeshFilter> ();
		Mesh mesh = new Mesh ();
		mf.mesh = mesh;
		
		// Create height map
		//Texture2D heightMap = mf.renderer.material.GetTexture (0) as Texture2D;
		Texture2D sourceMap = mf.renderer.material.GetTexture (0) as Texture2D;
		Texture2D heightMap = new Texture2D (sourceMap.width, sourceMap.height);
		heightMap.filterMode = FilterMode.Trilinear;

		// Init height data
		Color[] cols = new Color[heightMap.width * heightMap.height];
		Color[] colsOut = new Color[heightMap.width * heightMap.height];
		
		cols = (mf.renderer.material.GetTexture (0) as Texture2D).GetPixels ();
		/*for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{
			float value = Random.Range(-0.1f,0.1f);
			cols[n] = new Color(Mathf.Clamp01(cols[n].r+value),Mathf.Clamp01(cols[n].r+value),Mathf.Clamp01(cols[n].r+value));
		}*/

		// Generate sphere points for skinning
		Vector3[] spherePoints = new Vector3[heightMap.width * heightMap.height];
		for (int y = 0; y < heightMap.height; ++y)
		{
			float v = (float)y / (float)(heightMap.height - 1); // [0..1]
			float theta = Mathf.PI * v;
			//float z = 1.0f - v * 2.0f;
			//float theta = Mathf.Acos (z);
			for (int x = 0; x < heightMap.width; ++x)
			{
				int mapIndex = (heightMap.height - 1 - y) * heightMap.width + (heightMap.width - 1 - x); // inverted UV
				//int mapIndex = y * heightMap.width + x;
				float u = (float)x / (float)heightMap.width; // [0..1)
				float phi = 2.0f * Mathf.PI * u;
				Vector3 point = new Vector3 (
					Mathf.Sin (theta) * Mathf.Sin (phi),
					Mathf.Cos (theta),
					Mathf.Sin (theta) * Mathf.Cos (phi)
					);
				spherePoints[mapIndex] = point;
			}
		}

		// Build oct grid to optimize search

		DebugTimer timer = new DebugTimer("Oct grid building time");

		OctGrid<int> oct = new OctGrid<int>(32,1.0f);
		for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{
			Vector3 point = spherePoints[n];
			oct.AddPoint(point, n);
		}

		timer.Stop();

		DebugTimer timer2 = new DebugTimer("Compute texture smooth");

		// Compute texture smooth with oct grid search
		float smoothSize = 0.05f;
		for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{ 
			/*if(n < heightMap.width * heightMap.height / 2)*/
			{
				Vector3 point = spherePoints[n];

				float accum = 0.0f;
				float mass = 0.0f;

				float sm = smoothSize * (point.y * point.y);
				float sqrSm = sm*sm;
				//for(int m = 0; m < heightMap.width * heightMap.height; ++m)
				foreach(int m in oct.GetElementsNearTo(point, sm))
				{
					Vector3 pointNear = spherePoints[m];
					float sqrLen = (point - pointNear).sqrMagnitude;
					if(sqrLen < sqrSm)
					{
						float weight = 2.0f/(1.0f + sqrLen / sqrSm ) - 1.0f; // (1..0)
						//float weight = 2.0f*Mathf.Cos (Mathf.PI * sqrLen / sqrSm ) - 1.0f; // (1..0)
						accum += cols[m].r * weight;
						mass += weight;
					}
				}
				if(mass > 0.1f)
				{
					float value = Mathf.Clamp01((accum/mass-0.5f)*1.2f+0.5f);
					colsOut[n] = new Color(value,value,value);
				}
			}
			/*else
			{
				float value = cols[n].r;
				colsOut[n] = new Color(value,value,value);
			}*/
		}

		timer2.Stop ();

		// Update diffuse texture
		heightMap.SetPixels (colsOut);
		heightMap.Apply ();

		mf.renderer.materials[0].mainTexture = heightMap;

		float[] dispMap = new float[slices * slices];
		float sliceSize = 2.0f*Mathf.PI / (float)slices; // sector of the 1 unit circle
		float sqrSliceSize = sliceSize * sliceSize;
		for(int j = 0; j < slices; ++j)
		{
			for(int i = 0; i < slices; ++i)
			{
				int x = Mathf.FloorToInt((float)i / (float)slices * (float) heightMap.width); // [0..1)
				int y = Mathf.CeilToInt((float)j / (float)(slices - 1) * (float) (heightMap.height - 1)); // [0..1]
				int heightMapIndex = (heightMap.height - 1 - y) * heightMap.width + (heightMap.width - 1 - x);
				int dispMapIndex = j * slices + i;

				float accum = 0.0f;
				float mass = 0.0f;
				Vector3 point = spherePoints[heightMapIndex];
				foreach(int m in oct.GetElementsNearTo(point, sliceSize))
				{
					Vector3 pointNear = spherePoints[m];
					float sqrLen = (point - pointNear).sqrMagnitude;
					if(sqrLen < sqrSliceSize)
					{
						float weight = 2.0f/(1.0f + sqrLen / sqrSliceSize ) - 1.0f; // (1..0)
						accum += colsOut[m].r * weight;
						mass += weight;
					}
				}
				float height = accum/mass;
				dispMap[dispMapIndex] = height - 0.5f; // Zero middle
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
			for (int i = 0; i < vertCountI; ++i)
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
				vertices[index] = point * (radius + dispMap[mapIndex]*displacementMagnitude);
				normals[index] = point;

				/*
				// DEBUG QUAD
				vertices[index] = new Vector3(u*5.0f,dispMap[mapIndex]*1.0f,v*5.0f);
				normals[index] = new Vector3(0,1,0);*/

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
