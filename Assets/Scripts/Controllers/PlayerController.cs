using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : Singleton<PlayerController>
{
    // Public fields
    
    [SerializeField] private CharacterController charController;
    [SerializeField] private Transform characterTransform;
    [SerializeField] private Renderer characterRenderer;
    [SerializeField] private Animator characterAnimator;

    [SerializeField] private float movementSpeed; 
    [SerializeField] private float jumpPower = 8f;
    [SerializeField] private float jumpCooldown = 1f;
    [SerializeField] private float gravityAcceleration = -19.81f;
    [SerializeField] private float turnSpeed = 1f;
    [SerializeField] private float softLandVelocity = -7f;
    [SerializeField] private float walkspeedAcceleration = 2f;
    [SerializeField] private float runSpeedMultiplier = 2f;
    [SerializeField] private LayerMask groundedMask;

    // Properties

    public Transform CharacterTransform => characterTransform;
    public Renderer CharacterRenderer => characterRenderer;

    // Private fields

    private CameraController cameraController;
    
    private Vector2 movementInput;
    private Vector2 currentDirection;
    private float currentWalkspeed;
    private float speedMultiplier;
    private float timeSinceJump;
    private float lastLandingVelocity;
    private float yVelocity;

    private bool isInitialized;
    private bool isInputEnabled;
    private bool hasLanded;
    private bool isGrounded;

    // Constants
    
    private const float DEFAULT_WALKSPEED = 2f; // 2f is the speed where feet don't slide. Use this to adjust walk animation speed.
    private const float SPEED_RECOVERY_VALUE = 1f;
    private const float GROUNDED_GRAVITY_MULTIPLIER = 0.15f;
    private const float SPHERE__RADIUS_MULTIPLIER = 0.95f;

    // Animation parameters
    
    private readonly int ANIM_WALKSPEED_PARAM = Animator.StringToHash("WalkSpeed");
    private readonly int ANIM_GROUNDED_PARAM = Animator.StringToHash("isGrounded");
    private readonly int ANIM_MOVING_PARAM = Animator.StringToHash("isMoving");
    private readonly int ANIM_LANDED_PARAM = Animator.StringToHash("Landed");
    private readonly int ANIM_HARD_LANDED_PARAM = Animator.StringToHash("HardLanded");
    private readonly int ANIM_JUMP_PARAM = Animator.StringToHash("Jump");
    private readonly int ANIM_DIRECTION_X_PARAM = Animator.StringToHash("directionX");
    private readonly int ANIM_DIRECTION_Y_PARAM = Animator.StringToHash("directionY");

    // PlayerController

    private void Start()
    {
        cameraController = CameraController.Instance;
        currentDirection = new Vector2(transform.forward.x, transform.forward.z);

        isInputEnabled = true;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;
        
        DoMovement();
    }

    public void DoMovement()
    {
        if (speedMultiplier < 1f)
            speedMultiplier += Time.deltaTime * SPEED_RECOVERY_VALUE;

        speedMultiplier = Mathf.Clamp01(speedMultiplier);
        
        CheckGroundedState();

        characterAnimator.SetBool(ANIM_GROUNDED_PARAM, isGrounded);
        
        var horizontalInput = Input.GetAxisRaw("Horizontal");
        var verticalInput =Input.GetAxisRaw("Vertical");

        if (isInputEnabled)
        {
            movementInput = new Vector2(horizontalInput, verticalInput);
        
            if (cameraController.IsInFirstPerson())
            {
                currentDirection = movementInput;
            }
            else
            {
                currentDirection = Vector2.Lerp(currentDirection, movementInput, turnSpeed * Time.deltaTime);
            }
        }
        else
        {
            movementInput = Vector2.zero;
        }

        CalculateMoveDirection(out Vector3 moveDir, horizontalInput, verticalInput);
    
        if (!isInputEnabled)
            moveDir = Vector3.zero;
        
        HandleRun();
        HandleJump();

        var movementVector = moveDir * currentWalkspeed * speedMultiplier * Time.deltaTime;
        movementVector.y = yVelocity * Time.deltaTime;
    
        charController.Move(movementVector);

        AnimateMovement();
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;
        
        isGrounded = charController.isGrounded;
        if (!hasLanded && !isGrounded)
        {
            if (IsLandingNextPhysicsFrame())
            {
                lastLandingVelocity = charController.velocity.y;

                hasLanded = true;
            }
        }
        else if (isGrounded)
        {
            hasLanded = false;
        }
    }

    public void SetInputState(bool state)
    {
        isInputEnabled = state;
    }

    private void CheckGroundedState()
    {
        CheckIfLanded();
        
        if (isGrounded && yVelocity < 0)
        {
            yVelocity = gravityAcceleration * GROUNDED_GRAVITY_MULTIPLIER;
        }
    }

    private void CheckIfLanded()
    {
        if (hasLanded)
        {
            if (IsHardLanding())
            {
                speedMultiplier = 0.1f;
                    
                characterAnimator.SetTrigger(ANIM_HARD_LANDED_PARAM);
            }
            else
            {
                characterAnimator.SetTrigger(ANIM_LANDED_PARAM);
            }
            
            hasLanded = false;
        }
    }

    private bool IsLandingNextPhysicsFrame()
    {
        if (charController.velocity.y > 0f) return false;
        
        var startPosition = characterAnimator.transform.position;
        startPosition.y += charController.radius;
        var dist = Mathf.Abs(charController.velocity.y * Time.fixedDeltaTime);

        return Physics.SphereCast(startPosition, charController.radius * SPHERE__RADIUS_MULTIPLIER, Vector3.down, out RaycastHit hit, dist, groundedMask);
    }

    private bool IsHardLanding()
    {
        return hasLanded && lastLandingVelocity < softLandVelocity;
    }

    private void CalculateMoveDirection(out Vector3 moveDir, float horizontalInput, float verticalInput)
    {
        if (cameraController.IsInFirstPerson())
        {
            moveDir = transform.right * horizontalInput + transform.forward * verticalInput;
        }
        else
        {
            var cameraDirection = cameraController.GetCameraDirectionNormalized();
            var directionRight = Quaternion.AngleAxis(90, Vector3.up) * cameraDirection;
            var goalRotation = Quaternion.LookRotation(cameraDirection, Vector3.up);

            moveDir = directionRight * horizontalInput + cameraDirection * verticalInput;

            if (movementInput.magnitude > 0f)
                transform.rotation = Quaternion.Lerp(transform.rotation, goalRotation, turnSpeed * Time.deltaTime);
        }
        
        moveDir.Normalize();
    }
    
    private void HandleRun()
    {
        if (Input.GetKey(KeyCode.LeftShift) && isInputEnabled)
        {
            var runWalkspeed = movementSpeed * runSpeedMultiplier;
            currentWalkspeed = Mathf.MoveTowards(currentWalkspeed, runWalkspeed, Time.deltaTime * walkspeedAcceleration);
        }
        else
        {
            currentWalkspeed = Mathf.MoveTowards(currentWalkspeed, movementSpeed, Time.deltaTime * walkspeedAcceleration);
        }
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && CanJump())
        {
            timeSinceJump = Time.time;

            yVelocity += jumpPower;
            
            characterAnimator.SetTrigger(ANIM_JUMP_PARAM);
        }

        yVelocity += gravityAcceleration * Time.deltaTime;
    }

    private bool CanJump() => isGrounded && Time.time - timeSinceJump > jumpCooldown && isInputEnabled;

    private void AnimateMovement()
    {
        characterAnimator.SetBool(ANIM_MOVING_PARAM, movementInput.magnitude > 0f);
            
        // Adjust WalkSpeed parameter so that feet don't slide.
        var walkspeed = (currentWalkspeed * speedMultiplier) / DEFAULT_WALKSPEED;
        characterAnimator.SetFloat(ANIM_WALKSPEED_PARAM, walkspeed);
        characterAnimator.SetFloat(ANIM_DIRECTION_X_PARAM, currentDirection.x);
        characterAnimator.SetFloat(ANIM_DIRECTION_Y_PARAM, currentDirection.y);
    }

    public float GetSpawnHeight() => (charController.height / 2f) + charController.skinWidth;
}
