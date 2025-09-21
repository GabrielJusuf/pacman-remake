using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private AudioClip movementSound;
    
    private AudioSource audioSource;
    private Animator animator;
    
    private Tweener tweener;
    private Vector3[] waypoints;
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private Vector3 lastPosition;
    
    private Vector3[] topLeftBlockWaypoints = new Vector3[]
    {
        new Vector3(-12.5f, 13.5f, 0f),   // Top-left corner
        new Vector3(-7.5f, 13.5f, 0f),   // Top-right corner
        new Vector3(-7.5f, 9.5f, 0f),  // Bottom-right corner
        new Vector3(-12.5f, 9.5f, 0f)   // Bottom-left corner
    };

    void Start()
    {
        tweener = GetComponent<Tweener>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
        waypoints = topLeftBlockWaypoints;
        StartMovement();
    }

    void Update()
    {
        if (!isMoving && !tweener.IsTweening(transform))
        {
            MoveToNextWaypoint();
        }
    }

    private void StartMovement()
    {
        transform.position = waypoints[0];
        currentWaypointIndex = 0;
        MoveToNextWaypoint();
    }

    private void MoveToNextWaypoint()
    {
        int nextIndex = (currentWaypointIndex + 1) % waypoints.Length;
        Vector3 startPos = waypoints[currentWaypointIndex];
        Vector3 endPos = waypoints[nextIndex];
        
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / moveSpeed;
        
        tweener.AddTween(transform, startPos, endPos, duration);
        
        UpdateFacingDirection();
        currentWaypointIndex = nextIndex;
        isMoving = true;
        PlayMovementSound();

        StartCoroutine(ResetMovingState(duration));
    }

    private IEnumerator ResetMovingState(float duration)
    {
        yield return new WaitForSeconds(duration);
        isMoving = false;
    }

    private void PlayMovementSound()
    {
        if (audioSource.isPlaying) return;

        audioSource.clip = movementSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void UpdateFacingDirection()
    {
        animator.SetBool("MovingRight", false);
        animator.SetBool("MovingDown", false);
        animator.SetBool("MovingLeft", false);
        animator.SetBool("MovingUp", false);

        switch(currentWaypointIndex)
        {
            case 0:
                animator.SetBool("MovingRight", true);
                break;
            case 1:
                animator.SetBool("MovingDown", true);
                break;
            case 2:
                animator.SetBool("MovingLeft", true);
                break;
            case 3:
                animator.SetBool("MovingUp", true);
                break;
        }
    }
}
