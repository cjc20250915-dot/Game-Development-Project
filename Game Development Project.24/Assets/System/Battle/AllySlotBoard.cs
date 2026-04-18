using System;
using UnityEngine;

public class AllySlotBoard : MonoBehaviour
{
    [Serializable]
    public class SlotSpawnInfo
    {
        [Header("Spawn Source")]
        public GameObject allyPrefab;     // 这个槽位要生成的角色prefab（带 AllyUnit / SkillCaster 等）
        public bool spawnOnBattleStart = true;

        [Header("Placement Anchor")]
        public Transform anchor;          // 场景里的“点位/站位”Transform（建议用空物体）

        [Header("Local Pose (relative to anchor)")]
        public Vector3 localPosition = Vector3.zero;
        public Vector3 localEulerAngles = Vector3.zero;
        public Vector3 localScale = Vector3.one;
    }

    [Header("Slot A / Slot B Spawn Info")]
    public SlotSpawnInfo slotAInfo = new SlotSpawnInfo();
    public SlotSpawnInfo slotBInfo = new SlotSpawnInfo();

    [Header("Battle Result")]
    [SerializeField] private BattleResultHandler battleResultHandler;

    [Header("Current Allies (Read Only)")]
    [SerializeField] private AllyUnit slotA;
    [SerializeField] private AllyUnit slotB;

    // 保存实例，方便清理/替换
    private GameObject slotAInstance;
    private GameObject slotBInstance;

    private bool battleLoseTriggered;

    public AllyUnit SlotA => slotA;
    public AllyUnit SlotB => slotB;

    public event Action OnSlotsChanged;

    public int TotalStepsPerTurn
    {
        get
        {
            int total = 0;
            if (slotA != null && !slotA.IsDead) total += Mathf.Max(0, slotA.stepsPerTurn);
            if (slotB != null && !slotB.IsDead) total += Mathf.Max(0, slotB.stepsPerTurn);
            return total;
        }
    }

    private void Awake()
    {
        if (battleResultHandler == null)
            battleResultHandler = FindFirstObjectByType<BattleResultHandler>();
    }

    private void Start()
    {
        // 进入战斗场景后自动呈现
        SpawnAlliesForBattle();
    }

    // ===== Public API =====

    public void SpawnAlliesForBattle()
    {
        battleLoseTriggered = false;

        // A
        if (slotAInfo.spawnOnBattleStart)
            SpawnIntoSlot(0, slotAInfo, ref slotAInstance, ref slotA);

        // B
        if (slotBInfo.spawnOnBattleStart)
            SpawnIntoSlot(1, slotBInfo, ref slotBInstance, ref slotB);

        OnSlotsChanged?.Invoke();
    }

    /// <summary>
    /// 是否至少还有一名存活的友军（槽位上有实例且未死亡）。
    /// </summary>
    public bool HasAliveAlly()
    {
        if (slotA != null && !slotA.IsDead) return true;
        if (slotB != null && !slotB.IsDead) return true;
        return false;
    }

    private void CheckBattleLose()
    {
        if (battleLoseTriggered) return;
        if (HasAliveAlly()) return;

        battleLoseTriggered = true;

        if (battleResultHandler != null)
            battleResultHandler.OnBattleLose();
        else
            Debug.LogWarning("[AllySlotBoard] 友军全灭，但 BattleResultHandler 未绑定。");
    }

    public void ClearSlotA() => ClearSlotInternal(0, ref slotAInstance, ref slotA);
    public void ClearSlotB() => ClearSlotInternal(1, ref slotBInstance, ref slotB);

    public bool TrySetSlotAInstance(AllyUnit allyInstance) => TrySetSlotInstanceInternal(0, allyInstance, ref slotAInstance, ref slotA);
    public bool TrySetSlotBInstance(AllyUnit allyInstance) => TrySetSlotInstanceInternal(1, allyInstance, ref slotBInstance, ref slotB);

    // ===== Internal =====

    private void SpawnIntoSlot(int index, SlotSpawnInfo info, ref GameObject instance, ref AllyUnit allyUnit)
    {
        if (info.allyPrefab == null)
        {
            Debug.LogWarning($"[AllySlotBoard] Slot {(index == 0 ? "A" : "B")} prefab is NULL.");
            return;
        }
        if (info.anchor == null)
        {
            Debug.LogWarning($"[AllySlotBoard] Slot {(index == 0 ? "A" : "B")} anchor is NULL.");
            return;
        }

        // 先清理旧的
        ClearSlotInternal(index, ref instance, ref allyUnit);

        // 生成
        instance = Instantiate(info.allyPrefab, info.anchor, worldPositionStays: false);

        // 设置相对位姿
        var t = instance.transform;
        t.localPosition = info.localPosition;
        t.localRotation = Quaternion.Euler(info.localEulerAngles);
        t.localScale = info.localScale;

        // 取 AllyUnit
        allyUnit = instance.GetComponentInChildren<AllyUnit>();
        if (allyUnit == null)
        {
            Debug.LogError($"[AllySlotBoard] Spawned prefab for Slot {(index == 0 ? "A" : "B")} has no AllyUnit in children.");
        }
        else
        {
            // 监听死亡，步数会变化
            allyUnit.OnDead += OnAllyDead;
        }
    }

    private void ClearSlotInternal(int index, ref GameObject instance, ref AllyUnit allyUnit)
    {
        if (allyUnit != null)
        {
            allyUnit.OnDead -= OnAllyDead;
        }

        allyUnit = null;

        if (instance != null)
        {
            Destroy(instance);
            instance = null;
        }

        OnSlotsChanged?.Invoke();
    }

    private bool TrySetSlotInstanceInternal(int index, AllyUnit allyInstance, ref GameObject instance, ref AllyUnit allyUnit)
    {
        if (allyInstance == null) return false;

        // 防止同一个 Ally 放到两个槽
        if (allyInstance == slotA || allyInstance == slotB) return false;

        // 清理旧实例（若该槽由脚本生成）
        ClearSlotInternal(index, ref instance, ref allyUnit);

        allyUnit = allyInstance;
        allyUnit.OnDead += OnAllyDead;

        OnSlotsChanged?.Invoke();
        return true;
    }

    private void OnAllyDead(AllyUnit dead)
    {
        if (dead == null) return;

        dead.OnDead -= OnAllyDead;

        if (slotA == dead)
        {
            if (slotAInstance != null)
            {
                Destroy(slotAInstance);
                slotAInstance = null;
            }
            else
            {
                Destroy(dead.gameObject);
            }

            slotA = null;
        }
        else if (slotB == dead)
        {
            if (slotBInstance != null)
            {
                Destroy(slotBInstance);
                slotBInstance = null;
            }
            else
            {
                Destroy(dead.gameObject);
            }

            slotB = null;
        }

        OnSlotsChanged?.Invoke();

        CheckBattleLose();
    }
}