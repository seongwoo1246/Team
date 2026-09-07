/*
파밍 중 아주 낮은 확률로 나타나는 특수 몬스터. 잡으면 특별 강화재료를 확정으로 지급함
베이스(Monster)는 안 건드리고 OnDied() 훅만 override - 팀 규칙(virtual/override 유도) 그대로 따름
*/

using UnityEngine;

/// <summary>
/// 황금 고블린. 죽으면 MaterialWallet에 강화재료를 확정 지급
/// </summary>
public sealed class GoldenGoblin : Monster
{
    [Header("황금 고블린 전용 보상")]
    [Tooltip("처치 시 확정으로 지급할 강화재료 개수")]
    [SerializeField] private int materialReward = 1;

    /// <summary>
    /// 죽는 순간 강화재료를 확정 지급 (드랍 확률 없이 100%)
    /// </summary>
    protected override void OnDied()
    {
        if (MaterialWallet.instance != null)
        {
            MaterialWallet.instance.Add(materialReward);
        }
    }
}
