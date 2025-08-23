using Assets.Behavior.Interfaces;
using Assets.Behavior.TransferTypes;
using System;
using Behavior.Util;
using Behavior.Util.Log;
using Behavior.Util.Math;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

public class ArScreenControl : MonoBehaviour, IDockableSelectionListener
{
    public ArchonControl archon;
    public SpriteRenderer subImage;
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
        using (var log = new LogContext(nameof(OnDockableSelectedOrChanged), dockable))
        {

            this.dockable = dockable;

            var mod = dockable?.Modules ?? Array.Empty<Sprite>();
            //mod = mod.Repeat(4).ToArray();
            for (int i = 0; i < mod.Length && i < 8; i++) //only space for 8
            {
                Transform container;
                SpriteRenderer im;
                if (i < moduleContainer.childCount)
                {
                    
                    container = moduleContainer.GetChild(i); 
                    im = container.GetComponentInChildren<SpriteRenderer>();
                    if (im.sprite == mod[i])
                        continue;
                }
                else
                {
                    container = Instantiate(modulePrefab, moduleContainer).transform;
                    im = container.GetComponentInChildren<SpriteRenderer>();
                }
                im.sprite = mod[i];
                container.localPosition = new Vector3(i * container.localScale.x, 0, 0);
                if (mod[i])
                {
                    var spriteBounds = Bounds2.From(mod[i].vertices);
                    var scale = 1f / Mathf.Max(spriteBounds.X.Size,spriteBounds.Y.Size);
                    im.transform.localScale = M.V3(scale);
                }

                
            }

            while (mod.Length < moduleContainer.childCount)
            {
                var c = moduleContainer.GetChild(mod.Length);
                c.parent = null;
                Destroy(c.gameObject);
            }


            nothingDockedText.SetText(dockable == null
                ? TranslationAdapter.GetTranslation(TranslationCode.NothingDocked)
                : "");
            nameText.SetText(dockable?.Name);
            if (dockable == null)
                typeText.SetText("");
            else
                typeText.SetText(
                    $"#{Archon.SelectedDockedIndex + 1}/{Archon.bayControl.NumUndockableVehicles}: {dockable?.ClassName}");
            Apply(healthText, dockable?.HealthText);
            Apply(powerText, dockable?.PowerText);
            Apply(crushText, dockable?.CrushText);
            Apply(storageText, dockable?.StorageText);

            var sprite = dockable?.Image;
            subImage.sprite = sprite;

            if (sprite)
            {
                var spriteBounds = Bounds2.From(sprite.vertices);
                var scale = 1f / Mathf.Max(spriteBounds.X.Size,spriteBounds.Y.Size);
                subImage.transform.localScale = M.V3(scale);
            }

            //subImage.transform.localScale = 
            
            nextUpdateInSeconds = 1;
        }
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
