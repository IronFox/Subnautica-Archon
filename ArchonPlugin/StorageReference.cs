using Subnautica_Archon.Util;
using System;
using UnityEngine;
using VehicleFramework;
using VehicleFramework.VehicleParts;
using static VehicleUpgradeConsoleInput;

namespace Subnautica_Archon
{
    public class StorageReference
    {
        public StorageReference(Transform biofuel, int width = 8, int height = 10)
        {
            Reference = biofuel.gameObject;
            Width = width;
            Height = height;

            var innate = Reference.GetComponent<InnateStorageContainer>();
            if (!innate)
                innate = Reference.AddComponent<InnateStorageContainer>();
            innate.storageLabel = $"Biofuel";

        }

        public GameObject Reference { get; }
        public int Width { get; }
        public int Height { get; }

        public VehicleStorage ToVehicleStorage()
        {
            //InnateStorageContainer component2 = InnateStorages[slotID].Container.GetComponent<InnateStorageContainer>();
            return new VehicleStorage(Reference, Height, Width);
        }

        public ItemsContainer GetContainer(string reLabelTo)
        {
            var rs = Reference.GetComponent<InnateStorageContainer>();

            if (rs)
            {
                if (!string.IsNullOrEmpty(reLabelTo))
                {
                    rs.storageLabel = reLabelTo;
                    rs.name = reLabelTo;
                    FieldAdapter.OfNonPublic<string>(rs.container, "_label").Set(reLabelTo);
                }
                return rs.container;
            }
            Log.Error($"Unable to resovle container for storage reference {Reference.NiceName()}");
            return null;
        }
    }
}