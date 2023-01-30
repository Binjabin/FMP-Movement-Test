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

                //+ 0.001 on y axis to avoid collision with floor
                float yOffset = dashCheckRadius + 0.001f;

                Vector3 endPointOffset = new Vector3(dashDirection.x * dashSpeed * dashTime, 0f, dashDirection.z * dashSpeed * dashTime);
                
                Vector3 endPoint = transform.position + endPointOffset;
                
                int checkCount = Mathf.FloorToInt(endPointOffset.magnitude / (2f * dashCheckRadius));

                gizmosLocation = new Vector3[checkCount];
                bool lastWasCollide = false;
                for(int i = 0; i < checkCount; i++)
                {
                    Vector3 thisCheckOffset = new Vector3(endPointOffset.x * ((float)i / (float)checkCount), yOffset, endPointOffset.z * ((float)i / (float)checkCount));
                    Vector3 thisCheckLocation = transform.position + thisCheckOffset;
                    Collider[] collidersAtPoint = Physics.OverlapSphere(thisCheckLocation, dashCheckRadius, environmentLayer, QueryTriggerInteraction.Ignore);
                    

                    
                    if (collidersAtPoint.Length != 0)
                    {
                        if(lastWasCollide == false)
                        {
                            Debug.Log(collidersAtPoint[0]);
                        }
                        gizmosLocation[i] = thisCheckLocation;
                        lastWasCollide = true;
                    }
                    else
                    {
                        lastWasCollide = false;
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
