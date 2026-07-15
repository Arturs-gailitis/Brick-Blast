using System.Collections.Generic;
using UnityEngine;

public class BallReturnSpeedBooster : MonoBehaviour
{
    [Header("Speed boost")]
    [SerializeField] [Min(1f)] private float speedMultiplier;
    [SerializeField] [Min(0f)] private float boostDelay = 1.5f;

    [Header("Downward movement check")]
    [SerializeField] [Min(0f)] private float minimumDownwardVelocity = 0.05f;

    [Header("Brick check")]
    [SerializeField] [Min(0f)] private float brickCheckPadding = 0.02f;

    private PlayerBallTrajectory ownerBall;

    private readonly List<MultiBallProjectile> flyingBalls = new List<MultiBallProjectile>();

    private readonly Dictionary<MultiBallProjectile, float> originalBallSpeeds =
        new Dictionary<MultiBallProjectile, float>();

    private bool speedBoostIsActive;
    private float boostDelayTimer;

    private void Awake()
    {
        ownerBall = GetComponent<PlayerBallTrajectory>();
    }

    private void FixedUpdate()
    {
        if (ownerBall == null || !ownerBall.TurnIsActive)
        {
            ResetSpeedBoost();
            return;
        }

        FindFlyingBalls();

        if (flyingBalls.Count == 0)
        {
            ResetSpeedBoost();
            return;
        }

        RememberOriginalSpeeds();

        float highestBallY = float.NegativeInfinity;

        foreach (MultiBallProjectile ball in flyingBalls)
        {
            Rigidbody2D ballRigidbody = ball.GetComponent<Rigidbody2D>();

            if (ballRigidbody == null)
            {
                CancelBoostCondition();
                return;
            }

            if (ballRigidbody.linearVelocity.y >= -minimumDownwardVelocity)
            {
                CancelBoostCondition();
                return;
            }

            highestBallY = Mathf.Max(highestBallY, ballRigidbody.position.y);
        }

        if (HasBrickBelow(highestBallY))
        {
            CancelBoostCondition();
            return;
        }

        boostDelayTimer += Time.fixedDeltaTime;

        if (boostDelayTimer < boostDelay)
        {
            return;
        }

        speedBoostIsActive = true;

        ApplySpeedBoost();
    }

    private void FindFlyingBalls()
    {
        flyingBalls.Clear();

        MultiBallProjectile[] projectiles = FindObjectsByType<MultiBallProjectile>(FindObjectsSortMode.None);

        foreach (MultiBallProjectile projectile in projectiles)
        {
            if (projectile == null || projectile.OwnerBall != ownerBall || projectile.HasReturned)
            {
                continue;
            }

            Rigidbody2D projectileRigidbody = projectile.GetComponent<Rigidbody2D>();

            if (projectileRigidbody == null || !projectileRigidbody.simulated)
            {
                continue;
            }

            flyingBalls.Add(projectile);
        }
    }

    private void RememberOriginalSpeeds()
    {
        foreach (MultiBallProjectile ball in flyingBalls)
        {
            if (originalBallSpeeds.ContainsKey(ball))
            {
                continue;
            }

            Rigidbody2D ballRigidbody = ball.GetComponent<Rigidbody2D>();

            if (ballRigidbody == null)
            {
                continue;
            }

            float currentSpeed = ballRigidbody.linearVelocity.magnitude;

            if (currentSpeed > 0.0001f)
            {
                originalBallSpeeds.Add(ball, currentSpeed);
            }
        }
    }

    private bool HasBrickBelow(float highestBallY)
    {
        BrickCollision[] bricks = FindObjectsByType<BrickCollision>(FindObjectsSortMode.None);

        foreach (BrickCollision brick in bricks)
        {
            if (brick == null || !brick.gameObject.activeInHierarchy)
            {
                continue;
            }

            Collider2D brickCollider = brick.GetComponent<Collider2D>();

            if (brickCollider == null || !brickCollider.enabled)
            {
                continue;
            }

            if (brickCollider.bounds.min.y < highestBallY - brickCheckPadding)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplySpeedBoost()
    {
        float safeMultiplier = Mathf.Max(1f, speedMultiplier);

        foreach (MultiBallProjectile ball in flyingBalls)
        {
            if (ball == null || ball.HasReturned)
            {
                continue;
            }

            Rigidbody2D ballRigidbody = ball.GetComponent<Rigidbody2D>();

            if (ballRigidbody == null || !ballRigidbody.simulated)
            {
                continue;
            }

            Vector2 velocity = ballRigidbody.linearVelocity;

            if (velocity.sqrMagnitude < 0.000001f)
            {
                continue;
            }

            if (!originalBallSpeeds.TryGetValue(ball, out float originalSpeed))
            {
                originalSpeed = velocity.magnitude;

                originalBallSpeeds[ball] = originalSpeed;
            }

            ballRigidbody.linearVelocity = velocity.normalized * originalSpeed * safeMultiplier;
        }
    }

    private void CancelBoostCondition()
    {
        boostDelayTimer = 0f;

        if (!speedBoostIsActive)
        {
            return;
        }

        RestoreFlyingBallsToOriginalSpeed();

        speedBoostIsActive = false;
    }

    private void RestoreFlyingBallsToOriginalSpeed()
    {
        foreach (KeyValuePair<MultiBallProjectile, float> savedBall in originalBallSpeeds)
        {
            MultiBallProjectile ball = savedBall.Key;

            if (ball == null || ball.HasReturned)
            {
                continue;
            }

            Rigidbody2D ballRigidbody = ball.GetComponent<Rigidbody2D>();

            if (ballRigidbody == null || !ballRigidbody.simulated)
            {
                continue;
            }

            Vector2 velocity = ballRigidbody.linearVelocity;

            if (velocity.sqrMagnitude > 0.000001f)
            {
                ballRigidbody.linearVelocity = velocity.normalized * savedBall.Value;
            }
        }
    }

    private void ResetSpeedBoost()
    {
        RestoreFlyingBallsToOriginalSpeed();

        speedBoostIsActive = false;
        boostDelayTimer = 0f;

        flyingBalls.Clear();
        originalBallSpeeds.Clear();
    }

    private void OnDisable()
    {
        ResetSpeedBoost();
    }
}