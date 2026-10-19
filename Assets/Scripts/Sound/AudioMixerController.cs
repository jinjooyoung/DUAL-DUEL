using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioMixerController : MonoBehaviour
{
    [Header("오디오 믹서")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("볼륨 슬라이더 (0.001 ~ 1.0f)")]
    [SerializeField] private Slider musicMasterSlider;
    [SerializeField] private Slider musicBGMSlider;
    [SerializeField] private Slider musicSFXSlider;

    private const string MASTER_KEY = "Master_Vol";
    private const string BGM_KEY = "BGM_Vol";
    private const string SFX_KEY = "SFX_Vol";

    private bool isInitialized = false;

    private void Awake()
    {
        InitSliders();
    }

    private void OnEnable()
    {
        // 팝업이 다시 열릴 때마다 저장된 볼륨값으로 슬라이더 위치 동기화
        SyncSliderValues();
    }

    private void InitSliders()
    {
        if (isInitialized) return;

        float masterVal = PlayerPrefs.GetFloat(MASTER_KEY, 0.8f);
        float bgmVal = PlayerPrefs.GetFloat(BGM_KEY, 0.8f);
        float sfxVal = PlayerPrefs.GetFloat(SFX_KEY, 0.8f);

        if (musicMasterSlider != null) SetupSlider(musicMasterSlider, masterVal, SetMasterVolume);
        if (musicBGMSlider != null) SetupSlider(musicBGMSlider, bgmVal, SetBGMVolume);
        if (musicSFXSlider != null) SetupSlider(musicSFXSlider, sfxVal, SetSFXVolume);

        ApplyMixerVolume("Master", masterVal);
        ApplyMixerVolume("BGM", bgmVal);
        ApplyMixerVolume("SFX", sfxVal);

        isInitialized = true;
    }

    private void SyncSliderValues()
    {
        if (musicMasterSlider != null) musicMasterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MASTER_KEY, 0.8f));
        if (musicBGMSlider != null) musicBGMSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(BGM_KEY, 0.8f));
        if (musicSFXSlider != null) musicSFXSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SFX_KEY, 0.8f));
    }

    private void SetupSlider(Slider slider, float initValue, UnityEngine.Events.UnityAction<float> callback)
    {
        slider.minValue = 0.001f;
        slider.maxValue = 1.0f;
        slider.value = initValue;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(callback);
    }

    public void SetMasterVolume(float volume)
    {
        ApplyMixerVolume("Master", volume);
        PlayerPrefs.SetFloat(MASTER_KEY, volume);
    }

    public void SetBGMVolume(float volume)
    {
        ApplyMixerVolume("BGM", volume);
        PlayerPrefs.SetFloat(BGM_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        ApplyMixerVolume("SFX", volume);
        PlayerPrefs.SetFloat(SFX_KEY, volume);
    }

    private void ApplyMixerVolume(string paramName, float volume)
    {
        if (audioMixer == null) return;
        float safeVolume = Mathf.Clamp(volume, 0.001f, 1.0f);
        audioMixer.SetFloat(paramName, Mathf.Log10(safeVolume) * 20f);
    }
}