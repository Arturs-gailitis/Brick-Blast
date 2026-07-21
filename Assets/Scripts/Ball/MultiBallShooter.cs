using System;
using System.Collections.Generic;
using UnityEngine;

public class MultiBallShooter : MonoBehaviour
{
    [Header("Projectile prefab")]
    [SerializeField] private GameObject shotBallPrefab;

    [Header("Multi-ball settings")]
    [SerializeField] [Min(1)] private int ballsPerShot = 10;
    [SerializeField] [Min(0.01f)] private float distanceBetweenBalls = 0.8f;

    public bool ShotIsActive => shotIsActive;

    public int BallsPerShot
    {
        get
        {
            if (shotIsActive)
            {
                return Mathf.Max(1, currentShotBallCount);
            }

            int safeBaseBallCount = Mathf.Max(1, ballsPerShot);
            int safeMultiplier = Mathf.Max(1, nextShotBallMultiplier);

            return safeBaseBallCount * safeMultiplier;
        }
    }

    public event Action ShotFinished;

    private PlayerBallTrajectory ownerBall;
    private Rigidbody2D ownerRigidbody;
    private SpriteRenderer ownerSpriteRenderer;
    private Collider2D ownerCollider;

    private readonly List<MultiBallProjectile> activeBalls = new List<MultiBallProjectile>();

    private bool shotIsActive;
    private bool allBallsWereSpawned;
    private bool hasFirstReturnPosition;

    private Vector2 launchPosition;
    private Vector2 firstReturnPosition;
    
    private Vector2 currentDirection;
    private float currentBallSpeed;
    private LayerMask currentBottomWallLayer;
    private float currentBottomWallGap;

    private int spawnedBallCount;
    private int fixedStepCounter;
    private int fixedStepsBetweenBalls;
    private int nextShotBallMultiplier = 1;
    private int currentShotBallCount = 1;

    private void Awake()
    {
        ownerBall = GetComponent<PlayerBallTrajectory>();
        ownerRigidbody = GetComponent<Rigidbody2D>();
        ownerSpriteRenderer = GetComponent<SpriteRenderer>();
        ownerCollider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (!shotIsActive || allBallsWereSpawned)
        {
            return;
        }

        fixedStepCounter++;

        if (fixedStepCounter < fixedStepsBetweenBalls)
        {
            return;
        }

        fixedStepCounter = 0;

        SpawnNextBall();
    }

    public bool StartShot(Vector2 direction, float ballSpeed, LayerMask bottomWallLayer, float bottomWallGap)
    {
        if (shotIsActive || shotBallPrefab == null)
        {
            return false;
        }

        RemoveAllProjectiles();

        shotIsActive = true;
        allBallsWereSpawned = false;
        hasFirstReturnPosition = false;

        spawnedBallCount = 0;
        fixedStepCounter = 0;

        currentShotBallCount = Mathf.Max(1, ballsPerShot) * Mathf.Max(1, nextShotBallMultiplier);

        nextShotBallMultiplier = 1;

        currentDirection = direction.normalized;
        currentBallSpeed = Mathf.Max(0.01f, ballSpeed);
        currentBottomWallLayer = bottomWallLayer;
        currentBottomWallGap = bottomWallGap;

        launchPosition = ownerRigidbody != null ? ownerRigidbody.position : (Vector2)transform.position;

        float distancePerFixedStep = currentBallSpeed * Time.fixedDeltaTime;

        fixedStepsBetweenBalls = Mathf.Max(1,Mathf.RoundToInt(distanceBetweenBalls / distancePerFixedStep));

        if (ownerRigidbody != null)
        {
            ownerRigidbody.linearVelocity = Vector2.zero;
            ownerRigidbody.angularVelocity = 0f;
        }

        SetOwnerBallVisible(false);

        SpawnNextBall();

        return true;
    }

    private void SpawnNextBall()
    {
        if (!shotIsActive || allBallsWereSpawned)
        {
            return;
        }

        int safeBallCount = Mathf.Max(1, currentShotBallCount);

        SpawnBall(launchPosition, currentDirection, currentBallSpeed, currentBottomWallLayer, currentBottomWallGap);

        spawnedBallCount++;

        if (spawnedBallCount >= safeBallCount)
        {
            allBallsWereSpawned = true;

            TryFinishShot();
        }
    }

    public List<MultiBallProjectile> SpawnAbilityBalls(Vector2 spawnPosition, int ballCount)
    {
        List<MultiBallProjectile> spawnedBalls = new List<MultiBallProjectile>();

        if (!shotIsActive || shotBallPrefab == null)
        {
            return spawnedBalls;
        }

        int safeBallCount = Mathf.Max(1, ballCount);

        for (int i = 0; i < safeBallCount; i++)
        {
            Vector2 direction = GetUpwardFanDirection(i, safeBallCount);

            MultiBallProjectile spawnedBall = SpawnBall(spawnPosition, direction, currentBallSpeed, currentBottomWallLayer,
                currentBottomWallGap);

            if (spawnedBall != null)
            {
                spawnedBalls.Add(spawnedBall);
            }
        }

        return spawnedBalls;
    }

    private Vector2 GetUpwardFanDirection(int ballIndex, int ballCount)
    {
        if (ballCount <= 1)
        {
            return Vector2.up;
        }

        float directionPercent = ballIndex / (float)(ballCount - 1);

        float angleInDegrees = Mathf.Lerp(135f, 45f, directionPercent);

        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians)).normalized;
    }

    private MultiBallProjectile SpawnBall(Vector2 spawnPosition, Vector2 direction, float ballSpeed, LayerMask bottomWallLayer,
        float bottomWallGap)
    {
        GameObject spawnedObject = Instantiate(shotBallPrefab, spawnPosition, transform.rotation);

        MultiBallProjectile projectile = spawnedObject.GetComponent<MultiBallProjectile>();

        if (projectile == null)
        {
            Destroy(spawnedObject);
            return null;
        }

        Collider2D projectileCollider = spawnedObject.GetComponent<Collider2D>();

        if (projectileCollider != null)
        {
            if (ownerCollider != null)
            {
                Physics2D.IgnoreCollision(projectileCollider, ownerCollider, true);
            }

            foreach (MultiBallProjectile activeBall in activeBalls)
            {
                if (activeBall == null)
                {
                    continue;
                }

                Collider2D activeCollider = activeBall.GetComponent<Collider2D>();

                if (activeCollider != null)
                {
                    Physics2D.IgnoreCollision(projectileCollider, activeCollider, true);
                }
            }
        }

        activeBalls.Add(projectile);

        projectile.Initialize(this, ownerBall, direction, ballSpeed, bottomWallLayer, bottomWallGap);

        return projectile;
    }

    public void ProjectileReturned(MultiBallProjectile projectile, Vector2 returnPosition)
    {
        if (!shotIsActive || projectile == null)
        {
            return;
        }

        if (!hasFirstReturnPosition)
        {
            hasFirstReturnPosition = true;
            firstReturnPosition = returnPosition;
        }

        TryFinishShot();
    }

    private void TryFinishShot()
    {
        if (!shotIsActive || !allBallsWereSpawned)
        {
            return;
        }

        activeBalls.RemoveAll(ball => ball == null);

        foreach (MultiBallProjectile activeBall in activeBalls)
        {
            if (!activeBall.HasReturned)
            {
                return;
            }
        }

        shotIsActive = false;

        Vector2 finalPosition = hasFirstReturnPosition ? firstReturnPosition : launchPosition;

        ShotFinished?.Invoke();

        if (ownerBall != null)
        {
            ownerBall.FinishMultiBallShot(finalPosition);
        }
        else
        {
            CancelShot();
        }
    }

    public void MultiplyNextShotBallCount(int multiplier)
    {
        int safeMultiplier = Mathf.Max(1, multiplier);

        nextShotBallMultiplier = Mathf.Max(nextShotBallMultiplier, safeMultiplier);
    }

    public void ResetTemporaryBallCountBonus()
    {
        nextShotBallMultiplier = 1;

        if (!shotIsActive)
        {
            currentShotBallCount = Mathf.Max(1, ballsPerShot);
        }
    }

    public void CancelShot()
    {
        bool wasShotActive = shotIsActive;

        RemoveAllProjectiles();

        shotIsActive = false;
        allBallsWereSpawned = false;
        hasFirstReturnPosition = false;

        spawnedBallCount = 0;
        fixedStepCounter = 0;

        if (wasShotActive)
        {
            ShotFinished?.Invoke();
        }

        SetOwnerBallVisible(true);
    }

    private void RemoveAllProjectiles()
    {
        foreach (MultiBallProjectile activeBall in activeBalls)
        {
            if (activeBall != null)
            {
                activeBall.RemoveProjectile();
            }
        }

        activeBalls.Clear();
    }

    private void SetOwnerBallVisible(bool isVisible)
    {
        if (ownerSpriteRenderer != null)
        {
            ownerSpriteRenderer.enabled = isVisible;
        }

        if (ownerCollider != null)
        {
            ownerCollider.enabled = isVisible;
        }
    }

    private void OnDisable()
    {
        CancelShot();
    }
}