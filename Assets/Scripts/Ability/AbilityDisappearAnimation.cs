using System.Collections;
using TMPro;
using UnityEngine;

public class AbilityDisappearAnimation : MonoBehaviour
{
    [Header("Disappear animation")]
    [SerializeField] [Min(0.01f)] private float duration = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float endScaleMultiplier = 0.1f;
    [SerializeField] private float rotationDegrees = 120f;

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

        Vector3 endScale = startScale * endScaleMultiplier;

        Quaternion startRotation = transform.localRotation;

        Quaternion endRotation = startRotation * Quaternion.Euler(0f, 0f, rotationDegrees);

        float safeDuration = Mathf.Max(0.01f, duration);

        float elapsedTime = 0f;

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / safeDuration);

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.localScale = Vector3.Lerp(startScale, endScale, smoothProgress);

            transform.localRotation = Quaternion.Lerp(startRotation, endRotation, smoothProgress);

            SetVisualAlpha(1f - smoothProgress);

            yield return null;
        }

        transform.localScale = endScale;
        transform.localRotation = endRotation;

        SetVisualAlpha(0f);

        Destroy(gameObject);
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