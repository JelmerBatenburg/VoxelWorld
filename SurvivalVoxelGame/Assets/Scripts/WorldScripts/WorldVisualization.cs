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

    ///Setting the static instance
    public void Awake()
    {
        _instance = this;
    }

    ///Calling the main functions
    public void Update()
    {
        RenderChunks();
        ChunkDrawQueue();
    }

    //Rendering Chunks
    ///This funcion checks what chunks should be rendered based on the player location and world settings
    ///Each chunk that isn't generated yet will be added to the drawqueue, otherwise it will be drawn from here
    public void RenderChunks()
    {
        ///Checks if the world is done and if the player is there to check what chunks should be drawn
        if (!PlayerController._instance || !WorldData.isDone)
            return;

        ///Checks if the chunkdraws is empty or not, otherwise it will set the array size.
        if(chunkDraws == null)
        {
            int size = WorldSettings.worldSize;
            cashedChunkTiles = new MeshCashInformation[size, size];
            chunkDraws = new List<TileGroupDrawInformation>[size, size];
        }

        ///Cashes the information needed for the calculations.
        Vector3 playerPos = PlayerController._instance.transform.position;

        int drawDistance = WorldSettings.drawDistance;
        int chunkAmount = WorldSettings.worldSize;
        int chunkSize = WorldSettings.chunkWidth;

        ///Gets the chunk location the player currently is in for accessing the right chuncks.
        int centerX = Mathf.RoundToInt(playerPos.x / (chunkSize * WorldSettings.tileSize));
        int centerZ = Mathf.RoundToInt(playerPos.z / (chunkSize * WorldSettings.tileSize));

        ///Gets the direction the player is looking at.
        Vector3 lookDirection = PlayerController._instance.transform.forward;
        lookDirection.y = 0;
        lookDirection.Normalize();

        ///Forloops to go through each chunk around the player
        for (int x = centerX - drawDistance; x < centerX + drawDistance; x++)
        {
            for (int z = centerZ - drawDistance; z < centerZ + drawDistance; z++)
            {
                ///Checks if the chunk that is being checked is within the world size.
                if (x < 0 || x > chunkAmount - 1)
                    continue;
                if (z < 0 || z > chunkAmount - 1)
                    continue;

                ///Gets the worldspace chunk location
                Vector3 position = new Vector3(x, 0, z) * chunkSize * WorldSettings.tileSize;

                ///Checks if it is within the draw distance to make the chunks a circle around the player. That way when fog is enabled the corners aren't drawn outside the fog.
                if (Vector3.Distance(position, new Vector3(playerPos.x, 0, playerPos.z)) / (chunkSize * WorldSettings.tileSize) >= drawDistance)
                    continue;

                ///Checks if the chunk is within the view of the player, also adding a few behind the player so it will always have the correct information even when looking down.
                Vector3 thisDirection = ((position + (lookDirection * chunkSize * WorldSettings.tileSize * WorldSettings.visibleBackChunks)) - playerPos).normalized;
                if (Vector3.Dot(thisDirection, lookDirection) < 0)
                    continue;

                ///Checks if the chunk has been generated or not, if not it will add the chunk to the queue(if not already added).
                if (chunkDraws[x,z] == null)
                {
                    if (!chunkDrawQueue.Contains(new Vector2Int(x, z)))
                        chunkDrawQueue.Add(new Vector2Int(x, z));
                    continue;
                }

                ///Goes through all the draws in the chunk, then applies the right material to them and draws them for the player to see.
                foreach(TileGroupDrawInformation draw in chunkDraws[x, z])
                {
                    Material material = materialsInformation.materials[draw.materialType].material;
                    Graphics.DrawMesh(draw.mesh, position, Quaternion.identity, material, 0);
                }
            }
        }
    }

    //Chunk draw queue
    ///Goes through the queue to draw all required chunks.
    public void ChunkDrawQueue()
    {
        ///Checks if something is in the queue.
        if (chunkDrawQueue.Count == 0)
            return;

        ///Takes the first chunk in the queue and generates it draw data. It then removes it from the queue.
        DrawChunk(chunkDrawQueue[0].x, chunkDrawQueue[0].y);
        chunkDrawQueue.RemoveAt(0);
    }

    #region main mesh initialization
    //Draw chunk call
    ///The chunk draw that is being called from the queue, this will call the mesh calculation and then saves it in the chunkdraws.
    public void DrawChunk(int x , int z)
    {
        List<TileGroupDrawInformation> draws = CalculateChunkMeshInformation(x, z);
        chunkDraws[x, z] = draws;
    }

    //Calculate mesh information
    ///The main calculation to generate the mesh that needs to be drawn
    ///This also splits the chunk in different meshes for different materials and in to make the mesh faces count fall within unity limits.
    public List<TileGroupDrawInformation> CalculateChunkMeshInformation(int x, int z)
    {
        ///makes a cash for the multiple meshes
        List<TileGroupMeshCash> meshes = new List<TileGroupMeshCash>();
        ///Makes a clean list of tiles that only contains tiles that contains tiles that need to be drawn
        ///This also sets the information of what faces of the tiles need to be drawn
        List<TileDrawInformation> drawTiles = CleanTilePositions(x, z);

        ///Saves the drawtiles list for when the chunk gets edited it doesn't have to calculate the whole chunk again, but only the tiles surrounding the edited tile
        cashedChunkTiles[x, z] = new MeshCashInformation();
        cashedChunkTiles[x, z].cleanTiles = drawTiles;

        ///Uses the world settings to create layers for the chunk (this is because there is a Unity limit on how big a mesh can be that is generated through code).
        List<int> layers = new List<int>();
        for (int i = 1; i < WorldSettings.heightLayers; i++)
        {
            float value = (1f / WorldSettings.heightLayers) * i;
            layers.Add(Mathf.RoundToInt(Mathf.Lerp(0, WorldSettings.chunkHeight, value)));
        }

        ///Goes through all the tiles that needs to be drawn, it then set's the correct layer and adds the mesh data to the layer mesh.
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

            ///tries to find the a mesh it can add itself to, otherwise it will generate a new one.
            int meshIndex = 0;
            ///If there aren't any meshes it will jus generate the first one
            if(meshes.Count == 0)
            {
                meshes.Add(new TileGroupMeshCash(tile.materialType, layerIndex));
            }
            else
            {
                ///Goes through each mesh to check if that mesh has the same layer and material, when it does it will use that mesh to add onto.
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
                ///If there isn't a mesh with the correct layer and material it will make a new mesh with that information.
                if (!found)
                {
                    meshIndex = meshes.Count;
                    meshes.Add(new TileGroupMeshCash(tile.materialType, layerIndex));
                }
            }

            ///Checks what faces the tile has and it adds it to the main mesh.

            ///Top
            if (tile.top)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.up, false);

            ///BottomSide
            if (tile.bottom)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.down, true);

            ///Front
            if (tile.front)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.forward, false);

            ///Back
            if (tile.back)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.back, true);

            ///Right
            if (tile.right)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.right, false);

            ///Left
            if (tile.left)
                meshes = AddPlane(meshes, meshIndex, tile.position, Vector3.left, true);
        }

        ///Once it has all the required information of the mesh it will make a list of the final mesh data that should be drawn.
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
        ///Returns the drawdata of this chunk
        return drawInformations;
    }

    //Add planes
    ///This is the function that adds a face to a mesh with the given information.
    public List<TileGroupMeshCash> AddPlane(List<TileGroupMeshCash> meshes, int meshIndex, Vector3 position, Vector3 direction, bool flipNormal)
    {
        ///Sets the main calculation data
        float tileSize = WorldSettings.tileSize;
        position *= tileSize;

        float dirX = direction.x;
        float dirY = direction.y;
        float dirZ = direction.z;

        Vector3 offset1 = new Vector3(dirZ, dirX, dirY);
        Vector3 offset2 = new Vector3(dirY, dirZ, dirX);

        ///Gets the corners of the face that should be added
        Vector3 cornerA = position + (((direction + offset1 + offset2) / 2) * tileSize);
        Vector3 cornerB = position + (((direction + -offset1 + offset2) / 2) * tileSize);
        Vector3 cornerC = position + (((direction + offset1 + -offset2) / 2) * tileSize);
        Vector3 cornerD = position + (((direction + -offset1 + -offset2) / 2) * tileSize);

        ///Gets the acive vert count of the mesh for the calculation of the triangles
        int verticieCount = meshes[meshIndex].vertices.Count;

        ///Adds the normal in the right way to make the normal correct
        if (!flipNormal)
        {
            ///A B D C
            meshes[meshIndex].vertices.Add(cornerA);
            meshes[meshIndex].vertices.Add(cornerB);
            meshes[meshIndex].vertices.Add(cornerD);
            meshes[meshIndex].vertices.Add(cornerC);
        }
        else
        {
            ///A C D B
            meshes[meshIndex].vertices.Add(cornerA);
            meshes[meshIndex].vertices.Add(cornerC);
            meshes[meshIndex].vertices.Add(cornerD);
            meshes[meshIndex].vertices.Add(cornerB);
        }
        
        ///TriangleA
        meshes[meshIndex].triangles.Add(verticieCount);
        meshes[meshIndex].triangles.Add(verticieCount + 1);
        meshes[meshIndex].triangles.Add(verticieCount + 3);
        ///TriangleB
        meshes[meshIndex].triangles.Add(verticieCount + 1);
        meshes[meshIndex].triangles.Add(verticieCount + 2);
        meshes[meshIndex].triangles.Add(verticieCount + 3);

        ///Normals
        meshes[meshIndex].normals.Add(direction);
        meshes[meshIndex].normals.Add(direction);
        meshes[meshIndex].normals.Add(direction);
        meshes[meshIndex].normals.Add(direction);

        ///UVs
        meshes[meshIndex].uvs.Add(new Vector2(1, 1));
        meshes[meshIndex].uvs.Add(new Vector2(1, 0));
        meshes[meshIndex].uvs.Add(new Vector2(0, 0));
        meshes[meshIndex].uvs.Add(new Vector2(0, 1));

        return meshes;
    }
    #endregion

    //Clean tile positions
    ///This is called to only check the tiles that have information to draw.
    ///This also sets the information of what faces a tile has to draw.
    public List<TileDrawInformation> CleanTilePositions(int xIndex, int zIndex)
    {
        ///Gets the 3Dimensional tile array the the world data
        int[,,] tiles = WorldData.chunks[xIndex, zIndex].tiles;
        List<TileDrawInformation> drawTiles = new List<TileDrawInformation>();

        ///Gets the chunk sizes for the forloop
        int chunkSize = WorldSettings.chunkWidth;
        int chunkHeight = WorldSettings.chunkHeight;

        ///Goes through each tile to check its material to see if it's air and then it checks it neighbour if they are air to see if it needs to draw the faces on that side.
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    ///0 is air so when it's air it doesn't need to draw anything and it then continues.
                    if (tiles[x, y, z] == 0)
                        continue;

                    ///When not air it will add a cash tile to set it's neighbour face information
                    TileDrawInformation tile = new TileDrawInformation(x, y, z, tiles[x,y,z] - 1);

                    ///Checks to draw the top or bottom face based on if the neighbouring tile is empty or if it is at the border of the chunk.
                    if (y == WorldSettings.chunkHeight - 1 || tiles[x, y + 1, z] == 0)
                        tile.top = true;
                    if (y == 0 || tiles[x, y - 1, z] == 0)
                        tile.bottom = true;


                    ///Checks the neighbouring tiles on the sides if it is air, it also checks the tiles on the neighbouring chunks.
                    ///It also checks if the tiles are at the edge of the world area. When one of these is true it will draw the face.
                    if (z != chunkSize - 1 && tiles[x, y, z + 1] == 0 || (z == chunkSize - 1 && (zIndex == WorldSettings.worldSize - 1 || WorldData.chunks[xIndex, zIndex + 1].tiles[x, y, 0] == 0)))
                        tile.front = true;
                    if (z != 0 && tiles[x, y, z - 1] == 0 || (z == 0 && (zIndex == 0 || WorldData.chunks[xIndex, zIndex - 1].tiles[x, y, chunkSize - 1] == 0)))
                        tile.back = true;

                    if (x != chunkSize - 1 && tiles[x + 1, y, z] == 0 || (x == chunkSize - 1 && (xIndex == WorldSettings.worldSize - 1 || WorldData.chunks[xIndex + 1, zIndex].tiles[0, y, z] == 0)))
                        tile.right = true;
                    if (x != 0 && tiles[x - 1, y, z] == 0 || (x == 0 && (xIndex == 0 || WorldData.chunks[xIndex - 1, zIndex].tiles[chunkSize - 1, y, z] == 0)))
                        tile.left = true;

                    ///If any of the faces should be drawn it will add it to the drawtiles, otherwise it will be discarded.
                    if (tile.top || tile.bottom || tile.front || tile.back || tile.right || tile.left)
                        drawTiles.Add(tile);
                }
            }
        }

        ///Returns the tiles that should be drawn.
        return drawTiles;
    }

    #region constructors
    //Constructors
    ///Mesh cash information used for the cleaned tile list.
    public class MeshCashInformation
    {
        public List<TileDrawInformation> cleanTiles = new List<TileDrawInformation>();
    }

    ///Mesh Tile group cash used for saving the mesh data for the chunk generation.
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

    ///DrawInformation with the generated mesh and material
    public class TileGroupDrawInformation
    {
        public int materialType;
        public Mesh mesh;
    }

    ///TilDrawInformation with the information of each tile what faces should be drawn.
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
