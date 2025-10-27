using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private bool autoStartTimer = true;
    [SerializeField] private float startTimeSeconds = 0f;
    [SerializeField] private float timerStartDelaySeconds = 3f;
    [SerializeField] private HUDController hudController;

    private float elapsedTime;
    private bool isTimerRunning;

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

    private void PushTimeToHud()
    {
        if (hudController == null)
            return;

        hudController.UpdateTimerDisplay(elapsedTime);
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

