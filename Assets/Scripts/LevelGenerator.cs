using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject empty;            // 0
    public GameObject outsideCorner;    // 1
    public GameObject outsideWall;      // 2
    public GameObject insideCorner;     // 3
    public GameObject insideWall;       // 4
    public GameObject standardPellet;   // 5
    public GameObject powerPellet;      // 6
    public GameObject tJunction;        // 7
    public GameObject ghostExit;        // 8

    private float tileSize = 1.0f;
    private GameObject[,] spawnedTiles;

    // TO MARKER: Replace quad with another array for test case marking
    private int[,] quad = new int[,]
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };


    private int[,] full;

    void Start()
    {
        GameObject manualLevel = GameObject.Find("Level");
        Destroy(manualLevel);
        full = BuildFullMap(quad);

        var prevGen = GameObject.Find("GeneratedLevel");
        if (prevGen) Destroy(prevGen);
        var newLevel = new GameObject("GeneratedLevel").transform;

        var cam = Camera.main;
        var camPos = cam.transform.position;
        newLevel.position = new Vector3(camPos.x, camPos.y, 0f);

        Generate(full, newLevel);
    }

    int[,] BuildFullMap(int[,] quad)
    {
        // Dimensions of original quadrant
        int height = quad.GetLength(0);
        int width = quad.GetLength(1);

        // Dimensions of full map (exluding 1 row)
        int fullHeight = height * 2 - 1;
        int fullWidth = (width * 2);

        // Top half of the map
        int[,] topHalf = new int[height, fullWidth];

        // Left half: original
        for (int row = 0; row < height; row++)
            for (int col = 0; col < width; col++)
                topHalf[row, col] = quad[row, col];

        // Right half: horizontal mirror
        for (int row = 0; row < height; row++)
            for (int col = 0; col < width; col++)
                topHalf[row, fullWidth - 1 - col] = quad[row, col];

        int[,] fullMap = new int[fullHeight, fullWidth];

        // Copy top half map
        for (int row = 0; row < height; row++)
            for (int col = 0; col < fullWidth; col++)
                fullMap[row, col] = topHalf[row, col];

        // Reflect the top half, exluding the bottom row
        for (int row = 0; row < height - 1; row++)
        {
            int srcRow = fullHeight - 1 - row;
            for (int col = 0; col < fullWidth; col++)
                fullMap[srcRow, col] = topHalf[row, col];
        }
        return fullMap;
    }

    void Generate(int[,] map, Transform parent)
    {
        int rowSize = map.GetLength(0);
        int colSize = map.GetLength(1);

        spawnedTiles = new GameObject[rowSize, colSize];

        for (int row = 0; row < rowSize; row++)
        {
            for (int col = 0; col < colSize; col++)
            {
                int id = map[row, col];
                GameObject prefab = GetPrefab(id);
                if (!prefab) continue;

                Vector3 pos = GridToWorldCentered(col, row, colSize, rowSize);
                var tile = Instantiate(prefab, pos, Quaternion.identity, parent);
                tile.transform.rotation = FindRotation(id, map, row, col);
                spawnedTiles[row, col] = tile;
            }
        }
    }

    Vector3 GridToWorldCentered(int col, int row, int cols, int rows)
    {
        float cx = (cols - 1) * 0.5f;
        float cy = (rows - 1) * 0.5f;

        float x = (col - cx) * tileSize;
        float y = -(row - cy) * tileSize;
        return new Vector3(x, y, 0f);
    }

    GameObject GetPrefab(int id)
    {
        switch(id)
        {
            case 0: return empty;
            case 1: return outsideCorner;
            case 2: return outsideWall;
            case 3: return insideCorner;
            case 4: return insideWall;
            case 5: return standardPellet;
            case 6: return powerPellet;
            case 7: return tJunction;
            case 8: return ghostExit;
            default: return null;
        }
    }

    Quaternion FindRotation(int id, int[,] map, int row, int col)
    {
        // return if tile has no rotation
        if (id == 0 || id == 5 || id == 6) return Quaternion.identity;

        // Get the IDs of the tiles neighbours
        int up = GetID(map, row-1, col);
        int down = GetID(map, row+1, col);
        int left = GetID(map, row, col-1);
        int right = GetID(map, row, col+1);

        // Get wall status for neighbouring tiles
        bool wU = isWallLike(up);
        bool wD = isWallLike(down);
        bool wL = isWallLike(left);
        bool wR = isWallLike(right);

        // Get Straight wall status for neighbouring tiles
        bool sU = isStraightWall(up);
        bool sD = isStraightWall(down);
        bool sL = isStraightWall(left);
        bool sR = isStraightWall(right);

        // Case: Straight walls
        if (id == 2 || id == 4 || id == 8)
        {
            if ((wL && wR) && !(wU && wD)) return Quaternion.identity;
            if ((wU && wD) && !(wL && wR)) return Quaternion.Euler(0, 0, 90f);
            if (wL || wR) return Quaternion.identity;
            if (wU || wD) return Quaternion.Euler(0, 0, 90f);
            return Quaternion.identity;
        }

        // Case: Corner walls
        if (id == 1 || id == 3)
        {
            // Check for adjacent straight walls that extend more than 1 tile
            bool isLinkedU = (sU && isStraightWall(GetID(map, row-2, col)));
            bool isLinkedD = (sD && isStraightWall(GetID(map, row+2, col)));
            bool isLinkedL = (sL && isStraightWall(GetID(map, row, col-2)));
            bool isLinkedR = (sR && isStraightWall(GetID(map, row, col+2)));

            // Case 1: Only 2 neighbouring walls
            if ((wR && wD) && !(wL && wU)) return Quaternion.identity;
            if ((wL && wD) && !(wR && wU)) return Quaternion.Euler(0, 0, 270f);
            if ((wL && wU) && !(wR && wD)) return Quaternion.Euler(0, 0, 180f);
            if ((wR && wU) && !(wL && wD)) return Quaternion.Euler(0, 0, 90f);

            // Case 2: Connected to 2 straight walls
            if (isLinkedR && isLinkedD) return Quaternion.identity;
            if (isLinkedL && isLinkedD) return Quaternion.Euler(0, 0, 270f);
            if (isLinkedL && isLinkedU) return Quaternion.Euler(0, 0, 180f);
            if (isLinkedR && isLinkedU) return Quaternion.Euler(0, 0, 90f);
            return Quaternion.identity;
        }

        // Case T Junction
        if (id == 7)
        {
            if (!wU && wD && wL && wR) return Quaternion.identity;
            if (!wR && wD && wU && wL) return Quaternion.Euler(0,0, 90f);
            if (!wD && wU && wL && wR) return Quaternion.Euler(0, 0, 180f);
            if (!wL && wD && wU && wR) return Quaternion.Euler(0, 0, 270f);
            return Quaternion.identity;
        }
        return Quaternion.identity;
    }

    //Helpers

    bool isWallLike(int id)
    {
        return id == 1 || id == 2 || id == 3 || id == 4 || id == 7 || id == 8;
    }

    bool isStraightWall(int id)
    {
        return id == 2 || id == 4 || id == 8;
    }

    int GetID(int[,] map, int row, int col)
    {
        if (row < 0 || col < 0 || row >= map.GetLength(0) || col >= map.GetLength(1)) return 0;
        return map[row, col];
    }
    
    public bool IsWalkable(int gridX, int gridY)
    {
        if (full == null) return false;
        
        // Check bounds
        if (gridX < 0 || gridX >= full.GetLength(1) || gridY < 0 || gridY >= full.GetLength(0))
            return false;
        
        int tileType = full[gridY, gridX];
        
        // Walkable tiles: empty (0), standardPellet (5), powerPellet (6)
        // Non-walkable tiles: walls (1,2,3,4,7,8)
        return tileType == 0 || tileType == 5 || tileType == 6;
    }
    
    // Public method to check if a grid position has a pellet
    public bool HasPellet(int gridX, int gridY)
    {
        if (full == null) return false;
        
        // Check bounds
        if (gridX < 0 || gridX >= full.GetLength(1) || gridY < 0 || gridY >= full.GetLength(0))
            return false;
        
        int tileType = full[gridY, gridX];
        
        // Pellet tiles: standardPellet (5), powerPellet (6)
        return tileType == 5 || tileType == 6;
    }

    public bool ConsumePellet(int gridX, int gridY, out bool wasPowerPellet)
    {
        wasPowerPellet = false;

        if (full == null || spawnedTiles == null)
            return false;

        if (gridX < 0 || gridX >= full.GetLength(1) || gridY < 0 || gridY >= full.GetLength(0))
            return false;

        int tileType = full[gridY, gridX];

        if (tileType != 5 && tileType != 6)
            return false;

        wasPowerPellet = tileType == 6;

        if (wasPowerPellet)
        {
            return false;
        }

        GameObject pelletTile = spawnedTiles[gridY, gridX];
        Transform parent = null;
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        if (pelletTile != null)
        {
            parent = pelletTile.transform.parent;
            position = pelletTile.transform.position;
            rotation = pelletTile.transform.rotation;
            Destroy(pelletTile);
            spawnedTiles[gridY, gridX] = null;
        }

        full[gridY, gridX] = 0;

        if (empty != null && parent != null)
        {
            GameObject replacement = Instantiate(empty, position, rotation, parent);
            spawnedTiles[gridY, gridX] = replacement;
        }

        return true;
    }

}
