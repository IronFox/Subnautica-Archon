using Subnautica_Archon;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VehicleFramework.UpgradeTypes;

public abstract class ArchonBaseModule : ModVehicleUpgrade
{
    public CraftingNode? GroupNode { get; }
    public ArchonModule Module { get; }
    private Atlas.Sprite icon;

    public TechType TechType { get; private set; }

    public List<CraftingNode> craftingPath;

    public virtual IReadOnlyCollection<TechType> AutoDisplace { get; }
    public override string ClassId => $"Archon{Module}";

    public override string Description => Language.main.Get("desc_" + Module);
    public override string DisplayName => Language.main.Get("display_" + Module);

    public static CraftingNode RootCraftingNode { get; } = new CraftingNode
    {
        displayName = $"Archon",
        icon = Archon.craftingSprite,
        name = $"archonupgradetab"
    };

    public static string GetMarkFromType(ArchonModule m)
    {
        var s = m.ToString();
        return s.Substring(s.Length - 3);

    }

    public string MarkFromType => GetMarkFromType(Module);

    public ArchonBaseModule(ArchonModule module, CraftingNode groupNode)
    {
        GroupNode = groupNode;
        Module = module;
        var path = $"images/{module}.png";
        icon = Subnautica_Archon.MainPatcher.LoadSprite(path);
        if (icon == null)
            Debug.LogError($"Error while constructing {module} {this}: File {path} not found");

        craftingPath = new List<CraftingNode>()
        {
            RootCraftingNode,
            groupNode
        };
    }

    public ArchonBaseModule(ArchonModule module)
    {
        Module = module;
        var path = $"images/{module}.png";
        icon = Subnautica_Archon.MainPatcher.LoadSprite(path);
        if (icon == null)
            Debug.LogError($"Error while constructing {module} {this}: File {path} not found");

        craftingPath = new List<CraftingNode>()
        {
            RootCraftingNode
        };
    }



    public virtual TechType Register()
    {
        VehicleFramework.Admin.UpgradeCompat compat = new VehicleFramework.Admin.UpgradeCompat
        {
            skipCyclops = true,
            skipModVehicle = false,
            skipSeamoth = true,
            skipExosuit = true
        };

        var type = VehicleFramework.Admin.UpgradeRegistrar.RegisterUpgrade(this, compat).forModVehicle;
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

    public override List<CraftingNode> CraftingPath
    {
        get => craftingPath;
        set => craftingPath = value;
    }

    public override bool IsVehicleSpecific => true;
    public override void OnAdded(AddActionParams param)
    {
        var now = DateTime.Now;
        Debug.Log($"[{now:HH:mm:ss.fff}] ArchonBaseModule[{Module}].OnAdded(vehicle={param.vehicle},isAdded={param.isAdded},slot={param.slotID})");
        var archon = param.vehicle as Archon;
        if (archon == null)
        {
            Debug.LogError($"Added to incompatible vehicle {param.vehicle}");
            ErrorMessage.AddWarning("This is an Archon upgrade and will not work on other subs!");
            return;
        }

        var cnt = GetNumberInstalled(archon);
        try
        {
            foreach (var slot in archon.slotIDs)
            {
                if (slot == archon.slotIDs[param.slotID])
                    continue;
                var p = archon.modules.GetItemInSlot(slot);
                if (p != null)
                {
                    var t = p.item.GetComponent<TechTag>();
                    if (t != null && AutoDisplace.Contains(t.type))
                    {
                        Debug.Log($"Evacuating extra {t.type} type from slot {slot}");
                        if (!archon.modules.RemoveItem(p.item))
                        {
                            Debug.Log($"Failed remove");
                            continue;
                        }
                        Inventory.main.AddPending(p.item);
                        Debug.Log($"Inventory moved");
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        archon.SetModuleCount(Module, cnt);
    }
    public override void OnRemoved(AddActionParams param)
    {
        var archon = param.vehicle as Archon;
        if (archon == null)
        {
            return;
        }
        archon.SetModuleCount(Module, GetNumberInstalled(archon));
    }

    public override Atlas.Sprite Icon => icon ?? base.Icon;

}