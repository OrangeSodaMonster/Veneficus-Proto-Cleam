using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class RacerController : MonoBehaviour
{
    [Header("Default Values")]
    [SerializeField] float maxSpeed =  65f;
    [SerializeField] float turningSpeed = 90f;
    [SerializeField] float acceleration = 15f;
    [SerializeField] float groundDeceleration = 30f;
    [SerializeField] float airDeceleration = 15f;
    [SerializeField] float xDeceleration = 300f;
    [SerializeField] float maxReverseSpeed = 20f;
    [SerializeField] float alignDegPerSec = 50f;
    [SerializeField] float rotationUpdateDelta = 0.5f;
    public bool shouldRotate = true;
    [SerializeField] float jumpSpeed = 11f;
    [SerializeField] float maxFallSpeed = 55f;
    [SerializeField] float StickingPower = 100;
    [SerializeField] AnimationCurve StickingCurve;
    public bool isSticking = true;
    [SerializeField] float gravity = 50f;

    [Header("Braking and trusting Values")]
    [SerializeField] float brakingTrustingDeceleration = 30f;
    [SerializeField] float brakingTrustingTurningSpeed = 120f;

    [Header("Braking Values")]
    [SerializeField] float brakingDeceleration = 60f;
    [SerializeField] float brakingTurningSpeed = 160f;

    [Header("Floating Values")]
    [SerializeField] LayerMask ignoreLayers;
    [SerializeField] float floatingHight = .4f;
    [SerializeField] float floatingRayExtra = .1f;

    [Header("Debug UI")]
    [SerializeField] GameObject debugCanvas;
    [SerializeField] bool showDebugCanvas;
    [SerializeField] TextMeshProUGUI currentSpeedText;
    [SerializeField] TextMeshProUGUI isGroundedText;
    [SerializeField] TextMeshProUGUI ySpeedText;
    [SerializeField] TextMeshProUGUI groundNormalText;

    Rigidbody rb;    
    CheckGrounding checkGrounding;
    GetFloorNormal getFloorNormal;

    Vector3 currentVelocity;
    Vector3 addWorldVelocity;
    Vector3 addLocalVelocity;
    bool isGrounded;
    bool wasGroundedLastFrame;
    bool hasLanded;
    bool hasUngrounded;
    float currentSpeed;
    float currentTurningSpeed;

    // rotation Variables

    Quaternion initRot;
    Quaternion endRot;
    Quaternion targetRotation;
    Vector3 targetPosition;
    Quaternion addRotation;
    float currentTime = 0;

    Vector2 directionInput;
    bool braking;
    bool jump1Requested;
    bool jump2Requested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        checkGrounding = GetComponent<CheckGrounding>();
        getFloorNormal = GetComponent<GetFloorNormal>();

        targetPosition = transform.position;
    }

    private void Start()
    {
        //StartCoroutine(UpdateTargetRotation());
    }

    private void Update()
    {
        SendDebugInfo();

        //SlerpRotation(Time.deltaTime);


        //Float();
        //transform.position = targetPosition;
    }

    void FixedUpdate()
    {
        GroundingHandler();
        if (hasLanded) isSticking = true;

        currentTurningSpeed = turningSpeed;
        currentVelocity = rb.velocity;
        addWorldVelocity = Vector3.zero;
        addLocalVelocity = Vector3.zero;

        // On ground
        if (checkGrounding.isGrounded)
        {
            // Apply trust
            addLocalVelocity.z += directionInput.y * acceleration * Time.fixedDeltaTime;
            // Decelerate above maxSpeed
            if ((transform.InverseTransformDirection(currentVelocity).z + addLocalVelocity.z) > maxSpeed)
                currentVelocity = DecelerateInLocalSpace(currentVelocity, "z", maxSpeed, acceleration * 2f, Time.fixedDeltaTime);
            if ((transform.InverseTransformDirection(currentVelocity).z - addLocalVelocity.z) < -Mathf.Abs(maxReverseSpeed))
                currentVelocity = DecelerateInLocalSpace(currentVelocity, "z", maxReverseSpeed, acceleration * 1.5f, Time.fixedDeltaTime);

            // Brake
            if (braking)
            {
                // Braking Trusting
                if (Mathf.Abs(directionInput.y) >= float.Epsilon)
                {
                    currentVelocity = DecelerateInLocalSpace(currentVelocity, "z", 0, brakingTrustingDeceleration, Time.fixedDeltaTime);
                    currentTurningSpeed = brakingTrustingTurningSpeed;
                }
                // Braking only
                else
                {                    
                    currentVelocity = DecelerateInLocalSpace(currentVelocity, "z", 0, brakingDeceleration, Time.fixedDeltaTime);
                    if(Mathf.Abs(transform.InverseTransformDirection(currentVelocity).z) >= acceleration * 1.5f)
                    currentTurningSpeed = brakingTurningSpeed;
                }
            }

            // Decelerate in X on ground
            currentVelocity = DecelerateInLocalSpace(currentVelocity, "x", 0, xDeceleration, Time.fixedDeltaTime);

            // Ground Deaceleration
            if (Mathf.Abs(directionInput.y) <= float.Epsilon)
            {                
                currentVelocity = DecelerateInLocalSpace(currentVelocity, "z", 0, groundDeceleration, Time.fixedDeltaTime);                
            }

            //Jump
            if (jump1Requested)
            {
                isSticking = false;
                addWorldVelocity.y += jumpSpeed;
                jump1Requested = false;
            }

            // Stick - Apply velocity in the local -z to stick to surface
            if (isSticking)
            {
                float speedProportion = currentSpeed/maxSpeed;
                speedProportion = Mathf.Clamp(speedProportion, 0, 1);
                addLocalVelocity.y += -Mathf.Abs(StickingPower) * Time.fixedDeltaTime * StickingCurve.Evaluate(speedProportion);
            }
            
        }
        // On air
        else
        {           
            // Air Deceleration
            currentVelocity = DecelerateInLocalSpace(currentVelocity, "z", 0, airDeceleration, Time.fixedDeltaTime);
            // Decelerate in X on air (use airDeceleration to keep aspect between z and x on falls)
            currentVelocity = DecelerateInLocalSpace(currentVelocity, "x", 0, airDeceleration, Time.fixedDeltaTime);
            // Gravity
            addWorldVelocity.y += -Mathf.Abs(gravity) * Time.fixedDeltaTime;
            currentVelocity.y = Mathf.Clamp(currentVelocity.y, -Mathf.Abs(maxFallSpeed), Mathf.Abs(jumpSpeed) * 2);
        }


        // Apply new velocity
        rb.velocity = currentVelocity + transform.TransformDirection(addLocalVelocity) + addWorldVelocity;

        //Float();
        // Rotate;


        //targetRotation = transform.rotation;

        targetRotation = Quaternion.FromToRotation((transform.rotation*Vector3.up), getFloorNormal.Normal) * transform.rotation;
        //targetRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, alignDegPerSec * Time.fixedDeltaTime);
        targetRotation *= Quaternion.Euler(transform.TransformDirection(0, directionInput.x * currentTurningSpeed * Time.fixedDeltaTime, 0));

        rb.MoveRotation(targetRotation.normalized);
        //Float();
        //rb.MovePosition(targetPosition);
        //transform.Rotate(Vector3.up, directionInput.x * currentTurningSpeed * Time.fixedDeltaTime, Space.Self);
    }

    void GroundingHandler()
    {
        wasGroundedLastFrame = isGrounded;
        isGrounded = checkGrounding.isGrounded;

        if (isGrounded && !wasGroundedLastFrame) hasLanded = true;
        else if (!isGrounded && wasGroundedLastFrame) hasUngrounded = true;
        else
        {
            hasLanded = false;
            hasUngrounded = false;
        }
    }
    IEnumerator UpdateTargetRotation()
    {
        while (shouldRotate)
        {
            initRot = transform.rotation;
            endRot = Quaternion.FromToRotation((transform.rotation*Vector3.up), getFloorNormal.Normal) * transform.rotation;
            currentTime = 0f;

            yield return new WaitForSeconds(rotationUpdateDelta);
        }        
    }

    void SlerpRotation(float deltaTime)
    {
        if (!shouldRotate) return;

        targetRotation = Quaternion.Slerp(initRot, endRot, currentTime/rotationUpdateDelta);
        currentTime += deltaTime;
    }

    private void Float()
    {
        Color color = Color.green;
        Color color2 = Color.magenta;

        Vector3 origin = transform.position + transform.TransformDirection(Vector3.up) * (floatingHight + 0.05f);
        if (Physics.Raycast(origin,
            transform.TransformDirection(Vector3.down),
            out RaycastHit hit,
            floatingHight + floatingRayExtra + 0.05f,
            ~ignoreLayers))
        {
            targetPosition = transform.TransformDirection(transform.position);
            targetPosition.y = transform.TransformDirection(hit.point).y;
            targetPosition = transform.InverseTransformDirection(targetPosition);
            //color = Color.cyan;
        }
        //else if (Physics.Raycast(origin,
        //    transform.TransformDirection(Vector3.down),
        //    out RaycastHit hit2,
        //    floatingHight + floatingRayExtra + 0.05f))
        //{
        //    color = Color.cyan;
        //    //targetPosition = origin + transform.TransformDirection(Vector3.down) * (floatingHight + 0.05f);
        //}
        Debug.DrawRay(origin + transform.TransformDirection(Vector3.left) * 0.02f, transform.TransformDirection(Vector3.down) * (floatingHight + 0.05f), color);
        Debug.DrawRay(origin - transform.TransformDirection(Vector3.left) * 0.02f, transform.TransformDirection(Vector3.down) * (floatingHight + 0.05f + floatingRayExtra), color2);
        Debug.DrawLine(origin + transform.TransformDirection(Vector3.down) * (floatingHight + 0.05f + floatingRayExtra), origin + transform.TransformDirection(Vector3.down) * (floatingHight + 0.05f + floatingRayExtra + .5f), Color.blue);
            
    }    
  
    /// <summary>
    /// Return Vector3 after deceleration, use minuscule on axis (x, y, z)
    /// </summary>
    Vector3 DecelerateInLocalSpace(Vector3 worldVelocity, string axisMinuscule, float minValue, float ratePerSec, float deltaTime)
    {
        Vector3 currentLocalVelocity = transform.InverseTransformDirection(worldVelocity);
        if (axisMinuscule == "x")
            currentLocalVelocity.x = DecelerateReduceValue(currentLocalVelocity.x, minValue, ratePerSec, deltaTime);
        else if (axisMinuscule == "y")
            currentLocalVelocity.y = DecelerateReduceValue(currentLocalVelocity.y, minValue, ratePerSec, deltaTime);
        else if (axisMinuscule == "z")
            currentLocalVelocity.z = DecelerateReduceValue(currentLocalVelocity.z, minValue, ratePerSec, deltaTime);
        else
            Debug.Log("Unacceptable axis: " + axisMinuscule + " rate: " + ratePerSec.ToString());
        worldVelocity = transform.TransformDirection(currentLocalVelocity);
        return worldVelocity;
    }

    /// <summary>
    /// Return valueToDecelerate after deceleration at a rate per time with a min value especified
    /// </summary>
    float DecelerateReduceValue(float valueToDecelerate, float minValue, float ratePerSec, float deltaTime)
    {
        float newValue = valueToDecelerate - ratePerSec * deltaTime * Mathf.Sign(valueToDecelerate);
        if (newValue < minValue && Mathf.Sign(valueToDecelerate) > 0 || newValue > minValue && Mathf.Sign(valueToDecelerate) < 0)
            newValue = minValue;
        //Debug.Log(valueToDecelerate + " -> " + newValue);
        return newValue;
    }

    public void GetInputs(RacerInputs inputs)
    {
        directionInput = inputs.directionInput;
        braking = inputs.braking;

        if (checkGrounding.isGrounded)
        {
            if (inputs.jump1Triggered) jump1Requested = true;
            if (inputs.jump2Triggered) jump2Requested = true;
        }
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.grey;
    //    if (Physics.Raycast(transform.position + transform.TransformDirection(Vector3.up) * (floatingHight + 0.05f),
    //        transform.TransformDirection(Vector3.down),
    //        floatingHight + floatingRayExtra + 0.05f))
    //    {
    //        Gizmos.color = Color.green;
    //    }
    //    Gizmos.DrawRay(transform.position + transform.TransformDirection(Vector3.up) * (floatingHight + 0.05f),
    //        transform.TransformDirection(Vector3.down) * (floatingHight + floatingRayExtra + 0.05f));
    //}

    void SendDebugInfo()
    {
        if (debugCanvas != null)
        {
            if (Input.GetKeyDown(KeyCode.F3)) showDebugCanvas = !showDebugCanvas;
            debugCanvas.SetActive(showDebugCanvas);
        }

        float currentYSpeed;
        if (checkGrounding.isGrounded)
        {
            currentSpeed = rb.velocity.magnitude;
            currentYSpeed = 0f;
        }
        else
        {
            currentSpeed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
            currentYSpeed = rb.velocity.y;
        }
        if (currentSpeedText != null) currentSpeedText.text = "Current Speed: " + currentSpeed.ToString("F2");
        if (ySpeedText != null) ySpeedText.text = "Y Speed: " + currentYSpeed.ToString("F2");
        if (groundNormalText != null) groundNormalText.text = $"Ground Normal :{getFloorNormal.Normal}";

        if (isGroundedText != null)
        {

            if (checkGrounding.isGrounded)
            {
                isGroundedText.text = "Grounded";
                isGroundedText.color = Color.green;
            }
            else
            {
                isGroundedText.text = "UnGrounded";
                isGroundedText.color = Color.yellow;
            }
        }
    }
}
