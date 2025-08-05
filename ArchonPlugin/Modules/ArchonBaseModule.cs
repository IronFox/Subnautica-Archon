using AVS.Crafting;
using AVS.UpgradeModules;
using Subnautica_Archon;
using Subnautica_Archon.Exceptions;
using Subnautica_Archon.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ArchonBaseModule : AvsVehicleModule
{
    public ArchonModule Module { get; }
    private Atlas.Sprite? icon;

    public TechType TechType { get; private set; }



    public override string ClassId => $"Archon{Module}";

    public override string Description => Language.main.Get("desc_" + Module);
    public override string DisplayName => Language.main.Get("display_" + Module);

    //public static CraftingNode RootCraftingNode { get; } = new CraftingNode
    //(
    //    displayName: $"Archon",
    //    icon: Archon.craftingSprite!,
    //    name: $"archonupgradetab"
    //);

    public static string GetMarkFromType(ArchonModule m)
    {
        var s = m.ToString();
        return s.Substring(s.Length - 3);

    }

    public string MarkFromType => GetMarkFromType(Module);

    public ArchonBaseModule(ArchonModule module)
    {
        Module = module;
        var path = $"images/{module}.png";
        icon = MainPatcher.LoadSprite(path);
        if (icon == null)
            throw new InitializationException($"Error while constructing {module} {this}: File {path} not found");
    }



    public virtual TechType Register(Node node)
    {
        var compat = UpgradeCompat.AvsVehiclesOnly;

        var type = node.RegisterUpgrade(this, compat).ForAvsVehicle;
        TechType = type;
        All[type] = this;
        AllReverse[Module] = type;

        Debug.Log($"Registered module {Module} {this} as tech type {type}");

        return type;
    }

    private static Dictionary<TechType, ArchonBaseModule> All { get; } = new Dictionary<TechType, ArchonBaseModule>();
    private static Dictionary<ArchonModule, TechType> AllReverse { get; } = new Dictionary<ArchonModule, TechType>();
    public static IReadOnlyDictionary<TechType, ArchonBaseModule> Registered => All;
    public static IReadOnlyDictionary<ArchonModule, TechType> TechTypeMap => AllReverse;

    public static TechType GetTechTypeOf(ArchonModule module)
    {
        if (TechTypeMap.TryGetValue(module, out var type))
            return type;
        Debug.LogError($"Unable to retrieve tech type of archon module {module}: not registered");
        return TechType.None;
    }

    public override bool IsVehicleSpecific => true;
    public override void OnAdded(AddActionParams param)
    {
        base.OnAdded(param);
        var now = DateTime.Now;

        Log.Write($"ArchonBaseModule[{Module}].OnAdded(vehicle={param.Vehicle.NiceName()},isAdded={param.Added},slot={param.SlotID})");
        var archon = param.Vehicle as Archon;
        if (archon == null)
        {
            Log.Error($"Added to incompatible vehicle {param.Vehicle.NiceName()}");
            ErrorMessage.AddWarning("This is an Archon upgrade and will not work on other subs!");
            return;
        }

        var cnt = GetNumberInstalled(archon);
        archon.SetModuleCount(Module, cnt);
    }
    public override void OnRemoved(AddActionParams param)
    {
        var archon = param.Vehicle as Archon;
        if (archon == null)
        {
            return;
        }
        archon.SetModuleCount(Module, GetNumberInstalled(archon));
    }

    public override Atlas.Sprite Icon => icon ?? throw new InitializationException("Module Icon should have been loaded by now");

}