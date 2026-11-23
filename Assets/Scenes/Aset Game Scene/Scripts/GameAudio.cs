using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    public const string MusicVolumePref = "Audio_MusicVolume";
    public const string SfxVolumePref = "Audio_SfxVolume";
    public const string MusicMutedPref = "Audio_MusicMuted";
    public const string SfxMutedPref = "Audio_SfxMuted";

    private const float DefaultVolume = 0.15f;

    [Header("Pengaturan Suara")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button musicMuteButton;
    [SerializeField] private Button sfxMuteButton;
    [SerializeField] private Text musicButtonText;
    [SerializeField] private Text sfxButtonText;

    [SerializeField] private AudioClip lobbyBgm;
    [SerializeField] private AudioClip gameBgm;
    [SerializeField] private AudioClip walk;
    [SerializeField] private AudioClip jump;
    [SerializeField] private AudioClip death;
    [SerializeField] private AudioClip keyPickup;
    [SerializeField] private AudioClip gateOpen;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip bouncer;
    [SerializeField] private AudioClip buttonTap;

    private readonly List<Button> buttons = new();
    private AudioSource musicSource;
    private AudioSource effectsSource;
    private AudioSource walkSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        musicSource = CreateSource(true);
        effectsSource = CreateSource(false);
        walkSource = CreateSource(false);
        walkSource.loop = true;
        walkSource.clip = walk;

        ApplySavedVolumeSettings();
    }

    private void Start()
    {
        AudioClip music = SceneManager.GetActiveScene().name == "Lobby Scene"
            ? lobbyBgm
            : gameBgm;

        if (music != null)
        {
            musicSource.clip = music;
            musicSource.Play();
        }

        foreach (Button button in FindObjectsByType<Button>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
            RegisterButton(button);

        RefreshVolumeUI();
    }

    private void OnEnable()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SetSfxVolume);

        if (musicMuteButton != null)
            musicMuteButton.onClick.AddListener(ToggleMusicMute);

        if (sfxMuteButton != null)
            sfxMuteButton.onClick.AddListener(ToggleSfxMute);
    }

    private void OnDisable()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);

        if (musicMuteButton != null)
            musicMuteButton.onClick.RemoveListener(ToggleMusicMute);

        if (sfxMuteButton != null)
            sfxMuteButton.onClick.RemoveListener(ToggleSfxMute);
    }

    private void OnDestroy()
    {
        foreach (Button button in buttons)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayButtonTap);
        }

        if (Instance == this)
            Instance = null;
    }

    public void SetWalking(bool walking)
    {
        if (walkSource == null || walk == null)
            return;

        if (walking && !walkSource.isPlaying)
            walkSource.Play();
        else if (!walking && walkSource.isPlaying)
            walkSource.Stop();
    }

    public void RegisterButton(Button button)
    {
        if (button == null || buttons.Contains(button))
            return;

        button.onClick.AddListener(PlayButtonTap);
        buttons.Add(button);
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumePref, volume);
        ApplyMusicVolume();
        PlayerPrefs.Save();
        RefreshVolumeUI();
    }

    public void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumePref, volume);
        ApplySfxVolume();
        PlayerPrefs.Save();
        RefreshVolumeUI();
    }

    public void SetMusicMuted(bool muted)
    {
        PlayerPrefs.SetInt(MusicMutedPref, muted ? 1 : 0);
        ApplyMusicVolume();
        PlayerPrefs.Save();
        RefreshVolumeUI();
    }

    public void SetSfxMuted(bool muted)
    {
        PlayerPrefs.SetInt(SfxMutedPref, muted ? 1 : 0);
        ApplySfxVolume();
        PlayerPrefs.Save();
        RefreshVolumeUI();
    }

    public void ToggleMusicMute() =>
        SetMusicMuted(!IsMusicMuted);

    public void ToggleSfxMute() =>
        SetSfxMuted(!IsSfxMuted);

    private void RefreshVolumeUI()
    {
        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(MusicVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(SfxVolume);

        if (musicButtonText != null)
            musicButtonText.text = IsMusicMuted ? "BGM: OFF" : "BGM: ON";

        if (sfxButtonText != null)
            sfxButtonText.text = IsSfxMuted ? "SFX: OFF" : "SFX: ON";
    }

    public float MusicVolume =>
        PlayerPrefs.GetFloat(MusicVolumePref, DefaultVolume);

    public float SfxVolume =>
        PlayerPrefs.GetFloat(SfxVolumePref, DefaultVolume);

    public bool IsMusicMuted =>
        PlayerPrefs.GetInt(MusicMutedPref, 0) == 1;

    public bool IsSfxMuted =>
        PlayerPrefs.GetInt(SfxMutedPref, 0) == 1;

    public void PlayJump() => PlayEffect(jump);
    public void PlayDeath() => PlayEffect(death);
    public void PlayKeyPickup() => PlayEffect(keyPickup);
    public void PlayGateOpen() => PlayEffect(gateOpen);
    public void PlayWin() => PlayEffect(win);
    public void PlayBouncer() => PlayEffect(bouncer);

    private void PlayButtonTap() => PlayEffect(buttonTap);

    private void PlayEffect(AudioClip clip)
    {
        if (clip != null && effectsSource != null)
            effectsSource.PlayOneShot(clip);
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private void ApplySavedVolumeSettings()
    {
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private void ApplyMusicVolume()
    {
        if (musicSource != null)
            musicSource.volume = IsMusicMuted ? 0f : Mathf.Clamp01(MusicVolume);
    }

    private void ApplySfxVolume()
    {
        float volume = IsSfxMuted ? 0f : Mathf.Clamp01(SfxVolume);

        if (effectsSource != null)
            effectsSource.volume = volume;

        if (walkSource != null)
            walkSource.volume = volume;
    }
}
