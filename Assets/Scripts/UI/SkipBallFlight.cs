using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SkipBallFlight : MonoBehaviour
{
    private Button button;
    private PlayerBallTrajectory playerBall;

    private void Awake()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(SkipBallFlightAction);
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

        if (button != null)
        {
            button.interactable = playerBall != null && playerBall.TurnIsActive;
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

        if (playerBall == null)
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