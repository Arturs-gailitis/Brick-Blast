using TMPro;
using UnityEngine;

public class PowerAbility : MonoBehaviour
{
    [Header("Preview text")]
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private float previewTextWorldScale = 0.45f;
    [SerializeField] private float previewFontSize = 12f;

    public int Value { get; private set; } = 1;
    public int Aim { get; private set; }

    private bool hasBeenUsed;

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

        FindPreviewTextIfNeeded();
        UpdatePreview();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenUsed)
        {
            return;
        }

        MultiBallProjectile projectile = other.GetComponentInParent<MultiBallProjectile>();

        if (projectile != null)
        {
            hasBeenUsed = true;
            projectile.IncreaseAttackStrength(Value);

            HideAbilityObject();
            return;
        }

        PlayerBallTrajectory ballTrajectory = other.GetComponentInParent<PlayerBallTrajectory>();

        if (ballTrajectory == null)
        {
            return;
        }

        hasBeenUsed = true;
        ballTrajectory.IncreaseAttackStrength(Value);

        HideAbilityObject();
    }

    private void FindPreviewTextIfNeeded()
    {
        if (previewText != null)
        {
            return;
        }

        Transform textChild = transform.Find("PreviewText");

        if (textChild != null)
        {
            previewText = textChild.GetComponent<TMP_Text>();
        }
    }

    private void UpdatePreview()
    {
        if (previewText == null)
        {
            return;
        }

        previewText.gameObject.SetActive(true);
        previewText.text = "P";
        previewText.fontSize = previewFontSize;
        previewText.alignment = TextAlignmentOptions.Center;

        RectTransform rectTransform = previewText.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition3D = new Vector3(0f, 0f, -0.05f);
            rectTransform.sizeDelta = new Vector2(20f, 20f);
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
    }

    private void HideAbilityObject()
    {
        AbilityDisappearAnimation.Play(gameObject);
    }

    public SavedAbilityData CreateSaveData()
    {
        if (hasBeenUsed)
        {
            return null;
        }

        return new SavedAbilityData{abilityType = "power", x = transform.position.x, y = transform.position.y,
            value = Value, aim = Aim};
    }
}