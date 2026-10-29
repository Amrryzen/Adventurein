using UnityEngine;

public class PanelManager : MonoBehaviour
{
    [SerializeField] private GameObject[] panels;

    public void Show(string panelName)
    {
        foreach (GameObject panel in panels)
        {
            if (panel != null)
                panel.SetActive(panel.name == panelName);
        }
    }
}