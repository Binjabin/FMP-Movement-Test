using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed;
    [SerializeField] float dashSpeed;
    Vector2 movementInput;
    bool movementLocked;
    bool isDashing;
    Rigidbody rb;

    [Header("Visuals Settings")]
    [SerializeField] GameObject playerVisuals;
    [SerializeField] float dashAnimationTime;
    [SerializeField] [Range(0f, 0.999f)] float squashAmount;
    float timeSinceDash;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
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

                if(movementInput.magnitude > 0.1f)
                {
                    StartCoroutine(Dash(movementInput.normalized));
                }
                else
                {
                    StartCoroutine(Dash(Vector2.zero));
                }
            }
        }
    }

    private void Update()
    {
        movementLocked = isDashing;

        if (!movementLocked)
        {
            DoMovement();
        }
    }

    void DoMovement()
    {
        Vector3 moveDirection = new Vector3(movementInput.x, 0f, movementInput.y);

        Quaternion rotationAngle = Quaternion.Euler(0f, 45f, 0f);
        Matrix4x4 matrix = Matrix4x4.Rotate(rotationAngle);
        Vector3 rotatedMoveDirection = matrix.MultiplyPoint3x4(moveDirection);

        Vector3 moveAmount = rotatedMoveDirection * Time.deltaTime * moveSpeed;
        rb.velocity = moveAmount;
    }

    IEnumerator Dash(Vector2 direction)
    {
        float yMin = 1f - squashAmount;
        float xMax = Mathf.Sqrt(1f / yMin);
        

        while(isDashing)
        {
            float yScale;
            float horizontalScale;
            if (timeSinceDash >= dashAnimationTime)
            {
                //finished dash
                isDashing = false;
                yScale = 1f;
                horizontalScale = 1f;
            }
            else
            {
                //do little animation
                yScale = Mathf.SmoothStep(yMin, 1f, timeSinceDash / dashAnimationTime);
                horizontalScale = Mathf.SmoothStep(xMax, 1f, timeSinceDash / dashAnimationTime);

                //move player
                Vector3 moveDirection = new Vector3(direction.x, 0f, direction.y);

                Quaternion rotationAngle = Quaternion.Euler(0f, 45f, 0f);
                Matrix4x4 matrix = Matrix4x4.Rotate(rotationAngle);
                Vector3 rotatedMoveDirection = matrix.MultiplyPoint3x4(moveDirection);

                Vector3 moveAmount = rotatedMoveDirection * Time.deltaTime * dashSpeed;
                rb.velocity = moveAmount;
            }
            timeSinceDash += Time.deltaTime;
            playerVisuals.transform.localScale = new Vector3(horizontalScale, yScale, horizontalScale);
            yield return null;
        }
        
    }
}
