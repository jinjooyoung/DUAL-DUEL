#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DualDuel_CRT_FX 폴더의 텍스처/머티리얼을 항상 올바른 상태로 유지.
/// - 텍스처: Point 필터, 압축 없음, 밉맵 없음, Clamp (픽셀이 뭉개지지 않게)
/// - 머티리얼: URP Particles/Unlit, Color = Alpha 블렌드, Glow = Additive (HDR 블룸용 x1.6)
/// 머티리얼이 분홍색으로 보이면: Tools > DUAL DUEL > CRT FX 머티리얼/텍스처 재설정
/// </summary>
public class DualDuelFXSetup : AssetPostprocessor
{
    const string Folder = "DualDuel_CRT_FX";
    const string ShaderName = "Universal Render Pipeline/Particles/Unlit";
    const float GlowIntensity = 1.6f;

    static bool InFolder(string path) => path.Replace('\\', '/').Contains("/" + Folder + "/");

    void OnPreprocessTexture()
    {
        if (!InFolder(assetPath)) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        ti.sRGBTexture = true;
        ti.maxTextureSize = 2048;
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (string p in imported)
        {
            if (!InFolder(p) || !p.EndsWith(".mat")) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null && (m.shader == null || m.shader.name != ShaderName)) Fix(m, p);
        }
    }

    [MenuItem("Tools/DUAL DUEL/CRT FX 머티리얼·텍스처 재설정")]
    public static void FixAll()
    {
        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D VFX_CRT_"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (InFolder(p)) { AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate); n++; }
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Material VFX_CRT_"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (InFolder(p)) { Fix(AssetDatabase.LoadAssetAtPath<Material>(p), p); n++; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[CRT FX] {n}개 에셋 재설정 완료");
    }

    static void Fix(Material m, string path)
    {
        Shader sh = Shader.Find(ShaderName);
        if (sh == null) { Debug.LogWarning($"[CRT FX] '{ShaderName}' 셰이더를 찾을 수 없습니다 (URP 필요)."); return; }

        bool additive = Path.GetFileNameWithoutExtension(path).EndsWith("_Glow");
        string texPath = path.Replace("/Materials/", "/Textures/").Replace(".mat", ".png");
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        m.shader = sh;
        if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
        float k = additive ? GlowIntensity : 1f;
        m.SetColor("_BaseColor", new Color(k, k, k, 1f));

        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetFloat("_Cull", 0f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
    }
}
#endif
