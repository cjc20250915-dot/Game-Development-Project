using System;
using System.Collections;
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

    [Header("Promotion (back → front)")]
    [Tooltip("前排阵亡后后排顶上前所用移动时间（秒）")]
    [SerializeField] private float promoteMoveDuration = 0.45f;

    [Header("Battle Result")]
    [SerializeField] private BattleResultHandler battleResultHandler;

    [Header("Runtime (Read Only)")]
    [SerializeField] private List<EnemyUnit> spawnedEnemies = new List<EnemyUnit>();

    private GameObject inst1, inst2, inst3, inst4;
    private bool battleWinTriggered = false;

    public IReadOnlyList<EnemyUnit> Enemies => spawnedEnemies;

    public event Action OnEnemiesChanged;

    private void Awake()
    {
        if (battleResultHandler == null)
        {
            battleResultHandler = FindFirstObjectByType<BattleResultHandler>();
        }
    }

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
        battleWinTriggered = false;
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
        battleWinTriggered = false;
        OnEnemiesChanged?.Invoke();
    }

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

    public bool AreAllEnemiesDead()
    {
        return GetAllAliveEnemies().Count == 0;
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

        CheckBattleWin();
    }

    private void CheckBattleWin()
    {
        if (battleWinTriggered) return;
        if (!AreAllEnemiesDead()) return;

        battleWinTriggered = true;

        if (battleResultHandler != null)
        {
            battleResultHandler.OnBattleWin();
        }
        else
        {
            Debug.LogWarning("[EnemySlotBoard] All enemies are dead, but BattleResultHandler is missing.");
        }
    }

    private void TryPromoteBackRow()
    {
        // 1号位空了，3号位补到1号位
        if (inst1 == null && inst3 != null)
        {
            inst1 = inst3;
            inst3 = null;
            StartCoroutine(PromoteEnemyToSlotRoutine(inst1, slot1));
        }

        // 2号位空了，4号位补到2号位
        if (inst2 == null && inst4 != null)
        {
            inst2 = inst4;
            inst4 = null;
            StartCoroutine(PromoteEnemyToSlotRoutine(inst2, slot2));
        }
    }

    /// <summary>
    /// 后排顶前排：先挂到新锚点并保持世界坐标不变，再插值到槽位约定本地 pose（避免瞬移）。
    /// </summary>
    private IEnumerator PromoteEnemyToSlotRoutine(GameObject instance, SlotSpawnInfo targetSlot)
    {
        if (instance == null || targetSlot == null || targetSlot.anchor == null)
            yield break;

        Transform t = instance.transform;

        // 保留当前屏幕上的位置，换父物体后再 tween 到前排槽目标 pose
        t.SetParent(targetSlot.anchor, worldPositionStays: true);

        Vector3 startLocalPos = t.localPosition;
        Quaternion startLocalRot = t.localRotation;
        Vector3 startLocalScale = t.localScale;

        Vector3 endLocalPos = targetSlot.localPosition;
        Quaternion endLocalRot = Quaternion.Euler(targetSlot.localEulerAngles);
        Vector3 endLocalScale = targetSlot.localScale;

        float dur = Mathf.Max(0.05f, promoteMoveDuration);
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / dur);
            float s = Mathf.SmoothStep(0f, 1f, u);

            t.localPosition = Vector3.LerpUnclamped(startLocalPos, endLocalPos, s);
            t.localRotation = Quaternion.SlerpUnclamped(startLocalRot, endLocalRot, s);
            t.localScale = Vector3.LerpUnclamped(startLocalScale, endLocalScale, s);

            yield return null;
        }

        t.localPosition = endLocalPos;
        t.localRotation = endLocalRot;
        t.localScale = endLocalScale;
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