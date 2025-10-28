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

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
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
}
