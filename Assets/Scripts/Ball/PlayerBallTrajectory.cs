using System.Collections.Generic;
using UnityEngine;

public class BallTrajectory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Trajectory settings")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask bottomWallLayer;
    [SerializeField] private int maxBounces = 5;
    [SerializeField] private float trajectoryDistance = 100f;
    [SerializeField] private float rayStartOffset = 0.03f;

    [Header("Ball settings")]
    [SerializeField] private float ballSpeed = 10f;
    [SerializeField] private float bottomWallGap = 0.03f;

    private Rigidbody2D ballRigidbody;
    private CircleCollider2D ballCollider;
    private Camera mainCamera;

    private bool canShoot = true;

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

        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
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

        Vector2 direction = (pointerPosition - (Vector2)transform.position).normalized;

        if (direction.y <= 0.05f)
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
            LaunchBall(direction);
        }
    }

    private void LaunchBall(Vector2 direction)
    {
        canShoot = false;

        ballRigidbody.WakeUp();
        ballRigidbody.linearVelocity = direction * ballSpeed;
        ballRigidbody.angularVelocity = 0f;

        HideTrajectory();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsBottomWall(collision.collider.gameObject.layer))
        {
            return;
        }

        ballRigidbody.linearVelocity = Vector2.zero;
        ballRigidbody.angularVelocity = 0f;

        float ballHalfHeight = ballCollider.bounds.extents.y;

        float safeY = collision.collider.bounds.max.y + ballHalfHeight + bottomWallGap;

        Vector2 safePosition = ballRigidbody.position;
        safePosition.y = safeY;

        ballRigidbody.position = safePosition;

        ballRigidbody.WakeUp();

        canShoot = true;
        HideTrajectory();
    }

    private bool IsBottomWall(int objectLayer)
    {
        return (bottomWallLayer.value & (1 << objectLayer)) != 0;
    }

    private void DrawTrajectory(Vector2 startDirection)
    {
        List<Vector3> trajectoryPoints = new List<Vector3>();
        trajectoryPoints.Add(transform.position);

        float ballRadius = ballCollider.bounds.extents.x;

        Vector2 currentPosition = (Vector2)transform.position + startDirection * rayStartOffset;

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
                trajectoryPoints.Add(currentPosition + currentDirection * trajectoryDistance);
                break;
            }

            trajectoryPoints.Add(hit.point);

            if (IsBottomWall(hit.collider.gameObject.layer))
            {
                break;
            }

            currentDirection = Vector2.Reflect(currentDirection, hit.normal);

            currentPosition = hit.centroid + currentDirection * rayStartOffset;
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
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                distanceToBallPlane
            )
        );

        worldPosition = new Vector2(convertedPosition.x, convertedPosition.y);

        return true;
    }
}