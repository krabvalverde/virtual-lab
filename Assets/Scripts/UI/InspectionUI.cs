using TMPro;
using UnityEngine;

public class InspectionUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public GameObject panel;

    public void Show(GlasswareInfo info)
    {
        if (info == null) return;
        if (nameText != null) nameText.text = info.displayName;
        if (descriptionText != null) descriptionText.text = info.description;
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
