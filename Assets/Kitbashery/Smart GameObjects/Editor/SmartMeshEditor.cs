using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/* 

Smart GameObjects Copyright(c) 2023 Kitbashery. All rights reserved.
Development lead @ Kitbashery: Tolin Simpson

Permission is hereby granted to use & modify the software, to any person obtaining a copy
of this software and associated documentation files (the "Software") from the official 
Unity Asset Store & maintaining a valid license. Support for the software may be reserved for but not limited to
customers who provide a valid invoice number as confirmation of license ownership. Usage restrictions & other legal terms are defined by the
Unity Asset Store End-User License Agreement (EULA) For more information see: https://unity.com/legal/as-terms

Piracy is not a victimless crime!

The unathorized reproduction or distribution of this copyrighted work is illegal. Criminal copyright infringment is investigated by federal law enforcement agencies
and is punishable by up to 5 years of prison time and a fine of $250,000, for more information on how digital theft harms the economy see: https://www.iprcenter.gov/
if you suspect this software to be pirated please report the source to the copyright holder.

Need support or additional features? Please visit https://kitbashery.com/

*/

namespace Kitbashery.SmartGO
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SmartMesh))]
    public class SmartMeshEditor : Editor
    {
        SmartMesh self;

        SerializedProperty meshType, assetMesh, height, radius, width, depth, subdivisions, heightSegments, widthSegments, depthSegments, radialSegments, customVertices, customTriangles, start;

        SerializedProperty unwrapMode, vertexScale, vertexRotation, vertexOffset, noiseStrength, uvRotation, tiling, uvPreview, wireframe, vertexColor, sphereify, sphereifyRadius, weldVertices;

        SerializedProperty top, bottom, leftSide, rightSide, frontSide, backSide;

        private bool showCopyright, showSmartMeshHelp;

        SerializedProperty xBounds, yBounds, zBounds;

        public GUIStyle wrappedMiniLabel, wrappedCenteredGreyMiniLabel, richBoldLabel;
        public GUILayoutOption[] thinLine, thickLine;
        SerializedProperty topUVOffset, bottomUVOffset, leftUVOffset, rightUVOffset, frontUVOffset, backUVOffset;

        private bool showCustomTriangles;

        private void OnEnable()
        {
            meshType = serializedObject.FindProperty("meshType");
            assetMesh = serializedObject.FindProperty("assetMesh");
            height = serializedObject.FindProperty("height");
            width = serializedObject.FindProperty("width");
            depth = serializedObject.FindProperty("depth");
            subdivisions = serializedObject.FindProperty("subdivisions");
            radius = serializedObject.FindProperty("radius");
            heightSegments = serializedObject.FindProperty("heightSegments");
            widthSegments = serializedObject.FindProperty("widthSegments");
            depthSegments = serializedObject.FindProperty("depthSegments");
            radialSegments = serializedObject.FindProperty("radialSegments");
            customVertices = serializedObject.FindProperty("customVertices");
            customTriangles = serializedObject.FindProperty("customTriangles");
            start = serializedObject.FindProperty("start");
            unwrapMode = serializedObject.FindProperty("unwrapMode");
            vertexScale = serializedObject.FindProperty("vertexScale");
            vertexRotation = serializedObject.FindProperty("vertexRotation");
            vertexOffset = serializedObject.FindProperty("vertexOffset");
            noiseStrength = serializedObject.FindProperty("noiseStrength");
            xBounds = serializedObject.FindProperty("xBounds");
            yBounds = serializedObject.FindProperty("yBounds");
            zBounds = serializedObject.FindProperty("zBounds");
            top = serializedObject.FindProperty("top");
            bottom = serializedObject.FindProperty("bottom");
            leftSide = serializedObject.FindProperty("leftSide");
            rightSide = serializedObject.FindProperty("rightSide");
            frontSide = serializedObject.FindProperty("frontSide");
            backSide = serializedObject.FindProperty("backSide");
            uvRotation = serializedObject.FindProperty("uvRotation");
            tiling = serializedObject.FindProperty("tiling");
            uvPreview = serializedObject.FindProperty("uvPreview");
            wireframe = serializedObject.FindProperty("wireframe");
            topUVOffset = serializedObject.FindProperty("topUVOffset");
            bottomUVOffset = serializedObject.FindProperty("bottomUVOffset");
            leftUVOffset = serializedObject.FindProperty("leftUVOffset");
            rightUVOffset = serializedObject.FindProperty("rightUVOffset");
            frontUVOffset = serializedObject.FindProperty("frontUVOffset");
            backUVOffset = serializedObject.FindProperty("backUVOffset");
            vertexColor = serializedObject.FindProperty("vertexColor");
            sphereify = serializedObject.FindProperty("sphereify");
            sphereifyRadius = serializedObject.FindProperty("sphereifyRadius");
            weldVertices = serializedObject.FindProperty("weldVertices");
        }

        public override void OnInspectorGUI()
        {
            // Initialize Styles:
            wrappedMiniLabel = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 10 };
            wrappedCenteredGreyMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true, richText = true };
            richBoldLabel = new GUIStyle(EditorStyles.boldLabel) { richText = true };
            thinLine = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) };
            thickLine = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(3) };

            self = (SmartMesh)target;
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawSmartMeshHelp();

            DrawInitialMeshProperties();

            DrawVertexProperties();

            DrawUVUnwrappProperties();

            DrawMeshDebug();

            EditorGUILayout.EndVertical();
            SGO_EditorUtility.DrawCopyrightNotice("https://kitbashery.com/docs/smart-gameobjects", "https://assetstore.unity.com/packages/slug/248930", "https://unity.com/legal/as-terms", "2023", "Kitbashery", ref showCopyright);

            if (serializedObject.hasModifiedProperties == true)
            {
                serializedObject.ApplyModifiedProperties();
                self.BuildMesh();
            }
        }

        private void DrawVertexProperties()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#77C3E5>Vertex Modifiers:</color>", richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);

            EditorGUILayout.PropertyField(weldVertices);

            EditorGUILayout.PropertyField(vertexOffset);
            EditorGUILayout.PropertyField(vertexRotation);
            EditorGUILayout.PropertyField(vertexScale);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bounding Area:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(xBounds);
            EditorGUILayout.PropertyField(yBounds);
            EditorGUILayout.PropertyField(zBounds);
            EditorGUILayout.PropertyField(sphereify);
            if (sphereify.boolValue == true)
            {
                EditorGUILayout.PropertyField(sphereifyRadius);
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(noiseStrength);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(vertexColor);
            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }

        private void DrawInitialMeshProperties()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#77C3E5>Initial Mesh:</color>", richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(meshType);
            if (EditorGUI.EndChangeCheck())
            {
                if (meshType.enumValueIndex == (int)SmartMesh.MeshTypes.Cone)
                {
                    if (subdivisions.intValue < 10)
                    {
                        subdivisions.intValue = 10;
                    }
                }
            }

            switch ((SmartMesh.MeshTypes)meshType.enumValueIndex)
            {
                case SmartMesh.MeshTypes.Asset:

                    EditorGUILayout.PropertyField(assetMesh);

                    break;

                case SmartMesh.MeshTypes.Cube:

                    EditorGUILayout.PropertyField(width);
                    EditorGUILayout.PropertyField(height);
                    EditorGUILayout.PropertyField(depth);
                    EditorGUILayout.PropertyField(heightSegments);
                    EditorGUILayout.PropertyField(widthSegments);
                    EditorGUILayout.PropertyField(depthSegments);
                    EditorGUILayout.PropertyField(top);
                    if (top.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(topUVOffset);
                    }
                    EditorGUILayout.PropertyField(bottom);
                    if (bottom.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(bottomUVOffset);
                    }
                    EditorGUILayout.PropertyField(leftSide);
                    if (leftSide.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(leftUVOffset);
                    }
                    EditorGUILayout.PropertyField(rightSide);
                    if (rightSide.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(rightUVOffset);
                    }
                    EditorGUILayout.PropertyField(frontSide);
                    if (frontSide.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(frontUVOffset);
                    }
                    EditorGUILayout.PropertyField(backSide);
                    if (backSide.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(backUVOffset);
                    }

                    break;

                case SmartMesh.MeshTypes.Cylinder:

                    EditorGUILayout.PropertyField(radius);
                    EditorGUILayout.PropertyField(height);
                    EditorGUILayout.PropertyField(radialSegments);
                    EditorGUILayout.PropertyField(heightSegments);
                    EditorGUILayout.PropertyField(top);
                    if (top.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(topUVOffset);
                    }
                    EditorGUILayout.PropertyField(bottom);
                    if (bottom.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(bottomUVOffset);
                    }

                    break;

                case SmartMesh.MeshTypes.Cone:

                    EditorGUILayout.PropertyField(subdivisions);
                    EditorGUILayout.PropertyField(radius);
                    EditorGUILayout.PropertyField(height);
                    EditorGUILayout.PropertyField(bottom);

                    break;

                case SmartMesh.MeshTypes.Icosahedron:

                    EditorGUILayout.PropertyField(radius);
                    EditorGUILayout.PropertyField(subdivisions);

                    break;

                case SmartMesh.MeshTypes.Plane:

                    EditorGUILayout.PropertyField(width);
                    EditorGUILayout.PropertyField(height);
                    EditorGUILayout.PropertyField(widthSegments);
                    EditorGUILayout.PropertyField(heightSegments);

                    break;

                case SmartMesh.MeshTypes.Sphere:

                    EditorGUILayout.PropertyField(radius);
                    EditorGUILayout.PropertyField(widthSegments);
                    EditorGUILayout.PropertyField(heightSegments);
                    EditorGUILayout.PropertyField(top);
                    EditorGUILayout.PropertyField(bottom);

                    break;

                case SmartMesh.MeshTypes.Torus:

                    EditorGUILayout.PropertyField(start);
                    EditorGUILayout.PropertyField(radius);
                    EditorGUILayout.PropertyField(depth);
                    EditorGUILayout.PropertyField(radialSegments);
                    EditorGUILayout.PropertyField(widthSegments);

                    break;

                case SmartMesh.MeshTypes.Custom:

                    if (customTriangles.FindPropertyRelative("Array.size").hasMultipleDifferentValues == false)
                    {
                        if (customVertices.arraySize == 0)
                        {
                            customVertices.arraySize = 1;
                        }
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(customVertices);
                        EditorGUI.indentLevel--;
                        if (customTriangles.arraySize < 3)
                        {
                            customTriangles.arraySize = 3;
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Multi-object editing is not supported for triangle arrays of unequal size.", MessageType.None);
                    }

                    if (customTriangles.FindPropertyRelative("Array.size").hasMultipleDifferentValues == false)
                    {
                        if (customTriangles.arraySize % 3 != 0)
                        {
                            EditorGUILayout.HelpBox("The amount of triangles must be a multiple of 3!", MessageType.Error);
                        }
                        showCustomTriangles = SGO_EditorUtility.DrawFoldout(showCustomTriangles, "Custom Triangles", false);
                        if (showCustomTriangles == true)
                        {
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            for (int i = 0; i < customTriangles.arraySize; i++)
                            {
                                SerializedProperty property = customTriangles.GetArrayElementAtIndex(i);
                                if (property.intValue > customVertices.arraySize)
                                {
                                    property.intValue = customVertices.arraySize;
                                }
                                property.intValue = EditorGUILayout.IntSlider("Element " + i.ToString(), property.intValue, 0, customVertices.arraySize);
                            }
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(string.Empty, EditorStyles.boldLabel);
                            if (GUILayout.Button(string.Empty, "OL Plus", GUILayout.Width(25)))
                            {
                                customTriangles.InsertArrayElementAtIndex(customTriangles.arraySize);
                                customTriangles.InsertArrayElementAtIndex(customTriangles.arraySize);
                                customTriangles.InsertArrayElementAtIndex(customTriangles.arraySize);
                            }
                            if (GUILayout.Button(string.Empty, "OL Minus", GUILayout.Width(25)))
                            {
                                customTriangles.DeleteArrayElementAtIndex(customTriangles.arraySize - 3);
                                customTriangles.DeleteArrayElementAtIndex(customTriangles.arraySize - 2);
                                customTriangles.DeleteArrayElementAtIndex(customTriangles.arraySize - 1);
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                        }
                        if (serializedObject.hasModifiedProperties == true)
                        {
                            serializedObject.ApplyModifiedProperties();
                        }

                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Multi-object editing is not supported for triangle arrays of unequal size.", MessageType.None);
                    }


                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawUVUnwrappProperties()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#77C3E5>UV Unwrapping:</color>", richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUILayout.PropertyField(unwrapMode);
            EditorGUILayout.PropertyField(uvRotation);
            EditorGUILayout.PropertyField(tiling);
            EditorGUILayout.PropertyField(uvPreview);

            EditorGUILayout.EndVertical();
        }
        private void DrawMeshDebug()
        {
            if (self.vertices != null && self.triangles != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (self.meshfilter != null)
                {
                    if (self.meshfilter.sharedMesh != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Toggle Wireframe", EditorStyles.miniButton))
                        {
                            wireframe.boolValue = !wireframe.boolValue;
                        }
                        if (GUILayout.Button("Bake Mesh", EditorStyles.miniButton))
                        {
                            string path = EditorUtility.SaveFilePanelInProject("Save mesh asset", self.name + "_mesh", "asset", "Please enter a file name to save the mesh as");
                            if (path.Length != 0)
                            {
                                AssetDatabase.CreateAsset(self.meshfilter.sharedMesh, path);
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                                self.meshfilter.sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath(path, typeof(Mesh));
                            }
                            GUIUtility.ExitGUI();
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Vertices: " + self.vertices.Length, EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.LabelField("Triangles: " + (self.triangles.Length / 3), EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    self.meshfilter = self.GetComponent<MeshFilter>();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawSmartMeshHelp()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Space(7);
            SGO_EditorUtility.DrawHorizontalLine(true);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(EditorGUIUtility.IconContent("_help"), GUIStyle.none, GUILayout.Width(20))) { showSmartMeshHelp = !showSmartMeshHelp; }
            EditorGUILayout.EndHorizontal();

            if (showSmartMeshHelp == true)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(EditorGUIUtility.IconContent("_help@2x"), GUIStyle.none);
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Mesh:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Meshes are comprised of vertices, triangles and UVs where triangles are groups of 3 integers that corralate to a vertex and UVs represent vertices on a 2D plane.", wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("UV Unwrapping:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("UV unwrapping will approximate positions of vertices on a 2D plane by various projection methods. Stretching may occur when there are not enough vertices along a seam or projected faces are distorted.", wrappedMiniLabel);
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        [MenuItem("GameObject/Kitbashery/Smart Mesh")]
        static void CreateSmartMesh()
        {
            Selection.activeGameObject = new GameObject("Smart Mesh").AddComponent<SmartMesh>().gameObject;
        }
    }
}