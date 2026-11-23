using System.Collections;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Dipasang pada objek bertag "Pushable" (kotak/batu dorong).
/// - Pemilik (owner) PhotonView menjalankan fisika; client lain membuat Rigidbody kinematic
///   dan mengikuti data dari PhotonRigidbodyView.
/// - Dragon memanggil AmbilKendali() sebelum mendorong (butuh Ownership = Takeover).
/// Dipolling tiap FixedUpdate agar tetap benar saat owner pindah / master berganti.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(PhotonView))]
public class PushableObject : MonoBehaviourPun
{
    [Tooltip("Side-scroller: kunci rotasi agar kotak tidak terguling.")]
    [SerializeField] private bool kunciRotasi = true;
    [SerializeField, Min(0f)] private float jedaRespawn = 4f;

    private Rigidbody rb;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Vector3 posisiAwal;
    private Quaternion rotasiAwal;
    private Vector3 skalaAwal;
    private bool permintaanKendaliAktif;
    private bool sedangRespawn;

    public Rigidbody Body => rb;
    public bool AdaKendali => photonView.IsMine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        posisiAwal = transform.position;
        rotasiAwal = transform.rotation;
        skalaAwal = transform.localScale;

        RigidbodyConstraints c = RigidbodyConstraints.FreezePositionZ;
        if (kunciRotasi) c |= RigidbodyConstraints.FreezeRotation;
        rb.constraints = c;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        bool harusKinematic = !photonView.IsMine;
        if (rb.isKinematic != harusKinematic)
            rb.isKinematic = harusKinematic;
    }

    public void AmbilKendali()
    {
        if (permintaanKendaliAktif) return;

        // Kirim sekali per usaha dorong agar dua pemain tidak saling merebut kendali.
        permintaanKendaliAktif = true;
        if (!photonView.IsMine)
        {
            DragonDebug.Log($"Meminta ownership '{gameObject.name}'");
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }
    }

    public void LepasKendali()
    {
        permintaanKendaliAktif = false;
    }

    public void RespawnSetelahJatuh()
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RPC_MintaRespawnPushable), RpcTarget.All);
            return;
        }

        MulaiRespawnPushable();
    }

    [PunRPC]
    private void RPC_MintaRespawnPushable()
    {
        if (!photonView.IsMine) return;
        MulaiRespawnPushable();
    }

    private void MulaiRespawnPushable()
    {
        if (sedangRespawn) return;

        sedangRespawn = true;
        photonView.RPC(nameof(RPC_SembunyikanPushable), RpcTarget.All);
        StartCoroutine(RespawnSetelahJeda());
    }

    [PunRPC]
    private void RPC_SembunyikanPushable()
    {
        sedangRespawn = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        foreach (Collider colliderObject in colliders)
            if (colliderObject != null) colliderObject.enabled = false;

        foreach (Renderer rendererObject in renderers)
            if (rendererObject != null) rendererObject.enabled = false;
    }

    private IEnumerator RespawnSetelahJeda()
    {
        yield return new WaitForSeconds(jedaRespawn);

        transform.SetPositionAndRotation(posisiAwal, rotasiAwal);
        transform.localScale = skalaAwal;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = !photonView.IsMine;

        photonView.RPC(
            nameof(RPC_TampilkanPushable),
            RpcTarget.All,
            posisiAwal,
            rotasiAwal);
    }

    [PunRPC]
    private void RPC_TampilkanPushable(Vector3 posisi, Quaternion rotasi)
    {
        transform.SetPositionAndRotation(posisi, rotasi);
        transform.localScale = skalaAwal;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = !photonView.IsMine;

        foreach (Collider colliderObject in colliders)
            if (colliderObject != null) colliderObject.enabled = true;

        foreach (Renderer rendererObject in renderers)
            if (rendererObject != null) rendererObject.enabled = true;

        sedangRespawn = false;
    }
}
