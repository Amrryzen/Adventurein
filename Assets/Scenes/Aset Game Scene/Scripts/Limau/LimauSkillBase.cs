using UnityEngine;

/// <summary>
/// Kelas dasar "state" skill Limau (State Pattern berbasis komponen).
/// Controller yang memanggil OnMasuk/OnKeluar/OnUpdateSkill/OnFixedUpdateSkill.
/// </summary>
public abstract class LimauSkillBase : MonoBehaviour
{
    public abstract LimauSkillType Tipe { get; }

    protected LimauSkillInput Masukan { get; private set; }
    protected LimauPhysicsHandler Fisik { get; private set; }
    protected LimauSilkHandler Sutra { get; private set; }
    protected LimauSkillNetwork Jaringan { get; private set; }

    public void Inisialisasi(LimauSkillInput masukan, LimauPhysicsHandler fisik,
                             LimauSilkHandler sutra, LimauSkillNetwork jaringan)
    {
        Masukan = masukan;
        Fisik = fisik;
        Sutra = sutra;
        Jaringan = jaringan;
    }

    public virtual void OnMasuk() { }

    /// <summary>Harus membersihkan semua efek dan idempoten.</summary>
    public virtual void OnKeluar() { }

    public virtual void OnUpdateSkill() { }
    public virtual void OnFixedUpdateSkill() { }
}
