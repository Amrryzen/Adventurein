using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Dipasang pada objek bertag "Breakable" (batu yang bisa diremukkan).
/// Penghancuran disinkronkan lewat RPC AllBuffered: pemain yang bergabung belakangan
/// juga akan melihat batu tersebut sudah hancur.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class BreakableObject : MonoBehaviourPun
{
    [Tooltip("Prefab partikel/pecahan lokal (tanpa PhotonView) yang dimunculkan saat hancur.")]
    [SerializeField] private GameObject vfxHancur;

    [Tooltip("Dipanggil di semua client saat objek hancur (mis. buka jalan, mainkan suara).")]
    [SerializeField] private UnityEvent saatHancur;

    [Tooltip("Batu keras: hanya pecah oleh Hantaman Penuh (tahan Q sampai penuh).")]
    [SerializeField] private bool keras;

    private const float TenagaMinimumKeras = 0.99f;

    private bool sudahHancur;
    private bool permintaanHancurTerkirim;
    private bool pengumumanHancurTerkirim;

    /// <summary>Dipanggil Dragon (owner). Mengirim permintaan hancur ke semua client.</summary>
    public void RequestHancurkan(float tenaga = 0f)
    {
        if (sudahHancur || permintaanHancurTerkirim) return;
        if (!BisaDihancurkan(tenaga)) return;

        permintaanHancurTerkirim = true;
        CancelInvoke(nameof(ResetPermintaan));
        Invoke(nameof(ResetPermintaan), 1.5f); // jaga-jaga bila MasterClient menolak
        photonView.RPC(nameof(RPC_MintaHancurkan), RpcTarget.MasterClient, tenaga);
    }

    public bool Keras => keras;

    /// <summary>Batu biasa pecah oleh tenaga berapa pun; batu keras butuh isi penuh.</summary>
    public bool BisaDihancurkan(float tenaga) => !keras || tenaga >= TenagaMinimumKeras;

    private void ResetPermintaan()
    {
        if (!sudahHancur) permintaanHancurTerkirim = false;
    }

    [PunRPC]
    private void RPC_MintaHancurkan(float tenaga, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient ||
            info.Sender == null ||
            sudahHancur ||
            pengumumanHancurTerkirim)
        {
            return;
        }

        if (!PemainAdalahDragon(info.Sender))
            return;

        if (!BisaDihancurkan(tenaga))
            return;

        pengumumanHancurTerkirim = true;
        photonView.RPC(nameof(RPC_Hancurkan), RpcTarget.AllBuffered);
    }

    private static bool PemainAdalahDragon(Photon.Realtime.Player pemain)
    {
        if (pemain == null)
            return false;

        if (!pemain.CustomProperties.TryGetValue(KarakterPemain.Kunci, out object nilai))
            return true;

        return !(nilai is string nama) || string.Equals(nama, "Dragon", System.StringComparison.OrdinalIgnoreCase);
    }

    [PunRPC]
    private void RPC_Hancurkan()
    {
        if (sudahHancur) return;
        sudahHancur = true;
        permintaanHancurTerkirim = true;
        pengumumanHancurTerkirim = true;
        DragonDebug.Log($"'{gameObject.name}' hancur (dikonfirmasi RPC)");

        if (vfxHancur != null)
        {
            Collider kol = GetComponentInChildren<Collider>();
            Vector3 posisi = kol != null ? kol.bounds.center : transform.position;
            Instantiate(vfxHancur, posisi, Quaternion.identity);
        }

        saatHancur?.Invoke();
        Destroy(gameObject); // memicu OnDestroy() pada komponen lain di objek ini
    }
}