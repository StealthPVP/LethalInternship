// Drives animator parameters and attack trigger from movement/input.
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterMovement))]
public class CharacterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterMovement movement;
    [SerializeField] private float attackRepeatInterval = 0.4f;
    [SerializeField] private Camera attackCamera;
    [SerializeField] private LayerMask aimLayers = ~0;
    [SerializeField] private float aimRayDistance = 500f;
    [SerializeField] private bool smoothAttackRotation = true;
    [SerializeField] private float attackRotationSpeed = 12f;

    private float nextAttackTime;
    private NetworkObject networkObject;
    private bool hasNetworkObject;

    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int IsIdleHash = Animator.StringToHash("isIdle");
    private static readonly int IsFallingHash = Animator.StringToHash("isFalling");
    private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        if (movement == null)
        {
            movement = GetComponent<CharacterMovement>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        networkObject = GetComponent<NetworkObject>();
        hasNetworkObject = networkObject != null;
    }

    private void Update()
    {
        if (hasNetworkObject && !networkObject.IsOwner)
        {
            return;
        }

        if (animator == null || movement == null)
        {
            return;
        }

        UpdateLocomotion();
        UpdateAttack();
    }

    private void UpdateLocomotion()
    {
        bool grounded = movement.IsGrounded;
        bool moving = movement.IsMoving && grounded;

        animator.SetBool(IsRunningHash, moving && movement.IsRunning);
        animator.SetBool(IsWalkingHash, moving && movement.IsWalking);
        animator.SetBool(IsIdleHash, grounded && !movement.IsMoving);
        animator.SetBool(IsJumpingHash, movement.IsJumping);
        animator.SetBool(IsFallingHash, movement.IsFalling);
    }

    private void UpdateAttack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TriggerAttack();
            nextAttackTime = Time.time + attackRepeatInterval;
            return;
        }

        if (Input.GetMouseButton(0) && Time.time >= nextAttackTime)
        {
            TriggerAttack();
            nextAttackTime = Time.time + attackRepeatInterval;
        }
    }

    private void TriggerAttack()
    {
        RotateTowardMouse();
        animator.SetTrigger(AttackHash);
    }

    private void RotateTowardMouse()
    {
        if (movement == null)
        {
            return;
        }

        Camera cam = attackCamera != null ? attackCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, aimRayDistance, aimLayers, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }
        else
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!plane.Raycast(ray, out float enter))
            {
                return;
            }

            targetPoint = ray.GetPoint(enter);
        }

        Vector3 direction = targetPoint - transform.position;
        if (smoothAttackRotation)
        {
            movement.FaceDirection(direction, false, attackRotationSpeed);
        }
        else
        {
            movement.FaceDirection(direction, true);
        }
    }
}
