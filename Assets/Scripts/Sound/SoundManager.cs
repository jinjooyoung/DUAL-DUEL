using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("사운드 데이터베이스 SO")]
    [SerializeField] private SoundDatabaseSO soundDatabase;

    [Header("오디오 믹서 출력 그룹 연결")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("BGM 재생 채널 (2채널 크로스페이드)")]
    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private bool isUsingSourceA = true;
    public BGMType CurrentBGM { get; private set; } = BGMType.None;

    [Header("SFX 풀링")]
    [SerializeField] private int sfxPoolSize = 10;
    private List<AudioSource> sfxSources = new List<AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (soundDatabase != null)
            {
                soundDatabase.Initialize();
            }

            InitAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitAudioSources()
    {
        // BGM 채널 2개 동적 생성
        bgmSourceA = gameObject.AddComponent<AudioSource>();
        bgmSourceA.loop = true;
        bgmSourceA.outputAudioMixerGroup = bgmGroup;

        bgmSourceB = gameObject.AddComponent<AudioSource>();
        bgmSourceB.loop = true;
        bgmSourceB.outputAudioMixerGroup = bgmGroup;

        // SFX 채널 풀 생성
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource sfx = gameObject.AddComponent<AudioSource>();
            sfx.loop = false;
            sfx.outputAudioMixerGroup = sfxGroup;
            sfxSources.Add(sfx);
        }
    }

    #region BGM 제어

    /// <summary>
    /// BGM을 페이드인/아웃하며 루프 재생합니다.
    /// </summary>
    public void PlayBGM(BGMType type, float fadeDuration = 0.5f)
    {
        if (CurrentBGM == type && type != BGMType.None) return;
        if (soundDatabase == null) return;

        SoundData<BGMType> data = soundDatabase.GetBGM(type);
        if (data == null || data.clip == null)
        {
            StopBGM(fadeDuration);
            return;
        }

        CurrentBGM = type;

        AudioSource currentSource = isUsingSourceA ? bgmSourceA : bgmSourceB;
        AudioSource newSource = isUsingSourceA ? bgmSourceB : bgmSourceA;
        isUsingSourceA = !isUsingSourceA;

        newSource.clip = data.clip;
        newSource.pitch = data.pitch;
        newSource.volume = 0f;
        newSource.Play();

        currentSource.DOFade(0f, fadeDuration).OnComplete(() => currentSource.Stop());
        newSource.DOFade(data.volume, fadeDuration);
    }

    public void StopBGM(float fadeDuration = 0.5f)
    {
        CurrentBGM = BGMType.None;
        bgmSourceA.DOFade(0f, fadeDuration).OnComplete(() => bgmSourceA.Stop());
        bgmSourceB.DOFade(0f, fadeDuration).OnComplete(() => bgmSourceB.Stop());
    }

    #endregion

    #region SFX 제어

    public void PlaySFXByIndex(int index)
    {
        if (index < 0 || index >= System.Enum.GetValues(typeof(SFXType)).Length)
            return;

        PlaySFX((SFXType)index);
    }

    /// <summary>
    /// 효과음을 1회 재생합니다.
    /// </summary>
    public void PlaySFX(SFXType type)
    {
        if (soundDatabase == null) return;

        SoundData<SFXType> data = soundDatabase.GetSFX(type);
        if (data == null || data.clip == null) return;

        AudioSource availableSource = sfxSources.Find(s => !s.isPlaying);
        if (availableSource == null) availableSource = sfxSources[0];

        availableSource.clip = data.clip;
        availableSource.volume = data.volume;
        availableSource.pitch = data.pitch;
        availableSource.PlayOneShot(data.clip);
    }

    #endregion
}