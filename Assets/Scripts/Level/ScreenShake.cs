using System.Collections;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [Header("Shake settings")]
    [SerializeField] [Min(0f)] private float duration = 0.08f;
    [SerializeField] [Min(0f)] private float strength;

    private Coroutine shakeCoroutine;
    private Vector3 currentOffset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDisable()
    {
        StopShake();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Shake()
    {
        if (!isActiveAndEnabled || duration <= 0f || strength <= 0f)
        {
            return;
        }

        StopShake();
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            RemoveCurrentOffset();

            float fade = 1f - Mathf.Clamp01(elapsed / duration);

            currentOffset = Random.insideUnitCircle * strength * fade;
            transform.localPosition += currentOffset;

            yield return null;
        }

        RemoveCurrentOffset();
        shakeCoroutine = null;
    }

    private void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        RemoveCurrentOffset();
    }

    private void RemoveCurrentOffset()
    {
        transform.localPosition -= currentOffset;
        currentOffset = Vector3.zero;
    }
}