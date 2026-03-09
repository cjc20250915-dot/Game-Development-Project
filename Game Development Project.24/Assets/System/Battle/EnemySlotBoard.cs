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
    public SlotSpawnInfo slot1 = new SlotSpawnInfo();
    public SlotSpawnInfo slot2 = new SlotSpawnInfo();
    public SlotSpawnInfo slot3 = new SlotSpawnInfo();
    public SlotSpawnInfo slot4 = new SlotSpawnInfo();

    [Header("Runtime (Read Only)")]
    [SerializeField] private List<EnemyUnit> spawnedEnemies = new List<EnemyUnit>();

    private GameObject inst1, inst2, inst3, inst4;

    public IReadOnlyList<EnemyUnit> Enemies => spawnedEnemies;

    public event Action OnEnemiesChanged;

    // 不再自动生成，交给 BattleManager 控制
    // private void Start()
    // {
    //     SpawnAllEnemiesForBattle();
    // }

    public void ApplyNodeData(NodeData nodeData)
    {
        ClearAll();

        // 先清空所有槽位的预制体
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

        // 把 NodeData.enemyWaves 按顺序映射到 4 个槽位
        // 注意：这里的逻辑是“一个 wave 占一个槽位”
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

        unit.OnDead += () => HandleEnemyDead(unit);
    }

    private void HandleEnemyDead(EnemyUnit dead)
    {
        OnEnemiesChanged?.Invoke();
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