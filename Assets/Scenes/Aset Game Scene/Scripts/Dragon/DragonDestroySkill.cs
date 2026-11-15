using System.Collections;
using UnityEngine;

/// <summary>
/// LEVEL 2 - DESTROY.
/// Tekan E dekat objek bertag "Breakable" -> pukulan (animasi/VFX dikirim ke semua pemain),
/// lalu setelah jeda singkat (sinkron dengan animasi) objek dihancurkan lewat RPC di BreakableObject.
/// </summary>
public class DragonDestroySkill : DragonSkillBase
{
    public override DragonSkillType Tipe => DragonSkillType.Destroy;

    [SerializeField] private string tagBreakable = "Breakable";
    [SerializeField] private float jangkauan = 1.6f;
    [SerializeField] private float cooldown = 0.8f;
    [Tooltip("Jeda antara mulai animasi dan objek benar-benar hancur.")]
    [SerializeField] private float waktuTundaHit = 0.15f;

    private float cooldownSelesai;
    private Coroutine alurPukul;

    /// <summary>1 = baru dipakai, 0 = siap. Berguna untuk UI cooldown.</summary>
    public float CooldownNormal =>
        cooldown <= 0f ? 0f : Mathf.Clamp01((cooldownSelesai - Time.time) / cooldown);

    public override void OnUpdateSkill()
    {
        if (!Masukan.Ditekan || Time.time < cooldownSelesai) return;
        if (!Fisik.TryCariTerdekat(tagBreakable, jangkauan, out Collider kol)) return;

        BreakableObject target = kol.GetComponentInParent<BreakableObject>();
        if (target == null)
        {
            Debug.LogWarning($"'{kol.name}' bertag {tagBreakable} tetapi tidak punya komponen BreakableObject.", kol);
            return;
        }

        cooldownSelesai = Time.time + cooldown;
        Jaringan.KirimPukulan(kol.bounds.ClosestPoint(Fisik.Pusat));
        alurPukul = StartCoroutine(AlurPukul(target));
    }

    private IEnumerator AlurPukul(BreakableObject target)
    {
        yield return new WaitForSeconds(waktuTundaHit);

        if (target != null)
            target.RequestHancurkan();

        alurPukul = null;
    }

    public override void OnKeluar()
    {
        if (alurPukul != null) StopCoroutine(alurPukul);
        alurPukul = null;
    }
}
