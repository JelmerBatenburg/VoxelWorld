using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockMaterialList", menuName = "Custom/BlockMaterialList")]
public class MaterialTypeInformation : ScriptableObject
{
    public MaterialInformation[] materials;

    [System.Serializable]
    public class MaterialInformation
    {
        [Header("VisualData")]
        public Material material;
    }
}
