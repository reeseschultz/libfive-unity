using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace libfivesharp
{
    [CustomEditor(typeof(LFShape))]
    public class LFShapeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!GUILayout.Button("Save STL")) return;

            var shape = (LFShape)target;
            var pathString = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), shape.gameObject.name + " - " + System.DateTime.Now.ToShortTimeString().Replace(":", ".") + ".stl");
            var toWrite = shape.tree.RenderMesh(new Bounds(shape.transform.position, Vector3.one * shape.boundsSize), shape.resolution + 0.001f);
            var stlString = LFMeshRendering.createSTL(toWrite);

            try
            {
                File.WriteAllText(pathString, stlString);
                Debug.Log("Saved STL at: " + pathString);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save STL! - " + e);
            }
        }
    }
}
