using System;
using V57.Assembly.Data;

namespace V57.Assembly.Build
{
    /// <summary>Physics mode from <c>package.json → physics</c> ("2d" | "3d"; default 3d). V57 never mixes 2D and 3D colliders.</summary>
    public static class AssemblyPhysics
    {
        public static bool Is2D => string.Equals(GeneratedData.Package?.physics, "2d", StringComparison.OrdinalIgnoreCase);
    }
}
