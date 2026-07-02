using TMPro;
using UnityEngine;

public class BrickCollision : MonoBehaviour
{
    [Header("Brick settings")]
    [SerializeField] [Min(1)] private int health = 3;

    [Header("Reference")]
    [SerializeField] private TMP_Text healthText;

    private void Awake()
    {
        UpdateHealthText();
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