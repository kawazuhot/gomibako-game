using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private SfxDatabase sfxDatabase;
    [SerializeField] private AudioClip wrongSfx;
    [SerializeField] private AudioClip bombSfx;
    [SerializeField] private string wrongSfxKey = "Wrong";
    [SerializeField] private string bombSfxKey = string.Empty;
    [SerializeField] private int sourcePoolSize = 12;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.85f;

    private readonly List<AudioSource> sourcePool = new List<AudioSource>();
    private readonly HashSet<string> warnedMissingSuccessKeys = new HashSet<string>();
    private int nextSourceIndex;

    public void Configure(SfxDatabase database)
    {
        sfxDatabase = database;
        EnsurePool();
    }

    private void Awake()
    {
        Instance = this;
        EnsurePool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlaySuccessSfx(string sfxKey)
    {
        if (string.IsNullOrWhiteSpace(sfxKey))
        {
            return;
        }

        var clip = sfxDatabase != null ? sfxDatabase.GetClip(sfxKey) : null;
        if (clip == null)
        {
            if (warnedMissingSuccessKeys.Add(sfxKey))
            {
                Debug.LogWarning($"[AudioManager] Success SFX not found: {sfxKey}");
            }
            return;
        }

        PlayClip(clip);
    }

    public void PlayWrongSfx()
    {
        var clip = wrongSfx != null ? wrongSfx : sfxDatabase != null ? sfxDatabase.GetClip(wrongSfxKey) : null;
        PlayClip(clip);
    }

    public void PlayBombSfx()
    {
        var clip = bombSfx != null ? bombSfx : sfxDatabase != null ? sfxDatabase.GetClip(bombSfxKey) : null;
        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsurePool();
        if (sourcePool.Count == 0)
        {
            return;
        }

        var source = sourcePool[nextSourceIndex];
        nextSourceIndex = (nextSourceIndex + 1) % sourcePool.Count;
        source.Stop();
        source.PlayOneShot(clip, Mathf.Clamp01(masterVolume * sfxVolume));
    }

    private void EnsurePool()
    {
        var targetCount = Mathf.Max(1, sourcePoolSize);
        while (sourcePool.Count < targetCount)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            sourcePool.Add(source);
        }
    }
}
