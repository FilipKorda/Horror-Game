using System.Collections.Generic;
using UnityEngine;

public class StaticSwitcher : MonoBehaviour
{
    public List<GameObject> targets = new List<GameObject>();

    public void DisableStatic()
    {
        foreach (GameObject target in targets)
        {
            if (target != null)
                target.isStatic = false;
        }
    }

    public void RestoreStatic()
    {
        foreach (GameObject target in targets)
        {
            if (target != null)
                target.isStatic = true;
        }
    }
}