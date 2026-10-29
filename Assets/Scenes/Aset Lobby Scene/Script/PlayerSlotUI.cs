using System.Collections;
using Photon.Realtime;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PlayerSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textNama;
    [SerializeField] private GameObject ikonHost;
    [SerializeField] private GameObject penandaKamu;
    [SerializeField] private float durasiFade = 0.25f;

    private CanvasGroup canvasGroup;
    private Player player;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Bind(Player target)
    {
        player = target;
        Refresh();
        StartCoroutine(Fade(0f, 1f, null));
    }

    public void Refresh()
    {
        if (player == null) return;

        textNama.text = player.NickName;
        if (ikonHost != null) ikonHost.SetActive(player.IsMasterClient);
        if (penandaKamu != null) penandaKamu.SetActive(player.IsLocal);
    }

    public void Hilangkan()
    {
        if (!gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        StartCoroutine(Fade(canvasGroup.alpha, 0f, () => Destroy(gameObject)));
    }

    private IEnumerator Fade(float dari, float ke, System.Action selesai)
    {
        float t = 0f;
        canvasGroup.alpha = dari;

        while (t < durasiFade)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(dari, ke, t / durasiFade);
            yield return null;
        }

        canvasGroup.alpha = ke;
        selesai?.Invoke();
    }
}