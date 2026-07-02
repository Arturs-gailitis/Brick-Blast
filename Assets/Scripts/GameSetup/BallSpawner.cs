using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerBallPrefab;
    [SerializeField] private Transform bottomWall;
    [SerializeField] private Transform sideWalls;

    [SerializeField] private float distanceFromWall = 0.1f;

 private void Start()
{
    Collider2D bottomWallCollider = bottomWall.GetComponent<Collider2D>();
    Collider2D leftWallCollider = GetWallCollider("LeftWall");
    Collider2D rightWallCollider = GetWallCollider("RightWall");

    CircleCollider2D ballCollider = playerBallPrefab.GetComponent<CircleCollider2D>();

    float ballRadius = ballCollider.radius * playerBallPrefab.transform.localScale.x;

    float minX = leftWallCollider.bounds.max.x + ballRadius;
    float maxX = rightWallCollider.bounds.min.x - ballRadius;

    float randomX = Random.Range(minX, maxX);

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

    private Collider2D GetWallCollider(string wallName)
    {
        Collider2D[] colliders = sideWalls.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D wallCollider in colliders)
        {
            if (wallCollider.gameObject.name == wallName)
            {
                return wallCollider;
            }
        }

        return null;
    }
}