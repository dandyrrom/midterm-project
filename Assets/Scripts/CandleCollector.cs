using UnityEngine;
using UnityEngine.InputSystem;

public class CandleCollector : MonoBehaviour
{
    [Header("Pick Up Timing")]
    [Tooltip("Seconds after E before the candle disappears and HUD updates.")]
    public float grabDelay = 0.85f;

    CandleInventory inventory;
    CandleHUD hud;
    PlayerHealth health;
    Animator animator;
    PlayerInput playerInput;
    InputAction interactAction;
    CandlePickup pendingPickup;
    float grabAt = -1f;

    static readonly int PickUpHash = Animator.StringToHash("PickUp");

    void Awake()
    {
        inventory = GetComponent<CandleInventory>();
        hud = FindFirstObjectByType<CandleHUD>();
        health = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

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

        CandlePickup facingPickup = FindFacingPickup();
        if (facingPickup == null)
            return;

        if (inventory != null && inventory.IsFull)
        {
            hud?.ShowFullFeedback();
            pendingPickup = null;
            grabAt = -1f;
            return;
        }

        pendingPickup = facingPickup;
        grabAt = Time.time + grabDelay;

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

    CandlePickup FindFacingPickup()
    {
        CandlePickup best = null;
        float bestDist = float.MaxValue;

        foreach (CandlePickup pickup in FindObjectsByType<CandlePickup>(FindObjectsSortMode.None))
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
