using System.Collections;
using UnityEngine;

public class LaserBeamAnimation : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Material runtimeMaterial;

    private Color beamColor;

    private float beamWidth;
    private float fadeInSeconds;
    private float holdSeconds;
    private float fadeOutSeconds;
    private float startWidthMultiplier;
    private float flashWidthMultiplier;

    public void Play(LineRenderer targetLineRenderer, Material targetRuntimeMaterial, Color targetBeamColor,
        float targetBeamWidth, float targetFadeInSeconds, float targetHoldSeconds, float targetFadeOutSeconds,
        float targetStartWidthMultiplier, float targetFlashWidthMultiplier)
    {
        lineRenderer = targetLineRenderer;
        runtimeMaterial = targetRuntimeMaterial;
        beamColor = targetBeamColor;

        beamWidth = Mathf.Max(0f, targetBeamWidth);
        fadeInSeconds = Mathf.Max(0f, targetFadeInSeconds);
        holdSeconds = Mathf.Max(0f, targetHoldSeconds);
        fadeOutSeconds = Mathf.Max(0f, targetFadeOutSeconds);

        startWidthMultiplier = Mathf.Max(0f, targetStartWidthMultiplier);

        flashWidthMultiplier = Mathf.Max(0f, targetFlashWidthMultiplier);

        StartCoroutine(AnimateBeam());
    }

    private IEnumerator AnimateBeam()
    {
        SetBeamVisual(0f, startWidthMultiplier);

        yield return AnimateStage(fadeInSeconds, 0f, 1f, startWidthMultiplier, flashWidthMultiplier);

        yield return AnimateStage(holdSeconds, 1f, 1f, flashWidthMultiplier, 1f);

        yield return AnimateStage(fadeOutSeconds, 1f, 0f, 1f, 0.35f);

        DestroyBeam();
    }

    private IEnumerator AnimateStage(float duration, float startAlpha, float endAlpha, float startWidth, float endWidth)
    {
        if (duration <= 0f)
        {
            SetBeamVisual(endAlpha, endWidth);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            float alpha = Mathf.Lerp(startAlpha, endAlpha, smoothProgress);

            float widthMultiplier = Mathf.Lerp(startWidth, endWidth, smoothProgress);

            SetBeamVisual(alpha, widthMultiplier);

            yield return null;
        }

        SetBeamVisual(endAlpha, endWidth);
    }

    private void SetBeamVisual(float alpha, float widthMultiplier)
    {
        if (lineRenderer == null)
        {
            return;
        }

        Color currentColor = beamColor;

        currentColor.a *= Mathf.Clamp01(alpha);

        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;

        float currentWidth = beamWidth * Mathf.Max(0f, widthMultiplier);

        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth;
    }

    private void DestroyBeam()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }
}