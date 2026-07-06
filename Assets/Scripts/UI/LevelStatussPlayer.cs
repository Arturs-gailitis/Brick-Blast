using UnityEngine;

public class LevelStatussPlayer : MonoBehaviour
{
    [Header("Audio clips")]
    [SerializeField] private AudioClip levelCompleteClip;
    [SerializeField] private AudioClip gameOverClip;

    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float volume = 0.8f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayLevelCompleteSound()
    {
        PlaySound(levelCompleteClip);
    }

    public void PlayGameOverSound()
    {
        PlaySound(gameOverClip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, volume);
    }
}