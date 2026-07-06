using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WallHitSound : MonoBehaviour
{
    [Header("Wall sound")]
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private AudioClip wallHitClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.7f;

    [Header("Impact settings")]
    [SerializeField] [Min(0f)] private float minimumHitSpeed = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.Stop();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int hitLayer = collision.collider.gameObject.layer;

        if ((wallLayers.value & (1 << hitLayer)) == 0)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < minimumHitSpeed)
        {
            return;
        }

        if (wallHitClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(wallHitClip, volume);
    }
}