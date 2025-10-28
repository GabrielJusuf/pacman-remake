using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float tileSize = 1.0f;
    public float movementSpeed = 2.0f;
    
    [Header("Grid Settings")]
    public int gridWidth = 28;
    public int gridHeight = 29;
    
    [Header("Animation Settings")]
    public Animator animator;
    
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip movementAudio;
    public AudioClip pelletEatingAudio;
    [SerializeField] private AudioClip wallCollisionAudio;
    
    [Header("Collision Feedback")]
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private ParticleSystem wallCollisionParticles;
    [SerializeField] private Vector3 wallCollisionEffectOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private string wallCollisionParticleSortingLayer = "Characters";
    [SerializeField] private int wallCollisionParticleSortingOrder = 50;
    [SerializeField] private float wallCollisionEffectCooldown = 0.1f;
    [SerializeField] private ParticleSystem movementTrail;

    [Header("Death Settings")]
    [SerializeField] private string deathAnimationTrigger = "Die";
    [SerializeField] private ParticleSystem deathParticles;

    private struct TeleporterMapping
    {
        public Vector2Int entry;
        public Vector2Int exit;
        public Vector2Int requiredDirection;
    }

    private TeleporterMapping[] teleporters;
    
    // Grid position (in grid coordinates)
    private Vector2Int currentGridPosition;
    private Vector2Int targetGridPosition;
    
    // World position (in world coordinates)
    private Vector3 currentWorldPosition;
    private Vector3 targetWorldPosition;
    private Vector3 previousWorldPosition;
    private Vector2Int previousGridPosition;
    
    // Movement state
    private bool isMoving = false;
    private float movementStartTime;
    private float movementDuration;
    
    // Input handling
    private Vector2Int lastInput = Vector2Int.zero;
    private Vector2Int currentInput = Vector2Int.zero;
    
    // Animation and audio state
    private bool isPlayingMovementAudio = false;
    private bool isPlayingPelletAudio = false;
    private bool hasPlayedMidpointAudio = false;
    
    private Vector2Int lastFacingDirection = Vector2Int.right;
    private Vector2Int currentAnimationDirection = Vector2Int.zero;
    private Tweener tweener;
    private LevelGenerator levelGenerator;
    private GameManager gameManager;
    private CherryController cherryController;
    private float lastWallCollisionTime = -10f;
    private bool controlsEnabled = true;
    private bool isInDeathSequence = false;
    private Vector2Int spawnGridPosition;

    void Start()
    {
        tweener = FindObjectOfType<Tweener>();
        if (tweener == null)
        {
            GameObject tweenerObj = new GameObject("Tweener");
            tweener = tweenerObj.AddComponent<Tweener>();
        }
        
        levelGenerator = FindObjectOfType<LevelGenerator>();
        gameManager = FindObjectOfType<GameManager>();
        cherryController = FindObjectOfType<CherryController>();
        
        // Initialise animator and audio source
        if (animator == null)
            animator = GetComponent<Animator>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Initialise grid position to top-left walkable area
        currentGridPosition = new Vector2Int(1, 1);
        targetGridPosition = currentGridPosition;
        previousGridPosition = currentGridPosition;
        spawnGridPosition = currentGridPosition;
        
        // Set initial world position
        currentWorldPosition = GridToWorldPosition(currentGridPosition);
        transform.position = currentWorldPosition;
        ClearTrail();
        previousWorldPosition = currentWorldPosition;
        
        // Set initial rotation to face right
        transform.rotation = Quaternion.identity;
        
        // Set initial animation direction to face right
        if (animator != null)
        {
            // Ensure we start in idle state, not moving
            animator.SetBool("IsMoving", false);
            SetIdleDirection(Vector2Int.right);
        }

        InitializeTeleporters();
    }

    void Update()
    {
        if (isInDeathSequence)
            return;

        if (controlsEnabled)
        {
            // Handle input
            HandleInput();

            // Handle movement logic when not moving
            if (!isMoving)
            {
                HandleMovement();
            }
        }

        // Handle movement lerping
        if (isMoving)
        {
            float elapsed = Time.time - movementStartTime;
            float progress = elapsed / movementDuration;
            
            // Play audio at the middle of the movement (when reaching tile center)
            if (progress >= 0.5f && !hasPlayedMidpointAudio)
            {
                PlayMidpointAudio();
                hasPlayedMidpointAudio = true;
            }
            
            if (progress >= 1.0f)
            {
                // Movement complete
                transform.position = targetWorldPosition;
                currentWorldPosition = targetWorldPosition;
                currentGridPosition = targetGridPosition;
                isMoving = false;
                Vector2Int moveDirection = targetGridPosition - previousGridPosition;
                TryHandleTeleport(moveDirection);
                HandlePelletConsumption();
            }
            else
            {
                // Lerp between positions
                transform.position = Vector3.Lerp(currentWorldPosition, targetWorldPosition, progress);
            }
        }
    }
    
    private void HandleInput()
    {
        if (!controlsEnabled)
            return;

        // Check for new input
        Vector2Int newInput = Vector2Int.zero;
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            newInput = Vector2Int.down; // (0, -1)
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            newInput = Vector2Int.up; // (0, 1)
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            newInput = Vector2Int.left; // (-1, 0)
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            newInput = Vector2Int.right; // (1, 0)
        }
        
        // Update lastInput only if new input was provided
        if (newInput != Vector2Int.zero)
        {
            lastInput = newInput;
        }
    }
    
    private void HandleMovement()
    {
        if (!controlsEnabled)
            return;

        bool hasBufferedTurn = lastInput != Vector2Int.zero && (currentInput == Vector2Int.zero || lastInput != currentInput);

        if (hasBufferedTurn)
        {
            Vector2Int targetPos = currentGridPosition + lastInput;
            if (IsWalkable(targetPos))
            {
                currentInput = lastInput;
                StartMovement(targetPos);
                return;
            }
        }

        // Continue moving in the current direction regardless of walls so collisions can fire
        if (currentInput != Vector2Int.zero)
        {
            Vector2Int targetPos = currentGridPosition + currentInput;
            if (IsValidGridPosition(targetPos))
            {
                StartMovement(targetPos);
                return;
            }
        }
        
        StopMovement();
    }
    
    public void MoveToGridPosition(Vector2Int gridPos)
    {
        if (!controlsEnabled || isInDeathSequence)
            return;

        // Validate grid position
        if (!IsValidGridPosition(gridPos))
        {
            Debug.LogWarning($"Invalid grid position: {gridPos}");
            return;
        }
        
        // Don't move if already at target or currently moving
        if (gridPos == currentGridPosition || isMoving)
            return;
        
        // Set up movement
        targetGridPosition = gridPos;
        targetWorldPosition = GridToWorldPosition(targetGridPosition);
        
        // Calculate movement duration based on distance and speed
        float distance = Vector3.Distance(currentWorldPosition, targetWorldPosition);
        movementDuration = distance / movementSpeed;
        
        // Start movement
        isMoving = true;
        movementStartTime = Time.time;
    }
    
    public void MoveInDirection(Vector2Int direction)
    {
        Vector2Int newGridPos = currentGridPosition + direction;
        MoveToGridPosition(newGridPos);
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
    
    private bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }
    
    public Vector2Int GetCurrentGridPosition()
    {
        return currentGridPosition;
    }
    
    public bool IsMoving()
    {
        return isMoving;
    }
    
    public float GetMovementProgress()
    {
        if (!isMoving) return 1.0f;
        
        float elapsed = Time.time - movementStartTime;
        return Mathf.Clamp01(elapsed / movementDuration);
    }
    
    public Vector2Int GetLastInput()
    {
        return lastInput;
    }
    
    public Vector2Int GetCurrentInput()
    {
        return currentInput;
    }
    
    private void StartMovement(Vector2Int targetPos)
    {
        bool hasPellet = HasPellet(targetPos);
        CachePreviousPosition();
        
        hasPlayedMidpointAudio = false;
        
        MoveToGridPosition(targetPos);
        
        StartMovementAnimation();
    }
    
    private void StopMovement()
    {
        StopMovementAnimation();
        StopMovementAudio();
        
        hasPlayedMidpointAudio = false;
    }

    private void CachePreviousPosition()
    {
        previousWorldPosition = currentWorldPosition;
        previousGridPosition = currentGridPosition;
    }

    private void InitializeTeleporters()
    {
        int centerRow = Mathf.Clamp(gridHeight / 2, 0, gridHeight - 1);
        int leftColumn = 0;
        int rightColumn = Mathf.Max(0, gridWidth - 1);

        teleporters = new[]
        {
            new TeleporterMapping
            {
                entry = new Vector2Int(leftColumn, centerRow),
                exit = new Vector2Int(rightColumn, centerRow),
                requiredDirection = Vector2Int.left
            },
            new TeleporterMapping
            {
                entry = new Vector2Int(rightColumn, centerRow),
                exit = new Vector2Int(leftColumn, centerRow),
                requiredDirection = Vector2Int.right
            }
        };
    }
    
    private void PlayMidpointAudio()
    {
        bool hasPellet = HasPellet(targetGridPosition);
        StartMovementAudio(hasPellet);
    }
    
    private void StartMovementAnimation()
    {
        if (animator == null)
            return;

        Vector2Int direction = currentInput;
        if (direction == Vector2Int.zero)
        {
            direction = lastFacingDirection == Vector2Int.zero ? Vector2Int.right : lastFacingDirection;
        }

        // Reset all movement direction booleans
        animator.SetBool("IsMovingUp", false);
        animator.SetBool("IsMovingDown", false);
        animator.SetBool("IsMovingLeft", false);
        animator.SetBool("IsMovingRight", false);

        // Reset idle direction booleans
        animator.SetBool("IsIdleUp", false);
        animator.SetBool("IsIdleDown", false);
        animator.SetBool("IsIdleLeft", false);
        animator.SetBool("IsIdleRight", false);

        // Apply movement direction
        animator.SetBool("IsMovingUp", direction == Vector2Int.down);
        animator.SetBool("IsMovingDown", direction == Vector2Int.up);
        animator.SetBool("IsMovingLeft", direction == Vector2Int.left);
        animator.SetBool("IsMovingRight", direction == Vector2Int.right);

        if (currentInput != Vector2Int.zero)
        {
            lastFacingDirection = currentInput;
        }
        else
        {
            lastFacingDirection = direction;
        }

        animator.SetBool("IsMoving", true);
    }
    
    
    
    private void StopMovementAnimation()
    {
        if (animator != null)
        {
            // Stop movement
            animator.SetBool("IsMoving", false);

            // Reset all movement direction booleans
            animator.SetBool("IsMovingUp", false);
            animator.SetBool("IsMovingDown", false);
            animator.SetBool("IsMovingLeft", false);
            animator.SetBool("IsMovingRight", false);

            // Set idle state based on last facing direction
            SetIdleDirection(lastFacingDirection);
        }
    }
    
    private void SetIdleDirection(Vector2Int direction)
    {
        if (animator == null) return;
        
        // Reset all movement booleans
        animator.SetBool("IsMovingUp", false);
        animator.SetBool("IsMovingDown", false);
        animator.SetBool("IsMovingLeft", false);
        animator.SetBool("IsMovingRight", false);
        
        // Set idle direction booleans
        animator.SetBool("IsIdleUp", direction == Vector2Int.down);
        animator.SetBool("IsIdleDown", direction == Vector2Int.up);
        animator.SetBool("IsIdleLeft", direction == Vector2Int.left);
        animator.SetBool("IsIdleRight", direction == Vector2Int.right);

        lastFacingDirection = direction;
    }
    
    private void StartMovementAudio(bool hasPellet)
    {
        if (audioSource != null)
        {
            // Stop any currently playing audio
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // Choose appropriate audio clip
            AudioClip clipToPlay = hasPellet ? pelletEatingAudio : movementAudio;
            
            if (clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.loop = false;
                audioSource.Play();
                
                // Update audio state
                isPlayingMovementAudio = !hasPellet;
                isPlayingPelletAudio = hasPellet;
            }
        }
    }
    
    private void StopMovementAudio(bool force = false)
    {
        if (audioSource == null)
            return;

        bool shouldStop = force || isPlayingMovementAudio || isPlayingPelletAudio;
        if (!shouldStop)
            return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isPlayingMovementAudio = false;
        isPlayingPelletAudio = false;
    }
    
    private bool HasPellet(Vector2Int gridPos)
    {
        if (levelGenerator != null)
        {
            return levelGenerator.HasPellet(gridPos.x, gridPos.y);
        }
        return false;
    }

    private void HandlePelletConsumption()
    {
        if (levelGenerator == null)
            return;

        if (isInDeathSequence)
            return;

        if (!levelGenerator.ConsumePellet(currentGridPosition.x, currentGridPosition.y, out bool wasPowerPellet))
            return;

        if (gameManager != null)
        {
            gameManager.HandlePelletConsumed(wasPowerPellet);
        }
    }

    public void SetControlsEnabled(bool enabled)
    {
        if (controlsEnabled == enabled)
            return;

        controlsEnabled = enabled;

        if (!enabled)
        {
            lastInput = Vector2Int.zero;
            currentInput = Vector2Int.zero;

            if (isMoving)
            {
                isMoving = false;
                transform.position = targetWorldPosition;
                currentWorldPosition = targetWorldPosition;
                StopMovement();
            }
        }
    }

    public void EnterDeathSequence()
    {
        if (isInDeathSequence)
            return;

        isInDeathSequence = true;
        controlsEnabled = false;
        isMoving = false;
        StopMovement();
        StopMovementAudio(true);

        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;

        if (animator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
        {
            animator.ResetTrigger(deathAnimationTrigger);
            animator.SetTrigger(deathAnimationTrigger);
        }

        SpawnDeathParticles();
    }

    public void ResetToSpawnPosition()
    {
        currentGridPosition = spawnGridPosition;
        targetGridPosition = spawnGridPosition;
        previousGridPosition = spawnGridPosition;

        currentWorldPosition = GridToWorldPosition(spawnGridPosition);
        targetWorldPosition = currentWorldPosition;
        previousWorldPosition = currentWorldPosition;

        transform.position = currentWorldPosition;
        transform.rotation = Quaternion.identity;
        ClearTrail();

        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;
        lastFacingDirection = Vector2Int.right;
        SetIdleDirection(Vector2Int.right);
    }

    public void ExitDeathSequence()
    {
        StopMovementAudio(true);
        isInDeathSequence = false;
        controlsEnabled = true;
        hasPlayedMidpointAudio = false;
        isMoving = false;
        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;
        lastFacingDirection = Vector2Int.right;
        SetIdleDirection(Vector2Int.right);
    }

    private void SpawnDeathParticles()
    {
        if (deathParticles == null)
            return;

        ParticleSystem particles = Instantiate(deathParticles, transform.position, Quaternion.identity);
        var main = particles.main;
        float maxLifetime = main.startLifetime.mode switch
        {
            ParticleSystemCurveMode.Constant => main.startLifetime.constant,
            ParticleSystemCurveMode.TwoConstants => main.startLifetime.constantMax,
            _ => main.startLifetime.constantMax
        };
        float destroyDelay = Mathf.Max(0.1f, main.duration + maxLifetime);
        Destroy(particles.gameObject, destroyDelay);
    }

    private void ClearTrail()
    {
        if (movementTrail == null)
            return;

        if (movementTrail.isPlaying)
        {
            movementTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else
        {
            movementTrail.Clear();
        }

        movementTrail.Play();
    }

    private void TryHandleTeleport(Vector2Int moveDirection)
    {
        if (teleporters == null || teleporters.Length == 0)
            return;

        for (int i = 0; i < teleporters.Length; i++)
        {
            TeleporterMapping mapping = teleporters[i];

            if (currentGridPosition != mapping.entry)
                continue;

            if (mapping.requiredDirection != Vector2Int.zero && moveDirection != mapping.requiredDirection)
                continue;

            Vector2Int exitGrid = mapping.exit;
            Vector3 exitWorld = GridToWorldPosition(exitGrid);

            transform.position = exitWorld;
            currentWorldPosition = exitWorld;
            targetWorldPosition = exitWorld;

            previousWorldPosition = exitWorld;
            previousGridPosition = exitGrid;

            currentGridPosition = exitGrid;
            targetGridPosition = exitGrid;

            if (moveDirection != Vector2Int.zero)
            {
                currentInput = moveDirection;
            }

            break;
        }
    }

    private bool IsWalkable(Vector2Int gridPos)
    {
        if (!IsValidGridPosition(gridPos))
            return false;

        if (levelGenerator != null)
        {
            return levelGenerator.IsWalkable(gridPos.x, gridPos.y);
        }

        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsWallCollider(collision.collider))
            return;

        if (!isMoving)
            return;

        if (Time.time - lastWallCollisionTime < wallCollisionEffectCooldown)
            return;

        lastWallCollisionTime = Time.time;
        HandleWallCollision(collision);
    }

    private void HandleWallCollision(Collision2D collision)
    {
        CancelMovementFromCollision();

        if (wallCollisionParticles != null)
        {
            ParticleSystem particles = Instantiate(wallCollisionParticles, transform);
            particles.transform.localPosition = wallCollisionEffectOffset;
            particles.transform.localRotation = Quaternion.identity;
            particles.transform.localScale = Vector3.one;

            if (collision.contactCount > 0)
            {
                Vector2 contact = collision.GetContact(0).point;
                Vector3 worldPos = new Vector3(contact.x, contact.y, transform.position.z);
                particles.transform.position = worldPos + wallCollisionEffectOffset;
            }

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                if (!string.IsNullOrEmpty(wallCollisionParticleSortingLayer))
                {
                    renderer.sortingLayerName = wallCollisionParticleSortingLayer;
                }
                renderer.sortingOrder = wallCollisionParticleSortingOrder;
            }

            particles.Clear();
            particles.Play();
            var main = particles.main;
            float maxLifetime = main.startLifetime.mode switch
            {
                ParticleSystemCurveMode.Constant => main.startLifetime.constant,
                ParticleSystemCurveMode.TwoConstants => main.startLifetime.constantMax,
                _ => main.startLifetime.constantMax
            };
            float destroyDelay = Mathf.Max(0.1f, main.duration + maxLifetime);
            Destroy(particles.gameObject, destroyDelay);
        }

        if (audioSource != null && wallCollisionAudio != null)
        {
            audioSource.PlayOneShot(wallCollisionAudio);
        }
    }

    private void CancelMovementFromCollision()
    {
        isMoving = false;
        hasPlayedMidpointAudio = false;

        transform.position = previousWorldPosition;
        currentWorldPosition = previousWorldPosition;
        targetWorldPosition = previousWorldPosition;

        currentGridPosition = previousGridPosition;
        targetGridPosition = previousGridPosition;

        currentInput = Vector2Int.zero;

        StopMovement();
    }

    private bool IsWallCollider(Collider2D collider)
    {
        if (collider == null)
            return false;

        if (wallLayerMask == 0)
            return true;

        int colliderLayerMask = 1 << collider.gameObject.layer;
        return (wallLayerMask.value & colliderLayerMask) != 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        if (isInDeathSequence)
            return;

        if (collision.CompareTag("BonusCherry"))
        {
            HandleCherryPickup(collision.gameObject);
            return;
        }

        if (collision.CompareTag("Ghost"))
        {
            HandleGhostCollision(collision);
        }
    }

    private void HandleCherryPickup(GameObject cherryObject)
    {
        if (gameManager != null)
        {
            gameManager.AwardCherry();
        }

        if (cherryController != null)
        {
            cherryController.DestroyCherryInstance();
        }
        else if (cherryObject != null)
        {
            Destroy(cherryObject);
        }
    }

    private void HandleGhostCollision(Collider2D ghostCollider)
    {
        if (isInDeathSequence)
            return;

        if (gameManager == null)
            return;

        GhostController ghost = ghostCollider.GetComponent<GhostController>();
        if (ghost == null)
        {
            ghost = ghostCollider.GetComponentInParent<GhostController>();
        }

        gameManager.HandleGhostCollision(ghost);
    }
}
