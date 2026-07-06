using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    [Header("Button click sound")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.7f;

    private Button button;
    private AudioSource canvasAudioSource;

    private void Awake()
    {
        button = GetComponent<Button>();

        Canvas parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            return;
        }

        canvasAudioSource = parentCanvas.GetComponent<AudioSource>();

        if (canvasAudioSource == null)
        {
            canvasAudioSource = parentCanvas.gameObject.AddComponent<AudioSource>();
        }

        canvasAudioSource.playOnAwake = false;
        canvasAudioSource.loop = false;
        canvasAudioSource.spatialBlend = 0f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null || !button.interactable || clickClip == null || canvasAudioSource == null)
        {
            return;
        }

        canvasAudioSource.PlayOneShot(clickClip, volume);
    }
}