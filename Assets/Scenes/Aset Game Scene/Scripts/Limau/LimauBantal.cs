using UnityEngine;

/// <summary>
/// Efek bantal pantul saat Limau mengembang (Inflate). Berjalan di SEMUA client, tapi hanya
/// client PEMILIK pemain yang mendarat yang memberi pantulan ke karakternya sendiri
/// (pola yang sama dengan Bouncer: p.photonView.IsMine + hanya dari atas).
///
/// Dragon memantul saat MENDARAT di atas Limau, bukan berdiri (puncak kapsul bulat).
/// Tahan Lompat saat mendarat = pantulan lebih tinggi.
/// </summary>
[RequireComponent(typeof(LimauPhysicsHandler), typeof(CapsuleCollider))]
public class LimauBantal : MonoBehaviour
{
    [SerializeField] private float pantulanDasar = 11f;
    [SerializeField] private float pantulanLompat = 15f;
    [SerializeField] private float cooldown = 0.2f;

    private LimauPhysicsHandler fisik;
    private LimauSkillVisuals visuals;
    private CapsuleCollider kol;
    private float cooldownSelesai;

    private void Awake()
    {
        fisik = GetComponent<LimauPhysicsHandler>();
        visuals = GetComponent<LimauSkillVisuals>();
        kol = GetComponent<CapsuleCollider>();
    }

    private void OnCollisionEnter(Collision c)
    {
        if (!fisik.SudahMengembang || Time.time < cooldownSelesai) return;

        PlayerController p = c.collider.GetComponentInParent<PlayerController>();
        if (p == null || p.gameObject == gameObject || !p.photonView.IsMine) return;

        // Harus mendarat dari atas.
        if (c.collider.bounds.min.y < kol.bounds.max.y - 0.2f) return;

        cooldownSelesai = Time.time + cooldown;
        if (visuals != null) visuals.MainkanInflateBounce();

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayBouncer();

        p.Pantul(p.TahanLompat ? pantulanLompat : pantulanDasar);
    }
}
