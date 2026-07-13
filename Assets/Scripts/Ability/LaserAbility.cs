using TMPro;
using UnityEngine;

public class LaserAbility : MonoBehaviour
{
    [Header("Aim preview text")]
    [SerializeField] private TMP_Text aimPreviewText;
    [SerializeField] private float previewTextWorldScale = 0.45f;
    [SerializeField] private float previewFontSize = 12f;

    [Header("Laser damage")]
    [SerializeField] private float rowColumnTolerance = 0.35f;

    [Header("Laser beam visual")]
    [SerializeField] private float defaultBeamVisibleSeconds = 0.12f;
    [SerializeField] private float beamDistance = 20f;
    [SerializeField] private float beamWidth = 0.08f;

    public int Value { get; private set; } = 1;
    public float DurationSeconds { get; private set; }
    public int Aim { get; private set; } = 1;

    private bool hasBeenUsedByMainBall;
    private bool isWaitingForMultiBallShotToEnd;

    private MultiBallShooter activeMultiBallShooter;

    private void Awake()
    {
        FindPreviewTextIfNeeded();
        UpdateAimPreview();
    }

    public void Configure(AbilityConfig abilityConfig)
    {
        if (abilityConfig == null)
        {
            return;
        }

        Value = Mathf.Max(1, abilityConfig.value);
        DurationSeconds = Mathf.Max(0f, abilityConfig.durationSeconds);

        Aim = abilityConfig.aim;

        UnregisterMultiBallShooter();

        hasBeenUsedByMainBall = false;
        isWaitingForMultiBallShotToEnd = false;

        FindPreviewTextIfNeeded();
        UpdateAimPreview();
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

        FireLaser();

        PlayerBallTrajectory ownerBall = projectile.OwnerBall;

        MultiBallShooter shooter = ownerBall != null
            ? ownerBall.GetComponent<MultiBallShooter>()
            : null;

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

        hasBeenUsedByMainBall = true;

        FireLaser();
        DestroyAbility();
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

        activeMultiBallShooter.ShotFinished -=
            HandleMultiBallShotFinished;

        activeMultiBallShooter = null;
    }

    private void FireLaser()
    {
        bool shootHorizontal =
            Aim == 1 || Aim == 3;

        bool shootVertical =
            Aim == 2 || Aim == 3;

        if (!shootHorizontal && !shootVertical)
        {
            shootHorizontal = true;
        }

        DamageBricks(
            shootHorizontal,
            shootVertical
        );

        ShowLaserBeam(
            shootHorizontal,
            shootVertical
        );
    }

    private void DamageBricks(
        bool shootHorizontal,
        bool shootVertical)
    {
        Vector2 laserPosition =
            transform.position;

        BrickCollision[] bricks =
            FindObjectsByType<BrickCollision>(
                FindObjectsSortMode.None
            );

        foreach (BrickCollision brick in bricks)
        {
            if (brick == null)
            {
                continue;
            }

            Vector2 brickPosition =
                brick.transform.position;

            bool isInSameRow =
                shootHorizontal &&
                Mathf.Abs(
                    brickPosition.y -
                    laserPosition.y
                ) <= rowColumnTolerance;

            bool isInSameColumn =
                shootVertical &&
                Mathf.Abs(
                    brickPosition.x -
                    laserPosition.x
                ) <= rowColumnTolerance;

            if (isInSameRow || isInSameColumn)
            {
                brick.TakeLaserDamage(Value);
            }
        }
    }

    private void ShowLaserBeam(
        bool shootHorizontal,
        bool shootVertical)
    {
        Vector2 laserPosition =
            transform.position;

        if (shootHorizontal)
        {
            CreateBeam(
                laserPosition +
                Vector2.left * beamDistance,

                laserPosition +
                Vector2.right * beamDistance
            );
        }

        if (shootVertical)
        {
            CreateBeam(
                laserPosition +
                Vector2.down * beamDistance,

                laserPosition +
                Vector2.up * beamDistance
            );
        }
    }

    private void CreateBeam(
        Vector2 startPosition,
        Vector2 endPosition)
    {
        GameObject beamObject =
            new GameObject("LaserBeam");

        LineRenderer lineRenderer =
            beamObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;

        lineRenderer.SetPosition(
            0,
            startPosition
        );

        lineRenderer.SetPosition(
            1,
            endPosition
        );

        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;

        lineRenderer.numCapVertices = 8;
        lineRenderer.numCornerVertices = 8;

        lineRenderer.sortingOrder = 60;

        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.cyan;

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader != null)
        {
            lineRenderer.material =
                new Material(shader);
        }

        float visibleSeconds =
            DurationSeconds > 0f
                ? DurationSeconds
                : defaultBeamVisibleSeconds;

        Destroy(
            beamObject,
            visibleSeconds
        );
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

        if (aimPreviewText != null)
        {
            aimPreviewText.gameObject.SetActive(false);
        }

        Destroy(gameObject);
    }

    private void FindPreviewTextIfNeeded()
    {
        if (aimPreviewText != null)
        {
            return;
        }

        Transform textChild =
            transform.Find("AimPreviewText");

        if (textChild != null)
        {
            aimPreviewText =
                textChild.GetComponent<TMP_Text>();
        }
    }

    private void UpdateAimPreview()
    {
        if (aimPreviewText == null)
        {
            return;
        }

        aimPreviewText.gameObject.SetActive(true);

        switch (Aim)
        {
            case 1:
                aimPreviewText.text = "—";
                break;

            case 2:
                aimPreviewText.text = "|";
                break;

            case 3:
                aimPreviewText.text = "+";
                break;

            default:
                aimPreviewText.text = "—";
                break;
        }

        ConfigureAimPreviewText();
    }

    private void ConfigureAimPreviewText()
    {
        if (aimPreviewText == null)
        {
            return;
        }

        aimPreviewText.fontSize =
            previewFontSize;

        aimPreviewText.alignment =
            TextAlignmentOptions.Center;

        RectTransform rectTransform =
            aimPreviewText.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition3D =
                new Vector3(
                    0f,
                    0f,
                    -0.05f
                );

            rectTransform.sizeDelta =
                new Vector2(
                    20f,
                    20f
                );
        }

        float parentScaleX =
            Mathf.Abs(transform.lossyScale.x);

        float parentScaleY =
            Mathf.Abs(transform.lossyScale.y);

        if (parentScaleX <= 0f)
        {
            parentScaleX = 1f;
        }

        if (parentScaleY <= 0f)
        {
            parentScaleY = 1f;
        }

        aimPreviewText.transform.localScale =
            new Vector3(
                previewTextWorldScale /
                parentScaleX,

                previewTextWorldScale /
                parentScaleY,

                1f
            );
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
            abilityType = "laser",
            x = transform.position.x,
            y = transform.position.y,
            value = Value,
            durationSeconds = DurationSeconds,
            aim = Aim
        };
    }

    private void OnDestroy()
    {
        UnregisterMultiBallShooter();
    }
}