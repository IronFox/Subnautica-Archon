using Assets.Behavior.Interfaces;
using Assets.Behavior.TransferTypes;
using System;
using TMPro;
using UnityEngine;

public class ArScreenControl : MonoBehaviour, IDockableSelectionListener
{
    public ArchonControl archon;
    public AtlasImage subImage;
    public GameObject modulePrefab;
    public Transform moduleContainer;
    public TextMeshPro
        nameText,
        typeText,
        healthText,
        powerText,
        crushText,
        storageText,
        nothingDockedText;

    private ArchonControl Archon
    {
        get
        {
            if (archon == null)
            {
                archon = GetComponentInParent<ArchonControl>();
            }
            return archon;
        }
    }

    public void OnDockableSelectedOrChanged(IDockable dockable)
    {
        this.dockable = dockable;

        var mod = dockable?.Modules ?? Array.Empty<AtlasTexture>();
        //mod = mod.Repeat(4).ToArray();
        for (int i = 0; i < mod.Length && i < 8; i++)   //only space for 8
        {
            if (i < moduleContainer.childCount)
            {
                var im = moduleContainer.GetChild(i).GetComponent<AtlasImage>();
                if (im.Texture == mod[i])
                    continue;
                im.Texture = mod[i];
                continue;
            }

            var instance = Instantiate(modulePrefab, moduleContainer);
            var img = instance.GetComponent<AtlasImage>();
            img.Texture = mod[i];
            instance.transform.localPosition = new Vector3(i * instance.transform.localScale.x, 0, 0);
        }
        while (mod.Length < moduleContainer.childCount)
        {
            var c = moduleContainer.GetChild(mod.Length);
            c.parent = null;
            Destroy(c.gameObject);
        }


        nothingDockedText.SetText(dockable == null ? Archon.noDockableTitle : "");
        nameText.SetText(dockable?.Name);
        if (dockable == null)
            typeText.SetText("");
        else
            typeText.SetText($"#{Archon.SelectedDockedIndex + 1}/{Archon.bayControl.NumUndockableVehicles}: {dockable?.ClassName}");
        Apply(healthText, dockable?.HealthText);
        Apply(powerText, dockable?.PowerText);
        Apply(crushText, dockable?.CrushText);
        Apply(storageText, dockable?.StorageText);

        subImage.Texture = dockable?.Image ?? new AtlasTexture();
        nextUpdateInSeconds = 1;
    }

    private void Apply(TextMeshPro field, Text? text)
    {
        if (text == null)
        {
            field.SetText("");
            field.color = Color.white;
            return;
        }
        field.SetText(text.Value.Value);
        field.color = text.Value.Color;
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (archon == null)
        {
            archon = GetComponentInParent<ArchonControl>();
        }
    }
    private float nextUpdateInSeconds = 1;
    private IDockable dockable;

    // Update is called once per frame
    void Update()
    {
        nextUpdateInSeconds -= Time.deltaTime;
        if (nextUpdateInSeconds <= 0 && dockable != null)
        {
            nothingDockedText.SetText("");
            nextUpdateInSeconds = 1;
            nameText.SetText(dockable.Name);
            typeText.SetText($"#{Archon.SelectedDockedIndex + 1}/{Archon.bayControl.NumUndockableVehicles}: {dockable.ClassName}");
            Apply(healthText, dockable?.HealthText);
            Apply(powerText, dockable?.PowerText);
            Apply(crushText, dockable?.CrushText);
            Apply(storageText, dockable?.StorageText);
        }
    }
}
