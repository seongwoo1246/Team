// 작성자: 김주연
/*
파티 편성(최대 3명) 관리. 5명(전사/메이지/힐러/팔라딘/궁수) 중 최대 3명을 골라 실제 전투 필드로 내보낸다

편성이 바뀌면 한 번에 다 처리함:
  1) 필드에서 뺀 캐릭터는 스프라이트/콜라이더를 끄고 멀리(벤치 위치로) 치움 - 전투에 물리적으로 안 끼어들게
  2) 필드에 넣은 캐릭터는 정해진 슬롯 위치로 옮기고 스프라이트/콜라이더를 켬
  3) StageManager.party를 지금 편성으로 갱신
  4) 슬롯별 스킬 버튼(1/2/3번 슬롯 x 스킬1/2) 6개의 타겟을 그 슬롯 캐릭터로 갱신

챌린지(전투) 진행 중에는 편성을 못 바꾸게 막음 - 웨이브 도중 캐릭터가 갑자기 사라지면 이상해지므로
파밍 중이거나 대기 중일 때만 바꿀 수 있음
*/

using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UtilDebug = DebugLogger<PartyFormationManager>;

/// <summary>
/// 5명 중 3명을 골라 실전 파티를 구성하는 매니저. 캐릭터 화면의 "편성하기" 버튼이 ToggleFormation을 호출
/// </summary>
public sealed class PartyFormationManager : MonoBehaviour, ILoadable
{
    // 편성 가능한 최대 인원
    private const int SLOT_COUNT = 3;
    private readonly System.Collections.Generic.List<CharacterBase> _spawnedCharacters = new();

    [Header("편성 가능한 캐릭터 5명")]
    [Tooltip("전사/메이지/힐러/팔라딘/궁수 순서 상관없이 5명 전부")]
    [SerializeField] private CharacterBase[] allCharacters;

    [Header("필드 슬롯 위치 (3자리)")]
    [Tooltip("편성된 캐릭터가 실제로 서 있을 위치 3개 (빈 오브젝트로 표시)")]
    [SerializeField] private Transform[] fieldSlots;

    [Tooltip("편성에서 빠진 캐릭터를 치워둘 위치 (화면 밖, RosterOnly 등)")]
    [SerializeField] private Transform benchPosition;

    [Header("1번 슬롯 스킬 버튼 (스킬1, 스킬2 순서)")]
    [SerializeField] private SkillButtonUI[] slot1SkillButtons;

    [Header("2번 슬롯 스킬 버튼 (스킬1, 스킬2 순서)")]
    [SerializeField] private SkillButtonUI[] slot2SkillButtons;

    [Header("3번 슬롯 스킬 버튼 (스킬1, 스킬2 순서)")]
    [SerializeField] private SkillButtonUI[] slot3SkillButtons;

    // 슬롯별로 지금 배정된 캐릭터. 비어있으면 null
    private readonly CharacterBase[] _formation = new CharacterBase[SLOT_COUNT];

    public int LoadOrder => 17;

    // 편성이 바뀔 때마다 발생. 인자 = 새 편성(슬롯 순서). UI(편성 표시 텍스트 등)가 구독해서 갱신하는 용도
    public event System.Action<CharacterBase[]> FormationChanged;

    #region 김주연 - ServiceLocator 등록
    private void Awake()
    {
        ServiceLocator.Register<PartyFormationManager>(this, ServiceLifetime.Local);
        SceneLoadManager.Instance.RegisterLoadable(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<PartyFormationManager>();
        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.UnregisterLoadable(this);
        }
    }
    #endregion

    #region ILoadable 구현
    /// <summary>
    /// 씬 로드 단계에서 배치할 캐릭터 에셋 로드
    /// </summary>
    /// <param name="scene"></param>
    /// <returns></returns>
    public async UniTask OnSceneLoadCreate(SceneId scene)
    {
        if (scene == SceneId.BootstrapScene || scene == SceneId.None) return;

        System.Threading.CancellationToken ct = this.destroyCancellationToken;
        UtilDebug.Log($"[{scene}] 캐릭터 에셋 로드 및 인스턴스화 시작");

        var prefabs = await AddressableManager.Instance.LoadAssetsByLabelAsync<GameObject>("Character", ct);
        if (prefabs == null || prefabs.Count == 0)
        {
            UtilDebug.LogError("Character 라벨에 등록된 프리팹이 없습니다.");
            return;
        }

        _spawnedCharacters.Clear();
        foreach (var prefab in prefabs)
        {
            GameObject charGo = Instantiate(prefab, transform);
            if (charGo.TryGetComponent<CharacterBase>(out var characterComp))
            {
                _spawnedCharacters.Add(characterComp);
                SetFieldActive(characterComp, false);
            }
        }
        allCharacters = _spawnedCharacters.ToArray();
    }

    /// <summary>
    /// 캐릭터 목록과 유저의 저장된 파티 슬롯을 받아 파티 구성
    /// </summary>
    /// <param name="scene"></param>
    public void Init(SceneId scene)
    {
        if (scene == SceneId.BootstrapScene || scene == SceneId.None) return;
        UtilDebug.Log($"[{scene}] 캐릭터 에셋 로드 및 인스턴스화 시작");

        UserInfo user = UserManager.Instance.CurrentUser;
        Dictionary<string, CharacterSaveData> charDict = user?.Characters?.characterDictionary;
        if (user == null || charDict == null)
        {
            UtilDebug.LogError($"{user} 또는 charDict 널 에러.");
            return;
        }


        if (!ServiceLocator.TryGet<EquipmentInventory>(out var equipmentInventory))
        {
            UtilDebug.LogError("EquipmentInventory 서비스를 찾을 수 없습니다. 초기화 순서를 점검");
            return;
        }

        for (int i = 0; i < SLOT_COUNT; i++) _formation[i] = null;

        if (allCharacters != null)
        {
            foreach (var character in allCharacters)
            {
                if (character == null || character.StatData == null) continue;

                if (charDict.TryGetValue(character.StatData.Id, out var saveData))
                {
                    // 1. 장비 복원 (equippedItems가 있을 때만)
                    if (saveData.equippedItems != null)
                    {
                        foreach (var slotPair in saveData.equippedItems)
                        {
                            if (System.Enum.TryParse(slotPair.Key, out EquipmentSlot slot))
                            {
                                if (equipmentInventory.TryGetItem(slotPair.Value, out var equipItem))
                                {
                                    character.Equip(slot, equipItem, persist: false);
                                }
                            }
                        }
                    }

                    // 2. 파티 슬롯 배정 (장비 유무와 무관하게 항상 실행)
                    if (saveData.partySlot >= 0 && saveData.partySlot < SLOT_COUNT)
                    {
                        _formation[saveData.partySlot] = character;
                    }
                }
                character.RefreshStatsFromUpgradeSystem();
            }


            // Fallback (비어있으면 앞 3명) - 방어코드 리펙토링하면 없앨 수 있음
            if (_formation[0] == null && _formation[1] == null && _formation[2] == null)
            {
                for (int i = 0; i < SLOT_COUNT && i < allCharacters.Length; i++)
                {
                    _formation[i] = allCharacters[i];
                }
            }
        }
        if (!ServiceLocator.TryGet<StageManager>(out StageManager _stageManager))
        {
            UtilDebug.LogError("서비스 등록 순서 초기화");
        }
        if (_stageManager != null)
        {
            _stageManager.SetParty((CharacterBase[])_formation.Clone());
        }

        ApplyFieldPositions();
        UpdateAllSkillButtons();
    }

    public void OnSceneDestory(SceneId scene)
    {
        // 씬 전환 시 필요하다면 캐릭터 인스턴스 파괴 및 정리
        foreach (var charBase in _spawnedCharacters)
        {
            if (charBase != null) Destroy(charBase.gameObject);
        }
        _spawnedCharacters.Clear();
    }
    #endregion

    public CharacterBase[] GetAllCharacters()
    {
        return allCharacters;
    }

    /// <summary>
    /// 지정한 캐릭터를 편성에 넣거나 뺀다. 이미 편성돼있으면 빼고, 아니면 빈 슬롯에 넣는다
    /// 캐릭터 화면의 "편성하기" 버튼 OnClick에 연결
    /// 파티 편성 변경 시 서버에 전송 - 송태훈
    /// </summary>
    /// <param name="character">토글할 캐릭터</param>
    public void ToggleFormation(CharacterBase character)
    {
        if (character == null)
        {
            return;
        }

        if (!ServiceLocator.TryGet<StageManager>(out StageManager _stageManager))
        { UtilDebug.LogError("서비스 등록 순서 초기화"); }
        if (_stageManager != null && _stageManager.CurrentMode == StageMode.Challenge)
        {
            UtilDebug.LogWarning("챌린지 진행 중에는 파티 편성을 바꿀 수 없음");
            return;
        }

        int existingSlot = System.Array.IndexOf(_formation, character);
        if (existingSlot >= 0)
        {
            _formation[existingSlot] = null;
        }
        else
        {
            int emptySlot = System.Array.IndexOf(_formation, null);
            if (emptySlot < 0)
            {
                UtilDebug.LogWarning($"파티가 이미 꽉 참(최대 {SLOT_COUNT}명) - 다른 캐릭터를 먼저 빼야 함");
                return;
            }

            _formation[emptySlot] = character;
        }

        ApplyFieldPositions();

        if (_stageManager != null)
        {
            _stageManager.SetParty((CharacterBase[])_formation.Clone());
        }

        UpdateAllSkillButtons();
        FormationChanged?.Invoke((CharacterBase[])_formation.Clone());

        var slotMap = new Dictionary<string, int>();
        for (int i = 0; i < allCharacters.Length; i++)
        {
            CharacterBase charcterbase = allCharacters[i];
            int slotIndex = System.Array.IndexOf(_formation, charcterbase);
            slotMap[character.StatData.Id] = slotIndex; // 편성에 없으면 -1, 있으면 0~2
        }
        UserManager.Instance.UpdateAllPartySlotAsync(slotMap, this.destroyCancellationToken).Forget();
    }

    /// <summary>지금 이 캐릭터가 편성에 들어가있는지</summary>
    /// <param name="character">확인할 캐릭터</param>
    public bool IsInFormation(CharacterBase character)
    {
        return character != null && System.Array.IndexOf(_formation, character) >= 0;
    }

    /// <summary>지금 편성 상태 그대로 복사본을 돌려준다 (슬롯 순서, 빈 슬롯은 null)</summary>
    public CharacterBase[] GetFormation()
    {
        return (CharacterBase[])_formation.Clone();
    }

    /// <summary>
    /// 편성 결과대로 5명 전부의 필드 위치/스프라이트/콜라이더를 맞춘다
    /// (편성 안 된 캐릭터는 벤치 위치로 치우고 꺼둠, 편성된 캐릭터는 자기 슬롯 위치로 옮기고 켬)
    /// </summary>
    private void ApplyFieldPositions()
    {
        for (int i = 0; i < allCharacters.Length; i++)
        {
            CharacterBase character = allCharacters[i];
            if (character == null)
            {
                continue;
            }

            SetFieldActive(character, false);
        }

        for (int slot = 0; slot < SLOT_COUNT; slot++)
        {
            CharacterBase character = _formation[slot];
            if (character == null)
            {
                continue;
            }

            if (slot < fieldSlots.Length && fieldSlots[slot] != null)
            {
                character.transform.SetParent(fieldSlots[slot]);
                character.transform.position = fieldSlots[slot].position;
            }

            SetFieldActive(character, true);
        }
    }

    /// <summary>
    /// 캐릭터를 실제로 필드에서 싸울 수 있는 상태로 켜거나(스프라이트/콜라이더 on), 벤치로 치운다(off + 이동)
    /// </summary>
    /// <param name="character">대상 캐릭터</param>
    /// <param name="active">true면 필드 활성, false면 벤치로 치움</param>
    private void SetFieldActive(CharacterBase character, bool active)
    {
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = active;
        }

        if (character.TryGetComponent(out Collider2D collider2D))
        {
            collider2D.enabled = active;
        }

        // 꺼질 땐 벤치 위치로도 옮겨서, 콜라이더가 실수로 안 꺼진 경우에도 실전투와 물리적으로 안 겹치게 함
        if (!active && benchPosition != null)
        {
            character.transform.SetParent(benchPosition);
            character.transform.position = benchPosition.position;
        }
    }

    private void UpdateAllSkillButtons()
    {
        UpdateSkillButtons(slot1SkillButtons, _formation.Length > 0 ? _formation[0] : null);
        UpdateSkillButtons(slot2SkillButtons, _formation.Length > 1 ? _formation[1] : null);
        UpdateSkillButtons(slot3SkillButtons, _formation.Length > 2 ? _formation[2] : null);
    }

    /// <summary>슬롯 하나의 스킬 버튼들(스킬1, 스킬2)이 가리킬 캐릭터를 갱신</summary>
    private void UpdateSkillButtons(SkillButtonUI[] buttons, CharacterBase character)
    {
        if (buttons == null)
        {
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].SetTarget(character);
            }
        }
    }
}
