/// <summary>
/// Jenis skill Limau. Nilai angka = level yang membuka skill tersebut
/// (level dibaca dari BiomeProgression, sama seperti Dragon).
/// </summary>
public enum LimauSkillType
{
    None = 0,
    Inflate = 1,         // Level 1 - mengembang jadi bantal pantul
    Bioluminesensi = 2,  // Level 2 - aura cahaya + pengusir hama
    GrapplingSilk = 3    // Level 3 - benang sutra (ayun / tarik teman)
}

/// <summary>
/// Helper untuk LimauSkillType: pemetaan level -> skill (satu sumber kebenaran),
/// level pembuka, dan nama tampil untuk UI/log.
/// </summary>
public static class LimauSkillTypeExtensions
{
    public const int LevelMin = 1;
    public const int LevelMaks = 3;

    /// <summary>Level 1/2/3 -> skill. Di luar rentang -> None.</summary>
    public static LimauSkillType DariLevel(int level)
    {
        return level switch
        {
            1 => LimauSkillType.Inflate,
            2 => LimauSkillType.Bioluminesensi,
            3 => LimauSkillType.GrapplingSilk,
            _ => LimauSkillType.None
        };
    }

    /// <summary>Level yang membuka skill ini (0 untuk None).</summary>
    public static int LevelPembuka(this LimauSkillType tipe) => (int)tipe;

    /// <summary>Nama untuk UI/log.</summary>
    public static string NamaTampil(this LimauSkillType tipe)
    {
        return tipe switch
        {
            LimauSkillType.Inflate => "Inflate",
            LimauSkillType.Bioluminesensi => "Bioluminesensi",
            LimauSkillType.GrapplingSilk => "Grappling Silk",
            _ => "-"
        };
    }

    /// <summary>true jika tipe adalah skill nyata (bukan None / di luar enum).</summary>
    public static bool Valid(this LimauSkillType tipe)
    {
        int i = (int)tipe;
        return i >= LimauSkillTypeExtensions.LevelMin && i <= LimauSkillTypeExtensions.LevelMaks;
    }
}
