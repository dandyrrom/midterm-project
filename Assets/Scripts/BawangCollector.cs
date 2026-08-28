using UnityEngine;
using UnityEngine.InputSystem;

public class BawangCollector : MonoBehaviour
{
    [Header("Pick Up Timing")]
    [Tooltip("Seconds after E before garlic disappears and the HUD count updates. Raise if the hand grabs too early; lower if too late.")]
    public float grabDelay = 0.85f;

    BawangInventory inventory;
    PlayerHealth health;
    Animator animator;
    PlayerInput playerInput;
    InputAction interactAction;
    BawangPickup pendingPickup;
    float grabAt = -1f;

    static readonly int PickUpHash = Animator.StringToHash("PickUp");

    void Awake()
    {
        inventory = GetComponent<BawangInventory>();
        health = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput != null ? playerInput.actions["Interact"] : null;
    }

    void Update()
    {
        if (health != null && health.IsDead)
            return;

        if (grabAt > 0f && Time.time >= grabAt)
            CompletePendingGrab();

        bool pressed = interactAction != null
            ? interactAction.WasPressedThisFrame()
            : Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (!pressed)
            return;

        pendingPickup = inventory != null && !inventory.IsFull ? FindFacingPickup() : null;
        grabAt = pendingPickup != null ? Time.time + grabDelay : -1f;

        if (animator != null)
        {
            animator.ResetTrigger(PickUpHash);
            animator.SetTrigger(PickUpHash);
        }
    }

    void CompletePendingGrab()
    {
        grabAt = -1f;

        if (pendingPickup == null || inventory == null)
            return;

        if (!pendingPickup.CanCollectFrom(transform) || inventory.IsFull)
        {
            pendingPickup = null;
            return;
        }

        pendingPickup.CompletePickup(inventory);
        pendingPickup = null;
    }

    BawangPickup FindFacingPickup()
    {
        BawangPickup best = null;
        float bestDist = float.MaxValue;

        foreach (BawangPickup pickup in FindObjectsByType<BawangPickup>(FindObjectsSortMode.None))
        {
            if (!pickup.CanCollectFrom(transform))
                continue;

            float dist = (pickup.transform.position - transform.position).sqrMagnitude;
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            best = pickup;
        }

        return best;
    }
}
