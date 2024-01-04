using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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
    /// Custom <see cref="Editor"/> for <see cref="SmartGameObject"/>s.
    /// </summary>
    [CustomEditor(typeof(SmartGameObject)), CanEditMultipleObjects]
    public class SmartGameObjectEditor : Editor
    {
        #region Properties:

        SmartGameObject self;

        SerializedProperty behaviors;

        SerializedProperty onEnable, onDisable, onTriggerStay, onCollisionStay;

        SerializedProperty floatVariables, booleanVariables;

        SerializedProperty focusTarget, smartTargets;
        SerializedProperty maxCollisions, requiredTags, ignoredColliders, rigid, col;

        SerializedProperty colliders, lastContact;

        SerializedProperty layerMask, triggerInteraction, maxRayDistance, rayCount, raySpacing, verticalScatter, horizontalScatter, raycastDebug;

        SerializedProperty agent, animator;

        SerializedProperty translationTarget, translationCurve, rotationTarget, lookAtTarget, horizontalLook, directLook, rotationCurve, scaleTarget, scaleCurve, splines, useLocalSpace, pathCurve, usePath, lookAtPath, spin, snappyScale;

        const string nameStr = "name", actionsStr = "actions", conditionsStr = "conditions", arraySizeStr = "Array.size";

        string[] toolbarStrings = { "<color=#FD6D40>Logic</color>", "<color=#B1FD59>Interaction</color>", "<color=#77C3E5>Motion</color>" };

        static Rect emptyRect = new Rect();

        const int listOffset = 40;
        // Note: this value should get offset when the scrollbar is visible or not in the inspector, this would need to be done via reflection.
        // Would EditorGUIUtility.currrentViewWidth help?
        int expandedListOffset = 0;

        /// <summary>
        /// Temporary condition used when evaluating conditions in the editor inspector for runtime debugging.
        /// </summary>
        Condition tempCondition = new Condition();

        #region Option Arrays:

        /*
         * If modifying these lists to add new actions or conditions you must do the following:
         * 
         * 1) Ensure both readable and writable lists have the same amount of entries.
         * 
         * 2) Conditions intended to be a numerical event will need to be added to the switch case in SmartGameObject.cs in the following methods:
         * InvokeNumericalAction() & GetComponentFloatValue() or the boolean equivlents.
         * 
         * Actions will need to be added to the switch case in InvokeSmartAction().
         * 
         * To create an action/condition that will pass values through fields you may pass an unused property of the action or condition via the editor.
         * You will need to add the index of the action/condition you want a field for in the DrawCondition() and DrawAction() methods below with the others in the or || if statement.
         * 
         */

        [HideInInspector, NonReorderable]
        public string[] readableComponentFloats = new string[158]
        {
        "Custom Value",
        "Smart Variable",
        "Constants / PI",
        "Constants / Phi",
        "Constants / Epsilon",
        "Constants / Infinity",
        "Constants / Negative Infinity",
        "Constants / Max Value",
        "Constants / Degrees to Radians",
        "Constants / Radians to Degrees",
        "Transform / Position / X (Global)", // 10
        "Transform / Position / Y (Global)",
        "Transform / Position / Z (Global)",
        "Transform / Position / X (Local)",
        "Transform / Position / Y (Local)",
        "Transform / Position / Z (Local)",
        "Transform / Rotation / X (Global)",
        "Transform / Rotation / Y (Global)",
        "Transform / Rotation / Z (Global)",
        "Transform / Rotation / X (Local)",
        "Transform / Rotation / Y (Local)", // 20
        "Transform / Rotation / Z (Local)",
        "Transform / Scale / X (Global)",
        "Transform / Scale / Y (Global)",
        "Transform / Scale / Z (Global)",
        "Transform / Scale / X (Local)",
        "Transform / Scale / Y (Local)",
        "Transform / Scale / Z (Local)",
        "Transform / Child Count",
        "Rigidbody / Angular Velocity Magnitude",
        "Rigidbody / Max Angular Velocity", // 30
        "Rigidbody / Mass",
        "Rigidbody / Drag",
        "Rigidbody / Angular Drag",
        "Rigidbody / Max Depenetration Velocity",
        "Rigidbody / Velocity Magnitude",
        "NavMeshAgent / Acceleration",
        "NavMeshAgent / Angular Speed",
        "NavMeshAgent / Base Offset",
        "NavMeshAgent / Velocity Magnitude",
        "NavMeshAgent / Speed", // 40
        "NavMeshAgent / Height",
        "NavMeshAgent / Radius",
        "NavMeshAgent / Stopping Distance",
       "AudioSource / Doppler Level",
        "AudioSource / Min Distance",
       "AudioSource / Max Distance",
       "AudioSource / Volume",
       "AudioSource / Pan Stereo",
       "AudioSource / Pitch",
       "AudioSource / Reverb Zone Mix", // 50
       "AudioSource / Spread",
       "AudioSource / Spatial Blend",
       "AudioSource / Time",
       "Animator / Float Parameter",
       "Animator / Integer Parameter",
       "Animator / Speed",
       "Animator / Pivot Weight",
       "Animator / Playback Time",
       "Animator / Left Feet Bottom Height",
       "Animator / Right Feet Bottom Height", // 60
       "Animator / Angular Velocity Magnitude",
       "Distance / To Focus Target",
       "Distance / To First Target",
       "Distance / To Last Target",
       "Distance / To Random Target",
       "Distance / To Nearest Target",
       "Distance / To Farthest Target",
       "Distance / To Translation Target",
       "Distance / To First Raycast Hit",
       "Distance / To Last Raycast Hit", // 70
       "Distance / To Random Raycast Hit",
       "Distance / To Last Collision (trigger detection)",
       "Distance / To Last Contact",
       "Distance / To Start of Path",
       "Time / Delta Time",
       "Time / Unscaled Delta Time",
       "Time / Fixed Delta Time",
       "Time / Fixed Unscaled Delta Time",
       "Physics / Max Ray Distance",
       "Physics / Ray Spacing", // 80
       "Physics / Ray Count",
       "Physics / Ray Vertical Scatter",
       "Physics / Ray Horizontal Scatter",
       "Physics / Max Collisions",
        "Focus Target / Smart Variable",
        "Focus Target / Transform / Position / X (Global)",
        "Focus Target / Transform / Position / Y (Global)",
        "Focus Target / Transform / Position / Z (Global)",
        "Focus Target / Transform / Position / X (Local)",
        "Focus Target / Transform / Position / Y (Local)", // 90
        "Focus Target / Transform / Position / Z (Local)",
        "Focus Target / Transform / Rotation / X (Global)",
        "Focus Target / Transform / Rotation / Y (Global)",
        "Focus Target / Transform / Rotation / Z (Global)",
        "Focus Target / Transform / Rotation / X (Local)",
        "Focus Target / Transform / Rotation / Y (Local)",
        "Focus Target / Transform / Rotation / Z (Local)",
        "Focus Target / Transform / Scale / X (Global)",
        "Focus Target / Transform / Scale / Y (Global)",
        "Focus Target / Transform / Scale / Z (Global)", // 100
        "Focus Target / Transform / Scale / X (Local)",
        "Focus Target / Transform / Scale / Y (Local)",
        "Focus Target / Transform / Scale / Z (Local)",
        "Focus Target / Transform / Child Count",
        "Focus Target / Rigidbody / Angular Velocity Magnitude",
        "Focus Target / Rigidbody / Max Angular Velocity",
        "Focus Target / Rigidbody / Mass",
        "Focus Target / Rigidbody / Drag",
        "Focus Target / Rigidbody / Angular Drag",
        "Focus Target / Rigidbody / Max Depenetration Velocity", // 110
        "Focus Target / Rigidbody / Velocity Magnitude",
        "Focus Target / NavMeshAgent / Acceleration",
        "Focus Target / NavMeshAgent / Angular Speed",
        "Focus Target / NavMeshAgent / Base Offset",
        "Focus Target / NavMeshAgent / Velocity Magnitude",
        "Focus Target / NavMeshAgent / Speed",
        "Focus Target / NavMeshAgent / Height",
        "Focus Target / NavMeshAgent / Radius",
        "Focus Target / NavMeshAgent / Stopping Distance",
        "Focus Target / AudioSource / Doppler Level", // 120
        "Focus Target / AudioSource / Min Distance",
       "Focus Target / AudioSource / Max Distance",
       "Focus Target / AudioSource / Volume",
       "Focus Target / AudioSource / Pan Stereo",
       "Focus Target / AudioSource / Pitch",
       "Focus Target / AudioSource / Reverb Zone Mix",
       "Focus Target / AudioSource / Spread",
       "Focus Target / AudioSource / Spatial Blend",
       "Focus Target / AudioSource / Time",
       "Focus Target / Animator / Float Parameter", // 130
       "Focus Target / Animator / Integer Parameter",
       "Focus Target / Animator / Speed",
       "Focus Target / Animator / Pivot Weight",
       "Focus Target / Animator / Playback Time",
       "Focus Target / Animator / Left Feet Bottom Height",
       "Focus Target / Animator / Right Feet Bottom Height",
       "Focus Target / Animator / Angular Velocity Magnitude",
       "Focus Target / Physics / Max Ray Distance",
       "Focus Target / Physics / Ray Spacing",
       "Focus Target / Physics / Ray Count", // 140
       "Focus Target / Physics / Ray Vertical Scatter",
       "Focus Target / Physics / Ray Horizontal Scatter",
       "Focus Target / Physics / Max Collisions",
       "NavMeshAgent / Remaining Distance",
       "Focus Target / NavMeshAgent / Remaining Distance",
       "Physics / Last Raycast Hit Normal / X",
       "Physics / Last Raycast Hit Normal / Y",
       "Physics / Last Raycast Hit Normal / Z",
       "Focus Target / Physics / Last Raycast Hit Normal / X",
       "Focus Target / Physics / Last Raycast Hit Normal / Y",
       "Focus Target / Physics / Last Raycast Hit Normal / Z",
       "Transform / Position / Magnitude",
       "Focus Target / Transform / Position / Magnitude (Local)",
       "Transform / Scale / Magnitude",
       "Focus Target / Transform / Scale / Magnitude (Local)",
       "Transform / Rotation / Magnitude",
       "Focus Target / Transform / Rotation / Magnitude",
      };

        [HideInInspector, NonReorderable]
        public string[] writableComponentFloats = new string[158]
        {
        "Custom Value (read-only) / ",
        "Smart Variable",
        "Constants (read-only) / ",
        "Constants (read-only) / ",
        "Constants (read-only) / ",
        "Constants (read-only) / ",
        "Constants (read-only) / ",
        "Constants (read-only) / ",
        "Constants (read-only) / ",
        "Constants (read-only) / ",
        "Transform / Position / X (Global)",
        "Transform / Position / Y (Global)",
        "Transform / Position / Z (Global)",
        "Transform / Position / X (Local)",
        "Transform / Position / Y (Local)",
        "Transform / Position / Z (Local)",
        "Transform / Rotation / X (Global)",
        "Transform / Rotation / Y (Global)",
        "Transform / Rotation / Z (Global)",
        "Transform / Rotation / X (Local)",
        "Transform / Rotation / Y (Local)",
        "Transform / Rotation / Z (Local)",
        "Transform / Scale / X (Global) (read-only) / ",
        "Transform / Scale / Y (Global) (read-only) / ",
        "Transform / Scale / Z (Global) (read-only) / ",
        "Transform / Scale / X (Local)",
        "Transform / Scale / Y (Local)",
        "Transform / Scale / Z (Local)",
        "Transform / Child Count (read-only) / ",
        "Rigidbody / Angular Velocity Magnitude (read-only) / ",
        "Rigidbody / Max Angular Velocity",
        "Rigidbody / Mass",
        "Rigidbody / Drag",
        "Rigidbody / Angular Drag",
        "Rigidbody / Max Depenetration Velocity",
        "Rigidbody / Velocity Magnitude (read-only) / ",
        "NavMeshAgent / Acceleration",
        "NavMeshAgent / Angular Speed",
        "NavMeshAgent / Base Offset",
        "NavMeshAgent / Velocity Magnitude (read-only) / ",
        "NavMeshAgent / Speed",
        "NavMeshAgent / Height",
        "NavMeshAgent / Radius",
        "NavMeshAgent / Stopping Distance",
        "AudioSource / Doppler Level",
        "AudioSource / Min Distance",
       "AudioSource / Max Distance",
       "AudioSource / Volume",
       "AudioSource / Pan Stereo",
       "AudioSource / Pitch",
       "AudioSource / Reverb Zone Mix",
       "AudioSource / Spread",
       "AudioSource / Spatial Blend",
       "AudioSource / Time",
       "Animator / Float Parameter",
       "Animator / Integer Parameter",
       "Animator / Speed",
       "Animator / Pivot Weight (read-only) / ",
       "Animator / Playback Time",
       "Animator / Left Feet Bottom Height (read-only) / ",
       "Animator / Right Feet Bottom Height (read-only) / ",
       "Animator / Angular Velocity Magnitude (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Distance (read-only) / ",
       "Time (read-only) / ",
       "Time (read-only) / ",
       "Time (read-only) / ",
       "Time (read-only) / ",
       "Time (read-only) / ",
       "Physics / Max Ray Distance",
       "Physics / Ray Spacing",
       "Physics / Ray Count",
       "Physics / Ray Vertical Scatter",
       "Physics / Ray Horizontal Scatter",
       "Physics / Max Collisions",
       "Focus Target / Smart Variable",
        "Focus Target / Transform / Position / X (Global)",
        "Focus Target / Transform / Position / Y (Global)",
        "Focus Target / Transform / Position / Z (Global)",
        "Focus Target / Transform / Position / X (Local)",
        "Focus Target / Transform / Position / Y (Local)",
        "Focus Target / Transform / Position / Z (Local)",
        "Focus Target / Transform / Rotation / X (Global)",
        "Focus Target / Transform / Rotation / Y (Global)",
        "Focus Target / Transform / Rotation / Z (Global)",
        "Focus Target / Transform / Rotation / X (Local)",
        "Focus Target / Transform / Rotation / Y (Local)",
        "Focus Target / Transform / Rotation / Z (Local)",
        "Focus Target / Transform / Scale / X (Global) (read-only) / ",
        "Focus Target / Transform / Scale / Y (Global) (read-only) / ",
        "Focus Target / Transform / Scale / Z (Global) (read-only) / ",
        "Focus Target / Transform / Scale / X (Local)",
        "Focus Target / Transform / Scale / Y (Local)",
        "Focus Target / Transform / Scale / Z (Local)",
        "Focus Target / Transform / Child Count (read-only) / ",
        "Focus Target / Rigidbody / Angular Velocity Magnitude (read-only) / ",
        "Focus Target / Rigidbody / Max Angular Velocity",
        "Focus Target / Rigidbody / Mass",
        "Focus Target / Rigidbody / Drag",
        "Focus Target / Rigidbody / Angular Drag",
        "Focus Target / Rigidbody / Max Depenetration Velocity",
        "Focus Target / Rigidbody / Velocity Magnitude",
        "Focus Target / NavMeshAgent / Acceleration",
        "Focus Target / NavMeshAgent / Angular Speed",
        "Focus Target / NavMeshAgent / Base Offset",
        "Focus Target / NavMeshAgent / Velocity Magnitude (read-only) / ",
        "Focus Target / NavMeshAgent / Speed",
        "Focus Target / NavMeshAgent / Height",
        "Focus Target / NavMeshAgent / Radius",
        "Focus Target / NavMeshAgent / Stopping Distance",
        "Focus Target / AudioSource / Doppler Level",
        "Focus Target / AudioSource / Min Distance",
       "Focus Target / AudioSource / Max Distance",
       "Focus Target / AudioSource / Volume",
       "Focus Target / AudioSource / Pan Stereo",
       "Focus Target / AudioSource / Pitch",
       "Focus Target / AudioSource / Reverb Zone Mix",
       "Focus Target / AudioSource / Spread",
       "Focus Target / AudioSource / Spatial Blend",
       "Focus Target / AudioSource / Time",
       "Focus Target / Animator / Float Parameter",
       "Focus Target / Animator / Integer Parameter",
       "Focus Target / Animator / Speed",
       "Focus Target / Animator / Pivot Weight (read-only) / ",
       "Focus Target / Animator / Playback Time",
       "Focus Target / Animator / Left Feet Bottom Height (read-only) / ",
       "Focus Target / Animator / Right Feet Bottom Height (read-only) / ",
       "Focus Target / Animator / Angular Velocity Magnitude (read-only) / ",
       "Focus Target / Physics / Max Ray Distance",
       "Focus Target / Physics / Ray Spacing",
       "Focus Target / Physics / Ray Count",
       "Focus Target / Physics / Ray Vertical Scatter",
       "Focus Target / Physics / Ray Horizontal Scatter",
       "Focus Target / Physics / Max Collisions",
       "NavMeshAgent / Remaining Distance",
       "Focus Target / NavMeshAgent / Remaining Distance",
       "Physics / Last Raycast Hit Normal / X (read-only) / ",
       "Physics / Last Raycast Hit Normal / Y (read-only) / ",
       "Physics / Last Raycast Hit Normal / Z (read-only) / ",
       "Focus Target / Physics / Last Raycast Hit Normal / X (read-only) / ",
       "Focus Target / Physics / Last Raycast Hit Normal / Y (read-only) / ",
       "Focus Target / Physics / Last Raycast Hit Normal / Z (read-only) / ",
       "Transform / Position / Magnitude (read-only) / ",
       "Focus Target / Transform / Position / Magnitude (read-only) / ",
       "Transform / Scale / Magnitude (Local) (read-only) / ",
       "Focus Target / Transform / Scale / (Local) Magnitude (read-only) / ",
       "Transform / Rotation / Magnitude (read-only) / ",
       "Focus Target / Transform / Rotation / Magnitude (read-only) / ",
        };

        [HideInInspector, NonReorderable]
        public string[] readableComponentBooleans = new string[110]
        {
        "True",
        "False",
        "Smart Variable",
        "GameObject / Is Active",
        "GameObject / Is Static",
        "GameObject / Has Tag",
        "Transform / Is Child",
        "Rigidbody / Is Null",
        "Rigidbody / Is Sleeping",
        "Rigidbody / Is Kinematic",
        "NavMeshAgent / Is Null", // 10
        "NavMeshAgent / Is Enabled",
        "NavMeshAgent / Is On NavMesh",
        "NavMeshAgent / Is On NavMeshLink",
        "NavMeshAgent / Is Path Stale",
        "NavMeshAgent / Is Stopped",
        "NavMeshAgent / Path Pending",
        "Tween / Is Rotating",
        "Tween / Is Translating",
        "Tween / Is Scaling",
        "Tween / Use Path", // 20
        "Tween / Spinning",
        "Tween / Snappy Scale",
        "Tween / Path In Local Space",
        "Tween / Look At Path",
        "Audio Source / Is Null",
        "Audio Source / Enabled",
        "Audio Source / Playing",
        "Audio Source / Virtual",
        "Audio Source / Looping",
        "Audio Source / Muted", // 30
        "Audio Source / Spatialized",
        "Audio Source / Post Effects Spatialized",
        "Animator / Is Null",
        "Animator / Enabled",
        "Animator / Applying Root Motion",
        "Animator / Fire Events",
        "Animator / Bool Parameter",
        "Animator / Is Human",
        "Animator / Is Initialized",
        "Animator / Is Matching Target", // 40
        "Animator / Is Optimizable",
        "Animator / Stabilize Feet",
        "Animator / Is In Transition",
        "Targets / Has Targets",
        "Targets / Contains Target With Tag",
        "Physics / Has Contacts",
        "Physics / Has Raycast Hits",
        "Focus Target / Smart Variable",
        "Focus Target / GameObject / Is Null",
        "Focus Target / GameObject / Is Active",
        "Focus Target / GameObject / Is Static",
        "Focus Target / GameObject / Has Tag",
        "Focus Target / Transform / Is Child",
        "Focus Target / Rigidbody / Is Null",
        "Focus Target / Rigidbody / Is Sleeping",
        "Focus Target / Rigidbody / Is Kinematic",
        "Focus Target / NavMeshAgent / Is Null",
        "Focus Target / NavMeshAgent / Is Enabled",
        "Focus Target / NavMeshAgent / Is On NavMesh",
        "Focus Target / NavMeshAgent / Is On NavMeshLink",
        "Focus Target / NavMeshAgent / Is Path Stale",
        "Focus Target / NavMeshAgent / Is Stopped",
        "Focus Target / NavMeshAgent / Path Pending",
        "Focus Target / Tween / Is Rotating",
        "Focus Target / Tween / Is Translating",
        "Focus Target / Tween / Is Scaling",
        "Focus Target / Tween / Use Path", 
        "Focus Target / Tween / Spinning",
        "Focus Target / Tween / Snappy Scale",
        "Focus Target / Tween / Path In Local Space",
        "Focus Target / Tween / Look At Path",
        "Focus Target / Audio Source / Is Null",
        "Focus Target / Audio Source / Enabled",
        "Focus Target / Audio Source / Playing",
        "Focus Target / Audio Source / Virtual",
        "Focus Target / Audio Source / Looping",
        "Focus Target / Audio Source / Muted",
        "Focus Target / Audio Source / Spatialized",
        "Focus Target / Audio Source / Post Effects Spatialized",
        "Focus Target / Animator / Is Null",
        "Focus Target / Animator / Enabled",
        "Focus Target / Animator / Applying Root Motion",
        "Focus Target / Animator / Fire Events",
        "Focus Target / Animator / Bool Parameter",
        "Focus Target / Animator / Is Human",
        "Focus Target / Animator / Is Initialized",
        "Focus Target / Animator / Is Matching Target",
        "Focus Target / Animator / Is Optimizable",
        "Focus Target / Animator / Stabilize Feet",
        "Focus Target / Animator / Is In Transition",
        "Focus Target / Targets / Has Targets",
        "Focus Target / Targets / Has Focus Target",
        "Focus Target / Targets / Contains Target With Tag",
        "Focus Target / Physics / Has Contacts",
        "Focus Target / Physics / Has Raycast Hits",
        "Tween / Billboard Direct Look At",
        "Tween / Billboard Look Horizontal",
        "Focus Target / Tween / Billboard Direct Look At",
        "Focus Target / Tween / Billboard Look Horizontal",
        "Tween / Has Look At Target",
        "Focus Target / Tween / Has Look At Target",
        "NavMeshAgent / Auto Braking",
        "Focus Target / NavMeshAgent / Auto Braking",
        "NavMeshAgent / Destination Reached",
        "Focus Target / NavMeshAgent / Destination Reached",
        "Smart GameObject / Has Float Variable",
        "Focus Target / Smart GameObject / Has Float Variable",
        "Smart GameObject / Has Boolean Variable",
        "Focus Target / Smart GameObject / Has Boolean Variable",
        };

        [HideInInspector, NonReorderable]
        public string[] writableComponentBooleans = new string[110]
        {
        "True (read-only) / ",
        "False (read-only) / ",
        "Smart Variable",
        "GameObject / Is Active",
        "GameObject / Is Static",
        "GameObject / Has Tag (read-only) / ",
        "Transform / Is Child (read-only) / ",
        "Rigidbody / Is Null (read-only) / ",
        "Rigidbody / Is Sleeping (read-only) / ",
        "Rigidbody / Is Kinematic",
        "NavMeshAgent / Is Null (read-only) / ", // 10
        "NavMeshAgent / Is Enabled",
        "NavMeshAgent / Is On NavMesh (read-only) / ",
        "NavMeshAgent / Is On NavMeshLink (read-only) / ",
        "NavMeshAgent / Is Path Stale (read-only) / ",
        "NavMeshAgent / Is Stopped",
        "NavMeshAgent / Path Pending (read-only) / ",
        "Tween / Is Rotating (read-only) / ",
        "Tween / Is Translating (read-only) / ",
        "Tween / Is Scaling (read-only) / ",
        "Tween / Use Path", // 20
        "Tween / Spinning",
        "Tween / Snappy Scale",
        "Tween / Path In Local Space",
        "Tween / Look At Path",
        "Audio Source / Is Null (read-only) / ",
        "Audio Source / Enabled",
        "Audio Source / Playing (read-only) / ",
        "Audio Source / Virtual (read-only) / ",
        "Audio Source / Looping",
        "Audio Source / Muted", // 30
        "Audio Source / Spatialized",
        "Audio Source / Post Effects Spatialized",
        "Animator / Is Null (read-only) / ",
        "Animator / Enabled",
        "Animator / Applying Root Motion",
        "Animator / Fire Events",
        "Animator / Bool Parameter",
        "Animator / Is Human (read-only) / ",
        "Animator / Is Initialized (read-only) / ",
        "Animator / Is Matching Target (read-only) / ", // 40
        "Animator / Is Optimizable (read-only) / ",
        "Animator / Stabilize Feet",
        "Animator / Is In Transition (read-only) / ",
        "Targets / Has Targets (read-only) / ",
        "Targets / Contains Target With Tag (read-only) / ",
        "Physics / Has Contacts (read-only) / ",
        "Physics / Has Raycast Hits (read-only) / ",
        "Focus Target / Smart Variable",
        "Focus Target / GameObject / Is Null  (read-only) / ",
        "Focus Target / GameObject / Is Active", // 50
        "Focus Target / GameObject / Is Static",
        "Focus Target / GameObject / Has Tag (read-only) / ",
        "Focus Target / Transform / Is Child (read-only) / ",
        "Focus Target / Rigidbody / Is Null (read-only) / ",
        "Focus Target / Rigidbody / Is Sleeping (read-only) / ",
        "Focus Target / Rigidbody / Is Kinematic",
        "Focus Target / NavMeshAgent / Is Null (read-only) / ",
        "Focus Target / NavMeshAgent / Is Enabled",
        "Focus Target / NavMeshAgent / Is On NavMesh (read-only) / ",
        "Focus Target / NavMeshAgent / Is On NavMeshLink (read-only) / ", // 60
        "Focus Target / NavMeshAgent / Is Path Stale (read-only) / ",
        "Focus Target / NavMeshAgent / Is Stopped",
        "Focus Target / NavMeshAgent / Path Pending (read-only) / ",
        "Focus Target / Tween / Is Rotating (read-only) / ",
        "Focus Target / Tween / Is Translating (read-only) / ",
        "Focus Target / Tween / Is Scaling (read-only) / ",
        "Focus Target / Tween / Use Path",
        "Focus Target / Tween / Spinning",
        "Focus Target / Tween / Snappy Scale",
        "Focus Target / Tween / Path In Local Space", // 70
        "Focus Target / Tween / Look At Path",
        "Focus Target / Audio Source / Is Null (read-only) / ",
        "Focus Target / Audio Source / Enabled",
        "Focus Target / Audio Source / Playing (read-only) / ",
        "Focus Target / Audio Source / Virtual (read-only) / ",
        "Focus Target / Audio Source / Looping",
        "Focus Target / Audio Source / Muted",
        "Focus Target / Audio Source / Spatialized",
        "Focus Target / Audio Source / Post Effects Spatialized",
        "Focus Target / Animator / Is Null (read-only) / ", // 80
        "Focus Target / Animator / Enabled",
        "Focus Target / Animator / Applying Root Motion",
        "Focus Target / Animator / Fire Events",
        "Focus Target / Animator / Bool Parameter",
        "Focus Target / Animator / Is Human (read-only) / ",
        "Focus Target / Animator / Is Initialized (read-only) / ",
        "Focus Target / Animator / Is Matching Target (read-only) / ",
        "Focus Target / Animator / Is Optimizable (read-only) / ",
        "Focus Target / Animator / Stabilize Feet",
        "Focus Target / Animator / Is In Transition (read-only) / ", // 90
        "Focus Target / Targets / Has Targets (read-only) / ",
        "Focus Target / Targets / Has Focus Target (read-only) / ",
        "Focus Target / Targets / Contains Target With Tag (read-only) / ",
        "Focus Target / Physics / Has Contacts (read-only) / ",
        "Focus Target / Physics / Has Raycast Hits (read-only) / ",
        "Tween / Billboard Direct Look At",
        "Tween / Billboard Look Horizontal",
        "Focus Target / Tween / Billboard Direct Look At",
        "Focus Target / Tween / Billboard Look Horizontal",
        "Tween / Has Look At Target (read-only) / ", // 100
        "Focus Target / Tween / Has Look At Target (read-only) / ",
        "NavMeshAgent / Auto Braking",
        "Focus Target / NavMeshAgent / Auto Braking",
        "NavMeshAgent / Destination Reached (read-only) / ",
        "Focus Target / NavMeshAgent / Destination Reached (read-only) / ",
        "Smart GameObject / Has Float Variable (read-only) / ",
        "Focus Target / Smart GameObject / Has Float Variable (read-only) / ",
        "Smart GameObject / Has Boolean Variable (read-only) / ",
        "Focus Target / Smart GameObject / Has Boolean Variable (read-only) / ",
};
        [HideInInspector, NonReorderable]
        public string[] actions = new string[124]
        {
        "Do Nothing...",
        "Tween / Translate / Start Translation",
        "Tween / Translate / Stop Translation",
        "Tween / Translate / Pause Translation",
        "Tween / Translate / Resume Translation",
        "Tween / Translate / Start Path Translation",
        "Tween / Translate / Stop Path Translation",
        "Tween / Translate / Set Translation Target",
        "Tween / Translate / Set Translation Target To Focus Target",
        "Tween / Translate / Set Translation Target To Last Raycast Hit",
        "Tween / Rotate / Look At Focus Target", // 10
        "Tween / Rotate / Look At Main Camera",
        "Tween / Rotate / Stop Look At",
        "Tween / Rotate / Start Rotating",
        "Tween / Rotate / Stop Rotating",
        "Tween / Rotate / Pause Rotation",
        "Tween / Rotate / Resume Rotation",
        "Tween / Rotate / Set Rotation Target",
        "Tween / Rotate / Set Rotation Target To Focus Target",
        "Tween / Rotate / Set Rotation Target To Last Raycast Hit Normal",
        "Tween / Scale / Start Scale", // 20
        "Tween / Scale / Stop Scale",
        "Tween / Scale / Pause Scale",
        "Tween / Scale / Resume Scale",
        "Tween / Scale / Set Scale Target",
        "Tween / Scale / Set Scale Target To Focus Target Scale",
        "Transform / Parent To Focus Target",
        "Transform / Clear Parent",
        "Transform / Set To Initial Values",
        "Transform / Clear Transform Values",
        "Transform / Set To Focus Target Values", // 30
        "Raycast / Raycast Single",
        "Raycast / Raycast All",
        "Raycast / Raycast Row",
        "Raycast / Circle Cast (experimental)",
        "Raycast / Grid Cast",
        "Raycast / Scatter Cast",
        "Raycast / Fan Cast",
        "Raycast / Sphere Cast",
        "Raycast / Sphere Cast All",
        "Raycast / Box Cast", // 40
        "Raycast / Box Cast All",
        "Raycast / Capsule Cast",
        "Raycast / Line Cast",
        "Animator / Set Trigger",
        "Animator / Play",
        "Animator / Stop Playback",
        "Spawn / At Focus Target",
        "Spawn / At Last Raycast Hit",
        "Spawn / At All Raycast Hits",
        "Spawn / At All Colliders", // 50
        "Spawn / At Last Collision (trigger detection)",
        "Spawn / At Last Collider",
        "Spawn / At All Target Positions",
        "Spawn / At This Position",
        "NavMeshAgent / Follow Focus Target",
        "NavMeshAgent / Flee From Focus Target",
        "NavMeshAgent / Follow Focus Target Path",
        "NavMeshAgent / Follow Own Path",
        "NavMeshAgent / Wander",
        "Targets / Target Last Collision (trigger detection)", // 60
        "Targets / Target Last Collider Contact",
        "Targets / Target All Colliders Contacts",
        "Targets / Target All Raycast Hits",
        "Targets / Target Collider Contacts With Tag",
        "Targets / Target Raycast Hits With Tag",
        "Targets / Clear All",
        "Focus / On Nothing",
        "Focus / On First Target",
        "Focus / On Last Target",
        "Focus / On Farthest Target", // 70
        "Focus / On Nearest Target",
        "Focus / On Target With Tag",
        "Focus / On Farthest Target With Tag",
        "Focus / On Nearest Target With Tag",
        "Targets / Clear Nearest Target",
        "Targets / Clear Farthest Target",
        "GameObject / Deactivate",
        "GameObject / Destroy",
        "Debug / Log Info Message",
        "Debug / Log Warning Message", // 80
        "Debug / Log Error Message",
        "Debug / Root Direction",
        "Debug / Float Message",
        "Debug / Boolean Message",
        "Rigidbody / Seek Focus Target",
        "Rigidbody / Richochet",
        "Rigidbody / Add Force",
        "Rigidbody / Add Impulse Force",
        "Rigidbody / Add Acceleration Force",
        "Rigidbody / Change Velocity", // 90
        "Rigidbody / Add Torque Force",
        "Rigidbody / Add Torque Impulse Force",
        "Rigidbody / Add Torque Acceleration Force",
        "Rigidbody / Add Relative Force",
        "Rigidbody / Add Relative Torque",
        "Tween / Translate / Set Curve",
        "Tween / Rotate / Set Curve",
        "Tween / Scale / Set Curve",
        "Tween / Translate / Set Path Curve",
        "Smart GameObject / Disable Behavior",
        "Smart GameObject / Enable Behavior",
        "Smart GameObject / Force Stop Behavior Delay",
        "Smart GameObject / Stop All Coroutines",
        "Smart GameObject / Disable Self",
        "Transform / Set Position / To Custom Value",
        "Transform / Set Rotation / To Custom Value",
        "Transform / Set Scale / To Custom Value",
        "Transform / Set Position / To Focus Target Position",
        "Transform / Set Rotation / To Focus Target Rotation",
        "Transform / Set Scale / To Focus Target Scale",
        "Targets / Remove Focus Target",
        "Targets / Remove Null Entries",
        "Targets / Reverse Order",
        "Focus / On Last Spawned",
        "Rigidbody / Add Forward Force",
        "Raycast / Clear Hits",
        "Rigidbody / Clear Contacts",
        "Transform / Look At / Last Spawn",
        "Transform / Look At / Last Collider Contact",
        "Transform / Look At / Last Raycast Hit",
        "Transform / Look At / Last Collision Contact (trigger detection)",
        "Transform / Look At / Focus Target's Last Spawn",
        "Transform / Look At / Focus Target",
        };

        #endregion

        SmartBehaviorScriptableObject smartSO;

        #endregion

        #region Initialization & Updates:

        private void OnEnable()
        {
            behaviors = serializedObject.FindProperty("behaviors");

            floatVariables = serializedObject.FindProperty("floatVariables");
            booleanVariables = serializedObject.FindProperty("booleanVariables");

            focusTarget = serializedObject.FindProperty("focusTarget");
            smartTargets = serializedObject.FindProperty("targets");
            maxCollisions = serializedObject.FindProperty("maxCollisions");
            requiredTags = serializedObject.FindProperty("requiredTags");
            ignoredColliders = serializedObject.FindProperty("ignoredColliders");
            rigid = serializedObject.FindProperty("rigid");
            col = serializedObject.FindProperty("col");
            onCollisionStay = serializedObject.FindProperty("onCollisionStay");
            onTriggerStay = serializedObject.FindProperty("onTriggerStay");

            layerMask = serializedObject.FindProperty("layerMask");
            triggerInteraction = serializedObject.FindProperty("triggerInteraction");
            maxRayDistance = serializedObject.FindProperty("maxRayDistance");
            raySpacing = serializedObject.FindProperty("raySpacing");
            verticalScatter = serializedObject.FindProperty("verticalScatter");
            horizontalScatter = serializedObject.FindProperty("horizontalScatter");
            rayCount = serializedObject.FindProperty("rayCount");
            raycastDebug = serializedObject.FindProperty("raycastDebug");

            agent = serializedObject.FindProperty("agent");
            animator = serializedObject.FindProperty("animator");

            translationTarget = serializedObject.FindProperty("translationTarget");
            translationCurve = serializedObject.FindProperty("translationCurve");
            lookAtTarget = serializedObject.FindProperty("lookAtTarget");
            horizontalLook = serializedObject.FindProperty("horizontalLook");
            directLook = serializedObject.FindProperty("directLook");
            rotationTarget = serializedObject.FindProperty("rotationTarget");
            rotationCurve = serializedObject.FindProperty("rotationCurve");
            scaleTarget = serializedObject.FindProperty("scaleTarget");
            scaleCurve = serializedObject.FindProperty("scaleCurve");
            splines = serializedObject.FindProperty("splines");
            useLocalSpace = serializedObject.FindProperty("useLocalSpace");
            pathCurve = serializedObject.FindProperty("pathCurve");
            usePath = serializedObject.FindProperty("usePath");
            lookAtPath = serializedObject.FindProperty("lookAtPath");
            spin = serializedObject.FindProperty("spin");
            snappyScale = serializedObject.FindProperty("snappyScale");

            colliders = serializedObject.FindProperty("colliders");
            lastContact = serializedObject.FindProperty("lastContact");

            onEnable = serializedObject.FindProperty("onEnable");
            onDisable = serializedObject.FindProperty("onDisable");
        }

        public override void OnInspectorGUI()
        {
            self = (SmartGameObject)target;
            if(self.myTransform == null)
            {
                self.myTransform = self.transform;
            }
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            self.toolbarInt = GUILayout.Toolbar(self.toolbarInt, toolbarStrings, SGO_EditorUtility.richMiniButtonMid);
            if (EditorGUI.EndChangeCheck())
            {
                self.showInteractionHelp = false;
                self.showLogicHelp = false;
                self.showMotionHelp = false;
            }
            EditorGUILayout.EndHorizontal();
            SGO_EditorUtility.DrawHorizontalLine(true);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            switch (self.toolbarInt)
            {
                case 0:

                    DrawLogicHelp();
                    DrawLogicInspector();

                    break;

                case 1:

                    DrawInteractionHelp();
                    DrawInteractionInspector();

                    break;

                case 2:

                    DrawMotionHelp();
                    DrawMotionInspector();

                    break;
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            SGO_EditorUtility.DrawCopyrightNotice("https://kitbashery.com/docs/smart-gameobjects", "https://assetstore.unity.com/packages/slug/248930", "https://unity.com/legal/as-terms", "2023", "Kitbashery", ref self.showCopyright);

            if (serializedObject.hasModifiedProperties == true)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion

        #region Methods:

        private void DrawLogicInspector()
        {
            // Draw behaviors:
            if (behaviors.arraySize > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawPagination();
                DrawBehaviour(behaviors, self.pagination - 1);
                EditorGUILayout.EndVertical();
            }
            else
            {
                behaviors.InsertArrayElementAtIndex(0);
                behaviors.GetArrayElementAtIndex(0).FindPropertyRelative(nameStr).stringValue = "New Behavior";
                behaviors.GetArrayElementAtIndex(0).FindPropertyRelative("enabled").boolValue = true;
                self.pagination = behaviors.arraySize;
            }

            // Draw Variable Lists:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#FD6D40>Variables:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUI.indentLevel++;
            if (floatVariables.FindPropertyRelative(arraySizeStr).hasMultipleDifferentValues == false)
            {
                EditorGUILayout.PropertyField(floatVariables, GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));
            }
            else
            {
                EditorGUILayout.HelpBox("Multi-object editing Not supported. Make sure the selected objects have equal amounts of float variables.", MessageType.None);
            }

            SGO_EditorUtility.DrawHorizontalLine(false);

            if (booleanVariables.FindPropertyRelative(arraySizeStr).hasMultipleDifferentValues == false)
            {
                EditorGUILayout.PropertyField(booleanVariables, GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));
            }
            else
            {
                EditorGUILayout.HelpBox("Multi-object editing Not supported. Make sure the selected objects have equal amounts of boolean variables.", MessageType.None);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();

            // Draw Activation Events:
            EditorGUILayout.Space();
            self.showActivationEvents = SGO_EditorUtility.DrawFoldout(self.showActivationEvents, "<color=#FD6D40>Activation Events:</color>", true);
            if(self.showActivationEvents == true)
            {
                EditorGUILayout.PropertyField(onEnable);
                EditorGUILayout.PropertyField(onDisable);
            }

            if (expandedListOffset > 0)
            {
                expandedListOffset = 0;
            }
        }

        private void DrawInteractionInspector()
        {
            // Draw Raycast properties:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#B1FD59>Raycasts:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUILayout.PropertyField(layerMask);
            EditorGUILayout.PropertyField(triggerInteraction);
            EditorGUILayout.PropertyField(maxRayDistance);
            EditorGUILayout.PropertyField(rayCount);
            EditorGUILayout.PropertyField(raySpacing);
            EditorGUILayout.PropertyField(verticalScatter);
            EditorGUILayout.PropertyField(horizontalScatter);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Draw Physics properties:
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#B1FD59>Physics:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            rigid.objectReferenceValue = EditorGUILayout.ObjectField(new GUIContent("Rigidbody", "The Rigidbody on this GameObject."), rigid.objectReferenceValue, typeof(Rigidbody), true);
            col.objectReferenceValue = EditorGUILayout.ObjectField(new GUIContent("Collider", "A Collider on this GameObject."), col.objectReferenceValue, typeof(Collider), true);
            if (col.objectReferenceValue != null)
            {
                expandedListOffset += 15;
                EditorGUILayout.PropertyField(maxCollisions);
                if (ignoredColliders.FindPropertyRelative(arraySizeStr).hasMultipleDifferentValues == false)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(ignoredColliders, GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.HelpBox("Multi-object editing Not supported. Make sure the selected objects have equal amounts of ignoredColliders.", MessageType.None);
                }
                if (requiredTags.FindPropertyRelative(arraySizeStr).hasMultipleDifferentValues == false)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(requiredTags, GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.HelpBox("Multi-object editing Not supported. Make sure the selected objects have equal amounts of required tags.", MessageType.None);
                }
                EditorGUILayout.Space();
                if (self.col != null && self.col.isTrigger == true)
                {
                    EditorGUILayout.PropertyField(onTriggerStay, GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));
                }
                else
                {
                    EditorGUILayout.PropertyField(onCollisionStay, GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Draw Memory properties:
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#B1FD59>Memory:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUILayout.PropertyField(focusTarget);
            if (smartTargets.FindPropertyRelative(arraySizeStr).hasMultipleDifferentValues == false)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(smartTargets, GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Multi-object editing Not supported. Make sure the selected objects have equal amounts of targets.", MessageType.None);
            }
            EditorGUILayout.EndVertical();

            // Draw Debug properties:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#B1FD59>Debug:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            if(Application.isPlaying == false)
            {
                EditorGUILayout.PropertyField(raycastDebug);
            }


            if (Application.isPlaying == true)
            {
                EditorGUI.BeginDisabledGroup(true);
                if (self.hits != null)
                {
                    EditorGUILayout.LabelField("Raycast Hits: " + self.hits.Length);
                }
                else
                {
                    EditorGUILayout.LabelField("Raycast Hits: 0");
                }
                if (self.col != null)
                {
                    if (self.col.isTrigger == true)
                    {
                        if (self.lastCollision != null && self.lastCollision.collider != null && self.lastCollision.collider.gameObject != null)
                        {
                            EditorGUILayout.LabelField("Last Collision: " + self.lastCollision.collider.gameObject.name + " (Collider)");
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Last Collision: None (Collider)");
                        }
                    }
                    EditorGUILayout.PropertyField(lastContact);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(colliders, GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();

            if (expandedListOffset > 0)
            {
                expandedListOffset = 0;
            }
        }

        private void DrawMotionInspector()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#77C3E5>Motors:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUI.BeginChangeCheck();
            agent.objectReferenceValue = EditorGUILayout.ObjectField(new GUIContent("NavMeshAgent", "The NavMeshAgent determining motion."), agent.objectReferenceValue, typeof(NavMeshAgent), true);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying == true)
            {
                // If a NavMeshAgent is assigned while other tweens are playing stop the tweens.
                if (agent.objectReferenceValue != null)
                {
                    if (self.translating == true)
                    {
                        if (usePath.boolValue == true)
                        {
                            self.StopPathTranslation();
                        }
                        else
                        {
                            self.StopTranslation();
                        }
                    }
                    if (self.rotating == true)
                    {
                        self.StopRotation();
                    }
                    if (self.scaling == true)
                    {
                        self.StopScale();
                    }
                }
            }
            EditorGUILayout.PropertyField(animator);
            EditorGUILayout.EndVertical();

            EditorGUI.BeginDisabledGroup(agent.objectReferenceValue != null);
            // Draw Translation Properties:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#77C3E5>Translation:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUI.BeginDisabledGroup(Application.isPlaying == true && self.translating == true);
            EditorGUILayout.PropertyField(translationTarget);
            EditorGUI.EndDisabledGroup();
            SGO_EditorUtility.DrawAnimationCurve(ref translationCurve, new GUIContent("Translation Curve", "The interpolation curve where the horizontal axis is speed and the vertical axis is the duration of the translation. WrapMode can be set via the gear icon at the end of the curve."), SGO_EditorUtility.editorBlue, emptyRect);
            if (Application.isPlaying == true)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(self.translating == true);
                if (GUILayout.Button("Play", EditorStyles.miniButtonLeft))
                {
                    if (usePath.boolValue == true)
                    {
                        self.StartPathTranslation();
                    }
                    else
                    {
                        self.StartTranslation();
                    }
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(self.translating == false || self.translationPaused == true);
                if (GUILayout.Button("Pause", EditorStyles.miniButtonMid))
                {
                    self.PauseTranslation();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(self.translating == false || self.translationPaused == false);
                if (GUILayout.Button("Resume", EditorStyles.miniButtonMid))
                {
                    self.ResumeTranslation();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(self.translating == false);
                if (GUILayout.Button("Stop", EditorStyles.miniButtonRight))
                {
                    if (usePath.boolValue == true)
                    {
                        self.StopPathTranslation();
                    }
                    else
                    {
                        self.StopTranslation();
                    }
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
                if (self.translating == true)
                {
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 3), self.translationTime, string.Empty);
                }

            }
            EditorGUI.BeginDisabledGroup(useLocalSpace.boolValue == true || self.translating == true);
            EditorGUILayout.PropertyField(usePath);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            // Draw Rotation Properties:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#77C3E5>Billboard:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUILayout.PropertyField(lookAtTarget);
            if (lookAtTarget.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(horizontalLook);
                EditorGUILayout.PropertyField(directLook);
            }
            else
            {
                EditorGUILayout.LabelField("<color=#77C3E5>Rotation:</color>", SGO_EditorUtility.richBoldLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUI.BeginDisabledGroup(Application.isPlaying == true && self.rotating == true);
                EditorGUILayout.PropertyField(rotationTarget);
                // Apperently the propertyFields for quaternions don't wrap correctly, do it manually:
                if (Screen.width <= 346)
                {
                    GUILayout.Space(20);
                }
                EditorGUI.EndDisabledGroup();
                SGO_EditorUtility.DrawAnimationCurve(ref rotationCurve, new GUIContent("Rotation Curve", "The interpolation curve where the horizontal axis is speed and the vertical axis is the duration of the rotation. WrapMode can be set via the gear icon at the end of the curve."), SGO_EditorUtility.editorBlue, emptyRect);
                EditorGUILayout.PropertyField(spin);
                if (Application.isPlaying == true)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(self.rotating == true);
                    if (GUILayout.Button("Play", EditorStyles.miniButtonLeft))
                    {
                        self.StartRotation();
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.BeginDisabledGroup(self.rotating == false || self.rotationPaused == true);
                    if (GUILayout.Button("Pause", EditorStyles.miniButtonMid))
                    {
                        self.PauseRotation();
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(self.rotating == false || self.rotationPaused == false);
                    if (GUILayout.Button("Resume", EditorStyles.miniButtonMid))
                    {
                        self.ResumeRotation();
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.BeginDisabledGroup(self.rotating == false);
                    if (GUILayout.Button("Stop", EditorStyles.miniButtonRight))
                    {
                        self.StopRotation();
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();
                    if (self.rotating == true)
                    {
                        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 3), self.rotationTime, string.Empty);
                    }
                }
            }

            EditorGUILayout.EndVertical();

            // Draw Scale Properties:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#77C3E5>Scale:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUI.BeginDisabledGroup(Application.isPlaying == true && self.scaling == true);
            EditorGUILayout.PropertyField(scaleTarget);
            EditorGUI.EndDisabledGroup();
            SGO_EditorUtility.DrawAnimationCurve(ref scaleCurve, new GUIContent("Scale Curve", "The interpolation curve where the horizontal axis is speed and the vertical axis is the duration of the scale. WrapMode can be set via the gear icon at the end of the curve."), SGO_EditorUtility.editorBlue, emptyRect);
            EditorGUILayout.PropertyField(snappyScale);
            if (Application.isPlaying == true)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(self.scaling == true);
                if (GUILayout.Button("Play", EditorStyles.miniButtonLeft))
                {
                    self.StartScale();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(self.scaling == false || self.scalingPaused == true);
                if (GUILayout.Button("Pause", EditorStyles.miniButtonMid))
                {
                    self.PauseScale();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(self.scaling == false || self.scalingPaused == false);
                if (GUILayout.Button("Resume", EditorStyles.miniButtonMid))
                {
                    self.ResumeScale();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(self.scaling == false);
                if (GUILayout.Button("Stop", EditorStyles.miniButtonRight))
                {
                    self.StopScale();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
                if (self.scaling == true)
                {
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 3), self.scaleTime, string.Empty);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();

            // Draw Path Properties:
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<color=#77C3E5>Path:</color>", SGO_EditorUtility.richBoldLabel);
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUI.BeginDisabledGroup(self.translating == true && usePath.boolValue == true);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useLocalSpace);
            if (EditorGUI.EndChangeCheck())
            {
                if (usePath.boolValue == true)
                {
                    usePath.boolValue = false;
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(lookAtPath);
            SGO_EditorUtility.DrawAnimationCurve(ref pathCurve, new GUIContent("Path Curve", "The interpolation curve where the steeper the curve the shorter the duration of each step in the path. WrapMode can be set via the gear icon at the end of the curve."), SGO_EditorUtility.editorBlue, emptyRect);
            if (splines.FindPropertyRelative(arraySizeStr).hasMultipleDifferentValues == false)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(splines, GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));
                if (EditorGUI.EndChangeCheck())
                {
                    if (Application.isPlaying == true)
                    {
                        // TODO: this doesn't update in game view but does in scene view.
                        self.RecalculatePath();
                    }

                    if (splines.isExpanded == true)
                    {
                        expandedListOffset = 15;
                    }
                    else
                    {
                        expandedListOffset = 0;
                    }
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Multi-object editing Not supported. Make sure the selected objects have an equal amount of motion path vectors.", MessageType.None);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPagination()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("<color=#FD6D40>Behaviors:</color>", SGO_EditorUtility.richClippingBoldLabel, GUILayout.Width(65));
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent(string.Empty, EditorGUIUtility.IconContent("ScriptableObject Icon").image, "Convert behavior to/from ScriptableObject"), GUILayout.Width(25), GUILayout.Height(18)))
            {
                self.loadingSO = !self.loadingSO;
            }
            self.previousPage = self.pagination;
            if (GUILayout.Button(new GUIContent("←", "Back"), EditorStyles.miniButton, GUILayout.Width(23)))
            {
                self.pagination--;
            }
            self.pagination = EditorGUILayout.IntField(self.pagination, GUILayout.Width(25));
            EditorGUILayout.LabelField("/ " + behaviors.arraySize, EditorStyles.whiteLabel, GUILayout.Width(22));
            if (GUILayout.Button(new GUIContent("→", "Next"), EditorStyles.miniButtonLeft, GUILayout.Width(23)))
            {
                self.pagination++;
            }

            if (GUILayout.Button(new GUIContent("+", "Add Behavior"), EditorStyles.miniButtonRight, GUILayout.Width(22)))
            {
                self.renaming = false;
                behaviors.InsertArrayElementAtIndex(behaviors.arraySize);
                behaviors.GetArrayElementAtIndex(behaviors.arraySize - 1).FindPropertyRelative(nameStr).stringValue = "New Behaviour " + (behaviors.arraySize);
                behaviors.GetArrayElementAtIndex(behaviors.arraySize - 1).FindPropertyRelative(actionsStr).ClearArray();
                behaviors.GetArrayElementAtIndex(behaviors.arraySize - 1).FindPropertyRelative(conditionsStr).ClearArray();
                behaviors.GetArrayElementAtIndex(behaviors.arraySize - 1).FindPropertyRelative("weightThreshold").intValue = 0;
                behaviors.GetArrayElementAtIndex(behaviors.arraySize - 1).FindPropertyRelative("fallbackActions").ClearArray();
                behaviors.GetArrayElementAtIndex(behaviors.arraySize - 1).FindPropertyRelative("delaying").boolValue = false;
                behaviors.GetArrayElementAtIndex(behaviors.arraySize - 1).FindPropertyRelative("enabled").boolValue = true;
            }

            if (self.pagination > behaviors.arraySize)
            {
                self.pagination = behaviors.arraySize;
            }
            if (self.pagination < 1)
            {
                self.pagination = 1;
            }
            if (self.pagination != self.previousPage)
            {
                self.renaming = false;
            }
            string label = string.Empty;
            SerializedProperty enabled = behaviors.GetArrayElementAtIndex(self.pagination - 1).FindPropertyRelative("enabled");
            if (enabled.boolValue == false)
            {
                label = "Disabled";
            }
            else
            {
                label = "Enabled";
            }
            enabled.boolValue = EditorGUILayout.ToggleLeft(label, enabled.boolValue, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // Enable right-click copy/paste:
            EditorGUI.BeginProperty(GUILayoutUtility.GetLastRect(), GUIContent.none, behaviors);// behaviors.GetArrayElementAtIndex(self.pagination - 1)); // Alternatevly copy a single behavior.
            EditorGUI.EndProperty();

            // Debug current behavior evaluation state:
            // if (Application.isPlaying == true) { EditorGUILayout.LabelField("Evaluating: # " + self.currentBehavior, EditorStyles.centeredGreyMiniLabel); }
        }

        private void DrawBehaviour(SerializedProperty list, int page)
        {
            // Draw behavior options:
            SGO_EditorUtility.DrawHorizontalLine(true);
            if(self.loadingSO == true)
            {
                EditorGUILayout.BeginHorizontal();
                smartSO = (SmartBehaviorScriptableObject)EditorGUILayout.ObjectField(smartSO, typeof(SmartBehaviorScriptableObject), false);
                if(smartSO != null)
                {
                    if(GUILayout.Button(new GUIContent("Load", "Overrides the selected behavior with the ScriptableObject's behavior.")))
                    {
                        self.SetBehavior(page, smartSO.behavior);
                        Debug.LogFormat("|Smart GameObject|: Successfully loaded behavior: {0}", smartSO.behavior.name, self.gameObject);
                        self.loadingSO = false;
                    }
                    if (GUILayout.Button(new GUIContent("Save As", "Opens the save file dialog window to save the ScriptableObject.")))
                    {
                        string path = EditorUtility.SaveFilePanelInProject("Save ScriptableObject", smartSO.behavior.name + "_SO", "asset", "Please enter a file name to save the ScriptableObject as");
                        if (path.Length != 0)
                        {
                            SmartBehaviorScriptableObject temp = CreateInstance<SmartBehaviorScriptableObject>();
                            if(smartSO.behavior.delaying == true)
                            {
                                smartSO.behavior.delaying = false;
                            }
                            temp.behavior = smartSO.behavior;
                            smartSO = temp;
                            AssetDatabase.CreateAsset(smartSO, path);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            Debug.LogFormat("|Smart GameObject|: Successfully saved behavior at path {0}", path, self.gameObject);
                            self.loadingSO = false;
                        }
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (GUILayout.Button(new GUIContent("Create New", "Creates a new ScriptableObject from the currently selected behavior.")))
                    {
                        smartSO = CreateInstance<SmartBehaviorScriptableObject>();
                        smartSO.behavior = self.GetBehavior(page);
                    }
                }
                EditorGUILayout.EndHorizontal();
                SGO_EditorUtility.DrawHorizontalLine(true);
            }
            if (self.renaming == true)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(page).FindPropertyRelative(nameStr), SGO_EditorUtility.blankLabel);
                if (GUILayout.Button("Apply", EditorStyles.miniButton))
                {
                    self.renaming = false;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                if (self.renaming == false && GUILayout.Button(new GUIContent(list.GetArrayElementAtIndex(page).FindPropertyRelative(nameStr).stringValue, "Click to rename."), SGO_EditorUtility.centeredLabel, GUILayout.ExpandWidth(true)))
                {
                    self.renaming = true;
                }
                if (behaviors.arraySize > 1)
                {
                    if (GUILayout.Button(new GUIContent(string.Empty, "Remove Behaviour"), "OL Minus", GUILayout.Width(20)))
                    {
                        behaviors.DeleteArrayElementAtIndex(self.pagination - 1);
                        if (self.pagination > 1)
                        {
                            self.pagination--;
                            return;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUILayout.Space();

            // Draw Behavior properties:
            DrawBehaviorList(list.GetArrayElementAtIndex(page).FindPropertyRelative(conditionsStr), list.GetArrayElementAtIndex(page), ref self.showConditions, "<color=#FD6D40>Conditions</color>", true);
            EditorGUILayout.Space();
            SGO_EditorUtility.DrawHorizontalLine(false);
            EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(page).FindPropertyRelative("weightThreshold"));
            DrawBehaviorList(list.GetArrayElementAtIndex(page).FindPropertyRelative(actionsStr), list.GetArrayElementAtIndex(page), ref self.showActions, "<color=#FD6D40>Actions</color>", false);
            EditorGUILayout.Space();
            DrawBehaviorList(list.GetArrayElementAtIndex(page).FindPropertyRelative("fallbackActions"), list.GetArrayElementAtIndex(page), ref self.showFallbackActions, "<color=#FD6D40>Fallback Actions</color>", false);
        }

        /// <summary>
        /// Draws a list of the actions or conditions in a behavior.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="fold"></param>
        /// <param name="label"></param>
        /// <param name="isCondition"></param>
        private void DrawBehaviorList(SerializedProperty list, SerializedProperty behavior, ref bool fold, string label, bool isCondition)
        {
            fold = SGO_EditorUtility.DrawFoldout(fold, label, false);
            if (fold == true)
            {
                if (list.FindPropertyRelative(arraySizeStr).hasMultipleDifferentValues == false)
                {
                    if (list.arraySize > 0)
                    {
                        for (int i = 0; i < list.arraySize; i++)
                        {
                            SerializedProperty element = list.GetArrayElementAtIndex(i);
                            EditorGUILayout.BeginHorizontal();

                            if (isCondition == true)
                            {
                                GUI.backgroundColor = Color.white / 4f;
                                EditorGUILayout.BeginHorizontal(EditorStyles.textArea);
                                GUI.backgroundColor = Color.white;
                                DrawCondition(element);
                                if (GUILayout.Button(string.Empty, "OL Minus", GUILayout.Width(25)))
                                {
                                    list.MoveArrayElement(i, 0);
                                    list.DeleteArrayElementAtIndex(0);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            else
                            {
                                GUI.backgroundColor = Color.white / 4f;
                                EditorGUILayout.BeginVertical(EditorStyles.textArea);
                                GUI.backgroundColor = SGO_EditorUtility.darkGrey;
                                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                                GUI.backgroundColor = Color.white;

                                EditorGUILayout.LabelField((i + 1).ToString(), SGO_EditorUtility.centeredMiniLabel, GUILayout.Width(25), GUILayout.ExpandWidth(false));
                                if (list.arraySize > 1)
                                {
                                    int oldIndex = i;
                                    if (i == list.arraySize - 1)
                                    {
                                        if (GUILayout.Button("↑", EditorStyles.miniButton, GUILayout.Width(50)))
                                        {
                                            // Move action up.
                                            list.MoveArrayElement(i, oldIndex - 1);
                                        }
                                    }
                                    else if (i == 0)
                                    {
                                        if (GUILayout.Button("↓", EditorStyles.miniButton, GUILayout.Width(50)))
                                        {
                                            // Move action down.
                                            list.MoveArrayElement(i, oldIndex + 1);
                                        }
                                    }
                                    else if (i > 0 && i < list.arraySize)
                                    {
                                        if (GUILayout.Button("↓", EditorStyles.miniButtonLeft, GUILayout.Width(25)))
                                        {
                                            // Move action down.
                                            list.MoveArrayElement(i, oldIndex + 1);
                                        }
                                        if (GUILayout.Button("↑", EditorStyles.miniButtonRight, GUILayout.Width(25)))
                                        {
                                            // Move action up.
                                            list.MoveArrayElement(i, oldIndex - 1);
                                        }
                                    }
                                }
                                EditorGUILayout.LabelField(string.Empty, GUILayout.ExpandWidth(true));
                                if (GUILayout.Button(string.Empty, "OL Minus", GUILayout.Width(25)))
                                {
                                    list.MoveArrayElement(i, 0);
                                    list.DeleteArrayElementAtIndex(0);

                                    EditorGUILayout.EndHorizontal();
                                    EditorGUILayout.EndVertical();
                                }
                                else
                                {
                                    EditorGUILayout.EndHorizontal();
                                    DrawAction(element);
                                    EditorGUILayout.EndVertical();
                                }
                            }

                            EditorGUILayout.EndHorizontal();
                            // Enable condition/action list copy/paste.
                            if (i < list.arraySize)
                            {
                                EditorGUI.BeginProperty(GUILayoutUtility.GetLastRect(), GUIContent.none, list.GetArrayElementAtIndex(i));
                                EditorGUI.EndProperty();
                            }
                            GUILayout.Space(3);
                        }
                    }

                    EditorGUILayout.Space();
                    if (isCondition == true)
                    {
                        DrawConditionCreationMenu(list);
                    }
                    else
                    {
                        DrawActionCreationMenu(list, behavior);
                    }
                }
                else
                {
                    if (isCondition == true)
                    {
                        EditorGUILayout.HelpBox("Multi-object editing Not supported. Make sure the selected objects have an equal amount of conditions on the behavior.", MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Multi-object editing Not supported. Make sure the selected objects have an equal amount of actions on the behavior.", MessageType.None);
                    }

                }
            }
        }

        private void DrawCondition(SerializedProperty condition)
        {
            EditorGUILayout.BeginVertical();

            SerializedProperty componentFloat = condition.FindPropertyRelative("componentFloat");
            SerializedProperty customValue = condition.FindPropertyRelative("customValue");
            SerializedProperty variableName = condition.FindPropertyRelative("variableName");
            SerializedProperty variableID = condition.FindPropertyRelative("variableID");
            SerializedProperty numericalComparison = condition.FindPropertyRelative("numericalComparison");

            SerializedProperty componentFloat2 = condition.FindPropertyRelative("componentFloat2");
            SerializedProperty customValue2 = condition.FindPropertyRelative("customValue2");
            SerializedProperty variableName2 = condition.FindPropertyRelative("variableName2");
            SerializedProperty variableID2 = condition.FindPropertyRelative("variableID2");

            SerializedProperty componentBoolean = condition.FindPropertyRelative("componentBoolean");
            SerializedProperty componentBoolean2 = condition.FindPropertyRelative("componentBoolean2");
            SerializedProperty booleanComparison = condition.FindPropertyRelative("booleanComparison");
            SerializedProperty conditionType = condition.FindPropertyRelative("conditionType");

            switch (conditionType.enumValueIndex)
            {
                case 0:

                    // Numerical condition:
                    EditorGUILayout.BeginHorizontal();
                    componentFloat.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentFloat.intValue, readableComponentFloats);
                    if (componentFloat.intValue == 0)
                    {
                        customValue.floatValue = EditorGUILayout.FloatField(customValue.floatValue);
                    }
                    else if (componentFloat.intValue == 1 || componentFloat.intValue == 54 || componentFloat.intValue == 55 || componentFloat.intValue == 85 || componentFloat.intValue == 130 || componentFloat.intValue == 131)
                    {
                        variableName.stringValue = EditorGUILayout.TextField(variableName.stringValue);
                        variableID.intValue = variableName.stringValue.GetHashCode();
                    }
                    EditorGUILayout.EndHorizontal();

                    numericalComparison.enumValueIndex = (int)(NumericalComparisons)EditorGUILayout.EnumPopup((NumericalComparisons)numericalComparison.enumValueIndex);

                    EditorGUILayout.BeginHorizontal();
                    componentFloat2.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentFloat2.intValue, readableComponentFloats);
                    if (componentFloat2.intValue == 0)
                    {
                        customValue2.floatValue = EditorGUILayout.FloatField(customValue2.floatValue);
                    }
                    else if (componentFloat2.intValue == 1 || componentFloat2.intValue == 54 || componentFloat2.intValue == 55 || componentFloat2.intValue == 85 || componentFloat2.intValue == 130 || componentFloat2.intValue == 131)
                    {
                        variableName2.stringValue = EditorGUILayout.TextField(variableName2.stringValue);
                        variableID2.intValue = variableName2.stringValue.GetHashCode();
                    }
                    EditorGUILayout.EndHorizontal();

                    break;

                case 1:

                    // Boolean condition:
                    EditorGUILayout.BeginHorizontal();
                    componentBoolean.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentBoolean.intValue, readableComponentBooleans);
                    if (componentBoolean.intValue == 2 || (componentBoolean.intValue > 105 && componentBoolean.intValue < 110))
                    {
                        variableName.stringValue = EditorGUILayout.TextField(variableName.stringValue);
                        variableID.intValue = variableName.stringValue.GetHashCode();
                    }
                    EditorGUILayout.EndHorizontal();

                    booleanComparison.enumValueIndex = (int)(BooleanComparisons)EditorGUILayout.EnumPopup((BooleanComparisons)booleanComparison.enumValueIndex);

                    EditorGUILayout.BeginHorizontal();
                    componentBoolean2.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentBoolean2.intValue, readableComponentBooleans);
                    if (componentBoolean2.intValue == 2 || (componentBoolean.intValue > 105 && componentBoolean.intValue < 110))
                    {
                        variableName2.stringValue = EditorGUILayout.TextField(variableName2.stringValue);
                        variableID2.intValue = variableName2.stringValue.GetHashCode();
                    }
                    EditorGUILayout.EndHorizontal();

                    break;
            }
            // If playing debug condition values green = true, red = false.
            if (Application.isPlaying == true)
            {
                // Create temporary condition to evaluate (Might be able to just use SerializedProperty.boxingValue in 2022.x)?
                tempCondition.conditionType = (ConditionTypes)conditionType.enumValueIndex;
                tempCondition.componentFloat = componentFloat.intValue;
                tempCondition.customValue = customValue.floatValue;
                tempCondition.numericalComparison = (NumericalComparisons)numericalComparison.enumValueIndex;
                tempCondition.componentFloat2 = componentFloat2.intValue;
                tempCondition.customValue2 = customValue2.floatValue;

                tempCondition.componentBoolean = componentBoolean.intValue;
                tempCondition.booleanComparison = (BooleanComparisons)booleanComparison.enumValueIndex;
                tempCondition.componentBoolean2 = componentBoolean2.intValue;

                tempCondition.variableName = variableName.stringValue;
                tempCondition.variableName2 = variableName2.stringValue;
                tempCondition.variableID = variableID.intValue;
                tempCondition.variableID2 = variableID2.intValue;

                if (self.EvaluateCondition(tempCondition) == true)
                {
                    GUI.backgroundColor = Color.green;
                }
                else
                {
                    GUI.backgroundColor = Color.red;
                }
                EditorGUILayout.PropertyField(condition.FindPropertyRelative("weight"));
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.PropertyField(condition.FindPropertyRelative("weight"));
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAction(SerializedProperty action)
        {
            //Draw action:
            EditorGUILayout.BeginVertical();
            SerializedProperty componentFloat = action.FindPropertyRelative("componentFloat");
            SerializedProperty componentBoolean = action.FindPropertyRelative("componentBoolean");
            SerializedProperty variableName = action.FindPropertyRelative("variableName");
            SerializedProperty variableID = action.FindPropertyRelative("variableID");
            SerializedProperty customValue = action.FindPropertyRelative("customValue");
            SerializedProperty variableID2 = action.FindPropertyRelative("variableID2");
            SerializedProperty variableName2 = action.FindPropertyRelative("variableName2");
            switch (action.FindPropertyRelative("actionType").enumValueIndex)
            {
                case 0:

                    // Draw smart action:
                    SerializedProperty smartAction = action.FindPropertyRelative("actionIndex");
                    smartAction.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(smartAction.intValue, actions);
                    if (smartAction.intValue == 59 || smartAction.intValue == 85 || smartAction.intValue == 115 || (smartAction.intValue > 54 && smartAction.intValue < 57))
                    {
                        customValue.floatValue = EditorGUILayout.FloatField(new GUIContent("Value","This value may represent speed, distance or some other number related to the selected action."), customValue.floatValue);
                    }
                    if(smartAction.intValue == 7 || smartAction.intValue == 17 || smartAction.intValue == 24 || (smartAction.intValue > 86 && smartAction.intValue < 96) || (smartAction.intValue > 104 && smartAction.intValue < 108))
                    {
                        action.FindPropertyRelative("customVector").vector3Value = EditorGUILayout.Vector3Field(new GUIContent("Vector3", "A Vector3 to pass to the smart action."), action.FindPropertyRelative("customVector").vector3Value);
                    }
                    if(smartAction.intValue == 83)
                    {
                        componentFloat.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentFloat.intValue, readableComponentFloats);
                        variableName.stringValue = EditorGUILayout.TextField(variableName.stringValue);
                        variableID.intValue = variableName.stringValue.GetHashCode();
                    }
                    if(smartAction.intValue == 84)
                    {
                        componentBoolean.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentBoolean.intValue, readableComponentBooleans);
                        variableName.stringValue = EditorGUILayout.TextField(variableName.stringValue);
                        variableID.intValue = variableName.stringValue.GetHashCode();
                    }

                    if(smartAction.intValue == 44 || smartAction.intValue == 45 || (smartAction.intValue > 46 && smartAction.intValue < 55)  || smartAction.intValue == 64 || smartAction.intValue == 65 || smartAction.intValue == 73 || smartAction.intValue == 74 || (smartAction.intValue > 78 && smartAction.intValue < 82))
                    {
                        variableName.stringValue = EditorGUILayout.TextField(variableName.stringValue);
                    }
                    if (smartAction.intValue > 95 && smartAction.intValue < 100)
                    {
                        action.FindPropertyRelative("curve").animationCurveValue = EditorGUILayout.CurveField(action.FindPropertyRelative("curve").animationCurveValue);
                    }
                    // enable/disable behavior:
                    if(smartAction.intValue > 99 && smartAction.intValue < 103)
                    {
                        variableID.intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Behavior #", "Should be the behavior page number minus 1."), variableID.intValue), 0, behaviors.arraySize - 1);
                    }

                    break;

                case 1:

                    // Draw numerical action:
                    SerializedProperty componentFloat2 = action.FindPropertyRelative("componentFloat2");
                    EditorGUILayout.BeginHorizontal();
                    componentFloat.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentFloat.intValue, writableComponentFloats);
                    if (componentFloat.intValue == 1 || componentFloat.intValue == 54 || componentFloat.intValue == 55 || componentFloat.intValue == 85 || componentFloat.intValue == 130 || componentFloat.intValue == 131)
                    {
                        variableName.stringValue = EditorGUILayout.TextField(variableName.stringValue);
                        variableID.intValue = variableName.stringValue.GetHashCode();
                    }
                    EditorGUILayout.EndHorizontal();

                    action.FindPropertyRelative("numericalOperator").enumValueIndex = (int)(NumericalOperators)EditorGUILayout.EnumPopup((NumericalOperators)action.FindPropertyRelative("numericalOperator").enumValueIndex);

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.BeginHorizontal();
                    componentFloat2.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentFloat2.intValue, readableComponentFloats);
                    if (componentFloat2.intValue == 0)
                    {
                        action.FindPropertyRelative("customValue2").floatValue = EditorGUILayout.FloatField(action.FindPropertyRelative("customValue2").floatValue);
                    }
                    else if (componentFloat2.intValue == 1 || componentFloat2.intValue == 54 || componentFloat2.intValue == 55 || componentFloat2.intValue == 85 || componentFloat2.intValue == 130 || componentFloat2.intValue == 131)
                    {
                        variableName2.stringValue = EditorGUILayout.TextField(variableName2.stringValue);
                        variableID2.intValue = variableName2.stringValue.GetHashCode();
                    }
                    EditorGUILayout.EndHorizontal();
                    if (componentFloat2.intValue == 0 && action.FindPropertyRelative("numericalOperator").enumValueIndex == 3 && action.FindPropertyRelative("customValue2").floatValue == 0)
                    {
                        GUI.backgroundColor = Color.red;
                        EditorGUILayout.HelpBox("Cannot divide by zero.", MessageType.None);
                        GUI.backgroundColor = Color.white;
                    }
                    EditorGUILayout.EndVertical();

                    break;

                case 2:

                    // Draw boolean action:
                    SerializedProperty componentBoolean2 = action.FindPropertyRelative("componentBoolean2");
                    EditorGUILayout.BeginHorizontal();
                    componentBoolean.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentBoolean.intValue, writableComponentBooleans);
                    if (componentBoolean.intValue == 2 || componentBoolean.intValue == 5 || componentBoolean.intValue == 37 || componentBoolean.intValue == 45 || componentBoolean.intValue == 48 || componentBoolean.intValue == 52 || componentBoolean.intValue == 84 || componentBoolean.intValue == 92)
                    {
                        variableName.stringValue = EditorGUILayout.TextField(variableName.stringValue);
                        variableID.intValue = variableName.stringValue.GetHashCode();
                    }else if(componentBoolean.intValue == 43)
                    {
                        variableID.intValue = EditorGUILayout.IntField(variableID.intValue);
                    }

                    EditorGUILayout.EndHorizontal();

                    action.FindPropertyRelative("booleanComparison").enumValueIndex = (int)(BooleanComparisons)EditorGUILayout.EnumPopup((BooleanComparisons)action.FindPropertyRelative("booleanComparison").enumValueIndex);

                    EditorGUILayout.BeginHorizontal();
                    componentBoolean2.intValue = SGO_EditorUtility.DrawLabellessCompactPopup(componentBoolean2.intValue, readableComponentBooleans);
                    if (componentBoolean2.intValue == 2 || componentBoolean2.intValue == 5 || componentBoolean2.intValue == 37 || componentBoolean2.intValue == 45 || componentBoolean2.intValue == 48 || componentBoolean2.intValue == 52 || componentBoolean2.intValue == 84 || componentBoolean2.intValue == 92)
                    {
                        variableName2.stringValue = EditorGUILayout.TextField(variableName2.stringValue);
                        variableID2.intValue = variableName2.stringValue.GetHashCode();
                    }else if(componentBoolean2.intValue == 43)
                    {
                        variableID2.intValue = EditorGUILayout.IntField(variableID2.intValue);
                    }
                    EditorGUILayout.EndHorizontal();

                    break;

                case 3:

                    expandedListOffset = 15;
                    EditorGUILayout.PropertyField(action.FindPropertyRelative("unityEvent"), GUILayout.Width(Screen.width - (listOffset + expandedListOffset)));

                    break;

                case 4:

                    EditorGUILayout.LabelField("Delay Next Action", EditorStyles.boldLabel);

                    customValue.floatValue = Mathf.Round(Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Time", "The time in seconds to delay the next action."), customValue.floatValue), 0, 3600) * 10.0f) * 0.1f;
                    if (behaviors.GetArrayElementAtIndex(self.pagination - 1).FindPropertyRelative("delaying").boolValue == true)
                    {
                        // If a behavior was copy/pasted while delaying a SmartGO may start in an infinite delay loop. Prevent this:
                        if (Application.isPlaying == false)
                        {
                            behaviors.GetArrayElementAtIndex(self.pagination - 1).FindPropertyRelative("delaying").boolValue = false;
                        }
                        // EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight / 2), Mathf.InverseLerp(0, customValue.floatValue, Time.deltaTime) / 1, string.Empty);
                    }
                    else
                    {
                        // EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight / 2), 0, string.Empty);
                    }
                    break;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionCreationMenu(SerializedProperty list, SerializedProperty behavior)
        {
            if (list != null)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                self.actionType = (int)(ActionTypes)EditorGUILayout.EnumPopup("Add Action:", (ActionTypes)self.actionType);
                if (GUILayout.Button(string.Empty, "OL Plus", GUILayout.Width(20)))
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("actionType").enumValueIndex = self.actionType;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("actionIndex").intValue = 0;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("curve").animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("componentFloat").intValue = 1;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("componentBoolean").intValue = 2;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("variableName").stringValue = string.Empty;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("customValue").floatValue = 0f;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("customVector").vector3Value = Vector3.zero;
                    if(self.actionType == 4)
                    {
                        behavior.FindPropertyRelative("containsDelay").boolValue = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawConditionCreationMenu(SerializedProperty list)
        {
            if (list != null)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                self.conditionType = (int)(ConditionTypes)EditorGUILayout.EnumPopup("Add Condition:", (ConditionTypes)self.conditionType);
                if (GUILayout.Button(string.Empty, "OL Plus", GUILayout.Width(20)))
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("conditionType").enumValueIndex = self.conditionType;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("variableName").stringValue = string.Empty;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("componentFloat").intValue = 1;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("componentBoolean").intValue = 0;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("componentFloat2").intValue = 0;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("componentBoolean2").intValue = 0;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("customValue").floatValue = 0f;
                    list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("customValue2").floatValue = 0f;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawLogicHelp()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Space(7);
            SGO_EditorUtility.DrawHorizontalLine(true);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(EditorGUIUtility.IconContent("_help"), GUIStyle.none, GUILayout.Width(20))) { self.showLogicHelp = !self.showLogicHelp; }
            EditorGUILayout.EndHorizontal();
            if (self.showLogicHelp == true)
            {
                self.showActions = false;
                self.showConditions = false;
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(EditorGUIUtility.IconContent("_help@2x"), GUIStyle.none);
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Behaviors:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Behaviors organize conditional logic & actions. They are all evaluated during the same frame as long as a behavior does not have a delay action in that case that behavior may be skipped. ", SGO_EditorUtility.wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("Conditions:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("A conditional statement that adds its weight to a total when it returns a true statement. Note: If a Smart GameObject does not have a component referenced in a condition the condition float/boolean may always return 0 or false.", SGO_EditorUtility.wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("Actions:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Actions execute when the total weight of evaluated conditions exceeds the weight threshold. Fallback actions execute when that doesn't happen.", SGO_EditorUtility.wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("Custom Variables:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Also known as smart variables these are floats i.e. 1.2345678 or booleans i.e. true/false values that can be accessed by other Smart GameObjects if they also have a variable of the provided name defined.", SGO_EditorUtility.wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("Targets:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Targets are other Smart GameObjects this one is aware of. Targets must be gathered from Physics trigger collisions, rigidbody contacts or raycasts. A target can be focused on in order to access its variables & actions.", SGO_EditorUtility.wrappedMiniLabel);
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawInteractionHelp()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Space(7);
            SGO_EditorUtility.DrawHorizontalLine(true);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(EditorGUIUtility.IconContent("_help"), GUIStyle.none, GUILayout.Width(20))) { self.showInteractionHelp = !self.showInteractionHelp; }
            EditorGUILayout.EndHorizontal();

            if (self.showInteractionHelp == true)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(EditorGUIUtility.IconContent("_help@2x"), GUIStyle.none);
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Raycasts:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Settings for the next time this Smart GameObject casts a ray (always originating from the Transform's forward direction).", SGO_EditorUtility.wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("Physics:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("These are the settings used if there is a collider and/or rigidbody on this Smart GameObject.", SGO_EditorUtility.wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("Memory:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Memory contains Smart GameObjects that this one is aware of, targets are found using physics or raycasts then need to be added to the target's list via the logic tab.", SGO_EditorUtility.wrappedMiniLabel);
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawMotionHelp()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Space(7);
            SGO_EditorUtility.DrawHorizontalLine(true);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(EditorGUIUtility.IconContent("_help"), GUIStyle.none, GUILayout.Width(20))) { self.showMotionHelp = !self.showMotionHelp; }
            EditorGUILayout.EndHorizontal();

            if (self.showMotionHelp == true)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(EditorGUIUtility.IconContent("_help@2x"), GUIStyle.none);
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Motors:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Assign a NavMeshAgent and/or an Animator to enable use of external motion options. If a NavMeshAgent is assigned then tweens will be disabled since motion will be governed by the agent.", SGO_EditorUtility.wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("Translation, Rotation & Scale:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("These are parameters for interpolation (tweens). These options are unavialable if an NavMeshAgent is driving motion, or in the case of rotation if there is a look at target assigned for billboarding since motion will be overriden by those motors. Tween updates are applied after position changes by other actions.", SGO_EditorUtility.wrappedMiniLabel);
                SGO_EditorUtility.DrawHorizontalLine(false);
                EditorGUILayout.LabelField("Paths:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Paths can be used by interpolation through logic actions or via AI pathfinding actions. Local space paths may only be followed by other Smart GameObjects.", SGO_EditorUtility.wrappedMiniLabel);
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        [MenuItem("GameObject/Kitbashery/Smart GameObject")]
        static void CreateSmartGameObject()
        {
            if (FindObjectOfType<SmartManager>() == null)
            {
                GameObject instance = new GameObject("Smart Manager").AddComponent<SmartManager>().gameObject;
                Debug.Log("A SmartManager was missing, creating one...", instance);
            }
            Selection.activeGameObject = new GameObject("Smart GameObject").AddComponent<SmartGameObject>().gameObject;
        }

        #endregion
    }
}