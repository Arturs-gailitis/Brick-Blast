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
    public int BallsPerShot => Mathf.Max(1, ballsPerShot);

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

        int safeBallCount = Mathf.Max(1, ballsPerShot);

        SpawnBall(currentDirection, currentBallSpeed, currentBottomWallLayer, currentBottomWallGap);

        spawnedBallCount++;

        if (spawnedBallCount >= safeBallCount)
        {
            allBallsWereSpawned = true;

            TryFinishShot();
        }
    }

    private void SpawnBall(Vector2 direction, float ballSpeed, LayerMask bottomWallLayer, float bottomWallGap)
    {
        GameObject spawnedObject = Instantiate(shotBallPrefab, launchPosition, transform.rotation);

        MultiBallProjectile projectile = spawnedObject.GetComponent<MultiBallProjectile>();

        if (projectile == null)
        {
            Destroy(spawnedObject);
            return;
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