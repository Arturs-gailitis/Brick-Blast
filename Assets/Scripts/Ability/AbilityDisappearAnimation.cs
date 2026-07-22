using System.Collections;
using TMPro;
using UnityEngine;

public class AbilityDisappearAnimation : MonoBehaviour
{
    [Header("Disappear animation")]
    [SerializeField] [Min(0.01f)] private float duration = 0.25f;
    [SerializeField] [Min(1f)] private float popScaleMultiplier = 1.25f;
    [SerializeField] [Range(0.05f, 0.9f)] private float popDurationPercent = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float endScaleMultiplier = 0.1f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] spriteStartColors;

    private TMP_Text[] texts;
    private Color[] textStartColors;

    private bool isPlaying;

    public static void Play(GameObject abilityObject)
    {
        if (abilityObject == null)
        {
            return;
        }

        AbilityDisappearAnimation animation = abilityObject.GetComponent<AbilityDisappearAnimation>();

        if (animation == null)
        {
            animation = abilityObject.AddComponent<AbilityDisappearAnimation>();
        }

        animation.Begin();
    }

    public void Begin()
    {
        if (isPlaying)
        {
            return;
        }

        isPlaying = true;

        DisableColliders();
        RememberVisuals();

        StartCoroutine(AnimateDisappear());
    }

    private void DisableColliders()
    {
        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D abilityCollider in colliders)
        {
            if (abilityCollider != null)
            {
                abilityCollider.enabled = false;
            }
        }
    }

    private void RememberVisuals()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        spriteStartColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteStartColors[i] = spriteRenderers[i].color;
            }
        }

        texts = GetComponentsInChildren<TMP_Text>(true);

        textStartColors = new Color[texts.Length];

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
            {
                textStartColors[i] = texts[i].color;
            }
        }
    }

    private IEnumerator AnimateDisappear()
    {
        Vector3 startScale = transform.localScale;

        Vector3 popScale = startScale * Mathf.Max(1f, popScaleMultiplier);

        Vector3 endScale = startScale * Mathf.Clamp01(endScaleMultiplier);

        float safeDuration = Mathf.Max(0.01f, duration);

        float safePopPercent = Mathf.Clamp(popDurationPercent, 0.05f, 0.9f);

        float popDuration = safeDuration * safePopPercent;

        float shrinkDuration = safeDuration - popDuration;

        yield return AnimateScaleStage(startScale, popScale, popDuration, 1f, 1f);

        yield return AnimateScaleStage(popScale, endScale, shrinkDuration, 1f, 0f);

        transform.localScale = endScale;

        SetVisualAlpha(0f);

        Destroy(gameObject);
    }

    private IEnumerator AnimateScaleStage(Vector3 fromScale, Vector3 toScale, float stageDuration, float fromAlpha,
        float toAlpha)
    {
        if (stageDuration <= 0f)
        {
            transform.localScale = toScale;

            SetVisualAlpha(toAlpha);

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < stageDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / stageDuration);

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, smoothProgress);

            float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, smoothProgress);

            SetVisualAlpha(currentAlpha);

            yield return null;
        }

        transform.localScale = toScale;

        SetVisualAlpha(toAlpha);
    }

    private void SetVisualAlpha(float alphaMultiplier)
    {
        float safeAlpha = Mathf.Clamp01(alphaMultiplier);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            Color color = spriteStartColors[i];

            color.a *= safeAlpha;

            spriteRenderers[i].color = color;
        }

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
            {
                continue;
            }

            Color color = textStartColors[i];

            color.a *= safeAlpha;

            texts[i].color = color;
        }
    }
}