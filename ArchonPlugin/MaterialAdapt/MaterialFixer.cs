using Subnautica_Archon.Util;
using System;
using System.Collections.Generic;
using UnityEngine;
using VehicleFramework;

namespace Subnautica_Archon.MaterialAdapt
{


    /// <summary>
    /// Helper class to fix materials automatically. Should be instantiated on the vehicle
    /// you wish to fix materials of
    /// </summary>
    /// <author>https://github.com/IronFox</author>
    public class MaterialFixer
    {

        private DateTime repairMaterialsIn = DateTime.MaxValue;
        private int repairMaterialsInFrames = 3;
        private bool materialsFixed;
        private readonly List<MaterialAdaptation> adaptations = new List<MaterialAdaptation>();

        public bool MaterialsAreFixed => materialsFixed;

        public ModVehicle Vehicle { get; }

        /// <summary>
        /// Controls how debug logging should be performed
        /// </summary>
        public Logging Logging { get; set; }

        public Func<IEnumerable<SurfaceShaderData>> MaterialResolver { get; }
        public Func<IEnumerable<SurfaceShaderData>> GlassMaterialResolver { get; }

        /// <summary>
        /// Constructs the instance
        /// </summary>
        /// <param name="owner">Owning vehicle</param>
        /// <param name="materialResolver">The solver function to fetch all materials to translate.
        /// If null, a default implementation is used which 
        /// mimics VF's default material selection in addition to filtering out non-standard materials</param>
        /// <param name="logConfig">Log Configuration. If null, defaults to <see cref="Logging.Default" /></param>
        public MaterialFixer(
            ModVehicle owner,
            Logging? logConfig = null,
            Func<IEnumerable<SurfaceShaderData>> materialResolver = null,
            Func<IEnumerable<SurfaceShaderData>> glassMaterialResolver = null
            )
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            Vehicle = owner;
            Logging = logConfig ?? Logging.Default;
            MaterialResolver = materialResolver ?? (() => DefaultMaterialResolver(owner, Logging));
            GlassMaterialResolver = glassMaterialResolver ?? (() => DefaultGlassMaterialResolver(owner, Logging, true));
        }

        /// <summary>
        /// Default material address resolver function. Can be modified to also return materials with divergent shader names
        /// </summary>
        /// <param name="vehicle">Owning vehicle</param>
        /// <param name="ignoreShaderNames">True to return all materials, false to only return Standard materials</param>
        /// <param name="logConfig">Log Configuration</param>
        /// <returns>Enumerable of all suitable material addresses</returns>
        public static IEnumerable<SurfaceShaderData> DefaultMaterialResolver(ModVehicle vehicle, Logging logConfig, bool ignoreShaderNames = false)
        {
            var renderers = vehicle.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // copied from VF default behavior:

                // skip some materials
                if (renderer.gameObject.GetComponent<Skybox>())
                {
                    // I feel okay using Skybox as the designated "don't apply marmoset to me" component.
                    // I think there's no reason a vehicle should have a skybox anywhere.
                    // And if there is, I'm sure that developer can work around this.
                    UnityEngine.Object.DestroyImmediate(renderer.gameObject.GetComponent<Skybox>());
                    continue;
                }
                if (renderer.gameObject.name.ToLower().Contains("light"))
                {
                    continue;
                }
                if (vehicle.CanopyWindows != null && vehicle.CanopyWindows.Contains(renderer.gameObject))
                {
                    continue;
                }

                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (DefaultIsGlass(renderer.materials[i]))
                    {
                        logConfig.LogExtraStep($"Skipping glass material {renderer.materials[i].name} on {renderer.gameObject.name}");
                        continue;
                    }

                    var material = SurfaceShaderData.From(renderer, i, logConfig, ignoreShaderNames);
                    if (material != null)
                        yield return material;
                }
            }
        }

        public static bool DefaultIsGlass(Material material)
        {
            return material.name.Contains("[Glass]");
        }

        /// <summary>
        /// Default material address resolver function. Can be modified to also return materials with divergent shader names
        /// </summary>
        /// <param name="vehicle">Owning vehicle</param>
        /// <param name="ignoreShaderNames">True to return all materials, false to only return Standard materials</param>
        /// <param name="logConfig">Log Configuration</param>
        /// <returns>Enumerable of all suitable material addresses</returns>
        public static IEnumerable<SurfaceShaderData> DefaultGlassMaterialResolver(ModVehicle vehicle, Logging logConfig, bool ignoreShaderNames = false)
        {
            var renderers = vehicle.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // copied from VF default behavior:

                // skip some materials
                if (renderer.gameObject.GetComponent<Skybox>())
                {
                    // I feel okay using Skybox as the designated "don't apply marmoset to me" component.
                    // I think there's no reason a vehicle should have a skybox anywhere.
                    // And if there is, I'm sure that developer can work around this.
                    UnityEngine.Object.DestroyImmediate(renderer.gameObject.GetComponent<Skybox>());
                    continue;
                }
                if (renderer.gameObject.name.ToLower().Contains("light"))
                {
                    continue;
                }
                if (vehicle.CanopyWindows != null && vehicle.CanopyWindows.Contains(renderer.gameObject))
                {
                    continue;
                }

                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (!DefaultIsGlass(renderer.materials[i]))
                    {
                        logConfig.LogExtraStep($"Skipping non-glass material {renderer.materials[i].name} on {renderer.gameObject.name}");
                        continue;
                    }
                    var material = SurfaceShaderData.From(renderer, i, logConfig, ignoreShaderNames);
                    if (material != null)
                        yield return material;
                }
            }
        }

        /// <summary>
        /// Notifies that the vehicle has just undocked from a docking bay (moonpool, etc).
        /// </summary>
        /// <remarks>Should be called from your vehicle OnVehicleUndocked() method</remarks>
        public void OnVehicleUndocked()
        {
            repairMaterialsIn = DateTime.Now + TimeSpan.FromSeconds(0.5f);
            repairMaterialsInFrames = 3;
        }

        /// <summary>
        /// Forcefully reapplies all material adaptations.
        /// Normally not necessary
        /// </summary>
        public void ReApply()
        {
            foreach (MaterialAdaptation adaptation in adaptations)
                adaptation.ApplyToTarget(Logging);
        }

        private MaterialPrototype HullPrototype { get; set; }
        private MaterialPrototype GlassPrototype { get; set; }

        /// <summary>
        /// Fixes materials if necessary/possible.
        /// Also fixes undock material changes if <see cref="OnVehicleUndocked"/> was called before
        /// </summary>
        /// <remarks>Should be called from your vehicle Update() method</remarks>
        /// <param name="subTransform">Root transform of your sub</param>
        public void OnUpdate()
        {

            if (!materialsFixed)
            {
                HullPrototype = HullPrototype ?? MaterialPrototype.FromSeamoth(Logging);
                GlassPrototype = GlassPrototype ?? MaterialPrototype.GlassFromExosuit(Logging.Verbose);

                if (HullPrototype != null && GlassPrototype != null)
                {
                    materialsFixed = true;



                    if (HullPrototype.IsEmpty)
                    {
                        Logging.LogError($"No material prototype found on Seamoth");
                    }
                    else
                    {
                        Shader shader = Shader.Find("MarmosetUBER");

                        foreach (var data in MaterialResolver())
                        {
                            try
                            {
                                var materialAdaptation = new MaterialAdaptation(HullPrototype, data, shader);
                                materialAdaptation.ApplyToTarget(Logging);

                                adaptations.Add(materialAdaptation);
                            }
                            catch (Exception ex)
                            {
                                Logging.LogError($"Adaptation failed for material {data}: {ex}");
                                Debug.LogException(ex);
                            }
                        }
                        //foreach (var data in GlassMaterialResolver())
                        //{
                        //    try
                        //    {
                        //        var materialAdaptation = new MaterialAdaptation(GlassPrototype, data, shader);
                        //        materialAdaptation.ApplyToTarget(Logging);

                        //        adaptations.Add(materialAdaptation);
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        Logging.LogError($"Adaptation failed for glass material {data}: {ex}");
                        //        Debug.LogException(ex);
                        //    }
                        //}
                        Logging.LogExtraStep($"All done. Applied {adaptations.Count} adaptations");
                    }
                }
            }

            if (DateTime.Now > repairMaterialsIn && --repairMaterialsInFrames == 0)
            {
                repairMaterialsIn = DateTime.MaxValue;
                Logging.LogExtraStep($"Undocked. Resetting materials");
                foreach (MaterialAdaptation adaptation in adaptations)
                    adaptation.PostDockFixOnTarget(Logging);
            }
        }
    }
}