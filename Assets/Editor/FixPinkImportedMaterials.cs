using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Magenta meshes in this URP project mean the material's shader cannot run.
/// Tree packs often use Built-in or custom foliage shaders; Unity's converter
/// skips those. This tool assigns Universal Render Pipeline/Lit and keeps maps.
/// </summary>
public class FixPinkImportedMaterials : AssetPostprocessor
{
    const string UrpLitName = "Universal Render Pipeline/Lit";
    const string TreeBarkName = "Nature/Tree Creator Bark";
    const string TreeLeavesName = "Nature/Tree Creator Leaves";
    const string TreeLeavesFastName = "Nature/Tree Creator Leaves Fast";
    const string MenuPath = "Tools/Rendering/Fix Pink Materials (URP)";
    const string SelectedMenuPath = "Tools/Rendering/Fix Selected Pink Materials (URP)";

    static readonly string[] SkipShaderPrefixes =
    {
        "Universal Render Pipeline/",
        "Hidden/Universal Render Pipeline/",
        "Shader Graphs/",
        "Skybox/",
        "UI/",
        "Sprites/",
        "TextMeshPro/",
        "GUI/",
        "Hidden/Internal-",
        "Hidden/Core/"
    };

    static readonly string[] TreePackPathHints =
    {
        "treepack", "tree pack", "treespack", "trees pack", "treepackvol"
    };

    static readonly string[] FoliageNameHints =
    {
        "leaf", "leaves", "foliage", "canopy", "branch", "twig", "atlas", "fruit"
    };

    static readonly string[] AlbedoNames =
    {
        "_BaseMap", "_MainTex", "_BaseColorMap", "_Diffuse", "_Albedo", "_ColorMap",
        "_BarkTex", "_LeafTex", "_TrunkTex", "_DiffuseMap", "_Texture"
    };

    static readonly string[] NormalNames =
    {
        "_BumpMap", "_NormalMap", "_Normal", "_Bump", "_NormalTex"
    };

    static readonly string[] MaskNames =
    {
        "_MetallicGlossMap", "_MaskMap", "_MetallicMap", "_SpecGlossMap", "_RoughnessMap"
    };

    static readonly string[] OcclusionNames =
    {
        "_OcclusionMap", "_AO", "_AOMap"
    };

    [MenuItem(MenuPath)]
    public static void FixFromMenu()
    {
        ConversionResult result = ConvertProjectAndSceneMaterials();
        EditorUtility.DisplayDialog("Fix Pink Materials", BuildDialogMessage(result), "OK");
    }

    [MenuItem(SelectedMenuPath)]
    public static void FixSelectedFromMenu()
    {
        Shader urpLit = Shader.Find(UrpLitName);
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Fix Pink Materials", "Could not find Universal Render Pipeline/Lit.", "OK");
            return;
        }

        int converted = 0;
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            if (obj is Material material && NeedsUrpConversion(material))
            {
                ConvertToUrpLit(material, urpLit);
                EditorUtility.SetDirty(material);
                converted++;
                continue;
            }

            GameObject go = obj as GameObject;
            if (go == null)
                continue;

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] shared = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null || !NeedsUrpConversion(shared[i]))
                        continue;
                    ConvertToUrpLit(shared[i], urpLit);
                    EditorUtility.SetDirty(shared[i]);
                    converted++;
                    changed = true;
                }

                if (changed)
                    EditorUtility.SetDirty(renderer);
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Fix Pink Materials",
            converted == 0
                ? "Select a pink tree (or its .mat file) in the Hierarchy or Project window, then run this again."
                : $"Converted {converted} selected material(s) to Universal Render Pipeline/Lit.",
            "OK");
    }

    Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        if (material == null)
            return null;

        Shader urpLit = Shader.Find(UrpLitName);
        if (urpLit == null)
            return null;

        if (NeedsUrpConversion(material))
            ConvertToUrpLit(material, urpLit);

        return material;
    }

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool importedMaterialOrModel = false;
        foreach (string path in importedAssets)
        {
            if (path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                importedMaterialOrModel = true;
                break;
            }
        }

        if (!importedMaterialOrModel)
            return;

        EditorApplication.delayCall += () =>
        {
            ConversionResult result = ConvertProjectAndSceneMaterials();
            if (result.Converted > 0)
                Debug.Log($"[Fix Pink Materials] Converted {result.Converted} imported material(s) to URP Lit.");
        };
    }

    struct ConversionResult
    {
        public int Converted;
        public int Scanned;
        public List<string> ShaderNames;
    }

    static ConversionResult ConvertProjectAndSceneMaterials()
    {
        var result = new ConversionResult
        {
            ShaderNames = new List<string>()
        };

        Shader urpLit = Shader.Find(UrpLitName);
        if (urpLit == null)
        {
            Debug.LogError("[Fix Pink Materials] Could not find shader '" + UrpLitName + "'.");
            return result;
        }

        var convertedIds = new HashSet<int>();
        var shaderNames = new HashSet<string>();
        string[] guids = AssetDatabase.FindAssets("t:Material");
        result.Scanned = guids.Length;

        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Fix Pink Materials",
                        path,
                        guids.Length == 0 ? 1f : (float)i / guids.Length))
                    break;

                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                    continue;

                if (material.shader != null)
                    shaderNames.Add(material.shader.name);

                if (!NeedsUrpConversion(material))
                    continue;

                ConvertToUrpLit(material, urpLit);
                EditorUtility.SetDirty(material);
                convertedIds.Add(material.GetInstanceID());
            }

            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Renderer renderer in renderers)
            {
                Material[] shared = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    Material material = shared[i];
                    if (material == null)
                        continue;

                    if (material.shader != null)
                        shaderNames.Add(material.shader.name);

                    if (!NeedsUrpConversion(material) || convertedIds.Contains(material.GetInstanceID()))
                        continue;

                    ConvertToUrpLit(material, urpLit);
                    EditorUtility.SetDirty(material);
                    convertedIds.Add(material.GetInstanceID());
                    changed = true;
                }

                if (changed)
                    EditorUtility.SetDirty(renderer);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        if (convertedIds.Count > 0)
            AssetDatabase.SaveAssets();

        result.Converted = convertedIds.Count;
        result.ShaderNames.AddRange(shaderNames);
        result.ShaderNames.Sort();
        return result;
    }

    static string BuildDialogMessage(ConversionResult result)
    {
        if (result.Converted > 0)
        {
            return $"Converted {result.Converted} material(s) to Universal Render Pipeline/Lit.\n\n" +
                   "Leaf/tree materials use Alpha Clipping so canopies keep holes. " +
                   "If you already baked lighting, bake again.";
        }

        var message = new StringBuilder();
        message.Append("Nothing was converted. Scanned ");
        message.Append(result.Scanned);
        message.Append(" project material(s).");

        if (result.Scanned <= 2)
        {
            message.Append("\n\nThis Unity project looks empty (no Tree Pack). ");
            message.Append("Switch Git back to the danni branch, then import TreePackVol.1 there, ");
            message.Append("or select a pink tree and use Tools > Rendering > Fix Selected Pink Materials (URP).");
        }
        else if (result.ShaderNames.Count > 0)
        {
            message.Append("\n\nShaders currently in use:\n");
            int shown = 0;
            foreach (string name in result.ShaderNames)
            {
                message.Append("- ");
                message.Append(name);
                message.Append('\n');
                shown++;
                if (shown >= 12)
                    break;
            }

            message.Append("\nIf trees are still pink, select one in the Hierarchy and run ");
            message.Append("Tools > Rendering > Fix Selected Pink Materials (URP).");
        }

        return message.ToString();
    }

    static bool NeedsUrpConversion(Material material)
    {
        Shader shader = material.shader;
        if (shader == null)
            return true;

        string name = shader.name;
        if (string.IsNullOrEmpty(name) ||
            name == "Hidden/InternalErrorShader" ||
            name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (ShaderUtil.ShaderHasError(shader) || !shader.isSupported)
            return true;

        foreach (string prefix in SkipShaderPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                return false;
        }

        if (name.StartsWith("Hidden/", StringComparison.Ordinal) ||
            name.StartsWith("Nature/Tree", StringComparison.Ordinal))
            return false;

        return true;
    }

    static Shader ChooseTargetShader(Material material, Shader urpLit)
    {
        string oldShader = material.shader != null ? material.shader.name : string.Empty;
        string assetPath = AssetDatabase.GetAssetPath(material);
        bool foliage = IsFoliage(material.name, oldShader, assetPath);
        bool treeAsset = material.name.IndexOf("Optimized", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         LooksLikeTreePackPath(assetPath) ||
                         oldShader.IndexOf("Tree", StringComparison.OrdinalIgnoreCase) >= 0;

        if (treeAsset && foliage)
        {
            Shader leaves = Shader.Find(TreeLeavesFastName) ?? Shader.Find(TreeLeavesName);
            if (leaves != null)
                return leaves;
        }

        if (treeAsset)
        {
            Shader bark = Shader.Find(TreeBarkName);
            if (bark != null)
                return bark;
        }

        return urpLit;
    }

    static void ConvertToUrpLit(Material material, Shader urpLit)
    {
        Texture albedo = FirstTexture(material, AlbedoNames);
        Texture normal = FirstTexture(material, NormalNames);
        Texture mask = FirstTexture(material, MaskNames);
        Texture occlusion = FirstTexture(material, OcclusionNames);
        Color baseColor = FirstColor(material, new[] { "_BaseColor", "_Color" }, Color.white);
        float cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.4f;
        string oldShader = material.shader != null ? material.shader.name : string.Empty;
        string assetPath = AssetDatabase.GetAssetPath(material);
        bool foliage = IsFoliage(material.name, oldShader, assetPath);
        Shader target = ChooseTargetShader(material, urpLit);

        material.shader = target;

        if (albedo != null)
            material.SetTexture("_BaseMap", albedo);
        material.SetColor("_BaseColor", baseColor);

        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 1f);
        }

        if (mask != null)
            material.SetTexture("_MetallicGlossMap", mask);

        if (occlusion != null)
            material.SetTexture("_OcclusionMap", occlusion);

        material.SetFloat("_WorkflowMode", 1f);
        material.SetFloat("_Metallic", foliage ? 0f : 0.05f);
        material.SetFloat("_Smoothness", foliage ? 0.12f : 0.28f);
        material.SetFloat("_EnvironmentReflections", 1f);

        if (foliage)
        {
            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", cutoff > 0f ? cutoff : 0.4f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.doubleSidedGI = true;
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else
        {
            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 0f);
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.renderQueue = (int)RenderQueue.Geometry;
        }
    }

    static Texture FirstTexture(Material material, string[] names)
    {
        foreach (string name in names)
        {
            if (!material.HasProperty(name))
                continue;
            Texture texture = material.GetTexture(name);
            if (texture != null)
                return texture;
        }

        return null;
    }

    static Color FirstColor(Material material, string[] names, Color fallback)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
                return material.GetColor(name);
        }

        return fallback;
    }

    static bool IsFoliage(string materialName, string shaderName, string assetPath)
    {
        return ContainsAny(materialName, FoliageNameHints) ||
               ContainsAny(shaderName, FoliageNameHints) ||
               ContainsAny(assetPath, FoliageNameHints) ||
               shaderName.IndexOf("Soft Occlusion Leaves", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool LooksLikeTreePackPath(string path)
    {
        return ContainsAny(path, TreePackPathHints);
    }

    static bool ContainsAny(string value, string[] hints)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (string hint in hints)
        {
            if (value.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}
