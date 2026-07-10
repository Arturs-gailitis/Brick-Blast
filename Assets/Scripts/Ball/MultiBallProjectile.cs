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

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<Collider2D>();

        ballRigidbody.gravityScale = 0f;
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;
    }

    public void Initialize(
        MultiBallShooter newShooter,
        PlayerBallTrajectory newOwnerBall,
        Vector2 direction,
        float ballSpeed,
        LayerMask newBottomWallLayer,
        float newBottomWallGap)
    {
        shooter = newShooter;
        ownerBall = newOwnerBall;

        bottomWallLayer = newBottomWallLayer;
        bottomWallGap = newBottomWallGap;

        hasReturned = false;

        ballCollider.enabled = true;

        ballRigidbody.simulated = true;
        ballRigidbody.gravityScale = 0f;
        ballRigidbody.angularVelocity = 0f;
        ballRigidbody.WakeUp();

        ballRigidbody.linearVelocity =
            direction.normalized * ballSpeed;
    }

    public void RegisterBrickHit()
    {
        if (ownerBall != null)
        {
            ownerBall.RegisterBrickHit();
        }
    }

    public void IncreaseAttackStrength(int amount)
    {
        if (ownerBall != null)
        {
            ownerBall.IncreaseAttackStrength(amount);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int collisionLayer =
            collision.collider.gameObject.layer;

        bool hitBottomWall =
            (bottomWallLayer.value &
             (1 << collisionLayer)) != 0;

        if (!hitBottomWall || hasReturned)
        {
            return;
        }

        hasReturned = true;

        float ballHalfHeight =
            ballCollider.bounds.extents.y;

        float safeY =
            collision.collider.bounds.max.y +
            ballHalfHeight +
            bottomWallGap;

        Vector2 returnPosition =
            ballRigidbody.position;

        returnPosition.y = safeY;

        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;

        ballRigidbody.position = returnPosition;
        transform.position = returnPosition;

        ballRigidbody.simulated = false;
        ballCollider.enabled = false;

        if (shooter != null)
        {
            shooter.ProjectileReturned(
                this,
                returnPosition
            );
        }
    }

    public void RemoveProjectile()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}