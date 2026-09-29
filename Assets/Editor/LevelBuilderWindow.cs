using UnityEditor;
using UnityEngine;

public class LevelBuilderWindow : EditorWindow
{
    private GameObject platformPrefab;
    private float gapX = 0f;          // 이전 발판 오른쪽 끝에서 얼마나 띄울지 (0이면 딱 붙임)
    private float verticalOffset = 0f; // 계단식으로 높낮이 주고 싶을 때 (양수면 위로, 음수면 아래로)
    private float newScaleX = 8.72f;
    private float newScaleY = 0.9f;

    [MenuItem("Tools/Ori Level Builder")]
    public static void ShowWindow()
    {
        GetWindow<LevelBuilderWindow>("Level Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("발판 이어붙이기", EditorStyles.boldLabel);

        platformPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Platform Prefab", platformPrefab, typeof(GameObject), false);

        gapX = EditorGUILayout.FloatField("Gap (간격)", gapX);
        verticalOffset = EditorGUILayout.FloatField("Vertical Offset (계단식 높낮이)", verticalOffset);
        newScaleX = EditorGUILayout.FloatField("New Platform Scale X", newScaleX);
        newScaleY = EditorGUILayout.FloatField("New Platform Scale Y", newScaleY);

        EditorGUILayout.Space();

        if (GUILayout.Button("선택한 발판 다음에 이어붙이기"))
        {
            AttachAfterSelected();
        }
    }

    private void AttachAfterSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("Hierarchy에서 기준이 될 발판(이전 발판)을 먼저 선택하세요.");
            return;
        }

        if (!selected.TryGetComponent(out BoxCollider2D prevCollider))
        {
            Debug.LogWarning("선택한 오브젝트에 BoxCollider2D가 없습니다.");
            return;
        }

        if (platformPrefab == null)
        {
            Debug.LogWarning("Platform Prefab을 지정해주세요.");
            return;
        }

        // 저번에 손으로 계산했던 공식 그대로: 콜라이더 실제 월드 경계 계산
        Transform prevT = selected.transform;
        float prevRightX = prevT.position.x
            + (prevCollider.offset.x + prevCollider.size.x / 2f) * prevT.lossyScale.x;
        float prevTopY = prevT.position.y
            + (prevCollider.offset.y + prevCollider.size.y / 2f) * prevT.lossyScale.y;

        // 새 발판 생성 (프리팹 연결 유지된 상태로 인스턴스화)
        GameObject newPlatform = (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab, selected.transform.parent);
        Undo.RegisterCreatedObjectUndo(newPlatform, "Create Platform"); // Ctrl+Z로 취소 가능하게

        newPlatform.transform.localScale = new Vector3(newScaleX, newScaleY, 1f);

        // 새 발판의 왼쪽 끝 X, 윗면 Y가 이전 발판 끝에 정확히 맞도록 역산
        // (새 발판도 BlockPlatform 기본 콜라이더 Size 1,1 / Offset 0,0을 전제로 계산)
        float newX = prevRightX + gapX + (newScaleX / 2f);
        float newY = (prevTopY + verticalOffset) - (newScaleY / 2f);

        newPlatform.transform.position = new Vector3(newX, newY, 0f);

        Selection.activeGameObject = newPlatform; // 다음 클릭에 바로 이어서 체이닝되도록 자동 선택
    }
}