using UnityEngine;
using System.Collections.Generic;

public class OctGrid<T>
{
	List<T>[] octGrid;
	int octSize;
	int nextCels = 1;

	public OctGrid (int size)
	{
		octSize = size;
		octGrid = new List<T>[octSize*octSize*octSize];
	}

	public void AddPoint(Vector3 point, T value)
	{
		int x = Mathf.Min( (int)((point.x + 1.0f)/2.0f * (float)octSize), octSize - 1);
		int y = Mathf.Min( (int)((point.y + 1.0f)/2.0f * (float)octSize), octSize - 1);
		int z = Mathf.Min( (int)((point.z + 1.0f)/2.0f * (float)octSize), octSize - 1);
		List<T> indexList = octGrid[(z*octSize + y)*octSize + x];
		if(indexList == null)
		{
			indexList = new List<T>();
		}
		indexList.Add(value);
		octGrid[(z*octSize + y)*octSize + x] = indexList;
	}

	public void SetLookDistance (float size)
	{
		nextCels = Mathf.CeilToInt(size * 0.5f * (float)octSize);
	}

	public IEnumerable<T> GetElementsNearTo(Vector3 point)
	{
		int x = Mathf.Min( (int)((point.x + 1.0f)/2.0f * (float)octSize), octSize - 1);
		int y = Mathf.Min( (int)((point.y + 1.0f)/2.0f * (float)octSize), octSize - 1);
		int z = Mathf.Min( (int)((point.z + 1.0f)/2.0f * (float)octSize), octSize - 1);
		
		int minX = Mathf.Max(0, x - nextCels);
		int minY = Mathf.Max(0, y - nextCels);
		int minZ = Mathf.Max(0, z - nextCels);

		int maxX = Mathf.Min(x + nextCels, octSize - 1);
     	int maxY = Mathf.Min(y + nextCels, octSize - 1);
     	int maxZ = Mathf.Min(z + nextCels, octSize - 1);

		for(int octX = minX; octX <= maxX; ++octX)
		{
			for(int octY = minY; octY <= maxY; ++octY)
			{
				for(int octZ = minZ; octZ <= maxZ; ++octZ)
				{
					List<T> list = octGrid[(octZ*octSize + octY)*octSize + octX];
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

