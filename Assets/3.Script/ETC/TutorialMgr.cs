using System.Collections.Generic;
using UnityEngine;

public class TutorialMgr : MonoBehaviour
{
    public static TutorialMgr Instance;
    private TutorialPrompt activePrompt;
    private readonly Dictionary<string, TutorialPrompt> registry = new Dictionary<string, TutorialPrompt>();
    private readonly Queue<string> pendingQueue = new Queue<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        foreach (var prompt in FindObjectsByType<TutorialPrompt>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            RegisterPrompt(prompt);
        }
    }

    public void RegisterPrompt(TutorialPrompt p)
    {
        if (!string.IsNullOrEmpty(p.DismissActionId))
            registry[p.DismissActionId] = p;
    }

    public void RegisterActivePrompt(TutorialPrompt p) => activePrompt = p;

    public void UnregisterActivePrompt(TutorialPrompt p)
    {
        if (activePrompt == p)
        {
            activePrompt = null;
            TryShowNextInQueue();
        }
    }

    public void NotifyAction(string actionId)
    {
        if (activePrompt != null && activePrompt.DismissActionId == actionId)
            activePrompt.Hide();
    }

    public void RequestShow(string actionId)
    {
        if (!registry.TryGetValue(actionId, out var prompt)) return;

        if (activePrompt == null)
        {
            prompt.Show();
        }
        else if (activePrompt != prompt && !pendingQueue.Contains(actionId))
        {
            pendingQueue.Enqueue(actionId); // 이미 뭔가 떠있으면 대기열에 넣음
        }
    }

    private void TryShowNextInQueue()
    {
        if (pendingQueue.Count == 0) return;
        string nextId = pendingQueue.Dequeue();
        if (registry.TryGetValue(nextId, out var next))
        {
            next.Show();
        }
    }
}