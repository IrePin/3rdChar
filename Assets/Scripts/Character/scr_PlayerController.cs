using UnityEngine;
using static scr_Models;

public class scr_PlayerController : MonoBehaviour
{
    Rigidbody characterRigidBody;
    Animator characterAnimator;
    PlayerInputActions playerInputActions;
    [HideInInspector]
    public Vector2 input_Movement;
    [HideInInspector]
    public Vector2 input_View;

    Vector3 playerMovement;

    [Header("Settings")]
    public PlayerSettingsModel settings;
    public bool isTargetMode;

    [Header("Camera")]
    public Transform cameraTarget;
    public scr_CameraController cameraController;

    [Header("Movement")]
    public float movementSpeedOffset = 1;
    public float movementSmoothdamp = 0.3f;
    public bool isWalking;
    [HideInInspector]
    public bool isSprinting;

    private float verticalSpeed;
    private float targetVerticalSpeed;
    private float verticalSpeedVelocity;

    private float horizontalSpeed;
    private float targetHorizontalSpeed;
    private float horizontalSpeedVelocity;

    private Vector3 relativePlayerVelocity;

    [Header("Stats")]
    public PlayerStatsModel playerStats;

    [Header("Gravity")]
    public Transform groundCheck;
    public float gravity = 10;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private Vector3 gravityDirection;

    [Header("Jumping / Falling")]
    private float fallingSpeed;
    private float fallingSpeedPeak;
    public float fallingThreshold;
    public float fallingMovementSpeed;
    public float fallingRunningMovementSpeed;
    private bool jumpingTriggered;
    private bool fallingTriggered;
    public float maxFallingMovement;

    private Vector3 cameraRelativeForward;
    private Vector3 cameraRelativeRight;

    [Header("Combat")]
    public bool isFaceTarget;
    public Transform target;
    [HideInInspector]
    public bool isAtacking;
    public float distanceToTarget = 1;
    public float speedToTarget = 1;
    public float combatCooldown = 2;
    private float currentCombatCooldown;

    private float fire1Timer;

    #region - Awake -

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        characterRigidBody = GetComponent<Rigidbody>();
        characterAnimator = GetComponent<Animator>();

        playerInputActions = new PlayerInputActions();

        playerInputActions.Movement.Movement.performed += x => input_Movement = x.ReadValue<Vector2>();
        playerInputActions.Movement.View.performed += x => input_View = x.ReadValue<Vector2>();

        playerInputActions.Actions.Jump.performed += x => Jump();

        playerInputActions.Actions.WalkingToggle.performed += x => ToggleWalking();
        playerInputActions.Actions.Sprint.performed += x => Sprint();

        playerInputActions.Actions.Fire1.performed += x => Fire1();
        playerInputActions.Actions.Fire1Hold.performed += x => Fire1Hold();

        gravityDirection = Vector3.down;
    }

    #endregion

    #region - Jumping -

    private void Jump()
    {
        if (jumpingTriggered)
        {
            return;
        }

        jumpingTriggered = true;

        if (IsMoving() && IsInputMoving() && !isWalking)
        {
            characterAnimator.SetTrigger("RunningJump");
        }
        else if (IsMoving() && IsInputMoving() && isWalking)
        {
            characterAnimator.SetTrigger("WalkingJump");
        }
        else
        {
            characterAnimator.SetTrigger("Jump");
        }
    }

    public void ApplyJumpForce()
    {
        if (!IsGrounded())
        {
            return;
        }

        characterRigidBody.AddForce(transform.up * settings.JumpingForce, ForceMode.Impulse);
        fallingTriggered = true;
    }

    #endregion

    #region - Sprinting -

    private void Sprint()
    {
        if (!CanSprint())
        {
            return;
        }

        if (playerStats.Stamina > (playerStats.MaxStamina / 4))
        {
            isSprinting = true;
        }
    }

    private bool CanSprint()
    {
        if (isTargetMode)
        {
            return false;
        }

        var sprintFalloff = 0.8f;

        if ((input_Movement.y < 0 ? input_Movement.y * -1 : input_Movement.y) < sprintFalloff && (input_Movement.x < 0 ? input_Movement.x * -1 : input_Movement.x) < sprintFalloff)
        {
            return false;
        }

        return true;
    }

    private void CalculateSprint()
    {
        if (!CanSprint())
        {
            isSprinting = false;
        }

        if (isSprinting)
        {
            if (playerStats.Stamina > 0)
            {
                playerStats.Stamina -= playerStats.StaminaDrain * Time.deltaTime;
            }
            else
            {
                isSprinting = false;
            }

            playerStats.StaminaCurrentDelay = playerStats.StaminaDelay;
        }
        else
        {
            if (playerStats.StaminaCurrentDelay <= 0)
            {
                if (playerStats.Stamina < playerStats.MaxStamina)
                {
                    playerStats.Stamina += playerStats.StaminaRestore * Time.deltaTime;
                }
                else
                {
                    playerStats.Stamina = playerStats.MaxStamina;
                }
            }
            else
            {
                playerStats.StaminaCurrentDelay -= Time.deltaTime;
            }
        }
    }

    #endregion

    #region - Gravity -

    private bool IsGrounded()
    {

        if (Physics.CheckSphere(groundCheck.position, groundDistance, groundMask))
        {
            return true;
        }

        return false;
    }

    private bool IsFalling()
    {
        if (fallingSpeed < fallingThreshold)
        {
            return true;
        }

        return false;
    }

    private void CalculateGravity()
    {
        Physics.gravity = gravityDirection * gravity;
    }

    private void CalculateFalling()
    {
        fallingSpeed = relativePlayerVelocity.y;

        if (fallingSpeed < fallingSpeedPeak && fallingSpeed < -0.1f && (fallingTriggered || jumpingTriggered))
        {
            fallingSpeedPeak = fallingSpeed;
        }

        if ((IsFalling() && !IsGrounded() && !jumpingTriggered && !fallingTriggered) || (jumpingTriggered && !fallingTriggered && !IsGrounded()))
        {
            fallingTriggered = true;
            characterAnimator.SetTrigger("Falling");
        }

        if (fallingTriggered && IsGrounded() && fallingSpeed < -0.1f)
        {
            fallingTriggered = false;
            jumpingTriggered = false;

            if (fallingSpeedPeak < -7)
            {
                characterAnimator.SetTrigger("HardLand");
            }
            else
            {
                characterAnimator.SetTrigger("Land");
            }

            fallingSpeedPeak = 0;
        }
    }

    #endregion

    #region - Movement -

    private void ToggleWalking()
    {
        isWalking = !isWalking;
    }

    public bool IsMoving()
    {
        if (relativePlayerVelocity.x > 0.4f || relativePlayerVelocity.x < -0.4f)
        {
            return true;
        }

        if (relativePlayerVelocity.z > 0.4f || relativePlayerVelocity.z < -0.4f)
        {
            return true;
        }

        return false;
    }

    public bool IsInputMoving()
    {
        if (input_Movement.x > 0.2f || input_Movement.x < -0.2f)
        {
            return true;
        }


        if (input_Movement.y > 0.2f || input_Movement.y < -0.2f)
        {
            return true;
        }

        return false;
    }

    private void Movement()
    {
        characterAnimator.SetBool("IsTargetMode", isTargetMode);

        relativePlayerVelocity = transform.InverseTransformDirection(characterRigidBody.velocity);

        if (isTargetMode)
        {
            if (input_Movement.y > 0)
            {
                targetVerticalSpeed = (isWalking ? settings.WalkingSpeed : settings.RunningSpeed);
            }
            else
            {
                targetVerticalSpeed = (isWalking ? settings.WalkingBackwardSpeed : settings.RunningBackwardSpeed);
            }

            targetHorizontalSpeed = (isWalking ? settings.WalkingStrafingSpeed : settings.RunningStrafingSpeed);

            if (isFaceTarget && target)
            {
                var lookDirection = target.position - transform.position;
                lookDirection.y = 0;

                var currentRotation = transform.rotation;

                transform.LookAt(lookDirection + transform.position, Vector3.up);
                var newRotation = transform.rotation;

                transform.rotation = Quaternion.Lerp(currentRotation, newRotation, settings.CharacterRotationSmoothdamp);

            }
            else
            {
                var currentRotation = transform.rotation;

                var newRotation = currentRotation.eulerAngles;

                newRotation.y = cameraController.targetRotation.y;

                currentRotation = Quaternion.Lerp(currentRotation, Quaternion.Euler(newRotation), settings.CharacterRotationSmoothdamp);

                transform.rotation = currentRotation;
            }

        }
        else
        {
            var orginalRotation = transform.rotation;
            transform.LookAt(playerMovement + transform.position, Vector3.up);
            var newRotation = transform.rotation;

            transform.rotation = Quaternion.Lerp(orginalRotation, newRotation, settings.CharacterRotationSmoothdamp);

            float playerSpeed = 0;

            if (isSprinting)
            {
                playerSpeed = settings.SprintingSpeed;
            }
            else
            {
                playerSpeed = (isWalking ? settings.WalkingSpeed : settings.RunningSpeed);
            }

            targetVerticalSpeed = playerSpeed;
            targetHorizontalSpeed = playerSpeed;
        }

        targetVerticalSpeed = (targetVerticalSpeed * movementSpeedOffset) * input_Movement.y;
        targetHorizontalSpeed = (targetHorizontalSpeed * movementSpeedOffset) * input_Movement.x;

        verticalSpeed = Mathf.SmoothDamp(verticalSpeed, targetVerticalSpeed, ref verticalSpeedVelocity, movementSmoothdamp);
        horizontalSpeed = Mathf.SmoothDamp(horizontalSpeed, targetHorizontalSpeed, ref horizontalSpeedVelocity, movementSmoothdamp);

        if (isTargetMode)
        {
            var relativeMovement = transform.InverseTransformDirection(playerMovement);

            characterAnimator.SetFloat("Vertical", relativeMovement.z);
            characterAnimator.SetFloat("Horizontal", relativeMovement.x);
        }
        else
        {
            float verticalActualSpeed = verticalSpeed < 0 ? verticalSpeed * -1 : verticalSpeed;
            float horizontalActualSpeed = horizontalSpeed < 0 ? horizontalSpeed * -1 : horizontalSpeed;

            float animatorVertical = verticalActualSpeed > horizontalActualSpeed ? verticalActualSpeed : horizontalActualSpeed;

            characterAnimator.SetFloat("Vertical", animatorVertical);
        }

        if (IsInputMoving())
        {
            cameraRelativeForward = cameraController.transform.forward;
            cameraRelativeRight = cameraController.transform.right;
        }

         playerMovement = cameraRelativeForward * verticalSpeed;
         playerMovement += cameraRelativeRight * horizontalSpeed;



        //if (IsInputMoving())
        //{
        //    playerMovement = cameraController.transform.forward * verticalSpeed;
        //    playerMovement += cameraController.transform.right * horizontalSpeed;
        //}
        //else if (!IsInputMoving())
        //{
        //    playerMovement = Vector3.zero;
        //    characterRigidBody.freezeRotation = true;
        //}

        if (jumpingTriggered || IsFalling())
        {
            characterAnimator.applyRootMotion = false;
            characterRigidBody.AddForce(playerMovement * (isWalking ? fallingMovementSpeed : fallingRunningMovementSpeed));
        }
        else
        {
            characterAnimator.applyRootMotion = true;
        }

        if (isAtacking && IsGrounded() && isTargetMode)
        {
            isFaceTarget = true;

            if(Vector3.Distance(transform.position, target.transform.position) > distanceToTarget)
            {
                characterRigidBody.AddRelativeForce(Vector3.forward * speedToTarget, ForceMode.Force);
            }
        }
        //else
        //{
        //    isFaceTarget = false;
        //}
    }

    #endregion

    #region - Combat -

    public void Fire1()
    {
       

        if (!isAtacking && IsGrounded() && !jumpingTriggered)
        {
            if (fire1Timer <= 0)
            {
                fire1Timer = 0.4f;
                return;
            }

            StartAtacking();
            characterAnimator.SetTrigger("MeleePunch1");
            Debug.Log("fire");
        }
    }
    public void Fire1Hold()
    {
        if (IsGrounded() && !jumpingTriggered)
        {
            StartAtacking();
            characterAnimator.SetTrigger("MeleeHook1");
            Debug.Log("fire");
        }
    }

    public void CalculateCombat()
    {
        if(fire1Timer >= 0)
        {
            fire1Timer -= Time.deltaTime;
        }

        if(currentCombatCooldown < 0)
        {
            currentCombatCooldown -= Time.deltaTime;
        }
        else
        {
            isTargetMode = false;
        }

        if (IsFalling())
        {
            isTargetMode = false;
            isAtacking = false;
        }

    }

    #endregion

    #region - Events -

    public void StartAtacking()
    {
        isAtacking = true;
        isTargetMode = true;
    }

    public void FinishAtacking()
    {
        currentCombatCooldown = combatCooldown;
        isAtacking = false;
    }

    #endregion

    #region - Update -

    private void Update()
    {
        CalculateGravity();
        CalculateFalling();
        Movement();
        CalculateSprint();
        CalculateCombat();
    }

    #endregion

    #region - Enable/Disable -

    private void OnEnable()
    {
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    #endregion

    #region - Gizmos -

    private void OnDrawGizmosSelected()
    {
        //Gizmos.color = Color.red;
        //Gizmos.DrawSphere(transform.position, 0.2f);
    }

    #endregion
}
