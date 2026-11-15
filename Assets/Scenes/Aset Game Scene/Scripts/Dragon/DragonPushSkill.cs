using UnityEngine;

/// <summary>
/// LEVEL 1 - PUSH.
/// Tahan E + bergerak ke arah objek bertag "Pushable" -> beri gaya dorong fisik tambahan.
/// Dragon mengambil kepemilikan (ownership) objek lebih dulu agar fisika dijalankan di client Dragon,
/// lalu PhotonRigidbodyView menyebarkan posisinya ke pemain lain.
/// </summary>
public class DragonPushSkill : DragonSkillBase
{
    public override DragonSkillType Tipe => DragonSkillType.Push;

    [SerializeField] private string tagPushable = "Pushable";
    [SerializeField] private float jangkauan = 1.2f;
    [Tooltip("Newton. Kira-kira massa objek x percepatan yang diinginkan.")]
    [SerializeField] private float gayaDorong = 600f;
    [SerializeField] private float kecepatanMaks = 2.5f;

    private PushableObject target;
    private float arahDorong;
    private bool mendorong;
    private bool sudahPeringatan;

    public override void OnUpdateSkill()
    {
        PushableObject kandidat = null;

        if (Masukan.Ditahan && Masukan.Horizontal != 0f &&
            Fisik.TryCariTerdekat(tagPushable, jangkauan, out Collider kol))
        {
            kandidat = kol.GetComponentInParent<PushableObject>();

            if (kandidat == null)
            {
                PeringatanSekali(kol);
            }
            else
            {
                // Hanya mendorong jika Dragon berjalan MENUJU objek.
                float arahKeObjek = Mathf.Sign(kandidat.transform.position.x - transform.position.x);
                if (arahKeObjek != Masukan.Horizontal) kandidat = null;
            }
        }

        if (target != kandidat && target != null)
            target.LepasKendali();

        target = kandidat;
        arahDorong = Masukan.Horizontal;

        if (target != null) target.AmbilKendali();
        SetMendorong(target != null);
    }

    public override void OnFixedUpdateSkill()
    {
        if (!mendorong || target == null || !target.AdaKendali) return;
        Fisik.DorongTarget(target.Body, arahDorong, gayaDorong, kecepatanMaks);
    }

    public override void OnKeluar()
    {
        if (target != null) target.LepasKendali();
        target = null;
        SetMendorong(false);
    }

    private void SetMendorong(bool nilai)
    {
        if (mendorong == nilai) return;
        mendorong = nilai;
        Jaringan.KirimSkillAktif(Tipe, nilai);
    }

    private void PeringatanSekali(Collider kol)
    {
        if (sudahPeringatan) return;
        sudahPeringatan = true;
        Debug.LogWarning($"'{kol.name}' bertag {tagPushable} tetapi tidak punya komponen PushableObject.", kol);
    }
}
