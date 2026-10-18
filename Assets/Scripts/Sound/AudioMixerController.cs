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

    private void Start()
    {
        // 1. 저장된 볼륨값 불러오기 (기본값: 0.8f)
        float masterVal = PlayerPrefs.GetFloat(MASTER_KEY, 0.8f);
        float bgmVal = PlayerPrefs.GetFloat(BGM_KEY, 0.8f);
        float sfxVal = PlayerPrefs.GetFloat(SFX_KEY, 0.8f);

        // 2. 슬라이더 설정 및 이벤트 리스너 연결
        if (musicMasterSlider != null)
        {
            SetupSlider(musicMasterSlider, masterVal, SetMasterVolume);
        }

        if (musicBGMSlider != null)
        {
            SetupSlider(musicBGMSlider, bgmVal, SetBGMVolume);
        }

        if (musicSFXSlider != null)
        {
            SetupSlider(musicSFXSlider, sfxVal, SetSFXVolume);
        }

        // 3. 시작 시 오디오 믹서에 볼륨값 즉시 적용
        ApplyMixerVolume("Master", masterVal);
        ApplyMixerVolume("BGM", bgmVal);
        ApplyMixerVolume("SFX", sfxVal);
    }

    private void SetupSlider(Slider slider, float initValue, UnityEngine.Events.UnityAction<float> callback)
    {
        slider.minValue = 0.001f;
        slider.maxValue = 1.0f;
        slider.value = initValue;
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