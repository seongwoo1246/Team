
#if UNITY_EDITOR //에디터에서만 사용하는 기능들
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using Debug = DebugLogger<AddressableEditor>;


/// <summary>
/// 위 에디터 스크립트에 합치기 전에 실험 혹은 합치기 전 필요한 걸 만들기 위해 만든 스크립트
/// </summary>
public class AddressableEditor : EditorWindow
{

    [MenuItem("Tools/Fast Addressable Builder")]
    public static void ShowWindow()
    {
        GetWindow<AddressableEditor>("Addressable Quick Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Addressable 빌드 툴",EditorStyles.boldLabel);
        EditorGUILayout.Space();

        //빌드 데이터 정리
        if(GUILayout.Button("1. Clean Build Cache", GUILayout.Height(30)))
        {
            AddressableAssetSettings.CleanPlayerContent();
            Debug.Log("캐시가 삭제되었습니다.");
        }

        // Addressable 원본 전체 빌드
        if(GUILayout.Button("2. Build New Addressable", GUILayout.Height(30)))
        {
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("전체 빌드가 완료되었습니다.");
        }

        // 변경사항 패치 (업데이트 내용)
        if (GUILayout.Button("3. Update Addressable (patch)", GUILayout.Height(30)))
        {
            string buildPath = ContentUpdateScript.GetContentStateDataPath(false);
            if(!string.IsNullOrEmpty(buildPath) )
            {
                ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, buildPath);
                Debug.Log("패치 빌드가 완료 했습니다.");
            }
            else
            {
                Debug.LogError("패치 파일이 없습니다. 먼저 빌드를 하고 와주세요.");
            }
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
