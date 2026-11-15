using UnityEngine;

/// <summary>
/// Kelas dasar "state" skill (State Pattern berbasis komponen).
/// Setiap skill = satu MonoBehaviour agar parameternya bisa diatur di Inspector.
/// Controller yang memanggil OnMasuk/OnKeluar/OnUpdateSkill/OnFixedUpdateSkill,
/// jadi skill TIDAK memakai Update() sendiri.
/// </summary>
public abstract class DragonSkillBase : MonoBehaviour
{
    public abstract DragonSkillType Tipe { get; }

    protected DragonSkillInput Masukan { get; private set; }
    protected DragonPhysicsHandler Fisik { get; private set; }
    protected DragonSkillNetwork Jaringan { get; private set; }

    public void Inisialisasi(DragonSkillInput masukan, DragonPhysicsHandler fisik, DragonSkillNetwork jaringan)
    {
        Masukan = masukan;
        Fisik = fisik;
        Jaringan = jaringan;
    }

    /// <summary>Skill menjadi skill aktif (level berganti / respawn).</summary>
    public virtual void OnMasuk() { }

    /// <summary>Skill berhenti jadi skill aktif. HARUS membersihkan semua efek (idempoten).</summary>
    public virtual void OnKeluar() { }

    public virtual void OnUpdateSkill() { }
    public virtual void OnFixedUpdateSkill() { }
}
