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
    }
}
