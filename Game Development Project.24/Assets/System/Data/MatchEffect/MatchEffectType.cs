public enum MatchEffectType
{
    None = 0,

    // type0：前排随机单体伤害
    FrontRandomEnemyDamage = 1,

    // type1：前排全体伤害
    FrontAllEnemiesDamage = 2,

    // type2：治疗血量最低的友方（并列随机）
    HealLowestAlly = 3,

    // type3：回复步数
    RestoreMoves = 4,

    // type4：先预留
    Reserved = 5
}