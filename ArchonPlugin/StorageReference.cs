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
        public StorageReference(
            Transform biofuel,
            string storageLabel,
            IsAllowedToAdd isAllowedToAdd,
            IsAllowedToRemove isAllowedToRemove,
            int width = 8, int height = 10)
        {
            Reference = biofuel.gameObject;
            StorageLabel = storageLabel;
            IsAllowedToAdd = isAllowedToAdd;
            IsAllowedToRemove = isAllowedToRemove;
            Width = width;
            Height = height;

        }

        public GameObject Reference { get; }
        public string StorageLabel { get; }
        public IsAllowedToAdd IsAllowedToAdd { get; }
        public IsAllowedToRemove IsAllowedToRemove { get; }
        public int Width { get; }
        public int Height { get; }

        public VehicleStorage ToVehicleStorage()
        {
            //InnateStorageContainer component2 = InnateStorages[slotID].Container.GetComponent<InnateStorageContainer>();
            return new VehicleStorage(Reference, Height, Width);
        }

        private bool ContainerIsInitialized { get; set; }
        public ItemsContainer LazyInitGetContainer()
        {
            var rs = Reference.GetComponent<InnateStorageContainer>();

            if (rs)
            {
                if (!ContainerIsInitialized)
                {
                    ContainerIsInitialized = true;
                    if (!string.IsNullOrEmpty(StorageLabel))
                    {
                        rs.storageLabel = StorageLabel;
                        rs.name = StorageLabel;
                        FieldAdapter.OfNonPublic<string>(rs.container, "_label").Set(StorageLabel);
                    }

                    rs.container.isAllowedToAdd = (IsAllowedToAdd)Delegate.Combine(rs.container.isAllowedToAdd, IsAllowedToAdd);
                    rs.container.isAllowedToRemove = (IsAllowedToRemove)Delegate.Combine(rs.container.isAllowedToRemove, IsAllowedToRemove);
                }
                return rs.container;
            }
            Log.Error($"Unable to resovle container for storage reference {Reference.NiceName()}");
            return null;
        }
    }
}