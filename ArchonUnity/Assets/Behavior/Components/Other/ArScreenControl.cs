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
        storageText;

    public void OnDockableSelectedOrChanged(IDockable dockable)
    {
        this.dockable = dockable;

        var mod = dockable?.Modules ?? Array.Empty<AtlasTexture>();
        for (int i = 0; i < mod.Length; i++)
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
            instance.transform.localPosition += new Vector3(i * instance.transform.localScale.x, 0, 0);
        }
        while (mod.Length < moduleContainer.childCount)
            Destroy(moduleContainer.GetChild(mod.Length));


        nameText.SetText(dockable?.Name ?? archon.noDockableTitle);
        typeText.SetText(dockable?.ClassName ?? "");
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
    void Start()
    {

    }
    private float nextUpdateInSeconds = 1;
    private IDockable dockable;

    // Update is called once per frame
    void Update()
    {
        nextUpdateInSeconds -= Time.deltaTime;
        if (nextUpdateInSeconds <= 0 && dockable != null)
        {
            nextUpdateInSeconds = 1;
            nameText.SetText(dockable.Name);
            typeText.SetText(dockable.ClassName);
            Apply(healthText, dockable?.HealthText);
            Apply(powerText, dockable?.PowerText);
            Apply(crushText, dockable?.CrushText);
            Apply(storageText, dockable?.StorageText);
        }
    }
}
