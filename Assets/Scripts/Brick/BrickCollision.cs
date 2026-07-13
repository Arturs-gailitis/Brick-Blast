using TMPro;
using UnityEngine;

public class BrickCollision : MonoBehaviour
{
    [Header("Brick settings")]
    [SerializeField] [Min(1)] private int health = 3;
    [SerializeField] private int score;
    [SerializeField] private string blockType;
    [SerializeField] private int rotation;

    [Header("Reference")]
    [SerializeField] private TMP_Text healthText;

    private SpriteRenderer spriteRenderer;
    private bool isDestroyed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateBrickVisuals();
    }

    public void Configure(BrickConfig brickConfig)
    {
        if (brickConfig == null)
        {
            return;
        }

        health = Mathf.Max(1, brickConfig.hitPoints);
        score = Mathf.Max(0, brickConfig.score);

        blockType = brickConfig.blockType;
        rotation = NormalizeRotation(brickConfig.rotation);

        isDestroyed = false;

        ApplyRotation();
        UpdateBrickVisuals();
    }

    public void ConfigureSaved(SavedBrickData savedBrick)
    {
        if (savedBrick == null)
        {
            return;
        }

        health = Mathf.Max(1, savedBrick.health);
        score = Mathf.Max(0, savedBrick.score);

        blockType = savedBrick.blockType;
        rotation = NormalizeRotation(savedBrick.rotation);

        isDestroyed = false;

        ApplyRotation();
        UpdateBrickVisuals();
    }

    public SavedBrickData CreateSaveData()
    {
        return new SavedBrickData
        {
            x = transform.position.x,
            y = transform.position.y,

            health = health,
            score = score,

            blockType = blockType,
            rotation = rotation
        };
    }

    private void ApplyRotation()
    {
        transform.localRotation =Quaternion.Euler(0f, 0f, rotation);

        if (healthText != null)
        {
            healthText.transform.rotation = Quaternion.identity;
        }
    }

    private int NormalizeRotation(int value)
    {
        int normalizedRotation = ((value % 360) + 360) % 360;

        if (normalizedRotation == 90 || normalizedRotation == 180 || normalizedRotation == 270)
        {
            return normalizedRotation;
        }

        return 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed || !collision.collider.CompareTag("Player"))
        {
            return;
        }

        int ballDamage = 1;

        MultiBallProjectile projectile = collision.collider.GetComponent<MultiBallProjectile>();

        if (projectile != null)
        {
            ballDamage = projectile.AttackStrength;
        }
        else
        {
            PlayerBallTrajectory mainBall = collision.collider.GetComponent<PlayerBallTrajectory>();

            if (mainBall != null)
            {
                ballDamage = mainBall.AttackStrength;
            }
        }

        TakeBallDamage(ballDamage);
    }

    private void TakeBallDamage(int damage)
    {
        if (isDestroyed)
        {
            return;
        }

        int safeDamage = Mathf.Max(1, damage);

        health -= safeDamage;

        if (health <= 0)
        {
            DestroyBrickAndGiveScore();
            return;
        }

        UpdateBrickVisuals();
    }

    public void TakeLaserDamage(int damage)
    {
        if (isDestroyed)
        {
            return;
        }

        int safeDamage = Mathf.Max(1, damage);

        health -= safeDamage;

        if (health <= 0)
        {
            DestroyBrickAndGiveScore();
            return;
        }

        UpdateBrickVisuals();
    }

    private void DestroyBrickAndGiveScore()
    {
        if (isDestroyed)
        {
            return;
        }

        isDestroyed = true;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(score);
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.BrickDestroyed();
        }

        Destroy(gameObject);
    }

    private void UpdateBrickVisuals()
    {
        UpdateHealthText();
        UpdateBrickColor();
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = health.ToString();
        }
    }

    private void UpdateBrickColor()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        int repeatedHealthColor = ((health - 1) % 6) + 1;

        switch (repeatedHealthColor)
        {
            case 1:
                spriteRenderer.color = new Color(1f, 0.42f, 0.42f);
                break;

            case 2:
                spriteRenderer.color = new Color(0.2f, 0.8f, 0.35f);
                break;

            case 3:
                spriteRenderer.color = new Color(1f, 0.85f, 0.1f);
                break;

            case 4:
                spriteRenderer.color = new Color(0.84f, 0.35f, 0f);
                break;

            case 5:
                spriteRenderer.color = new Color(0.2f, 0.55f, 1f);
                break;

            case 6:
                spriteRenderer.color = new Color(0.65f, 0.3f, 0.9f);
                break;
        }
    }
}