using System.Collections.Generic;
using UnityEngine;

namespace AvatarPlacement.Editor
{
    /// <summary>One detected blend shape whose name contains breast, ignoring case.</summary>
    /// <remarks>Renderer and Mesh are current Unity references. Recheck the mesh before using its captured index.</remarks>
    public readonly struct BreastBlendShape
    {
        /// <summary>Renderer using the detected shape; may later change or be destroyed.</summary>
        public SkinnedMeshRenderer Renderer { get; }
        /// <summary>Shared mesh at detection time; a Unity reference for inspection.</summary>
        public Mesh Mesh { get; }
        /// <summary>Blend shape index at detection time; mesh changes can invalidate its meaning.</summary>
        public int Index { get; }
        /// <summary>Blend shape name captured at detection time.</summary>
        public string Name { get; }

        internal BreastBlendShape(SkinnedMeshRenderer renderer, Mesh mesh, int index, string name)
        { Renderer = renderer; Mesh = mesh; Index = index; Name = name; }
    }

    /// <summary>A direct child was created or reparented under an avatar and contained named breast blend shapes.</summary>
    /// <remarks>Name matching does not establish clothing identity or avatar compatibility. Edit Unity references through HandlerContext.</remarks>
    public sealed class AvatarChildBreastBlendShapesPlaced
    {
        /// <summary>Immediate parent avatar selected as the handler's editable root.</summary>
        public GameObject Avatar { get; }
        /// <summary>Created or reparented direct child; need not be a Prefab instance.</summary>
        public GameObject PlacedObject { get; }
        /// <summary>Read-only detection results; Unity references may change before a later handler uses them.</summary>
        public IReadOnlyList<BreastBlendShape> BreastBlendShapes { get; }

        internal AvatarChildBreastBlendShapesPlaced(GameObject avatar, GameObject placedObject, BreastBlendShape[] shapes)
        { Avatar = avatar; PlacedObject = placedObject; BreastBlendShapes = System.Array.AsReadOnly(shapes); }
    }
}
