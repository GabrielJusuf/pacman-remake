using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float tileSize = 1.0f;
    public float movementSpeed = 2.0f; // Units per second
    
    [Header("Grid Settings")]
    public int gridWidth = 28;  // Based on LevelGenerator full map width
    public int gridHeight = 29; // Based on LevelGenerator full map height
    
    [Header("Animation Settings")]
    public Animator animator;
    
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip movementAudio;
    public AudioClip pelletEatingAudio;
    
    // Grid position (in grid coordinates)
    private Vector2Int currentGridPosition;
    private Vector2Int targetGridPosition;
    
    // World position (in world coordinates)
    private Vector3 currentWorldPosition;
    private Vector3 targetWorldPosition;
    
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
    
    // Last facing direction for idle state
    private Vector2Int lastFacingDirection = Vector2Int.zero;
    
    // Animation state tracking
    private Vector2Int currentAnimationDirection = Vector2Int.zero;
    
    // Reference to Tweener for movement
    private Tweener tweener;
    
    // Reference to LevelGenerator for map data
    private LevelGenerator levelGenerator;

    void Start()
    {
        // Get or create Tweener component
        tweener = FindObjectOfType<Tweener>();
        if (tweener == null)
        {
            GameObject tweenerObj = new GameObject("Tweener");
            tweener = tweenerObj.AddComponent<Tweener>();
        }
        
        // Get LevelGenerator reference
        levelGenerator = FindObjectOfType<LevelGenerator>();
        
        // Initialize animator and audio source
        if (animator == null)
            animator = GetComponent<Animator>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Initialize grid position to top-left walkable area
        // Based on the LevelGenerator map, position (1,1) should be walkable (pellet)
        currentGridPosition = new Vector2Int(1, 1);
        targetGridPosition = currentGridPosition;
        
        // Set initial world position
        currentWorldPosition = GridToWorldPosition(currentGridPosition);
        transform.position = currentWorldPosition;
        
        // Set initial rotation to face right
        transform.rotation = Quaternion.identity;
        
        // Set initial animation direction to face right
        if (animator != null)
        {
            // Ensure we start in idle state, not moving
            animator.SetBool("IsMoving", false);
            SetIdleDirection(Vector2Int.right);
        }
    }

    void Update()
    {
        // Handle input
        HandleInput();
        
        // Handle movement logic when not moving
        if (!isMoving)
        {
            HandleMovement();
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
                
                // Stop movement animations and audio
                StopMovement();
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
        // Try to move based on lastInput first
        if (lastInput != Vector2Int.zero)
        {
            Vector2Int targetPos = currentGridPosition + lastInput;
            if (IsWalkable(targetPos))
            {
                // Store lastInput as currentInput and move
                currentInput = lastInput;
                StartMovement(targetPos);
                return;
            }
        }
        
        // If lastInput doesn't work, try currentInput
        if (currentInput != Vector2Int.zero)
        {
            Vector2Int targetPos = currentGridPosition + currentInput;
            if (IsWalkable(targetPos))
            {
                StartMovement(targetPos);
                return;
            }
        }
        
        // If neither works, PacStudent stops moving
        StopMovement();
    }
    
    public void MoveToGridPosition(Vector2Int gridPos)
    {
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
    
    private bool IsWalkable(Vector2Int gridPos)
    {
        // Check if position is within bounds
        if (!IsValidGridPosition(gridPos))
            return false;
        
        // Use LevelGenerator to check if the position is walkable
        if (levelGenerator != null)
        {
            return levelGenerator.IsWalkable(gridPos.x, gridPos.y);
        }
        
        // Fallback: assume all positions within bounds are walkable
        return true;
    }
    
    private void StartMovement(Vector2Int targetPos)
    {
        // Check if target position has a pellet
        bool hasPellet = HasPellet(targetPos);
        
        // Reset midpoint audio flag
        hasPlayedMidpointAudio = false;
        
        // Start movement
        MoveToGridPosition(targetPos);
        
        // Start animations only (audio will play at midpoint)
        StartMovementAnimation();
    }
    
    private void StopMovement()
    {
        // Stop animations and audio
        StopMovementAnimation();
        StopMovementAudio();
        
        // Reset midpoint audio flag
        hasPlayedMidpointAudio = false;
    }
    
    private void PlayMidpointAudio()
    {
        // Check if target position has a pellet
        bool hasPellet = HasPellet(targetGridPosition);
        
        // Play appropriate audio at the midpoint (tile center)
        StartMovementAudio(hasPellet);
    }
    
    private void StartMovementAnimation()
    {
        if (animator != null)
        {
            Vector2Int direction = currentInput;
            
            // Reset all direction booleans first
            animator.SetBool("IsMovingUp", false);
            animator.SetBool("IsMovingDown", false);
            animator.SetBool("IsMovingLeft", false);
            animator.SetBool("IsMovingRight", false);
            
            // Set the correct direction
            animator.SetBool("IsMovingUp", direction == Vector2Int.down);
            animator.SetBool("IsMovingDown", direction == Vector2Int.up);
            animator.SetBool("IsMovingLeft", direction == Vector2Int.left);
            animator.SetBool("IsMovingRight", direction == Vector2Int.right);
            
            // Set moving state
            animator.SetBool("IsMoving", true);
        }
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
    
    private void StopMovementAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            isPlayingMovementAudio = false;
            isPlayingPelletAudio = false;
        }
    }
    
    private bool HasPellet(Vector2Int gridPos)
    {
        if (levelGenerator != null)
        {
            return levelGenerator.HasPellet(gridPos.x, gridPos.y);
        }
        return false;
    }
}
