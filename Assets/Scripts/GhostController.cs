using System.Collections.Generic;
using UnityEngine;

public enum GhostState
{
    Normal = 0,
    Scared = 1,
    Recovering = 2,
    Dead = 3
}

public enum GhostBehaviourType
{
    Flee = 0,
    Chase = 1,
    Random = 2,
    Clockwise = 3
}

public class GhostController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D ghostCollider;
    [SerializeField] private GhostBehaviourType behaviourType;
    [SerializeField] private GhostSpawnGateType spawnGatePreference = GhostSpawnGateType.Top;

    public GhostState CurrentState { get; private set; } = GhostState.Normal;
    public bool IsFrozen { get; private set; }

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float defaultAnimatorSpeed = 1f;
    private LevelGenerator levelGenerator;
    private bool hasExitedSpawn;
    private HashSet<Vector2Int> spawnAreaTilesCache = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, int> topGateDistances = new Dictionary<Vector2Int, int>();
    private Dictionary<Vector2Int, int> bottomGateDistances = new Dictionary<Vector2Int, int>();
    private bool spawnDistancesBuilt;
    private Vector2Int currentGridPosition;
    private Vector2Int targetGridPosition;
    private Vector2Int previousGridPosition;
    private Vector3 currentWorldPosition;
    private Vector3 targetWorldPosition;
    private float movementSpeed = 2f;
    private float tileSize = 1f;
    [SerializeField] private int gridWidth = 28;
    [SerializeField] private int gridHeight = 29;
    private float movementStartTime;
    private float movementDuration;
    private bool isMoving;
    private Vector2Int lastMoveDirection = Vector2Int.right;
    private Transform pacStudentTransform;


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

        levelGenerator = FindObjectOfType<LevelGenerator>();
        var pac = FindObjectOfType<PacStudentController>();
        if (pac != null)
        {
            pacStudentTransform = pac.transform;
            tileSize = pac.tileSize;
            gridWidth = pac.gridWidth;
            gridHeight = pac.gridHeight;
            movementSpeed = pac.movementSpeed * 0.9f;
        }
        else
        {
            pacStudentTransform = null;
            tileSize = 1f;
        }

        hasExitedSpawn = false;

        ApplyState(CurrentState);

        currentWorldPosition = transform.position;
        currentGridPosition = WorldToGridPosition(currentWorldPosition);
        targetGridPosition = currentGridPosition;
        previousGridPosition = currentGridPosition;
        targetWorldPosition = currentWorldPosition;
        isMoving = false;

        lastMoveDirection = spawnGatePreference == GhostSpawnGateType.Top ? Vector2Int.up : Vector2Int.down;
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
        hasExitedSpawn = false;
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

    public void MarkExitedSpawn()
    {
        hasExitedSpawn = true;
    }

    public bool HasExitedSpawn()
    {
        return hasExitedSpawn;
    }

    public GhostSpawnGateType GetGatePreference()
    {
        return spawnGatePreference;
    }

    private void Update()
    {
        if (IsFrozen)
            return;

        if (CurrentState == GhostState.Dead)
        {
            UpdateDeadMovement();
        }
        else
        {
            UpdateGridMovement();
        }
    }

    private void UpdateGridMovement()
    {
        if (!isMoving)
        {
            Vector2Int direction = DetermineNextDirection();
            if (direction != Vector2Int.zero)
            {
                Vector2Int nextGrid = currentGridPosition + direction;
                BeginMovement(nextGrid);
            }
        }

        if (isMoving)
        {
            float elapsed = Time.time - movementStartTime;
            float progress = movementDuration > 0f ? elapsed / movementDuration : 1f;

            if (progress >= 1f)
            {
                transform.position = targetWorldPosition;
                currentWorldPosition = targetWorldPosition;
                previousGridPosition = currentGridPosition;
                currentGridPosition = targetGridPosition;
                isMoving = false;
            }
            else
            {
                transform.position = Vector3.Lerp(currentWorldPosition, targetWorldPosition, progress);
            }
        }
    }

    private void UpdateDeadMovement()
    {
        float speed = GetCurrentSpeed();
        transform.position = Vector3.MoveTowards(transform.position, spawnPosition, speed * Time.deltaTime);
        currentWorldPosition = transform.position;
        currentGridPosition = WorldToGridPosition(currentWorldPosition);
    }

    private Vector2Int DetermineNextDirection()
    {
        List<Vector2Int> validDirections = GetValidDirections();
        if (validDirections.Count == 0)
            return Vector2Int.zero;

        if (!hasExitedSpawn && levelGenerator != null && IsInSpawnArea(currentGridPosition))
        {
            var distanceField = GetDistanceFieldForPreference();
            int bestDistance = int.MaxValue;
            Vector2Int bestDir = Vector2Int.zero;

        Vector2Int preferredDir = GetPreferredGateDirection();

        foreach (var dir in validDirections)
        {
            Vector2Int next = currentGridPosition + dir;
            if (distanceField.TryGetValue(next, out int dist))
            {
                if (dist < bestDistance || (dist == bestDistance && dir == preferredDir))
                {
                    bestDistance = dist;
                    bestDir = dir;
                }
            }
        }

            if (bestDir != Vector2Int.zero)
            {
                lastMoveDirection = bestDir;
                return bestDir;
            }
        }

        return ChooseDirection(validDirections);
    }

    private void BeginMovement(Vector2Int gridTarget)
    {
        previousGridPosition = currentGridPosition;
        targetGridPosition = gridTarget;
        targetWorldPosition = GridToWorldPosition(gridTarget);
        currentWorldPosition = transform.position;

        if (CurrentState != GhostState.Dead && !hasExitedSpawn && levelGenerator != null && levelGenerator.IsSpawnGate(gridTarget))
        {
            MarkExitedSpawn();
        }

        float distance = Vector3.Distance(currentWorldPosition, targetWorldPosition);
        float speed = GetCurrentSpeed();
        movementDuration = speed > 0f ? distance / speed : 0.0001f;
        movementStartTime = Time.time;
        isMoving = true;
    }

    private List<Vector2Int> GetValidDirections()
    {
        List<Vector2Int> primary = new List<Vector2Int>();
        List<Vector2Int> fallback = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int reverse = -lastMoveDirection;

        foreach (var dir in directions)
        {
            bool isReverse = dir == reverse;
            Vector2Int next = currentGridPosition + dir;

            if (!IsWithinBounds(next))
                continue;

            bool allow = true;

            if (levelGenerator != null)
            {
                bool isGate = levelGenerator.IsSpawnGate(next);
                if (isGate)
                {
                    if (CurrentState != GhostState.Dead)
                    {
                        var gateType = levelGenerator.GetSpawnGateType(next);
                        if (hasExitedSpawn)
                        {
                            allow = false;
                        }
                        else if (spawnGatePreference != GhostSpawnGateType.None && gateType != spawnGatePreference)
                        {
                            allow = false;
                        }
                    }
                }
                else if (CurrentState != GhostState.Dead)
                {
                    if (!levelGenerator.IsWalkable(next.x, next.y))
                    {
                        allow = false;
                    }
                }
            }

            if (!allow)
                continue;

            if (isReverse)
            {
                fallback.Add(dir);
            }
            else
            {
                primary.Add(dir);
            }
        }

        if (primary.Count > 0)
            return primary;
        return fallback;
    }

    private bool IsWithinBounds(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth && gridPos.y >= 0 && gridPos.y < gridHeight;
    }

    private float GetCurrentSpeed()
    {
        switch (CurrentState)
        {
            case GhostState.Scared:
            case GhostState.Recovering:
            case GhostState.Dead:
                return movementSpeed * 0.5f;
            case GhostState.Normal:
            default:
                return movementSpeed;
        }
    }

    private Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float cx = (gridWidth - 1) * 0.5f;
        float cy = (gridHeight - 1) * 0.5f;
        float x = (gridPos.x - cx) * tileSize;
        float y = -(gridPos.y - cy) * tileSize;
        return new Vector3(x, y, transform.position.z);
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float cx = (gridWidth - 1) * 0.5f;
        float cy = (gridHeight - 1) * 0.5f;
        int gridX = Mathf.RoundToInt(worldPos.x / tileSize + cx);
        int gridY = Mathf.RoundToInt(-worldPos.y / tileSize + cy);
        return new Vector2Int(gridX, gridY);
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
        hasExitedSpawn = false;
    }

    public void EnterDeadState()
    {
        SetFrozen(false);
        SetState(GhostState.Dead);
        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }
        hasExitedSpawn = false;
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
        hasExitedSpawn = false;
    }


    private void EnsurePacStudentReference()
    {
        if (pacStudentTransform == null)
        {
            var pac = FindObjectOfType<PacStudentController>();
            if (pac != null)
            {
                pacStudentTransform = pac.transform;
                tileSize = pac.tileSize;
                gridWidth = pac.gridWidth;
                gridHeight = pac.gridHeight;
                movementSpeed = pac.movementSpeed * 0.9f;
            }
        }
    }

    private Vector3 DirectionToWorldOffset(Vector2Int dir)
    {
        Vector3 current = GridToWorldPosition(currentGridPosition);
        Vector3 next = GridToWorldPosition(currentGridPosition + dir);
        return next - current;
    }

    private void EnsureSpawnAreaData()
    {
        if (spawnDistancesBuilt || levelGenerator == null)
            return;

        spawnAreaTilesCache.Clear();
        spawnAreaTilesCache.UnionWith(levelGenerator.GetSpawnAreaTiles());

        topGateDistances = BuildDistanceField(levelGenerator.GetTopSpawnGates());
        bottomGateDistances = BuildDistanceField(levelGenerator.GetBottomSpawnGates());
        spawnDistancesBuilt = true;
    }

    private Dictionary<Vector2Int, int> BuildDistanceField(IReadOnlyCollection<Vector2Int> gates)
    {
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();
        if (gates == null)
            return distances;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        foreach (var gate in gates)
        {
            distances[gate] = 0;
            queue.Enqueue(gate);
        }

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentDist = distances[current];

            foreach (var dir in dirs)
            {
                var next = current + dir;
                if (!spawnAreaTilesCache.Contains(next))
                    continue;
                if (distances.ContainsKey(next))
                    continue;

                distances[next] = currentDist + 1;
                queue.Enqueue(next);
            }
        }

        return distances;
    }

    private bool IsInSpawnArea(Vector2Int gridPos)
    {
        EnsureSpawnAreaData();
        return spawnAreaTilesCache.Contains(gridPos);
    }

    private Vector2Int GetPreferredGateDirection()
    {
        return spawnGatePreference == GhostSpawnGateType.Top ? Vector2Int.up : Vector2Int.down;
    }

    private Dictionary<Vector2Int, int> GetDistanceFieldForPreference()
    {
        EnsureSpawnAreaData();
        return spawnGatePreference == GhostSpawnGateType.Bottom ? bottomGateDistances : topGateDistances;
    }

    private Vector2Int SelectFleeDirection(List<Vector2Int> validDirections)
    {
        if (validDirections == null || validDirections.Count == 0)
            return lastMoveDirection;

        EnsurePacStudentReference();

        bool isInSpawnArea = !hasExitedSpawn && levelGenerator != null && levelGenerator.IsSpawnGate(currentGridPosition);

        Vector3 currentPos = transform.position;
        Vector3 targetPos = pacStudentTransform != null ? pacStudentTransform.position : currentPos;
        float currentDist = (targetPos - currentPos).sqrMagnitude;

        List<Vector2Int> candidates = new List<Vector2Int>();
        foreach (var dir in validDirections)
        {
            if (isInSpawnArea && dir == (spawnGatePreference == GhostSpawnGateType.Top ? Vector2Int.up : Vector2Int.down))
            {
                candidates.Add(dir);
                continue;
            }

            Vector3 candidatePos = currentPos + DirectionToWorldOffset(dir);
            float candidateDist = (targetPos - candidatePos).sqrMagnitude;
            if (candidateDist >= currentDist - 0.0001f)
            {
                candidates.Add(dir);
            }
        }

        if (candidates.Count == 0)
        {
            candidates = validDirections;
        }

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private GhostBehaviourType GetEffectiveBehaviour()
    {
        if (CurrentState == GhostState.Scared || CurrentState == GhostState.Recovering)
            return GhostBehaviourType.Flee;
        return behaviourType;
    }

    private Vector2Int ChooseDirection(List<Vector2Int> validDirections)
    {
        if (validDirections == null || validDirections.Count == 0)
            return lastMoveDirection;

        Vector2Int chosen;
        switch (GetEffectiveBehaviour())
        {
            case GhostBehaviourType.Flee:
                chosen = SelectFleeDirection(validDirections);
                break;
            case GhostBehaviourType.Chase:
                chosen = SelectChaseDirection(validDirections);
                break;
            case GhostBehaviourType.Clockwise:
                chosen = SelectClockwiseDirection(validDirections);
                break;
            default:
                chosen = validDirections[Random.Range(0, validDirections.Count)];
                break;
        }

        lastMoveDirection = chosen;
        return chosen;
    }


    private Vector2Int SelectChaseDirection(List<Vector2Int> validDirections)
    {
        if (validDirections == null || validDirections.Count == 0)
            return lastMoveDirection;

        EnsurePacStudentReference();
        Vector3 currentPos = transform.position;
        Vector3 targetPos = pacStudentTransform != null ? pacStudentTransform.position : currentPos;
        float currentDist = (targetPos - currentPos).sqrMagnitude;

        List<Vector2Int> candidates = new List<Vector2Int>();
        float bestDist = currentDist;

        foreach (var dir in validDirections)
        {
            Vector3 candidatePos = currentPos + DirectionToWorldOffset(dir);
            float candidateDist = (targetPos - candidatePos).sqrMagnitude;
            if (candidateDist <= bestDist + 0.0001f)
            {
                if (candidateDist < bestDist - 0.0001f)
                {
                    candidates.Clear();
                    bestDist = candidateDist;
                }
                candidates.Add(dir);
            }
        }

        if (candidates.Count == 0)
        {
            candidates = validDirections;
        }

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }


    private Vector2Int SelectClockwiseDirection(List<Vector2Int> validDirections)
    {
        if (validDirections == null || validDirections.Count == 0)
            return lastMoveDirection;

        Vector2Int rightTurn = new Vector2Int(lastMoveDirection.y, -lastMoveDirection.x);
        Vector2Int straight = lastMoveDirection;
        Vector2Int leftTurn = new Vector2Int(-lastMoveDirection.y, lastMoveDirection.x);
        Vector2Int reverse = -lastMoveDirection;

        Vector2Int[] priority = { rightTurn, straight, leftTurn, reverse };

        foreach (var dir in priority)
        {
            if (validDirections.Contains(dir))
            {
                return dir;
            }
        }

        return validDirections[Random.Range(0, validDirections.Count)];
    }


}