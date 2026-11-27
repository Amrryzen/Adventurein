using Photon.Pun;
using UnityEngine;

/// <summary>
/// Utilitas bersama: jam jaringan (sinkron antar client) dan pencarian pemain lokal.
/// </summary>
public static class UtilJaringan
{
    private static PlayerController pemainLokal;
    private static float cekBerikutnya;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatik()
    {
        pemainLokal = null;
        cekBerikutnya = 0f;
    }

    public static double Jam =>
        PhotonNetwork.InRoom || PhotonNetwork.OfflineMode ? PhotonNetwork.Time : Time.timeAsDouble;

    /// <summary>Pemain milik client ini. Di-cache; pencarian ulang maksimal tiap 0,5 detik, kecuali paksa = true.</summary>
    public static PlayerController AmbilPemainLokal(bool paksa = false)
    {
        if (pemainLokal != null) return pemainLokal;
        if (!paksa && Time.unscaledTime < cekBerikutnya) return null;

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