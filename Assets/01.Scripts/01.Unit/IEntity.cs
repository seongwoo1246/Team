/*
전투에 참여하는 모든 대상(캐릭터, 몬스터)이 공통으로 갖는 기능
다른 시스템(예: 데미지 판정, 힐)은 CharacterBase / Monster 같은 구체 타입 대신
이 인터페이스에만 의존해서 결합도를 낮춤
*/

/// <summary>
/// 피해를 받고, 회복하고, 죽을 수 있는 전투 대상
/// </summary>
public interface IEntity
{
    float CurrentHP { get; }

    float MaxHP { get; }

    // 이미 죽었는지 여부. true면 더 이상 피해,힐 안받음
    bool IsDead { get; }

    /// <summary>
    /// 피해를 입음
    /// </summary>
    /// <param name="amount">받을 피해량 (0 이상)</param>
    void TakeDamage(float amount);

    /// <summary>
    /// 체력을 회복한다. 최대 체력을 넘지 않음
    /// </summary>
    /// <param name="amount">회복량 (0 이상)</param>
    void Heal(float amount);


    // 사망 처리. 체력이 0이 되면 호출
    void Die();
}
