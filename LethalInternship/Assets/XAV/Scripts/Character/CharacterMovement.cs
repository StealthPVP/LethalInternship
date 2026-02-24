// Handles local character locomotion, jump, gravity, and rotation.
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4.5f;

    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGraceTime = 0.12f;
    [SerializeField] private float fallingVelocityThreshold = -1f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Input")]
    [SerializeField] private bool allowWASD = true;
    [SerializeField] private bool useCameraRelative = true;
    [SerializeField] private Transform cameraTransform;

    [Header("Camera-Relative Behavior")]
    [SerializeField] private bool horizontalOnlyTurnsInPlace = true;
    [SerializeField] private float turnInPlaceSpeed = 240f;

    private CharacterController controller;
    private NetworkObject networkObject;
    private bool hasNetworkObject;
    private float verticalVelocity;
    private float lastGroundedTime;

    public Vector2 MoveInput { get; private set; }
    public Vector3 MoveDirection { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsWalking { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsFalling { get; private set; }
    public float VerticalVelocity => verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        networkObject = GetComponent<NetworkObject>();
        hasNetworkObject = networkObject != null;
        lastGroundedTime = -999f;
    }

    private void Update()
    {
        if (hasNetworkObject && !networkObject.IsOwner)
        {
            return;
        }

        ReadMoveInput();
        UpdateMovement();
        UpdateStateFlags();
    }

    private void ReadMoveInput()
    {
        bool right = Input.GetKey(KeyCode.D);
        bool left = Input.GetKey(KeyCode.Q) || (allowWASD && Input.GetKey(KeyCode.A));
        bool forward = Input.GetKey(KeyCode.Z) || (allowWASD && Input.GetKey(KeyCode.W));
        bool back = Input.GetKey(KeyCode.S);

        float x = (right ? 1f : 0f) + (left ? -1f : 0f);
        float y = (forward ? 1f : 0f) + (back ? -1f : 0f);

        MoveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    private void UpdateMovement()
    {
        Vector3 moveDir = GetMoveDirection(MoveInput);
        bool turningInPlace = ShouldTurnInPlace(MoveInput);
        if (turningInPlace)
        {
            float yawStep = MoveInput.x * turnInPlaceSpeed * Time.deltaTime;
            transform.Rotate(0f, yawStep, 0f, Space.World);
            moveDir = Vector3.zero;
        }

        bool walkHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        IsMoving = moveDir.sqrMagnitude > 0.01f;
        IsWalking = IsMoving && walkHeld;
        IsRunning = IsMoving && !walkHeld;

        MoveDirection = moveDir;

        float targetSpeed = IsMoving ? (IsWalking ? walkSpeed : runSpeed) : 0f;
        Vector3 horizontalVelocity = moveDir * targetSpeed;

        bool wasGrounded = controller.isGrounded;
        if (wasGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (wasGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        IsGrounded = controller.isGrounded;
        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
        }

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateStateFlags()
    {
        IsJumping = !IsGrounded && verticalVelocity > 0.1f;

        bool recentlyGrounded = Time.time - lastGroundedTime <= groundedGraceTime;
        IsFalling = !recentlyGrounded && verticalVelocity <= fallingVelocityThreshold;
    }

    public void FaceDirection(Vector3 direction, bool instant = true, float customRotationSpeed = -1f)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        float speed = customRotationSpeed > 0f ? customRotationSpeed : rotationSpeed;
        transform.rotation = instant
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
    }

    public void SetCameraTransform(Transform newCameraTransform)
    {
        cameraTransform = newCameraTransform;
    }

    private Vector3 GetMoveDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (useCameraRelative && cameraTransform != null)
        {
            forward = cameraTransform.forward;
            right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
        }

        Vector3 direction = right * input.x + forward * input.y;
        direction.Normalize();
        return direction;
    }

    private bool ShouldTurnInPlace(Vector2 input)
    {
        if (!useCameraRelative || !horizontalOnlyTurnsInPlace)
        {
            return false;
        }

        bool horizontalPressed = Mathf.Abs(input.x) > 0.01f;
        bool forwardPressed = Mathf.Abs(input.y) > 0.01f;
        return horizontalPressed && !forwardPressed;
    }
}
