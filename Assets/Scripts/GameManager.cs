using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private bool autoStartTimer = true;
    [SerializeField] private float startTimeSeconds = 0f;
    [SerializeField] private float timerStartDelaySeconds = 3f;
    [SerializeField] private HUDController hudController;
    
    [Header("Score Settings")]
    [SerializeField] private int pelletScoreValue = 10;
    [SerializeField] private int powerPelletScoreValue = 50;
    [SerializeField] private int cherryScoreValue = 100;

    private float elapsedTime;
    private bool isTimerRunning;
    private int currentScore;

    private void Awake()
    {
        if (hudController == null)
        {
            hudController = FindObjectOfType<HUDController>();
        }
    }

    private void Start()
    {
        ResetTimer(startTimeSeconds);
        ResetScore();

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

    public void AwardPellet(bool isPowerPellet)
    {
        int amount = isPowerPellet ? powerPelletScoreValue : pelletScoreValue;
        AddScore(amount);
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

    private IEnumerator StartTimerAfterDelay()
    {
        float delay = Mathf.Max(0f, timerStartDelaySeconds);

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        StartTimer();
    }
}
