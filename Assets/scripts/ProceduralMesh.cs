using UnityEngine;
using System.Linq;

public class ProceduralMesh : MonoBehaviour
{

	public float radius = 2;
	public int slices = 16;
	public float displacementMagnitude = 0.2f;

	public int textureWidth = 512;
	public int textureHieght = 512;

	// Use this for initialization
	void Start ()
	{
		MeshFilter mf = GetComponent<MeshFilter> ();
		Mesh mesh = new Mesh ();
		mf.mesh = mesh;

		// Generate sphere points for skinning
		Vector3[] spherePoints = new Vector3[textureWidth * textureHieght];
		using (new DebugTimer("Generate textels space cloud"))
		{
			for (int y = 0; y < textureHieght; ++y)
			{
				float v = (float)y / (float)(textureHieght - 1); // [0..1]
				float theta = Mathf.PI * v;
				//float z = 1.0f - v * 2.0f;
				//float theta = Mathf.Acos (z);
				for (int x = 0; x < textureWidth; ++x)
				{
					float u = (float)x / (float)textureWidth; // [0..1)
					float phi = 2.0f * Mathf.PI * u;
					Vector3 point = new Vector3 (
						Mathf.Sin (theta) * Mathf.Sin (phi),
						Mathf.Cos (theta),
						Mathf.Sin (theta) * Mathf.Cos (phi)
					);
					int mapIndex = y * textureWidth + x;
					spherePoints[mapIndex] = point;
				}
			}
		}

		// Build oct grid to optimize search
		OctGrid<int> oct = new OctGrid<int>(32,1.0f);
		using (new DebugTimer("Oct grid building time"))
		{
			for(int n = 0; n < textureWidth * textureHieght; ++n)
			{
				Vector3 point = spherePoints[n];
				oct.AddPoint(point, n);
			}
		}

		// Init height data
		float[] height = new float[textureWidth * textureHieght];
		float[] heightTmp = new float[textureWidth * textureHieght];

		using (new DebugTimer("Generate the surface"))
		{
			/*for(int n = 0; n < textureWidth * textureHieght; ++n)
			{
				height[n] = Random.Range(-0.1f,0.1f);
			}*/

			for(int n = 0; n < textureWidth * textureHieght; ++n)
			{
				Vector3 point = spherePoints[n];

				float[] w={2.0f,5.0f,20.0f};
				float[] a={1.0f,0.3f,0.1f};

				float value = 0;
				for(int i = 0; i < 3; ++i)
				{
					value += PerlinNoise.GetValue(point * w[i]) * a[i];
					if(i == 0)
					{
						value = Mathf.Clamp(value, -0.2f, 0.2f);
					}
				}

				heightTmp[n] = height[n] = value;
			}
		}
		
		using (new DebugTimer("Morph the surface"))
		{
			int impacts = 200;
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
				float texSize = 2.0f*Mathf.PI / (float)textureWidth; // size of the textel at the equator
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
				float sm = 10.0f/(i+50.0f);
				float sqrSm = sm*sm;
				float holeDepth = sm * 0.1f;

				foreach(int m in oct.GetElementsNearTo(point, sm))
				{
					Vector3 pointNear = spherePoints[m];
					float sqrLen = (point - pointNear).sqrMagnitude;
					if(sqrLen < sqrSm)
					{
						float weight = 2.0f - 1.0f/(2.0f/(1.0f + sqrLen / sqrSm ) - 1.0f); // (1..0)
						float holeValue = height[n] - holeDepth * weight;
						float value = Mathf.Min(height[m], holeValue);
						heightTmp[m] = value;
					}
				}

				float noiseValue = 1.0f/(float)impacts;
				for(int m = 0; m < textureWidth * textureHieght; ++m)
				{
					float value = Random.Range(-noiseValue, noiseValue);
					height[m] = heightTmp[m] + value;
					//height[m] = heightTmp[m];
				}
			}
		}

		// Build Diffuse
		Texture2D heightMap;
		using (new DebugTimer("Build Diffuse"))
		{	
			heightMap = Generator.BuildHeightMap(height,textureWidth,textureHieght, 1.0f);
		}

		// Build Normalmap
		Texture2D normalMap;
		using (new DebugTimer("Build Normalmap"))
		{	
			normalMap = Generator.BuildNormalMap(height, textureWidth, textureHieght, 5.0f);
		}

		// Update material
		mf.renderer.materials[0].mainTexture = heightMap;
		mf.renderer.materials[0].SetTexture("_BumpMap", normalMap);

		// Build displacement grid
		float[] dispMap = new float[slices * slices];
		using (new DebugTimer("Build vertex dispacement map"))
		{
			float sliceSize = 2.0f*Mathf.PI / (float)slices; // sector of the 1 unit circle
			float sqrSliceSize = sliceSize * sliceSize;
			for(int j = 0; j < slices; ++j)
			{
				for(int i = 0; i < slices; ++i)
				{
					int x = Mathf.FloorToInt((float)i / (float)slices * (float) textureWidth); // [0..1)
					int y = Mathf.CeilToInt((float)j / (float)(slices - 1) * (float) (textureHieght - 1)); // [0..1]

					int heightMapIndex = y * textureWidth + x;
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
							accum += height[m] * weight;
							mass += weight;
						}
					}
					float value = accum/mass;
					dispMap[dispMapIndex] = value; // Zero middle
				}
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
		using (new DebugTimer("Generate mesh vertices"))
		{
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
					Vector3 t = new Vector3(Mathf.Cos(phi), 0, -Mathf.Sin(phi));
					Vector3 tmp = (t - point * Vector3.Dot(point, t)).normalized;
					tangents[index] = new Vector4(tmp.x, tmp.y, tmp.z, 1.0f);
					*/

					tangents[index] = new Vector4( Mathf.Cos(phi), 0, -Mathf.Sin(phi), 1);

					/*
					// DEBUG QUAD
					vertices[index] = new Vector3(u*5.0f,dispMap[mapIndex]*1.0f,v*5.0f);
					normals[index] = new Vector3(0,1,0);
					*/

					uv[index] = new Vector2(u, v);

					// Polar texture coords fix
					if(j == 0) uv[index].x -= 0.5f/(float)(vertCountI - 1);
					if(j == vertCountJ - 1) uv[index].x += 0.5f/(float)(vertCountI - 1);
				}
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
		using (new DebugTimer("Generate mesh triangles"))
		{
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
		}
		mesh.triangles = tri;
	}

	// Update is called once per frame
	void Update ()
	{

	}
}
