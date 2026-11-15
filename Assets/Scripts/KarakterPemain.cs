using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public static class KarakterPemain
{
    public const string Kunci = "karakter";

    public static void Set(string namaPrefab)
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { Kunci, namaPrefab }
        });
    }

    public static void SetUntuk(Player pemain, string namaPrefab)
    {
        pemain.SetCustomProperties(new Hashtable
        {
            { Kunci, namaPrefab }
        });
    }

    public static string Ambil(Player pemain, string fallback)
    {
        if (pemain == null ||
            !pemain.CustomProperties.TryGetValue(Kunci, out object nilai) ||
            !(nilai is string namaPrefab) ||
            string.IsNullOrEmpty(namaPrefab))
        {
            return fallback;
        }

        return namaPrefab;
    }
}
