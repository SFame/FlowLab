using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

public class UI_Settings : MonoBehaviour
{
    // sound volume? screen size? etc.

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider vfxSlider;
    [SerializeField] private TextMeshProUGUI musicValueText;
    [SerializeField] private TextMeshProUGUI vfxValueText;

    // 오디오 믹서 파라미터 이름
    private const string MusicVolume = "Music";
    private const string VFXVolume = "VFX";

    private void Start()
    {
        DefalutSettings();
        // 이벤트 리스너 설정
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        vfxSlider.onValueChanged.AddListener(SetVFXVolume);

    }

    public void Init()
    {
        // 믹서에서 값 가져오기
        GetAudioSettings();
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        vfxSlider.onValueChanged.AddListener(SetVFXVolume);
    }

    private void DefalutSettings()
    {
        // 기본값 1f (100%)
        float bgmVolume = 1f;
        float sfxVolume = 1f;

        // 슬라이더 값 설정
        musicSlider.value = bgmVolume;
        vfxSlider.value = sfxVolume;

        // 슬라이더 값을 오디오 믹서에 적용
        SetMusicVolume(bgmVolume);
        SetVFXVolume(sfxVolume);

        // 텍스트 업데이트
        UpdateVolumeText();
    }

    public void SetMusicVolume(float volume)
    {
        // 볼륨 값을 데시벨로 변환 (로그 스케일)
        // 0 = 음소거, 1 = 0dB (최대 볼륨)
        float dB = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
        audioMixer.SetFloat(MusicVolume, dB);

       
        // 텍스트 업데이트
        if (musicValueText != null)
            musicValueText.text = Mathf.RoundToInt(volume * 100) + "%";
    }

    public void SetVFXVolume(float volume)
    {
        // 볼륨 값을 데시벨로 변환
        float dB = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
        audioMixer.SetFloat(VFXVolume, dB);

        // 텍스트 업데이트
        if (vfxValueText != null)
            vfxValueText.text = Mathf.RoundToInt(volume * 100) + "%";
    }

    private void UpdateVolumeText()
    {
        if (musicValueText != null)
            musicValueText.text = Mathf.RoundToInt(musicSlider.value * 100) + "%";

        if (vfxValueText != null)
            vfxValueText.text = Mathf.RoundToInt(vfxSlider.value * 100) + "%";
    }

    private void GetAudioSettings()
    {
        float musicDB, vfxDB;

        if (audioMixer.GetFloat(MusicVolume, out musicDB))
        {
            // dB 값을 0-1 범위로 변환
            float bgmVolume = Mathf.Pow(10, musicDB / 20);
            musicSlider.value = bgmVolume;
        }
        else
        {
            // 기본값 설정
            musicSlider.value = 1f; // 75% 볼륨
            SetMusicVolume(1f);
        }

        if (audioMixer.GetFloat(VFXVolume, out vfxDB))
        {
            float sfxVolume = Mathf.Pow(10, vfxDB / 20);
            vfxSlider.value = sfxVolume;
        }
        else
        {
            vfxSlider.value = 1f;
            SetVFXVolume(1f);
        }

        // 텍스트 업데이트
        UpdateVolumeText();
    }
    public void OnClickClose()
    {
        gameObject.SetActive(false);
    }
}