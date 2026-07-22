using UnityEngine;

public class BallHitSound : MonoBehaviour
{
    [Header("Impact sound")]
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private AudioClip wallHitClip;
    [SerializeField] [Range(0f, 1f)] private float volume;

    [Header("Wall particle effect")]
    [SerializeField] private ParticleSystem wallHitParticlePrefab;
    [SerializeField] [Min(0f)] private float particleWallOffset = 0.02f;
    [SerializeField] [Min(0f)] private float particleDestroyDelay = 0.1f;

    [Header("Impact settings")]
    [SerializeField] [Min(0f)] private float minimumHitSpeed = 0.1f;

    private static AudioSource sharedImpactAudioSource;
    private AudioSource localAudioSource;
    private Rigidbody2D ballRigidbody;

    private void Awake()
    {
        localAudioSource = GetComponent<AudioSource>();
        ballRigidbody = GetComponent<Rigidbody2D>();

        ConfigureLocalAudioSource();
        EnsureSharedImpactAudioSource();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int hitLayer = collision.collider.gameObject.layer;

        bool isValidImpactLayer = (wallLayers.value & (1 << hitLayer)) != 0;

        if (!isValidImpactLayer)
        {
            return;
        }

        float hitSpeed = GetReliableHitSpeed(collision);

        if (hitSpeed < minimumHitSpeed)
        {
            return;
        }

        PlayImpactSound();

        if (TryGetSideOrTopWallContact(collision, out ContactPoint2D contact))
        {
            PlayWallHitParticles(contact);
        }
    }

    private float GetReliableHitSpeed(Collision2D collision)
    {
        float relativeHitSpeed = collision.relativeVelocity.magnitude;

        if (ballRigidbody == null)
        {
            return relativeHitSpeed;
        }

        float currentBallSpeed = ballRigidbody.linearVelocity.magnitude;

        return Mathf.Max(relativeHitSpeed, currentBallSpeed);
    }

    private void PlayImpactSound()
    {
        if (wallHitClip == null)
        {
            return;
        }

        EnsureSharedImpactAudioSource();

        if (sharedImpactAudioSource == null)
        {
            return;
        }

        SyncMuteState();

        if (sharedImpactAudioSource.mute || AudioListener.pause)
        {
            return;
        }

        sharedImpactAudioSource.PlayOneShot(wallHitClip, volume);
    }

    private void ConfigureLocalAudioSource()
    {
        if (localAudioSource == null)
        {
            return;
        }

        localAudioSource.playOnAwake = false;
        localAudioSource.loop = false;

        localAudioSource.spatialBlend = 0f;
        localAudioSource.dopplerLevel = 0f;

        localAudioSource.Stop();
    }

    private void EnsureSharedImpactAudioSource()
    {
        if (sharedImpactAudioSource != null)
        {
            return;
        }

        GameObject audioObject = new GameObject("Ball Impact Audio Source");

        DontDestroyOnLoad(audioObject);

        sharedImpactAudioSource = audioObject.AddComponent<AudioSource>();

        sharedImpactAudioSource.playOnAwake = false;
        sharedImpactAudioSource.loop = false;

        sharedImpactAudioSource.volume = 1f;
        sharedImpactAudioSource.pitch = 1f;

        sharedImpactAudioSource.priority = 0;

        sharedImpactAudioSource.spatialBlend = 0f;
        sharedImpactAudioSource.dopplerLevel = 0f;

        if (localAudioSource == null)
        {
            return;
        }

        sharedImpactAudioSource.outputAudioMixerGroup = localAudioSource.outputAudioMixerGroup;

        sharedImpactAudioSource.bypassEffects = localAudioSource.bypassEffects;

        sharedImpactAudioSource.bypassListenerEffects = localAudioSource.bypassListenerEffects;

        sharedImpactAudioSource.bypassReverbZones = localAudioSource.bypassReverbZones;

        sharedImpactAudioSource.mute = localAudioSource.mute;
    }

    private void SyncMuteState()
    {
        if (sharedImpactAudioSource == null || localAudioSource == null)
        {
            return;
        }

        sharedImpactAudioSource.mute = localAudioSource.mute;
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