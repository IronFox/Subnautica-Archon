using UnityEngine;

namespace Subnautica_Archon
{
    public class LoadSaveComponent : MonoBehaviour, IProtoTreeEventListener
    {
        public ArchonControl control;


        void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            control.PrepareForSaving();
        }

        void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            control.SignalLoading();
        }
    }
}