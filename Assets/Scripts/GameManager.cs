using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private bool autoStartTimer = true;
    [SerializeField] private float startTimeSeconds = 0f;
    [SerializeField] private float timerStartDelaySeconds = 3f;
    [SerializeField] private HUDController hudController;
    [SerializeField] private PacStudentController pacStudent;
    
    [Header("Score Settings")]
    [SerializeField] private int pelletScoreValue = 10;
    [SerializeField] private int powerPelletScoreValue = 50;
    [SerializeField] private int cherryScoreValue = 100;
    
    [Header("Power Pellet Settings")]
    [SerializeField] private float powerPelletDurationSeconds = 10f;
    [SerializeField] private float powerPelletRecoverWarningSeconds = 3f;
    [SerializeField] private AudioPlayer audioPlayer;

    [Header("Lives Settings")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float pacDeathAnimationDuration = 1.5f;

    private float elapsedTime;
    private bool isTimerRunning;
    private int currentScore;
    private GhostController[] ghostControllers;
    private Coroutine powerModeRoutine;
    private float powerModeTimeRemaining;
    private bool powerModeRecoverTriggered;
    private int livesRemaining;
    private bool isPacStudentInDeathSequence;

    private void Awake()
    {
        if (hudController == null)
        {
            hudController = FindObjectOfType<HUDController>();
        }
        
        if (audioPlayer == null)
        {
            audioPlayer = FindObjectOfType<AudioPlayer>();
        }

        if (pacStudent == null)
        {
            pacStudent = FindObjectOfType<PacStudentController>();
        }

        RefreshGhostControllers();
    }

    private void Start()
    {
        ResetTimer(startTimeSeconds);
        ResetScore();
        InitializeLives();
        if (hudController != null)
        {
            hudController.SetGhostTimerActive(false);
        }

        if (autoStartTimer)
        {
            StartCoroutine(StartTimerAfterDelay());
        }
    }

    private void Update()
    {
        if (!isTimerRunning)
            return;

        elapsedTime += Time.deltaTime;
        PushTimeToHud();
    }

    public void StartTimer()
    {
        isTimerRunning = true;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public void ResetTimer(float newTimeSeconds = 0f)
    {
        elapsedTime = Mathf.Max(0f, newTimeSeconds);
        PushTimeToHud();
    }

    public void ResetScore()
    {
        currentScore = 0;
        PushScoreToHud();
    }

    public void AddScore(int amount)
    {
        currentScore = Mathf.Max(0, currentScore + amount);
        PushScoreToHud();
    }

    public void HandlePelletConsumed(bool isPowerPellet)
    {
        if (isPowerPellet)
        {
            AddScore(powerPelletScoreValue);
            ActivatePowerMode();
        }
        else
        {
            AddScore(pelletScoreValue);
        }
    }

    public void HandleGhostCollision(GhostController ghost)
    {
        if (isPacStudentInDeathSequence)
            return;

        if (ghost == null)
            return;

        switch (ghost.CurrentState)
        {
            case GhostState.Normal:
                HandlePacStudentDeath();
                break;
            case GhostState.Scared:
            case GhostState.Recovering:
                // Future: PacStudent eats ghost
                break;
            case GhostState.Dead:
                // No action when ghost already dead
                break;
        }
    }

    public void AwardCherry()
    {
        AddScore(cherryScoreValue);
    }

    public int GetScore()
    {
        return currentScore;
    }

    private void PushTimeToHud()
    {
        if (hudController == null)
            return;

        hudController.UpdateTimerDisplay(elapsedTime);
    }

    private void PushScoreToHud()
    {
        if (hudController == null)
            return;

        hudController.UpdateScoreDisplay(currentScore);
    }

    private void InitializeLives()
    {
        livesRemaining = Mathf.Max(0, startingLives);
        UpdateHudLives();
    }

    private void UpdateHudLives()
    {
        if (hudController == null)
            return;

        hudController.UpdateLivesDisplay(livesRemaining);
    }

    private void ActivatePowerMode()
    {
        RefreshGhostControllers();

        if (powerModeRoutine != null)
        {
            StopCoroutine(powerModeRoutine);
        }

        powerModeRoutine = StartCoroutine(PowerModeRoutine());
    }

    private void HandlePacStudentDeath()
    {
        if (pacStudent == null)
            return;

        isPacStudentInDeathSequence = true;

        CancelPowerModeIfActive();
        SetGhostsFrozen(true);
        pacStudent.EnterDeathSequence();
        PlayDeathAudio();

        StartCoroutine(PacStudentDeathRoutine());
    }

    private IEnumerator PowerModeRoutine()
    {
        powerModeTimeRemaining = Mathf.Max(0f, powerPelletDurationSeconds);
        powerModeRecoverTriggered = false;
        float recoverThreshold = Mathf.Clamp(powerPelletRecoverWarningSeconds, 0f, powerModeTimeRemaining);

        SetGhostsStateExceptDead(GhostState.Scared);

        if (audioPlayer != null)
        {
            audioPlayer.PlayScaredLoop();
        }

        if (hudController != null)
        {
            hudController.SetGhostTimerActive(true);
            hudController.UpdateGhostTimerDisplay(powerModeTimeRemaining);
        }

        while (powerModeTimeRemaining > 0f)
        {
            powerModeTimeRemaining -= Time.deltaTime;

            if (hudController != null)
            {
                float displayTime = Mathf.Max(0f, powerModeTimeRemaining);
                hudController.UpdateGhostTimerDisplay(displayTime);
            }

            if (!powerModeRecoverTriggered && recoverThreshold > 0f && powerModeTimeRemaining <= recoverThreshold)
            {
                powerModeRecoverTriggered = true;
                SetGhostsStateExceptDead(GhostState.Recovering);
            }

            yield return null;
        }

        EndPowerMode();
    }

    private void EndPowerMode()
    {
        RefreshGhostControllers();
        powerModeRoutine = null;
        powerModeTimeRemaining = 0f;
        powerModeRecoverTriggered = false;
        SetGhostsStateExceptDead(GhostState.Normal);

        if (hudController != null)
        {
            hudController.SetGhostTimerActive(false);
        }

        if (audioPlayer != null)
        {
            audioPlayer.PlayNormalLoop();
        }
    }

    private void CancelPowerModeIfActive()
    {
        if (powerModeRoutine != null)
        {
            StopCoroutine(powerModeRoutine);
            powerModeRoutine = null;
        }

        if (powerModeTimeRemaining > 0f || powerModeRecoverTriggered)
        {
            EndPowerMode();
        }
    }

    private void RefreshGhostControllers()
    {
        ghostControllers = FindObjectsOfType<GhostController>();
    }

    private void SetGhostsStateExceptDead(GhostState targetState)
    {
        if (ghostControllers == null)
            return;

        foreach (GhostController ghost in ghostControllers)
        {
            if (ghost == null)
                continue;

            ghost.SetStateIfNotDead(targetState);
        }
    }

    private void SetGhostsFrozen(bool frozen)
    {
        RefreshGhostControllers();

        if (ghostControllers == null)
            return;

        foreach (GhostController ghost in ghostControllers)
        {
            if (ghost == null)
                continue;

            ghost.SetFrozen(frozen);
        }
    }

    private void ResetGhostsToSpawn()
    {
        RefreshGhostControllers();

        if (ghostControllers == null)
            return;

        foreach (GhostController ghost in ghostControllers)
        {
            if (ghost == null)
                continue;

            ghost.ResetToSpawn();
        }
    }

    private IEnumerator PacStudentDeathRoutine()
    {
        float waitDuration = Mathf.Max(0f, pacDeathAnimationDuration);
        if (waitDuration > 0f)
        {
            yield return new WaitForSeconds(waitDuration);
        }

        livesRemaining = Mathf.Max(0, livesRemaining - 1);
        UpdateHudLives();

        if (pacStudent != null)
        {
            pacStudent.ResetToSpawnPosition();
        }

        ResetGhostsToSpawn();

        SetGhostsFrozen(false);
        PlayNormalAudioIfNeeded();

        if (pacStudent != null)
        {
            pacStudent.ExitDeathSequence();
        }

        isPacStudentInDeathSequence = false;
    }

    private IEnumerator StartTimerAfterDelay()
    {
        float delay = Mathf.Max(0f, timerStartDelaySeconds);

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        StartTimer();
    }

    private void PlayDeathAudio()
    {
        if (audioPlayer == null)
            return;

        audioPlayer.PlayDeathOnce();
    }

    private void PlayNormalAudioIfNeeded()
    {
        if (audioPlayer == null)
            return;

        audioPlayer.PlayNormalLoop();
    }
}
