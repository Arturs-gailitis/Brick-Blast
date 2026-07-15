using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    [Header("Side wall bounce protection")]
    [SerializeField] [Range(0.01f, 0.5f)] private float minimumVerticalDirection = 0.12f;

    [Header("Attack settings")]
    [SerializeField] [Min(1)] private int attackStrength = 1;

    public int AttackStrength => Mathf.Max(1, attackStrength);
    public bool TurnIsActive => turnIsActive;

    public float MinimumSideWallVerticalDirection => Mathf.Clamp(minimumVerticalDirection, 0.01f, 0.5f);

    public event Action<int> AttackStrengthChanged;

    private Rigidbody2D ballRigidbody;
    private CircleCollider2D ballCollider;
    private Camera mainCamera;
    private MultiBallShooter multiBallShooter;

    private bool canShoot = true;
    private bool turnIsActive;

    private bool correctSideWallBounceOnNextFixedUpdate;
    private float sideWallBounceVerticalSign = 1f;

    private Vector2 launchPosition;

    private bool gameplayInputEnabled = true;
    private bool waitForPointerRelease;
    private bool ignoreCurrentPointerUntilReleased;
    private int inputEnabledFrame;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

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

        attackStrength = GameSaveManager.LoadBallAttackStrength();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        multiBallShooter = GetComponent<MultiBallShooter>();
    }

    private void Start()
    {
        NotifyAttackStrengthChanged();
    }

    private void Update()
    {
        if (!gameplayInputEnabled)
        {
            HideTrajectory();
            return;
        }

        if (ignoreCurrentPointerUntilReleased)
        {
            HideTrajectory();

            if (!IsPointerCurrentlyPressed())
            {
                ignoreCurrentPointerUntilReleased = false;
            }

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

    private void FixedUpdate()
    {
        if (!correctSideWallBounceOnNextFixedUpdate)
        {
            return;
        }

        correctSideWallBounceOnNextFixedUpdate = false;

        CorrectTooHorizontalVelocity();
    }

    private void LaunchBall(Vector2 direction)
    {
        canShoot = false;
        turnIsActive = true;

        launchPosition = ballRigidbody.position;

        correctSideWallBounceOnNextFixedUpdate = false;
        sideWallBounceVerticalSign = direction.y < 0f ? -1f : 1f;

        HideTrajectory();

        if (multiBallShooter != null)
        {
            bool startedMultiBallShot = multiBallShooter.StartShot(direction, ballSpeed, bottomWallLayer, 
                bottomWallGap);

            if (startedMultiBallShot)
            {
                return;
            }
        }

        ballRigidbody.WakeUp();
        ballRigidbody.linearVelocity = direction.normalized * ballSpeed;

        ballRigidbody.angularVelocity = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int collisionLayer = collision.collider.gameObject.layer;

        if (IsBottomWall(collisionLayer))
        {
            float ballHalfHeight = ballCollider.bounds.extents.y;

            float safeY = collision.collider.bounds.max.y + ballHalfHeight + bottomWallGap;

            Vector2 safePosition = ballRigidbody.position;

            safePosition.y = safeY;

            StopBallAndFinishTurn(safePosition, true);

            return;
        }

        if (IsSideWallCollision(collision, collisionLayer))
        {
            RememberSideWallBounceDirection(collision);

            correctSideWallBounceOnNextFixedUpdate = true;
        }
    }

    private bool IsSideWallCollision(Collision2D collision, int collisionLayer)
    {
        if (!IsTrajectoryWallLayer(collisionLayer))
        {
            return false;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            bool contactIsHorizontal = Mathf.Abs(contact.normal.x) > Mathf.Abs(contact.normal.y);

            if (contactIsHorizontal)
            {
                return true;
            }
        }

        return false;
    }

    private void RememberSideWallBounceDirection(Collision2D collision)
    {
        float verticalVelocity = ballRigidbody.linearVelocity.y;

        if (Mathf.Abs(verticalVelocity) < 0.0001f)
        {
            verticalVelocity = collision.relativeVelocity.y;
        }

        if (Mathf.Abs(verticalVelocity) >= 0.0001f)
        {
            sideWallBounceVerticalSign = Mathf.Sign(verticalVelocity);
        }
    }

    private void CorrectTooHorizontalVelocity()
    {
        Vector2 velocity = ballRigidbody.linearVelocity;

        float speed = velocity.magnitude;

        if (speed < 0.0001f)
        {
            return;
        }

        Vector2 direction = velocity / speed;

        float safeMinimumVerticalDirection = MinimumSideWallVerticalDirection;

        if (Mathf.Abs(direction.y) >= safeMinimumVerticalDirection)
        {
            return;
        }

        float verticalSign;

        if (Mathf.Abs(direction.y) >= 0.0001f)
        {
            verticalSign = Mathf.Sign(direction.y);
        }
        else
        {
            verticalSign = sideWallBounceVerticalSign;
        }

        float horizontalSign = direction.x < 0f ? -1f : 1f;

        float correctedY =verticalSign * safeMinimumVerticalDirection;

        float correctedX = horizontalSign * Mathf.Sqrt(1f - correctedY * correctedY);

        Vector2 correctedDirection = new Vector2(correctedX, correctedY);

        ballRigidbody.linearVelocity = correctedDirection * speed;
    }

    public bool IsTrajectoryWallLayer(int objectLayer)
    {
        return (wallLayer.value & (1 << objectLayer)) != 0;
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
            RaycastHit2D hit = Physics2D.CircleCast(currentPosition, ballRadius, currentDirection, trajectoryDistance,
                    wallLayer);

            if (hit.collider == null)
            {
                trajectoryPoints.Add(currentPosition + currentDirection * trajectoryDistance);

                break;
            }

            Vector2 bounceCenter = hit.centroid;

            trajectoryPoints.Add(hit.point);

            if (IsBottomWall(hit.collider.gameObject.layer))
            {
                break;
            }

            currentDirection = Vector2.Reflect(currentDirection, hit.normal).normalized;

            bool hitSideWall = Mathf.Abs(hit.normal.x) > Mathf.Abs(hit.normal.y);

            if (hitSideWall)
            {
                currentDirection = CorrectTooHorizontalDirection(currentDirection);
            }

            currentPosition = bounceCenter + currentDirection * rayStartOffset;
        }

        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = trajectoryPoints.Count;

        lineRenderer.SetPositions(trajectoryPoints.ToArray());
    }

    private Vector2 CorrectTooHorizontalDirection(Vector2 direction)
    {
        float safeMinimumVerticalDirection = MinimumSideWallVerticalDirection;

        if (Mathf.Abs(direction.y) >= safeMinimumVerticalDirection)
        {
            return direction;
        }

        float verticalSign;

        if (Mathf.Abs(direction.y) >= 0.0001f)
        {
            verticalSign = Mathf.Sign(direction.y);
        }
        else
        {
            verticalSign = 1f;
        }

        float horizontalSign = direction.x < 0f ? -1f : 1f;

        float correctedY = verticalSign * safeMinimumVerticalDirection;

        float correctedX = horizontalSign * Mathf.Sqrt(1f - correctedY * correctedY);

        return new Vector2(correctedX, correctedY);
    }

    public void IncreaseAttackStrength(int amount)
    {
        int safeAmount = Mathf.Max(1, amount);

        attackStrength += safeAmount;

        GameSaveManager.SaveBallAttackStrength(AttackStrength);

        NotifyAttackStrengthChanged();
    }

    public void ResetAttackStrength()
    {
        attackStrength = 1;

        GameSaveManager.SaveBallAttackStrength(1);

        NotifyAttackStrengthChanged();
    }

    public void SetAttackStrength(int newAttackStrength)
    {
        attackStrength = Mathf.Max(1, newAttackStrength);

        AttackStrengthChanged?.Invoke(attackStrength);
    }

    private void NotifyAttackStrengthChanged()
    {
        AttackStrengthChanged?.Invoke(AttackStrength);
    }

    private void ReturnBallToLaunchPosition(bool moveBricksDown = true)
    {
        StopBallAndFinishTurn(launchPosition,moveBricksDown);
    }

    private void StopBallAndFinishTurn(Vector2 finalPosition, bool moveBricksDown)
    {
        multiBallShooter?.CancelShot();

        bool shouldMoveBricks = turnIsActive && moveBricksDown;

        turnIsActive = false;

        correctSideWallBounceOnNextFixedUpdate = false;

        ballRigidbody.linearVelocity = Vector2.zero;

        ballRigidbody.angularVelocity = 0f;

        ballRigidbody.position = finalPosition;

        transform.position = finalPosition;

        ballRigidbody.WakeUp();

        canShoot = true;

        if (shouldMoveBricks)
        {
            LevelManager.Instance?.MoveAllBricksDown();
        }

        HideTrajectory();
    }

    public void SkipCurrentBallFlight()
    {
        if (!turnIsActive)
        {
            return;
        }

        ReturnBallToLaunchPosition(true);
    }

    public void FinishMultiBallShot(Vector2 finalPosition)
    {
        if (!turnIsActive)
        {
            return;
        }

        StopBallAndFinishTurn(finalPosition, true);
    }

    private void HideTrajectory()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
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

            isPointerHeld = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved ||
                touch.phase == TouchPhase.Stationary;

            wasPointerReleased = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
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

        if (IsPointerOverUI(screenPosition))
        {
            ignoreCurrentPointerUntilReleased = true;
            return false;
        }

        if (screenPosition.x < 0 || screenPosition.x > Screen.width || screenPosition.y < 0 ||
            screenPosition.y > Screen.height)
        {
            return false;
        }

        float distanceToBallPlane = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        Vector3 convertedPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y,
                    distanceToBallPlane));

        worldPosition = new Vector2(convertedPosition.x, convertedPosition.y);

        return true;
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

        uiRaycastResults.Clear();

        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        foreach (RaycastResult result in uiRaycastResults)
        {
            if (result.module is GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
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

    private void OnEnable()
    {
        waitForPointerRelease = true;
        inputEnabledFrame = Time.frameCount;

        correctSideWallBounceOnNextFixedUpdate = false;

        HideTrajectory();
    }

    public void ResetBallsForRetry()
    {
        Vector2 resetPosition = turnIsActive ? launchPosition : ballRigidbody.position;

        StopBallAndFinishTurn(resetPosition, false);

        gameplayInputEnabled = false;
        waitForPointerRelease = true;
        ignoreCurrentPointerUntilReleased = true;
        inputEnabledFrame = Time.frameCount;
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

    public SavedBallData CreateSaveData()
    {
        return new SavedBallData
        {
            x = ballRigidbody.position.x,
            y = ballRigidbody.position.y
        };
    }

    public void RestoreSavedState(SavedBallData savedBall)
    {
        if (savedBall == null || ballRigidbody == null)
        {
            return;
        }

        Vector2 restoredPosition = new Vector2(savedBall.x, savedBall.y);

        ballRigidbody.linearVelocity = Vector2.zero;

        ballRigidbody.angularVelocity = 0f;

        ballRigidbody.position = restoredPosition;

        transform.position = restoredPosition;

        ballRigidbody.WakeUp();

        launchPosition = restoredPosition;

        canShoot = true;
        turnIsActive = false;

        correctSideWallBounceOnNextFixedUpdate = false;

        gameplayInputEnabled = true;
        waitForPointerRelease = true;
        ignoreCurrentPointerUntilReleased = false;
        inputEnabledFrame = Time.frameCount;

        NotifyAttackStrengthChanged();

        HideTrajectory();
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

    private void OnDisable()
    {
        SaveAttackStrengthIfAllowed();
    }

    private void OnDestroy()
    {
        SaveAttackStrengthIfAllowed();
    }

    private void SaveAttackStrengthIfAllowed()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.IsGameOver)
        {
            return;
        }

        GameSaveManager.SaveBallAttackStrength(AttackStrength);
    }
}