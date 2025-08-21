using AVS.Log;
using AVS.MaterialAdapt;
using UnityEngine;

namespace Subnautica_Archon
{
    public class MaterialAdaptConfig : DefaultMaterialAdaptConfig
    {
        public MaterialAdaptConfig()
            : base(MaterialLog.Silent)
        { }

        public override UnityMaterialData ConvertUnityMaterial(UnityMaterialData materialData)
        {
            if (materialData.MaterialName.ToLower().Contains("[gold]"))
            {
                return new UnityMaterialData(
                    MaterialType.Opaque,
                    materialData.MaterialName,
                    color: new Color(255, 167, 0) / 255 / 3,
                    specularColor: new Color(191, 80, 13) / 255 * 4f,
                    emissionColor: Color.black,
                    mainTex: materialData.MainTex,
                    smoothness: materialData.Smoothness,
                    smoothnessTextureChannel: materialData.SmoothnessTextureChannel,
                    metallicTexture: materialData.MetallicTexture,
                    bumpMap: materialData.BumpMap,
                    emissionTexture: materialData.EmissionTexture,
                    source: materialData.Source
                    );

            }
            else
                return base.ConvertUnityMaterial(materialData);
        }
    }
}
