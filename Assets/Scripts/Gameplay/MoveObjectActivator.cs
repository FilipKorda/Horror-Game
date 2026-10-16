using System.Collections;
using UnityEngine;

public class MoveObjectActivator : MonoBehaviour
{
    [SerializeField] private BoxCollider boxCollider;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Rigidbody body;

    [Header("Ruch w dół")]
    [SerializeField] private float moveDistance = 0.3f;
    [SerializeField] private float moveSpeed = 3f;

    private bool isMoving;

    public bool onlyOn = false;

    private void Start()
    {
        meshRenderer.enabled = false;
    }

    [ContextMenu("Activate Trigger in Box")]
    public void ActiveTriggerInBox()
    {
        if (onlyOn)
        {
            boxCollider.isTrigger = false;
        }
        else
        {
            if (isMoving)
                return;

            boxCollider.isTrigger = false;
            StartCoroutine(MoveDownRoutine());
        }

    }

    private IEnumerator MoveDownRoutine()
    {
        isMoving = true;

        Vector3 start = body.position;
        float travelled = 0f;

        while (travelled < moveDistance)
        {
            travelled = Mathf.Min(travelled + (moveSpeed * Time.deltaTime), moveDistance);
            body.MovePosition(start + Vector3.down * travelled);
            yield return null;
        }

        isMoving = false;
        boxCollider.isTrigger = true;
    }
}