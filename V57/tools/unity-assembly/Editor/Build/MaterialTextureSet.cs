using UnityEngine;

namespace V57.Assembly.Build
{
    /// <summary>The <c>T_&lt;Asset&gt;_*</c> textures found for one asset folder.</summary>
    public sealed class MaterialTextureSet
    {
        public MaterialTextureSet(string assetName, string assetFolder)
        {
            AssetName = assetName;
            AssetFolder = assetFolder;
        }

        public string AssetName { get; }

        /// <summary>Folder that holds Meshes/ and Textures/ (parent of the Textures folder).</summary>
        public string AssetFolder { get; }

        public string MaterialPath => $"{AssetFolder}/Materials/MAT_{AssetName}.mat";

        public Texture2D BaseColor { get; set; }

        public Texture2D Normal { get; set; }

        public Texture2D Orm { get; set; }

        public Texture2D Emission { get; set; }

        public Texture2D Mask { get; set; }
    }
}
