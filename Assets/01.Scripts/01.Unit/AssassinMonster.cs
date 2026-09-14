/*
전열(CharacterRow.Front)을 무시하고 후열(CharacterRow.Back) 캐릭터를 우선 노리는 몬스터
후열(마법사/힐러 등)이 전멸했으면 자동으로 전열까지 포함해서 가장 가까운 대상을 노림
베이스(Monster)는 안건드리고 PickTargetCollider() 훅만 override - 팀 규칙(virtual/override 유도) 그대로따름
*/

using UnityEngine;

/// <summary>
/// 후열 우선 타겟팅 몬스터. 이동(AcquireTarget)/공격(PerformAttack) 둘 다 Monster의
/// PickTargetCollider()를 거쳐가므로, 여기 하나만 override해도 이동/공격 타겟이 항상 일치
/// </summary>
public sealed class AssassinMonster : Monster
{
    /// <summary>
    /// 후보 중 후열 캐릭터가 하나라도 있으면 그중 가장 가까운 후열을, 없으면(후열전멸) 가장가까운
    /// 아무 대상(전열 포함)을 노린다
    /// </summary>
    /// <param name="buffer">OverlapCircle로 찾은 콜라이더 후보들</param>
    /// <param name="count">buffer에서 앞쪽 유효한 개수</param>
    /// <param name="originPosition">거리계산 기준이될 이 몬스터의 현재위치</param>
    protected override Collider2D PickTargetCollider(Collider2D[] buffer, int count, Vector2 originPosition)
    {
        Collider2D nearestBack = null;
        float nearestBackSqr = float.MaxValue;

        Collider2D nearestAny = null;
        float nearestAnySqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = buffer[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IEntity entity) || entity.IsDead)
            {
                continue;
            }

            float sqr = ((Vector2)hit.transform.position - originPosition).sqrMagnitude;

            if (sqr < nearestAnySqr)
            {
                nearestAnySqr = sqr;
                nearestAny = hit;
            }

            // 몬스터끼리는 Row가 없으므로(캐릭터만 해당), CharacterBase로 잡힐 때만 후열 여부를 확인
            if (hit.TryGetComponent(out CharacterBase character) && character.Row == CharacterRow.Back)
            {
                if (sqr < nearestBackSqr)
                {
                    nearestBackSqr = sqr;
                    nearestBack = hit;
                }
            }
        }

        // 후열이 살아있으면 후열 우선, 후열이 전멸했으면(nearestBack == null) 가장 가까운 아무대상으로 대체
        return nearestBack != null ? nearestBack : nearestAny;
    }
}
