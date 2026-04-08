using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/VFX Config", fileName = "VFXConfig")]
public class VFXConfig : ScriptableObject
{
    [Header("我方 · 恢复生命值")]
    public GameObject healVFX;                      // CFXR4 Falling Stars

    [Header("普通攻击")]
    public GameObject normalAttackVFX;              // CFXR Hit A (Red)

    [Header("电属性 · 单体攻击")]
    public GameObject lightningAttackVFX;           // CFXR Electrified 3
    [Tooltip("电属性镭射扫描 Shader（Material 上的 Shader）")]
    public Material lightningHitShaderMaterial;     // 镭射扫描 Shader Material

    [Header("风属性 · 范围攻击")]
    public GameObject windAoEVFX;                   // CFXR3 Shield Leaves A (Lit)

    [Header("火属性 · 范围攻击")]
    public GameObject fireAoEVFX;                   // CFXR2 Firewall A

    [Header("冰属性 · 单体攻击")]
    public GameObject iceAttackVFX;                 // CFXR4 Sword Hit ICE (Cross)
    [Tooltip("冰冻状态 Shader（叠加在目标 Renderer 上）")]
    public Material iceFrozenShaderMaterial;        // 冰冻 Shader Material
    [Tooltip("冰冻状态持续时间（秒）")]
    public float iceFrozenDuration = 2.5f;

    [Header("敌人 · 护盾效果")]
    public GameObject enemyShieldVFX;               // CFXR4 Bouncing Glows Bubble (Blue Purple)

    [Header("敌人 · 死亡")]
    public GameObject enemyDeathVFX;                // CFXR2 Skull Head Alt
    [Tooltip("死亡溶解 Shader（叠加在敌人 Renderer 上）")]
    public Material enemyDeathDissolveShaderMaterial; // 死亡溶解 Shader Material
    [Tooltip("溶解动画持续时间（秒）")]
    public float dissolveduration = 1.0f;
    [Tooltip("溶解进度在 Shader 中的属性名")]
    public string dissolvePropertyName = "_DissolveAmount";
}