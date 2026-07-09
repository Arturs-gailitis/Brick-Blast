using TMPro;
using UnityEngine;

public class DirectionAbility : MonoBehaviour
{
    [Header("Direction text")]
    [SerializeField] private TMP_Text directionText;

    private int aim = 1;

    private void Awake()
    {
        if (directionText == null)
        {
            directionText = GetComponentInChildren<TMP_Text>(true);
        }

        UpdateDirectionText();
    }

    public void Configure(AbilityConfig config)
    {
        if (config == null)
        {
            return;
        }

        aim = Mathf.Clamp(config.aim, 1, 8);

        UpdateDirectionText();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }

        Rigidbody2D ballRigidbody = collision.GetComponent<Rigidbody2D>();

        if (ballRigidbody == null)
        {
            return;
        }

        Vector2 newDirection = GetDirectionFromAim(aim);

        float currentSpeed = ballRigidbody.linearVelocity.magnitude;

        if (currentSpeed <= 0f)
        {
            currentSpeed = 8f;
        }

        ballRigidbody.linearVelocity = newDirection * currentSpeed;

        Destroy(gameObject);
    }

    private void UpdateDirectionText()
    {
        if (directionText == null)
        {
            return;
        }

        directionText.text = GetDirectionTextFromAim(aim);
    }

    private string GetDirectionTextFromAim(int selectedAim)
    {
        switch (selectedAim)
        {
            case 1:
                return "N";

            case 2:
                return "NE";

            case 3:
                return "E";

            case 4:
                return "SE";

            case 5:
                return "S";

            case 6:
                return "SW";

            case 7:
                return "W";

            case 8:
                return "NW";

            default:
                return "N";
        }
    }

    private Vector2 GetDirectionFromAim(int selectedAim)
    {
        switch (selectedAim)
        {
            case 1:
                return Vector2.up;

            case 2:
                return new Vector2(1f, 1f).normalized;

            case 3:
                return Vector2.right;

            case 4:
                return new Vector2(1f, -1f).normalized;

            case 5:
                return Vector2.down;

            case 6:
                return new Vector2(-1f, -1f).normalized;

            case 7:
                return Vector2.left;

            case 8:
                return new Vector2(-1f, 1f).normalized;

            default:
                return Vector2.up;
        }
    }

    public SavedAbilityData CreateSaveData()
    {
        SavedAbilityData savedAbilityData = new SavedAbilityData();

        savedAbilityData.abilityType = "direction";
        savedAbilityData.x = transform.position.x;
        savedAbilityData.y = transform.position.y;
        savedAbilityData.value = 1;
        savedAbilityData.durationSeconds = 0f;
        savedAbilityData.aim = aim;

        return savedAbilityData;
    }
}