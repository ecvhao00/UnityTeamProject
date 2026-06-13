using TarodevController;
using UnityEngine;

public sealed class ElectricSfxManager : MonoBehaviour
{
    private const string ClipPath = "Music/Electric";
    private const float Volume = 0.8f;
    private const float FullVolumeDistance = 3f;
    private const float MaxDistance = 12f;
    private const float MinInterval = 0.1f;

    private static ElectricSfxManager instance;

    private AudioSource source;
    private AudioClip clip;
    private Transform listener;
    private Vector3 pendingPosition;
    private float pendingVolume;
    private float nextPlayTime;
    private bool hasPending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        instance = new GameObject("Electric SFX Manager").AddComponent<ElectricSfxManager>();
        DontDestroyOnLoad(instance.gameObject);
    }

    public static void PlayAt(Vector3 position)
    {
        if (instance == null || instance.clip == null) return;

        float volume = instance.GetVolume(position);
        if (volume <= 0f || volume <= instance.pendingVolume) return;

        instance.pendingPosition = position;
        instance.pendingVolume = volume;
        instance.hasPending = true;
    }

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        clip = Resources.Load<AudioClip>(ClipPath);
    }

    private void LateUpdate()
    {
        if (!hasPending || Time.unscaledTime < nextPlayTime) return;

        source.PlayOneShot(clip, pendingVolume);
        nextPlayTime = Time.unscaledTime + MinInterval;
        hasPending = false;
        pendingVolume = 0f;
    }

    private float GetVolume(Vector3 position)
    {
        Transform target = GetListener();
        if (target == null) return 0f;

        float distance = Vector2.Distance(position, target.position);
        float t = Mathf.InverseLerp(MaxDistance, FullVolumeDistance, distance);
        return Mathf.Clamp01(t) * Volume;
    }

    private Transform GetListener()
    {
        if (listener != null) return listener;

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            listener = player.transform;
            return listener;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }
}
