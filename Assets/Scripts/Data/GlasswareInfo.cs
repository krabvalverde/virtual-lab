using UnityEngine;

[CreateAssetMenu(menuName = "Virtual Lab/Glassware Info", fileName = "GlasswareInfo")]
public class GlasswareInfo : ScriptableObject
{
    public string displayName;

    [TextArea(4, 10)]
    public string description;
}
