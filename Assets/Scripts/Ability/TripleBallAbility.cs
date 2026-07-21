using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TripleBallAbility : MonoBehaviour
{
    [Header("Preview text")]
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private float previewTextWorldScale = 0.32f;
    [SerializeField] private float previewFontSize = 9f;

    public int Value { get; private set; } = 3;
    public int Aim { get; private set; }

    private readonly HashSet<int> affectedBallIds = new HashSet<int>();

    private bool isWaitingForMultiBallShotToEnd;
    private MultiBallShooter activeMultiBallShooter;

    private void Awake()
    {
        FindPreviewTextIfNeeded();
        UpdatePreview();
    }

    public void Configure(AbilityConfig abilityConfig)
    {
        if (abilityConfig == null)
        {
            return;
        }

        Value = Mathf.Max(1, abilityConfig.value);
        Aim = abilityConfig.aim;

        affectedBallIds.Clear();

        isWaitingForMultiBallShotToEnd = false;

        UnregisterMultiBallShooter();

        FindPreviewTextIfNeeded();
        UpdatePreview();
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
        PlayerBallTrajectory ownerBall = projectile.OwnerBall;

        TryCloneBalls(projectile.GetInstanceID(), ownerBall);
    }

    private void HandleMainBall(PlayerBallTrajectory mainBall)
    {
        TryCloneBalls(mainBall.GetInstanceID(), mainBall);
    }

    private void TryCloneBalls(int ballId, PlayerBallTrajectory ownerBall)
    {
        if (affectedBallIds.Contains(ballId))
        {
            return;
        }

        if (ownerBall == null)
        {
            return;
        }

        MultiBallShooter shooter = ownerBall.GetComponent<MultiBallShooter>();

        if (shooter == null || !shooter.ShotIsActive)
        {
            return;
        }

        affectedBallIds.Add(ballId);

        RegisterMultiBallShooter(shooter);

        List<MultiBallProjectile> clonedBalls = shooter.SpawnAbilityBalls( transform.position, Value);

        foreach (MultiBallProjectile clonedBall in clonedBalls)
        {
            if (clonedBall != null)
            {
                affectedBallIds.Add(clonedBall.GetInstanceID());
            }
        }
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

    private void FindPreviewTextIfNeeded()
    {
        if (previewText != null)
        {
            return;
        }

        previewText = GetComponentInChildren<TMP_Text>(true);
    }

    private void UpdatePreview()
    {
        if (previewText == null)
        {
            return;
        }

        previewText.gameObject.SetActive(true);

        previewText.text = "x" + Value;
        previewText.fontSize = previewFontSize;
        previewText.alignment = TextAlignmentOptions.Center;

        previewText.margin = Vector4.zero;
        previewText.overflowMode = TextOverflowModes.Overflow;
        previewText.color = Color.white;

        RectTransform rectTransform = previewText.rectTransform;

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);

            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            rectTransform.anchoredPosition3D = new Vector3(0f, 0f, -0.05f);

            rectTransform.sizeDelta = new Vector2(20f, 20f);

            rectTransform.localRotation = Quaternion.identity;
        }

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

        previewText.transform.localScale = new Vector3(previewTextWorldScale / parentScaleX,
            previewTextWorldScale / parentScaleY, 1f);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        Renderer textRenderer = previewText.GetComponent<Renderer>();

        if (spriteRenderer != null && textRenderer != null)
        {
            textRenderer.sortingLayerID = spriteRenderer.sortingLayerID;

            textRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        previewText.ForceMeshUpdate();
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

        if (previewText != null)
        {
            previewText.gameObject.SetActive(false);
        }

        Destroy(gameObject);
    }

    public SavedAbilityData CreateSaveData()
    {

        if (affectedBallIds.Count > 0 || isWaitingForMultiBallShotToEnd)
        {
            return null;
        }

        return new SavedAbilityData
        {
            abilityType = "triple", x = transform.position.x, y = transform.position.y, value = Value, aim = Aim
        };
    }

    private void OnDestroy()
    {
        UnregisterMultiBallShooter();
    }
}