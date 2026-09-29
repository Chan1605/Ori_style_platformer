using System;
using UnityEngine;

public struct CheckpointSnapshot
{
    public Vector3 position;
    public int health;
    public int skillGauge;
    public int checkpointGauge;
    public int accumulatedSouls;
}

public static class GameEvents
{
    public static class Player
    {
        public static event Action<CheckpointSnapshot> OnCheckpointCreated;
        public static void RaiseCheckpointCreated(CheckpointSnapshot snapshot) => OnCheckpointCreated?.Invoke(snapshot);

        public static event Action OnDied;
        public static void RaiseDied() => OnDied?.Invoke();
    }
    public static class Stage
    {
        public static event Action<CheckpointSnapshot> OnCheckpointRestore;
        public static void RaiseCheckpointRestore(CheckpointSnapshot snapshot) => OnCheckpointRestore?.Invoke(snapshot);

        public static event Action OnKeyGuideCompleted;
        public static void RaiseKeyGuideCompleted() => OnKeyGuideCompleted?.Invoke();
    }
}