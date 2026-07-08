using TMPro;
using UnityEngine;

public class AttackUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text attackText;

    [Header("Text")]
    [SerializeField] private string prefix = "Attack: ";

    private PlayerBallTrajectory playerBall;
    private PlayerBallTrajectory subscribedPlayerBall;

    private int lastShownAttack = -1;

    private void Awake()
    {
        FindAttackTextIfNeeded();
    }

    private void OnEnable()
    {
        FindAttackTextIfNeeded();
        FindPlayerBallIfNeeded();
        SubscribeToPlayerBallIfNeeded();
        RefreshAttackText();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerBall();
    }

    private void Update()
    {
        FindPlayerBallIfNeeded();
        SubscribeToPlayerBallIfNeeded();
        RefreshAttackText();
    }

    private void FindAttackTextIfNeeded()
    {
        if (attackText != null)
        {
            return;
        }

        attackText = GetComponent<TMP_Text>();

        if (attackText != null)
        {
            return;
        }

        Transform attackTextChild = transform.Find("AttackText");

        if (attackTextChild != null)
        {
            attackText = attackTextChild.GetComponent<TMP_Text>();
        }
    }

    private void FindPlayerBallIfNeeded()
    {
        if (playerBall != null && playerBall.gameObject.scene.IsValid())
        {
            return;
        }

        playerBall = null;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        playerBall = playerObject.GetComponent<PlayerBallTrajectory>();
    }

    private void SubscribeToPlayerBallIfNeeded()
    {
        if (playerBall == null)
        {
            return;
        }

        if (subscribedPlayerBall == playerBall)
        {
            return;
        }

        UnsubscribeFromPlayerBall();

        subscribedPlayerBall = playerBall;
        subscribedPlayerBall.AttackStrengthChanged += OnAttackStrengthChanged;
    }

    private void UnsubscribeFromPlayerBall()
    {
        if (subscribedPlayerBall == null)
        {
            return;
        }

        subscribedPlayerBall.AttackStrengthChanged -= OnAttackStrengthChanged;
        subscribedPlayerBall = null;
    }

    private void OnAttackStrengthChanged(int attackStrength)
    {
        UpdateAttackText(attackStrength);
    }

    private void RefreshAttackText()
    {
        int currentAttack = 1;

        if (playerBall != null)
        {
            currentAttack = playerBall.AttackStrength;
        }

        if (currentAttack != lastShownAttack)
        {
            UpdateAttackText(currentAttack);
        }
    }

    private void UpdateAttackText(int attackStrength)
    {
        lastShownAttack = Mathf.Max(1, attackStrength);

        if (attackText == null)
        {
            return;
        }

        attackText.text = prefix + lastShownAttack;
    }
}