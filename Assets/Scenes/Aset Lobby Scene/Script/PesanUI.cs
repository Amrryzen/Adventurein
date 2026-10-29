using System.Collections;
using TMPro;
using UnityEngine;

public class PesanUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI teks;
    [SerializeField] private float durasi = 2f;

    private Coroutine routine;

    private void Awake()
    {
        teks.gameObject.SetActive(false);
    }

    public void Tampilkan(string isi)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(TampilkanRoutine(isi));
    }

    private IEnumerator TampilkanRoutine(string isi)
    {
        teks.text = isi;
        teks.gameObject.SetActive(true);
        yield return new WaitForSeconds(durasi);
        teks.gameObject.SetActive(false);
    }
}