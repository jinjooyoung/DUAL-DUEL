#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 배틀씬을 "초록 단색 CRT" 분위기로 한 번에 세팅하는 에디터 메뉴 (여러 번 실행해도 같은 결과)
/// Tools > DUAL DUEL > 배틀씬 CRT 연출 세팅
/// 1. Main Camera: 후처리 켜기, 배경 검정
/// 2. 새 Volume Profile 생성 후 Global Volume에 연결 (기존 프로필은 건드리지 않음)
/// 3. Overlay 캔버스 -> Screen Space Camera (후처리 색보정을 받도록. 툴팁 캔버스는 제외)
/// 4. 스캔라인 오버레이 캔버스 생성
/// </summary>
public static class BattleSceneCRTSetup
{
    private const string ProfileFolder = "Assets/Settings/Volumes";
    private const string ProfilePath = ProfileFolder + "/BattleScene_CRT_Profile.asset";
    private const string CRTCanvasName = "CRTCanvas";
    private const int CRTSortingOrder = 200;
    private const int PopupSortingOrder = 100;
    private const int TooltipSortingOrder = 300;

    [MenuItem("Tools/DUAL DUEL/배틀씬 CRT 연출 세팅")]
    public static void Setup()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            EditorUtility.DisplayDialog("CRT 세팅", "MainCamera 태그가 붙은 카메라를 찾을 수 없습니다.", "확인");
            return;
        }

        SetupCamera(cam);
        VolumeProfile profile = GetOrCreateProfile();
        SetupGlobalVolume(profile);
        int converted = ConvertOverlayCanvases(cam);
        SetupCRTOverlay(cam);

        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CRT 세팅 완료] 후처리 ON, 프로필: {ProfilePath}, Overlay -> Camera 캔버스 {converted}개, 스캔라인 오버레이 생성. 씬을 저장하세요.");
    }

    // ---------- 1. 카메라 ----------
    private static void SetupCamera(Camera cam)
    {
        Undo.RecordObject(cam, "CRT Camera");
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.025f, 0.015f);

        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        Undo.RecordObject(data, "CRT Camera Data");
        data.renderPostProcessing = true;   // 이게 꺼져 있으면 Volume이 아무 효과 없음
        EditorUtility.SetDirty(data);
    }

    // ---------- 2. 후처리 프로필 ----------
    private static VolumeProfile GetOrCreateProfile()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
        if (!AssetDatabase.IsValidFolder(ProfileFolder)) AssetDatabase.CreateFolder("Assets/Settings", "Volumes");

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        // 단색 초록: 채도 대신 채널 믹서로 모든 출력 채널을 밝기(0.30R + 0.59G + 0.11B)로 만들고 초록에 가중치
        // (채도 -100 + 컬러 필터 방식은 적용 순서에 따라 초록까지 회색이 될 수 있어서 사용하지 않음)
        ChannelMixer mixer = GetOrAdd<ChannelMixer>(profile);
        SetMixerRow(mixer.redOutRedIn, mixer.redOutGreenIn, mixer.redOutBlueIn, 0.25f);
        SetMixerRow(mixer.greenOutRedIn, mixer.greenOutGreenIn, mixer.greenOutBlueIn, 1.0f);
        SetMixerRow(mixer.blueOutRedIn, mixer.blueOutGreenIn, mixer.blueOutBlueIn, 0.38f);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.postExposure.Override(0.2f);
        color.contrast.Override(20f);
        color.saturation.Override(0f);
        color.colorFilter.Override(Color.white);

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.color.Override(Color.black);
        vignette.intensity.Override(0.42f);
        vignette.smoothness.Override(0.45f);
        vignette.rounded.Override(false);

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.7f);
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(new Color(0.6f, 1f, 0.65f));

        FilmGrain grain = GetOrAdd<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Medium3);
        grain.intensity.Override(0.3f);
        grain.response.Override(0.8f);

        ChromaticAberration chroma = GetOrAdd<ChromaticAberration>(profile);
        chroma.intensity.Override(0.1f);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void SetMixerRow(ClampedFloatParameter r, ClampedFloatParameter g, ClampedFloatParameter b, float scale)
    {
        // 채널 믹서 값은 % 단위
        r.Override(30f * scale);
        g.Override(59f * scale);
        b.Override(11f * scale);
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(false);
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
        }

        component.active = true;
        EditorUtility.SetDirty(component);
        return component;
    }

    // ---------- 3. Global Volume ----------
    private static void SetupGlobalVolume(VolumeProfile profile)
    {
        Volume volume = null;
        foreach (Volume v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (v.isGlobal) { volume = v; break; }
        }

        if (volume == null)
        {
            GameObject go = new GameObject("Global Volume");
            Undo.RegisterCreatedObjectUndo(go, "Create Global Volume");
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
        }

        Undo.RecordObject(volume, "CRT Volume");
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volume);

        if (volume.GetComponent<CRTFlicker>() == null)
            Undo.AddComponent<CRTFlicker>(volume.gameObject);
    }

    // ---------- 4. 캔버스 ----------
    /// <summary>
    /// Overlay 캔버스는 후처리 이후에 그려져서 초록 색보정을 받지 못하므로 Screen Space Camera로 변경.
    /// 툴팁 캔버스는 글자 가독성을 위해 CRT 효과보다 위에 두고, Overlay라면 그대로 둠
    /// </summary>
    private static int ConvertOverlayCanvases(Camera cam)
    {
        int converted = 0;

        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.name == CRTCanvasName) continue;

            bool isTooltipCanvas = canvas.GetComponentInChildren<TooltipPanel>(true) != null;
            Undo.RecordObject(canvas, "CRT Canvas");

            if (isTooltipCanvas)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, TooltipSortingOrder);
                EditorUtility.SetDirty(canvas);
                continue;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 100f;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, PopupSortingOrder);
            EditorUtility.SetDirty(canvas);
            converted++;
        }

        return converted;
    }

    // ---------- 5. 스캔라인 오버레이 ----------
    private static void SetupCRTOverlay(Camera cam)
    {
        GameObject canvasGo = GameObject.Find(CRTCanvasName);
        if (canvasGo == null)
        {
            canvasGo = new GameObject(CRTCanvasName, typeof(RectTransform), typeof(Canvas));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create CRT Canvas");
        }

        canvasGo.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 100f;
        canvas.sortingOrder = CRTSortingOrder;   // 게임 화면과 HUD, 팝업 위 / 툴팁 아래
        // GraphicRaycaster를 붙이지 않아 입력을 막지 않음

        Transform child = canvasGo.transform.Find("Scanlines");
        GameObject lines = child != null ? child.gameObject : null;
        if (lines == null)
        {
            lines = new GameObject("Scanlines", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(CRTOverlay));
            Undo.RegisterCreatedObjectUndo(lines, "Create Scanlines");
            lines.transform.SetParent(canvasGo.transform, false);
        }

        lines.layer = canvasGo.layer;
        RectTransform rt = lines.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        RawImage image = lines.GetComponent<RawImage>();
        image.raycastTarget = false;

        EditorUtility.SetDirty(canvasGo);
        EditorUtility.SetDirty(lines);
    }
}
#endif
