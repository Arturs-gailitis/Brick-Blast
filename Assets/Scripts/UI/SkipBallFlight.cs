using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SkipBallFlight : MonoBehaviour
{
    private Button button;
    private CanvasGroup buttonCanvasGroup;
    private PlayerBallTrajectory playerBall;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonCanvasGroup = GetComponent<CanvasGroup>();

        if (buttonCanvasGroup == null)
        {
            buttonCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        button.onClick.AddListener(SkipBallFlightAction);

        SetButtonVisible(false);
    }

    private void Start()
    {
        FindPlayerBall();
    }

    private void Update()
    {
        if (!IsPlayerBallValid())
        {
            FindPlayerBall();
        }

        bool ballsAreFlying = playerBall != null && playerBall.TurnIsActive;

        SetButtonVisible(ballsAreFlying);
    }

    private void SetButtonVisible(bool visible)
    {
        if (buttonCanvasGroup != null)
        {
            buttonCanvasGroup.alpha = visible ? 1f : 0f;
            buttonCanvasGroup.interactable = visible;
            buttonCanvasGroup.blocksRaycasts = visible;
        }

        if (button != null)
        {
            button.interactable = visible;
        }
    }

    private void FindPlayerBall()
    {
        PlayerBallTrajectory foundPlayerBall = FindFirstObjectByType<PlayerBallTrajectory>();

        if (foundPlayerBall != null && foundPlayerBall.gameObject.scene.IsValid())
        {
            playerBall = foundPlayerBall;
        }
        else
        {
            playerBall = null;
        }
    }

    private bool IsPlayerBallValid()
    {
        return playerBall != null && playerBall.gameObject.scene.IsValid() && playerBall.gameObject.activeInHierarchy;
    }

    public void SkipBallFlightAction()
    {
        if (!IsPlayerBallValid())
        {
            FindPlayerBall();
        }

        if (playerBall == null || !playerBall.TurnIsActive)
        {
            return;
        }

        playerBall.SkipCurrentBallFlight();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(SkipBallFlightAction);
        }
    }
}