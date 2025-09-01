using Assets.Behavior.Adapters;
using Assets.Behavior.Interfaces;
using Assets.Behavior.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Behavior.Components.AR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ArButton))]
    [AddComponentMenu("Behavior/Components/AR/ArAccessRow")]
    /// <summary>
    /// Manages a row of AR buttons for accessing module and storage functions of a dockable object.
    /// Dynamically creates and positions buttons based on the selected dockable's storage count.
    /// </summary>
    public class ArAccessRow : MonoBehaviour, IDockableSelectionListener
    {
        public ArButton modulesButton;
        public GameObject storageButtonPrefab;
        public Orientation orientation = Orientation.TopDown;
        public enum Orientation
        {
            CenterHorizontal,
            TopDown
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private float SizeOf(Transform t)
        {
            var bc = t.GetComponent<BoxCollider>();
            if (bc != null)
            {
                switch (orientation)
                {
                    case Orientation.CenterHorizontal:
                        return bc.size.x * t.localScale.x;
                    case Orientation.TopDown:
                        return bc.size.y * t.localScale.y;
                    default:
                        break;
                }
            }
            return 1;
        }

        public void OnDockableSelectedOrChanged(IDockable dockable)
        {
            using (var log = Log.NewLazy())
            {
                var cnt = Mathf.Min(4, dockable?.StorageCount ?? 0);
                foreach (var c in transform.GetChildren().ToList())
                {
                    if (c != modulesButton.transform)
                    {
                        var btn = c.GetComponent<ArButton>();
                        if (btn == null || btn.function != ArButton.Function.OpenStorage || btn.parameter >= cnt)
                        {
                            log.Debug($"Destroying button {c.NiceName()} since it is no longer needed");
                            c.parent = null;
                            Destroy(c.gameObject);
                        }
                    }
                }

                float s = SizeOf(modulesButton.transform);
                var buttons = new List<(Transform T, float At)>()
            {
                (modulesButton.transform, s/2)
            };
                float spacing = 0.11f;
                for (int i = 0; i < cnt; i++)
                {
                    var mname = $"storage{i}";
                    var btnTransform = transform.Find(mname);
                    if (btnTransform == null)
                    {
                        log.Debug($"Creating button for storage {i}");
                        var btnObj = Instantiate(storageButtonPrefab, transform);
                        btnObj.name = mname;
                        btnTransform = btnObj.transform;
                        var btn = btnTransform.GetComponent<ArButton>();
                        btn.function = ArButton.Function.OpenStorage;
                        btn.archon = modulesButton.archon;
                        btn.parameter = i;
                    }
                    s += spacing;
                    var mw = SizeOf(btnTransform);
                    buttons.Add((btnTransform, s + mw / 2));
                    s += mw;
                }

                for (int i = 0; i < buttons.Count; i++)
                {
                    var b = buttons[i];
                    float p;
                    switch (orientation)
                    {
                        case Orientation.CenterHorizontal:
                            p = b.At - s / 2;
                            break;
                        case Orientation.TopDown:
                            p = -b.At;
                            break;
                        default:
                            continue;
                    }
                    log.Debug($"Positioning button {i} ({b.T.name}) at {p}");
                    switch (orientation)
                    {
                        case Orientation.CenterHorizontal:
                            b.T.localPosition = new Vector3(p, b.T.localRotation.y, b.T.localRotation.z);
                            break;
                        case Orientation.TopDown:
                            b.T.localPosition = new Vector3(0, p, 0);
                            break;
                    }
                }
            }
        }


    }

}