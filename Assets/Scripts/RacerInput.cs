using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public struct RacerInputs
{
    public Vector2 directionInput;
    public bool braking;
    public bool fireTriggered;
    public bool fireHolding;
    public bool jump1Triggered;
    public bool jump1Holding;
    public bool jump2Triggered;
    public bool jump2Holding;
    public bool superJumpTriggered;

    public bool basicProjectile;
    public bool shield;
    public bool boost;
    public bool aoeAttack;
}

public class RacerInput : MonoBehaviour
{

    [SerializeField] RacerController racerController;
    RacerInputs inputs;

    bool untriggerJump1;
    bool untriggerJump2;

    bool jump1Started;
    bool jump2Started;

    public void Move(InputAction.CallbackContext context)
    {
        inputs.directionInput = context.ReadValue<Vector2>();
    }

    public void Jump1(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jump1Started = true;            
        }
        if (context.performed && !jump2Started)
        {
            inputs.jump1Triggered = true;
            inputs.jump1Holding = true;
        }
        if (context.canceled)
        {
            inputs.jump1Holding = false;
            jump1Started = false;
        }
    }

    public void Jump2(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jump2Started = true;
        }
        if (context.performed && !jump1Started)
        {
            inputs.jump2Triggered = true;
            inputs.jump2Holding = true;
        }
        if (context.canceled)
        {
            inputs.jump2Holding = false;
            jump2Started = false;
        }
    }

    public void Brake(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            inputs.braking = true;
        }
        if (context.canceled)
        {
            inputs.braking = false;
        }
    }

    private void Update()
    {
        CleanTriggered(ref inputs.jump1Triggered, ref untriggerJump1);
        CleanTriggered(ref inputs.jump2Triggered, ref untriggerJump2);

        racerController.GetInputs(inputs);

        if(jump1Started && jump2Started)
        {
            inputs.superJumpTriggered = true;
        }
        else inputs.superJumpTriggered = false;
    }

    private void CleanTriggered(ref bool triggered, ref bool untrigger)
    {
        if (untrigger)
        {
            triggered = false;
            untrigger = false;
        }
        if (triggered) untrigger = true;
    }
}