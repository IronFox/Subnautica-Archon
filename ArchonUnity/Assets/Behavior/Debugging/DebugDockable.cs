using Assets.Behavior.TransferTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugDockable : MonoBehaviour, IDockable
{
    public bool undockUpright = true;

    public GameObject GameObject => base.gameObject;

    public bool ShouldUnfreezeImmediately => false;

    public bool IsDocked { get; private set; } = false;

    public bool UndockUpright => undockUpright;

    public Bounds debugOutBounds;
    public Bounds debugOutBounds2;
    public int health = 100;
    public int maxHealth = 100;
    public string vehicleName = "Unnamed";
    public Bounds LocalBounds { get; private set; }

    public HashSet<string> Tags { get; } = new HashSet<string>();

    public Texture2D texture;
    public Texture2D[] moduleTextures;
    public int crushDepth = 300;

    public AtlasTexture Image => AtlasTexture.FromFullTexture(texture);

    public int storageCapacity = 16;
    public int storageUsage = 3;

    public int power = 64;
    public int powerCapacity = 100;


    public AtlasTexture[] Modules => moduleTextures.Select(AtlasTexture.FromFullTexture).ToArray();

    public string Name => vehicleName;

    public string ClassName => nameof(DebugDockable);

    public Text HealthText
        => Text.Info($"Health: {health.Percentage(maxHealth)}");

    public Text PowerText
        => Text.Warning($"Power: {power.Percentage(powerCapacity)}");

    public Text CrushText
        => Text.Info($"Crush: {M.Round(-transform.position.y, 0).ToStr()}/{crushDepth}m");

    public Text StorageText
        => Text.Info($"Storage: {storageUsage}/{storageCapacity}");


    public void BeginDocking()
    {
        IsDocked = true;
    }

    public void EndDocking()
    { }

    public void BeginUndocking()
    {
        IsDocked = false;
    }

    public void EndUndocking()
    { }

    public IEnumerable<T> GetAllComponents<T>() where T : Component
        => gameObject.GetComponentsInChildren<T>();


    void Awake()
    {
        LocalBounds = debugOutBounds = transform.ComputeScaledLocalBounds(includeRenderers: false, includeColliders: true);
        debugOutBounds2 = transform.ComputeScaledLocalBounds(includeRenderers: true, includeColliders: false);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OpenStorage()
    { }
    public void OpenModules()
    { }

    public void OnDockingDone()
    { }

    public void UpdateWaitingForBayDoorClose()
    { }

    public void PrepareUndocking()
    { }

    public void UpdateWaitingForBayDoorOpen()
    { }

    public void OnUndockingDone()
    { }

    public void RestoreDockedStateFromSaveGame()
    { }

    public void Tag(string tag)
    {
        Tags.Add(tag);
    }

    public void Untag(string tag)
    {
        Tags.Remove(tag);
    }

    public bool IsTagged(string tag)
    {
        return Tags.Contains(tag);
    }

    public void OnUndockedForSaving()
    { }

    public void OnRedockedAfterSaving()
    { }
}
