using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private GameObject brickPrefab;
    [SerializeField] private BrickConfigReader brickConfigReader;

    [Header("Level")]
    [SerializeField] [Min(1)] private int selectedLevel = 1;

    [Header("Walls")]
    [SerializeField] private Transform[] walls;

    [Header("Spacing")]
    [SerializeField] private float horizontalSpacing = 0.08f;
    [SerializeField] private float verticalSpacing = 0.08f;
    [SerializeField] private float distanceFromTopWall = 0.2f;

    private IEnumerator Start()
    {
        yield return null;

        SpawnBricksFromCsv();
    }

    private void SpawnBricksFromCsv()
    {
        if (brickPrefab == null)
        {
            return;
        }

        if (brickConfigReader == null)
        {
            return;
        }

        if (walls == null || walls.Length < 2 || walls[0] == null || walls[1] == null)
        {
            return;
        }

        List<BrickConfig> bricksForLevel = brickConfigReader.GetBricksForLevel(selectedLevel);

        if (bricksForLevel.Count == 0)
        {
            return;
        }

        Transform sideWalls = walls[0];
        Transform topWall = walls[1];

        Collider2D[] sideWallColliders = sideWalls.GetComponentsInChildren<Collider2D>();

        if (sideWallColliders.Length < 2)
        {
            return;
        }

        Collider2D leftWall = sideWallColliders[0];
        Collider2D rightWall = sideWallColliders[1];

        if (leftWall.bounds.center.x > rightWall.bounds.center.x)
        {
            Collider2D temporaryWall = leftWall;
            leftWall = rightWall;
            rightWall = temporaryWall;
        }

        Collider2D topWallCollider = topWall.GetComponent<Collider2D>();

        if (topWallCollider == null)
        {
            return;
        }

        BoxCollider2D brickCollider = brickPrefab.GetComponent<BoxCollider2D>();

        if (brickCollider == null)
        {
            return;
        }

        float brickWidth = brickCollider.size.x * Mathf.Abs(brickPrefab.transform.localScale.x);

        float brickHeight = brickCollider.size.y * Mathf.Abs(brickPrefab.transform.localScale.y);

        int numberOfColumns = GetRequiredColumnCount(bricksForLevel);

        float availableWidth = rightWall.bounds.min.x - leftWall.bounds.max.x;

        float gridWidth = numberOfColumns * brickWidth + (numberOfColumns - 1) * horizontalSpacing;

        if (gridWidth > availableWidth)
        {
            return;
        }

        float firstBrickX = (leftWall.bounds.max.x + rightWall.bounds.min.x) / 2f - gridWidth / 2f + brickWidth / 2f;

        float firstBrickY = topWallCollider.bounds.min.y - distanceFromTopWall - brickHeight / 2f;

        GameObject bricksParent = new GameObject($"Bricks_Level_{selectedLevel}");

        foreach (BrickConfig brickConfig in bricksForLevel)
        {
            if (brickConfig.row < 0 || brickConfig.column < 0)
            {
                continue;
            }

            float x = firstBrickX + brickConfig.column * (brickWidth + horizontalSpacing);

            float y = firstBrickY - brickConfig.row * (brickHeight + verticalSpacing);

            GameObject newBrick = Instantiate(
                brickPrefab,
                new Vector3(x, y, 0f),
                Quaternion.identity,
                bricksParent.transform
            );

            ConfigureBrick(newBrick, brickConfig);
        }
    }

    private int GetRequiredColumnCount(List<BrickConfig> bricksForLevel)
    {
        int largestColumn = 0;

        foreach (BrickConfig brickConfig in bricksForLevel)
        {
            largestColumn = Mathf.Max(largestColumn, brickConfig.column);
        }

        return largestColumn + 1;
    }

    private void ConfigureBrick(GameObject newBrick, BrickConfig brickConfig)
    {
        BrickCollision brickCollision = newBrick.GetComponent<BrickCollision>();

        if (brickCollision != null)
        {
            brickCollision.Configure(brickConfig);
        }

        SpriteRenderer spriteRenderer = newBrick.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && ColorUtility.TryParseHtmlString(brickConfig.colorHex, out Color brickColor))
        {
            spriteRenderer.color = brickColor;
        }
    }
}