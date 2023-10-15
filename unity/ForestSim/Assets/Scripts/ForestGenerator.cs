using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ForestSettings
{
    public static int ChunkSize = 64;
    public static float NoiseScale = 0.2f;
    public static float NoiseOffsetX = 1000;
    public static float NoiseOffsetY = 1000;
    public static float TreeDensity = 10;
    public static float Threshold = 3;

    public static byte TreeProbe(Vector2Int chunkPos, float x, float y)
    {
        float probe = TreeDensity * Mathf.PerlinNoise(NoiseOffsetX + (x + ChunkSize * chunkPos.x) * NoiseScale,
            NoiseOffsetY + (y + ChunkSize * chunkPos.y) * NoiseScale);
        return (byte) (probe > Threshold ? 1 : 0);
    }
}

public class ForestGenerator : MonoBehaviour
{
    public static Vector2Int ForestSize;
    
    public Terrain terrain;
    public Wind wind;
    public ForestChunk forestChunkPrefab;
    public float chunkUpdateInterval = 0.05f;

    private ForestChunk[,] chunks;
    private Coroutine simulationCoroutine;

    void Start()
    {
        //Collect terrain data and calculate forest size in chunks
        var data = terrain.terrainData.size;
        ForestSize = new Vector2Int((int)data.x / ForestSettings.ChunkSize, (int)data.z / ForestSettings.ChunkSize);
       
        // ForestSize.x = 2;
        // ForestSize.y = 2;
        
        chunks = new ForestChunk[ForestSize.x, ForestSize.y];
        
        //Spawn chunk game objects objects
        for (int x = 0; x < ForestSize.x; x++)
        {
            for (int y = 0; y < ForestSize.y; y++)
            {
                var pos = new Vector3(ForestSettings.ChunkSize * x, 0, ForestSettings.ChunkSize * y);
                var instance = Instantiate(forestChunkPrefab, pos, Quaternion.identity, transform);
                instance.Setup(this,  ForestSettings.ChunkSize, new Vector2Int(x,y));
                instance.OtherParts = chunks;
                chunks[x, y] = instance;
            }
        }
        
        wind.UpdateWind();

    }
    
    public void Generate()
    {
        ForestSettings.NoiseScale = Random.Range(0.1f, 0.6f);
        ForestSettings.NoiseOffsetX = Random.Range(-1, 1) * 1000;
        ForestSettings.NoiseOffsetY = Random.Range(-1, 1) * 1000;

        for (int x = 0; x < ForestSize.x; x++)
        {
            for (int y = 0; y < ForestSize.y; y++)
            {
                chunks[x, y].ActivateChunk();
            }
        }
    }

    public void Clear()
    {
        for (int x = 0; x < ForestSize.x; x++)
        {
            for (int y = 0; y < ForestSize.y; y++)
            {
                chunks[x, y].DisposeChunk();
            }
        }
    }
    
    public void PlaySimulation(bool value)
    {
        if(simulationCoroutine != null)
            StopCoroutine(simulationCoroutine);
        if (value)
            simulationCoroutine = StartCoroutine(Simulation());
    }
    
    private IEnumerator Simulation()
    {
        while (true)
        {
            yield return new WaitForSeconds(chunkUpdateInterval);
            
            for (int x = 0; x < ForestSize.x; x++)
            {
                for (int y = 0; y < ForestSize.y; y++)
                {
                    chunks[x, y].UpdateChunk();
                }
            }
        }
    }

    public Vector3 GetHeight(Vector3 pos)
    {
        pos.y = terrain.SampleHeight(pos);
        return pos;
    }

    public void AddRandomFire()
    {
        foreach (var part in chunks)
        {
            part.AddRandomFire();
        }
    }
    
    public void AddTreeAt(Vector3 p)
    {
        GetForestPart(p).AddTreeAt(p);
    }
	
    public void AddFireAt(Vector3 p)
    {
        GetForestPart(p).AddFireAt(p);
    }
	
    public void RemoveTreeAt(Vector3 p)
    {
        GetForestPart(p).RemoveTreeAt(p);
    }
	
    public void ExtinguishAt(Vector3 p)
    {
        GetForestPart(p).ExtinguishAt(p);
    }

    private ForestChunk GetForestPart(Vector3 p)
    {
        var i = Mathf.FloorToInt( p.x / ForestSettings.ChunkSize );
        var j = Mathf.FloorToInt( p.z / ForestSettings.ChunkSize );
        return chunks[i, j];
    }
}
