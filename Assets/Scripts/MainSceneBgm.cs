using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainSceneBgm : MonoBehaviour
{
    private const string MainSceneName = "NewMainScene";
    private const string BgmClipPath = "Music/A_Morning_Window";
    private const string JumpClipPath = "Music/Cat jump_1";
    private const string CollectClipPath = "Music/item_Collect";
    private const float BgmStartDelay = 0.5f;
    private const float WindStartDelay = 1f;
    private const float FadeDuration = 0.75f;
    private const float BgmVolume = 0.5f;
    private const float JumpVolume = 1f;
    private const float CollectVolume = 1f;
    private const float WindVolume = 0.6f;

    private static readonly string[] WindClipPaths =
    {
        "Music/Gentle distant wind_1",
        "Music/Gentle distant wind_2",
        "Music/Gentle distant wind_3"
    };

    private static MainSceneBgm instance;

    private AudioSource bgmSource;
    private AudioSource jumpSource;
    private AudioSource collectSource;
    private AudioSource windSource;
    private AudioClip jumpClip;
    private AudioClip collectClip;
    private AudioClip[] windClips;
    private Coroutine bgmRoutine;
    private Coroutine windRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        instance = new GameObject("Main Scene BGM").AddComponent<MainSceneBgm>();
        DontDestroyOnLoad(instance.gameObject);
    }

    public static void FadeOut()
    {
        if (instance != null)
        {
            instance.StartFadeOut();
        }
    }

    public static void PlayJump()
    {
        if (instance != null && instance.jumpClip != null)
        {
            instance.jumpSource.PlayOneShot(instance.jumpClip);
        }
    }

    public static void PlayCollect()
    {
        if (instance != null && instance.collectClip != null)
        {
            instance.collectSource.PlayOneShot(instance.collectClip);
        }
    }

    private void Awake()
    {
        bgmSource = CreateSource(Resources.Load<AudioClip>(BgmClipPath), true);
        jumpSource = CreateSource(null, false);
        jumpSource.volume = JumpVolume;
        collectSource = CreateSource(null, false);
        collectSource.volume = CollectVolume;
        windSource = CreateSource(null, false);
        jumpClip = Resources.Load<AudioClip>(JumpClipPath);
        collectClip = Resources.Load<AudioClip>(CollectClipPath);
        windClips = LoadClips(WindClipPaths);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == MainSceneName)
        {
            Play();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (scene.name == MainSceneName)
        {
            Play();
        }
        else
        {
            StopNow();
        }
    }

    private void Play()
    {
        if (bgmSource.clip != null)
        {
            StopRoutine(ref bgmRoutine);
            bgmRoutine = StartCoroutine(FadeBgmInRoutine());
        }

        StopRoutine(ref windRoutine);
        windRoutine = StartCoroutine(WindRoutine());
    }

    private void StartFadeOut()
    {
        StopRoutine(ref bgmRoutine);
        StopRoutine(ref windRoutine);
        bgmRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeBgmInRoutine()
    {
        bgmSource.volume = 0f;
        bgmSource.Play();
        yield return new WaitForSecondsRealtime(BgmStartDelay);
        yield return FadeTo(bgmSource, BgmVolume);
        bgmRoutine = null;
    }

    private IEnumerator WindRoutine()
    {
        windSource.volume = 0f;
        yield return new WaitForSecondsRealtime(WindStartDelay);

        bool firstClip = true;
        while (SceneManager.GetActiveScene().name == MainSceneName)
        {
            windSource.clip = GetRandomWindClip();
            if (windSource.clip == null) yield break;

            windSource.Play();
            if (firstClip)
            {
                firstClip = false;
                yield return FadeTo(windSource, WindVolume);
            }
            else
            {
                windSource.volume = WindVolume;
            }

            while (windSource.isPlaying)
            {
                yield return null;
            }
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        float bgmStart = bgmSource.volume;
        float windStart = windSource.volume;

        for (float elapsed = 0f; elapsed < FadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed / FadeDuration;
            bgmSource.volume = Mathf.Lerp(bgmStart, 0f, t);
            windSource.volume = Mathf.Lerp(windStart, 0f, t);
            yield return null;
        }

        bgmSource.Stop();
        windSource.Stop();
        bgmSource.volume = 0f;
        windSource.volume = 0f;
        bgmRoutine = null;
    }

    private IEnumerator FadeTo(AudioSource audioSource, float target)
    {
        float start = audioSource.volume;
        for (float elapsed = 0f; elapsed < FadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(start, target, elapsed / FadeDuration);
            yield return null;
        }

        audioSource.volume = target;
    }

    private void StopNow()
    {
        StopRoutine(ref bgmRoutine);
        StopRoutine(ref windRoutine);
        bgmSource.Stop();
        windSource.Stop();
        bgmSource.volume = 0f;
        windSource.volume = 0f;
    }

    private AudioSource CreateSource(AudioClip clip, bool loop)
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    private AudioClip GetRandomWindClip()
    {
        return windClips == null || windClips.Length == 0
            ? null
            : windClips[Random.Range(0, windClips.Length)];
    }

    private static AudioClip[] LoadClips(string[] paths)
    {
        AudioClip[] clips = new AudioClip[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            clips[i] = Resources.Load<AudioClip>(paths[i]);
        }

        return clips;
    }

    private void StopRoutine(ref Coroutine coroutine)
    {
        if (coroutine == null) return;

        StopCoroutine(coroutine);
        coroutine = null;
    }
}
