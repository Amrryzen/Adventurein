#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Photon.Pun;
using UnityEngine;

/// <summary>
/// CHEAT TES: ganti karakter lokal secara instan saat Play (F5 = Dragon, F6 = Limau).
/// Membuat dirinya sendiri otomatis (tanpa perlu dipasang). Hanya ada di Editor/Development build.
/// Karakter diganti di tempat yang sama: objek lama di-PhotonNetwork.Destroy, yang baru di-Instantiate.
/// </summary>
public class CheatGantiKarakter : MonoBehaviour
{
    [SerializeField] private KeyCode tombolDragon = KeyCode.F5;
    [SerializeField] private KeyCode tombolLimau = KeyCode.F6;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Buat()
    {
        if (FindFirstObjectByType<CheatGantiKarakter>() != null) return;

        GameObject objek = new GameObject("CheatGantiKarakter");
        objek.AddComponent<CheatGantiKarakter>();
        DontDestroyOnLoad(objek);
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom) return;

        if (Input.GetKeyDown(tombolDragon)) Ganti("Dragon");
        else if (Input.GetKeyDown(tombolLimau)) Ganti("Limau");
    }

    private void Ganti(string nama)
    {
        PlayerController lama = UtilJaringan.AmbilPemainLokal(true);
        if (lama == null || lama.gameObject.name.StartsWith(nama)) return;

        Vector3 posisi = lama.transform.position;
        Quaternion rotasi = lama.transform.rotation;

        PhotonNetwork.Destroy(lama.gameObject);
        KarakterPemain.Set(nama);

        GameObject baru = PhotonNetwork.Instantiate(nama, posisi, rotasi);
        PlayerController pemain = baru.GetComponent<PlayerController>();
        if (pemain == null)
        {
            Debug.LogError($"[Cheat] Prefab '{nama}' tidak punya PlayerController.", baru);
            return;
        }

        // Titik respawn = titik spawn asli (sama seperti SpawnPemain), bukan tempat cheat dipakai.
        Transform titik = CariTitikRespawn();
        if (titik != null) pemain.AturTitikRespawn(titik.position, titik.rotation);
        else pemain.AturTitikRespawn(posisi, rotasi);

        Debug.Log($"[Cheat] Karakter diganti ke {nama}");
    }

    private static Transform CariTitikRespawn()
    {
        SpawnPemain spawn = FindFirstObjectByType<SpawnPemain>(FindObjectsInactive.Include);
        if (spawn == null || spawn.titikSpawn == null || spawn.titikSpawn.Length == 0) return null;

        int i = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawn.titikSpawn.Length;
        return spawn.titikSpawn[i];
    }

    private void OnGUI()
    {
        if (!PhotonNetwork.InRoom) return;
        GUI.Label(new Rect(10f, Screen.height - 28f, 320f, 22f), "[F5] Dragon   [F6] Limau");
    }
}
#endif