using System.Collections;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerBallPrefab;

    [Header("Optional fallback references")]
    [SerializeField] private Transform bottomWall;
    [SerializeField] private Transform sideWalls;

    [Header("Spawn settings")]
    [SerializeField] private float distanceFromWall = 0.1f;
    [SerializeField] private float insideSideWallMargin = 0.03f;

    private IEnumerator Start()
    {
        yield return null;

        Physics2D.SyncTransforms();

        Collider2D bottomWallCollider = GetRuntimeBottomWall();
        Collider2D leftWallCollider = GetRuntimeWallCollider("LeftWall");
        Collider2D rightWallCollider = GetRuntimeWallCollider("RightWall");

        if (playerBallPrefab == null || bottomWallCollider == null || leftWallCollider == null || rightWallCollider == null)
        {
            yield break;
        }

        CircleCollider2D ballCollider = playerBallPrefab.GetComponent<CircleCollider2D>();

        if (ballCollider == null)
        {
            yield break;
        }

        float ballScale = Mathf.Max(
            Mathf.Abs(playerBallPrefab.transform.localScale.x),
            Mathf.Abs(playerBallPrefab.transform.localScale.y)
        );

        float ballRadius = ballCollider.radius * ballScale;

        float minX = leftWallCollider.bounds.max.x + ballRadius + insideSideWallMargin;
        float maxX = rightWallCollider.bounds.min.x - ballRadius - insideSideWallMargin;

        if (minX >= maxX)
        {
            yield break;
        }

        float randomX = Random.Range(minX, maxX);
        randomX = Mathf.Clamp(randomX, minX, maxX);

        float spawnY = bottomWallCollider.bounds.max.y + ballRadius + distanceFromWall;

        Vector3 spawnPosition = new Vector3(randomX, spawnY, 0f);

        GameObject ball = Instantiate(playerBallPrefab);
        ball.SetActive(false);

        ball.transform.position = spawnPosition;

        Rigidbody2D ballRigidbody = ball.GetComponent<Rigidbody2D>();

        if (ballRigidbody != null)
        {
            ballRigidbody.position = spawnPosition;
            ballRigidbody.linearVelocity = Vector2.zero;
            ballRigidbody.angularVelocity = 0f;
        }

        ball.SetActive(true);
    }

    private Collider2D GetRuntimeBottomWall()
    {
        GameObject bottomWallObject = GameObject.Find("ButtomWall");

        if (bottomWallObject != null)
        {
            return bottomWallObject.GetComponent<Collider2D>();
        }

        if (bottomWall != null && bottomWall.gameObject.scene.IsValid())
        {
            return bottomWall.GetComponent<Collider2D>();
        }

        return null;
    }

    private Collider2D GetRuntimeWallCollider(string wallName)
    {
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

        foreach (Collider2D wallCollider in colliders)
        {
            if (wallCollider.gameObject.name == wallName)
            {
                return wallCollider;
            }
        }

        if (sideWalls != null && sideWalls.gameObject.scene.IsValid())
        {
            Collider2D[] fallbackColliders = sideWalls.GetComponentsInChildren<Collider2D>();

            foreach (Collider2D wallCollider in fallbackColliders)
            {
                if (wallCollider.gameObject.name == wallName)
                {
                    return wallCollider;
                }
            }
        }

        return null;
    }
}