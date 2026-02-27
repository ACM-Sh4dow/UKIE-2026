using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class ProtagonistController : MonoBehaviour
{
    #region Variables

    [SerializeField] private float stepHeight = 0.3f;

    public Camera camera;
    public GameObject activeCameraIndicator;
    public static ProtagonistController Instance;

    public bool walkingStarted;
    public bool walkingFinished;

    public Coroutine SoundCoroutine;
    public float SecondsBetweenFootsteps;

    public PerspectivePuzzleSolve perspectivePuzzle;

    public Vector3 playerPosition;
    public Quaternion playerRotation;

    public enum MovementState
    {
        Idle,
        Walking,
        Falling
    }
    public MovementState currentState = MovementState.Idle;
    public Vector3 movementDir;

    private static Vector2 movementInput;

    [SerializeField] float movementSpeed;
    [SerializeField] float slowSpeed;
    private static bool slowed = false;
    [SerializeField] float gravity = 20f;
    private const float MaxSlopeAngle = 55f;
    private const int MaxDepth = 3;

    private float capsulePointFromCenter;
    private float capsuleRadius;

    [SerializeField] private float aimSensitivity;
    private float targetYaw;
    private float targetPitch;
    private static Vector2 lookInput;

    private const float LeewayFraction = 0.95f;
    private const float FloatingPointErrorCheck = 0.001f;
    private const float BottomClamp = -85;
    private const float TopClamp = 85;
    private const int RotationOffset = -30;

    private Vector3 velocity;
    private float verticalVelocity; // Separate gravity velocity
    private bool isGrounded;

    private AkAudioListener audioListener;

    #endregion
    #region Initialization

    void Start()
    {
        var playerCollider = GetComponent<CapsuleCollider>();
        capsuleRadius = playerCollider.radius;
        float capsuleHeight = playerCollider.height;
        capsulePointFromCenter = (capsuleHeight / 2) - capsuleRadius;

        Cursor.lockState = CursorLockMode.Locked;
        targetYaw = transform.rotation.eulerAngles.y;

        audioListener = GetComponentInChildren<AkAudioListener>();
    }

    #endregion
    #region Synchronise Input

    public void SyncMovementInput(InputAction.CallbackContext input)
    {
        if (input.started)
        {
            walkingStarted = true;
        }

        movementInput = input.ReadValue<Vector2>();

        if (input.canceled)
        {
            walkingFinished = true;
        }
        else
        {
            walkingFinished = false;
        }
    }

    public void SyncLookInput(Vector2 input)
    {
        lookInput = input;
    }

    #endregion
    #region Step

    private bool TryStepUp(Vector3 moveVelocity, ref Vector3 position)
    {
        if (moveVelocity.magnitude < FloatingPointErrorCheck) return false;

        Vector3 moveDir = new Vector3(moveVelocity.x, 0f, moveVelocity.z).normalized;
        float moveDist = new Vector3(moveVelocity.x, 0f, moveVelocity.z).magnitude;

        // Cast forward from stepHeight above current position to check for a surface to land on
        Vector3 stepCheckOrigin = position + Vector3.up * stepHeight;
        Vector3 point1 = stepCheckOrigin + Vector3.up * capsulePointFromCenter;
        Vector3 point2 = stepCheckOrigin - Vector3.up * capsulePointFromCenter;

        // First, check there's actually something blocking us at foot level
        RaycastHit forwardHit;
        Vector3 footPoint1 = position + Vector3.up * capsulePointFromCenter;
        Vector3 footPoint2 = position - Vector3.up * capsulePointFromCenter;

        if (!Physics.CapsuleCast(footPoint1, footPoint2, capsuleRadius * LeewayFraction,
                moveDir, out forwardHit, moveDist + FloatingPointErrorCheck,
                ~0, QueryTriggerInteraction.Ignore))
        {
            return false; // Nothing blocking, no step needed
        }

        float hitAngle = Vector3.Angle(Vector3.up, forwardHit.normal);
        if (hitAngle <= MaxSlopeAngle) return false; // It's a walkable slope, not a step

        // Now check if the raised capsule can move forward without hitting anything
        if (Physics.CapsuleCast(point1, point2, capsuleRadius * LeewayFraction,
                moveDir, moveDist + FloatingPointErrorCheck,
                ~0, QueryTriggerInteraction.Ignore))
        {
            return false; // Something is blocking even at step height, can't step up
        }

        // Cast downward from the raised forward position to find the step surface
        Vector3 raisedForwardPos = stepCheckOrigin + moveDir * (moveDist + FloatingPointErrorCheck);
        RaycastHit downHit;

        if (!Physics.Raycast(raisedForwardPos, Vector3.down, out downHit,
                stepHeight + FloatingPointErrorCheck, ~0, QueryTriggerInteraction.Ignore))
        {
            return false; // No surface found below
        }

        float stepUp = stepHeight - downHit.distance;
        if (stepUp <= 0) return false; // Surface is not higher than current position

        position.y += stepUp + FloatingPointErrorCheck;
        return true;
    }

    #endregion
    #region Slow

    public static void Slow(InputAction.CallbackContext context)
    {
        if (!context.canceled)
        {
            slowed = true;
        }
        else
        {
            slowed = false;
        }
    }

    #endregion
    #region Collide and Slide

    private Vector3 CollideAndSlide(Vector3 velocity, Vector3 position, Vector3 targetDirection, int depth)
    {
        #region Base Case

        if (depth >= MaxDepth)
        {
            return Vector3.zero;
        }

        #endregion
        #region Logic

        float distance = velocity.magnitude;

        if (distance < FloatingPointErrorCheck)
        {
            return Vector3.zero;
        }

        RaycastHit hit;

        Vector3 point1 = new Vector3(position.x, position.y + capsulePointFromCenter, position.z);
        Vector3 point2 = new Vector3(position.x, position.y - capsulePointFromCenter, position.z);

        LayerMask layerMask = ~0;

        if (Physics.CapsuleCast(
                point1,
                point2,
                capsuleRadius * LeewayFraction,
                velocity.normalized,
                out hit,
                distance,
                layerMask,
                QueryTriggerInteraction.Ignore))
        {
            Vector3 snapToSurface = velocity.normalized * Mathf.Max(0, hit.distance - FloatingPointErrorCheck);
            Vector3 remainder = velocity - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            float scale;

            if (angle <= MaxSlopeAngle)
            {
                scale = 1;
            }
            else
            {
                scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(targetDirection.x, 0, targetDirection.z).normalized);
            }

            remainder = Vector3.ProjectOnPlane(remainder, hit.normal) * scale;

            return snapToSurface + CollideAndSlide(
                remainder,
                position + snapToSurface,
                targetDirection,
                depth + 1);
        }

        #endregion
        #region Default Case

        return velocity;

        #endregion
    }

    #endregion
    #region Ground Check

    private bool CheckGrounded()
    {
        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y - capsulePointFromCenter - (capsuleRadius * 0.5f),
            transform.position.z
        );

        return Physics.CheckSphere(
            spherePosition,
            capsuleRadius * LeewayFraction,
            LayerMask.GetMask("Default")
        );
    }

    #endregion
    #region Look

    private void Look()
    {
        targetYaw += lookInput.x * aimSensitivity * Time.deltaTime;
        targetPitch -= lookInput.y * aimSensitivity * Time.deltaTime;

        targetYaw = ClampAngle(targetYaw, float.MinValue, float.MaxValue);
        targetPitch = ClampAngle(targetPitch, BottomClamp, TopClamp);

        transform.rotation = Quaternion.Euler(
            targetPitch,
            targetYaw + RotationOffset,
            0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    #endregion
    #region Interact

    //public static void Interact()
    //{
    //    float interactionRange = 5;

    //    Ray centerRay = Camera.main.ScreenPointToRay(new Vector3(
    //            Screen.width / 2,
    //            Screen.height / 2,
    //            0f));

    //    if (Physics.Raycast(centerRay, out RaycastHit hitInfo, interactionRange))
    //    {
    //        if (hitInfo.collider.TryGetComponent<InteractionPoint>(out InteractionPoint interaction))
    //        {
    //            interaction.Interact();
    //        }
    //    }
    //}

    #endregion
    #region Perspective Puzzle

    public void GivePuzzle(PerspectivePuzzleSolve puzzle)
    {
        if (!perspectivePuzzle)
        {
            perspectivePuzzle = puzzle;
        }
    }

    #endregion

    #region Audio

    public IEnumerator BeginWalking()
    {
        // Begin walking sfx

        yield return new WaitForSeconds(SecondsBetweenFootsteps);

        SoundCoroutine = StartCoroutine(ContinueWalking());
    }

    public IEnumerator ContinueWalking()
    {
        // Continue Walking sfx

        yield return new WaitForSeconds(SecondsBetweenFootsteps);

        SoundCoroutine = StartCoroutine(ContinueWalking());
    }

    #endregion

    private void Update()
    {
        if (Instance == this)
        {
            activeCameraIndicator.SetActive(true);
            audioListener.enabled = true;
        }
        else
        {
            activeCameraIndicator.SetActive(false);
            audioListener.enabled = false;
            return;
        }

        #region Movement

        Vector3 newMovementInput = new Vector3(movementInput.x, 0, movementInput.y);
        Vector3 moveDirection = Quaternion.Euler(0, camera.transform.eulerAngles.y, 0) * newMovementInput;

        if (slowed)
        {
            velocity = moveDirection * slowSpeed;
        }
        else
        {
            velocity = moveDirection * movementSpeed;
        }
        
        Vector3 intendedVelocity = velocity * Time.deltaTime;

        // Attempt step-up before resolving collisions
        Vector3 positionBeforeStep = transform.position;
        bool steppedUp = isGrounded && TryStepUp(intendedVelocity, ref positionBeforeStep);
        if (steppedUp) transform.position = positionBeforeStep;

        velocity = CollideAndSlide(intendedVelocity, transform.position, moveDirection, 0);
        transform.position += velocity;

        #endregion
        #region Gravity

        isGrounded = CheckGrounded();

        if (!isGrounded)
        {
            currentState = MovementState.Falling;

            velocity = CollideAndSlide(Vector3.down * (gravity * Time.deltaTime), transform.position, Vector3.down, 0);
            transform.position += velocity;
        }

        #endregion
        #region Look

        Look();

        #endregion
        #region Update Player Information

        playerPosition = transform.position;
        playerRotation = transform.rotation;

        #endregion

        #region Animation Information

        if (isGrounded)
        {
            currentState = velocity.magnitude > 0 ? MovementState.Walking : MovementState.Idle;
        }
        movementDir = velocity.normalized;

        #endregion
        #region Audio Information

        if (walkingStarted)
        {
            Debug.Log("Audio: Movement_Start");
            AkUnitySoundEngine.PostEvent("Movement_Start", gameObject);
            walkingStarted = false;
        }

        if (walkingFinished)
        {
            Debug.Log("Audio: Movement_Stop");
            AkUnitySoundEngine.PostEvent("Movement_Stop", gameObject);

            // Finish Walking sfx
            walkingFinished = false;
        }

        #endregion
    }
}