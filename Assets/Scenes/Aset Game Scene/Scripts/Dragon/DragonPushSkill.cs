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
    [SerializeField] private float gayaTarik = 500f;

    private PushableObject target;
    private float arahDorong;
    private bool mendorong;
    private bool menarik;
    private bool aksiAktif;
    private float arahTarik;
    private bool sudahPeringatan;

    public override void OnUpdateSkill()
    {
        PushableObject kandidat = null;
        bool aksiDitekan = Masukan.Ditahan;

        if (aksiDitekan &&
            Fisik.TryCariTerdekat(tagPushable, jangkauan, out Collider kol))
        {
            kandidat = kol.GetComponentInParent<PushableObject>();

            if (kandidat == null)
            {
                PeringatanSekali(kol);
            }
            else
            {
                if (Masukan.Horizontal == 0f)
                    kandidat = null;
                else
                    arahTarik = Masukan.Horizontal;
            }
        }

        if (target != kandidat && target != null)
        {
            DragonDebug.Log("Push berhenti");
            target.LepasKendali();
        }

        bool mulaiBaru = target == null && kandidat != null;
        target = kandidat;
        arahDorong = Masukan.Horizontal;
        if (target != null)
        {
            float arahKeObjek = Mathf.Sign(target.transform.position.x - transform.position.x);
            menarik = arahKeObjek != arahTarik;
        }
        else
        {
            menarik = false;
        }

        mendorong = Masukan.Ditahan && target != null && !menarik;

        if (target != null)
        {
            if (mulaiBaru)
                DragonDebug.Log(menarik
                    ? $"Tarik mulai pada '{target.name}'"
                    : $"Push mulai pada '{target.name}'");
            target.AmbilKendali();
        }
        SetMendorong(mendorong || menarik);
    }

    public override void OnFixedUpdateSkill()
    {
        if ((!mendorong && !menarik) || target == null || !target.AdaKendali) return;

        if (menarik)
            Fisik.DorongTarget(target.Body, arahTarik, gayaTarik, kecepatanMaks);
        else
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
        if (aksiAktif == nilai) return;
        aksiAktif = nilai;
        Jaringan.KirimSkillAktif(Tipe, nilai);
    }

    private void PeringatanSekali(Collider kol)
    {
        if (sudahPeringatan) return;
        sudahPeringatan = true;
        Debug.LogWarning($"'{kol.name}' bertag {tagPushable} tetapi tidak punya komponen PushableObject.", kol);
    }
}
