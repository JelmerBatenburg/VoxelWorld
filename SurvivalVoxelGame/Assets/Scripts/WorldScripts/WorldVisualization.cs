using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class WorldVisualization : MonoBehaviour
{
    public static WorldVisualization _instance;
    public MaterialTypeInformation materialsInformation;

    public MeshCashInformation[,] cashedChunkTiles;
    public List<TileGroupDrawInformation>[,] chunkDraws;

    public List<Vector2Int> chunkDrawQueue = new List<Vector2Int>();

    public void Awake()
    {
        _instance = this;
    }

    public void Update()
    {
        RenderChunks();
        ChunkDrawQueue();
    }

    public void RenderChunks()
    {
        if (!PlayerController._instance || !WorldData.isDone)
            return;

        if(chunkDraws == null)
        {
            int size = WorldSettings.worldSize;
            cashedChunkTiles = new MeshCashInformation[size, size];
            chunkDraws = new List<TileGroupDrawInformation>[size, size];
        }

        Vector3 playerPos = PlayerController._instance.transform.position;

        int drawDistance = WorldSettings.drawDistance;
        int chunkAmount = WorldSettings.worldSize;
        int chunkSize = WorldSettings.chunkWidth;

        int centerX = Mathf.RoundToInt(playerPos.x / (chunkSize * WorldSettings.tileSize));
        int centerZ = Mathf.RoundToInt(playerPos.z / (chunkSize * WorldSettings.tileSize));

        Vector3 lookDirection = PlayerController._instance.transform.forward;
        lookDirection.y = 0;
        lookDirection.Normalize();

        for (int x = centerX - drawDistance; x < centerX + drawDistance; x++)
        {
            for (int z = centerZ - drawDistance; z < centerZ + drawDistance; z++)
            {

                if (x < 0 || x > chunkAmount - 1)
                    continue;
                if (z < 0 || z > chunkAmount - 1)
                    continue;

                Vector3 position = new Vector3(x, 0, z) * chunkSize * WorldSettings.tileSize;

                if (Vector3.Distance(position, new Vector3(playerPos.x, 0, playerPos.z)) / (chunkSize * WorldSettings.tileSize) >= drawDistance)
                    continue;

                Vector3 thisDirection = ((position + (lookDirection * chunkSize * WorldSettings.tileSize * WorldSettings.visibleBackChunks)) - playerPos).normalized;
                if (Vector3.Dot(thisDirection, lookDirection) < 0)
                    continue;

                if (chunkDraws[x,z] == null)
                {
                    if (!chunkDrawQueue.Contains(new Vector2Int(x, z)))
                        chunkDrawQueue.Add(new Vector2Int(x, z));
                    continue;
                }


                foreach(TileGroupDrawInformation draw in chunkDraws[x, z])
                {
                    Material material = materialsInformation.materials[draw.materialType].material;
                    Graphics.DrawMesh(draw.mesh, position, Quaternion.identity, material, 0);
                }
            }
        }
    }

    public void ChunkDrawQueue()
    {
        if (chunkDrawQueue.Count == 0)
            return;

        DrawChunk(chunkDrawQueue[0].x, chunkDrawQueue[0].y);
        chunkDrawQueue.RemoveAt(0);
    }

    #region main mesh initialization
    public void DrawChunk(int x , int z)
    {
        List<TileGroupDrawInformation> draws = CalculateChunkMeshInformation(x, z);
        chunkDraws[x, z] = draws;
    }

    public List<TileGroupDrawInformation> CalculateChunkMeshInformation(int x, int z)
    {
        List<TileGroupMeshCash> meshes = new List<TileGroupMeshCash>();
        List<TileDrawInformation> drawTiles = CleanTilePositions(x, z);

        cashedChunkTiles[x, z] = new MeshCashInformation();
        cashedChunkTiles[x, z].cleanTiles = drawTiles;

        List<int> layers = new List<int>();
        for (int i = 1; i < WorldSettings.heightLayers; i++)
        {
            float value = (1f / WorldSettings.heightLayers) * i;
            layers.Add(Mathf.RoundToInt(Mathf.Lerp(0, WorldSettings.chunkHeight, value)));
        }

        foreach(TileDrawInformation tile in drawTiles)
        {
            int height = tile.position.y;
            int layerIndex = 0;
            foreach(int layer in layers)
            {
                if (height >= layer)
                    layerIndex++;
                else
                    break;
            }


            int meshIndex = 0;
            if(meshes.Count == 0)
            {
                meshes.Add(new TileGroupMeshCash(tile.materialType, layerIndex));
            }
            else
            {
                bool found = false;
                for (int i = 0; i < meshes.Count; i++)
                {
                    if (meshes[i].material == tile.materialType && meshes[i].layer == layerIndex)
                    {
                        found = true;
                        meshIndex = i;
                        break;
                    }
                }
                if (!found)
                {
                    meshIndex = meshes.Count;
                    meshes.Add(new TileGroupMeshCash(tile.materialType, layerIndex));
                }
            }

            //Top
            if (tile.top)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.up, false);

            //BottomSide
            if (tile.bottom)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.down, true);

            //Front
            if (tile.front)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.forward, false);

            //Back
            if (tile.back)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.back, true);

            //Right
            if (tile.right)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.right, false);

            //Left
            if (tile.left)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.left, true);
        }

        List<TileGroupDrawInformation> drawInformations = new List<TileGroupDrawInformation>();

        foreach (TileGroupMeshCash meshCash in meshes)
        {
            TileGroupDrawInformation draw = new TileGroupDrawInformation();
            draw.materialType = meshCash.material;

            draw.mesh = new Mesh();
            draw.mesh.vertices = meshCash.vertices.ToArray();
            draw.mesh.triangles = meshCash.triangles.ToArray();
            draw.mesh.normals = meshCash.normals.ToArray();
            draw.mesh.uv = meshCash.uvs.ToArray();

            draw.mesh.Optimize();
            draw.mesh.OptimizeIndexBuffers();
            draw.mesh.OptimizeReorderVertexBuffer();

            drawInformations.Add(draw);
        }

        return drawInformations;
    }

    public List<TileGroupMeshCash> AddPlane(List<TileGroupMeshCash> meshes, int meshIndex, Vector3 position, Vector3 direction, bool flipNormal)
    {
        float tileSize = WorldSettings.tileSize;
        position *= tileSize;

        float dirX = direction.x;
        float dirY = direction.y;
        float dirZ = direction.z;

        Vector3 offset1 = new Vector3(dirZ, dirX, dirY);
        Vector3 offset2 = new Vector3(dirY, dirZ, dirX);

        Vector3 cornerA = position + (((direction + offset1 + offset2) / 2) * tileSize);
        Vector3 cornerB = position + (((direction + -offset1 + offset2) / 2) * tileSize);
        Vector3 cornerC = position + (((direction + offset1 + -offset2) / 2) * tileSize);
        Vector3 cornerD = position + (((direction + -offset1 + -offset2) / 2) * tileSize);

        int verticieCount = meshes[meshIndex].vertices.Count;

        //Verticies
        if (!flipNormal)
        {
            meshes[meshIndex].vertices.Add(cornerA);
            meshes[meshIndex].vertices.Add(cornerB);
            meshes[meshIndex].vertices.Add(cornerD);
            meshes[meshIndex].vertices.Add(cornerC);
        }
        else
        {
            meshes[meshIndex].vertices.Add(cornerA);
            meshes[meshIndex].vertices.Add(cornerC);
            meshes[meshIndex].vertices.Add(cornerD);
            meshes[meshIndex].vertices.Add(cornerB);
        }
        
        //TriangleA
        meshes[meshIndex].triangles.Add(verticieCount);
        meshes[meshIndex].triangles.Add(verticieCount + 1);
        meshes[meshIndex].triangles.Add(verticieCount + 3);
        //TriangleB
        meshes[meshIndex].triangles.Add(verticieCount + 1);
        meshes[meshIndex].triangles.Add(verticieCount + 2);
        meshes[meshIndex].triangles.Add(verticieCount + 3);

        //Normals
        meshes[meshIndex].normals.Add(direction);
        meshes[meshIndex].normals.Add(direction);
        meshes[meshIndex].normals.Add(direction);
        meshes[meshIndex].normals.Add(direction);

        //UVs
        meshes[meshIndex].uvs.Add(new Vector2(1, 1));
        meshes[meshIndex].uvs.Add(new Vector2(1, 0));
        meshes[meshIndex].uvs.Add(new Vector2(0, 0));
        meshes[meshIndex].uvs.Add(new Vector2(0, 1));

        return meshes;
    }
    #endregion

    public List<TileDrawInformation> CleanTilePositions(int xIndex, int zIndex)
    {
        int[,,] tiles = WorldData.chunks[xIndex, zIndex].tiles;
        List<TileDrawInformation> drawTiles = new List<TileDrawInformation>();

        int chunkSize = WorldSettings.chunkWidth;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < WorldSettings.chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (tiles[x, y, z] == 0)
                        continue;

                    TileDrawInformation tile = new TileDrawInformation(x, y, z, tiles[x,y,z] - 1);

                    if (y == WorldSettings.chunkHeight - 1 || tiles[x, y + 1, z] == 0)
                        tile.top = true;
                    if (y == 0 || tiles[x, y - 1, z] == 0)
                        tile.bottom = true;

                    if (z != chunkSize - 1 && tiles[x, y, z + 1] == 0 || (z == chunkSize - 1 && (zIndex == WorldSettings.worldSize - 1 || WorldData.chunks[xIndex, zIndex + 1].tiles[x, y, 0] == 0)))
                        tile.front = true;
                    if (z != 0 && tiles[x, y, z - 1] == 0 || (z == 0 && (zIndex == 0 || WorldData.chunks[xIndex, zIndex - 1].tiles[x, y, chunkSize - 1] == 0)))
                        tile.back = true;

                    if (x != chunkSize - 1 && tiles[x + 1, y, z] == 0 || (x == chunkSize - 1 && (xIndex == WorldSettings.worldSize - 1 || WorldData.chunks[xIndex + 1, zIndex].tiles[0, y, z] == 0)))
                        tile.right = true;
                    if (x != 0 && tiles[x - 1, y, z] == 0 || (x == 0 && (xIndex == 0 || WorldData.chunks[xIndex - 1, zIndex].tiles[chunkSize - 1, y, z] == 0)))
                        tile.left = true;


                    if (tile.top || tile.bottom || tile.front || tile.back || tile.right || tile.left)
                        drawTiles.Add(tile);
                }
            }
        }

        return drawTiles;
    }

    public class MeshCashInformation
    {
        public List<TileDrawInformation> cleanTiles = new List<TileDrawInformation>();
    }

    #region constructors
    public class TileGroupMeshCash
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<int> triangles = new List<int>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();

        public int material;
        public int layer;

        public TileGroupMeshCash(int material, int layer)
        {
            this.material = material;
            this.layer = layer;
        }
    }

    public class TileGroupDrawInformation
    {
        public int materialType;
        public Mesh mesh;
    }

    public class TileDrawInformation
    {
        public Vector3Int position;
        public int materialType;
        public bool top, bottom, front, back, right, left;

        public TileDrawInformation(int x, int y, int z, int materialType)
        {
            position = new Vector3Int(x, y, z);
            this.materialType = materialType;
            top = false;
            bottom = false;
            front = false;
            back = false;
            right = false;
            left = false;
        }
    }
    #endregion
}
