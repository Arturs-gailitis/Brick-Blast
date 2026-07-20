using System.Collections;
using TMPro;
using UnityEngine;

public class BrickBumpAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text healthText;

    [Header("Bump settings")]
    [SerializeField] [Min(0f)] private float bumpDistance = 0.05f;
    [SerializeField] [Min(0.01f)] private float bumpMoveDuration = 0.04f;
    [SerializeField] [Min(0.01f)] private float bumpReturnDuration = 0.09f;
    [SerializeField] [Range(0.8f, 1f)] private float bumpScaleMultiplier = 0.98f;

    private SpriteRenderer originalSpriteRenderer;
    private SpriteRenderer animatedSpriteRenderer;
    private Transform animatedVisual;

    private Coroutine bumpCoroutine;
    private Vector3 healthTextStartLocalPosition;

    private void Awake()
    {
        originalSpriteRenderer = GetComponent<SpriteRenderer>();

        if (healthText == null)
        {
            healthText = GetComponentInChildren<TMP_Text>(true);
        }

        CreateAnimatedVisual();
    }

    public void Play(Collision2D collision)
    {
        if (collision == null || collision.contactCount == 0 || originalSpriteRenderer == null)
        {
            return;
        }

        if (!originalSpriteRenderer.enabled)
        {
            return;
        }

        if (bumpCoroutine != null)
        {
            StopCoroutine(bumpCoroutine);
            ResetVisuals();
        }

        SyncAnimatedSprite();

        ContactPoint2D contact = collision.GetContact(0);

        Collider2D brickCollider = GetComponent<Collider2D>();
        Vector2 brickCenter = brickCollider != null ? brickCollider.bounds.center : transform.position;

        Vector2 worldDirection = brickCenter - contact.point;

        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            worldDirection = -collision.relativeVelocity;
        }

        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            worldDirection = Vector2.up;
        }

        worldDirection.Normalize();

        Vector3 worldOffset = worldDirection * bumpDistance;

        Vector3 visualLocalOffset = transform.InverseTransformVector(worldOffset);
        Vector3 textLocalOffset = visualLocalOffset;

        if (healthText != null && healthText.transform.parent != null)
        {
            textLocalOffset = healthText.transform.parent.InverseTransformVector(worldOffset);

            healthTextStartLocalPosition = healthText.transform.localPosition;
        }

        originalSpriteRenderer.enabled = false;
        animatedSpriteRenderer.enabled = true;

        bumpCoroutine = StartCoroutine(PlayBump(visualLocalOffset, textLocalOffset));
    }

    private void CreateAnimatedVisual()
    {
        GameObject visualObject = new GameObject("BrickBumpVisual");

        visualObject.layer = gameObject.layer;

        animatedVisual = visualObject.transform;
        animatedVisual.SetParent(transform, false);
        animatedVisual.localPosition = Vector3.zero;
        animatedVisual.localRotation = Quaternion.identity;
        animatedVisual.localScale = Vector3.one;

        animatedSpriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        animatedSpriteRenderer.enabled = false;
    }

    private void SyncAnimatedSprite()
    {
        animatedSpriteRenderer.sprite = originalSpriteRenderer.sprite;
        animatedSpriteRenderer.color = originalSpriteRenderer.color;
        animatedSpriteRenderer.flipX = originalSpriteRenderer.flipX;
        animatedSpriteRenderer.flipY = originalSpriteRenderer.flipY;
        animatedSpriteRenderer.drawMode = originalSpriteRenderer.drawMode;
        animatedSpriteRenderer.size = originalSpriteRenderer.size;
        animatedSpriteRenderer.maskInteraction = originalSpriteRenderer.maskInteraction;
        animatedSpriteRenderer.spriteSortPoint = originalSpriteRenderer.spriteSortPoint;
        animatedSpriteRenderer.sortingLayerID = originalSpriteRenderer.sortingLayerID;
        animatedSpriteRenderer.sortingOrder = originalSpriteRenderer.sortingOrder;
        animatedSpriteRenderer.sharedMaterials = originalSpriteRenderer.sharedMaterials;
    }

    private IEnumerator PlayBump(Vector3 visualOffset, Vector3 textOffset)
    {
        Vector3 startPosition = Vector3.zero;
        Vector3 targetPosition = visualOffset;

        Vector3 startScale = Vector3.one;
        Vector3 targetScale = new Vector3(bumpScaleMultiplier, bumpScaleMultiplier, 1f);

        Vector3 textTargetPosition = healthTextStartLocalPosition + textOffset;

        float elapsedTime = 0f;

        while (elapsedTime < bumpMoveDuration)
        {
            elapsedTime += Time.deltaTime;

            float percent = Mathf.Clamp01(elapsedTime / bumpMoveDuration);
            float smoothPercent = Mathf.SmoothStep(0f, 1f, percent);

            animatedVisual.localPosition = Vector3.Lerp(startPosition, targetPosition, smoothPercent);
            animatedVisual.localScale = Vector3.Lerp(startScale, targetScale, smoothPercent);

            if (healthText != null)
            {
                healthText.transform.localPosition = Vector3.Lerp(healthTextStartLocalPosition, textTargetPosition,
                    smoothPercent);
            }

            yield return null;
        }

        elapsedTime = 0f;

        while (elapsedTime < bumpReturnDuration)
        {
            elapsedTime += Time.deltaTime;

            float percent = Mathf.Clamp01(elapsedTime / bumpReturnDuration);
            float smoothPercent = Mathf.SmoothStep(0f, 1f, percent);

            animatedVisual.localPosition = Vector3.Lerp(targetPosition, startPosition, smoothPercent);
            animatedVisual.localScale = Vector3.Lerp(targetScale, startScale, smoothPercent);

            if (healthText != null)
            {
                healthText.transform.localPosition = Vector3.Lerp(textTargetPosition, healthTextStartLocalPosition,
                    smoothPercent);
            }

            yield return null;
        }

        ResetVisuals();
        bumpCoroutine = null;
    }

    private void ResetVisuals()
    {
        if (animatedVisual != null)
        {
            animatedVisual.localPosition = Vector3.zero;
            animatedVisual.localScale = Vector3.one;
        }

        if (animatedSpriteRenderer != null)
        {
            animatedSpriteRenderer.enabled = false;
        }

        if (originalSpriteRenderer != null)
        {
            originalSpriteRenderer.enabled = true;
        }

        if (healthText != null)
        {
            healthText.transform.localPosition = healthTextStartLocalPosition;
        }
    }

    private void OnDisable()
    {
        if (bumpCoroutine != null)
        {
            StopCoroutine(bumpCoroutine);
            bumpCoroutine = null;
        }

        ResetVisuals();
    }
}