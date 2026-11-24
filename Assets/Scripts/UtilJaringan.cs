using Photon.Pun;
using UnityEngine;

/// <summary>
/// Utilitas kecil untuk hazard: jam jaringan (sinkron antar client) dan pencarian pemain lokal.
/// </summary>
public static class UtilJaringan
{
    private static PlayerController pemainLokal;
    private static float cekBerikutnya;

    /// <summary>Jam yang sama di semua client (PhotonNetwork.Time); cadangan Time.timeAsDouble bila offline.</summary>
    public static double Jam =>
        PhotonNetwork.InRoom || PhotonNetwork.OfflineMode ? PhotonNetwork.Time : Time.timeAsDouble;

    /// <summary>Pemain milik client ini (di-cache; pencarian ulang maksimal tiap 0,5 detik).</summary>
    public static PlayerController AmbilPemainLokal()
    {
        if (pemainLokal != null) return pemainLokal;
        if (Time.unscaledTime < cekBerikutnya) return null;

        cekBerikutnya = Time.unscaledTime + 0.5f;
        foreach (PlayerController p in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (p.photonView.IsMine)
            {
                pemainLokal = p;
                break;
            }
        }

        return pemainLokal;
    }
}
