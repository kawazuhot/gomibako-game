using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private SfxDatabase sfxDatabase;
    [SerializeField] private AudioClip wrongSfx;
    [SerializeField] private AudioClip bombSfx;
    [SerializeField] private AudioClip timeupWhistleClip;
    [SerializeField] private AudioClip resultScoreStartClip;
    [SerializeField] private AudioClip resultRankRevealClip;
    [SerializeField] private string wrongSfxKey = "Wrong";
    [SerializeField] private string bombSfxKey = string.Empty;
    [SerializeField] private string timeupWhistleSfxKey = "sfx_timeup_whistle";
    [SerializeField] private string resultScoreStartSfxKey = "sfx_result_score_start";
    [SerializeField] private string resultRankRevealSfxKey = "sfx_result_rank_reveal";
    [SerializeField] private int sourcePoolSize = 8;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float successSfxVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float wrongSfxVolume = 0.68f;
    [SerializeField, Range(0f, 1f)] private float bombSfxVolume = 0.68f;
    [SerializeField, Range(0f, 1f)] private float timeupWhistleVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float resultScoreStartVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float resultRankRevealVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float uiSfxVolume = 0.6f;
    [SerializeField] private Vector2 successPitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private AudioClip gameplayBgmClip;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.32f;
    [SerializeField] private float bgmFadeInDuration = 0.2f;
    [SerializeField] private float bgmFadeOutDuration = 0.8f;

    private readonly List<AudioSource> sourcePool = new List<AudioSource>();
    private readonly HashSet<string> warnedMissingSuccessKeys = new HashSet<string>();
    private int nextSourceIndex;
    private AudioSource bgmSource;
    private Tween bgmFadeTween;

    public void Configure(SfxDatabase database, AudioClip gameplayBgm = null)
    {
        sfxDatabase = database;
        if (gameplayBgm != null)
        {
            gameplayBgmClip = gameplayBgm;
        }
        EnsurePool();
        EnsureBgmSource();
    }

    private void Awake()
    {
        Instance = this;
        EnsurePool();
        EnsureBgmSource();
    }

    private void OnDestroy()
    {
        bgmFadeTween?.Kill();
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

        PlayClip(clip, successSfxVolume, true, false);
    }

    public void PlayWrongSfx()
    {
        var clip = wrongSfx != null ? wrongSfx : sfxDatabase != null ? sfxDatabase.GetClip(wrongSfxKey) : null;
        PlayClip(clip, wrongSfxVolume, false, true);
    }

    public void PlayBombSfx()
    {
        var clip = bombSfx != null ? bombSfx : sfxDatabase != null ? sfxDatabase.GetClip(bombSfxKey) : null;
        PlayClip(clip, bombSfxVolume, false, true);
    }

    public void PlayTimeupWhistle()
    {
        var clip = timeupWhistleClip != null ? timeupWhistleClip : sfxDatabase != null ? sfxDatabase.GetClip(timeupWhistleSfxKey) : null;
        PlayClip(clip, timeupWhistleVolume, false, true);
    }

    public void PlayResultScoreStartSfx()
    {
        var clip = resultScoreStartClip != null ? resultScoreStartClip : sfxDatabase != null ? sfxDatabase.GetClip(resultScoreStartSfxKey) : null;
        PlayClip(clip, resultScoreStartVolume, false, true);
    }

    public void PlayResultRankRevealSfx()
    {
        var clip = resultRankRevealClip != null ? resultRankRevealClip : sfxDatabase != null ? sfxDatabase.GetClip(resultRankRevealSfxKey) : null;
        PlayClip(clip, resultRankRevealVolume, false, true);
    }

    public void PlayUiSfx(AudioClip clip)
    {
        PlayClip(clip, uiSfxVolume, false, false);
    }

    public void PlayGameplayBgm()
    {
        if (gameplayBgmClip == null)
        {
            return;
        }

        EnsureBgmSource();
        if (bgmSource == null)
        {
            return;
        }

        bgmFadeTween?.Kill();
        bgmSource.clip = gameplayBgmClip;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }

        bgmFadeTween = bgmSource.DOFade(Mathf.Clamp01(masterVolume * bgmVolume), Mathf.Max(0f, bgmFadeInDuration))
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void StopGameplayBgm()
    {
        EnsureBgmSource();
        bgmFadeTween?.Kill();
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.volume = 0f;
    }

    public void FadeOutGameplayBgm()
    {
        EnsureBgmSource();
        bgmFadeTween?.Kill();
        if (bgmSource == null || !bgmSource.isPlaying)
        {
            return;
        }

        bgmFadeTween = bgmSource.DOFade(0f, Mathf.Max(0f, bgmFadeOutDuration))
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => bgmSource.Stop());
    }

    private void PlayClip(AudioClip clip, float categoryVolume, bool randomizePitch, bool priority)
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

        var source = priority ? GetPrioritySource() : GetNextSource();
        source.Stop();
        source.pitch = randomizePitch ? Random.Range(successPitchRange.x, successPitchRange.y) : 1f;
        source.PlayOneShot(clip, Mathf.Clamp01(masterVolume * categoryVolume));
    }

    private AudioSource GetNextSource()
    {
        var source = sourcePool[nextSourceIndex];
        nextSourceIndex = (nextSourceIndex + 1) % sourcePool.Count;
        return source;
    }

    private AudioSource GetPrioritySource()
    {
        for (var i = 0; i < sourcePool.Count; i++)
        {
            if (!sourcePool[i].isPlaying)
            {
                return sourcePool[i];
            }
        }

        return GetNextSource();
    }

    private void EnsureBgmSource()
    {
        if (bgmSource != null)
        {
            return;
        }

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = 0f;
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
