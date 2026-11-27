using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restart stage butuh persetujuan semua pemain. Persetujuan pemain mengacu pada ID permintaan room yang sama.
/// Setelah semua setuju, scene dimuat ulang sehingga seluruh progres stage kembali ke awal.
/// </summary>
public class PersetujuanRestart : MonoBehaviourPunCallbacks
{
    private const string Kunci = "restart";
    public const string KunciPermintaanRoom = "restartRequestId";
    public const string KunciPemintaRoom = "restartRequester";

    private static PersetujuanRestart instance;
    private bool memuat;
    private bool aksiMenungguPropertiRoom;
    private string permintaanMenungguKonfirmasi;
    private GUIStyle gaya;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Buat()
    {
        if (instance != null) return;

        GameObject objek = new GameObject("PersetujuanRestart");
        DontDestroyOnLoad(objek);
        instance = objek.AddComponent<PersetujuanRestart>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static bool SayaSetuju
    {
        get
        {
            string idPermintaan = AmbilIdPermintaan();
            return !string.IsNullOrEmpty(idPermintaan) &&
                Setuju(PhotonNetwork.LocalPlayer, idPermintaan);
        }
    }

    public static void Hitung(out int setuju, out int total)
    {
        setuju = 0;
        total = 0;
        if (!PhotonNetwork.InRoom) return;

        string idPermintaan = AmbilIdPermintaan();
        if (string.IsNullOrEmpty(idPermintaan)) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            total++;
            if (Setuju(p, idPermintaan)) setuju++;
        }
    }

    /// <summary>Memulai permintaan atau menyetujui permintaan restart yang sedang aktif.</summary>
    public static void MintaAtauSetujui()
    {
        if (!PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (instance == null)
        {
            Debug.LogError("Pengelola persetujuan restart belum dibuat.");
            return;
        }

        instance.MintaAtauSetujuiLokal();
    }

    private static string AmbilIdPermintaan()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null ||
            !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KunciPermintaanRoom, out object nilai))
            return null;

        return nilai as string;
    }

    private static int AmbilPeminta()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null ||
            !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KunciPemintaRoom, out object nilai) ||
            !(nilai is int actorNumber))
            return 0;

        return actorNumber;
    }

    private static bool Setuju(Player pemain, string idPermintaan)
    {
        return pemain != null &&
            pemain.CustomProperties.TryGetValue(Kunci, out object nilai) &&
            nilai is string idSetuju &&
            idSetuju == idPermintaan;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += SaatSceneDimuat;
        PastikanPropertiRoom();
    }

    public override void OnJoinedRoom()
    {
        PastikanPropertiRoom();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= SaatSceneDimuat;
    }

    public override void OnLeftRoom()
    {
        aksiMenungguPropertiRoom = false;
        permintaanMenungguKonfirmasi = null;
        memuat = false;
    }

    private void SaatSceneDimuat(Scene scene, LoadSceneMode mode)
    {
        string idPermintaan = PhotonNetwork.InRoom ? AmbilIdPermintaan() : null;
        memuat = !string.IsNullOrEmpty(idPermintaan);
        CancelInvoke();
        aksiMenungguPropertiRoom = false;
        permintaanMenungguKonfirmasi = null;

        if (!PhotonNetwork.InRoom)
        {
            memuat = false;
            return;
        }

        AturPersetujuan(string.Empty);

        if (PhotonNetwork.IsMasterClient && !string.IsNullOrEmpty(idPermintaan))
            AturPermintaanRoom(string.Empty, 0, idPermintaan);
    }

    private void PastikanPropertiRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        Hashtable propertiAwal = new Hashtable();
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KunciPermintaanRoom))
            propertiAwal[KunciPermintaanRoom] = string.Empty;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KunciPemintaRoom))
            propertiAwal[KunciPemintaRoom] = 0;

        if (propertiAwal.Count > 0 &&
            !PhotonNetwork.CurrentRoom.SetCustomProperties(propertiAwal))
            Debug.LogError("Gagal menginisialisasi properti persetujuan restart pada room.");
    }

    public override void OnPlayerPropertiesUpdate(Player target, Hashtable berubah)
    {
        if (berubah.ContainsKey(Kunci)) Periksa();
    }

    public override void OnRoomPropertiesUpdate(Hashtable berubah)
    {
        if (!berubah.ContainsKey(KunciPermintaanRoom))
            return;

        string idPermintaan = AmbilIdPermintaan();
        if (aksiMenungguPropertiRoom)
        {
            aksiMenungguPropertiRoom = false;
            MintaAtauSetujuiLokal();
        }
        else if (!string.IsNullOrEmpty(permintaanMenungguKonfirmasi))
        {
            permintaanMenungguKonfirmasi = null;
            if (!string.IsNullOrEmpty(idPermintaan))
                AturPersetujuan(idPermintaan);
        }
        else if (string.IsNullOrEmpty(idPermintaan))
        {
            memuat = false;
            AturPersetujuan(string.Empty);
        }

        Periksa();
    }

    public override void OnPlayerLeftRoom(Player pemainKeluar)
    {
        if (PhotonNetwork.IsMasterClient && AmbilPeminta() == pemainKeluar.ActorNumber)
        {
            string idPermintaan = AmbilIdPermintaan();
            if (!string.IsNullOrEmpty(idPermintaan))
                AturPermintaanRoom(string.Empty, 0, idPermintaan);
        }

        Periksa();
    }

    public override void OnMasterClientSwitched(Player masterClientBaru)
    {
        Periksa();
    }

    private void MintaAtauSetujuiLokal()
    {
        if (memuat || aksiMenungguPropertiRoom || !string.IsNullOrEmpty(permintaanMenungguKonfirmasi))
            return;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KunciPermintaanRoom) ||
            !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KunciPemintaRoom))
        {
            aksiMenungguPropertiRoom = true;
            PastikanPropertiRoom();
            return;
        }

        string idPermintaan = AmbilIdPermintaan();
        if (string.IsNullOrEmpty(idPermintaan))
        {
            MulaiPermintaan();
            return;
        }

        if (!SayaSetuju)
        {
            AturPersetujuan(idPermintaan);
            return;
        }

        if (AmbilPeminta() == PhotonNetwork.LocalPlayer.ActorNumber)
            AturPermintaanRoom(string.Empty, 0, idPermintaan);
        else
            AturPersetujuan(string.Empty);
    }

    private void MulaiPermintaan()
    {
        string idPermintaan = Guid.NewGuid().ToString("N");
        permintaanMenungguKonfirmasi = idPermintaan;

        Hashtable perubahan = new Hashtable
        {
            { KunciPermintaanRoom, idPermintaan },
            { KunciPemintaRoom, PhotonNetwork.LocalPlayer.ActorNumber }
        };
        Hashtable harapan = new Hashtable
        {
            { KunciPermintaanRoom, string.Empty }
        };

        if (!PhotonNetwork.CurrentRoom.SetCustomProperties(perubahan, harapan))
        {
            permintaanMenungguKonfirmasi = null;
            Debug.LogError("Gagal mengirim permintaan restart ke room.");
        }
    }

    private void AturPermintaanRoom(string idPermintaanBaru, int pemintaBaru, string idPermintaanSaatIni)
    {
        Hashtable perubahan = new Hashtable
        {
            { KunciPermintaanRoom, idPermintaanBaru },
            { KunciPemintaRoom, pemintaBaru }
        };
        Hashtable harapan = new Hashtable
        {
            { KunciPermintaanRoom, idPermintaanSaatIni }
        };

        if (!PhotonNetwork.CurrentRoom.SetCustomProperties(perubahan, harapan))
            Debug.LogError("Gagal memperbarui permintaan restart pada room.");
    }

    private void AturPersetujuan(string idPermintaan)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return;

        if (!PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { Kunci, idPermintaan } }))
        {
            Debug.LogError("Gagal mengirim persetujuan restart.");
            return;
        }

        Periksa();
    }

    private void Periksa()
    {
        if (memuat || !PhotonNetwork.InRoom || string.IsNullOrEmpty(AmbilIdPermintaan())) return;

        Hitung(out int setuju, out int total);
        if (total == 0 || setuju < total) return;

        memuat = true;
        Invoke(nameof(ResetMemuat), 5f);

        PlayerController.InputDikunci = false;
        PlayerController.PermainanSelesai = false;
        PhotonNetwork.RemoveBufferedRPCs();

        // Dengan AutomaticallySyncScene, hanya master yang memuat; client lain mengikuti otomatis.
        if (PhotonNetwork.OfflineMode || !PhotonNetwork.AutomaticallySyncScene || PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);
    }

    private void ResetMemuat() => memuat = false;

    private void OnGUI()
    {
        if (!PhotonNetwork.InRoom || memuat || string.IsNullOrEmpty(AmbilIdPermintaan())) return;

        Hitung(out int setuju, out int total);
        if (total < 2 || setuju == 0) return;

        if (gaya == null)
        {
            gaya = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            gaya.normal.textColor = Color.white;
        }

        string teks = SayaSetuju
            ? $"Menunggu pemain lain untuk restart ({setuju}/{total})"
            : $"Pemain lain minta restart ({setuju}/{total}). Tekan Esc lalu Restart untuk setuju";

        GUI.Label(new Rect(0f, 16f, Screen.width, 28f), teks, gaya);
    }
}