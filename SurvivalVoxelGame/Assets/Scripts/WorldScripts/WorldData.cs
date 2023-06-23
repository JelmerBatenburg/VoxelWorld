using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class WorldData : MonoBehaviour
{
    public static WorldData _instance;
    public static bool isDone = false;

    public static ChunkInformation[,] chunks;

    public void Awake()
    {
        _instance = this;
        StartCoroutine(GenerateChunks());
    }

    public IEnumerator GenerateChunks()
    {
        yield return new WaitForEndOfFrame();

        chunks = new ChunkInformation[WorldSettings.worldSize, WorldSettings.worldSize];
        int count = 0;
        for (int x = 0; x < WorldSettings.worldSize; x++)
        {
            for (int z = 0; z < WorldSettings.worldSize; z++)
            {
                ChunkInformation chunk = RandomFillChunk(new Vector2Int(x, z));
                chunks[x,z] = chunk;

                Debug.Log("Finished chunk " + count + " out of " + (WorldSettings.worldSize * WorldSettings.worldSize));
                count++;
                yield return null;
            }
        }
        isDone = true;
    }

    public ChunkInformation RandomFillChunk(Vector2Int chunkIndex)
    {
        int xAddValue = chunkIndex.x * WorldSettings.chunkWidth;
        int zAddvalue = chunkIndex.y * WorldSettings.chunkWidth;

        ChunkInformation cashChunk = new ChunkInformation(WorldSettings.chunkWidth, WorldSettings.chunkHeight);
        cashChunk.chunkIndex = chunkIndex;
        float multiplyValueSides = 1f / WorldSettings.chunkWidth;
        float multiplyValueHeight = 1f / WorldSettings.chunkHeight;
        for (int x = 0; x < WorldSettings.chunkWidth; x++)
        {
            for (int y = 0; y < WorldSettings.chunkHeight; y++)
            {
                for (int z = 0; z < WorldSettings.chunkWidth; z++)
                {
                    float xNoise = Mathf.PerlinNoise((x + xAddValue) * multiplyValueSides, y * multiplyValueSides);
                    float zNoise = Mathf.PerlinNoise((z + zAddvalue) * multiplyValueSides, y * multiplyValueSides);
                    float wNoise = Mathf.PerlinNoise((x + xAddValue) * multiplyValueSides, (z + zAddvalue) * multiplyValueSides);
                    float noise = (xNoise + zNoise + wNoise) / 3f;

                    float xMNoise = Mathf.PerlinNoise((x - xAddValue) * multiplyValueSides, y * multiplyValueSides);
                    float zMNoise = Mathf.PerlinNoise((z - zAddvalue) * multiplyValueSides, y * multiplyValueSides);
                    float MNoise = Mathf.PerlinNoise((x - xAddValue) * multiplyValueSides, (z - zAddvalue) * multiplyValueSides);
                    float Materialnoise = (xMNoise + zMNoise + MNoise) * 1.5f;

                    Materialnoise = Mathf.Clamp(Materialnoise, 0, WorldVisualization._instance.materialsInformation.materials.Length);

                    cashChunk.tiles[x, y, z] = Mathf.RoundToInt(noise);
                    if (cashChunk.tiles[x, y, z] == 1)
                        cashChunk.tiles[x, y, z] = Mathf.RoundToInt(Materialnoise);
                }
            }
        }

        return cashChunk;
    }

    public class ChunkInformation
    {
        public Vector2Int chunkIndex;
        public int[,,] tiles;

        public ChunkInformation(int width, int height)
        {
            tiles = new int[width, height, width];
        }
    }

    /*public void OnDrawGizmos()
    {
        if (!EditorApplication.isPlaying)
            return;
        float worldSize = WorldSettings.worldSize;
        float chunkSize = WorldSettings.chunkWidth;
        float chunkheight = WorldSettings.chunkHeight;
        float tileSize = WorldSettings.tileSize;

        Vector3 size = Vector3.one * chunkSize * tileSize;
        size.y = tileSize * chunkheight;

        for (int x = 0; x < worldSize; x++)
            for (int y = 0; y < worldSize; y++)
            {
                Vector3 location = new Vector3(x * chunkSize * tileSize, 0, y * chunkSize * tileSize);
                location += size / 2f;
                location -= Vector3.one * tileSize / 2f;

                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(location, size);
            }

        Color tempCol = Color.white;
        tempCol.a = 0.3f;
        Gizmos.color = tempCol;

        for (int x = 0; x < chunkSize * worldSize; x++)
        {
            Vector3 location = Vector3.right * x * tileSize;
            location -= Vector3.one * tileSize / 2f;
            Gizmos.DrawRay(location, Vector3.up * chunkheight * tileSize);
        }

        float totalWidth = chunkSize * worldSize * tileSize;

        for (int y = 0; y < chunkheight; y++)
        {
            Vector3 location = Vector3.up * y * tileSize;
            location -= Vector3.one * tileSize / 2f;
            Gizmos.DrawRay(location, Vector3.right * totalWidth);
        }
    }*/
}
