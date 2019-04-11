using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OderonNodes
{
    [CustomEditor(typeof(NodesGrid))]
    public class NodesGridEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            //EditorGUILayout.HelpBox("This is a help box", MessageType.Info);

            NodesGrid nodesGridComponent = (NodesGrid)target;

            //myScript.health = EditorGUILayout.FloatField("Health", myScript.health);
            //EditorGUILayout.LabelField("Level", myTarget.Level.ToString());

            if (GUILayout.Button("Generate Grid")) { nodesGridComponent.GenerateGrid(); }
            if (GUILayout.Button("Clear Grid")) { nodesGridComponent.ClearGrid(); }
        }
    }
}