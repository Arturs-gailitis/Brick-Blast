using UnityEngine;

public class AbilityHitSound : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

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

        audioSource.clip = hitSound;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;

        audioSource.Play();

        Destroy(soundObject, hitSound.length + 0.1f);
    }
}