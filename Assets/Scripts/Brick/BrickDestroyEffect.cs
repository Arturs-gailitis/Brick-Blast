using UnityEngine;

public class BrickDestroyEffect : MonoBehaviour
{
    [Header("Destroy effect")]
    [SerializeField] private GameObject destroyEffectPrefab;
    [SerializeField] private float effectScale;
    [SerializeField] [Range(0.1f, 1f)] private float durationMultiplier = 0.5f;
    [SerializeField] [Min(0f)] private float extraLifetime = 0.5f;

    [Header("Visual settings")]
    [SerializeField] private bool placeAboveBrick = true;
    [SerializeField] private int sortingOrderOffset = 5;
    [SerializeField] private bool tintParticlesWithBrickColor;

    private SpriteRenderer brickRenderer;

    private void Awake()
    {
        brickRenderer = GetComponent<SpriteRenderer>();
    }

    public void Play()
    {
        if (destroyEffectPrefab == null)
        {
            return;
        }

        Vector3 effectPosition = brickRenderer != null ? brickRenderer.bounds.center : transform.position;

        GameObject effectObject = Instantiate(destroyEffectPrefab, effectPosition, Quaternion.identity);

        effectObject.transform.localScale *= Mathf.Max(0.1f, effectScale);

        ParticleSystem[] particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>(true);

        float effectLifetime = 0f;

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystem.main;

            main.loop = false;

            main.simulationSpeed = 1f / durationMultiplier;

            float particleLifetime = (main.duration + main.startLifetime.constantMax) * durationMultiplier;

            effectLifetime = Mathf.Max(effectLifetime, particleLifetime);

            if (tintParticlesWithBrickColor && brickRenderer != null)
            {
                main.startColor = brickRenderer.color;
            }

            if (placeAboveBrick && brickRenderer != null)
            {
                ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();

                if (particleRenderer != null)
                {
                    particleRenderer.sortingLayerID = brickRenderer.sortingLayerID;

                    particleRenderer.sortingOrder = brickRenderer.sortingOrder + sortingOrderOffset;
                }
            }

            particleSystem.Play(true);
        }

        if (effectLifetime <= 0f)
        {
            effectLifetime = 2f * durationMultiplier;
        }

        Destroy(effectObject, effectLifetime + extraLifetime * durationMultiplier);
    }
}