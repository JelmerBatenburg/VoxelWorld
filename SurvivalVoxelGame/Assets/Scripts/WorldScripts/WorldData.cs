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

    ///Awake to call the chunk generation, aswell as setting the satic reference.
    public void Awake()
    {
        _instance = this;
        StartCoroutine(GenerateChunks());
    }

    //Chunk generation
    ///Called for setting the chunk information, currently used for random fill but can be used for more inforamtion.
    public IEnumerator GenerateChunks()
    {
        ///waits a frame for all the data from other scripts to be set (just to be sure).
        yield return new WaitForEndOfFrame();

        ///Settings the 2Dimensional array for the chunks
        chunks = new ChunkInformation[WorldSettings.worldSize, WorldSettings.worldSize];
        int count = 0;
        ///Forloops to generate each chunk.
        for (int x = 0; x < WorldSettings.worldSize; x++)
        {
            for (int z = 0; z < WorldSettings.worldSize; z++)
            {
                ///Calling the RandomFillChunk to randomly fill the chunk.
                ChunkInformation chunk = RandomFillChunk(new Vector2Int(x, z));
                chunks[x,z] = chunk;

                ///Debugging the progress
                Debug.Log("Finished chunk " + count + " out of " + (WorldSettings.worldSize * WorldSettings.worldSize));
                count++;
                yield return null;
            }
        }
        isDone = true;
    }

    //Random chunk fill
    ///Randomly fills the chunk with multiple overlapping noisemaps for testing purposes.
    public ChunkInformation RandomFillChunk(Vector2Int chunkIndex)
    {
        ///getting the right noisemap location for better transitions between chunks
        int xAddValue = chunkIndex.x * WorldSettings.chunkWidth;
        int zAddvalue = chunkIndex.y * WorldSettings.chunkWidth;

        ///Creating the chunk data for setting the information, This is what will be send back
        ChunkInformation cashChunk = new ChunkInformation(WorldSettings.chunkWidth, WorldSettings.chunkHeight);
        cashChunk.chunkIndex = chunkIndex;
        float multiplyValueSides = 1f / WorldSettings.chunkWidth;
        float multiplyValueHeight = 1f / WorldSettings.chunkHeight;
        ///Going through each tile in the chunk to set it's information.
        for (int x = 0; x < WorldSettings.chunkWidth; x++)
        {
            for (int y = 0; y < WorldSettings.chunkHeight; y++)
            {
                for (int z = 0; z < WorldSettings.chunkWidth; z++)
                {
                    ///Noisemaps on each axis to check if it's empty or not
                    float xNoise = Mathf.PerlinNoise((x + xAddValue) * multiplyValueSides, y * multiplyValueSides);
                    float zNoise = Mathf.PerlinNoise((z + zAddvalue) * multiplyValueSides, y * multiplyValueSides);
                    float wNoise = Mathf.PerlinNoise((x + xAddValue) * multiplyValueSides, (z + zAddvalue) * multiplyValueSides);
                    float noise = (xNoise + zNoise + wNoise) / 3f;

                    ///Noisemaps on each axis to get the material in this chunk
                    float xMNoise = Mathf.PerlinNoise((x - xAddValue) * multiplyValueSides, y * multiplyValueSides);
                    float zMNoise = Mathf.PerlinNoise((z - zAddvalue) * multiplyValueSides, y * multiplyValueSides);
                    float MNoise = Mathf.PerlinNoise((x - xAddValue) * multiplyValueSides, (z - zAddvalue) * multiplyValueSides);
                    float Materialnoise = (xMNoise + zMNoise + MNoise) * 1.5f;

                    ///Getting the correct material type by checking how many are possible.
                    Materialnoise = Mathf.Clamp(Materialnoise, 0, WorldVisualization._instance.materialsInformation.materials.Length);

                    ///Rounding the noise when the rounded noise is 0 it is air, when 1 it will become the material generate above this.
                    cashChunk.tiles[x, y, z] = Mathf.RoundToInt(noise);
                    if (cashChunk.tiles[x, y, z] == 1)
                        cashChunk.tiles[x, y, z] = Mathf.RoundToInt(Materialnoise);
                }
            }
        }
        ///Sending back the chunk
        return cashChunk;
    }

    //Constructor containing the data for a chunk
    public class ChunkInformation
    {
        ///Stashed location of chunk
        public Vector2Int chunkIndex;
        public int[,,] tiles;

        ///Setting the size of the 3Dimensional array of tiles
        public ChunkInformation(int width, int height)
        {
            tiles = new int[width, height, width];
        }
    }

    ///Gizmos to visualize chunks and tiles for screenshots
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
