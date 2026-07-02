using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private GameObject brickPrefab;

    [Header("Walls")]
    [SerializeField] private Transform[] walls;

    [Header("Potential Block Places")]
    [SerializeField] private int rows = 4;
    [SerializeField] private int columns = 6;
    [SerializeField] private int bricksToSpawn = 2;

    [Header("Spacing")]
    [SerializeField] private float horizontalSpacing = 0.08f;
    [SerializeField] private float verticalSpacing = 0.08f;
    [SerializeField] private float distanceFromTopWall = 0.2f;

    private IEnumerator Start()
    {
        yield return null;

        SpawnBricks();
    }

    private void SpawnBricks()
    {
        if (brickPrefab == null)
        {
            return;
        }

        if (walls == null || walls.Length < 2)
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

        float availableWidth = rightWall.bounds.min.x - leftWall.bounds.max.x;

        float gridWidth = columns * brickWidth + (columns - 1) * horizontalSpacing;

        if (gridWidth > availableWidth)
        {
            return;
        }

        float firstBrickX = (leftWall.bounds.max.x + rightWall.bounds.min.x) / 2f - gridWidth / 2f + brickWidth / 2f;

        float firstBrickY = topWallCollider.bounds.min.y - distanceFromTopWall - brickHeight / 2f;

        GameObject bricksParent = new GameObject("Bricks");

        List<Vector2Int> availablePositions = new List<Vector2Int>();

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                availablePositions.Add(new Vector2Int(row, column));
            }
        }

        int maximumBricks = Mathf.Min(bricksToSpawn, availablePositions.Count);

        for (int i = 0; i < maximumBricks; i++)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);

            Vector2Int selectedPosition = availablePositions[randomIndex];

            availablePositions.RemoveAt(randomIndex);

            float x = firstBrickX + selectedPosition.y * (brickWidth + horizontalSpacing);

            float y = firstBrickY - selectedPosition.x * (brickHeight + verticalSpacing);

            Instantiate(brickPrefab, new Vector3(x, y, 0f), Quaternion.identity, bricksParent.transform);
        }
    }
}