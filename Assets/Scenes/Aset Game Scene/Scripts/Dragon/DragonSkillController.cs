using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// HUB / STATE MACHINE. Memilih skill berdasarkan level bioma dan meneruskan Update/FixedUpdate
/// ke skill aktif. Hanya berjalan di client pemilik Dragon (photonView.IsMine);
/// pemain remote cukup menerima event visual dari DragonSkillNetwork.
/// </summary>
[RequireComponent(typeof(DragonSkillInput), typeof(DragonPhysicsHandler), typeof(DragonSkillNetwork))]
public class DragonSkillController : MonoBehaviourPun
{
    [Tooltip("Dipakai jika scene tidak punya BiomeProgression (untuk tes).")]
    [SerializeField, Range(1, 3)] private int levelAwal = 1;
    [Tooltip("Editor/Development build: tekan 1/2/3 untuk ganti level.")]
    [SerializeField] private bool debugGantiLevel = true;

    public event Action<DragonSkillType> SkillBerganti;
    public DragonSkillType SkillSaatIni { get; private set; } = DragonSkillType.None;

    /// <summary>Dipakai hazard (angin/burung) untuk melewati dorongan saat Steel Skin aktif.</summary>
    public bool KebalDorongan => jaringan != null && jaringan.IsAktif(DragonSkillType.SteelSkin);

    private DragonSkillInput masukan;
    private DragonPhysicsHandler fisik;
    private DragonSkillNetwork jaringan;
    private BiomeProgression biome;

    private readonly Dictionary<DragonSkillType, DragonSkillBase> daftarSkill = new();
    private DragonSkillBase skillAktif;
    private bool sedangMati;

    private void Awake()
    {
        masukan = GetComponent<DragonSkillInput>();
        fisik = GetComponent<DragonPhysicsHandler>();
        jaringan = GetComponent<DragonSkillNetwork>();

        foreach (DragonSkillBase skill in GetComponents<DragonSkillBase>())
        {
            skill.Inisialisasi(masukan, fisik, jaringan);
            daftarSkill[skill.Tipe] = skill;
        }
    }

    private void Start()
    {
        // Pastikan eksekusi input hanya di pemilik karakter.
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        biome = BiomeProgression.Instance;
        if (biome != null)
        {
            biome.LevelBerubah += TerapkanLevel;
            TerapkanLevel(biome.LevelSaatIni);
        }
        else
        {
            TerapkanLevel(levelAwal);
        }
    }

    private void OnDestroy()
    {
        if (biome != null) biome.LevelBerubah -= TerapkanLevel;
    }

    private void OnDisable()
    {
        if (skillAktif != null) skillAktif.OnKeluar();
    }

    private void Update()
    {
        // Mati -> batalkan skill (mis. lepas Steel Skin) sekali, lalu diam sampai respawn.
        if (fisik.Mati)
        {
            if (!sedangMati)
            {
                sedangMati = true;
                if (skillAktif != null) skillAktif.OnKeluar();
            }
            return;
        }

        if (sedangMati)
        {
            sedangMati = false;
            if (skillAktif != null) skillAktif.OnMasuk();
        }

        masukan.Sampel();
        DebugLevel();

        if (skillAktif != null) skillAktif.OnUpdateSkill();
    }

    private void FixedUpdate()
    {
        if (fisik.Mati) return;
        if (skillAktif != null) skillAktif.OnFixedUpdateSkill();
    }

    private void TerapkanLevel(int level)
    {
        GantiSkill(LevelKeSkill(level));
    }

    private static DragonSkillType LevelKeSkill(int level)
    {
        return level switch
        {
            1 => DragonSkillType.Push,
            2 => DragonSkillType.Destroy,
            3 => DragonSkillType.SteelSkin,
            _ => DragonSkillType.None
        };
    }

    private void GantiSkill(DragonSkillType tipe)
    {
        if (tipe == SkillSaatIni && skillAktif != null) return;

        if (skillAktif != null) skillAktif.OnKeluar();

        SkillSaatIni = tipe;
        daftarSkill.TryGetValue(tipe, out skillAktif);

        if (skillAktif != null && !sedangMati) skillAktif.OnMasuk();
        SkillBerganti?.Invoke(tipe);
    }

    private void DebugLevel()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!debugGantiLevel) return;

        for (int i = 1; i <= 3; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha0 + i)) continue;

            if (biome != null) biome.MintaGantiLevel(i);
            else TerapkanLevel(i);
        }
#endif
    }
}
