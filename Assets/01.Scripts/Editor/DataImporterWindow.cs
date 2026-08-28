/*
깃허브에 누가올려놔서 베껴옴 개나이스
구글 시트에서 내보낸 CSV를 읽어 ScriptableObject 에셋을 자동 갱신하는 에디터툴

이 파일은 Editor 폴더에 있으므로 실제 게임 빌드에는 포함되지 않음! (개발자만가능)

혹시모르니 툴 사용법:
  1) 구글 시트의 각 탭을 CSV로 다운로드
  2) 프로젝트의  Assets/03.Data/02.CSV/  폴더에 넣는다
        Characters.csv  (또는 3_Characters.csv)
        Monsters.csv    (또는 5_Monsters.csv)
  3) 상단 메뉴  Tools → 데이터 임포터  에서 창을 열고 버튼을 누름
  4) 결과 SO는  Assets/03.Data/00.UnitSO/Characters , /Monsters  에 생성됨
*/

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CSV → ScriptableObject 임포터 창.
/// </summary>
public class DataImporterWindow : EditorWindow
{
    // 경로 상수
    private const string CSV_FOLDER = "Assets/03.Data/02.CSV";
    private const string OUTPUT_ROOT = "Assets/03.Data/00.UnitSO";
    private const string CHARACTER_OUTPUT = OUTPUT_ROOT + "/Characters";
    private const string MONSTER_OUTPUT = OUTPUT_ROOT + "/Monsters";

    // CSV 파일 이름 후보 (숫자 접두사가 붙어도 찾을 수 있게)
    private static readonly string[] CharacterCsvNames = { "Characters.csv", "3_Characters.csv" };
    private static readonly string[] MonsterCsvNames = { "Monsters.csv", "5_Monsters.csv" };

    [MenuItem("Tools/데이터 임포터")]
    private static void Open()
    {
        DataImporterWindow window = GetWindow<DataImporterWindow>("데이터 임포터");
        window.minSize = new Vector2(360f, 220f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("구글 시트 CSV → ScriptableObject", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            $"CSV 위치:  {CSV_FOLDER}\n결과 SO:  {OUTPUT_ROOT}",
            MessageType.Info);

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("캐릭터 CSV 가져오기", GUILayout.Height(30f)))
        {
            ImportCharacters();
        }

        if (GUILayout.Button("몬스터 CSV 가져오기", GUILayout.Height(30f)))
        {
            ImportMonsters();
        }

        EditorGUILayout.Space(6f);

        if (GUILayout.Button("전체 가져오기", GUILayout.Height(34f)))
        {
            ImportCharacters();
            ImportMonsters();
        }
    }

    /// <summary>
    /// Characters CSV를 읽어 BaseStatData 에셋을 생성/갱신한다.
    /// </summary>
    private void ImportCharacters()
    {
        string path = FindCsv(CharacterCsvNames);
        if (path == null)
        {
            return;
        }

        EnsureFolder(CHARACTER_OUTPUT);
        List<Dictionary<string, string>> rows = ReadCsv(path);
        int count = 0;

        foreach (Dictionary<string, string> row in rows)
        {
            if (!row.TryGetValue("id", out string id) || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            string assetPath = $"{CHARACTER_OUTPUT}/{id}.asset";
            BaseStatData asset = GetOrCreateAsset<BaseStatData>(assetPath);
            ApplyRow(asset, row);
            count++;
        }

        SaveAll();
        DebugLogger<DataImporterWindow>.Log($"캐릭터 {count}개 가져오기 완료");
        EditorUtility.DisplayDialog("데이터 임포터", $"캐릭터 {count}개 가져오기 완료", "확인");
    }

    /// <summary>
    /// Monsters CSV를 읽어 MonsterStatData 에셋 갱신함
    /// </summary>
    private void ImportMonsters()
    {
        string path = FindCsv(MonsterCsvNames);
        if (path == null)
        {
            return;
        }

        EnsureFolder(MONSTER_OUTPUT);
        List<Dictionary<string, string>> rows = ReadCsv(path);
        int count = 0;

        foreach (Dictionary<string, string> row in rows)
        {
            if (!row.TryGetValue("id", out string id) || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            string assetPath = $"{MONSTER_OUTPUT}/{id}.asset";
            MonsterStatData asset = GetOrCreateAsset<MonsterStatData>(assetPath);
            ApplyRow(asset, row);
            count++;
        }

        SaveAll();
        DebugLogger<DataImporterWindow>.Log($"몬스터 {count}개 가져오기 완료");
        EditorUtility.DisplayDialog("데이터 임포터", $"몬스터 {count}개 가져오기 완료", "확인");
    }

    // 에셋 유틸

    /// <summary>
    /// 경로에 에셋이 있으면 불러오고, 없으면 새로 만든다. (GUID 유지가 목적)
    /// </summary>
    private static T GetOrCreateAsset<T>(string assetPath) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
        {
            asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }
        return asset;
    }

    /// <summary>
    /// CSV 한 줄의 값들을 SO의 [SerializeField] 필드에 채운다.
    /// 컬럼명을 필드명으로 바꿔 매칭 base_power → basePower 보기좋게
    /// </summary>
    private static void ApplyRow(ScriptableObject asset, Dictionary<string, string> row)
    {
        SerializedObject so = new SerializedObject(asset);

        foreach (KeyValuePair<string, string> cell in row)
        {
            string fieldName = SnakeToCamel(cell.Key);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                continue;
            }

            SetProperty(prop, cell.Value);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    /// <summary>
    /// 문자열 값을 SerializedProperty 타입에 맞게 넣는다.
    /// </summary>
    private static void SetProperty(SerializedProperty prop, string raw)
    {
        string value = raw.Trim();

        switch (prop.propertyType)
        {
            case SerializedPropertyType.String:
                prop.stringValue = value;
                break;

            case SerializedPropertyType.Integer:
                if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int i))
                {
                    prop.intValue = i;
                }
                break;

            case SerializedPropertyType.Float:
                if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float f))
                {
                    prop.floatValue = f;
                }
                break;

            case SerializedPropertyType.Boolean:
                prop.boolValue = value == "1" || value.ToLowerInvariant() == "true";
                break;

            case SerializedPropertyType.Enum:
                int index = System.Array.IndexOf(prop.enumNames, value);
                if (index >= 0)
                {
                    prop.enumValueIndex = index;
                }
                break;
        }
    }

    private static void SaveAll()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// "Assets/A/B/C" 형태의 폴더가 없으면 순서대로 만든다.
    /// </summary>
    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int p = 1; p < parts.Length; p++)
        {
            string next = $"{current}/{parts[p]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[p]);
            }
            current = next;
        }
    }

    // CSV 파싱

    /// <summary>
    /// 후보 이름들 중 실제로 존재하는 CSV 경로를 찾는다.
    /// </summary>
    private static string FindCsv(string[] candidateNames)
    {
        for (int n = 0; n < candidateNames.Length; n++)
        {
            string path = $"{CSV_FOLDER}/{candidateNames[n]}";
            if (File.Exists(path))
            {
                return path;
            }
        }
        return null;
    }

    /// <summary>
    /// CSV 파일을 읽어 컬럼명 → 값 딕셔너리 리스트로 돌려준다.
    /// </summary>
    private static List<Dictionary<string, string>> ReadCsv(string path)
    {
        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2)
        {
            return result;
        }

        string[] headers = SplitCsvLine(lines[0]);
        
        if (headers.Length > 0)
        {
            headers[0] = headers[0].TrimStart('﻿').Trim();
        }

        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            string[] cells = SplitCsvLine(lines[lineIndex]);
            Dictionary<string, string> row = new Dictionary<string, string>();

            for (int c = 0; c < headers.Length && c < cells.Length; c++)
            {
                row[headers[c].Trim()] = cells[c].Trim();
            }

            result.Add(row);
        }

        return result;
    }

    /// <summary>
    /// CSV 한 줄을 쉼표로 나눈다 큰따옴표로 감싼 셀 안의 쉼표는 무시함.
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        List<string> fields = new List<string>();
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];

            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Length = 0;
            }
            else
            {
                current.Append(ch);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }

    /// <summary>
    /// snake_case 를 camelCase 로 바꾼다. base_power → basePower 컬럼명을 필드명으로
    /// </summary>
    private static string SnakeToCamel(string snake)
    {
        if (string.IsNullOrEmpty(snake) || !snake.Contains("_"))
        {
            return snake;
        }

        string[] parts = snake.Split('_');
        StringBuilder sb = new StringBuilder(parts[0]);
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length == 0)
            {
                continue;
            }
            sb.Append(char.ToUpperInvariant(parts[i][0]));
            if (parts[i].Length > 1)
            {
                sb.Append(parts[i].Substring(1));
            }
        }
        return sb.ToString();
    }
}
