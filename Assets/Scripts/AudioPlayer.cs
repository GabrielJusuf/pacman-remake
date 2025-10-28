using System.Collections;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip introBGM;
    [SerializeField] private AudioClip ghostNormalBGM;
    [SerializeField] private AudioClip ghostScaredBGM;

    AudioSource source;
    Coroutine introRoutine;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    void Start()
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

    IEnumerator PlayIntroThenNormal()
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

    private void PlayLoop(AudioClip clip)
    {
        if (clip == null || source == null)
            return;

        source.loop = true;
        source.clip = clip;
        source.Play();
    }
}
