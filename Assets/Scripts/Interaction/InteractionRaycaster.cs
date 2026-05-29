using System;
using UnityEngine;

public class InteractionRaycaster : MonoBehaviour
{
    public Transform rayOrigin;
    public float maxDistance = 2.5f;
    public LayerMask inspectableLayer;
    public KeyCode interactKey = KeyCode.E;

    public event Action<Inspectable> OnPressed;

    private Inspectable currentHover;

    private void Awake()
    {
        if (rayOrigin == null)
        {
            Debug.LogError($"InteractionRaycaster em {name} sem rayOrigin (Camera).", this);
            enabled = false;
        }
    }

    private void Update()
    {
        UpdateHover();
        if (currentHover != null && Input.GetKeyDown(interactKey))
        {
            OnPressed?.Invoke(currentHover);
        }
    }

    private void UpdateHover()
    {
        Inspectable next = null;
        if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, maxDistance, inspectableLayer))
        {
            next = hit.collider.GetComponentInParent<Inspectable>();
            if (next != null && next.info == null)
            {
                next = null;
            }
        }

        if (next == currentHover) return;

        if (currentHover != null) currentHover.SetHighlight(false);
        if (next != null) next.SetHighlight(true);
        currentHover = next;
    }

    private void OnDisable()
    {
        if (currentHover != null)
        {
            currentHover.SetHighlight(false);
            currentHover = null;
        }
    }
}
