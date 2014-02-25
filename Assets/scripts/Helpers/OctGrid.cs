using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public class OctGrid<T>
{
	private List<T>[] octGrid;
	private int octCells;
	private float octSize;
	private int nextCels = 1;

	public OctGrid (int cells, float size)
	{
		octCells = cells;
		octGrid = new List<T>[octCells*octCells*octCells];
		octSize = size;

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
		x = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01((point.x/octSize + 1.0f)/2.0f) * (float)octCells), octCells - 1);
		y = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01((point.y/octSize + 1.0f)/2.0f) * (float)octCells), octCells - 1);
		z = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01((point.z/octSize + 1.0f)/2.0f) * (float)octCells), octCells - 1);
	}

	public void AddPoint(Vector3 point, T value)
	{
		int x,y,z;
		getGridIndex(point, out x, out y, out z);
		List<T> list = octGrid[(z*octCells + y)*octCells + x];
		if(list == null)
		{
			list = new List<T>();
		}
		list.Add(value);
		octGrid[(z*octCells + y)*octCells + x] = list;
	}

	public void SetLookDistance (float size)
	{
		nextCels = Mathf.CeilToInt(size * (float)octCells * 0.5f / octSize); // 1/2
	}

	public IEnumerable<T> GetElementsNearTo(Vector3 point)
	{
		int x,y,z;
		getGridIndex(point, out x, out y, out z);
		
		int minX = Mathf.Max(0, x - nextCels);
		int minY = Mathf.Max(0, y - nextCels);
		int minZ = Mathf.Max(0, z - nextCels);

		int maxX = Mathf.Min(x + nextCels, octCells - 1);
		int maxY = Mathf.Min(y + nextCels, octCells - 1);
		int maxZ = Mathf.Min(z + nextCels, octCells - 1);

		for(int octX = minX; octX <= maxX; octX++)
		{
			for(int octY = minY; octY <= maxY; octY++)
			{
				for(int octZ = minZ; octZ <= maxZ; octZ++)
				{
					List<T> list = octGrid[(z*octCells + y)*octCells + x];
					if(list == null)
						break;
					
					foreach(T element in list)
					{
						yield return element;
					}
				}
			}
		}
	}
}

