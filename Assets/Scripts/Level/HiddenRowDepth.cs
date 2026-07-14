using UnityEngine;

public class HiddenRowDepth : MonoBehaviour
{
    private float originalZ;
    private float hiddenZ;

    private float emergeY;
    private float revealY;
    private float revealTolerance;

    private int hiddenSortingLayerId;
    private int hiddenSortingOrder;

    private bool isHidden;
    private bool isEmerging;

    private Collider2D[] colliders;
    private bool[] originalColliderStates;

    private Renderer[] renderers;
    private bool[] originalRendererStates;
    private int[] originalRendererSortingLayerIds;
    private int[] originalRendererSortingOrders;

    private Canvas[] canvases;
    private bool[] originalCanvasStates;
    private bool[] originalCanvasOverrideSorting;
    private int[] originalCanvasSortingLayerIds;
    private int[] originalCanvasSortingOrders;

    private BrickCollision brickCollision;

    public bool IsHidden => isHidden;

    public void Initialize(float newOriginalZ, float newHiddenZ, float newRevealY, float topWallTopY,bool startsHidden,
        float newRevealTolerance, int newHiddenSortingLayerId, int newHiddenSortingOrder)
    {
        originalZ = newOriginalZ;
        hiddenZ = newHiddenZ;

        revealY = newRevealY;
        revealTolerance = Mathf.Max(0f, newRevealTolerance);

        hiddenSortingLayerId = newHiddenSortingLayerId;

        hiddenSortingOrder = newHiddenSortingOrder;

        SaveColliderStates();
        SaveRendererStates();
        SaveCanvasStates();

        brickCollision = GetComponent<BrickCollision>();

        float visualHalfHeight = GetVisualHalfHeight();

        emergeY = topWallTopY - visualHalfHeight;

        if (startsHidden)
        {
            HideBehindTopWall();
        }
        else
        {
            ShowOnBoard();
        }
    }

    public void RefreshVisibility()
    {
        if (!isHidden)
        {
            return;
        }

        if (!isEmerging && transform.position.y <= emergeY + revealTolerance)
        {
            BeginEmerging();
        }

        if (isEmerging && transform.position.y <= revealY + revealTolerance)
        {
            ShowOnBoard();
        }
    }

    private void HideBehindTopWall()
    {
        isHidden = true;
        isEmerging = false;

        SetZ(hiddenZ);

        ApplyHiddenSorting();

        SetCollidersEnabled(false);
        SetRenderersEnabled(false);
        SetCanvasesEnabled(false);

        if (brickCollision != null)
        {
            brickCollision.SetGameplayActive(false);
        }
    }

    private void BeginEmerging()
    {
        if (!isHidden || isEmerging)
        {
            return;
        }

        isEmerging = true;

        SetZ(hiddenZ);

        RestoreRendererStates();
        RestoreCanvasStates();

        ApplyHiddenSorting();

        SetCollidersEnabled(false);

        if (brickCollision != null)
        {
            brickCollision.SetGameplayActive(false);
        }
    }

    private void ShowOnBoard()
    {
        isHidden = false;
        isEmerging = false;

        SetZ(originalZ);

        RestoreRendererStates();
        RestoreCanvasStates();

        RestoreRendererSorting();
        RestoreCanvasSorting();

        RestoreColliderStates();

        if (brickCollision != null)
        {
            brickCollision.SetGameplayActive(true);
        }
    }

    private float GetVisualHalfHeight()
    {
        SpriteRenderer mainSpriteRenderer = GetComponent<SpriteRenderer>();

        if (mainSpriteRenderer != null)
        {
            return mainSpriteRenderer.bounds.extents.y;
        }

        Collider2D mainCollider = GetComponent<Collider2D>();

        if (mainCollider != null)
        {
            return mainCollider.bounds.extents.y;
        }

        return 0f;
    }

    private void SaveColliderStates()
    {
        colliders = GetComponentsInChildren<Collider2D>(true);

        originalColliderStates = new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
        {
            originalColliderStates[i] = colliders[i] != null && colliders[i].enabled;
        }
    }

    private void SaveRendererStates()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        originalRendererStates = new bool[renderers.Length];

        originalRendererSortingLayerIds = new int[renderers.Length];

        originalRendererSortingOrders = new int[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            originalRendererStates[i] = renderers[i].enabled;

            originalRendererSortingLayerIds[i] = renderers[i].sortingLayerID;

            originalRendererSortingOrders[i] = renderers[i].sortingOrder;
        }
    }

    private void SaveCanvasStates()
    {
        canvases = GetComponentsInChildren<Canvas>(true);

        originalCanvasStates = new bool[canvases.Length];

        originalCanvasOverrideSorting = new bool[canvases.Length];

        originalCanvasSortingLayerIds = new int[canvases.Length];

        originalCanvasSortingOrders = new int[canvases.Length];

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null)
            {
                continue;
            }

            originalCanvasStates[i] = canvases[i].enabled;

            originalCanvasOverrideSorting[i] = canvases[i].overrideSorting;

            originalCanvasSortingLayerIds[i] = canvases[i].sortingLayerID;

            originalCanvasSortingOrders[i] = canvases[i].sortingOrder;
        }
    }

    private void ApplyHiddenSorting()
    {
        if (renderers != null)
        {
            foreach (Renderer objectRenderer in renderers)
            {
                if (objectRenderer == null)
                {
                    continue;
                }

                objectRenderer.sortingLayerID = hiddenSortingLayerId;

                objectRenderer.sortingOrder = hiddenSortingOrder;
            }
        }

        if (canvases != null)
        {
            foreach (Canvas objectCanvas in canvases)
            {
                if (objectCanvas == null)
                {
                    continue;
                }

                objectCanvas.overrideSorting = true;

                objectCanvas.sortingLayerID = hiddenSortingLayerId;

                objectCanvas.sortingOrder = hiddenSortingOrder;
            }
        }
    }

    private void RestoreRendererSorting()
    {
        if (renderers == null || originalRendererSortingLayerIds == null || originalRendererSortingOrders == null)
        {
            return;
        }

        int count = Mathf.Min(renderers.Length, originalRendererSortingOrders.Length);

        for (int i = 0; i < count; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].sortingLayerID = originalRendererSortingLayerIds[i];

            renderers[i].sortingOrder = originalRendererSortingOrders[i];
        }
    }

    private void RestoreCanvasSorting()
    {
        if (canvases == null || originalCanvasSortingOrders == null)
        {
            return;
        }

        int count = Mathf.Min(canvases.Length, originalCanvasSortingOrders.Length);

        for (int i = 0; i < count; i++)
        {
            if (canvases[i] == null)
            {
                continue;
            }

            canvases[i].overrideSorting = originalCanvasOverrideSorting[i];

            canvases[i].sortingLayerID = originalCanvasSortingLayerIds[i];

            canvases[i].sortingOrder = originalCanvasSortingOrders[i];
        }
    }

    private void SetZ(float z)
    {
        Vector3 position = transform.position;

        position.z = z;

        transform.position = position;
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (colliders == null)
        {
            return;
        }

        foreach (Collider2D objectCollider in colliders)
        {
            if (objectCollider != null)
            {
                objectCollider.enabled = isEnabled;
            }
        }
    }

    private void SetRenderersEnabled(bool isEnabled)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (Renderer objectRenderer in renderers)
        {
            if (objectRenderer != null)
            {
                objectRenderer.enabled = isEnabled;
            }
        }
    }

    private void SetCanvasesEnabled(bool isEnabled)
    {
        if (canvases == null)
        {
            return;
        }

        foreach (Canvas objectCanvas in canvases)
        {
            if (objectCanvas != null)
            {
                objectCanvas.enabled = isEnabled;
            }
        }
    }

    private void RestoreColliderStates()
    {
        if (colliders == null || originalColliderStates == null)
        {
            return;
        }

        int count = Mathf.Min(colliders.Length, originalColliderStates.Length);

        for (int i = 0; i < count; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = originalColliderStates[i];
            }
        }
    }

    private void RestoreRendererStates()
    {
        if (renderers == null || originalRendererStates == null)
        {
            return;
        }

        int count = Mathf.Min(renderers.Length, originalRendererStates.Length);

        for (int i = 0; i < count; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = originalRendererStates[i];
            }
        }
    }

    private void RestoreCanvasStates()
    {
        if (canvases == null || originalCanvasStates == null)
        {
            return;
        }

        int count = Mathf.Min(canvases.Length, originalCanvasStates.Length);

        for (int i = 0; i < count; i++)
        {
            if (canvases[i] != null)
            {
                canvases[i].enabled = originalCanvasStates[i];
            }
        }
    }
}