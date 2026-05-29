using NUnit.Framework;
using UnityEngine;

public class GlasswareInfoTests
{
    private GlasswareInfo info;

    [TearDown]
    public void TearDown()
    {
        if (info != null)
        {
            Object.DestroyImmediate(info);
            info = null;
        }
    }

    [Test]
    public void StoresDisplayNameAndDescription()
    {
        info = ScriptableObject.CreateInstance<GlasswareInfo>();
        info.displayName = "Béquer";
        info.description = "Recipiente de vidro borossilicato para misturar e aquecer líquidos.";

        Assert.AreEqual("Béquer", info.displayName);
        Assert.AreEqual("Recipiente de vidro borossilicato para misturar e aquecer líquidos.", info.description);
    }
}
