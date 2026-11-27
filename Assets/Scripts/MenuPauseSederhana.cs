using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuPauseSederhana : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle musicMuteToggle;
    [SerializeField] private Toggle sfxMuteToggle;

    private bool initialized;
    private TMP_Text labelTmp;
    private Text labelLegacy;
    private string labelAsli;

    public bool IsReady => initialized;

    private void Awake()
    {
        if (!ResolveUi())
            return;

        initialized = true;
        panel.SetActive(false);
        PlayerController.InputDikunci = false;
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        resumeButton.onClick.AddListener(Lanjut);
        restartButton.onClick.AddListener(RestartStage);
        exitButton.onClick.AddListener(Keluar);
        musicSlider.onValueChanged.AddListener(TerapkanMusic);
        sfxSlider.onValueChanged.AddListener(TerapkanSfx);
        musicMuteToggle.onValueChanged.AddListener(TerapkanMuteMusic);
        sfxMuteToggle.onValueChanged.AddListener(TerapkanMuteSfx);
    }

    private void Start()
    {
        if (!initialized)
            return;

        labelTmp = restartButton.GetComponentInChildren<TMP_Text>();
        labelLegacy = restartButton.GetComponentInChildren<Text>();
        labelAsli = labelTmp != null ? labelTmp.text : (labelLegacy != null ? labelLegacy.text : null);

        GameAudio audio = GameAudio.Instance;
        musicSlider.SetValueWithoutNotify(audio != null ? audio.MusicVolume : PlayerPrefs.GetFloat(GameAudio.MusicVolumePref, 0.15f));
        sfxSlider.SetValueWithoutNotify(audio != null ? audio.SfxVolume : PlayerPrefs.GetFloat(GameAudio.SfxVolumePref, 0.15f));
        musicMuteToggle.SetIsOnWithoutNotify(audio != null ? audio.IsMusicMuted : PlayerPrefs.GetInt(GameAudio.MusicMutedPref, 0) == 1);
        sfxMuteToggle.SetIsOnWithoutNotify(audio != null ? audio.IsSfxMuted : PlayerPrefs.GetInt(GameAudio.SfxMutedPref, 0) == 1);
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            AturPause(!panel.activeSelf);

        if (panel.activeSelf)
            PerbaruiLabelRestart();
    }

    private void PerbaruiLabelRestart()
    {
        if (labelAsli == null)
            return;

        PersetujuanRestart.Hitung(out int setuju, out int total);
        string teks = setuju > 0 && total > 1 ? $"{labelAsli} ({setuju}/{total})" : labelAsli;

        if (labelTmp != null) labelTmp.text = teks;
        else if (labelLegacy != null) labelLegacy.text = teks;
    }

    private void OnDisable()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Lanjut);
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartStage);
        if (exitButton != null) exitButton.onClick.RemoveListener(Keluar);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(TerapkanMusic);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(TerapkanSfx);
        if (musicMuteToggle != null) musicMuteToggle.onValueChanged.RemoveListener(TerapkanMuteMusic);
        if (sfxMuteToggle != null) sfxMuteToggle.onValueChanged.RemoveListener(TerapkanMuteSfx);

        PlayerController.InputDikunci = false;
    }

    private bool ResolveUi()
    {
        if (panel == null)
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Transform kandidat = canvas.transform.Find("P_Pause");
                if (kandidat == null)
                    continue;

                panel = kandidat.gameObject;
                break;
            }
        }

        if (panel == null)
            return false;

        resumeButton = Cari(resumeButton, "B_Resume");
        restartButton = Cari(restartButton, "B_Restart");
        exitButton = Cari(exitButton, "B_Exit");
        musicSlider = Cari(musicSlider, "Slider");
        sfxSlider = Cari(sfxSlider, "Slider_SFX");
        musicMuteToggle = Cari(musicMuteToggle, "Toggle_MuteBGM");
        sfxMuteToggle = Cari(sfxMuteToggle, "Toggle_MuteSFX");

        if (resumeButton == null || restartButton == null || exitButton == null ||
            musicSlider == null || sfxSlider == null ||
            musicMuteToggle == null || sfxMuteToggle == null)
        {
            Debug.LogError("Pause Canvas belum lengkap. Pastikan P_Pause memiliki tiga tombol, dua slider, dan dua toggle mute.", this);
            return false;
        }

        return true;
    }

    private T Cari<T>(T komponen, string nama) where T : Component
    {
        if (komponen != null)
            return komponen;

        Transform anak = panel.transform.Find(nama);
        return anak != null ? anak.GetComponent<T>() : null;
    }

    // Multiplayer: pause hanya menampilkan menu, dunia tetap berjalan.
    private void AturPause(bool buka)
    {
        panel.SetActive(buka);
        PlayerController.InputDikunci = buka;
    }

    private void Lanjut() => AturPause(false);

    private void TerapkanMusic(float volume)
    {
        if (GameAudio.Instance != null) GameAudio.Instance.SetMusicVolume(volume);
        else SimpanFloat(GameAudio.MusicVolumePref, volume);
    }

    private void TerapkanSfx(float volume)
    {
        if (GameAudio.Instance != null) GameAudio.Instance.SetSfxVolume(volume);
        else SimpanFloat(GameAudio.SfxVolumePref, volume);
    }

    private void TerapkanMuteMusic(bool muted)
    {
        if (GameAudio.Instance != null) GameAudio.Instance.SetMusicMuted(muted);
        else SimpanInt(GameAudio.MusicMutedPref, muted ? 1 : 0);
    }

    private void TerapkanMuteSfx(bool muted)
    {
        if (GameAudio.Instance != null) GameAudio.Instance.SetSfxMuted(muted);
        else SimpanInt(GameAudio.SfxMutedPref, muted ? 1 : 0);
    }

    private static void SimpanFloat(string kunci, float nilai)
    {
        PlayerPrefs.SetFloat(kunci, Mathf.Clamp01(nilai));
        PlayerPrefs.Save();
    }

    private static void SimpanInt(string kunci, int nilai)
    {
        PlayerPrefs.SetInt(kunci, nilai);
        PlayerPrefs.Save();
    }

    private void RestartStage() => PersetujuanRestart.MintaAtauSetujui();

    private void Keluar()
    {
        PlayerController.InputDikunci = false;
        Application.Quit();
    }
}