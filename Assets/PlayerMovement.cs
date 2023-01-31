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
    [SerializeField] LayerMask environmentLayer;
    [SerializeField] float dashCheckRadius = 0.1f;

    [Header("Visuals Settings")]
    [SerializeField] GameObject playerVisuals;
    [SerializeField] [Range(0f, 0.999f)] float squashAmount;
    float timeSinceDash;

    Vector3[] gizmosLocation;


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
                Vector2 dashInputDirection;
                if (movementInput.magnitude > 0.1f)
                {
                    dashInputDirection = movementInput.normalized;
                }
                else
                {
                    dashInputDirection = Vector2.zero;
                }

                //calculate end point
                Vector3 dashDirection = GetRotatedDirectionFromInput(dashInputDirection);

                int hitCount = 0;
                //raycast positive direction
                bool reachedDestination = false;
                Vector3 raycastStart = new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z);
                Vector3 dashOffset = new Vector3(dashDirection.x * dashSpeed * dashTime, 0.0f, dashDirection.z * dashSpeed * dashTime);
                Vector3 endPoint = transform.position + dashOffset + new Vector3(0f, 0.1f, 0f);

                while (!reachedDestination)
                {
                    Vector3 raycastDirection = endPoint - raycastStart;
                    RaycastHit hit;
                    bool hasHit = Physics.Raycast(raycastStart, raycastDirection.normalized, out hit, raycastDirection.magnitude, environmentLayer);
                    Debug.DrawRay(raycastStart, raycastDirection, Color.blue, 1.0f);
                    if(hasHit)
                    {
                        hitCount++;
                        raycastStart = hit.point;
                        reachedDestination = true;
                    }
                    else
                    {
                        reachedDestination = true;
                    }
                }



                StartCoroutine(Dash(dashDirection));
            }
        }
    }

    

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if(gizmosLocation != null)
        {
            for (int i = 0; i < gizmosLocation.Length; i++)
            {
                Gizmos.DrawSphere(gizmosLocation[i], dashCheckRadius);
            }
        }
        
    }

    private void Update()
    {
        movementLocked = isDashing;

        if (!movementLocked)
        {
            //normal movement
            Vector3 moveDirection = GetRotatedDirectionFromInput(movementInput); 
            DoMovement(moveDirection, moveSpeed);
        }
    }

    void DoMovement(Vector3 direction, float speed)
    {
        if (direction.magnitude >= 1f)
        {
            direction = direction.normalized;
        }
        Vector3 desiredVelocity = new Vector3(direction.x * speed, rb.velocity.y, direction.z * speed);
        rb.velocity = desiredVelocity;
    }

    //there is a better way of doing this probably
    Vector3 GetRotatedDirectionFromInput(Vector2 inDirection)
    {
        Vector3 inDirection3D = new Vector3(inDirection.x, 0f, inDirection.y);
        Quaternion rotationAngle = Quaternion.Euler(0f, 45f, 0f);
        Matrix4x4 matrix = Matrix4x4.Rotate(rotationAngle);
        Vector3 outDirection = matrix.MultiplyPoint3x4(inDirection3D);
        return outDirection;
    }


    IEnumerator Dash(Vector3 direction)
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
