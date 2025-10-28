using System.Collections;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip introBGM;
    [SerializeField] private AudioClip ghostNormalBGM;
    [SerializeField] private AudioClip ghostScaredBGM;
    [SerializeField] private AudioClip pacDeathSFX;

    private AudioSource source;
    private Coroutine introRoutine;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
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
        if (introBGM != null && source != null)
        {
            source.loop = false;
            source.clip = introBGM;
            source.Play();

            float elapsed = 0f;
            while (elapsed < 3f && source.isPlaying)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        introRoutine = null;
        PlayNormalLoop();
    }

    public void PlayNormalLoop()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        PlayLoop(ghostNormalBGM);
    }

    public void PlayScaredLoop()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        PlayLoop(ghostScaredBGM);
    }

    public void PlayDeathOnce(float delaySeconds = 1f)
    {
        if (source == null)
            return;

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        if (pacDeathSFX == null)
            return;

        StopAllCoroutines();
        StartCoroutine(PlayDeathAfterDelay(Mathf.Max(0f, delaySeconds)));
    }

    private IEnumerator PlayDeathAfterDelay(float delay)
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (source == null || pacDeathSFX == null)
            yield break;

        source.loop = false;
        source.clip = pacDeathSFX;
        source.Play();
    }

    private void PlayLoop(AudioClip clip)
    {
        if (clip == null || source == null)
            return;

        source.loop = true;
        source.clip = clip;
        source.Play();
    }
}
