using UnityEngine;

public class SkillCastTester : MonoBehaviour
{
    [SerializeField] private SkillCaster skillCaster;
    [SerializeField] private SkillData testSkill;

    public void TestCastSkill()
    {
        if (skillCaster == null)
        {
            Debug.LogWarning("[SkillCastTester] skillCaster is null.");
            return;
        }

        if (testSkill == null)
        {
            Debug.LogWarning("[SkillCastTester] testSkill is null.");
            return;
        }

        bool success = skillCaster.TryCast(testSkill);
        Debug.Log($"[SkillCastTester] Cast result = {success}, skill = {testSkill.skillName}");
    }
}