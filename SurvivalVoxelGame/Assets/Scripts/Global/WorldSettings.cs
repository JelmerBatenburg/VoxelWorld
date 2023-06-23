using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSettings : MonoBehaviour
{
    [Header("Performance")]
    public int _drawDistance;
    public static int drawDistance;
    public int _heightLayers;
    public static int heightLayers;
    public int _visibleBackChunks;
    public static int visibleBackChunks;

    [Header("ChunkSettings")]
    public int _chunkWidth;
    public int _chunkHeight;
    public int _worldSize;

    public static int chunkWidth;
    public static int chunkHeight;
    public static int worldSize;

    [Header("Other Settings")]
    public float _tileSize;
    public static float tileSize;

    ///Applying all the static world settings values
    public void Start()
    {
        drawDistance = _drawDistance;
        heightLayers = _heightLayers;
        visibleBackChunks = _visibleBackChunks;

        chunkWidth = _chunkWidth;
        chunkHeight = _chunkHeight;
        worldSize = _worldSize;

        tileSize = _tileSize;
    }
}
