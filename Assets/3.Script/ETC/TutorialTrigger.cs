using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private string actionId;
    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        TutorialMgr.Instance?.RequestShow(actionId);
    }
}