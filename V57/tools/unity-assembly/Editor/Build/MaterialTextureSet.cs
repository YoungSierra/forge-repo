using UnityEngine;

namespace V57.Assembly.Build
{
    /// <summary>Textures of one asset (by asset folder + asset name) that build <c>MAT_&lt;Asset&gt;</c>.</summary>
    public sealed class MaterialTextureSet
    {
        public MaterialTextureSet(string assetName, string assetFolder)
        {
            AssetName = assetName;
            AssetFolder = assetFolder;
        }

        public string AssetName { get; }

        public string AssetFolder { get; }

        public string MaterialPath => $"{AssetFolder}/Materials/MAT_{AssetName}.mat";

        public Texture2D BaseColor { get; set; }

        public Texture2D Normal { get; set; }

        /// <summary>Contract ORM (R occlusion, G roughness, B metallic); repacked for URP.</summary>
        public Texture2D Orm { get; set; }

        /// <summary>Unity-ready metallic (R) + smoothness (A) mask, used as delivered.</summary>
        public Texture2D MetallicSmoothness { get; set; }

        /// <summary>Separate metallic map (DCC export); packed with <see cref="Roughness"/> when no MS mask exists.</summary>
        public Texture2D Metallic { get; set; }

        /// <summary>Separate roughness map (DCC export); smoothness = 1 − roughness.</summary>
        public Texture2D Roughness { get; set; }

        public Texture2D Occlusion { get; set; }

        public Texture2D Emission { get; set; }

        public Texture2D Mask { get; set; }
    }
}
