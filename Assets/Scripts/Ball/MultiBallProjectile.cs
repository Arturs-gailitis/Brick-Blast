using System.Collections;
using UnityEngine;

public class MultiBallProjectile : MonoBehaviour
{
    public int AttackStrength
    {
        get
        {
            if (ownerBall == null)
            {
                return 1;
            }

            return ownerBall.AttackStrength;
        }
    }

    public PlayerBallTrajectory OwnerBall => ownerBall;

    public bool HasReturned => hasReturned;

    [Header("Bottom exit")]
    [SerializeField] [Min(0.01f)] private float exitSpeed = 12f;
    [SerializeField] [Min(0f)] private float outsideScreenMargin = 0.2f;
    [SerializeField] [Min(0.1f)] private float fallbackExitDistance = 2f;
    [SerializeField] [Min(0.001f)] private float behindBottomWallZOffset = 0.05f;

    private MultiBallShooter shooter;
    private PlayerBallTrajectory ownerBall;
    private Rigidbody2D ballRigidbody;
    private Collider2D ballCollider;
    private Camera mainCamera;
    private LayerMask bottomWallLayer;

    private float bottomWallGap;
    private bool hasReturned;
    private bool isExitingScreen;

    private Vector2 velocityBeforeCollision;

    private bool correctSideWallBounceOnNextFixedUpdate;
    private float sideWallBounceVerticalSign = 1f;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody2D>();

        ballCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;

        ballRigidbody.gravityScale = 0f;
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
    }

    public void Initialize(MultiBallShooter newShooter, PlayerBallTrajectory newOwnerBall, Vector2 direction,
        float ballSpeed, LayerMask newBottomWallLayer, float newBottomWallGap)
    {
        shooter = newShooter;
        ownerBall = newOwnerBall;

        bottomWallLayer = newBottomWallLayer;

        bottomWallGap = newBottomWallGap;

        hasReturned = false;
        isExitingScreen = false;

        correctSideWallBounceOnNextFixedUpdate = false;

        sideWallBounceVerticalSign = direction.y < 0f ? -1f : 1f;

        ballCollider.enabled = true;

        ballRigidbody.simulated = true;
        ballRigidbody.gravityScale = 0f;
        ballRigidbody.angularVelocity = 0f;

        ballRigidbody.WakeUp();

        ballRigidbody.linearVelocity = direction.normalized * ballSpeed;

        velocityBeforeCollision = ballRigidbody.linearVelocity;
    }

    private void FixedUpdate()
    {
        if (hasReturned || isExitingScreen)
        {
            return;
        }

        velocityBeforeCollision = ballRigidbody.linearVelocity;

        if (!correctSideWallBounceOnNextFixedUpdate)
        {
            return;
        }

        correctSideWallBounceOnNextFixedUpdate = false;

        CorrectTooHorizontalVelocity();

        velocityBeforeCollision = ballRigidbody.linearVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int collisionLayer = collision.collider.gameObject.layer;

        bool hitBottomWall = (bottomWallLayer.value & (1 << collisionLayer)) != 0;

        if (hitBottomWall)
        {
            StartLeavingScreen(collision);
            return;
        }

        if (!hasReturned && !isExitingScreen && IsSideWallCollision(collision, collisionLayer))
        {
            RememberSideWallBounceDirection(collision);

            correctSideWallBounceOnNextFixedUpdate = true;
        }
    }

    private bool IsSideWallCollision(Collision2D collision, int collisionLayer)
    {
        if (ownerBall == null)
        {
            return false;
        }

        if (!ownerBall.IsTrajectoryWallLayer(collisionLayer))
        {
            return false;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            bool contactIsHorizontal = Mathf.Abs(contact.normal.x) > Mathf.Abs(contact.normal.y);

            if (contactIsHorizontal)
            {
                return true;
            }
        }

        return false;
    }

    private void RememberSideWallBounceDirection(Collision2D collision)
    {
        float verticalVelocity = ballRigidbody.linearVelocity.y;

        if (Mathf.Abs(verticalVelocity) < 0.0001f)
        {
            verticalVelocity = collision.relativeVelocity.y;
        }

        if (Mathf.Abs(verticalVelocity) >= 0.0001f)
        {
            sideWallBounceVerticalSign = Mathf.Sign(verticalVelocity);
        }
    }

    private void CorrectTooHorizontalVelocity()
    {
        Vector2 velocity = ballRigidbody.linearVelocity;

        float speed = velocity.magnitude;

        if (speed < 0.0001f)
        {
            return;
        }

        Vector2 direction = velocity / speed;

        float minimumVerticalDirection = ownerBall != null ? ownerBall.MinimumSideWallVerticalDirection : 0.12f;

        if (Mathf.Abs(direction.y) >= minimumVerticalDirection)
        {
            return;
        }

        float verticalSign;

        if (Mathf.Abs(direction.y) >= 0.0001f)
        {
            verticalSign = Mathf.Sign(direction.y);
        }
        else
        {
            verticalSign = sideWallBounceVerticalSign;
        }

        float horizontalSign = direction.x < 0f ? -1f : 1f;

        float correctedY = verticalSign * minimumVerticalDirection;

        float correctedX = horizontalSign * Mathf.Sqrt(1f - correctedY * correctedY);

        Vector2 correctedDirection = new Vector2(correctedX, correctedY);

        ballRigidbody.linearVelocity = correctedDirection * speed;
    }

    public void IncreaseAttackStrength(int amount)
    {
        if (ownerBall != null)
        {
            ownerBall.IncreaseAttackStrength(amount);
        }
    }

    private void StartLeavingScreen(Collision2D collision)
    {
        if (hasReturned || isExitingScreen)
        {
            return;
        }

        isExitingScreen = true;
        correctSideWallBounceOnNextFixedUpdate = false;

        float ballHalfHeight = ballCollider.bounds.extents.y;

        float safeY = collision.collider.bounds.max.y + ballHalfHeight +
        bottomWallGap;

        Vector2 returnPosition = ballRigidbody.position;

        returnPosition.y = safeY;

        ballRigidbody.angularVelocity = 0f;

        ballRigidbody.position = returnPosition;

        float wallZ = collision.collider.transform.position.z;

        float cameraZ = mainCamera != null ? mainCamera.transform.position.z : -10f;

        float awayFromCameraDirection = wallZ >= cameraZ ? 1f : -1f;

        float exitZ = wallZ + awayFromCameraDirection * Mathf.Abs(behindBottomWallZOffset);

        transform.position = new Vector3(returnPosition.x, returnPosition.y, exitZ);

        ballCollider.enabled = false;

        ballRigidbody.simulated = true;
        ballRigidbody.WakeUp();

        Vector2 exitDirection = velocityBeforeCollision;

        if (exitDirection.sqrMagnitude < 0.0001f)
        {
            exitDirection = ballRigidbody.linearVelocity;
        }

        if (exitDirection.sqrMagnitude < 0.0001f)
        {
            exitDirection = Vector2.down;
        }

        exitDirection.y = -Mathf.Abs(exitDirection.y);
        exitDirection.Normalize();

        ballRigidbody.linearVelocity = exitDirection * Mathf.Max(0.01f, exitSpeed);

        StartCoroutine(LeaveScreen(returnPosition));
    }

    private IEnumerator LeaveScreen(Vector2 returnPosition)
    {
        float targetY = GetOutsideScreenY(returnPosition.y);

        while (ballRigidbody != null && ballRigidbody.position.y > targetY)
        {
            yield return new WaitForFixedUpdate();
        }

        hasReturned = true;
        isExitingScreen = false;

        if (ballRigidbody != null)
        {
            ballRigidbody.linearVelocity = Vector2.zero;
            ballRigidbody.angularVelocity = 0f;
            ballRigidbody.simulated = false;
        }

        if (shooter != null)
        {
            shooter.ProjectileReturned(this, returnPosition);
        }

        if (gameObject.activeSelf)
        {
            RemoveProjectile();
        }
    }

    private float GetOutsideScreenY(float returnY)
    {
        float targetY = returnY - Mathf.Max(0.1f, fallbackExitDistance);

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return targetY;
        }

        float distanceToBallPlane = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        float screenBottomY = mainCamera.ScreenToWorldPoint(new Vector3(0f, 0f, distanceToBallPlane)).y;

        float ballHalfHeight = ballCollider != null ? ballCollider.bounds.extents.y : 0f;

        float cameraExitY = screenBottomY - ballHalfHeight - outsideScreenMargin;

        return Mathf.Min(targetY, cameraExitY);
    }

    public void RemoveProjectile()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}