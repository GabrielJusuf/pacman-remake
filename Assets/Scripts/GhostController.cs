using UnityEngine;

public enum GhostState
{
    Normal = 0,
    Scared = 1,
    Recovering = 2,
    Dead = 3
}

public class GhostController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D ghostCollider;

    public GhostState CurrentState { get; private set; } = GhostState.Normal;
    public bool IsFrozen { get; private set; }

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float defaultAnimatorSpeed = 1f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (ghostCollider == null)
        {
            ghostCollider = GetComponent<Collider2D>();
        }

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        if (animator != null)
        {
            defaultAnimatorSpeed = animator.speed;
        }

        ApplyState(CurrentState);
    }

    public void SetState(GhostState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        ApplyState(CurrentState);
    }

    public void SetStateIfNotDead(GhostState newState)
    {
        if (CurrentState == GhostState.Dead && newState != GhostState.Dead)
            return;

        SetState(newState);
    }

    private void ApplyState(GhostState state)
    {
        if (animator != null)
        {
            animator.SetInteger("GhostState", (int)state);
        }
    }

    public void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;

        if (animator != null)
        {
            animator.speed = frozen ? 0f : defaultAnimatorSpeed;
        }
    }

    public void ResetToSpawn()
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        SetFrozen(false);
        SetState(GhostState.Normal);
        if (ghostCollider != null)
        {
            ghostCollider.enabled = true;
        }
    }

    public void EnterDeadState()
    {
        SetFrozen(false);
        SetState(GhostState.Dead);
        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }
    }

    public void RespawnToState(GhostState newState)
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        if (ghostCollider != null)
        {
            ghostCollider.enabled = true;
        }
        SetFrozen(false);
        SetState(newState);
    }
}
