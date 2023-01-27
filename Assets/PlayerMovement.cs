using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed;
    [SerializeField] float dashSpeed;
    [SerializeField] float dashTime;
    Vector2 movementInput;
    bool movementLocked;
    bool isDashing;
    Rigidbody rb;
    Collider col;

    [Header("Visuals Settings")]
    [SerializeField] GameObject playerVisuals;
    [SerializeField] [Range(0f, 0.999f)] float squashAmount;
    float timeSinceDash;

    [SerializeField] GameObject testMarker;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        bool triggered = context.action.triggered;
        if (triggered)
        {
            Debug.Log("Attack!");
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!isDashing)
            {
                timeSinceDash = 0f;
                isDashing = true;

                //check dash direction
                Vector2 dashDirection;
                if (movementInput.magnitude > 0.1f)
                {
                    dashDirection = movementInput.normalized;
                }
                else
                {
                    dashDirection = Vector2.zero;
                }

                //calculate end point
                Vector3 rotatedDashDirection = GetRotatedDirection(dashDirection);

                Vector3 dashOffset = new Vector3(rotatedDashDirection.x * dashSpeed * dashTime, 0f, rotatedDashDirection.z * dashSpeed * dashTime);
                Vector3 endPoint = transform.position + dashOffset;
                Instantiate(testMarker, endPoint, Quaternion.identity);

                StartCoroutine(Dash(rotatedDashDirection));
            }
        }
    }

    private void Update()
    {
        movementLocked = isDashing;

        if (!movementLocked)
        {
            DoMovement(GetRotatedDirection(movementInput), moveSpeed);
        }
    }

    void DoMovement(Vector2 direction, float speed)
    {
        Vector3 moveDirection = new Vector3(direction.x, 0f, direction.y);
        if (moveDirection.magnitude >= 1f)
        {
            moveDirection = moveDirection.normalized;
        }

        Vector3 desiredVelocity = new Vector3(moveDirection.x * speed, rb.velocity.y, moveDirection.z * speed);

        rb.velocity = desiredVelocity;
    }

    Vector3 GetRotatedDirection(Vector3 inDirection)
    {
        Quaternion rotationAngle = Quaternion.Euler(0f, 45f, 0f);
        Matrix4x4 matrix = Matrix4x4.Rotate(rotationAngle);
        Vector3 rotatedMoveDirection = matrix.MultiplyPoint3x4(inDirection);
        return rotatedMoveDirection;
    }


    IEnumerator Dash(Vector2 direction)
    {
        float yMin = 1f - squashAmount;
        float xMax = Mathf.Sqrt(1f / yMin);
        col.enabled = false;

        while(isDashing)
        {
            float yScale;
            float horizontalScale;
            if (timeSinceDash >= dashTime)
            {
                //finished dash
                isDashing = false;
                yScale = 1f;
                horizontalScale = 1f;
                col.enabled = true;
            }
            else
            {
                //do little animation
                yScale = Mathf.SmoothStep(yMin, 1f, timeSinceDash / dashTime);
                horizontalScale = Mathf.SmoothStep(xMax, 1f, timeSinceDash / dashTime);

                //move player
                DoMovement(direction, dashSpeed);
            }
            timeSinceDash += Time.deltaTime;
            playerVisuals.transform.localScale = new Vector3(horizontalScale, yScale, horizontalScale);
            yield return null;
        }
        
    }
}
