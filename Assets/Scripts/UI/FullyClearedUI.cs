using System.Collections;
using TMPro;
using UnityEngine;

public class FullyClearedUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject messageObject;
    [SerializeField] private TMP_Text messageText;

    [Header("Text appearance")]
    [SerializeField] [Min(1f)] private float fontSize = 96f;
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;

    [SerializeField] private Color textColor = Color.white;

    [Header("Animation")]
    [SerializeField] [Min(0.01f)] private float fadeInDuration = 0.18f;

    [SerializeField] [Min(0f)] private float holdDuration = 0.85f;

    [SerializeField] [Min(0.01f)] private float fadeOutDuration = 0.25f;

    [SerializeField] [Range(0.1f, 1f)] private float startScale = 0.45f;

    [SerializeField] [Min(1f)] private float overshootScale = 1.18f;

    [SerializeField] [Min(0.01f)] private float settleDuration = 0.12f;

    private Coroutine messageCoroutine;

    private RectTransform messageRectTransform;
    private CanvasGroup messageCanvasGroup;

    private void Awake()
    {
        FindReferences();
        ApplyTextAppearance();
        HideImmediate();
    }

    public void Show()
    {
        FindReferences();

        if (messageObject == null)
        {
            return;
        }

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        messageCoroutine = StartCoroutine(ShowMessageRoutine());
    }

    public void HideImmediate()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        if (messageCanvasGroup != null)
        {
            messageCanvasGroup.alpha = 0f;
        }

        if (messageRectTransform != null)
        {
            messageRectTransform.localScale = Vector3.one;
        }

        if (messageObject != null)
        {
            messageObject.SetActive(false);
        }
    }

    private IEnumerator ShowMessageRoutine()
    {
        messageObject.SetActive(true);

        ApplyTextAppearance();

        if (messageCanvasGroup != null)
        {
            messageCanvasGroup.alpha = 0f;
        }

        if (messageRectTransform != null)
        {
            messageRectTransform.localScale = Vector3.one * startScale;
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / fadeInDuration);

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (messageCanvasGroup != null)
            {
                messageCanvasGroup.alpha = smoothProgress;
            }

            if (messageRectTransform != null)
            {
                float currentScale = Mathf.Lerp(startScale, overshootScale, smoothProgress);

                messageRectTransform.localScale = Vector3.one * currentScale;
            }

            yield return null;
        }

        elapsedTime = 0f;

        while (elapsedTime < settleDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / settleDuration);

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (messageRectTransform != null)
            {
                float currentScale = Mathf.Lerp(overshootScale, 1f, smoothProgress);

                messageRectTransform.localScale = Vector3.one * currentScale;
            }

            yield return null;
        }

        if (messageCanvasGroup != null)
        {
            messageCanvasGroup.alpha = 1f;
        }

        if (messageRectTransform != null)
        {
            messageRectTransform.localScale = Vector3.one;
        }

        yield return new WaitForSecondsRealtime(holdDuration);

        elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01( elapsedTime / fadeOutDuration);

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (messageCanvasGroup != null)
            {
                messageCanvasGroup.alpha = 1f - smoothProgress;
            }

            if (messageRectTransform != null)
            {
                float currentScale = Mathf.Lerp(1f, 1.08f, smoothProgress);

                messageRectTransform.localScale = Vector3.one * currentScale;
            }

            yield return null;
        }

        messageObject.SetActive(false);
        messageCoroutine = null;
    }

    private void FindReferences()
    {
        if (messageObject == null && messageText != null)
        {
            messageObject = messageText.gameObject;
        }

        if (messageText == null && messageObject != null)
        {
            messageText = messageObject.GetComponent<TMP_Text>();

            if (messageText == null)
            {
                messageText = messageObject.GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (messageObject == null)
        {
            return;
        }

        if (messageRectTransform == null)
        {
            messageRectTransform = messageObject.GetComponent<RectTransform>();
        }

        if (messageCanvasGroup == null)
        {
            messageCanvasGroup = messageObject.GetComponent<CanvasGroup>();

            if (messageCanvasGroup == null)
            {
                messageCanvasGroup = messageObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void ApplyTextAppearance()
    {
        if (messageText == null)
        {
            return;
        }

        messageText.text = "FULLY CLEARED";
        messageText.fontSize = fontSize;
        messageText.fontStyle = fontStyle;
        messageText.alignment = TextAlignmentOptions.Center;

        messageText.color = textColor;
        messageText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void OnValidate()
    {
        FindReferences();
        ApplyTextAppearance();
    }
}