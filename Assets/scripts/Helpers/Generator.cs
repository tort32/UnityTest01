using UnityEngine;
using System.Linq;
using System.Collections;

public class Generator
{
	public static Texture2D BuildHeightMap(float[] heightValues, int width, int height, float bumpFactor)
	{
		Texture2D heightMap = new Texture2D (width, height);
		Color[] heightColors = new Color[width * height];
		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				int mapIndex = y * width + x;
				float h = heightValues[mapIndex];
				float v = Mathf.Clamp01 ((h * bumpFactor + 1.0f)*0.5f);
				heightColors[mapIndex] = new Color(v,v,v);
			}
		}
		heightMap.SetPixels(heightColors);
		heightMap.anisoLevel = 1;
		heightMap.filterMode = FilterMode.Trilinear;
		heightMap.Apply ();
		return heightMap;
	}

	public static Texture2D BuildNormalMap(float[] heightValues, int width, int height,float bumpFactor)
	{
		Texture2D normalMap = new Texture2D(width, width);
		Color[] colsNormal = new Color[width * height];
		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				int mapIndex = y * width + x;
				int mapIndexNextX = y * width + ((x + 1) % width);
				int mapIndexNextY = ((y + 1) % height) * height + x;
				float h0 = heightValues[mapIndex];
				float h1 = heightValues[mapIndexNextX];
				float h2 = heightValues[mapIndexNextY];
				float xDelta = Mathf.Clamp01(((h0-h1)*bumpFactor+1.0f)*0.5f);
				float yDelta = Mathf.Clamp01(((h2-h0)*bumpFactor+1.0f)*0.5f);
				colsNormal[mapIndex] = new Color(1.0f,yDelta,1.0f,xDelta);
			}
		}
		normalMap.SetPixels(colsNormal);
		normalMap.anisoLevel = 1;
		normalMap.filterMode = FilterMode.Trilinear;
		normalMap.Apply();
		return normalMap;
	}
}

