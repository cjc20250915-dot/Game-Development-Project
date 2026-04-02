using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySlotBoard : MonoBehaviour
{
    [Serializable]
    public class SlotSpawnInfo
    {
        [Header("Spawn Source")]
        public GameObject enemyPrefab;

        public bool spawnOnBattleStart = true;

        [Header("Placement Anchor")]
        public Transform anchor;

        [Header("Local Pose (relative to anchor)")]
        public Vector3 localPosition = Vector3.zero;
        public Vector3 localEulerAngles = Vector3.zero;
        public Vector3 localScale = Vector3.one;
    }

    [Header("Slots (max 4)")]
    public SlotSpawnInfo slot1 = new SlotSpawnInfo(); // 左前
    public SlotSpawnInfo slot2 = new SlotSpawnInfo(); // 右前
    public SlotSpawnInfo slot3 = new SlotSpawnInfo(); // 左后
    public SlotSpawnInfo slot4 = new SlotSpawnInfo(); // 右后

    [Header("Runtime (Read Only)")]
    [SerializeField] private List<EnemyUnit> spawnedEnemies = new List<EnemyUnit>();

    private GameObject inst1, inst2, inst3, inst4;

    public IReadOnlyList<EnemyUnit> Enemies => spawnedEnemies;

    public event Action OnEnemiesChanged;

    public void ApplyNodeData(NodeData nodeData)
    {
        ClearAll();

        slot1.enemyPrefab = null;
        slot2.enemyPrefab = null;
        slot3.enemyPrefab = null;
        slot4.enemyPrefab = null;

        slot1.spawnOnBattleStart = false;
        slot2.spawnOnBattleStart = false;
        slot3.spawnOnBattleStart = false;
        slot4.spawnOnBattleStart = false;

        if (nodeData == null || nodeData.enemyWaves == null)
        {
            Debug.LogWarning("[EnemySlotBoard] NodeData 或 enemyWaves 为空。");
            return;
        }

        for (int i = 0; i < nodeData.enemyWaves.Count && i < 4; i++)
        {
            EnemyWave wave = nodeData.enemyWaves[i];
            if (wave == null || wave.enemyPrefab == null || wave.count <= 0) continue;

            SlotSpawnInfo slot = GetSlotByIndex(i);
            if (slot == null) continue;

            slot.enemyPrefab = wave.enemyPrefab;
            slot.spawnOnBattleStart = true;
        }
    }

    private SlotSpawnInfo GetSlotByIndex(int index)
    {
        switch (index)
        {
            case 0: return slot1;
            case 1: return slot2;
            case 2: return slot3;
            case 3: return slot4;
            default: return null;
        }
    }

    public void SpawnAllEnemiesForBattle()
    {
        spawnedEnemies.Clear();

        SpawnIntoSlot(1, slot1, ref inst1);
        SpawnIntoSlot(2, slot2, ref inst2);
        SpawnIntoSlot(3, slot3, ref inst3);
        SpawnIntoSlot(4, slot4, ref inst4);

        OnEnemiesChanged?.Invoke();
    }

    public void ClearAll()
    {
        ClearSlot(ref inst1);
        ClearSlot(ref inst2);
        ClearSlot(ref inst3);
        ClearSlot(ref inst4);

        spawnedEnemies.Clear();
        OnEnemiesChanged?.Invoke();
    }

    // =========================
    // 前后排接口
    // =========================

    public List<EnemyUnit> GetFrontRowAliveEnemies()
    {
        List<EnemyUnit> result = new List<EnemyUnit>();

        AddAliveEnemyFromInstance(inst1, result);
        AddAliveEnemyFromInstance(inst2, result);

        return result;
    }

    public List<EnemyUnit> GetBackRowAliveEnemies()
    {
        List<EnemyUnit> result = new List<EnemyUnit>();

        AddAliveEnemyFromInstance(inst3, result);
        AddAliveEnemyFromInstance(inst4, result);

        return result;
    }

    public List<EnemyUnit> GetAllAliveEnemies()
    {
        List<EnemyUnit> result = new List<EnemyUnit>();

        AddAliveEnemyFromInstance(inst1, result);
        AddAliveEnemyFromInstance(inst2, result);
        AddAliveEnemyFromInstance(inst3, result);
        AddAliveEnemyFromInstance(inst4, result);

        return result;
    }

    public EnemyUnit GetRandomAliveEnemy()
    {
        List<EnemyUnit> all = GetAllAliveEnemies();
        if (all.Count == 0) return null;
        return all[UnityEngine.Random.Range(0, all.Count)];
    }

    private void AddAliveEnemyFromInstance(GameObject instance, List<EnemyUnit> result)
    {
        if (instance == null) return;

        EnemyUnit unit = instance.GetComponentInChildren<EnemyUnit>();
        if (unit == null) return;
        if (unit.IsDead) return;

        result.Add(unit);
    }

    private void SpawnIntoSlot(int index, SlotSpawnInfo info, ref GameObject instance)
    {
        if (!info.spawnOnBattleStart) return;
        if (info.enemyPrefab == null) return;

        if (info.anchor == null)
        {
            Debug.LogWarning($"[EnemySlotBoard] Slot {index} anchor is NULL.");
            return;
        }

        ClearSlot(ref instance);

        instance = Instantiate(info.enemyPrefab, info.anchor, false);

        Transform t = instance.transform;
        t.localPosition = info.localPosition;
        t.localRotation = Quaternion.Euler(info.localEulerAngles);
        t.localScale = info.localScale;

        EnemyUnit unit = instance.GetComponentInChildren<EnemyUnit>();
        if (unit == null)
        {
            Debug.LogError($"[EnemySlotBoard] Spawned prefab in Slot {index} has no EnemyUnit in children.");
            return;
        }

        spawnedEnemies.Add(unit);

        GameObject spawnedRoot = instance;
        unit.OnDead += () => HandleEnemyDead(unit, spawnedRoot);
    }

    private void HandleEnemyDead(EnemyUnit dead, GameObject rootInstance)
    {
        if (dead != null)
        {
            spawnedEnemies.Remove(dead);
        }

        bool wasSlot1 = inst1 == rootInstance;
        bool wasSlot2 = inst2 == rootInstance;
        bool wasSlot3 = inst3 == rootInstance;
        bool wasSlot4 = inst4 == rootInstance;

        if (wasSlot1) inst1 = null;
        if (wasSlot2) inst2 = null;
        if (wasSlot3) inst3 = null;
        if (wasSlot4) inst4 = null;

        if (rootInstance != null)
        {
            Destroy(rootInstance);
        }

        TryPromoteBackRow();

        OnEnemiesChanged?.Invoke();
    }

    private void TryPromoteBackRow()
    {
        // 1号位空了，3号位补到1号位
        if (inst1 == null && inst3 != null)
        {
            inst1 = inst3;
            inst3 = null;
            MoveInstanceToSlot(inst1, slot1);
        }

        // 2号位空了，4号位补到2号位
        if (inst2 == null && inst4 != null)
        {
            inst2 = inst4;
            inst4 = null;
            MoveInstanceToSlot(inst2, slot2);
        }
    }

    private void MoveInstanceToSlot(GameObject instance, SlotSpawnInfo targetSlot)
    {
        if (instance == null || targetSlot == null || targetSlot.anchor == null) return;

        Transform t = instance.transform;
        t.SetParent(targetSlot.anchor, false);
        t.localPosition = targetSlot.localPosition;
        t.localRotation = Quaternion.Euler(targetSlot.localEulerAngles);
        t.localScale = targetSlot.localScale;
    }

    private void ClearSlot(ref GameObject instance)
    {
        if (instance != null)
        {
            Destroy(instance);
            instance = null;
        }
    }
}