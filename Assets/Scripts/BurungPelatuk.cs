using System.Collections;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// GDD Level 3: "Burung Pelatuk lapar". Menyambar horizontal sepanjang satu jalur; siapa pun yang
/// PERTAMA terkena di jalur itu menerima serangan, dan benda padat memblokir jalur.
/// - Dragon dengan Steel Skin aktif kebal, sehingga Limau di belakangnya aman.
/// - PARRY: Steel Skin yang baru diaktifkan tepat sebelum sambaran (jendela parry) membuat burung
///   pingsan beberapa detik dan mengembalikan sebagian stamina.
/// - Selain itu korban mati lewat PlayerController.Mati() (aturan GDD: kedua pemain respawn bersamaan).
///
/// Jaringan: MasterClient memutuskan kapan menyerang (RPC ke semua). Semua client menganimasikan
/// sambaran dari jam jaringan yang sama; tiap client HANYA mengeksekusi akibat untuk pemain lokalnya.
/// Visual placeholder dibuat otomatis (bola + paruh) bila 'model' kosong.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class BurungPelatuk : MonoBehaviourPunCallbacks, IBisaDistun
{
    [Header("Jadwal (MasterClient yang memutuskan)")]
    [SerializeField] private float jedaMin = 4f;
    [SerializeField] private float jedaMaks = 7f;
    [Tooltip("Telegraf: jalur merah berkedip sebelum burung menyambar.")]
    [SerializeField] private float waktuPeringatan = 1.6f;
    [SerializeField] private float waktuDash = 0.45f;

    [Header("Jalur serang (berpusat di posisi objek ini)")]
    [SerializeField] private float lebarJalur = 20f;
    [SerializeField] private float radiusSerang = 0.45f;
    [Tooltip("Layer yang diperiksa jalur serang (pemain + penghalang).")]
    [SerializeField] private LayerMask lapisSerang = ~0;

    [Header("Parry")]
    [SerializeField] private float durasiPingsan = 4f;

    [Header("Visual (opsional)")]
    [SerializeField] private Transform model;

    private Vector3 posisiBertengger;
    private Transform bar;
    private Renderer barRenderer;
    private bool sedangMenyerang;
    private bool dibatalkan;
    private float pingsanSampai;
    private Coroutine loopMaster;
    private Coroutine alurSerang;
    private readonly RaycastHit[] hits = new RaycastHit[16];

    private void Start()
    {
        posisiBertengger = transform.position;
        if (model == null) model = BuatModelDefault();
        model.position = posisiBertengger;

        GameObject objekBar = FabrikEfek.BuatPrimitif(PrimitiveType.Cube, "Peringatan Patuk", new Color(1f, 0.2f, 0.15f, 0.45f), null);
        bar = objekBar.transform;
        barRenderer = objekBar.GetComponent<Renderer>();
        objekBar.SetActive(false);

        if (PhotonNetwork.IsMasterClient) loopMaster = StartCoroutine(LoopMaster());
    }

    private void OnDestroy()
    {
        if (bar != null) Destroy(bar.gameObject);
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player masterBaru)
    {
        if (PhotonNetwork.IsMasterClient && loopMaster == null)
            loopMaster = StartCoroutine(LoopMaster());
    }

    // ---------------- Keputusan (MasterClient) ----------------

    private IEnumerator LoopMaster()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(jedaMin, jedaMaks));
            while (sedangMenyerang || Time.time < pingsanSampai) yield return null;

            if (!PhotonNetwork.IsMasterClient)
            {
                loopMaster = null;
                yield break;
            }

            if (!TryPilihTarget(out float y)) continue;

            int arah = Random.value < 0.5f ? -1 : 1;
            photonView.RPC(nameof(RPC_Serang), RpcTarget.All, y, arah, UtilJaringan.Jam);
            yield return null;
        }
    }

    private bool TryPilihTarget(out float y)
    {
        y = 0f;

        PlayerController[] semua = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        PlayerController terpilih = null;
        int dalamJangkauan = 0;

        foreach (PlayerController p in semua)
        {
            if (Mathf.Abs(p.transform.position.x - transform.position.x) > lebarJalur * 0.5f) continue;

            dalamJangkauan++;
            if (Random.Range(0, dalamJangkauan) == 0) terpilih = p; // pilih acak seragam
        }

        if (terpilih == null) return false;

        Collider kol = terpilih.GetComponent<Collider>();
        y = kol != null ? kol.bounds.center.y : terpilih.transform.position.y;
        return true;
    }

    // ---------------- Animasi (semua client) ----------------

    [PunRPC]
    private void RPC_Serang(float y, int arah, double tMulai, PhotonMessageInfo info)
    {
        if (info.Sender != null && !info.Sender.IsMasterClient) return;

        if (alurSerang != null) StopCoroutine(alurSerang);
        alurSerang = StartCoroutine(AlurSerang(y, arah, tMulai));
    }

    private IEnumerator AlurSerang(float y, int arah, double tMulai)
    {
        sedangMenyerang = true;
        dibatalkan = false;

        float z = transform.position.z;
        float cx = transform.position.x;
        Vector3 awal = new Vector3(cx - arah * lebarJalur * 0.5f, y, z);
        Vector3 akhir = new Vector3(cx + arah * lebarJalur * 0.5f, y, z);
        double tDash = tMulai + waktuPeringatan;

        model.rotation = Quaternion.Euler(0f, arah > 0 ? 0f : 180f, 0f);
        TampilkanBar(awal, akhir, true);

        // Fase peringatan: burung terbang ke posisi awal, jalur berkedip.
        Vector3 dariBertengger = model.position;
        while (UtilJaringan.Jam < tDash && !dibatalkan)
        {
            float p = Mathf.Clamp01((float)((UtilJaringan.Jam - tMulai) / (waktuPeringatan * 0.7f)));
            model.position = Vector3.Lerp(dariBertengger, awal, Mathf.SmoothStep(0f, 1f, p));
            KedipBar();
            yield return null;
        }

        TampilkanBar(awal, akhir, false);

        // Fase sambaran. Dampak dievaluasi sekali di tengah sambaran.
        bool sudahEvaluasi = false;
        while (!dibatalkan)
        {
            float p = (float)((UtilJaringan.Jam - tDash) / waktuDash);
            if (p >= 1f) break;

            p = Mathf.Max(0f, p);
            model.position = Vector3.Lerp(awal, akhir, p);

            if (!sudahEvaluasi && p >= 0.5f)
            {
                sudahEvaluasi = true;
                EvaluasiSerangan(awal, akhir);
            }

            yield return null;
        }

        if (dibatalkan) yield return AlurJatuh();
        else yield return AlurKembali();

        sedangMenyerang = false;
        alurSerang = null;
    }

    private IEnumerator AlurKembali()
    {
        Vector3 dari = model.position;
        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            model.position = Vector3.Lerp(dari, posisiBertengger, Mathf.SmoothStep(0f, 1f, t / 0.8f));
            yield return null;
        }
    }

    private IEnumerator AlurJatuh()
    {
        TampilkanBar(Vector3.zero, Vector3.zero, false);

        Vector3 dari = model.position;
        float t = 0f;
        while (t < 0.9f)
        {
            t += Time.deltaTime;
            model.position = Vector3.Lerp(dari, posisiBertengger, Mathf.SmoothStep(0f, 1f, t / 0.9f));
            model.Rotate(0f, 0f, 720f * Time.deltaTime, Space.World);
            yield return null;
        }
    }

    private void Update()
    {
        if (model == null || sedangMenyerang) return;

        bool pingsan = Time.time < pingsanSampai;
        Quaternion target = Quaternion.Euler(0f, 0f, pingsan ? 80f : 0f);
        model.rotation = Quaternion.Slerp(model.rotation, target, 8f * Time.deltaTime);

        Vector3 tujuan = posisiBertengger;
        if (!pingsan) tujuan.y += Mathf.Sin(Time.time * 2f) * 0.12f;
        model.position = Vector3.Lerp(model.position, tujuan, 6f * Time.deltaTime);
    }

    // ---------------- Dampak (tiap client untuk pemain lokalnya) ----------------

    private void EvaluasiSerangan(Vector3 awal, Vector3 akhir)
    {
        if (Time.time < pingsanSampai) return;

        Vector3 arah = akhir - awal;
        float panjang = arah.magnitude;
        arah /= panjang;

        int jumlah = Physics.SphereCastNonAlloc(
            awal, radiusSerang, arah, hits, panjang, lapisSerang, QueryTriggerInteraction.Ignore);

        float jarakKorban = float.MaxValue;
        float jarakPenghalang = float.MaxValue;
        PlayerController korban = null;

        for (int i = 0; i < jumlah; i++)
        {
            Collider c = hits[i].collider;
            if (c.transform.IsChildOf(transform)) continue;

            float jarak = hits[i].distance;
            PlayerController p = c.GetComponentInParent<PlayerController>();

            if (p == null)
            {
                if (jarak < jarakPenghalang) jarakPenghalang = jarak; // benda dunia memblokir jalur
            }
            else if (jarak < jarakKorban)
            {
                jarakKorban = jarak;
                korban = p;
            }
        }

        if (korban == null || jarakPenghalang < jarakKorban) return; // meleset / terhalang

        DragonSkillNetwork dragon = korban.GetComponent<DragonSkillNetwork>();
        bool berbaja = dragon != null && dragon.IsAktif(DragonSkillType.SteelSkin);

        if (berbaja)
        {
            // Hanya client Dragon yang menilai blok/parry untuk dirinya sendiri.
            if (korban.photonView.IsMine)
            {
                DragonSteelSkinSkill baja = korban.GetComponent<DragonSteelSkinSkill>();
                if (baja != null && baja.Terima(korban.transform.position) == DragonSteelSkinSkill.HasilBlok.Parry)
                    Distun(durasiPingsan, korban.transform.position);
            }

            return;
        }

        if (korban.photonView.IsMine)
            korban.Mati();
    }

    // ---------------- IBisaDistun ----------------

    public void Distun(float durasi, Vector3 dariTitik)
    {
        photonView.RPC(nameof(RPC_Pingsan), RpcTarget.All, durasi);
    }

    [PunRPC]
    private void RPC_Pingsan(float durasi)
    {
        pingsanSampai = Time.time + durasi;
        if (sedangMenyerang) dibatalkan = true;
    }

    // ---------------- Visual ----------------

    private void TampilkanBar(Vector3 awal, Vector3 akhir, bool tampil)
    {
        if (bar == null) return;

        bar.gameObject.SetActive(tampil);
        if (!tampil) return;

        barRenderer.enabled = true;
        bar.position = (awal + akhir) * 0.5f;
        bar.localScale = new Vector3(Mathf.Abs(akhir.x - awal.x), radiusSerang * 2f, 0.05f);
    }

    private void KedipBar()
    {
        if (barRenderer != null)
            barRenderer.enabled = Mathf.PingPong(Time.time * 5f, 1f) > 0.35f;
    }

    private Transform BuatModelDefault()
    {
        GameObject badan = FabrikEfek.BuatPrimitif(PrimitiveType.Sphere, "Burung Pelatuk", new Color(0.9f, 0.75f, 0.2f, 1f), transform);
        badan.transform.localScale = Vector3.one * 1.2f;

        GameObject paruh = FabrikEfek.BuatPrimitif(PrimitiveType.Cube, "Paruh", new Color(1f, 0.45f, 0.15f, 1f), badan.transform);
        paruh.transform.localPosition = new Vector3(0.65f, 0f, 0f);
        paruh.transform.localScale = new Vector3(0.6f, 0.2f, 0.2f);

        return badan.transform;
    }
}
