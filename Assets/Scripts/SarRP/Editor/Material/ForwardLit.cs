using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace SarRP.Editor.Material
{
    class ForwardLit : ShaderGUI
    {
        const string MotionVectorPassName = "MotionVectors";
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            materialEditor.PropertiesDefaultGUI(properties);

            foreach(var obj in materialEditor.targets)
            {
                var material = obj as UnityEngine.Material;
                material.SetShaderPassEnabled(MotionVectorPassName, false);
            }
        }
    }
}
