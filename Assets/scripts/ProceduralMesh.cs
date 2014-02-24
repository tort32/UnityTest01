using UnityEngine;
using System.Collections.Generic;

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
		
		// Build displacement map
		Texture2D heightMap = mf.renderer.material.GetTexture (0) as Texture2D;
		/*Texture2D heightMap = new Texture2D (256, 256);

		// set texture
		Color[] cols = new Color[heightMap.width * heightMap.height];
		Color[] colsOut = new Color[heightMap.width * heightMap.height];
		for(int x = 0; x < heightMap.width; ++x)
			for(int y = 0; y < heightMap.height; ++y)
		{
			float value = Random.Range(0.0f,1.0f);
			cols[y * heightMap.width + x] = new Color(value,value,value);
		}
		
		Vector3 spherePoints = new Vector3[heightMap.width * heightMap.height];
		for (int y = 0; y < heightMap.height; ++y)
		{
			float v = (float)y / (float)heightMap.height; // [0..1)
			float theta = Mathf.PI * v;
			for (int x = 0; x < heightMap.width; ++x)
			{
				int mapIndex = (heightMap.height - y) * heightMap.width + (heightMap.width - x);
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

		int octSize = 8;
		List<int>[] octGrid = new List<int>[octSize*octSize*octSize];
		for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{
			int x = Mathf.Min( (spherePoints[n].x - 1.0f)/2.0f * octSize, octSize-1);
			int y = Mathf.Min( (spherePoints[n].y - 1.0f)/2.0f * octSize, octSize-1);
			int z = Mathf.Min( (spherePoints[n].z - 1.0f)/2.0f * octSize, octSize-1);
			List<int> indexList = octGrid[z*octSize*octSize + y*octSize + x];
			if(indexList == null)
			{
				indexList = new List<int>(n);
			}
			else
			{
				indexList.Add(n);
			}
		}

		// Compute texture smooth with oct grid search
		float smoothSize = 0.1;
		float s2 = smoothSize * smoothSize;
		float nextCels = Mathf.CeilToInt(smoothSize * 0.5f * (float)octGrid);
		for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{ 
			Vector3 point = spherePoints[n];
			int x = Mathf.Min( (point.x - 1.0f)/2.0f * octSize, octSize-1);
			int y = Mathf.Min( (point.y - 1.0f)/2.0f * octSize, octSize-1);
			int z = Mathf.Min( (point.z - 1.0f)/2.0f * octSize, octSize-1);

			int minX = Mathf.Max(0, x - nextCels); int maxX = Mathf.Min(x + nextCels, octSize);
			int minY = Mathf.Max(0, y - nextCels); int maxY = Mathf.Min(y + nextCels, octSize);
			int minZ = Mathf.Max(0, z - nextCels); int maxZ = Mathf.Min(z + nextCels, octSize);

			float accum = 0;
			float mass = 0;
			for(int octX = minX; octX <= maxX; ++octX)
			{
				for(int octY = minY; octY <= maxY; ++octY)
				{
					for(int octZ = minZ; octZ <= maxZ; ++octZ)
					{
						List<int> indexList = octGrid[z*octSize*octSize + y*octSize + x];
						if(indexList == null)
							break;

						foreach(int index in indexList)
						{
							Vector3 pointNear = spherePoints[index];
							float l2 = (point - pointNear).sqrMagnitude;
							if(l2 < smoothSize*smoothSize)
							{
								float weight = 2.0f/(1.0f + l2 / s2) - 1.0f;
								accum += Color[n].r * weight;
								mass += weight;
							}
						}
					}
				}
			}
			float value = accum/mass;
			colsOut[n] = new Color(value,value,value);
		}


		for(int x = 0; x < heightMap.width; ++x)
			for(int y = 0; y < heightMap.height; ++y)
		{
			float value = Random.Range(0.0f,1.0f);
			cols[y * heightMap.width + x] = new Color(value,value,value);
		}

		heightMap.SetPixels (cols);
		heightMap.Apply ();
		mf.renderer.material.mainTexture = heightMap;
		*/

		float[] dispMap = new float[slices * slices];
		int texPerVertX = heightMap.width / slices;
		int texPerVertY = heightMap.height / slices;
		for(int j = 0; j < slices; ++j)
		{
			for(int i = 0; i < slices; ++i)
			{
				int index = j * slices + i;
				float height = 0;
				for(int x = 0; x < texPerVertX; ++x)
					for(int y = 0; y < texPerVertY; ++y)
					{
					Color pixel = heightMap.GetPixel(	heightMap.width - (i*texPerVertX + x - texPerVertX/2),
					                                 	heightMap.height - (j*texPerVertY + y - texPerVertY/2) );
						height += pixel.r;
					}
				dispMap[index] = height/(texPerVertX*texPerVertY);
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
				vertices[index] = point * (1.0f + dispMap[mapIndex]) * radius;
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
