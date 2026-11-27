using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aura Bioluminesensi sebagai "fakta dunia" yang bisa ditanya oleh musuh/hama.
/// Berjalan di SEMUA client (status ON/OFF datang dari LimauSkillNetwork), jadi musuh yang
/// dikendalikan client/otoritas mana pun bisa memeriksa aura tanpa RPC tambahan:
///
///   if (LimauAura.TryCariPengusir(transform.position, out LimauAura aura)) { /* kabur dari aura */ }
///
/// Catatan: posisi Limau disinkronkan oleh sinkronisasi transform prefab Player.
/// </summary>
public class LimauAura : MonoBehaviour
{
    private static readonly List<LimauAura> daftarAktif = new();

    [Tooltip("Radius CAHAYA (unit dunia): seberapa jauh lorong terang.")]
    [SerializeField] private float radius = 6f;
    [Tooltip("Radius PENGUSIR hama (unit dunia): lebih kecil dari cahaya supaya hama tidak kabur kejauhan.")]
    [SerializeField] private float radiusPengusir = 3f;

    private LimauSkillNetwork jaringan;

    public float Radius => radius;
    public float RadiusPengusir => radiusPengusir;
    public bool Menyala { get; private set; }

    public static IReadOnlyList<LimauAura> DaftarAktif => daftarAktif;

    // Aman bila "Enter Play Mode Options" mematikan domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatik() => daftarAktif.Clear();

    private void Awake()
    {
        jaringan = GetComponent<LimauSkillNetwork>();
    }

    private void OnEnable()
    {
        if (jaringan != null) jaringan.SkillAktifBerubah += SaatSkillAktif;
    }

    private void OnDisable()
    {
        if (jaringan != null) jaringan.SkillAktifBerubah -= SaatSkillAktif;
        Atur(false);
    }

    private void SaatSkillAktif(LimauSkillType tipe, bool aktif)
    {
        if (tipe == LimauSkillType.Bioluminesensi) Atur(aktif);
    }

    private void Atur(bool nyala)
    {
        if (Menyala == nyala) return;

        Menyala = nyala;
        if (nyala) daftarAktif.Add(this);
        else daftarAktif.Remove(this);
    }

    public bool Mencakup(Vector3 posisi)
    {
        return (posisi - transform.position).sqrMagnitude <= radiusPengusir * radiusPengusir;
    }

    /// <summary>Aura menyala terdekat yang mencakup posisi tersebut.</summary>
    public static bool TryCariPengusir(Vector3 posisi, out LimauAura aura)
    {
        aura = null;
        float terdekat = float.MaxValue;

        for (int i = 0; i < daftarAktif.Count; i++)
        {
            LimauAura kandidat = daftarAktif[i];
            if (!kandidat.Mencakup(posisi)) continue;

            float jarak = (posisi - kandidat.transform.position).sqrMagnitude;
            if (jarak < terdekat)
            {
                terdekat = jarak;
                aura = kandidat;
            }
        }

        return aura != null;
    }
}