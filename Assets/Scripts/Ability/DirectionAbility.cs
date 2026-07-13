using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DirectionAbility : MonoBehaviour
{
    [Header("Direction text")]
    [SerializeField] private TMP_Text directionText;

    [Header("Ball settings")]
    [SerializeField] private float defaultBallSpeed = 8f;

    private int aim = 1;

    private readonly HashSet<int> affectedBallIds =
        new HashSet<int>();

    private bool hasBeenUsedByMainBall;
    private bool isWaitingForMultiBallShotToEnd;

    private MultiBallShooter activeMultiBallShooter;

    private void Awake()
    {
        if (directionText == null)
        {
            directionText =
                GetComponentInChildren<TMP_Text>(true);
        }

        UpdateDirectionText();
    }

    public void Configure(AbilityConfig config)
    {
        if (config == null)
        {
            return;
        }

        aim = Mathf.Clamp(config.aim, 1, 8);

        affectedBallIds.Clear();

        hasBeenUsedByMainBall = false;
        isWaitingForMultiBallShotToEnd = false;

        UnregisterMultiBallShooter();

        UpdateDirectionText();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MultiBallProjectile projectile =
            other.GetComponentInParent<MultiBallProjectile>();

        if (projectile != null)
        {
            HandleMultiBallProjectile(projectile);
            return;
        }

        PlayerBallTrajectory mainBall =
            other.GetComponentInParent<PlayerBallTrajectory>();

        if (mainBall != null)
        {
            HandleMainBall(mainBall);
        }
    }

    private void HandleMultiBallProjectile(
        MultiBallProjectile projectile)
    {
        int ballId = projectile.GetInstanceID();

        if (!affectedBallIds.Add(ballId))
        {
            return;
        }

        Rigidbody2D ballRigidbody =
            projectile.GetComponent<Rigidbody2D>();

        ChangeBallDirection(ballRigidbody);

        PlayerBallTrajectory ownerBall =
            projectile.OwnerBall;

        if (ownerBall == null)
        {
            DestroyAbility();
            return;
        }

        MultiBallShooter shooter =
            ownerBall.GetComponent<MultiBallShooter>();

        if (shooter == null || !shooter.ShotIsActive)
        {
            DestroyAbility();
            return;
        }

        RegisterMultiBallShooter(shooter);
    }

    private void HandleMainBall(
        PlayerBallTrajectory mainBall)
    {
        if (hasBeenUsedByMainBall)
        {
            return;
        }

        MultiBallShooter shooter =
            mainBall.GetComponent<MultiBallShooter>();

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

        Rigidbody2D ballRigidbody =
            mainBall.GetComponent<Rigidbody2D>();

        ChangeBallDirection(ballRigidbody);

        DestroyAbility();
    }

    private void ChangeBallDirection(
        Rigidbody2D ballRigidbody)
    {
        if (ballRigidbody == null)
        {
            return;
        }

        Vector2 newDirection =
            GetDirectionFromAim(aim);

        float currentSpeed =
            ballRigidbody.linearVelocity.magnitude;

        if (currentSpeed <= 0f)
        {
            currentSpeed = defaultBallSpeed;
        }

        ballRigidbody.linearVelocity =
            newDirection * currentSpeed;
    }

    private void RegisterMultiBallShooter(
        MultiBallShooter shooter)
    {
        if (activeMultiBallShooter == shooter)
        {
            return;
        }

        UnregisterMultiBallShooter();

        activeMultiBallShooter = shooter;
        isWaitingForMultiBallShotToEnd = true;

        activeMultiBallShooter.ShotFinished +=
            HandleMultiBallShotFinished;
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

        if (activeMultiBallShooter == null ||
            !activeMultiBallShooter.ShotIsActive)
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

        activeMultiBallShooter.ShotFinished -=
            HandleMultiBallShotFinished;

        activeMultiBallShooter = null;
    }

    private void DestroyAbility()
    {
        UnregisterMultiBallShooter();

        SpriteRenderer spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        Collider2D abilityCollider =
            GetComponent<Collider2D>();

        if (abilityCollider != null)
        {
            abilityCollider.enabled = false;
        }

        if (directionText != null)
        {
            directionText.gameObject.SetActive(false);
        }

        Destroy(gameObject);
    }

    private void UpdateDirectionText()
    {
        if (directionText == null)
        {
            return;
        }

        directionText.text =
            GetDirectionTextFromAim(aim);
    }

    private string GetDirectionTextFromAim(
        int selectedAim)
    {
        switch (selectedAim)
        {
            case 1:
                return "N";

            case 2:
                return "NE";

            case 3:
                return "E";

            case 4:
                return "SE";

            case 5:
                return "S";

            case 6:
                return "SW";

            case 7:
                return "W";

            case 8:
                return "NW";

            default:
                return "N";
        }
    }

    private Vector2 GetDirectionFromAim(
        int selectedAim)
    {
        switch (selectedAim)
        {
            case 1:
                return Vector2.up;

            case 2:
                return new Vector2(1f, 1f).normalized;

            case 3:
                return Vector2.right;

            case 4:
                return new Vector2(1f, -1f).normalized;

            case 5:
                return Vector2.down;

            case 6:
                return new Vector2(-1f, -1f).normalized;

            case 7:
                return Vector2.left;

            case 8:
                return new Vector2(-1f, 1f).normalized;

            default:
                return Vector2.up;
        }
    }

    public SavedAbilityData CreateSaveData()
    {
        if (hasBeenUsedByMainBall ||
            isWaitingForMultiBallShotToEnd)
        {
            return null;
        }

        return new SavedAbilityData
        {
            abilityType = "direction",
            x = transform.position.x,
            y = transform.position.y,
            value = 1,
            durationSeconds = 0f,
            aim = aim
        };
    }

    private void OnDestroy()
    {
        UnregisterMultiBallShooter();
    }
}