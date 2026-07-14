using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BallHitSound : MonoBehaviour
{
    [Header("Wall sound")]
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private AudioClip wallHitClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.7f;

    [Header("Wall particle effect")]
    [SerializeField] private ParticleSystem wallHitParticlePrefab;
    [SerializeField] [Min(0f)] private float particleWallOffset = 0.02f;
    [SerializeField] [Min(0f)] private float particleDestroyDelay = 0.1f;

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

        PlayWallHitSound();

        if (TryGetSideOrTopWallContact(collision, out ContactPoint2D contact))
        {
            PlayWallHitParticles(contact);
        }
    }

    private void PlayWallHitSound()
    {
        if (wallHitClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(wallHitClip, volume);
    }

    private bool TryGetSideOrTopWallContact(Collision2D collision, out ContactPoint2D selectedContact)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            bool hitSideWall = Mathf.Abs(contact.normal.x) > Mathf.Abs(contact.normal.y);

            bool hitTopWall = Mathf.Abs(contact.normal.y) >= Mathf.Abs(contact.normal.x) && contact.normal.y < -0.5f;

            if (hitSideWall || hitTopWall)
            {
                selectedContact = contact;
                return true;
            }
        }

        selectedContact = default;
        return false;
    }

    private void PlayWallHitParticles(ContactPoint2D contact)
    {
        if (wallHitParticlePrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = contact.point + contact.normal * particleWallOffset;

        Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, contact.normal);

        ParticleSystem spawnedParticles = Instantiate(wallHitParticlePrefab, spawnPosition, spawnRotation);

        spawnedParticles.Play();

        ParticleSystem.MainModule main = spawnedParticles.main;

        float destroyAfter = main.duration + main.startLifetime.constantMax + particleDestroyDelay;

        Destroy(spawnedParticles.gameObject, destroyAfter);
    }
}