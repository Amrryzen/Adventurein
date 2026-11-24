using Photon.Pun;
using UnityEngine;

/// <summary>
/// CONTOH/TES (GDD Level 2: "kumbang agresif di terowongan"): berpatroli kiri-kanan, mematikan pemain
/// yang tersentuh, PINGSAN oleh Hantaman Penuh Dragon (IBisaDistun, lewat RPC ke semua client),
/// dan KABUR dari aura Bioluminesensi Limau (LimauAura). Loop kerja sama Level 2:
/// Limau menerangi + mengusir, Dragon menghantam dan melumpuhkan.
/// Posisi patroli dihitung lokal per client (tidak disinkronkan) -> untuk musuh sungguhan jalankan AI
/// di MasterClient + PhotonTransformView. Butuh Collider trigger (kontak mematikan).
/// </summary>
[RequireComponent(typeof(PhotonView), typeof(Collider))]
public class KumbangContoh : MonoBehaviourPun, IBisaDistun
{
    [SerializeField] private float jangkauanPatroli = 3f;
    [SerializeField] private float kecepatan = 1.5f;
    [SerializeField] private float kecepatanKabur = 3.5f;

    private Vector3 asal;
    private float arah = 1f;
    private float distunSampai;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        asal = transform.position;
    }

    private void Update()
    {
        bool distun = Time.time < distunSampai;

        // Pingsan = terbalik, diam.
        Quaternion target = Quaternion.Euler(0f, 0f, distun ? 180f : 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 10f * Time.deltaTime);
        if (distun) return;

        if (LimauAura.TryCariPengusir(transform.position, out LimauAura aura))
        {
            float menjauh = Mathf.Sign(transform.position.x - aura.transform.position.x);
            transform.position += Vector3.right * (menjauh * kecepatanKabur * Time.deltaTime);
            return;
        }

        transform.position += Vector3.right * (arah * kecepatan * Time.deltaTime);
        float jarak = transform.position.x - asal.x;
        if (Mathf.Abs(jarak) > jangkauanPatroli) arah = -Mathf.Sign(jarak);
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < distunSampai) return;

        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p != null && p.photonView.IsMine) p.Mati();
    }

    public void Distun(float durasi, Vector3 dariTitik)
    {
        photonView.RPC(nameof(RPC_Distun), RpcTarget.All, durasi);
    }

    [PunRPC]
    private void RPC_Distun(float durasi)
    {
        distunSampai = Time.time + durasi;
    }
}
