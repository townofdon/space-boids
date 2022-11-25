using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

// USAGE:
// - Add your UI Image && Mask to a UI element per usual
// - Place this script onto a --> CHILD <-- component!
// For more info see: https://www.youtube.com/watch?v=d5nENoQN4Tw

class CutoutMaskUI : Image
{
    int stencilPropertyId = Shader.PropertyToID("_StencilComp");
    int stencilPropertyValue = (int)CompareFunction.NotEqual;

    public override Material materialForRendering
    {
        get
        {
            Material material = new Material(base.materialForRendering);
            material.SetInt(stencilPropertyId, stencilPropertyValue);
            return material;
        }
    }
}
