using System.Collections;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [Header("Main Tracks")]
    [SerializeField] private AudioClip introBGM;
    [SerializeField] private AudioClip ghostNormalBGM;
    [SerializeField] private AudioClip ghostScaredBGM;

    [Header("Overlay & SFX")]
    [SerializeField] private AudioClip ghostEatenBGM;
    [SerializeField] private AudioClip pacDeathSFX;

    [Header("Volumes")]
    [SerializeField] private float normalVolume = 1f;
    [SerializeField] private float scaredVolume = 1f;
    [SerializeField] private float scaredDuckedVolume = 0.35f;
    [SerializeField] private float mainFadeDuration = 0.25f;
    [SerializeField] private float overlayFadeDuration = 0.1f;


    private AudioSource mainSource;
    private AudioSource overlaySource;
    
    private Coroutine introRoutine;
    private Coroutine deathRoutine;
    private Coroutine mainFadeRoutine;
    private Coroutine overlayFadeRoutine;

    private bool overlayActive;
    private float currentTargetMainVolume;

    private void Awake()
    {
        if (mainSource == null)
            mainSource = GetComponent<AudioSource>();

        if (mainSource != null)
        {
            mainSource.playOnAwake = false;
            mainSource.loop = true;
            currentTargetMainVolume = normalVolume;
        }

        if (overlaySource == null)
        {
            GameObject overlayObject = new GameObject("AudioOverlaySource");
            overlayObject.transform.SetParent(transform);
            overlaySource = overlayObject.AddComponent<AudioSource>();
            overlaySource.playOnAwake = false;
            overlaySource.loop = true;
            if (mainSource != null)
            {
                overlaySource.outputAudioMixerGroup = mainSource.outputAudioMixerGroup;
            }
        }
    }

    private void Start()
    {
        if (introBGM != null)
        {
            introRoutine = StartCoroutine(PlayIntroThenNormal());
        }
        else
        {
            PlayNormalLoop();
        }
    }

    private IEnumerator PlayIntroThenNormal()
    {
        if (introBGM != null && mainSource != null)
        {
            mainSource.loop = false;
            mainSource.clip = introBGM;
            mainSource.volume = normalVolume;
            mainSource.Play();

            while (mainSource.isPlaying)
            {
                yield return null;
            }
        }

        introRoutine = null;
        PlayNormalLoop();
    }

    private void EnsureIntroStopped()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }
    }

    public void PlayNormalLoop()
    {
        EnsureIntroStopped();
        SetMainLoop(ghostNormalBGM, normalVolume);
    }

    public void PlayScaredLoop()
    {
        EnsureIntroStopped();
        SetMainLoop(ghostScaredBGM, scaredVolume);
        if (overlayActive && mainSource != null && mainSource.clip == ghostScaredBGM)
        {
            StartMainFade(scaredDuckedVolume);
        }
    }

    public void PlayGhostEatenOverlay()
    {
        if (ghostEatenBGM == null || overlaySource == null)
            return;

        if (overlayActive && overlaySource.isPlaying)
            return;

        overlayActive = true;

        overlaySource.clip = ghostEatenBGM;
        overlaySource.loop = true;
        overlaySource.volume = 0f;
        overlaySource.Play();
        StartOverlayFade(1f, false);

        if (mainSource != null && mainSource.clip == ghostScaredBGM)
        {
            StartMainFade(scaredDuckedVolume);
        }
    }

    public void StopGhostEatenOverlay()
    {
        if (!overlayActive)
            return;

        overlayActive = false;
        StartOverlayFade(0f, true);

        if (mainSource != null)
        {
            float target = (mainSource.clip == ghostScaredBGM) ? scaredVolume : normalVolume;
            StartMainFade(target);
        }
    }

    public void PlayDeathOnce(float delaySeconds = 1f)
    {
        if (mainSource == null)
            return;

        EnsureIntroStopped();
        StopGhostEatenOverlayImmediate();

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }
        deathRoutine = StartCoroutine(PlayDeathAfterDelay(Mathf.Max(0f, delaySeconds)));
    }

    private IEnumerator PlayDeathAfterDelay(float delay)
    {
        if (mainSource != null)
        {
            mainSource.Stop();
        }

        StopGhostEatenOverlayImmediate();

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (mainSource == null || pacDeathSFX == null)
            yield break;

        mainSource.loop = false;
        mainSource.clip = pacDeathSFX;
        mainSource.volume = normalVolume;
        mainSource.Play();
    }

    private void SetMainLoop(AudioClip clip, float targetVolume)
    {
        if (clip == null || mainSource == null)
            return;

        bool clipChanged = mainSource.clip != clip;

        mainSource.loop = true;
        mainSource.clip = clip;
        if (clipChanged || !mainSource.isPlaying)
        {
            mainSource.Play();
        }

        currentTargetMainVolume = targetVolume;

        if (!overlayActive || mainSource.clip != ghostScaredBGM)
        {
            StartMainFade(targetVolume);
        }
    }

    private void StartMainFade(float target)
    {
        if (mainSource == null)
            return;

        if (mainFadeRoutine != null)
            StopCoroutine(mainFadeRoutine);
        mainFadeRoutine = StartCoroutine(FadeVolume(mainSource, target, mainFadeDuration));
    }

    private void StartOverlayFade(float target, bool stopAfter)
    {
        if (overlaySource == null)
            return;

        if (overlayFadeRoutine != null)
            StopCoroutine(overlayFadeRoutine);
        overlayFadeRoutine = StartCoroutine(FadeVolume(overlaySource, target, overlayFadeDuration, stopAfter));
    }

    private IEnumerator FadeVolume(AudioSource audio, float target, float duration, bool stopAfter = false)
    {
        if (audio == null)
            yield break;

        float start = audio.volume;
        if (Mathf.Approximately(duration, 0f))
        {
            audio.volume = target;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                audio.volume = Mathf.Lerp(start, target, t);
                yield return null;
            }
            audio.volume = target;
        }

        if (stopAfter && Mathf.Approximately(target, 0f))
        {
            audio.Stop();
        }
    }

    private void StopGhostEatenOverlayImmediate()
    {
        overlayActive = false;
        if (overlayFadeRoutine != null)
        {
            StopCoroutine(overlayFadeRoutine);
            overlayFadeRoutine = null;
        }

        if (overlaySource != null)
        {
            overlaySource.Stop();
            overlaySource.volume = 0f;
        }
    }

    public void StopAllLoops()
    {
        StopGhostEatenOverlayImmediate();
        if (mainSource != null)
        {
            mainSource.Stop();
        }
    }
}
