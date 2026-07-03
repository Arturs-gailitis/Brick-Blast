using TMPro;
using UnityEngine;

public class BrickCollision : MonoBehaviour
{
    [Header("Brick settings")]
    [SerializeField] [Min(1)] private int health = 3;
    [SerializeField] private int score;
    [SerializeField] private string blockType;

    [Header("Reference")]
    [SerializeField] private TMP_Text healthText;

    private void Awake()
    {
        UpdateHealthText();
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

        UpdateHealthText();
    }

    public int GetScore()
    {
        return score;
    }

    public string GetBlockType()
    {
        return blockType;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player"))
        {
            return;
        }

        TakeHit();
    }

    private void TakeHit()
    {
        health--;

        if (health <= 0)
        {
            Destroy(gameObject);
            return;
        }

        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        if (healthText == null)
        {
            return;
        }

        healthText.text = health.ToString();
    }
}