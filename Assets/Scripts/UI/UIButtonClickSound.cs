using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    [Header("Button click sound")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.7f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null || !button.interactable || clickClip == null)
        {
            return;
        }

        GameObject soundObject = new GameObject("ButtonClickSound");

        AudioSource audioSource = soundObject.AddComponent<AudioSource>();
        audioSource.clip = clickClip;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        DontDestroyOnLoad(soundObject);

        audioSource.Play();

        Destroy(soundObject, clickClip.length);
    }
}