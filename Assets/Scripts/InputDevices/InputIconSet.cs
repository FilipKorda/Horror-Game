using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InputIconSet", menuName = "Horror/Input Icon Set")]
public class InputIconSet : ScriptableObject
{
    [System.Serializable]
    public class IconEntry
    {
        public string bindingPath;
        public string spriteTagName;
    }

    public InputDeviceType deviceType;
    public List<IconEntry> icons = new();

    public string GetSpriteTag(string bindingPath)
    {
        foreach (IconEntry entry in icons)
        {
            if (entry.bindingPath == bindingPath)
                return $"<sprite name=\"{entry.spriteTagName}\">";
        }

        return null;
    }
}