using System.Diagnostics;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public static class DragonDebug
{
    public static bool Aktif = true;

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string pesan, Object konteks = null)
    {
        if (Aktif)
            UnityEngine.Debug.Log("<color=#FFD700>[Dragon]</color> " + pesan, konteks);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Peringatan(string pesan, Object konteks = null)
    {
        if (Aktif)
            UnityEngine.Debug.LogWarning("<color=#FFD700>[Dragon]</color> " + pesan, konteks);
    }

    public static string Label(PhotonView pv)
    {
        if (pv == null)
            return "Unknown";

        if (pv.IsMine)
            return "<color=#55FFFF>Lokal</color>";

        string nama = pv.Owner != null ? pv.Owner.NickName : null;
        if (string.IsNullOrEmpty(nama))
            nama = $"Actor{pv.OwnerActorNr}";

        return $"<color=#FFB347>Remote:{nama}</color>";
    }

    public static string WarnaSkill(DragonSkillType tipe)
    {
        return tipe switch
        {
            DragonSkillType.Push => "<color=#55FFFF>",
            DragonSkillType.Destroy => "<color=#FF9F43>",
            DragonSkillType.SteelSkin => "<color=#C8C8C8>",
            _ => "<color=#FFD700>"
        };
    }
}
