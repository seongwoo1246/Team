/// <summary>
/// 문자열 고유 Id를 가지는 데이터 에셋용 인터페이스
/// </summary>
public interface IIdentifiable
{
    string Id { get; }
}

public interface ISceneBootstrap
{
    Cysharp.Threading.Tasks.UniTask OnSceneReadyAsync();
}

public interface ISyncable
{
    /// <summary>
    /// 현재 데이터를 서버에 저장할 최신 메모리로 갱신
    /// </summary>
    void SyncToUserMemory();
}