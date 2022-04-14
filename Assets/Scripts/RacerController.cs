using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class RacerController : MonoBehaviour
{
    [Header("Default Values")]
    [SerializeField] float maxSpeed =  65f;
    [SerializeField] float acceleration = 15f;
    [SerializeField] float turningSpeed = 90f;
    [SerializeField] AnimationCurve dragingCurve;
    [SerializeField] float groundDeceleration = 30f;
    [SerializeField] float airDeceleration = 15f;
    [SerializeField] float xDeceleration = 300f;
    [SerializeField] float maxReverseSpeed = 20f;
    [SerializeField] float alignSpeed = 20f;
    [SerializeField] float jumpSpeed = 11f;
    [SerializeField] float superJumpSpeed = 20f;
    [SerializeField] float maxFallSpeed = 55f;
    [SerializeField] float gravity = 50f;

    [Header("Drifting Values")]
    [SerializeField] float driftingMaxSpeed = 72f;
    [SerializeField] float driftingAcceleration = 17f;
    [SerializeField] float driftingTurningSpeed = 60f;
    [SerializeField] ParticleSystem particles;

    [Header("Braking and trusting Values")]
    [SerializeField] float brakingTrustingDeceleration = 30f;
    [SerializeField] float brakingTrustingTurningSpeed = 120f;

    [Header("Braking Values")]
    [SerializeField] float brakingDeceleration = 60f;
    [SerializeField] float brakingTurningSpeed = 160f;

    [Header("Stiking Down Force")]
    [SerializeField] float stickingPower = 100;
    [SerializeField] float stickingMaxDistance = .1f;
    [SerializeField] AnimationCurve StickingCurve;
    public bool isSticking = true;

    [Header("Debug UI")]
    [SerializeField] GameObject debugCanvas;
    [SerializeField] bool showDebugCanvas;
    [SerializeField] TextMeshProUGUI currentSpeedText;
    [SerializeField] TextMeshProUGUI isGroundedText;
    [SerializeField] TextMeshProUGUI ySpeedText;
    [SerializeField] TextMeshProUGUI groundNormalText;
    [SerializeField] TextMeshProUGUI isDriftingText;

    Rigidbody rb;    
    CheckGrounding checkGrounding;
    GetFloorNormal getFloorNormal;

    bool hasLanded;
    bool hasUngrounded;
    bool isDrifting;
    bool ableToDrift1;
    bool ableToDrift2;
    float currentSpeed;
    float currentAcceleration;
    float currentMaxSpeed;
    float currentTurningSpeed;

    Quaternion targetRotation;
    float speedProportion;

    Vector2 directionInput;
    bool braking;
    bool jump1Requested;
    bool jump1Holding;
    bool jump2Requested;
    bool jump2Holding;
    bool superJumpRequested;
    bool driftingBoostRequested;

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
        currentMaxSpeed = maxSpeed;
        currentAcceleration = acceleration;
        currentTurningSpeed = turningSpeed;

        HandleDrifting();

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

    private void HandleDrifting()
    {        
        if ((jump1Holding || jump2Holding) && directionInput.y > 0.9f && !braking && !hasUngrounded)
        {
            if (Mathf.Abs(directionInput.x) > 0.9f && hasLanded && (jump1Holding && ableToDrift1 || jump2Holding && ableToDrift2))
            {
                isDrifting = true;
                ableToDrift1 = false;
                ableToDrift2 = false;
            }
        }
        else
            isDrifting = false;


        var partEmission = particles.emission;
        if (isDrifting)
        {            
            partEmission.enabled = true;
            currentAcceleration = driftingAcceleration;
            currentMaxSpeed = driftingMaxSpeed;
            currentTurningSpeed = driftingTurningSpeed;
        }
        else
        {
            partEmission.enabled = false;
        }        
    }

    void ApplyDeceleration()
    {
        if (checkGrounding.isGrounded)
        {
            // Ground Deaceleration
            if (Mathf.Abs(directionInput.y) <= float.Epsilon && !braking)
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
        if (superJumpRequested)
        {
            isSticking = false;
            rb.AddForce(superJumpSpeed * Vector3.up, ForceMode.VelocityChange);
            superJumpRequested = false;
        }
        else if (jump1Requested)
        {
            isSticking = false;
            rb.AddForce(jumpSpeed * Vector3.up, ForceMode.VelocityChange);
            jump1Requested = false;
            ableToDrift1 = true;
        }
        else if (jump2Requested)
        {
            isSticking = false;
            rb.AddForce(jumpSpeed * Vector3.up, ForceMode.VelocityChange);
            jump2Requested = false;
            ableToDrift2 = true;
        }

    }

    bool directionBrake;
    private void HandleBraking()
    {
        if (Math.Sign(currentSpeed) != directionInput.y && Mathf.Abs(directionInput.y) >= float.Epsilon) directionBrake = true;
        else directionBrake = false;

        if (!braking && !directionBrake) return;
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
            propulsion = directionInput.y * currentAcceleration * dragingCurve.Evaluate(1 - currentSpeed/currentMaxSpeed);
        else if (Mathf.Sign(directionInput.y) < 0)
            propulsion = directionInput.y * currentAcceleration * dragingCurve.Evaluate(1 - currentSpeed/-Mathf.Abs(maxReverseSpeed));
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

    float speedBeforeDecelerating;
    //float speedAfterDecelerating;
    void DecelereteInDirection(float ratePerSec, Vector3 direction, float minValue)
    {
        speedBeforeDecelerating = Vector3.Dot(rb.velocity, direction);
        if (speedBeforeDecelerating == 0) return;
        //speedAfterDecelerating = speedBeforeDecelerating - ratePerSec * Time.fixedDeltaTime * Mathf.Sign(speedBeforeDecelerating);

        //if (speedAfterDecelerating < minValue && Mathf.Sign(speedBeforeDecelerating) > 0 || speedAfterDecelerating > minValue && Mathf.Sign(speedBeforeDecelerating) < 0)
        if (Mathf.Abs(speedBeforeDecelerating) <= ratePerSec * Time.fixedDeltaTime)
            rb.AddForce(-speedBeforeDecelerating * direction, ForceMode.VelocityChange);
        else
            rb.AddForce(ratePerSec * direction * -Mathf.Sign(speedBeforeDecelerating), ForceMode.Acceleration);
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

        jump1Holding = inputs.jump1Holding;
        jump2Holding = inputs.jump2Holding;
        if(!isDrifting)
            superJumpRequested = inputs.superJumpTriggered;
        else
            driftingBoostRequested = inputs.superJumpTriggered;
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
        if (isDriftingText != null)
        {
            if (isDrifting)
            {
                isDriftingText.text = "Drifting";
                isDriftingText.color = new Color(1, .35f, 1, 1);
            }
            else
            {
                isDriftingText.text = "Normal";
                isDriftingText.color = Color.gray;
            }
        }
    }
}
