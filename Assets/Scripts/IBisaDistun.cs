using UnityEngine;

/// <summary>
/// Musuh/hama yang bisa dipingsankan Hantaman Penuh Dragon atau parry Steel Skin.
/// Dipanggil di client Dragon. Bila musuh disinkronkan lewat Photon, implementasinya
/// HARUS meneruskan lewat RPC ke semua client (lihat KumbangContoh / BurungPelatuk).
/// Collider musuh harus non-trigger agar terdeteksi Hantaman Penuh.
/// </summary>
public interface IBisaDistun
{
    void Distun(float durasi, Vector3 dariTitik);
}
