using UnityEngine;

public class LastQuest : MonoBehaviour
{
    [SerializeField] private GameObject wall;
    [SerializeField] private GameObject woredrope;
    [SerializeField] private GameObject activeFinalRoom;

    public void CompleteLastQuest()
    {
        wall.SetActive(false);
        woredrope.SetActive(false);
        activeFinalRoom.SetActive(true);
    }

}
