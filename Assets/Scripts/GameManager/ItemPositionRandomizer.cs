using System.Collections.Generic;
using UnityEngine;

public class ItemPositionRandomizer : MonoBehaviour
{
    [System.Serializable]
    public class RandomizedItemGroup
    {
        public string groupName; 
        public Transform[] items;      
        public Transform[] spawnPoints;
    }

    [SerializeField] private RandomizedItemGroup[] groups;

    private void Awake()
    {
        foreach (RandomizedItemGroup group in groups)
            RandomizeGroup(group);
    }

    private void RandomizeGroup(RandomizedItemGroup group)
    {
        if (group.items == null || group.spawnPoints == null || group.spawnPoints.Length == 0)
            return;

        List<Transform> availablePoints = new List<Transform>(group.spawnPoints);

        foreach (Transform item in group.items)
        {
            if (item == null)
                continue;

            Transform point = PickAndRemoveRandomPoint(availablePoints, group.spawnPoints);
            if (point == null)
                continue;

            item.position = point.position;
            item.rotation = point.rotation;
        }
    }


    private Transform PickAndRemoveRandomPoint(List<Transform> availablePoints, Transform[] fullPool)
    {
        if (availablePoints.Count == 0)
            return fullPool.Length > 0 ? fullPool[Random.Range(0, fullPool.Length)] : null;

        int index = Random.Range(0, availablePoints.Count);
        Transform chosen = availablePoints[index];
        availablePoints.RemoveAt(index);
        return chosen;
    }
}