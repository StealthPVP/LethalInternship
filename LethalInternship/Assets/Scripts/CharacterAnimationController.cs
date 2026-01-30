using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
public class CharacterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterMovement movement;
    [SerializeField] private float attackRepeatInterval = 0.4f;

    private float nextAttackTime;

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
    }

    private void Update()
    {
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
        animator.SetTrigger(AttackHash);
    }
}
