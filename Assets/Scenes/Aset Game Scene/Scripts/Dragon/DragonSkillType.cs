/// <summary>
/// Jenis skill Dragon. Nilai angka = level bioma yang membuka skill tersebut.
/// </summary>
public enum DragonSkillType
{
    None = 0,
    Push = 1,        // Level 1 - dorong objek "Pushable"
    Destroy = 2,     // Level 2 - hancurkan objek "Breakable"
    SteelSkin = 3    // Level 3 - tameng statis
}
