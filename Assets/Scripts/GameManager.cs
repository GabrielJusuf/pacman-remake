using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private bool autoStartTimer = true;
    [SerializeField] private float startTimeSeconds = 0f;
    [SerializeField] private float timerStartDelaySeconds = 3f;
    [SerializeField] private HUDController hudController;
    [SerializeField] private PacStudentController pacStudent;
    [Header("Round Start UI")]
    [SerializeField] private GameObject roundStartContainer;
    [SerializeField] private TMP_Text roundStartText;
    [SerializeField] private string[] roundStartSequence = { "3", "2", "1", "GO!" };
    [SerializeField] private float countdownStepDuration = 0.85f;
    
    [Header("Score Settings")]
    [SerializeField] private int pelletScoreValue = 10;
    [SerializeField] private int powerPelletScoreValue = 50;
    [SerializeField] private int cherryScoreValue = 100;
    [SerializeField] private int ghostEatenScoreValue = 300;

    [Header("Power Pellet Settings")]
    [SerializeField] private float powerPelletDurationSeconds = 10f;
    [SerializeField] private float powerPelletRecoverWarningSeconds = 3f;
    [SerializeField] private AudioPlayer audioPlayer;

    [Header("Lives Settings")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float pacDeathAnimationDuration = 1.5f;
    [SerializeField] private float ghostRespawnDelaySeconds = 3f;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverContainer;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private float gameOverDisplaySeconds = 3f;

    private float elapsedTime;
    private bool isTimerRunning;
    private int currentScore;
    private GhostController[] ghostControllers;
    private Coroutine powerModeRoutine;
    private float powerModeTimeRemaining;
    private bool powerModeRecoverTriggered;
    private int livesRemaining;
    private bool isPacStudentInDeathSequence;
    private readonly System.Collections.Generic.Dictionary<GhostController, Coroutine> ghostRespawnCoroutines = new System.Collections.Generic.Dictionary<GhostController, Coroutine>();
    private int activeDeadGhosts;
    private bool roundHasStarted;
    private bool isGameOver;
    private LevelGenerator levelGenerator;
    private UIManager uiManager;

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

        levelGenerator = FindObjectOfType<LevelGenerator>();
        uiManager = FindObjectOfType<UIManager>();

        if (roundStartContainer != null)
        {
            roundStartContainer.SetActive(true);
        }
        if (roundStartText != null)
        {
            roundStartText.text = string.Empty;
        }

        if (audioPlayer != null)
        {
            audioPlayer.StopAllLoops();
        }

        if (gameOverContainer != null)
        {
            gameOverContainer.SetActive(false);
        }
        if (gameOverText != null)
        {
            gameOverText.text = string.Empty;
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
            StartCoroutine(RoundStartSequence());
        }
        else
        {
            roundHasStarted = true;
            if (roundStartContainer != null)
            {
                roundStartContainer.SetActive(false);
            }
            ApplyCurrentBackgroundMusic();
            StartTimer();
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
        if (isGameOver)
            return;

        if (isPowerPellet)
        {
            AddScore(powerPelletScoreValue);
            ActivatePowerMode();
        }
        else
        {
            AddScore(pelletScoreValue);

            if (levelGenerator != null && levelGenerator.AreAllNormalPelletsCollected())
            {
                TriggerGameOver("Game Over!");
            }
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
                HandleGhostEaten(ghost);
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

    private void HandleGhostEaten(GhostController ghost)
    {
        if (isGameOver)
            return;

        if (ghost == null)
            return;

        AddScore(ghostEatenScoreValue);

        if (ghostRespawnCoroutines.TryGetValue(ghost, out Coroutine existing))
        {
            if (existing != null)
            {
                StopCoroutine(existing);
            }
            ghostRespawnCoroutines.Remove(ghost);
        }

        ghost.EnterDeadState();
        activeDeadGhosts++;
        ApplyCurrentBackgroundMusic();

        Coroutine routine = StartCoroutine(GhostRespawnRoutine(ghost));
        ghostRespawnCoroutines[ghost] = routine;
    }

    private IEnumerator RoundStartSequence()
    {
        roundHasStarted = false;

        if (pacStudent != null)
        {
            pacStudent.SetControlsEnabled(false);
        }

        SetGhostsFrozen(true);

        if (roundStartContainer != null)
        {
            roundStartContainer.SetActive(true);
        }

        if (audioPlayer != null)
        {
            audioPlayer.StopAllLoops();
        }

        string[] sequence = (roundStartSequence != null && roundStartSequence.Length > 0)
            ? roundStartSequence
            : new[] { "3", "2", "1", "GO!" };

        float stepDuration = Mathf.Clamp(countdownStepDuration, 0.1f, 1f);
        float totalCountdown = 4f;
        float elapsed = 0f;

        for (int i = 0; i < sequence.Length; i++)
        {
            string message = sequence[i];
            if (roundStartText != null)
            {
                roundStartText.text = message;
            }

            if (i < sequence.Length - 1)
            {
                yield return new WaitForSeconds(stepDuration);
                elapsed += stepDuration;
            }
            else
            {
                float remaining = Mathf.Max(0f, totalCountdown - elapsed);
                float goDuration = Mathf.Max(stepDuration, remaining);
                yield return new WaitForSeconds(goDuration);
                elapsed += goDuration;
            }
        }

        if (roundStartContainer != null)
        {
            roundStartContainer.SetActive(false);
        }
        if (roundStartText != null)
        {
            roundStartText.text = string.Empty;
        }

        if (pacStudent != null)
        {
            pacStudent.SetControlsEnabled(true);
        }

        SetGhostsFrozen(false);

        roundHasStarted = true;
        ApplyCurrentBackgroundMusic();
        StartTimer();
    }

    private void TriggerGameOver(string message)
    {
        if (isGameOver)
            return;

        isGameOver = true;
        roundHasStarted = false;
        isPacStudentInDeathSequence = false;

        StopTimer();

        if (audioPlayer != null)
        {
            audioPlayer.StopAllLoops();
        }

        foreach (var kvp in new Dictionary<GhostController, Coroutine>(ghostRespawnCoroutines))
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        ghostRespawnCoroutines.Clear();
        activeDeadGhosts = 0;

        if (pacStudent != null)
        {
            pacStudent.SetControlsEnabled(false);
        }

        SetGhostsFrozen(true);

        if (gameOverContainer != null)
        {
            gameOverContainer.SetActive(true);
        }
        if (gameOverText != null)
        {
            gameOverText.text = string.IsNullOrEmpty(message) ? "Game Over" : message;
        }

        SaveHighScore();

        StartCoroutine(GameOverSequence());
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

        ApplyCurrentBackgroundMusic();

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

        ApplyCurrentBackgroundMusic();
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

    private IEnumerator GhostRespawnRoutine(GhostController ghost)
    {
        float delay = Mathf.Max(0f, ghostRespawnDelaySeconds);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (ghost != null)
        {
            while (ghost != null && !ghost.IsAtSpawnPosition())
            {
                yield return null;
            }

            if (ghost != null)
            {
                GhostState returnState = DetermineGhostReturnState();
                ghost.RespawnToState(returnState);
            }
        }

        if (ghost != null)
        {
            ghostRespawnCoroutines.Remove(ghost);
        }

        activeDeadGhosts = Mathf.Max(0, activeDeadGhosts - 1);
        ApplyCurrentBackgroundMusic();
    }

    private GhostState DetermineGhostReturnState()
    {
        if (powerModeTimeRemaining <= 0f)
            return GhostState.Normal;

        return powerModeRecoverTriggered ? GhostState.Recovering : GhostState.Scared;
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
        foreach (var kvp in new Dictionary<GhostController, Coroutine>(ghostRespawnCoroutines))
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        ghostRespawnCoroutines.Clear();
        activeDeadGhosts = 0;

        RefreshGhostControllers();

        if (ghostControllers == null)
            return;

        foreach (GhostController ghost in ghostControllers)
        {
            if (ghost == null)
                continue;

            ghost.ResetToSpawn();
        }

        ApplyCurrentBackgroundMusic();
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

        if (livesRemaining <= 0)
        {
            if (pacStudent != null)
            {
                pacStudent.ExitDeathSequence();
            }
            isPacStudentInDeathSequence = false;
            TriggerGameOver("Game Over");
            yield break;
        }

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
        ApplyCurrentBackgroundMusic();
    }

    private void ApplyCurrentBackgroundMusic()
    {
        if (audioPlayer == null)
            return;

        if (!roundHasStarted || isGameOver)
        {
            audioPlayer.StopAllLoops();
            return;
        }

        if (powerModeTimeRemaining > 0f)
        {
            audioPlayer.PlayScaredLoop();
        }
        else
        {
            audioPlayer.PlayNormalLoop();
        }

        if (activeDeadGhosts > 0)
        {
            audioPlayer.PlayGhostEatenOverlay();
        }
        else
        {
            audioPlayer.StopGhostEatenOverlay();
        }
    }

    private IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, gameOverDisplaySeconds));

        if (uiManager != null)
        {
            uiManager.ExitLevel();
        }
        else
        {
            SceneManager.LoadScene("StartScene");
        }
    }

    private void SaveHighScore()
    {
        int bestScore = PlayerPrefs.GetInt("HighScore", 0);
        float bestTime = PlayerPrefs.GetFloat("HighScoreTime", float.MaxValue);

        bool isBetter = currentScore > bestScore;
        bool sameScoreBetterTime = currentScore == bestScore && elapsedTime < bestTime;

        if (isBetter || sameScoreBetterTime)
        {
            PlayerPrefs.SetInt("HighScore", currentScore);
            PlayerPrefs.SetFloat("HighScoreTime", elapsedTime);
            PlayerPrefs.Save();
        }
    }
}
