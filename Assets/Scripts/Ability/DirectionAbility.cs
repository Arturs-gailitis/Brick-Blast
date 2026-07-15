using System.Collections.Generic;
using UnityEngine;

public class DirectionAbility : MonoBehaviour
{
    [Header("Ball settings")]
    [SerializeField] private float defaultBallSpeed = 8f;

    private int aim = 2;

    private readonly HashSet<int> affectedBallIds = new HashSet<int>();

    private bool hasBeenUsedByMainBall;
    private bool isWaitingForMultiBallShotToEnd;

    private MultiBallShooter activeMultiBallShooter;

    public void Configure(AbilityConfig config)
    {
        if (config == null)
        {
            return;
        }

        if (config.aim >= 1 && config.aim <= 3)
        {
            aim = config.aim;
        }
        else
        {
            aim = Random.Range(1, 4);
        }

        affectedBallIds.Clear();

        hasBeenUsedByMainBall = false;
        isWaitingForMultiBallShotToEnd = false;

        UnregisterMultiBallShooter();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MultiBallProjectile projectile = other.GetComponentInParent<MultiBallProjectile>();

        if (projectile != null)
        {
            HandleMultiBallProjectile(projectile);
            return;
        }

        PlayerBallTrajectory mainBall = other.GetComponentInParent<PlayerBallTrajectory>();

        if (mainBall != null)
        {
            HandleMainBall(mainBall);
        }
    }

    private void HandleMultiBallProjectile(MultiBallProjectile projectile)
    {
        int ballId = projectile.GetInstanceID();

        if (!affectedBallIds.Add(ballId))
        {
            return;
        }

        Rigidbody2D ballRigidbody = projectile.GetComponent<Rigidbody2D>();

        ChangeBallDirection(ballRigidbody);

        PlayerBallTrajectory ownerBall = projectile.OwnerBall;

        if (ownerBall == null)
        {
            DestroyAbility();
            return;
        }

        MultiBallShooter shooter = ownerBall.GetComponent<MultiBallShooter>();

        if (shooter == null || !shooter.ShotIsActive)
        {
            DestroyAbility();
            return;
        }

        RegisterMultiBallShooter(shooter);
    }

    private void HandleMainBall(PlayerBallTrajectory mainBall)
    {
        if (hasBeenUsedByMainBall)
        {
            return;
        }

        MultiBallShooter shooter = mainBall.GetComponent<MultiBallShooter>();

        if (shooter != null && shooter.ShotIsActive)
        {
            return;
        }

        int ballId = mainBall.GetInstanceID();

        if (!affectedBallIds.Add(ballId))
        {
            return;
        }

        hasBeenUsedByMainBall = true;

        Rigidbody2D ballRigidbody = mainBall.GetComponent<Rigidbody2D>();

        ChangeBallDirection(ballRigidbody);

        DestroyAbility();
    }

    private void ChangeBallDirection(Rigidbody2D ballRigidbody)
    {
        if (ballRigidbody == null)
        {
            return;
        }

        Vector2 newDirection = GetDirectionFromAim(aim);

        float currentSpeed = ballRigidbody.linearVelocity.magnitude;

        if (currentSpeed <= 0f)
        {
            currentSpeed = defaultBallSpeed;
        }

        ballRigidbody.linearVelocity = newDirection * currentSpeed;
    }

    private void RegisterMultiBallShooter(MultiBallShooter shooter)
    {
        if (activeMultiBallShooter == shooter)
        {
            return;
        }

        UnregisterMultiBallShooter();

        activeMultiBallShooter = shooter;
        isWaitingForMultiBallShotToEnd = true;

        activeMultiBallShooter.ShotFinished += HandleMultiBallShotFinished;
    }

    private void HandleMultiBallShotFinished()
    {
        isWaitingForMultiBallShotToEnd = false;

        UnregisterMultiBallShooter();
        DestroyAbility();
    }

    private void Update()
    {
        if (!isWaitingForMultiBallShotToEnd)
        {
            return;
        }

        if (activeMultiBallShooter == null || !activeMultiBallShooter.ShotIsActive)
        {
            isWaitingForMultiBallShotToEnd = false;

            UnregisterMultiBallShooter();
            DestroyAbility();
        }
    }

    private void UnregisterMultiBallShooter()
    {
        if (activeMultiBallShooter == null)
        {
            return;
        }

        activeMultiBallShooter.ShotFinished -= HandleMultiBallShotFinished;

        activeMultiBallShooter = null;
    }

    private void DestroyAbility()
    {
        UnregisterMultiBallShooter();

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        Collider2D abilityCollider = GetComponent<Collider2D>();

        if (abilityCollider != null)
        {
            abilityCollider.enabled = false;
        }

        Destroy(gameObject);
    }

    private Vector2 GetDirectionFromAim(int selectedAim)
    {
        switch (selectedAim)
        {
            case 1:
                return new Vector2(-1f, 1f).normalized;

            case 2:
                return Vector2.up;

            case 3:
                return new Vector2(1f, 1f).normalized;

            default:
                return Vector2.up;
        }
    }

    public SavedAbilityData CreateSaveData()
    {
        if (hasBeenUsedByMainBall || isWaitingForMultiBallShotToEnd)
        {
            return null;
        }

        return new SavedAbilityData
        {
            abilityType = "direction", x = transform.position.x, y = transform.position.y, value = 1, aim = aim
        };
    }

    private void OnDestroy()
    {
        UnregisterMultiBallShooter();
    }
}