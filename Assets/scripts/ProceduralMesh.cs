using UnityEngine;
using System.Linq;

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
		//Texture2D sourceMap = mf.renderer.material.GetTexture (0) as Texture2D;
		//Texture2D heightMap = new Texture2D (sourceMap.width, sourceMap.height);
		Texture2D heightMap = new Texture2D (512, 512);
		heightMap.filterMode = FilterMode.Trilinear;

		DebugTimer timer = new DebugTimer("Generate space cloud");

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

		timer.Stop();

		// Init height data
		Color[] cols = new Color[heightMap.width * heightMap.height];
		Color[] colsOut = new Color[heightMap.width * heightMap.height];

		DebugTimer timer2 = new DebugTimer("Generate texture");
		
		//cols = (mf.renderer.material.GetTexture (0) as Texture2D).GetPixels ();
		/*for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{
			float value = Random.Range(-0.1f,0.1f);
			cols[n] = new Color(Mathf.Clamp01(cols[n].r+value),Mathf.Clamp01(cols[n].r+value),Mathf.Clamp01(cols[n].r+value));
		}*/
		for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{
			Vector3 point = spherePoints[n]*10;

			float[] w={0.2f,0.5f,3.0f};
			float[] a={1.0f,0.3f,0.1f};

			float value = 0;
			for(int i=0;i<3;++i)
			{
				value += PerlinNoise.GetValue(point * w[i])*a[i];
				if(i == 0)
				{
					value = Mathf.Clamp(value, -0.2f, 0.2f);
				}
			}

			value = Mathf.Clamp01((value+1.0f)*0.5f);
			cols[n] = new Color(value,value,value);
		}

		timer2.Stop();

		// Build oct grid to optimize search

		DebugTimer timer3 = new DebugTimer("Oct grid building time");

		OctGrid<int> oct = new OctGrid<int>(32,1.0f);
		for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{
			Vector3 point = spherePoints[n];
			oct.AddPoint(point, n);
		}

		timer3.Stop();

		DebugTimer timer4 = new DebugTimer("Compute texture smooth");

		// Compute texture smooth with oct grid search
		/*
		float smoothSize = 0.05f;
		for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{ 
			if(n < heightMap.width * heightMap.height / 2)
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
			else
			{
				float value = cols[n].r;
				colsOut[n] = new Color(value,value,value);
			}
		}*/

		for(int n = 0; n < heightMap.width * heightMap.height; ++n)
		{ 
			colsOut[n] = cols[n];
		}

		timer4.Stop ();

		DebugTimer timer5 = new DebugTimer("Morph the texture");

		int impacts = 100;
		for(int i=0; i<impacts; ++i)
		{
			// Find impact point
			float z = Random.Range(-1.0f, 1.0f);
			float theta = Mathf.Acos (z);
			float phi = Random.Range(0.0f, 2.0f * Mathf.PI);

			Vector3 point = new Vector3 (
				Mathf.Sin (theta) * Mathf.Sin (phi),
				Mathf.Cos (theta),
				Mathf.Sin (theta) * Mathf.Cos (phi)
				);

			// Find nearest textel
			float texSize = 2.0f*Mathf.PI / (float)heightMap.width; // size of the textel at the equator
			float sqrMinLen = texSize*texSize;
			int n = -1; // nearest textel index
			foreach(int m in oct.GetElementsNearTo(point, texSize))
			{
				Vector3 pointNear = spherePoints[m];
				float sqrLen = (point - pointNear).sqrMagnitude;
				if(sqrLen < sqrMinLen)
				{
					n = m;
					sqrMinLen = sqrLen;
				}
			}

			// Form a crater around the impact point
			float sm = 2.0f/(Mathf.Sqrt(i)+10.0f);
			float sqrSm = sm*sm;
			float holeDepth = sm * 2.0f;

			foreach(int m in oct.GetElementsNearTo(point, sm))
			{
				Vector3 pointNear = spherePoints[m];
				float sqrLen = (point - pointNear).sqrMagnitude;
				if(sqrLen < sqrSm)
				{
					float weight = 2.0f/(1.0f + sqrLen / sqrSm ) - 1.0f; // (1..0)
					float holeValue = cols[m].r - holeDepth * weight;
					float value = Mathf.Min(cols[m].r, holeValue);
					colsOut[m] = new Color(value,value,value);
				}
			}

			float noiseValue = 1.0f/(float)impacts;
			for(int j = 0; j < heightMap.width * heightMap.height; ++j)
			{
				float value = Random.Range(-noiseValue, noiseValue);
				cols[j] = new Color(colsOut[j].r + value, colsOut[j].g + value, colsOut[j].b + value);
			}
		}
		
		timer5.Stop ();

		// Update diffuse texture
		heightMap.SetPixels (colsOut);
		heightMap.Apply ();

		// Build Normalmap
		Texture2D normalMap = new Texture2D(heightMap.width, heightMap.height, TextureFormat.ARGB32, false);

		Color[] colsNormal = new Color[heightMap.width * heightMap.height];
		for (int y = 0; y < heightMap.height; ++y)
		{
			for (int x = 0; x < heightMap.width; ++x)
			{
				/*float h0 = heightMap.GetPixel(x,y).r;
				float h1 = heightMap.GetPixel((x + 1) % heightMap.width, y ).r;
				float h2 = heightMap.GetPixel(x, (y + 1) % heightMap.height ).r;

				const float bumpFactor = 0.05f;
				float sideMove = 1.0f/((float)heightMap.width*bumpFactor);
				Vector3 v01 = new Vector3(sideMove, 0, h1 - h0);
				Vector3 v02 = new Vector3(0, sideMove, h2 - h0);

				//int mapIndex = (heightMap.height - 1 - y) * heightMap.width + (heightMap.width - 1 - x); // inverted UV
				int mapIndex = y * heightMap.width + x;
				Vector3 n = Vector3.Cross( v01, v02 ).normalized;
				//Vector3 n = spherePoints[mapIndex];

				//colsNormal[mapIndex] = new Color(Mathf.Clamp01(n.x * 0.5f + 0.5f),Mathf.Clamp01(n.y * 0.5f + 0.5f),Mathf.Clamp01(n.z * 0.5f + 0.5f));
				colsNormal[mapIndex] = new Color(n.x * 0.5f + 0.5f,n.y * 0.5f + 0.5f,n.z * 0.5f + 0.5f);*/

				float h0 = heightMap.GetPixel(x,y).r;
				float h1 = heightMap.GetPixel((x + 1) % heightMap.width, y ).r;
				float h2 = heightMap.GetPixel(x, (y + 1) % heightMap.height ).r;
				const float bumpFactor = 5.0f;
				float xDelta = Mathf.Clamp01(((h1-h0)*bumpFactor+1.0f)*0.5f);
				float yDelta = Mathf.Clamp01(((h0-h2)*bumpFactor+1.0f)*0.5f);
				int mapIndex = y * heightMap.width + x;
				colsNormal[mapIndex] = new Color(1.0f,yDelta,1.0f,xDelta);
				//normalMap.SetPixel(x,y, new Color(1.0f,yDelta,1.0f,xDelta));
			}
		}
		normalMap.SetPixels(colsNormal);
		normalMap.anisoLevel = 1;
		normalMap.Apply();

		// Update material
		mf.renderer.materials[0].mainTexture = heightMap;
		mf.renderer.materials[0].SetTexture("_BumpMap", normalMap);

		// Build displacement grid

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
		Vector4[] tangents = new Vector4[vertCount];
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

				/*Vector3 t = new Vector3(Mathf.Cos(phi), 0, -Mathf.Sin(phi));
				Vector3 tmp = (t - point * Vector3.Dot(point, t)).normalized;
				tangents[index] = new Vector4(tmp.x, tmp.y, tmp.z, 1.0f);*/

				tangents[index] = new Vector4( Mathf.Cos(phi), 0, -Mathf.Sin(phi), 1);

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
		mesh.tangents = tangents;
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
