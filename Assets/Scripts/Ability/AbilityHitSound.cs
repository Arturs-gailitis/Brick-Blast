using UnityEngine;

public class AbilityHitSound : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] [Range(0, 256)] private int audioPriority = 0;

    [Header("Hit settings")]
    [SerializeField] private bool playOnEveryHit;

    private bool hasPlayed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (!playOnEveryHit && hasPlayed)
        {
            return;
        }

        if (hitSound == null)
        {
            return;
        }

        hasPlayed = true;
        PlaySound();
    }

    private void PlaySound()
    {
        GameObject soundObject = new GameObject("AbilityHitSound");

        AudioSource audioSource = soundObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        audioSource.priority = Mathf.Clamp(audioPriority, 0, 256);

        audioSource.volume = 1f;
        audioSource.pitch = 1f;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;

        audioSource.PlayOneShot(hitSound, volume);

        Destroy(soundObject, hitSound.length + 0.1f);
    }
}