using TMPro;
using UnityEngine;

public class LaserAbility : MonoBehaviour
{
    [Header("Aim preview text")]
    [SerializeField] private TMP_Text aimPreviewText;
    [SerializeField] private float previewTextWorldScale;
    [SerializeField] private float previewFontSize;
    [SerializeField] private float previewTextXOffset;
    [SerializeField] private float previewTextYOffset;

    [Header("Laser damage")]
    [SerializeField] private float rowColumnTolerance = 0.35f;

    [Header("Laser beam visual")]
    [SerializeField] private float beamDistance = 20f;
    [SerializeField] private float beamWidth = 0.08f;
    [SerializeField] private Color beamColor = Color.cyan;

    [Header("Laser placeholder animation")]
    [SerializeField] private float beamFadeInSeconds = 0.035f;
    [SerializeField] private float beamHoldSeconds = 0.04f;
    [SerializeField] private float beamFadeOutSeconds = 0.14f;
    [SerializeField] private float beamStartWidthMultiplier = 0.2f;
    [SerializeField] private float beamFlashWidthMultiplier = 1.8f;

    [Header("Vertical laser wall limits")]
    [SerializeField] private string topWallName = "TopWall";
    [SerializeField] private string bottomWallName = "ButtomWall";

    public int Value { get; private set; } = 1;
    public int Aim { get; private set; } = 1;

    private bool hasBeenUsedByMainBall;
    private bool isWaitingForMultiBallShotToEnd;

    private MultiBallShooter activeMultiBallShooter;
    private Collider2D topWallCollider;
    private Collider2D bottomWallCollider;

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

        MultiBallShooter shooter = ownerBall != null ? ownerBall.GetComponent<MultiBallShooter>() : null;

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

        hasBeenUsedByMainBall = true;

        FireLaser();
        DestroyAbility();
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

    private void FireLaser()
    {
        bool shootHorizontal = Aim == 1 || Aim == 3;

        bool shootVertical = Aim == 2 || Aim == 3;

        if (!shootHorizontal && !shootVertical)
        {
            shootHorizontal = true;
        }

        DamageBricks(shootHorizontal, shootVertical);

        ShowLaserBeam(shootHorizontal, shootVertical);
    }

    private void DamageBricks(bool shootHorizontal, bool shootVertical)
    {
        Vector2 laserPosition = transform.position;

        BrickCollision[] bricks = FindObjectsByType<BrickCollision>(FindObjectsSortMode.None);

        foreach (BrickCollision brick in bricks)
        {
            if (brick == null)
            {
                continue;
            }

            Vector2 brickPosition = brick.transform.position;

            bool isInSameRow = shootHorizontal && Mathf.Abs(brickPosition.y - laserPosition.y) <= rowColumnTolerance;

            bool isInSameColumn = shootVertical && Mathf.Abs(brickPosition.x - laserPosition.x) <= rowColumnTolerance;

            if (isInSameRow || isInSameColumn)
            {
                brick.TakeLaserDamage(Value);
            }
        }
    }

    private void ShowLaserBeam(bool shootHorizontal, bool shootVertical)
    {
        Vector2 laserPosition = transform.position;

        if (shootHorizontal)
        {
            CreateBeam(laserPosition + Vector2.left * beamDistance, laserPosition + Vector2.right * beamDistance);
        }

        if (shootVertical)
        {
            Vector2 startPosition = laserPosition + Vector2.down * beamDistance;

            Vector2 endPosition = laserPosition + Vector2.up * beamDistance;

            if (TryGetVerticalLaserLimits(out float bottomLimitY, out float topLimitY))
            {
                startPosition = new Vector2(laserPosition.x, bottomLimitY);

            endPosition = new Vector2(laserPosition.x, topLimitY);
            }

            CreateBeam(startPosition, endPosition);
        }
    }

    private bool TryGetVerticalLaserLimits(out float bottomLimitY, out float topLimitY)
    {
        bottomLimitY = 0f;
        topLimitY = 0f;
    
        FindWallCollidersIfNeeded();

        if (topWallCollider == null || bottomWallCollider == null)
        {
            return false;
        }

        bottomLimitY = bottomWallCollider.bounds.center.y;
        topLimitY = topWallCollider.bounds.center.y;

        return topLimitY > bottomLimitY;
    }

    private void FindWallCollidersIfNeeded()
    {
        if (topWallCollider == null)
        {
            GameObject topWall = GameObject.Find(topWallName);

            if (topWall != null)
            {
                topWallCollider = topWall.GetComponent<Collider2D>();
            }
        }

        if (bottomWallCollider == null)
        {
            GameObject bottomWall = GameObject.Find(bottomWallName);

            if (bottomWall != null)
            {
                bottomWallCollider = bottomWall.GetComponent<Collider2D>();
            }
        }
    }

    private void CreateBeam(Vector2 startPosition, Vector2 endPosition)
    {
        GameObject beamObject = new GameObject("LaserBeam");

        LineRenderer lineRenderer = beamObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;

        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);

        lineRenderer.startWidth = beamWidth * beamStartWidthMultiplier;
        lineRenderer.endWidth = beamWidth * beamStartWidthMultiplier;

        lineRenderer.numCapVertices = 8;
        lineRenderer.numCornerVertices = 8;

        SetBeamBehindWalls(lineRenderer);

        Color invisibleBeamColor = beamColor;
        invisibleBeamColor.a = 0f;

        lineRenderer.startColor = invisibleBeamColor;
        lineRenderer.endColor = invisibleBeamColor;

        Material runtimeMaterial = null;

        Shader shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            runtimeMaterial = new Material(shader);
            lineRenderer.material = runtimeMaterial;
        }

        LaserBeamAnimation beamAnimation = beamObject.AddComponent<LaserBeamAnimation>();

        beamAnimation.Play(lineRenderer, runtimeMaterial, beamColor, beamWidth, beamFadeInSeconds, beamHoldSeconds,
            beamFadeOutSeconds, beamStartWidthMultiplier, beamFlashWidthMultiplier);
    }

    private void SetBeamBehindWalls(LineRenderer lineRenderer)
    {
        FindWallCollidersIfNeeded();

        Renderer wallRenderer = null;

        if (topWallCollider != null)
        {
            wallRenderer = topWallCollider.GetComponent<Renderer>();
        }

        if (wallRenderer == null && bottomWallCollider != null)
        {
            wallRenderer = bottomWallCollider.GetComponent<Renderer>();
        }

        if (wallRenderer != null)
        {
            lineRenderer.sortingLayerID = wallRenderer.sortingLayerID;

            lineRenderer.sortingOrder = wallRenderer.sortingOrder - 1;

            return;
        }

        lineRenderer.sortingOrder = -1;
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

        Transform textChild = transform.Find("AimPreviewText");

        if (textChild != null)
        {
            aimPreviewText = textChild.GetComponent<TMP_Text>();
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

        aimPreviewText.fontSize = previewFontSize;
        aimPreviewText.alignment = TextAlignmentOptions.Center;
        aimPreviewText.margin = Vector4.zero;

        RectTransform rectTransform = aimPreviewText.rectTransform;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (rectTransform == null || spriteRenderer == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.localRotation = Quaternion.identity;
        rectTransform.sizeDelta = new Vector2(20f, 20f);

        float parentScaleX = Mathf.Abs(transform.lossyScale.x);
        float parentScaleY = Mathf.Abs(transform.lossyScale.y);

        if (parentScaleX <= 0f)
        {
            parentScaleX = 1f;
        }

        if (parentScaleY <= 0f)
        {
            parentScaleY = 1f;
        }

        rectTransform.localScale = new Vector3(previewTextWorldScale / parentScaleX,
            previewTextWorldScale / parentScaleY, 1f);

        Vector3 spriteCenterLocal = transform.InverseTransformPoint(spriteRenderer.bounds.center);

        rectTransform.localPosition = new Vector3(spriteCenterLocal.x, spriteCenterLocal.y, -0.05f);

        aimPreviewText.ForceMeshUpdate();

        Renderer textRenderer = aimPreviewText.GetComponent<Renderer>();

        if (textRenderer != null)
        {
            Vector3 centerDifference = spriteRenderer.bounds.center - textRenderer.bounds.center;

            centerDifference.x += previewTextXOffset;
            centerDifference.y += previewTextYOffset;
            centerDifference.z = 0f;

            aimPreviewText.transform.position += centerDifference;
        }
    }

    public SavedAbilityData CreateSaveData()
    {

        if (hasBeenUsedByMainBall || isWaitingForMultiBallShotToEnd)
        {
            return null;
        }

        return new SavedAbilityData{abilityType = "laser", x = transform.position.x, y = transform.position.y,
            value = Value, aim = Aim};
    }

    private void OnDestroy()
    {
        UnregisterMultiBallShooter();
    }
}