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
    /// <summary>
    /// Custom <see cref="Editor"/> for <see cref="SmartManager"/>.cs
    /// </summary>
    [CustomEditor(typeof(SmartManager))]
    public class SmartManagerEditor : Editor
    {
        SerializedProperty updateDelay, throttleUpdates, targetFrameRate, currentFPS, smartGameObjects, smartPhysicsGameObjects, pauseUpdates, pools;

        private bool showPools, showCopyright, showSmartManagerHelp;

        private void OnEnable()
        {
            updateDelay = serializedObject.FindProperty("updateDelay");
            throttleUpdates = serializedObject.FindProperty("throttleUpdates");
            currentFPS = serializedObject.FindProperty("currentFPS");
            targetFrameRate = serializedObject.FindProperty("targetFrameRate");
            smartGameObjects = serializedObject.FindProperty("smartGameObjects");
            smartPhysicsGameObjects = serializedObject.FindProperty("smartPhysicsGameObjects");
            pauseUpdates = serializedObject.FindProperty("pauseUpdates");
            pools = serializedObject.FindProperty("pools");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawSmartManagerHelp();

            DrawFrameUpdateProperties();

            DrawPoolingUpdateProperties();

            DrawDebugProperties();

            EditorGUILayout.EndVertical();
            SGO_EditorUtility.DrawCopyrightNotice("https://kitbashery.com/docs/smart-gameobjects", "https://assetstore.unity.com/packages/slug/248930", "https://unity.com/legal/as-terms", "2023", "Kitbashery", ref showCopyright);

            if (serializedObject.hasModifiedProperties == true)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawFrameUpdateProperties()
        {
            // Draw frame update properties:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Frame Updates:", EditorStyles.boldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);

            EditorGUILayout.PropertyField(pauseUpdates);
            EditorGUILayout.PropertyField(throttleUpdates);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(targetFrameRate);
            if (EditorGUI.EndChangeCheck())
            {
                Application.targetFrameRate = targetFrameRate.intValue;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPoolingUpdateProperties()
        {
            // Draw pooling properties:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pooling:", EditorStyles.boldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            showPools = SGO_EditorUtility.DrawFoldout(showPools, "Pools", false);
            if (showPools == true)
            {
                EditorGUI.indentLevel++;
                for (int i = pools.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty prefab = pools.GetArrayElementAtIndex(i).FindPropertyRelative("prefab");
                    if (Application.isPlaying == false)
                    {
                        SerializedProperty amount = pools.GetArrayElementAtIndex(i).FindPropertyRelative("amount");
                        SerializedProperty maxAmount = pools.GetArrayElementAtIndex(i).FindPropertyRelative("maxAmount");

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(prefab);

                        if (GUILayout.Button(string.Empty, "OL Minus", GUILayout.Width(25)))
                        {
                            pools.DeleteArrayElementAtIndex(i);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.PropertyField(pools.GetArrayElementAtIndex(i).FindPropertyRelative("hideFlags"), GUILayout.Width(Screen.width - 68f));
                        EditorGUILayout.PropertyField(pools.GetArrayElementAtIndex(i).FindPropertyRelative("sequencialNaming"));

                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(amount);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (amount.intValue > maxAmount.intValue)
                            {
                                amount.intValue = maxAmount.intValue;
                            }
                        }
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(maxAmount);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (maxAmount.intValue < amount.intValue)
                            {
                                amount.intValue = maxAmount.intValue;
                            }
                        }

                        SGO_EditorUtility.DrawHorizontalLine(true);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Prefab Name: " + prefab.objectReferenceValue.name);
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.PropertyField(pools.GetArrayElementAtIndex(i).FindPropertyRelative("pooledObjects"));
                        EditorGUI.EndDisabledGroup();
                        SGO_EditorUtility.DrawHorizontalLine(false);
                    }

                    EditorGUILayout.Space();

                }

                if(Application.isPlaying == false)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Add Pool:");
                    if (GUILayout.Button(string.Empty, "OL Plus", GUILayout.Width(20)))
                    {
                        pools.InsertArrayElementAtIndex(0);
                        SerializedProperty newPool = pools.GetArrayElementAtIndex(0);
                        newPool.FindPropertyRelative("prefab").objectReferenceValue = null;
                        newPool.FindPropertyRelative("amount").intValue = 1;
                        newPool.FindPropertyRelative("maxAmount").intValue = 100;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawDebugProperties()
        {
            // Draw debug properties:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug:", EditorStyles.boldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUI.BeginDisabledGroup(true);
            if (throttleUpdates.boolValue == true)
            {
                EditorGUILayout.PropertyField(updateDelay);
                EditorGUILayout.PropertyField(currentFPS);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(smartGameObjects, GUILayout.Width(Screen.width - 40));
            EditorGUILayout.PropertyField(smartPhysicsGameObjects, GUILayout.Width(Screen.width - 40));
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;

            /*if (Application.isPlaying == true && GUILayout.Button("Stress Test (+100k)"))
            {
                throttleUpdates.boolValue = true;
                for (int i = 0; i < 100000; i++)
                {
                    new GameObject("SmartGO").AddComponent<SmartGameObject>();
                }
            }*/

            EditorGUILayout.EndVertical();
        }

        private void DrawSmartManagerHelp()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Space(7);
            SGO_EditorUtility.DrawHorizontalLine(true);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(EditorGUIUtility.IconContent("_help"), GUIStyle.none, GUILayout.Width(20))) { showSmartManagerHelp = !showSmartManagerHelp; }
            EditorGUILayout.EndHorizontal();

            if (showSmartManagerHelp == true)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(EditorGUIUtility.IconContent("_help@2x"), GUIStyle.none);
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Frame Updates:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Frame update options control the frequency of Smart GameObject updates based on the application's framerate target & current FPS. Note: update throttling does not apply to Smart GameObjects with rigidbodies.", SGO_EditorUtility.wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("Pooling:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Pooling recycles GameObjects to save instantiation overhead improving performance. Pools can be accessed by the name of the object that it creates.", SGO_EditorUtility.wrappedMiniLabel);
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }


        [MenuItem("GameObject/Kitbashery/Smart Manager")]
        static void CreateSmartManager()
        {
            SmartManager instance = FindObjectOfType<SmartManager>();
            if (instance == null)
            {
                Selection.activeGameObject = new GameObject("Smart Manager").AddComponent<SmartManager>().gameObject;
            }
            else
            {
                Debug.Log("There is already a SmartManager in the scene.", instance);
                Selection.activeGameObject = instance.gameObject;
            }
        }
    }
}