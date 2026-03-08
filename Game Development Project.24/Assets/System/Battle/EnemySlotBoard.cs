using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySlotBoard : MonoBehaviour
{
    [Serializable]
    public class SlotSpawnInfo
    {
        [Header("Spawn Source")]
        public GameObject enemyPrefab;       // 槽位要生成的敌人prefab（需要带 EnemyUnit）
        public bool spawnOnBattleStart = true;

        [Header("Placement Anchor")]
        public Transform anchor;             // 场景站位点（建议空物体）

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

    // 保存实例引用，方便清理
    private GameObject inst1, inst2, inst3, inst4;

    public IReadOnlyList<EnemyUnit> Enemies => spawnedEnemies;

    public event Action OnEnemiesChanged;

    private void Start()
    {
        SpawnAllEnemiesForBattle();

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

        // 清掉旧实例
        ClearSlot(ref instance);

        // 生成
        instance = Instantiate(info.enemyPrefab, info.anchor, worldPositionStays: false);

        // 设置相对位姿
        Transform t = instance.transform;
        t.localPosition = info.localPosition;
        t.localRotation = Quaternion.Euler(info.localEulerAngles);
        t.localScale = info.localScale;

        // 获取 EnemyUnit
        EnemyUnit unit = instance.GetComponentInChildren<EnemyUnit>();
        if (unit == null)
        {
            Debug.LogError($"[EnemySlotBoard] Spawned prefab in Slot {index} has no EnemyUnit in children.");
            return;
        }

        spawnedEnemies.Add(unit);

        // 死亡后更新列表（不销毁模型，先只从“存活名单”意义上更新）
        unit.OnDead += () => HandleEnemyDead(unit);
    }

    private void HandleEnemyDead(EnemyUnit dead)
    {
        // 这里不 Destroy，保持你之后可以播死亡动画
        // 只是通知“敌人列表状态改变”
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