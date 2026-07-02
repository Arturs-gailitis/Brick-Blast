using System.Collections.Generic;
using UnityEngine;

public class BallTrajectory : MonoBehaviour
{
    [Header("Trajectory settings")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask bottomWallLayer;
    [SerializeField] private int maxBounces = 5;
    [SerializeField] private float rayStartOffset = 0.05f;

    private LineRenderer lineRenderer;
    private Camera mainCamera;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        mainCamera = Camera.main;

        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        if (IsPointerHeld())
        {
            Vector2 direction = GetAimDirection();

            if (direction.y > 0)
            {
                DrawTrajectory(direction);
            }
        }
        else
        {
            HideTrajectory();
        }
    }

    private Vector2 GetAimDirection()
    {
        Vector3 pointerPosition;

#if UNITY_EDITOR
        pointerPosition = Input.mousePosition;
#else
        pointerPosition = Input.GetTouch(0).position;
#endif

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(pointerPosition);
        worldPosition.z = 0;

        return ((Vector2)worldPosition - (Vector2)transform.position).normalized;
    }

    private bool IsPointerHeld()
    {
#if UNITY_EDITOR
        return Input.GetMouseButton(0);
#else
        return Input.touchCount > 0;
#endif
    }

    private void DrawTrajectory(Vector2 startDirection)
    {
        List<Vector3> trajectoryPoints = new List<Vector3>();

        Vector2 currentPosition = transform.position;
        Vector2 currentDirection = startDirection;

        trajectoryPoints.Add(currentPosition);

        for (int i = 0; i < maxBounces; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                currentPosition,
                currentDirection,
                100f,
                wallLayer
            );

            if (hit.collider == null)
            {
                trajectoryPoints.Add(currentPosition + currentDirection * 100f);
                break;
            }

            trajectoryPoints.Add(hit.point);

            if ((bottomWallLayer.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                break;
            }

            currentDirection = Vector2.Reflect(currentDirection, hit.normal);
            currentPosition = hit.point + currentDirection * rayStartOffset;
        }

        lineRenderer.positionCount = trajectoryPoints.Count;
        lineRenderer.SetPositions(trajectoryPoints.ToArray());
    }

    private void HideTrajectory()
    {
        lineRenderer.positionCount = 0;
    }
}