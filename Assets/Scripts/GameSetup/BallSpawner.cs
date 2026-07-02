using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerBallPrefab;
    [SerializeField] private Transform bottomWall;
    [SerializeField] private Transform sideWalls;

    [SerializeField] private float distanceFromWall = 0.1f;

    private void Start()
    {
        GameObject ball = Instantiate(playerBallPrefab);

        Collider2D bottomWallCollider = bottomWall.GetComponent<Collider2D>();
        Collider2D ballCollider = ball.GetComponent<Collider2D>();

        Collider2D leftWallCollider = GetWallCollider("LeftWall");
        Collider2D rightWallCollider = GetWallCollider("RightWall");

        if (leftWallCollider == null || rightWallCollider == null)
        {
            return;
        }

        float ballHalfHeight = ballCollider.bounds.extents.y;
        float ballHalfWidth = ballCollider.bounds.extents.x;

        float minX = leftWallCollider.bounds.max.x + ballHalfWidth;
        float maxX = rightWallCollider.bounds.min.x - ballHalfWidth;

        float randomX = Random.Range(minX, maxX);

        float spawnY = bottomWallCollider.bounds.max.y
                       + ballHalfHeight
                       + distanceFromWall;

        ball.transform.position = new Vector3(randomX, spawnY, 0f);
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