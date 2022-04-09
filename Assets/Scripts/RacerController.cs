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
    [SerializeField] AnimationCurve dragingCurve;
    [SerializeField] float groundDeceleration = 30f;
    [SerializeField] float airDeceleration = 15f;
    [SerializeField] float xDeceleration = 300f;
    [SerializeField] float maxReverseSpeed = 20f;
    [SerializeField] float alignSpeed = 20f;
    [SerializeField] float jumpSpeed = 11f;
    [SerializeField] float maxFallSpeed = 55f;
    [SerializeField] float gravity = 50f;

    [Header("Stiking Down Force")]
    [SerializeField] float stickingPower = 100;
    [SerializeField] float stickingMaxDistance = .1f;
    [SerializeField] AnimationCurve StickingCurve;
    public bool isSticking = true;

    [Header("Braking and trusting Values")]
    [SerializeField] float brakingTrustingDeceleration = 30f;
    [SerializeField] float brakingTrustingTurningSpeed = 120f;

    [Header("Braking Values")]
    [SerializeField] float brakingDeceleration = 60f;
    [SerializeField] float brakingTurningSpeed = 160f;

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

    bool hasLanded;
    bool hasUngrounded;
    float currentSpeed;
    float currentTurningSpeed;

    Quaternion targetRotation;
    float speedProportion;

    Vector2 directionInput;
    bool braking;
    bool jump1Requested;
    bool jump2Requested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        checkGrounding = GetComponent<CheckGrounding>();
        getFloorNormal = GetComponent<GetFloorNormal>();
    }  

    void FixedUpdate()
    {
        // update values
        SendDebugInfo();
        LandingHandler();
        if (!checkGrounding.isGrounded) isSticking = false;
        if (hasLanded) isSticking = true;

        currentSpeed = Vector3.Dot(rb.velocity, transform.forward);
        speedProportion = currentSpeed/maxSpeed;
        currentTurningSpeed = turningSpeed;       

        // On ground
        if (checkGrounding.isGrounded)
        {
            ApplyPropulsion();

            HandleJumping();

            HandleBraking();      

            HandleStickingForce();
        }

        // On air
        else
        {            
            // Gravity
            if (rb.velocity.y > -Mathf.Abs(maxFallSpeed))
                rb.AddForce(gravity * Vector3.down, ForceMode.Acceleration);
        }

        ApplyDeceleration(); //On ground and on air

        HandleRotation(); // must be called after handle braking
    }



    void ApplyDeceleration()
    {
        if (checkGrounding.isGrounded)
        {
            // Ground Deaceleration
            if (Mathf.Abs(directionInput.y) <= float.Epsilon)
                DecelereteInDirection(groundDeceleration, transform.forward, 0);
            // Decelerate in X on ground
            DecelereteInDirection(xDeceleration, transform.right, 0);
        }
        else
        {
            // Air Deceleration
            DecelereteInDirection(airDeceleration, transform.forward, 0);
            //Decelerate in X on air(use airDeceleration to keep aspect between z and x on falls)
            DecelereteInDirection(airDeceleration, transform.right, 0);
        }
    }

    float currentAlignSpeed;
    private void HandleRotation()
    {
        // Align to groundNormals
        currentAlignSpeed = alignSpeed;
        if (checkGrounding.isGrounded)
        {
            targetRotation = Quaternion.FromToRotation((transform.rotation*Vector3.up), getFloorNormal.Normal) * transform.rotation;
        }
        else
        {
            targetRotation = Quaternion.FromToRotation((transform.rotation*Vector3.up), Vector3.up) * transform.rotation;
            currentAlignSpeed /= 5;
        }
        targetRotation = Quaternion.Lerp(rb.rotation, targetRotation, Time.deltaTime * currentAlignSpeed);
        //turn
        targetRotation *= Quaternion.Euler(transform.TransformDirection(0, directionInput.x * currentTurningSpeed * Time.fixedDeltaTime, 0));
        //Apply new rotation
        rb.MoveRotation(targetRotation);
    }

    private void HandleJumping()
    {
        //Jump
        if (jump1Requested)
        {
            isSticking = false;
            rb.AddForce(jumpSpeed * Vector3.up, ForceMode.VelocityChange);
            jump1Requested = false;
        }
    }

    private void HandleBraking()
    {
        if (!braking) return;
        // Braking Trusting
        if (Mathf.Abs(directionInput.y) >= float.Epsilon)
        {
            DecelereteInDirection(brakingTrustingDeceleration, transform.forward, 0);
            if (Mathf.Abs(currentSpeed) >= acceleration * 1.5f)
                currentTurningSpeed = brakingTrustingTurningSpeed;
        }
        // Braking only
        else
        {
            DecelereteInDirection(brakingDeceleration, transform.forward, 0);
            if (Mathf.Abs(currentSpeed) >= acceleration * 1.5f)
                currentTurningSpeed = brakingTurningSpeed;
        }
    }

    float propulsion;
    private void ApplyPropulsion()
    {
        propulsion = 0;
        if (Mathf.Sign(directionInput.y) > 0)
            propulsion = directionInput.y * acceleration * dragingCurve.Evaluate(1 - currentSpeed/maxSpeed);
        else if (Mathf.Sign(directionInput.y) < 0)
            propulsion = directionInput.y * acceleration * dragingCurve.Evaluate(1 - currentSpeed/-Mathf.Abs(maxReverseSpeed));
        if (!braking) rb.AddForce(propulsion * transform.forward, ForceMode.Acceleration);
    }

    Vector3 power;
    private void HandleStickingForce()
    {
        if(!isSticking) return;

        if (Physics.Raycast(transform.position + 0.05f * transform.up, -transform.up, out RaycastHit hit, stickingMaxDistance - 0.05f, ~getFloorNormal.layersIgnored))
        {
            speedProportion = Mathf.Clamp(speedProportion, 0, 1);
            power = -Mathf.Abs(stickingPower) * StickingCurve.Evaluate(speedProportion) * hit.normal.normalized;
            rb.AddForce(power, ForceMode.Acceleration);
        }
        //else Debug.Log("Not Sticking");
    }

    void LandingHandler()
    {
        if (checkGrounding.isGrounded && !checkGrounding.wasGroundedLastFrame) hasLanded = true;
        else if (!checkGrounding.isGrounded && checkGrounding.wasGroundedLastFrame) hasUngrounded = true;
        else
        {
            hasLanded = false;
            hasUngrounded = false;
        }
    }

    void DecelereteInDirection(float ratePerSec, Vector3 direction, float minValue)
    {
        float currentValue = Vector3.Dot(rb.velocity, direction);
        if (currentValue == 0) return;
        float newValue = currentValue - ratePerSec * Time.fixedDeltaTime * Mathf.Sign(currentValue);
        if (newValue < minValue && Mathf.Sign(currentValue) > 0 || newValue > minValue && Mathf.Sign(currentValue) < 0)
            rb.AddForce(-currentValue * direction, ForceMode.VelocityChange);
        else
            rb.AddForce(ratePerSec * direction * -Mathf.Sign(currentValue), ForceMode.Acceleration);
            //rb.AddForce((newValue - currentValue) * direction, ForceMode.VelocityChange);
    }

    //void OnCollisionStay(Collision collision)
    //{
    //    //Eliminate upward force from collisions
    //    if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
    //    {
    //        Vector3 upwardForceFromCollision = Vector3.Dot(collision.impulse, transform.up) * transform.up;
    //        rb.AddForce(-upwardForceFromCollision, ForceMode.Impulse);
    //    }
    //}

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
    //    Gizmos.color = Color.cyan;
    //    Gizmos.DrawRay(transform.position + 0.05f * transform.up, -transform.up * (stickingMaxDistance - 0.05f));
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
            currentYSpeed = 0f;
        }
        else
        {
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
