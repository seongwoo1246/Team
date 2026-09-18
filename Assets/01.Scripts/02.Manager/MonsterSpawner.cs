// 작성자: 김주연
/*
파밍/챌린지 스테이지에서 몬스터를 실제로 소환하는 스포너.

ObjcetPoolManager는 enumType(Cartoon/Pixel/Item/Particle) 하나당 프리팹 하나만 등록되는 구조라
스테이지마다 서로 다른 몬스터 프리팹을 여러 개 쓰는 우리 상황엔 안맞음
그래서 프리팹별로 자체 스택 풀을 갖는 경량풀을 여기서 직접 관리
*/
/* 공동 작성자 - 송태훈
 
 */


using System.Collections.Generic;
using UnityEngine;
using UtilDebug = DebugLogger<MonsterSpawner>;

/// <summary>
/// ObjectPoolMangerTest를 기반으로 몬스터를 스폰 및 관리하는 스포너
/// </summary>
public sealed class MonsterSpawner : MonoBehaviour
{
    [Header("소환 위치")]
    [Tooltip("몬스터가 소환될 지점들. 여러 개면 그중 무작위 위치에 소환한다")]
    [SerializeField] private Transform[] spawnPoints;

    // 현재 필드에 활성화된 몬스터 집합 (모드 전환 시 일괄 회수용)
    private readonly HashSet<Monster> _activeMonsters = new();


    /// <summary>
    /// 전역 풀(ObjectPoolManagerTest)에서 몬스터를 꺼내어 배치
    /// </summary>
    /// <param name="prefab">소환할 몬스터 프리팹</param>
    /// <param name="level">몬스터 레벨 (보통 스테이지 번호)</param>
    /// <param name="harmless">true면 공격은 하되 캐릭터에게 실제 피해를 주지 않음 (파밍 모드용)</param>
    /// <returns>소환된 몬스터. prefab이 비어있으면 null</returns>
    public Monster Spawn(string poolKey, int level, bool harmless = false)
    {
        if (poolKey == string.Empty)
        {
            UtilDebug.LogWarning("스폰하려는 프리팹 ID 또는 StatData가 유효하지 않습니다.");
            return null;
        }

        Monster monster = ObjectPoolManagerTest.Instance.Spawn<Monster>(poolKey);

        if (monster == null)
        {
            UtilDebug.LogError($"오브젝트 풀에서 몬스터를 스폰하지 못했습니다: {poolKey}");
            return null;
        }

        _activeMonsters.Add(monster);

        // 위치 및 스탯 설정
        monster.transform.position = GetSpawnPosition();
        monster.SetLevel(level);
        monster.SetHarmless(harmless);

        monster.Died += OnMonsterDied;

        return monster;
    }

    /// <summary>
    /// 현재 필드에 살아있는 몬스터를 전부 강제 회수한다 (죽은걸로 치지 않아 보상은 지급되지 않음)
    /// 파밍 ↔ 챌린지 모드를 전환할 때, 이전 모드의 몬스터가 새 모드 필드에 남지 않도록 호출
    /// </summary>
    public void DespawnAll()
    {
        if (_activeMonsters.Count == 0) return;

        // 컬렉션 수정 충돌 방지를 위한 스냅샷 생성
        List<Monster> snapshot = new List<Monster>(_activeMonsters);
        _activeMonsters.Clear();

        for (int monsterIndex = 0; monsterIndex < snapshot.Count; monsterIndex++)
        {
            Monster monster = snapshot[monsterIndex];
            if (monster == null) continue;

            // Despawn()이 Died 구독을 전부 비우므로, 풀 반환은 여기서 직접 해준다
            monster.Died -= OnMonsterDied;
            monster.Despawn();

            ReturnToPool(monster);
        }
    }

    /// <summary>
    /// 몬스터가 죽으면 자신을 소환한 프리팹의 풀로 되돌림
    /// (Monster.Die()가 SetActive(false)까지 처리하므로 여기서 따로 끄지 않는다)
    /// </summary>
    private void OnMonsterDied(Monster monster)
    {
        if(monster == null) return;

        monster.Died -= OnMonsterDied;
        _activeMonsters.Remove(monster);
        ReturnToPool(monster);
    }

    /// <summary>
    /// 인스턴스를 자신이 나온 프리팹의 풀로 되돌림
    /// </summary>
    private void ReturnToPool(Monster monster)
    {
        if(monster == null || monster.StatData == null) return;

        string poolKey = monster.StatData.Id;
        ObjectPoolManagerTest.Instance.Despawn(poolKey, monster);
    }

    /// <summary>
    /// 소환 지점 중 하나를 무작위로 고른다. 지정된 지점이 없으면 스포너 자신의 위치를 씀
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform.position;
        }

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index].position;
    }
}
