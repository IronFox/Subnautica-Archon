﻿using Subnautica_Archon.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VehicleFramework;

namespace Subnautica_Archon.MaterialAdapt
{

    internal interface IShaderVariable
    {
        ShaderPropertyType Type { get; }

        /// <summary>
        /// Updates a material according to the preserved values present in the local variable
        /// </summary>
        /// <param name="m">Material to update</param>
        /// <param name="logConfig">Log Configuration</param>
        void SetTo(Material m, Logging logConfig);
    }

    internal readonly struct ColorVariable : IShaderVariable
    {
        public ShaderPropertyType Type => ShaderPropertyType.Color;
        public Color Value { get; }
        public string Name { get; }
        public ColorVariable(Material m, string n)
        {
            Value = m.GetColor(n);
            Name = n;
        }

        /// <summary>
        /// Updates a single color variable on the given material
        /// </summary>
        /// <param name="m">Material to change</param>
        /// <param name="name">Variable name to change</param>
        /// <param name="value">Color value to set</param>
        /// <param name="logConfig">Log Configuration</param>
        public static void Set(Material m, string name, Color value, Logging logConfig)
        {
            try
            {
                var old = m.GetColor(name);
                if (old == value)
                    return;
                logConfig.LogMaterialVariableSet(ShaderPropertyType.Color, name, old, value, m);
                m.SetColor(name, value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                logConfig.LogError($"Failed to set color {name} ({value}) on material {m.NiceName()}");
            }
        }

        public void SetTo(Material m, Logging logConfig)
        {
            Set(m, Name, Value, logConfig);
        }
    }

    internal readonly struct VectorVariable : IShaderVariable
    {
        public ShaderPropertyType Type => ShaderPropertyType.Vector;


        public Vector4 Value { get; }
        public string Name { get; }
        public VectorVariable(Material m, string n)
        {
            Value = m.GetVector(n);
            Name = n;
        }

        public void SetTo(Material m, Logging logConfig)
        {
            try
            {
                var old = m.GetVector(Name);
                if (old == Value)
                    return;
                logConfig.LogMaterialVariableSet(Type, Name, old, Value, m);
                m.SetVector(Name, Value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                logConfig.LogError($"Failed to set {Type} {Name} ({Value}) on material {m.NiceName()}");
            }
        }
    }

    internal readonly struct TextureVariable : IShaderVariable
    {
        public ShaderPropertyType Type => ShaderPropertyType.Vector;


        public Texture Texture { get; }
        public Vector2 Offset { get; }
        public Vector2 Scale { get; }
        public string Name { get; }
        public TextureVariable(Material m, string n)
        {
            Texture = m.GetTexture(n);
            Offset = m.GetTextureOffset(n);
            Scale = m.GetTextureScale(n);
            Name = n;
        }

        public void SetTo(Material m, Logging logConfig)
        {
            try
            {
                var oldT = m.GetTexture(Name);
                var oldO = m.GetTextureOffset(Name);
                var oldS = m.GetTextureScale(Name);
                if (oldT != Texture)
                {
                    logConfig.LogMaterialVariableSet(Type, Name, oldT, Texture, m);
                    m.SetTexture(Name, Texture);
                }
                if (oldO != Offset)
                {
                    logConfig.LogMaterialVariableSet(Type, Name, oldO, Offset, m);
                    m.SetTextureOffset(Name, Offset);
                }
                if (oldS != Scale)
                {
                    logConfig.LogMaterialVariableSet(Type, Name, oldS, Scale, m);
                    m.SetTextureScale(Name, Scale);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                logConfig.LogError($"Failed to set {Type} {Name} ({Texture.NiceName()}, {Offset}, {Scale}) on material {m.NiceName()}");
            }
        }
    }

    internal readonly struct FloatVariable : IShaderVariable
    {
        public ShaderPropertyType Type => ShaderPropertyType.Texture;


        public float Value { get; }
        public string Name { get; }
        public FloatVariable(Material m, string n)
        {
            Value = m.GetFloat(n);
            Name = n;
        }

        public void SetTo(Material m, Logging logConfig)
        {
            try
            {
                var old = m.GetFloat(Name);
                if (old == Value)
                    return;
                logConfig.LogMaterialVariableSet(Type, Name, old, Value, m);
                m.SetFloat(Name, Value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                logConfig.LogError($"Failed to set {Type} {Name} ({Value.ToString(CultureInfo.InvariantCulture)}) on material {m}");
            }
        }
    }

    /// <summary>
    /// Read-only material definition as retrieved from some existing material
    /// </summary>
    /// <author>https://github.com/IronFox</author>
    public class MaterialPrototype
    {
        /// <summary>
        /// True if this instance was created without a source material.
        /// All local values are empty/default if true
        /// </summary>
        public bool IsEmpty { get; private set; }

        private HashSet<string> ShaderKeywords { get; } = new HashSet<string>();
        public MaterialGlobalIlluminationFlags MaterialGlobalIlluminationFlags { get; }
        private ColorVariable[] ColorVariables { get; }
        private VectorVariable[] VectorVariables { get; }
        private TextureVariable[] TextureVariables { get; }
        private FloatVariable[] FloatVariables { get; }

        /// <summary>
        /// Updates all recorded shader variables in the specified material
        /// </summary>
        /// <param name="m">Target material</param>
        /// <param name="logConfig">Log Configuration</param>
        /// <param name="variableNamePredicate">
        /// Optional predicate to only check/update certain shader variables by name.
        /// If non-null updates only variables for which this function returns true</param>
        public void ApplyTo(Material m, Logging logConfig, Func<string, bool> variableNamePredicate = null)
        {
            variableNamePredicate = variableNamePredicate ?? (_ => true);

            foreach (var v in ColorVariables)
                if (variableNamePredicate(v.Name))
                    v.SetTo(m, logConfig);
            foreach (var v in VectorVariables)
                if (variableNamePredicate(v.Name))
                    v.SetTo(m, logConfig);
            foreach (var v in FloatVariables)
                if (variableNamePredicate(v.Name))
                    v.SetTo(m, logConfig);
            foreach (var v in TextureVariables)
                if (variableNamePredicate(v.Name))
                    v.SetTo(m, logConfig);

            if (m.globalIlluminationFlags != MaterialGlobalIlluminationFlags)
            {
                logConfig.LogMaterialChange($"Applying global illumination flags ({m.globalIlluminationFlags} -> {MaterialGlobalIlluminationFlags})");

                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags;
            }

            foreach (var existing in m.shaderKeywords.ToList())
                if (!ShaderKeywords.Contains(existing))
                {
                    logConfig.LogMaterialChange($"Removing shader keyword {existing}");
                    m.DisableKeyword(existing);
                }
            foreach (var kw in ShaderKeywords)
                if (!m.IsKeywordEnabled(kw))
                {
                    logConfig.LogMaterialChange($"Enabling shader keyword {kw}");
                    m.EnableKeyword(kw);
                }
        }

        /// <summary>
        /// Constructs the prototype from a given material
        /// </summary>
        /// <param name="source">Material to read. Can be null, causing <see cref="IsEmpty"/> to be set true</param>
        public MaterialPrototype(Material source, bool loadTextures = false)
        {
            if (source == null)
            {
                IsEmpty = true;
                return;
            }
            MaterialGlobalIlluminationFlags = source.globalIlluminationFlags;
            ListExtensions.AddRange(ShaderKeywords, source.shaderKeywords);

            var colorVariables = new List<ColorVariable>();
            var floatVariables = new List<FloatVariable>();
            var vectorVariables = new List<VectorVariable>();
            var textureVariables = new List<TextureVariable>();

            for (int v = 0; v < source.shader.GetPropertyCount(); v++)
            {
                var n = source.shader.GetPropertyName(v);
                switch (source.shader.GetPropertyType(v))
                {
                    case ShaderPropertyType.Color:
                        if (!n.StartsWith("_Color")    //don't copy colors (_Color, _Color2, _Color3)
                            &&
                            !n.StartsWith("_SpecColor")    //not sure if these have an impact but can be left out
                            )
                            colorVariables.Add(new ColorVariable(source, n));
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        floatVariables.Add(new FloatVariable(source, n));
                        break;
                    case ShaderPropertyType.Vector:
                        vectorVariables.Add(new VectorVariable(source, n));
                        break;

                    case ShaderPropertyType.Texture:
                        if (loadTextures)
                        {
                            if (n != "_MainTex" && n != "_BumpMap" && n != "_SpecTex" && n != "_Illum")
                                textureVariables.Add(new TextureVariable(source, n));
                        }
                        break;
                }
            }

            ColorVariables = colorVariables.ToArray();
            FloatVariables = floatVariables.ToArray();
            VectorVariables = vectorVariables.ToArray();
            TextureVariables = textureVariables.ToArray();
        }


        public static MaterialPrototype GlassFromExosuit(Logging logConfig = default)
        {

            var exo = PrawnHelper.Prawn;
            if (exo == null)
            {
                //logConfig.LogWarning($"Cyclops not yet available. Keep trying until it is loaded.");
                return null;
            }

            logConfig.LogExtraStep($"Found Exosuit");

            Material glassMaterial = null;
            var renderers = exo.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var material = renderer.materials[i];
                    //logConfig.LogExtraStep($"Found glass candidate material {material.NiceName()} #{i + 1}/{renderer.materials.Length} which uses shader {material.shader}");
                    if (material.shader.name == "MarmosetUBER"
                        && material.name.StartsWith("exosuit_01_glass"))
                    {
                        logConfig.LogExtraStep($"Found material prototype: {material.NiceName()}");
                        glassMaterial = material;
                        break;
                    }
                    else
                    {
                        logConfig.LogExtraStep($"(Expected) shader mismatch on material {material.NiceName()} which uses shader {material.shader}");
                    }
                }
                if (glassMaterial != null)
                    break;
            }
            return new MaterialPrototype(glassMaterial, loadTextures: true);
        }

        /// <summary>
        /// Creates a material prototype for the main material of the Seamoth body.
        /// While the Seamoth is not yet available, the method returns null.
        /// If the Seamoth is loaded but the material could not be found, the return
        /// value is an empty material prototype (IsEmpty=true)
        /// </summary>
        /// <param name="verbose">True to log details</param>
        /// <returns>Null if the seamoth is not (yet) available. Keep trying if null.
        /// Non-null if the seamoth is loaded, but can then be empty (IsEmpty is true)
        /// if the respective material is not found</returns>
        public static MaterialPrototype FromSeamoth(Logging logConfig = default)
        {
            var sm = SeamothHelper.Seamoth;
            if (sm == null)
                return null;

            logConfig.LogExtraStep($"Found Seamoth");

            Material seamothMaterial = null;
            var renderers = sm.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                    if (material.shader.name == "MarmosetUBER"
                        && material.name.StartsWith("Submersible_SeaMoth"))
                    {
                        logConfig.LogExtraStep($"Found material prototype: {material.NiceName()}");
                        seamothMaterial = material;
                        break;
                    }
                    else
                    {
                        logConfig.LogExtraStep($"(Expected) shader mismatch on material {material.NiceName()} which uses shader {material.shader}");
                    }
                if (seamothMaterial != null)
                    break;
            }
            return new MaterialPrototype(seamothMaterial);
        }
    }
}