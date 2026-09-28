/*
  작업자 - 홍준호
  캐릭터 패널 상단 캐릭터 선택 담당 스크립트
 */

/* 공동 작업자 - 송태훈
원본 코드를 최대한 덜 훼손하는 상태로 해결할 수 있는 방식으로 리펙토링
캐릭터 선택 버튼이 보유 중인 캐릭터 선택에 대한 패널이 연결되도록 수정
 */

using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UtilDebug = DebugLogger<CharacterSelectController>;

public class CharacterSelectController : MonoBehaviour, ILoadable

{
    [Header("장비 UI")]
    [SerializeField] private EquipmentInventoryController equipmentInventoryController;

    [Header("캐릭터 이미지")]
    [SerializeField] private UnityEngine.UI.Image characterImage;

    [Header("캐릭터 이미지")]
    [SerializeField] private Sprite warriorSprite;
    [SerializeField] private Sprite mageSprite;
    [SerializeField] private Sprite healerSprite;
    [SerializeField] private Sprite paladinSprite;
    [SerializeField] private Sprite archerSprite;

    private readonly Dictionary<AttackType, CharacterBase> _characterMap = new();
    private AttackType currentAttackType = AttackType.Physical;
    public AttackType CurrentAttackType => currentAttackType;

    private void Awake()
    {
        SceneLoadManager.Instance.RegisterLoadable(this);
    }

    // 현재 선택된 캐릭터
    public CharacterBase CurrentCharacter
    {
        get
        {
            _characterMap.TryGetValue(currentAttackType, out var character);

            return character;
        }
    }

    public int LoadOrder => 50;

    // 캐릭터 패널 상단 캐릭터 선택버튼
    public void SelectWarrior()
    {
        SelectType(AttackType.Physical);
    }

    public void SelectMage()
    {
        SelectType(AttackType.Magic);
    }

    public void SelectHealer()
    {
        SelectType(AttackType.Heal);
    }

    public void SelectPaladin()
    {
        SelectType(AttackType.Paladin);
    }

    public void SelectArcher()
    {
        SelectType(AttackType.Archer);
    }

    private void SelectType(AttackType type)
    {
        currentAttackType = type;
        RefreshCharacterImage();
        if (equipmentInventoryController != null)
        {
            equipmentInventoryController.RefreshEquippedSlots();
        }
    }

    private void RefreshCharacterImage()
    {
        if (characterImage == null)
            return;

        switch (currentAttackType)
        {
            case AttackType.Physical:
                characterImage.sprite = warriorSprite;
                break;

            case AttackType.Magic:
                characterImage.sprite = mageSprite;
                break;

            case AttackType.Heal:
                characterImage.sprite = healerSprite;
                break;

            case AttackType.Paladin:
                characterImage.sprite = paladinSprite;
                break;

            case AttackType.Archer:
                characterImage.sprite = archerSprite;
                break;
        }

        characterImage.enabled = characterImage.sprite != null;
    }

    public UniTask OnSceneLoadCreate(SceneId scene)
    {
        return UniTask.CompletedTask;
    }

    public void Init(SceneId scene)
    {
        if(!ServiceLocator.TryGet<PartyFormationManager>(out var service))
        {
            UtilDebug.LogError("PartyFormationManager와 초기화 순서 확인");
            return;
        }

        _characterMap.Clear();
        var characters = service.GetAllCharacters();
        if(characters != null)
        {
            foreach( var character in characters)
            {
                if (character != null && character.StatData != null)
                {
                    _characterMap[character.StatData.AttackType] = character;
                }
            }
        }

        UtilDebug.Log("캐릭터 선택 컨트롤러 매핑 완료");
        SelectWarrior();
        if(equipmentInventoryController != null)
        {
            equipmentInventoryController.gameObject.SetActive(false);
        }
    }

    public void OnSceneDestory(SceneId scene)
    {
        SceneLoadManager.Instance.UnregisterLoadable(this);
    }
}
