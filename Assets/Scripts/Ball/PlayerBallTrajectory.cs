using System.Collections.Generic;
using UnityEngine;

public class PlayerBallTrajectory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Trajectory settings")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask bottomWallLayer;
    [SerializeField] private int maxBounces = 5;
    [SerializeField] private float trajectoryDistance = 100f;
    [SerializeField] private float rayStartOffset = 0.03f;

    [Header("Line quality")]
    [SerializeField] [Range(0, 16)] private int cornerSmoothness = 8;
    [SerializeField] [Range(0, 16)] private int capSmoothness = 6;
    [SerializeField] [Min(0.01f)] private float minimumAimDistance = 0.35f;

    [Header("Ball settings")]
    [SerializeField] private float ballSpeed = 10f;
    [SerializeField] private float bottomWallGap = 0.03f;

    [Header("No brick hit reset")]
    [SerializeField] private float noBrickHitTimeLimit = 6f;

    private Rigidbody2D ballRigidbody;
    private CircleCollider2D ballCollider;
    private Camera mainCamera;

    private bool canShoot = true;
    private bool turnIsActive;

    private Vector2 launchPosition;
    private float timeSinceLastBrickHit;

    private bool gameplayInputEnabled = true;
    private bool waitForPointerRelease;
    private int inputEnabledFrame;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponentInChildren<LineRenderer>();
        }

        ballRigidbody = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<CircleCollider2D>();
        mainCamera = Camera.main;

        ballRigidbody.gravityScale = 0f;
        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;

        ConfigureLineRenderer();

        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        if (!gameplayInputEnabled)
        {
            HideTrajectory();
            return;
        }

        if (waitForPointerRelease)
        {
            HideTrajectory();

            if (Time.frameCount <= inputEnabledFrame)
            {
                return;
            }

            if (!IsPointerCurrentlyPressed())
            {
                waitForPointerRelease = false;
            }

            return;
        }

        CheckNoBrickHitTimer();

        if (!canShoot)
        {
            HideTrajectory();
            return;
        }

        if (!TryGetPointerData(out Vector2 pointerPosition, out bool isPointerHeld, out bool wasPointerReleased))
        {
            HideTrajectory();
            return;
        }

        Vector2 aimVector = pointerPosition - (Vector2)transform.position;

        if (aimVector.sqrMagnitude < minimumAimDistance * minimumAimDistance)
        {
            HideTrajectory();
            return;
        }

        Vector2 direction = aimVector.normalized;

        if (direction.y < -0.01f)
        {
            HideTrajectory();
            return;
        }

        if (isPointerHeld)
        {
            DrawTrajectory(direction);
        }

        if (wasPointerReleased)
        {
            if (direction.y > 0.01f)
            {
                LaunchBall(direction);
            }
            else
            {
                HideTrajectory();
            }
        }
    }

    private void LaunchBall(Vector2 direction)
    {
        canShoot = false;
        turnIsActive = true;

        launchPosition = ballRigidbody.position;
        timeSinceLastBrickHit = 0f;

        ballRigidbody.WakeUp();
        ballRigidbody.linearVelocity = direction * ballSpeed;
        ballRigidbody.angularVelocity = 0f;

        HideTrajectory();
    }

    private void CheckNoBrickHitTimer()
    {
        if (canShoot)
        {
            return;
        }

        timeSinceLastBrickHit += Time.deltaTime;

        if (timeSinceLastBrickHit >= noBrickHitTimeLimit)
        {
            ReturnBallToLaunchPosition();
        }
    }

    public void RegisterBrickHit()
    {
        timeSinceLastBrickHit = 0f;
    }

    private void ReturnBallToLaunchPosition(bool moveBricksDown = true)
    {
        StopBallAndFinishTurn(launchPosition, moveBricksDown);
    }

    private void StopBallAndFinishTurn(Vector2 finalPosition, bool moveBricksDown)
    {
        bool shouldMoveBricks = turnIsActive && moveBricksDown;

        turnIsActive = false;

        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;

        ballRigidbody.position = finalPosition;
        transform.position = finalPosition;

        ballRigidbody.WakeUp();

        canShoot = true;
        timeSinceLastBrickHit = 0f;

        if (shouldMoveBricks)
        {
            LevelManager.Instance?.MoveAllBricksDown();
        }

        HideTrajectory();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsBottomWall(collision.collider.gameObject.layer))
        {
            return;
        }

        float ballHalfHeight = ballCollider.bounds.extents.y;
        float safeY = collision.collider.bounds.max.y + ballHalfHeight + bottomWallGap;

        Vector2 safePosition = ballRigidbody.position;
        safePosition.y = safeY;

        StopBallAndFinishTurn(safePosition, true);
    }

    private bool IsBottomWall(int objectLayer)
    {
        return (bottomWallLayer.value & (1 << objectLayer)) != 0;
    }

    private void DrawTrajectory(Vector2 startDirection)
    {
        List<Vector3> trajectoryPoints = new List<Vector3>(maxBounces + 2);
        trajectoryPoints.Add(ballRigidbody.position);

        float ballRadius = ballCollider.bounds.extents.x;

        Vector2 currentPosition = ballRigidbody.position + startDirection * rayStartOffset;

        Vector2 currentDirection = startDirection;

        for (int i = 0; i < maxBounces; i++)
        {
            RaycastHit2D hit = Physics2D.CircleCast(
                currentPosition,
                ballRadius,
                currentDirection,
                trajectoryDistance,
                wallLayer
            );

            if (hit.collider == null)
            {
                trajectoryPoints.Add( currentPosition + currentDirection * trajectoryDistance);
                break;
            }

            Vector2 bounceCenter = hit.centroid;

            trajectoryPoints.Add(hit.point);

            if (IsBottomWall(hit.collider.gameObject.layer))
            {
                break;
            }

            currentDirection = Vector2.Reflect(currentDirection, hit.normal).normalized;
            currentPosition = bounceCenter + currentDirection * rayStartOffset;
        }

        lineRenderer.positionCount = trajectoryPoints.Count;
        lineRenderer.SetPositions(trajectoryPoints.ToArray());
    }

    private void HideTrajectory()
    {
        lineRenderer.positionCount = 0;
    }

    private bool TryGetPointerData(out Vector2 worldPosition, out bool isPointerHeld, out bool wasPointerReleased)
    {
        worldPosition = Vector2.zero;
        isPointerHeld = false;
        wasPointerReleased = false;

        if (mainCamera == null)
        {
            return false;
        }

        Vector3 screenPosition;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            screenPosition = touch.position;

            isPointerHeld =
                touch.phase == TouchPhase.Began ||
                touch.phase == TouchPhase.Moved ||
                touch.phase == TouchPhase.Stationary;

            wasPointerReleased =
                touch.phase == TouchPhase.Ended ||
                touch.phase == TouchPhase.Canceled;
        }
        else
        {
            isPointerHeld = Input.GetMouseButton(0);
            wasPointerReleased = Input.GetMouseButtonUp(0);

            if (!isPointerHeld && !wasPointerReleased)
            {
                return false;
            }

            screenPosition = Input.mousePosition;
        }

        if (!float.IsFinite(screenPosition.x) || !float.IsFinite(screenPosition.y))
        {
            return false;
        }

        if (screenPosition.x < 0 ||
            screenPosition.x > Screen.width ||
            screenPosition.y < 0 ||
            screenPosition.y > Screen.height)
        {
            return false;
        }

        float distanceToBallPlane = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        Vector3 convertedPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distanceToBallPlane)
        );

        worldPosition = new Vector2(convertedPosition.x, convertedPosition.y);

        return true;
    }

    public void SetGameplayInputEnabled(bool isEnabled)
    {
        gameplayInputEnabled = isEnabled;

        if (isEnabled)
        {
            waitForPointerRelease = true;
            inputEnabledFrame = Time.frameCount;
        }
        else
        {
            HideTrajectory();
        }
    }

    public void PrepareForNextLevel()
    {
        ReturnBallToLaunchPosition(false);

        gameplayInputEnabled = false;
    }

    private bool IsPointerCurrentlyPressed()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
        }

        return Input.GetMouseButton(0);
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;

        lineRenderer.numCornerVertices = cornerSmoothness;
        lineRenderer.numCapVertices = capSmoothness;

        lineRenderer.generateLightingData = false;
    }
    
}