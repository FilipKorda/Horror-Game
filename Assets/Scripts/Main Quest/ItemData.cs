using UnityEngine;


[CreateAssetMenu(fileName = "NewItem", menuName = "Horror/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon; 
}