using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Photon.Pun;

public class MenuPauseSederhana : MonoBehaviour
{
    private bool terbuka;
    private GUIStyle gayaJudul;
    private GUIStyle gayaTombol;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private bool musicMuted;
    private bool sfxMuted;
    private bool sedangRestart;
    private bool musicMutedTerakhir;
    private bool sfxMutedTerakhir;

    private void Awake()
    {
        musicVolume = PlayerPrefs.GetFloat(GameAudio.MusicVolumePref, 1f);
        sfxVolume = PlayerPrefs.GetFloat(GameAudio.SfxVolumePref, 1f);
        musicMuted = PlayerPrefs.GetInt(GameAudio.MusicMutedPref, 0) == 1;
        sfxMuted = PlayerPrefs.GetInt(GameAudio.SfxMutedPref, 0) == 1;
        musicMutedTerakhir = musicMuted;
        sfxMutedTerakhir = sfxMuted;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            AturPause(!terbuka);
    }

    private void AturPause(bool buka)
    {
        terbuka = buka;
        PlayerController.InputDikunci = buka;
        Time.timeScale = buka ? 0f : 1f;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        PlayerController.InputDikunci = false;
    }

    private void TerapkanMusic()
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.SetMusicVolume(musicVolume);
        else
            PlayerPrefs.SetFloat(GameAudio.MusicVolumePref, musicVolume);
    }

    private void TerapkanSfx()
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.SetSfxVolume(sfxVolume);
        else
            PlayerPrefs.SetFloat(GameAudio.SfxVolumePref, sfxVolume);
    }

    private void TerapkanMuteMusic()
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.SetMusicMuted(musicMuted);
        else
        {
            PlayerPrefs.SetInt(GameAudio.MusicMutedPref, musicMuted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void TerapkanMuteSfx()
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.SetSfxMuted(sfxMuted);
        else
        {
            PlayerPrefs.SetInt(GameAudio.SfxMutedPref, sfxMuted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void RestartStage()
    {
        sedangRestart = true;
        StartCoroutine(RestartSetelahPause());
    }

    private IEnumerator RestartSetelahPause()
    {
        Time.timeScale = 1f;
        PlayerController.InputDikunci = false;
        PlayerController.PermainanSelesai = false;

        PlayerController pemainLokal = null;
        foreach (PlayerController pemain in FindObjectsByType<PlayerController>(
                     FindObjectsSortMode.None))
        {
            if (pemain != null && pemain.photonView.IsMine)
            {
                pemainLokal = pemain;
                break;
            }
        }

        if (pemainLokal != null)
        {
            pemainLokal.MintaRestartStage();
        }
        else
        {
            string namaScene = SceneManager.GetActiveScene().name;
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LoadLevel(namaScene);
            else
                SceneManager.LoadScene(namaScene);
        }

        yield break;
    }

    private void OnGUI()
    {
        if (!terbuka)
            return;

        if (gayaJudul == null)
        {
            gayaJudul = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
        }

        if (gayaTombol == null)
        {
            gayaTombol = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18
            };
        }

        float lebar = 320f;
        float tinggi = 390f;
        Rect panel = new Rect(
            (Screen.width - lebar) * 0.5f,
            (Screen.height - tinggi) * 0.5f,
            lebar,
            tinggi);

        GUI.Box(panel, "");
        GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 40f), "PAUSE", gayaJudul);

        GUI.Label(new Rect(panel.x + 40f, panel.y + 70f, panel.width - 80f, 20f), "Volume BGM");
        float nilaiMusic = GUI.HorizontalSlider(new Rect(panel.x + 40f, panel.y + 92f, panel.width - 80f, 18f), musicVolume, 0f, 1f);
        if (Mathf.Abs(nilaiMusic - musicVolume) > 0.001f)
        {
            musicVolume = nilaiMusic;
            TerapkanMusic();
        }

        musicMuted = GUI.Toggle(new Rect(panel.x + 40f, panel.y + 112f, panel.width - 80f, 22f), musicMuted, "Mute BGM");
        if (musicMuted != musicMutedTerakhir)
        {
            musicMutedTerakhir = musicMuted;
            TerapkanMuteMusic();
        }

        GUI.Label(new Rect(panel.x + 40f, panel.y + 142f, panel.width - 80f, 20f), "Volume SFX");
        float nilaiSfx = GUI.HorizontalSlider(new Rect(panel.x + 40f, panel.y + 164f, panel.width - 80f, 18f), sfxVolume, 0f, 1f);
        if (Mathf.Abs(nilaiSfx - sfxVolume) > 0.001f)
        {
            sfxVolume = nilaiSfx;
            TerapkanSfx();
        }

        sfxMuted = GUI.Toggle(new Rect(panel.x + 40f, panel.y + 184f, panel.width - 80f, 22f), sfxMuted, "Mute SFX");
        if (sfxMuted != sfxMutedTerakhir)
        {
            sfxMutedTerakhir = sfxMuted;
            TerapkanMuteSfx();
        }

        if (GUI.Button(new Rect(panel.x + 60f, panel.y + 218f, panel.width - 120f, 36f), "Lanjut", gayaTombol))
            AturPause(false);

        if (!sedangRestart && GUI.Button(new Rect(panel.x + 60f, panel.y + 264f, panel.width - 120f, 36f), "Restart Stage", gayaTombol))
            RestartStage();

        if (GUI.Button(new Rect(panel.x + 60f, panel.y + 350f, panel.width - 120f, 36f), "Keluar", gayaTombol))
        {
            Time.timeScale = 1f;
            Application.Quit();
        }
    }
}
