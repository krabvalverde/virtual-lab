using NUnit.Framework;
using TMPro;
using UnityEngine;

public class InspectionUITests
{
    private GameObject _root;
    private GlasswareInfo _info;

    [TearDown]
    public void TearDown()
    {
        if (_root != null) Object.DestroyImmediate(_root);
        if (_info != null) Object.DestroyImmediate(_info);
    }

    private InspectionUI BuildUI(out GameObject panel, out TMP_Text name, out TMP_Text desc)
    {
        _root = new GameObject("UI");
        var ui = _root.AddComponent<InspectionUI>();

        panel = new GameObject("Panel");
        panel.transform.SetParent(_root.transform);
        panel.SetActive(false);

        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(panel.transform);
        name = nameGO.AddComponent<TextMeshPro>();

        var descGO = new GameObject("Desc");
        descGO.transform.SetParent(panel.transform);
        desc = descGO.AddComponent<TextMeshPro>();

        ui.panel = panel;
        ui.nameText = name;
        ui.descriptionText = desc;
        return ui;
    }

    [Test]
    public void Show_ActivatesPanelAndFillsText()
    {
        var ui = BuildUI(out var panel, out var name, out var desc);
        _info = ScriptableObject.CreateInstance<GlasswareInfo>();
        _info.displayName = "Béquer";
        _info.description = "Usado para medir e aquecer líquidos.";

        ui.Show(_info);

        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Béquer", name.text);
        Assert.AreEqual("Usado para medir e aquecer líquidos.", desc.text);
    }

    [Test]
    public void Hide_DeactivatesPanel()
    {
        var ui = BuildUI(out var panel, out _, out _);
        panel.SetActive(true);

        ui.Hide();

        Assert.IsFalse(panel.activeSelf);
    }
}
