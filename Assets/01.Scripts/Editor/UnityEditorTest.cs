using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;


/// <summary>
/// 위 에디터 스크립트에 합치기 전에 실험 혹은 합치기 전 필요한 걸 만들기 위해 만든 스크립트
/// </summary>
public class UnityEditorTest : EditorWindow
{
#if UNITY_EDITOR //에디터에서만 사용하는 기능들
    [MenuItem("Tools/Fast Addressable Builder")]
    public static void ShowWindow()
    {
        GetWindow<UnityEditorTest>("Addressable Quick Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Addressable 빌드 툴",EditorStyles.boldLabel);
        EditorGUILayout.Space();

        //빌드 데이터 정리
        if(GUILayout.Button("1. Clean Build Cache", GUILayout.Height(30)))
        {
            AddressableAssetSettings.CleanPlayerContent();
            //로그 Addressable 캐시가 삭제된다는 내용
        }

        // Addressable 원본 전체 빌드
        if(GUILayout.Button("2. Build New Addressable", GUILayout.Height(30)))
        {
            AddressableAssetSettings.BuildPlayerContent();
            //로그 전체 빌드가 완료 되었다는 내용
        }

        // 변경사항 패치 (업데이트 내용)
        if (GUILayout.Button("3. Update Addressable (patch)", GUILayout.Height(30)))
        {
            //string buildPath =
        }

        EditorGUILayout.Space();
        GUI.backgroundColor = Color.gold;

        //원클릭 올인원 빌드(Addressable + App Player)
        if(GUILayout.Button("One_Click Full Build(Addressable + App Player)", GUILayout.Height(45)))
        {
            //에셋 먼저 빌드
            AddressableAssetSettings.BuildPlayerContent();
            // 그 다음 플레이어  설정 후 실행 
            BuildPlayerOptions buildPlayerOptions = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }


        
    }






#endif
}
