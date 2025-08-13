using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;        // base movement speed
    public float dashSpeed = 14f;       // dash burst speed
    public float dashTime = 0.15f;      // how long a dash lasts
    public float dashCooldown = 0.5f;   // time before next dash

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 facing = Vector2.right; // last non-zero aim for attacks/aim
    private bool isDashing;
    private float dashTimer;
    private float dashCDTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        // track cooldown timers
        if (dashCDTimer > 0) dashCDTimer -= Time.deltaTime;
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;
        }

        // keep a "facing" dir for aiming if moving
        if (moveInput.sqrMagnitude > 0.001f)
            facing = moveInput.normalized;
    }

    private void FixedUpdate()
    {
        // move: normal vs dash
        float speed = isDashing ? dashSpeed : moveSpeed;
        rb.linearVelocity = moveInput * speed;
    }

    // Input System callback (bind to "Move" Vector2)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Input System callback (bind to "Dash" Button)
    public void OnDash(InputValue value)
    {
        if (value.isPressed && !isDashing && dashCDTimer <= 0f)
        {
            isDashing = true;
            dashTimer = dashTime;
            dashCDTimer = dashCooldown;
        }
    }

    // Expose facing so melee/ranged can use it later
    public Vector2 GetFacing() => facing;
}
