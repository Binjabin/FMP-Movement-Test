using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

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
    [SerializeField] float dashCheckResolution = 0.05f;
    [SerializeField] float playerColliderRadius = 0.25f;

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
                Vector3 dashDirection = GetRotatedDirectionFromInput(dashInputDirection);
                Vector3 totalDashVector = dashDirection * dashSpeed * dashTime;
                //dash as far as possible, so need possible end points in reverse order


                bool foundDashLocation = false;
                float checkValue = 1.0f;
                Debug.Log("Start Dash Check");
                while (!foundDashLocation)
                {
                    if (checkValue <= 0f)
                    {
                        foundDashLocation = true;
                        checkValue = 0f;
                        timeSinceDash = 0f;
                        isDashing = true;
                        foundDashLocation = true;

                        StartCoroutine(Dash(transform.position));
                    }
                    else
                    {
                        Vector3 directionOffset = totalDashVector * checkValue;
                        Vector3 checkOffset = new Vector3(0f, playerColliderRadius + 0.001f, 0f);
                        Vector3 playerLocationWithOffset = transform.position + checkOffset;
                        Vector3 checkLocation = transform.position + checkOffset + directionOffset;
                        Vector3 dashEndLocation = transform.position + directionOffset;

                        //check if it is in a collider
                        //probably a way to have to avoid doing this check for each location
                        //and instead just do this once, and compare the values
                        
                        if (!DashEndsInCollider(playerLocationWithOffset, checkLocation))
                        {
                            //check if near enough to collider

                            Collider[] collidersAtPoint = Physics.OverlapSphere(checkLocation, playerColliderRadius, environmentLayer);

                            //gizmosLocation = dashEndLocation;
                            if (collidersAtPoint.Length == 0)
                            {
                                timeSinceDash = 0f;
                                isDashing = true;
                                foundDashLocation = true;

                                StartCoroutine(Dash(dashEndLocation));
                            }
                        }


                        checkValue -= dashCheckResolution;
                    }
                }
            }
        }
    }

    void OnDrawGizmos()
    {

        Gizmos.color = Color.yellow;
        for (int i = 0; i < gizmosLocation.Length; i++)
        {
            Gizmos.DrawSphere(gizmosLocation[i], 0.1f);
        }
        
        
    }

    //happy accident that made a dash to the top of collider mechanic
    /*
    bool foundDashLocation = false;
    float checkValue = 1.0f;
    while (!foundDashLocation)
    {
        if (checkValue <= 0f)
        {
            foundDashLocation = true;
            checkValue = 0f;
        }

        Vector3 startPosition = transform.position += new Vector3(0f, playerColliderRadius + 0.001f, 0f);
        Vector3 offset = dashDirection * checkValue;
        Vector3 checkLocation = offset + startPosition;

        //check if it is in a collider
        //probably a way to have to avoid doing this check for each location
        //and instead just do this once, and compare the values
        if(!DashEndsInCollider(startPosition, checkLocation))
        {
            //check if near enough to collider
            Collider[] collidersAtPoint = Physics.OverlapSphere(checkLocation, playerColliderRadius, environmentLayer);

            if (collidersAtPoint.Length > 0)
            {
                timeSinceDash = 0f;
                isDashing = true;
                foundDashLocation = true;
                StartCoroutine(Dash(checkLocation));
            }
        }

                    
        checkValue -= dashCheckResolution;
    */


    List<Vector3> GetPossibleDashEndPoints(Vector3 location, Vector3 dashDirection)
    {
        List<Vector3> points = new List<Vector3>();

        bool reachedDestination = false;
        Vector3 raycastStart = new Vector3(location.x, location.y + 0.1f, location.z);
        Vector3 dashOffset = new Vector3(dashDirection.x * dashSpeed * dashTime, 0.0f, dashDirection.z * dashSpeed * dashTime);
        Vector3 endPoint = location + dashOffset + new Vector3(0f, 0.1f, 0f);

        while (!reachedDestination)
        {
            Vector3 raycastDirection = endPoint - raycastStart;
            RaycastHit hit;
            bool hasHit = Physics.Raycast(raycastStart, raycastDirection.normalized, out hit, raycastDirection.magnitude, environmentLayer);

            if (hasHit)
            {
                raycastStart = hit.point + dashOffset.normalized * 0.0001f;
                Vector3 point = hit.point - dashOffset.normalized * 0.0001f;
                points.Add(point);
            }
            else
            {
                reachedDestination = true;
                points.Add(endPoint);
            }
        }
        return points;
    }



    bool DashEndsInCollider(Vector3 location, Vector3 endPoint)
    {
        int hitCount = 0;

        //raycast positive direction
        Vector3 raycastStart = location;
        Vector3 raycastEnd = endPoint;
        bool reachedDestination = false;
        Vector3 dashOffset = raycastEnd - raycastStart;

        while (!reachedDestination)
        {
            Vector3 raycastDirection = raycastEnd - raycastEnd;
            RaycastHit hit;
            bool hasHit = Physics.Raycast(raycastStart, raycastDirection.normalized, out hit, raycastDirection.magnitude, environmentLayer);

            if (hasHit)
            {
                hitCount++;
                raycastStart = hit.point + dashOffset.normalized * 0.0001f;

            }
            else
            {
                reachedDestination = true;
            }
        }

        //raycast backwards
        
        //swap
        raycastStart = endPoint;
        raycastEnd = location;
        reachedDestination = false;
        dashOffset = raycastEnd - raycastStart;

        while (!reachedDestination)
        {
            Vector3 raycastDirection = raycastEnd - raycastStart;
            RaycastHit hit;
            bool hasHit = Physics.Raycast(raycastStart, raycastDirection.normalized, out hit, raycastDirection.magnitude, environmentLayer);
            

            if (hasHit)
            {
                hitCount--;
                
                raycastStart = hit.point + dashOffset.normalized * 0.0001f;
            }
            else
            {
                reachedDestination = true;
            }
        }
        Debug.Log("Raycast check returns " + hitCount);

        return (!(hitCount == 0));
    }

    Vector3 GetRotatedDirectionFromInput(Vector2 inDirection)
    {
        Vector3 inDirection3D = new Vector3(inDirection.x, 0f, inDirection.y);
        Quaternion rotationAngle = Quaternion.Euler(0f, 45f, 0f);
        Matrix4x4 matrix = Matrix4x4.Rotate(rotationAngle);
        Vector3 outDirection = matrix.MultiplyPoint3x4(inDirection3D);
        return outDirection;
    }


    IEnumerator Dash(Vector3 endPoint)
    {
        Vector3 offset = new Vector3(endPoint.x, 0f, endPoint.z) - transform.position;
        Vector3 direction = offset.normalized;

        float finishAfterPercent = offset.magnitude / (dashSpeed * dashTime);
        //dumb solution to occasional clipping, if it works it works
        float finishAfterTime = finishAfterPercent * dashTime - 0.01f;

        float yMin = 1f - squashAmount;
        float xMax = Mathf.Sqrt(1f / yMin);
        col.enabled = false;

        float yScale = 1f;
        float horizontalScale = 1f;

        while (isDashing)
        {
            if (timeSinceDash >= dashTime)
            {
                //finished dash
                isDashing = false;
                yScale = 1f;
                horizontalScale = 1f;
                col.enabled = true;
            }


            else if (timeSinceDash >= finishAfterTime)
            {
                col.enabled = true;
                yScale = Mathf.SmoothStep(yMin, 1f, timeSinceDash / dashTime);
                horizontalScale = Mathf.SmoothStep(xMax, 1f, timeSinceDash / dashTime);
                DoMovement(direction, dashSpeed);
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

}