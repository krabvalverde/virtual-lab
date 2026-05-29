using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class InspectionController : MonoBehaviour
{
    public Transform holdAnchor;
    public float rotationSpeed = 200f;
    public PlayerController playerController;
    public InspectionUI ui;
    public InteractionRaycaster raycaster;
    public KeyCode exitKey = KeyCode.Escape;

    private enum State { Idle, Inspecting }
    private State state = State.Idle;

    private Inspectable currentTarget;
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private bool originalRigidbodyKinematic;
    private Rigidbody targetRigidbody;
    private readonly List<Collider> disabledColliders = new List<Collider>();

    private void Awake()
    {
        Assert.IsNotNull(holdAnchor, "InspectionController: holdAnchor não atribuído.");
        Assert.IsNotNull(playerController, "InspectionController: playerController não atribuído.");
        Assert.IsNotNull(ui, "InspectionController: ui não atribuído.");
        Assert.IsNotNull(raycaster, "InspectionController: raycaster não atribuído.");
    }

    private void OnEnable()
    {
        raycaster.OnPressed += HandlePressed;
    }

    private void OnDisable()
    {
        raycaster.OnPressed -= HandlePressed;
    }

    private void HandlePressed(Inspectable target)
    {
        if (state != State.Idle) return;
        Enter(target);
    }

    private void Update()
    {
        if (state != State.Inspecting) return;

        float yaw = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float pitch = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
        currentTarget.transform.Rotate(Vector3.up, -yaw, Space.World);
        currentTarget.transform.Rotate(holdAnchor.right, pitch, Space.World);

        if (Input.GetKeyDown(raycaster.interactKey) || Input.GetKeyDown(exitKey))
        {
            Exit();
        }
    }

    private void Enter(Inspectable target)
    {
        state = State.Inspecting;
        currentTarget = target;
        currentTarget.SetHighlight(false);

        originalParent = target.transform.parent;
        originalLocalPos = target.transform.localPosition;
        originalLocalRot = target.transform.localRotation;

        targetRigidbody = target.GetComponent<Rigidbody>();
        if (targetRigidbody != null)
        {
            originalRigidbodyKinematic = targetRigidbody.isKinematic;
            targetRigidbody.isKinematic = true;
        }

        disabledColliders.Clear();
        foreach (var c in target.GetComponentsInChildren<Collider>())
        {
            if (c.enabled)
            {
                c.enabled = false;
                disabledColliders.Add(c);
            }
        }

        target.transform.SetParent(holdAnchor, worldPositionStays: false);
        target.transform.localPosition = Vector3.zero;
        target.transform.localRotation = Quaternion.identity;

        playerController.enabled = false;
        raycaster.enabled = false;

        ui.Show(target.info);
    }

    private void Exit()
    {
        if (currentTarget == null) return;
        ui.Hide();

        currentTarget.transform.SetParent(originalParent, worldPositionStays: false);
        currentTarget.transform.localPosition = originalLocalPos;
        currentTarget.transform.localRotation = originalLocalRot;

        if (targetRigidbody != null)
        {
            targetRigidbody.isKinematic = originalRigidbodyKinematic;
        }

        foreach (var c in disabledColliders)
        {
            if (c != null) c.enabled = true;
        }
        disabledColliders.Clear();

        currentTarget = null;
        targetRigidbody = null;
        state = State.Idle;

        playerController.enabled = true;
        raycaster.enabled = true;
    }
}
