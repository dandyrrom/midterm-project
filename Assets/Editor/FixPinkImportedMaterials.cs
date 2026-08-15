using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Tree packs (and most Asset Store vegetation) ship Built-in / custom shaders.
/// This URP project cannot draw those, so Unity shows magenta even after lighting
/// bakes or a Render Pipeline Converter pass. This tool remaps those materials
/// to Universal Render Pipeline/Lit and keeps albedo, normal, and cutout settings.
/// </summary>
public class FixPinkImportedMaterials : AssetPostprocessor
{
    const string UrpLitName = "Universal Render Pipeline/Lit";
    const string MenuPath = "Tools/Rendering/Fix Pink Materials (URP)";

    static readonly string[] TreePackPathHints =
    {
        "treepack",
        "tree pack",
        "treespack",
        "trees pack",
        "treespackvol",
        "treepackvol"
    };

    static readonly string[] FoliageNameHints =
    {
        "leaf", "leaves", "foliage", "canopy", "branch", "twig", "atlas"
    };

    static readonly string[] AlbedoNames =
    {
        "_BaseMap", "_MainTex", "_BaseColorMap", "_Diffuse", "_Albedo", "_ColorMap",
        "_BarkTex", "_LeafTex", "_TrunkTex", "_DiffuseMap"
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
        int converted = ConvertProjectMaterials(restrictToTreePackFolders: false);
        EditorUtility.DisplayDialog(
            "Fix Pink Materials",
            converted == 0
                ? "No Built-in / missing-shader materials were found. If trees are still magenta, select a material and confirm its Shader is Universal Render Pipeline/Lit."
                : $"Converted {converted} material(s) to Universal Render Pipeline/Lit.\n\nLeaf materials use Alpha Clipping so canopies keep holes. Re-bake lighting if you already generated lightmaps.",
            "OK");
    }

    Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        if (material == null || !LooksLikeTreePackPath(assetPath))
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
        bool importedTreePack = false;
        foreach (string path in importedAssets)
        {
            if (LooksLikeTreePackPath(path) &&
                (path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
            {
                importedTreePack = true;
                break;
            }
        }

        if (!importedTreePack)
            return;

        EditorApplication.delayCall += () =>
        {
            int converted = ConvertProjectMaterials(restrictToTreePackFolders: true);
            if (converted > 0)
                Debug.Log($"[Fix Pink Materials] Converted {converted} Tree Pack material(s) to URP Lit.");
        };
    }

    static int ConvertProjectMaterials(bool restrictToTreePackFolders)
    {
        Shader urpLit = Shader.Find(UrpLitName);
        if (urpLit == null)
        {
            Debug.LogError("[Fix Pink Materials] Could not find shader '" + UrpLitName + "'.");
            return 0;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material");
        var converted = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (restrictToTreePackFolders && !LooksLikeTreePackPath(path))
                    continue;

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Fix Pink Materials",
                        path,
                        guids.Length == 0 ? 1f : (float)i / guids.Length))
                    break;

                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !NeedsUrpConversion(material))
                    continue;

                ConvertToUrpLit(material, urpLit);
                EditorUtility.SetDirty(material);
                converted.Add(path);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        if (converted.Count > 0)
            AssetDatabase.SaveAssets();

        return converted.Count;
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

        if (name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) ||
            name.StartsWith("Shader Graphs/", StringComparison.Ordinal) ||
            name.StartsWith("HDRP/", StringComparison.Ordinal) ||
            name.StartsWith("Hidden/Universal Render Pipeline/", StringComparison.Ordinal))
            return false;

        return name.StartsWith("Standard", StringComparison.Ordinal) ||
               name.StartsWith("Legacy Shaders/", StringComparison.Ordinal) ||
               name.StartsWith("Nature/", StringComparison.Ordinal) ||
               name.StartsWith("Mobile/", StringComparison.Ordinal) ||
               name.StartsWith("Particles/", StringComparison.Ordinal) ||
               name.StartsWith("Autodesk Interactive", StringComparison.Ordinal) ||
               name.IndexOf("SpeedTree", StringComparison.OrdinalIgnoreCase) >= 0 ||
               LooksLikeTreePackPath(AssetDatabase.GetAssetPath(material));
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
        bool foliage = IsFoliage(material.name, oldShader, AssetDatabase.GetAssetPath(material));

        material.shader = urpLit;

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
               ContainsAny(Path.GetFileNameWithoutExtension(assetPath), FoliageNameHints) ||
               shaderName.IndexOf("Leaf", StringComparison.OrdinalIgnoreCase) >= 0 ||
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
