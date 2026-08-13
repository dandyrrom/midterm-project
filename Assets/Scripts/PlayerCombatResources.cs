using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Lives plus a finite lunas pouch. Throwing is the only fight option.
/// </summary>
public class PlayerCombatResources : MonoBehaviour
{
    public static PlayerCombatResources Instance { get; private set; }

    public int lives = 3;
    public int maxLives = 3;
    public int lunas = 6;
    public float throwForce = 14f;
    public float iFrameSeconds = 1.1f;

    InputAction attackAction;
    float iFrameUntil;
    bool dead;

    public bool IsDead => dead;
    public bool CanThrow => !dead && lunas > 0;

    void Awake()
    {
        Instance = this;
        gameObject.tag = "Player";
    }

    void Start()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            attackAction = playerInput.actions["Attack"];
    }

    void Update()
    {
        if (dead)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (attackAction != null && attackAction.WasPressedThisFrame())
            TryThrow();
    }

    public void TryThrow()
    {
        if (!CanThrow)
        {
            SoftSfx.Play(transform.position, 90f, 0.2f);
            return;
        }

        lunas--;
        var spawn = transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Lunas";
        go.transform.position = spawn;
        go.transform.localScale = Vector3.one * 0.28f;
        var renderer = go.GetComponent<Renderer>();
        renderer.material.color = new Color(0.85f, 0.95f, 1f);

        Object.Destroy(go.GetComponent<SphereCollider>());
        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = (transform.forward + Vector3.up * 0.15f).normalized * throwForce;

        go.AddComponent<LunasProjectile>();
        SoftSfx.Play(spawn, 620f, 0.3f);
    }

    public void TakeHit()
    {
        if (dead || Time.time < iFrameUntil)
            return;

        lives--;
        iFrameUntil = Time.time + iFrameSeconds;
        SoftSfx.Play(transform.position, 140f, 0.5f);

        if (lives <= 0)
            Die();
    }

    void Die()
    {
        dead = true;
        lives = 0;
        var motor = GetComponent<ThirdPersonController>();
        if (motor != null)
            motor.enabled = false;
    }
}
