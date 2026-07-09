using System.Collections.Generic;
using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject brickPrefab;
    [SerializeField] private BrickConfigReader brickConfigReader;

    [Header("Walls")]
    [SerializeField] private Transform[] walls;

    [Header("Spacing")]
    [SerializeField] [Min(1)] private int gridColumns = 7;
    [SerializeField] private float horizontalSpacing = 0.08f;
    [SerializeField] private float verticalSpacing = 0.08f;
    [SerializeField] private float distanceFromTopWall = 0.5f;
    [SerializeField] private float horizontalOffset = 0.25f;

    private List<BrickConfig> currentLevelBricks = new List<BrickConfig>();
    private int nextRowToSpawn;
    private int maxRowInLevel = -1;
    private int selectedLevel = 1;

    private float cachedFirstBrickX;
    private float cachedFirstBrickY;
    private float cachedBrickWidth;
    private float cachedBrickHeight;
    private bool hasCachedGrid;

    private Transform bricksParent;

    private void Awake()
    {
        GameObject parentObject = new GameObject("Bricks");
        bricksParent = parentObject.transform;
    }

    public bool LevelExists(int level)
    {
        return brickConfigReader != null && brickConfigReader.GetBricksForLevel(level).Count > 0;
    }

    public int SpawnLevel(int level)
    {
        return SpawnLevel(level, 3);
        }

    public int SpawnLevel(int level, int visibleRowsAtStart)
    {
        ClearBricks();

        selectedLevel = level;
        nextRowToSpawn = 0;
        maxRowInLevel = -1;
        hasCachedGrid = false;

        if (brickPrefab == null || brickConfigReader == null)
        {
            return 0;
        }

        currentLevelBricks = brickConfigReader.GetBricksForLevel(level);

        if (currentLevelBricks.Count == 0)
        {
            return 0;
        }

        if (!PrepareGrid())
        {
            return 0;
        }

        maxRowInLevel = GetMaxRow(currentLevelBricks);

        for (int i = 0; i < visibleRowsAtStart; i++)
        {
            SpawnCurrentNextRow(nextRowToSpawn);
        }

        return currentLevelBricks.Count;
    }

    public int SpawnSavedLevel(List<SavedBrickData> savedBricks)
    {
        ClearBricks();

        if (brickPrefab == null || savedBricks == null)
        {
            return 0;
        }

        int spawnedBricks = 0;

        foreach (SavedBrickData savedBrick in savedBricks)
        {
            if (savedBrick == null)
            {
                continue;
            }

            GameObject newBrick = Instantiate(
                brickPrefab, new Vector3(savedBrick.x, savedBrick.y, 0f), Quaternion.identity, bricksParent
            );

            BrickCollision brickCollision = newBrick.GetComponent<BrickCollision>();

            if (brickCollision == null)
            {
                Destroy(newBrick);
                continue;
            }

            newBrick.name = "SavedBrick_" + spawnedBricks;

            brickCollision.ConfigureSaved(savedBrick);
            spawnedBricks++;
        }

        return spawnedBricks;
    }

    public List<SavedBrickData> GetCurrentBricks()
    {
        List<SavedBrickData> savedBricks = new List<SavedBrickData>();

        if (bricksParent == null)
        {
            return savedBricks;
        }

        for (int i = 0; i < bricksParent.childCount; i++)
        {
            BrickCollision brick = bricksParent.GetChild(i).GetComponent<BrickCollision>();

            if (brick != null)
            {
                savedBricks.Add(brick.CreateSaveData());
            }
        }

        return savedBricks;
    }

    private bool TryGetWalls(out Collider2D leftWall, out Collider2D rightWall, out Collider2D topWall)
    {
        leftWall = null;
        rightWall = null;
        topWall = null;

        if (walls == null || walls.Length < 2)
        {
            return false;
        }

        Collider2D[] sideWallColliders = walls[0].GetComponentsInChildren<Collider2D>();

        if (sideWallColliders.Length < 2)
        {
            return false;
        }

        leftWall = sideWallColliders[0];
        rightWall = sideWallColliders[1];

        if (leftWall.bounds.center.x > rightWall.bounds.center.x)
        {
            Collider2D temporaryWall = leftWall;
            leftWall = rightWall;
            rightWall = temporaryWall;
        }

        topWall = walls[1].GetComponent<Collider2D>();

        return topWall != null;
    }

    private void ClearBricks()
    {
        if (bricksParent == null)
        {
            return;
        }

        for (int i = bricksParent.childCount - 1; i >= 0; i--)
        {
            Destroy(bricksParent.GetChild(i).gameObject);
        }
    }

    public int SpawnNextRowAtTop()
    {
        return SpawnCurrentNextRow(0);
        }

    private int SpawnCurrentNextRow(int visualRow)
    {
        if (!hasCachedGrid || currentLevelBricks == null)
        {
            return 0;
        }

        if (nextRowToSpawn > maxRowInLevel)
        {
            return 0;
        }

        int spawnedBricks = SpawnRow(nextRowToSpawn, visualRow);

        nextRowToSpawn++;

        return spawnedBricks;
    }

    private int SpawnRow(int csvRow, int visualRow)
    {
        int spawnedBricks = 0;

        foreach (BrickConfig brickConfig in currentLevelBricks)
        {
            if (brickConfig.row != csvRow)
            {
                continue;
            }

            if (brickConfig.column >= gridColumns)
            {
                continue;
            }

            float x = cachedFirstBrickX + brickConfig.column * (cachedBrickWidth + horizontalSpacing);
            float y = cachedFirstBrickY - visualRow * (cachedBrickHeight + verticalSpacing);

            GameObject newBrick = Instantiate(
                brickPrefab,
                new Vector3(x, y, 0f),
                Quaternion.identity,
                bricksParent
            );

            BrickCollision brickCollision = newBrick.GetComponent<BrickCollision>();

            if (brickCollision == null)
            {
                Destroy(newBrick);
                continue;
            }

            newBrick.name =
                "Brick_" +
                selectedLevel + "_" +
                brickConfig.row + "_" +
                brickConfig.column;

            brickCollision.Configure(brickConfig);

            spawnedBricks++;
        }

        return spawnedBricks;
    }

    private bool PrepareGrid()
    {
        if (!TryGetWalls(out Collider2D leftWall, out Collider2D rightWall, out Collider2D topWall))
        {
            return false;
        }

        BoxCollider2D brickCollider = brickPrefab.GetComponent<BoxCollider2D>();

        if (brickCollider == null)
        {
            return false;
        }

        cachedBrickWidth =
            brickCollider.size.x *
            Mathf.Abs(brickPrefab.transform.localScale.x);

        cachedBrickHeight =
            brickCollider.size.y *
            Mathf.Abs(brickPrefab.transform.localScale.y);

        float gridWidth =
            gridColumns * cachedBrickWidth +
            (gridColumns - 1) * horizontalSpacing;

        float availableWidth =
            rightWall.bounds.min.x - leftWall.bounds.max.x;

        if (gridWidth > availableWidth)
        {
            return false;
        }

        cachedFirstBrickX =
            (leftWall.bounds.max.x + rightWall.bounds.min.x) / 2f -
            gridWidth / 2f +
            cachedBrickWidth / 2f +
            horizontalOffset;

        cachedFirstBrickY =
            topWall.bounds.min.y -
            distanceFromTopWall -
            cachedBrickHeight / 2f;

        hasCachedGrid = true;

        return true;
    }

    private int GetMaxRow(List<BrickConfig> bricks)
    {
        int maxRow = -1;

        foreach (BrickConfig brickConfig in bricks)
        {
            maxRow = Mathf.Max(maxRow, brickConfig.row);
        }

        return maxRow;
    }
}