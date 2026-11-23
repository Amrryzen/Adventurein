using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// HUB / STATE MACHINE. Memilih skill berdasarkan level (BiomeProgression) dan meneruskan
/// Update/FixedUpdate ke skill aktif. Hanya berjalan di client pemilik Limau (photonView.IsMine);
/// pemain remote cukup menerima event dari LimauSkillNetwork.
/// </summary>
[RequireComponent(typeof(LimauSkillInput), typeof(LimauPhysicsHandler),
                  typeof(LimauSilkHandler))]
[RequireComponent(typeof(LimauSkillNetwork))]
public class LimauSkillController : MonoBehaviourPun
{
    [Tooltip("Dipakai jika scene tidak punya BiomeProgression (untuk tes).")]
    [SerializeField, Range(1, 3)] private int levelAwal = 1;
    [Tooltip("Editor/Development build: tekan 1/2/3 untuk ganti level.")]
    [SerializeField] private bool debugGantiLevel = true;

    public event Action<LimauSkillType> SkillBerganti;
    public LimauSkillType SkillSaatIni { get; private set; } = LimauSkillType.None;

    private LimauSkillInput masukan;
    private LimauPhysicsHandler fisik;
    private LimauSilkHandler sutra;
    private LimauSkillNetwork jaringan;
    private BiomeProgression biome;

    private readonly Dictionary<LimauSkillType, LimauSkillBase> daftarSkill = new();
    private LimauSkillBase skillAktif;
    private bool sedangMati;

    private void Awake()
    {
        masukan = GetComponent<LimauSkillInput>();
        fisik = GetComponent<LimauPhysicsHandler>();
        sutra = GetComponent<LimauSilkHandler>();
        jaringan = GetComponent<LimauSkillNetwork>();

        foreach (LimauSkillBase skill in GetComponents<LimauSkillBase>())
        {
            skill.Inisialisasi(masukan, fisik, sutra, jaringan);
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
        // Mati -> batalkan skill (lepas sutra, kecilkan badan, matikan cahaya) sekali, lalu diam.
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
        GantiSkill(LimauSkillTypeExtensions.DariLevel(level));
    }

    private void GantiSkill(LimauSkillType tipe)
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
