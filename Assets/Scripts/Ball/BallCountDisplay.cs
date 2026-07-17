using TMPro;
using UnityEngine;

public class BallCountDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MultiBallShooter multiBallShooter;
    [SerializeField] private GameObject ballCountObject;
    [SerializeField] private TMP_Text ballCountText;

    [Header("Text settings")]
    [SerializeField] private string countPrefix = "x";

    [Header("Text position")]
    [SerializeField] [Min(0f)] private float textGapFromBall;

    [SerializeField] [Min(0f)] private float rightWallSwitchDistance;

    private Collider2D ballCollider;
    private Collider2D rightWallCollider;

    private int lastDisplayedCount = -1;
    private bool lastVisibility;
    private bool visibilityWasSet;
    private bool textIsOnLeft;
    private RectTransform ballCountRectTransform;

    private void Awake()
    {
        FindMissingReferences();
        RefreshDisplay(true);
        RefreshTextPosition(true);
    }

    private void OnEnable()
    {
        FindMissingReferences();
        RefreshDisplay(true);
        RefreshTextPosition(true);
    }

    private void Update()
    {
        if (multiBallShooter == null || ballCountObject == null || ballCountText == null || rightWallCollider == null)
        {
            FindMissingReferences();
        }

        RefreshDisplay(false);
        RefreshTextPosition(false);
    }

    private void FindMissingReferences()
    {
        if (multiBallShooter == null)
        {
            multiBallShooter = GetComponent<MultiBallShooter>();
        }

        if (ballCollider == null)
        {
            ballCollider = GetComponent<Collider2D>();
        }

        if (ballCountText == null && ballCountObject != null)
        {
            ballCountText = ballCountObject.GetComponent<TMP_Text>();

            if (ballCountText == null)
            {
                ballCountText = ballCountObject.GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (ballCountObject == null && ballCountText != null)
        {
            ballCountObject = ballCountText.gameObject;
        }

        if (rightWallCollider == null)
        {
            GameObject rightWall = GameObject.Find("RightWall");

            if (rightWall != null)
            {
                rightWallCollider = rightWall.GetComponent<Collider2D>();
            }
        }

        if (ballCountRectTransform == null && ballCountObject != null)
        {
            ballCountRectTransform = ballCountObject.GetComponent<RectTransform>();
        }
    }

    private void RefreshDisplay(bool forceRefresh)
    {
        if (multiBallShooter == null || ballCountObject == null || ballCountText == null)
        {
            return;
        }

        int currentBallCount = multiBallShooter.BallsPerShot;

        if (forceRefresh || currentBallCount != lastDisplayedCount)
        {
            ballCountText.text = countPrefix + currentBallCount;
            lastDisplayedCount = currentBallCount;
        }

        bool shouldBeVisible = !multiBallShooter.ShotIsActive;

        if (forceRefresh || !visibilityWasSet || shouldBeVisible != lastVisibility)
        {
            ballCountObject.SetActive(shouldBeVisible);

            lastVisibility = shouldBeVisible;
            visibilityWasSet = true;
        }
    }

    private void RefreshTextPosition(bool forceRefresh)
    {
        if (ballCountRectTransform == null || rightWallCollider == null)
        {
            return;
        }

        float ballRightEdge;

        if (ballCollider != null)
        {
            ballRightEdge = ballCollider.bounds.max.x;
        }
        else
        {
            ballRightEdge = transform.position.x;
        }

        float distanceToRightWall = rightWallCollider.bounds.min.x - ballRightEdge;

        bool shouldBeOnLeft = distanceToRightWall <= rightWallSwitchDistance;

        if (!forceRefresh && shouldBeOnLeft == textIsOnLeft)
        {
            return;
        }

        float ballRadius = ballCollider != null ? ballCollider.bounds.extents.x : 3.5f;

        float horizontalPosition = ballRadius + textGapFromBall;

        Vector2 anchoredPosition = ballCountRectTransform.anchoredPosition;

        if (shouldBeOnLeft)
        {
            ballCountRectTransform.pivot = new Vector2(1f, 0.5f);

            anchoredPosition.x = -horizontalPosition;

            ballCountText.alignment = TextAlignmentOptions.MidlineRight;
        }
        else
        {
            ballCountRectTransform.pivot = new Vector2(0f, 0.5f);

            anchoredPosition.x = horizontalPosition;

            ballCountText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        ballCountRectTransform.anchoredPosition = anchoredPosition;

        textIsOnLeft = shouldBeOnLeft;
    }

    private void OnValidate()
    {
        FindMissingReferences();

        if (Application.isPlaying)
        {
            RefreshDisplay(true);
            RefreshTextPosition(true);
        }
    }
}