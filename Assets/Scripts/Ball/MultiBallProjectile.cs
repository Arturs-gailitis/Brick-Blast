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

    private MultiBallShooter shooter;
    private PlayerBallTrajectory ownerBall;
    private Rigidbody2D ballRigidbody;
    private Collider2D ballCollider;
    private LayerMask bottomWallLayer;

    private float bottomWallGap;
    private bool hasReturned;

    private bool correctSideWallBounceOnNextFixedUpdate;
    private float sideWallBounceVerticalSign = 1f;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody2D>();

        ballCollider = GetComponent<Collider2D>();

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

        correctSideWallBounceOnNextFixedUpdate = false;

        sideWallBounceVerticalSign = direction.y < 0f ? -1f : 1f;

        ballCollider.enabled = true;

        ballRigidbody.simulated = true;
        ballRigidbody.gravityScale = 0f;
        ballRigidbody.angularVelocity = 0f;

        ballRigidbody.WakeUp();

        ballRigidbody.linearVelocity = direction.normalized * ballSpeed;
    }

    private void FixedUpdate()
    {
        if (!correctSideWallBounceOnNextFixedUpdate || hasReturned)
        {
            return;
        }

        correctSideWallBounceOnNextFixedUpdate = false;

        CorrectTooHorizontalVelocity();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int collisionLayer = collision.collider.gameObject.layer;

        bool hitBottomWall = (bottomWallLayer.value & (1 << collisionLayer)) != 0;

        if (hitBottomWall)
        {
            ReturnProjectile(collision);
            return;
        }

        if (!hasReturned && IsSideWallCollision(collision, collisionLayer))
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

    private void ReturnProjectile(Collision2D collision)
    {
        if (hasReturned)
        {
            return;
        }

        hasReturned = true;

        correctSideWallBounceOnNextFixedUpdate = false;

        float ballHalfHeight = ballCollider.bounds.extents.y;

        float safeY = collision.collider.bounds.max.y + ballHalfHeight + bottomWallGap;

        Vector2 returnPosition = ballRigidbody.position;

        returnPosition.y = safeY;

        ballRigidbody.linearVelocity = Vector2.zero;

        ballRigidbody.angularVelocity = 0f;

        ballRigidbody.position = returnPosition;

        transform.position = returnPosition;

        ballRigidbody.simulated = false;
        ballCollider.enabled = false;

        if (shooter != null)
        {
            shooter.ProjectileReturned(this, returnPosition);
        }
    }

    public void RemoveProjectile()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}