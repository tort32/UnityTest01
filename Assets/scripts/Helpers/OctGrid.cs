using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public class OctGrid<T>
{
	private List<T>[] octGrid;
	private int nCells;
	private float gridSize;

	public OctGrid (int cells, float size)
	{
		nCells = cells;
		octGrid = new List<T>[nCells*nCells*nCells];
		gridSize = size;

		/*
		float[] points = {-1.1f, -1.0f, -0.01f, 0.0f, 0.01f, 0.8f, 0.9f, 1.0f, 1.1f};
		foreach(float point in points)
		{
			int x,y,z;
			getGridIndex(new Vector3(point,0,0),out x,out y,out z);
			Debug.Log(string.Format("{0} -> {1}", point, x));
		}
		*/
	}

	private void getGridIndex(Vector3 point, out int x, out int y, out int z)
	{
		/*
		x = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01((point.x/gridSize + 1.0f)/2.0f) * (float)nCells), nCells - 1);
		y = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01((point.y/gridSize + 1.0f)/2.0f) * (float)nCells), nCells - 1);
		z = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01((point.z/gridSize + 1.0f)/2.0f) * (float)nCells), nCells - 1);
		*/
		x = Mathf.Clamp( Mathf.CeilToInt((point.x/gridSize + 1.0f)/2.0f * (float)nCells), 0, nCells - 1);
		y = Mathf.Clamp( Mathf.CeilToInt((point.y/gridSize + 1.0f)/2.0f * (float)nCells), 0, nCells - 1);
		z = Mathf.Clamp( Mathf.CeilToInt((point.z/gridSize + 1.0f)/2.0f * (float)nCells), 0, nCells - 1);
	}

	public void AddPoint(Vector3 point, T value)
	{
		int x,y,z;
		getGridIndex(point, out x, out y, out z);
		List<T> list = octGrid[(z*nCells + y)*nCells + x];
		if(list == null)
		{
			list = new List<T>();
		}
		list.Add(value);
		octGrid[(z*nCells + y)*nCells + x] = list;
	}

	public IEnumerable<T> GetElementsNearTo(Vector3 point, float distance)
	{
		int x,y,z;
		getGridIndex(point, out x, out y, out z);

		int nextCels = Mathf.CeilToInt(distance * (float)nCells * 0.5f / gridSize);
		
		int minX = Mathf.Max(0, x - nextCels);
		int minY = Mathf.Max(0, y - nextCels);
		int minZ = Mathf.Max(0, z - nextCels);

		int maxX = Mathf.Min(x + nextCels, nCells - 1);
		int maxY = Mathf.Min(y + nextCels, nCells - 1);
		int maxZ = Mathf.Min(z + nextCels, nCells - 1);

		for(int octX = minX; octX <= maxX; octX++)
		{
			for(int octY = minY; octY <= maxY; octY++)
			{
				for(int octZ = minZ; octZ <= maxZ; octZ++)
				{
					List<T> list = octGrid[(octZ*nCells + octY)*nCells + octX];
					if(list == null)
						continue;
					
					foreach(T element in list)
					{
						yield return element;
					}
				}
			}
		}
	}
}

