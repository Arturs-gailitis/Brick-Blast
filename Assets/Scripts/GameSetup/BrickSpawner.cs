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
    [SerializeField] private float horizontalSpacing = 0.08f;
    [SerializeField] private float verticalSpacing = 0.08f;
    [SerializeField] private float distanceFromTopWall = 0.5f;
    [SerializeField] private float horizontalOffset = 0.15f;

    private Transform bricksParent;

    private void Awake()
    {
        GameObject parentObject = new GameObject("Bricks");
        bricksParent = parentObject.transform;
    }

    public bool LevelExists(int level)
    {
        if (brickConfigReader == null)
        {
            return false;
        }

        return brickConfigReader.GetBricksForLevel(level).Count > 0;
    }

    public int SpawnLevel(int level)
    {
        ClearBricks();

        if (brickPrefab == null || brickConfigReader == null)
        {
            return 0;
        }

        if (!TryGetWalls(out Collider2D leftWall, out Collider2D rightWall, out Collider2D topWall))
        {
            return 0;
        }

        BoxCollider2D brickCollider = brickPrefab.GetComponent<BoxCollider2D>();

        if (brickCollider == null)
        {
            return 0;
        }

        List<BrickConfig> levelBricks = brickConfigReader.GetBricksForLevel(level);

        if (levelBricks.Count == 0)
        {
            return 0;
        }

        float brickWidth = brickCollider.size.x * Mathf.Abs(brickPrefab.transform.localScale.x);
        float brickHeight = brickCollider.size.y * Mathf.Abs(brickPrefab.transform.localScale.y);

        int largestColumn = 0;

        foreach (BrickConfig brickConfig in levelBricks)
        {
            largestColumn = Mathf.Max(largestColumn, brickConfig.column);
        }

        int gridColumns = largestColumn + 1;

        float gridWidth = gridColumns * brickWidth + (gridColumns - 1) * horizontalSpacing;

        float availableWidth = rightWall.bounds.min.x - leftWall.bounds.max.x;

        if (gridWidth > availableWidth)
        {
            return 0;
        }

        float firstBrickX = (leftWall.bounds.max.x + rightWall.bounds.min.x) / 2f - gridWidth / 2f + brickWidth / 2f 
            + horizontalOffset;

        float firstBrickY = topWall.bounds.min.y - distanceFromTopWall - brickHeight / 2f;

        int spawnedBricks = 0;

        foreach (BrickConfig brickConfig in levelBricks)
        {
            float x = firstBrickX + brickConfig.column * (brickWidth + horizontalSpacing);

            float y = firstBrickY - brickConfig.row * (brickHeight + verticalSpacing);

            GameObject newBrick = Instantiate(brickPrefab, new Vector3(x, y, 0f), Quaternion.identity, bricksParent);

            BrickCollision brickCollision = newBrick.GetComponent<BrickCollision>();

            if (brickCollision == null)
            {
                Destroy(newBrick);
                continue;
            }

            newBrick.name = "Brick_" + level + "_" + brickConfig.row + "_" + brickConfig.column;

            brickCollision.Configure(brickConfig);
            ApplyColor(newBrick, brickConfig.colorHex);

            spawnedBricks++;
        }

        return spawnedBricks;
    }

    private bool TryGetWalls( out Collider2D leftWall, out Collider2D rightWall, out Collider2D topWall)
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

        if (topWall == null)
        {
            return false;
        }

        return true;
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

    private void ApplyColor(GameObject brick, string colorHex)
    {
        SpriteRenderer spriteRenderer = brick.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            spriteRenderer.color = color;
        }
    }
}