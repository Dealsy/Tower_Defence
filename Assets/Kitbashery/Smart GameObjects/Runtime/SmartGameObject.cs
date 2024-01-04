using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

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
    /// Managed GameObject that can evaluate custom conditions and invoke events.
    /// </summary>
    [DisallowMultipleComponent]
    [HelpURL("https://kitbashery.com/docs/smart-gameobjects/smart-gameobject.html")]
    [AddComponentMenu("Kitbashery/Gameplay/Smart GameObject")]
    public class SmartGameObject : MonoBehaviour
    {
        #region Properties:
        /// <summary>
        /// Cache to get around the GetComponent call that's under the hood.
        /// </summary>
        public Transform myTransform;
        private Vector3 initialPos;
        private Quaternion initialRot;
        private Vector3 initialScale;
        /// <summary>
        /// The index of the current behavior being evaluated.
        /// </summary>
        public int currentBehavior { get; private set; } = 0;
        /// <summary>
        /// The next behavior to evaluate.
        /// </summary>
        private SmartBehavior nextBehavior;

        [SerializeField]
        private List<SmartBehavior> behaviors = new List<SmartBehavior>();

        // Temp variables for when conditions are evaluated or actions invoked.
        [HideInInspector]
        private float fx, fy;
        [HideInInspector]
        private bool bx, by;
        private Vector3 tempVector;
        private float tempFloat;
        /// <summary>
        /// The current condition weight of the current behavior being evaluated.
        /// </summary>
        [SerializeField]
        private int currentWeight = 0;
        private int behaviorCount = 0;

        public int pathProgress { get; private set; } = 0;

        /// <summary>
        /// Constant value of the golden ratio.
        /// </summary>
        const float phi = 1.61803398f;

        #region Custom Variable Properties:

        [Header("Custom Variables:")]
        public Dictionary<int, float> floatVariableLookup = new Dictionary<int, float>();
        public Dictionary<int, bool> booleanVariableLookup = new Dictionary<int, bool>();
        public List<FloatVariable> floatVariables = new List<FloatVariable>();
        public List<BooleanVariable> booleanVariables = new List<BooleanVariable>();
        private BooleanVariable tempBoolVar;
        private FloatVariable tempFloatVar;

        #endregion

        #region Interaction Properties:

        [Tooltip("The SmartGameObject that is being focused on.")]
        public SmartGameObject focusTarget;
        [Tooltip("Other smart GameObjects this one is aware of.")]
        public List<SmartGameObject> targets;
        /// <summary>
        /// The last SmartGameObject that was attempted to be found/registered as a target.
        /// </summary>
        public SmartGameObject lastTargeted;
        public SmartGameObject nearestTarget;
        public SmartGameObject farthestTarget;

        public Rigidbody rigid;
        public Collider col;
        [Space]
        [Tooltip("If the SmartGameObject has a collider marked as trigger this should be true.")]
        public bool isTrigger = true;
        [Tooltip("The max amount of collisions to happen during a frame before collisions stop being detected.")]
        [Min(1)]
        public int maxCollisions = 50;

        [Tooltip("Tags a collider must have in order for it to be detected.")]
        public List<string> requiredTags = new List<string>();

        /// <summary>
        /// Colliders to ignore when the script is initialized.
        /// </summary>
        [Tooltip("Colliders to ignore (Only supported in Unity 5.5.0b3 and later).")]
        public List<Collider> ignoredColliders = new List<Collider>();


        [Tooltip("All colliders currently interacting with the interaction collider.")]
        public List<Collider> colliders = new List<Collider>();

        [Tooltip("The collider that last interacted with the interaction collider.")]
        public Collider lastContact;

        /// <summary>
        /// The last collision that occured.
        /// </summary>
        public Collision lastCollision;

        public UnityEvent onTriggerStay;
        public UnityEvent onCollisionStay;
        public UnityEvent onEnable;
        public UnityEvent onDisable;

        private Ray tempRay = new Ray(Vector3.zero, Vector3.forward);

        public RaycastHit[] hits = new RaycastHit[0];

        [Min(1), Tooltip("The amount of rays to cast.")]
        public int rayCount = 1;

        [Min(0.001f), Tooltip("The length of a raycast.")]
        public float maxRayDistance = 1000;
        [Tooltip("Spacing for grid, line and circle raycast types & the size for sphere, capsule and box casts.")]
        public float raySpacing = 1f;
        [Range(0, 360), Tooltip("The vertical range when invoking the scatter raycast action.")]
        public float verticalScatter = 45;
        [Range(0, 360), Tooltip("The horizontal range when invoking the scatter raycast action.")]
        public float horizontalScatter = 45;
        [Tooltip("The layer mask that the next raycast will detect objects in.")]
        public LayerMask layerMask = 1;
        [Tooltip("How raycasts and colliders interact with colliders marked as triggers.")]
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        #endregion

        #region Motion Properties:

        [Tooltip("The NavMeshAgent determining motion.")]
        public NavMeshAgent agent;

        [Tooltip("The Animator determining motion.")]
        public Animator animator;

        public List<Spline> splines = new List<Spline>();
        public List<Vector3> path = new List<Vector3>();
        private List<Vector3> tempPath = new List<Vector3>();
        [Tooltip("If enabled the path will follow the GameObject in local space (this GameObject won't be allowed to follow its own path).")]
        public bool useLocalSpace = false;
        public bool useFocusTargetPath = false;
        [Tooltip("If enabled the SmartGameObject will translate along its path of splines (disabled for its own local space paths).")]
        public bool usePath = false;
        [Tooltip("If enabled a SmartGameObject will look at the next path segment while following a path.")]
        public bool lookAtPath = false;
        public AnimationCurve pathCurve = AnimationCurve.Linear(0, 1, 0.1f, 1);
        private int currentPathPoint = 0;
        /// <summary>
        /// Is the path being moved along in reverse? (wrap mode ping-pong)
        /// </summary>
        private bool pathReversed = false;

        public bool translating { get; private set; } = false;
        public bool translationPaused { get; private set; } = false;
        public AnimationCurve translationCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [Tooltip("The position to translate to during a translation tween. (world space)")]
        public Vector3 translationTarget;
        [Tooltip("The initial position to translate to when a translation tween starts. (world space)")]
        private Vector3 initialTranslationTarget;
        private Vector3 previousPosition;
        /// <summary>
        /// The next position to be determined by a translation tween (useful for networking/physics).
        /// </summary>
        [HideInInspector]
        public Vector3 nextPosition { get; private set; }

        public bool rotating { get; private set; } = false;
        public bool rotationPaused { get; private set; } = false;
        public AnimationCurve rotationCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [Tooltip("The target to rotate to during a rotation tween.")]
        public Quaternion rotationTarget = Quaternion.identity;
        [Tooltip("The initial target to rotate to when a rotation tween starts.")]
        private Quaternion initialRotationTarget;
        private Quaternion previousRotation;
        /// <summary>
        /// The next rotation to be determined by a rotation tween (useful for networking/physics).
        /// </summary>
        [HideInInspector]
        public Quaternion nextRotation { get; private set; }
        [Tooltip("If enabled rotation will spin around the target rotation.")]
        public bool spin = false;

        public bool scaling { get; private set; } = false;
        public bool scalingPaused { get; private set; } = false;
        public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [Tooltip("The target to scale to during a scale tween.")]
        public Vector3 scaleTarget = Vector3.one;
        [Tooltip("The initial target to scale to when a scale tween starts.")]
        private Vector3 initialScaleTarget;
        private Vector3 previousScale;
        /// <summary>
        /// The next scale to be determined by a scale tween (useful for networking/physics).
        /// </summary>
        [HideInInspector]
        public Vector3 nextScale { get; private set; }
        [Tooltip("If enabled scale will be rounded so the scale snaps between increments.")]
        public bool snappyScale = false;
        /// <summary>
        /// The current progress of <see cref="translationCurve"/>'s evaluation.
        /// </summary>
        public float translationTime { get; private set; } = 0;
        /// <summary>
        /// The current progress of <see cref="rotationCurve"/>'s evaluation.
        /// </summary>
        public float rotationTime { get; private set; } = 0;
        /// <summary>
        /// The current progress of <see cref="scaleCurve"/>'s evaluation.
        /// </summary>
        public float scaleTime { get; private set; } = 0;

        // Billboard Properties:
        [Tooltip("The transform to rotate toward (overrides other rotation tweens).")]
        public Transform lookAtTarget;
        private Vector3 lookAtPos;
        [Tooltip("If enabled the billboard be constrained to only looking horizontally.")]
        public bool horizontalLook = false;
        [Tooltip("If enabled the billboard look directly at the target.")]
        public bool directLook = true;

        private Vector3 currentPosition;
        private Quaternion currentRotation;

        #endregion

        public AudioSource audioSrc;

        /// <summary>
        /// The last GameObject that was spawned.
        /// </summary>
        public GameObject lastSpawned;

        #endregion

        #region Initialization & Updates:

#if UNITY_EDITOR

        [Tooltip("Visualize approximations of raycasts based on the current raycast settings.")]
        public RaycastTypes raycastDebug = RaycastTypes.None;
        private Mesh debugCapsule;
        /// <summary>
        /// Internal editor-only integer for tracking the selected toolbar mode in the editor.
        /// </summary>
        public int toolbarInt = 0;
        /// <summary>
        /// Internal editor-only integer for tracking the previously inspected behavior in the editor pagination.
        /// </summary>
        public int previousPage = 0;
        /// <summary>
        ///  Internal editor-only integer for tracking the currently inspected behavior in the editor pagination.
        /// </summary>
        public int pagination = 1;
        /// <summary>
        /// Internal editor-only values for tracking the toggle states of the custom inspector menu.
        /// </summary>
        public bool showConditions = true, showActions, showFallbackActions, showLogicHelp, showInteractionHelp, showMotionHelp, showCopyright, showActivationEvents, renaming = false, loadingSO = false;
        /// <summary>
        /// Internal editor-only values for tracking the enum states of the custom inspector.
        /// </summary>
       public int conditionType = 0, actionType = 0;

        private void OnDrawGizmosSelected()
        {              
            if (myTransform == null)
            {
                myTransform = transform;
            }

            // Draw Splines:
            if (splines.Count > 0)
            {
                path.Clear();

                Gizmos.color = Color.cyan;

                foreach (Spline spline in splines)
                {
                    if (useLocalSpace == true)
                    {
                        Gizmos.DrawSphere(myTransform.localPosition + spline.startPoint, 0.15f);
                        Gizmos.DrawWireSphere(myTransform.localPosition + spline.controlPoint1, 0.15f);
                        Gizmos.DrawWireSphere(myTransform.localPosition + spline.controlPoint2, 0.15f);
                        Gizmos.DrawSphere(myTransform.localPosition + spline.endPoint, 0.15f);
                    }
                    else
                    {
                        Gizmos.DrawSphere(spline.startPoint, 0.15f);
                        Gizmos.DrawWireSphere(spline.controlPoint1, 0.15f);
                        Gizmos.DrawWireSphere(spline.controlPoint2, 0.15f);
                        Gizmos.DrawSphere(spline.endPoint, 0.15f);
                    }

                    CreateSplinePath(spline);
                }

                for (int i = 1; i < path.Count; i++)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(path[i - 1], path[i]);
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(path[i - 1] + Vector3.left, path[i] + Vector3.left);
                    Gizmos.DrawLine(path[i - 1] + Vector3.left, path[i - 1] + Vector3.right);
                    Gizmos.DrawLine(path[i - 1] + Vector3.right, path[i] + Vector3.right);
                    if(i == path.Count - 1)
                    {
                        Gizmos.DrawLine(path[i] + Vector3.left, path[i] + Vector3.right);
                    }
                }
            }

            Gizmos.color = Color.green;
            switch (raycastDebug)
            {
                case RaycastTypes.None:

                    break;

                case RaycastTypes.Raycast:

                    Gizmos.DrawRay(myTransform.position, myTransform.forward * maxRayDistance);

                    break;

                case RaycastTypes.RaycastRow:

                    for (int i = 0; i < rayCount; i++)
                    {
                        Gizmos.DrawRay(myTransform.position + ((i - (rayCount - 1) / 2f) * raySpacing * myTransform.right), myTransform.forward * maxRayDistance);
                    }

                    break;

                case RaycastTypes.ScatterCast:

                    for (int i = 0; i < rayCount; i++)
                    {
                        Gizmos.DrawRay(myTransform.position, (Quaternion.Euler(UnityEngine.Random.Range(-verticalScatter, verticalScatter), UnityEngine.Random.Range(-horizontalScatter, horizontalScatter), 0) * myTransform.forward) * maxRayDistance);
                    }

                    break;

                case RaycastTypes.SphereCast:

                    Gizmos.DrawWireSphere(myTransform.position, raySpacing / 2);

                    break;

                case RaycastTypes.GridCast:

                    int gridSize = (int)Mathf.Sqrt(rayCount);
                    Vector3[] rayOrigins = new Vector3[gridSize * gridSize];

                    for (int i = 0; i < gridSize; i++)
                    {
                        for (int j = 0; j < gridSize; j++)
                        {
                            rayOrigins[i * gridSize + j] = myTransform.position + new Vector3((i - (gridSize - 1) / 2f) * raySpacing, (j - (gridSize - 1) / 2f) * raySpacing, 0);
                        }
                    }

                    for (int i = 0; i < rayOrigins.Length; i++)
                    {
                        Gizmos.DrawRay(rayOrigins[i], myTransform.forward * maxRayDistance);
                    }

                    break;

                case RaycastTypes.FanCast:

                    for (int i = 0; i < rayCount; i++)
                    {
                        Gizmos.DrawRay(myTransform.position, (Quaternion.AngleAxis(Mathf.Clamp(180f / rayCount * i - 90f, -180f, 180f), myTransform.up) * myTransform.forward) * maxRayDistance);
                    }

                    break;

                case RaycastTypes.CircleCast:

                    for (int i = 0; i < rayCount; i++)
                    {
                        Gizmos.DrawRay(myTransform.position + Quaternion.LookRotation(myTransform.up, myTransform.forward) * Quaternion.Euler(0, i * (360f / rayCount), 0) * myTransform.right * rayCount * raySpacing, myTransform.forward * maxRayDistance);
                    }

                    break;

                case RaycastTypes.CapsuleCast:

                    if(debugCapsule == null)
                    {
                        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        capsule.hideFlags = HideFlags.HideInHierarchy;
                        debugCapsule = capsule.GetComponent<MeshFilter>().sharedMesh;
                        if(Application.isPlaying == true)
                        {
                            Destroy(capsule);
                        }
                        else
                        {
                            DestroyImmediate(capsule);
                        }
                    }
                    else
                    {
                        Gizmos.DrawWireMesh(debugCapsule, 0, myTransform.position, Quaternion.Euler(myTransform.forward), Vector3.one * (raySpacing / 2));
                    }

                    break;

                case RaycastTypes.BoxCast:

                    Gizmos.DrawWireCube(myTransform.position, Vector3.one * raySpacing);

                    break;

                case RaycastTypes.LineCast:

                    Gizmos.DrawLine(myTransform.position, Vector3.forward * maxRayDistance);

                    break;

                default:

                    break;
                }

            if (Application.isPlaying == true && hits.Length > 0)
            {
                raycastDebug = RaycastTypes.None;
                foreach (RaycastHit hit in hits)
                {
                    if(hit.collider != null)
                    {
                        Gizmos.DrawLine(tempRay.origin, hit.point);
                        Gizmos.DrawSphere(hit.point, 0.15f);
                    }
                }
            }
        }
#endif

        // Awake is called when the script instance is being loaded
        private void Awake()
        {
            if (SmartManager.Singleton == null)
            {
                SmartManager.Singleton = new GameObject("Smart Manager").AddComponent<SmartManager>();
                Debug.LogWarning("|Smart GameObject|: SmartManager instance not found, creating one...", gameObject);
            }
            myTransform = transform;
            initialPos = myTransform.position;
            initialRot = myTransform.rotation;
            initialScale = myTransform.localScale;

            if (col == null)
            {
                col = GetComponent<Collider>();
            }
            if (rigid == null)
            {
                rigid = GetComponent<Rigidbody>();
            }
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
            if (audioSrc == null)
            {
                audioSrc = GetComponent<AudioSource>();
            }

            if (agent != null)
            {
                rotating = false;
                translating = false;
            }

            if (col != null)
            {
                isTrigger = col.isTrigger;
                IgnoreColliders(ignoredColliders, true);
            }

            RecalculatePath();

            CreateFloatVariableDictionary();
            CreateBooleanVariableDictionary();

            behaviorCount = behaviors.Count;
        }

        private void OnEnable()
        {
            if (SmartManager.Singleton != null)
            {
                SmartManager.Singleton.Register(this);
            }
            else
            {
                SmartManager.Singleton = new GameObject("Smart Manager").AddComponent<SmartManager>();
                Debug.LogWarning("|Smart GameObject|: SmartManager instance not found, creating one...", gameObject);
                SmartManager.Singleton.Register(this);
            }
            onEnable.Invoke();

#if UNITY_EDITOR

            // This is here to prevent mass warnings if recompling this script while in playmode.
            if(Application.isPlaying == true)
            {
                if (floatVariableLookup.Count == 0)
                {
                    CreateFloatVariableDictionary();
                }
                if (booleanVariableLookup.Count == 0)
                {
                    CreateBooleanVariableDictionary();
                }
            }
#endif
        }

        private void OnDisable()
        {
            if (SmartManager.Singleton != null)
            {
                SmartManager.Singleton.Unregister(this);
            }
            onDisable.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTrigger == true && colliders.Count < maxCollisions && (requiredTags.Count == 0 || requiredTags.Contains(other.gameObject.tag)))
            {
                lastContact = other;
                if (!colliders.Contains(other))
                {
                    colliders.Add(other);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (isTrigger == true && colliders.Count < maxCollisions && (requiredTags.Count == 0 || requiredTags.Contains(other.gameObject.tag)))
            {
                lastContact = other;
                colliders.Remove(other);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (isTrigger == true && (requiredTags.Count == 0 || requiredTags.Contains(other.gameObject.tag)))
            {
                lastContact = other;
                onTriggerStay.Invoke();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isTrigger == false && colliders.Count < maxCollisions && (requiredTags.Count == 0 || requiredTags.Contains(collision.gameObject.tag)))
            {
                lastContact = collision.collider;
                lastCollision = collision;
                if (!colliders.Contains(collision.collider))
                {
                    colliders.Add(collision.collider);
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (isTrigger == false && colliders.Count < maxCollisions && (requiredTags.Count == 0 || requiredTags.Contains(collision.gameObject.tag)))
            {
                lastContact = collision.collider;
                lastCollision = collision;
                colliders.Remove(collision.collider);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (isTrigger == false && (requiredTags.Count == 0 || requiredTags.Contains(collision.gameObject.tag)))
            {
                lastContact = collision.collider;
                lastCollision = collision;
                onCollisionStay.Invoke();
            }
        }

        #endregion

        #region Core Methods:

        public void UpdateSmartGameObject()
        {
            if (behaviors.Count > 0)
            {
                // Prevent delay loop if modifying behavior count at runtime.
                if (behaviorCount != behaviors.Count)
                {
                    foreach (SmartBehavior behavior in behaviors)
                    {
                        behavior.delaying = false;
                    }
                    behaviorCount = behaviors.Count;
                }

                if (currentBehavior >= behaviors.Count)
                {
                    currentBehavior = 0;
                }
                nextBehavior = behaviors[currentBehavior];
                if (nextBehavior.enabled == true && nextBehavior.delaying == false)
                {
                    EvaluateBehavior(nextBehavior);
                }
                currentBehavior++;
            }

            UpdateTweens();
        }

        /// <summary>
        /// Evaluates a behavior by weighing its conditions and invoking actions.
        /// </summary>
        /// <param name="behavior">The behavior to evaluate.</param>
        public void EvaluateBehavior(SmartBehavior behavior)
        {
            if (behavior.containsDelay == true)
            {
                StartCoroutine(EvaluateDelayedBehavior(behavior));
            }
            else
            {
                currentWeight = 0;
                foreach (Condition condition in behavior.conditions)
                {
                    if (EvaluateCondition(condition) == true)
                    {
                        currentWeight += condition.weight;
                    }
                }

                if (currentWeight >= behavior.weightThreshold)
                {
                    foreach (Action action in behavior.actions)
                    {
                        switch (action.actionType)
                        {
                            case ActionTypes.SmartAction:

                                InvokeSmartAction(action);

                                break;

                            case ActionTypes.Numerical:

                                InvokeNumericalAction(action, behavior.name);

                                break;

                            case ActionTypes.Boolean:

                                InvokeBooleanAction(action, behavior.name);

                                break;

                            case ActionTypes.UnityEvent:

                                action.unityEvent.Invoke();

                                break;

                            case ActionTypes.DelayNextAction:

                                behavior.containsDelay = true;

                                break;
                        }
                    }
                }
                else
                {
                    foreach (Action action in behavior.fallbackActions)
                    {
                        switch (action.actionType)
                        {
                            case ActionTypes.SmartAction:

                                InvokeSmartAction(action);

                                break;

                            case ActionTypes.Numerical:

                                InvokeNumericalAction(action, behavior.name);

                                break;

                            case ActionTypes.Boolean:

                                InvokeBooleanAction(action, behavior.name);

                                break;

                            case ActionTypes.UnityEvent:

                                action.unityEvent.Invoke();

                                break;

                            case ActionTypes.DelayNextAction:

                                behavior.containsDelay = true;

                                break;
                        }
                    }
                }
            }
        }

        public IEnumerator EvaluateDelayedBehavior(SmartBehavior behavior)
        {
            currentWeight = 0;
            foreach (Condition condition in behavior.conditions)
            {
                if (EvaluateCondition(condition) == true)
                {
                    currentWeight += condition.weight;
                }
            }

            if (currentWeight >= behavior.weightThreshold)
            {
                foreach (Action action in behavior.actions)
                {
                    switch (action.actionType)
                    {
                        case ActionTypes.SmartAction:

                            InvokeSmartAction(action);

                            break;

                        case ActionTypes.Numerical:

                            InvokeNumericalAction(action, behavior.name);

                            break;

                        case ActionTypes.Boolean:

                            InvokeBooleanAction(action, behavior.name);

                            break;

                        case ActionTypes.UnityEvent:

                            action.unityEvent.Invoke();

                            break;

                        case ActionTypes.DelayNextAction:

                            behavior.delaying = true;
                            if (SmartManager.Singleton.delays.ContainsKey(action.customValue))
                            {
                                yield return SmartManager.Singleton.delays[action.customValue];
                            }
                            else
                            {
                                SmartManager.Singleton.delays.Add(action.customValue, new WaitForSeconds(action.customValue));
                                yield return SmartManager.Singleton.delays[action.customValue];
                            }
                            // yield return new WaitForSeconds(action.customValue); // Honestly this call doesn't generate all that much more GC than the singleton cache.
                            behavior.delaying = false;

                            break;
                    }
                }
            }
            else
            {
                foreach (Action action in behavior.fallbackActions)
                {
                    switch (action.actionType)
                    {
                        case ActionTypes.SmartAction:

                            InvokeSmartAction(action);

                            break;

                        case ActionTypes.Numerical:

                            InvokeNumericalAction(action, behavior.name);

                            break;

                        case ActionTypes.Boolean:

                            InvokeBooleanAction(action, behavior.name);

                            break;

                        case ActionTypes.UnityEvent:

                            action.unityEvent.Invoke();

                            break;

                        case ActionTypes.DelayNextAction:

                            behavior.delaying = true;
                            if(SmartManager.Singleton.delays.ContainsKey(action.customValue))
                            {
                                yield return SmartManager.Singleton.delays[action.customValue];
                            }
                            else
                            {
                                SmartManager.Singleton.delays.Add(action.customValue, new WaitForSeconds(action.customValue));
                                yield return SmartManager.Singleton.delays[action.customValue];
                            }

                            // yield return new WaitForSeconds(action.customValue); // Honestly this call doesn't generate all that much more GC than the singleton cache.
                            behavior.delaying = false;

                            break;
                    }
                }
            }
        }

        public void InvokeSmartAction(Action action)
        {
            // Note: Large switch cases are converted to jump tables post compilation.
            switch (action.actionIndex)
            {
                case 0: return;
                case 1: StartTranslation(); return;
                case 2: StopTranslation(); return;
                case 3: PauseTranslation(); return;
                case 4: ResumeTranslation(); return;
                case 5: StartPathTranslation(); return;
                case 6: StopPathTranslation(); return;
                case 7: translationTarget = action.customVector; return;
                case 8: if (focusTarget != null) { translationTarget = focusTarget.myTransform.position; } return;
                case 9: if (hits.Length > 0) { translationTarget = hits[hits.Length].point; } return;
                case 10: if (focusTarget != null) { lookAtTarget = focusTarget.myTransform; } return;
                case 11: lookAtTarget = Camera.main.transform; return;
                case 12: lookAtTarget = null; return;
                case 13: StartRotation(); return;
                case 14: StopRotation(); return;
                case 15: PauseRotation(); return;
                case 16: ResumeRotation(); return;
                case 17: rotationTarget = Quaternion.Euler(action.customVector); return;
                case 18: if (focusTarget != null) { rotationTarget = focusTarget.myTransform.rotation; } return;
                case 19: if (hits.Length > 0) { rotationTarget = Quaternion.Euler(hits[hits.Length].normal); } return;
                case 20: StartScale(); return;
                case 21: StopScale(); return;
                case 22: PauseScale(); return;
                case 23: ResumeScale(); return;
                case 24: scaleTarget = action.customVector; return;
                case 25: if (focusTarget != null) { scaleTarget = focusTarget.myTransform.localScale; } return;
                case 26: myTransform.SetParent(focusTarget.myTransform); return;
                case 27: myTransform.SetParent(null); return;
                case 28: SetTransformValuesToInitial(); return;
                case 29: ClearTransformValues(); return;
                case 30: if (focusTarget != null) { myTransform.position = focusTarget.myTransform.position; myTransform.rotation = focusTarget.myTransform.rotation; myTransform.localScale = focusTarget.myTransform.localScale; } return;
                case 31: RaycastSingle(); return;
                case 32: RaycastAll(); return;
                case 33: RaycastRow(); return;
                case 34: RaycastCircle(); return;
                case 35: RaycastGrid(); return;
                case 36: RaycastScatter(); return;
                case 37: RaycastFan(); return;
                case 38: SphereCast(); return;
                case 39: SphereCastAll(); return;
                case 40: BoxCast(); return;
                case 41: BoxCastAll(); return;
                case 42: CapsuleCast(); return;
                case 43: LineCast(); return;
                case 44: if (animator != null) { animator.SetTrigger(action.variableName); } return;
                case 45: if (animator != null) { animator.Play(action.variableName); } return;
                case 46: if (animator != null) { animator.StopPlayback(); } return;
                case 47: SpawnAtTransform(focusTarget.myTransform, action.variableName); return;
                case 48: if (focusTarget != null) { lastSpawned = SmartManager.Singleton.GetPooledObject(action.variableName); if (lastSpawned != null) { lastSpawned.transform.position = hits[hits.Length].point; lastSpawned.SetActive(true); } else { Debug.LogWarning("|Smart GameObject|: Tried to spawn a GameObject but failed to get one from the pool. Make sure the provided name is correct and there is an adequate amount available.", gameObject); } } return;
                case 49: SpawnAtRaycastHits(action.variableName); return;
                case 50: SpawnAtColliders(action.variableName); return;
                case 51: if (lastCollision != null) { lastSpawned = SmartManager.Singleton.GetPooledObject(action.variableName); if (lastSpawned != null) { lastSpawned.transform.position = lastCollision.contacts[0].point; lastSpawned.transform.rotation = Quaternion.Euler(lastCollision.contacts[0].normal); } else { Debug.LogWarning("|Smart GameObject|: Tried to spawn a GameObject but failed to get one from the pool. Make sure the provided name is correct and there is an adequate amount available.", gameObject); } } return;
                case 52: SpawnAtTransform(lastContact.transform, action.variableName); return;
                case 53: SpawnAtTargets(action.variableName); return;
                case 54: SpawnAtTransform(myTransform, action.variableName); return;
                case 55: AIFollowFocusTarget(action.customValue); return;
                case 56: AIFleeFocusTarget(action.customValue); return;
                case 57: AIFollowFocusTargetPath();  return;
                case 58: AIFollowPath();  return;
                case 59: AIWander(action.customValue); return;
                case 60: TargetLastCollision(); return;
                case 61: TargetLastContact(); return;
                case 62: TargetColliders(); return;
                case 63: TargetRaycastHits(); return;
                case 64: TargetCollidersWithTag(action.variableName); return;
                case 65: TargetRaycastHitsWithTag(action.variableName); return;
                case 66: targets.Clear(); return;
                case 67: focusTarget = null; return;
                case 68: focusTarget = targets[0]; return;
                case 69: if (targets.Count > 0) { focusTarget = targets[targets.Count - 1]; } return;
                case 70: GetFarthestTarget();  focusTarget = farthestTarget; return;
                case 71: GetNearestTarget();  focusTarget = nearestTarget; return;
                case 72: 
                    foreach (SmartGameObject smartGO in targets)
                    {
                        if(smartGO.CompareTag(action.variableName) == true)
                        {
                            focusTarget = smartGO;
                            return;
                        }
                    }
                    return;
                case 73: GetFarthestTargetWithTag(action.variableName); if (farthestTarget != null) { focusTarget = farthestTarget; } return;
                case 74: GetNearestTargetWithTag(action.variableName); if (farthestTarget != null) { focusTarget = nearestTarget; } return;
                case 75: nearestTarget = null; return;
                case 76: farthestTarget = null; return;
                case 77: gameObject.SetActive(false); return;
                case 78: Destroy(gameObject); return;
                case 79: Debug.LogFormat("|Smart GameObject|: {0}", action.variableName); return;
                case 80: Debug.LogWarningFormat("|Smart GameObject|: {0}", action.variableName); return;
                case 81: Debug.LogErrorFormat("|Smart GameObject|: {0}", action.variableName); return;
                case 82: DebugRootDirection(); return;
                case 83: Debug.LogFormat("|Smart GameObject|: The value of {0} is: {1} ", action.variableName, GetComponentFloatValue(action.componentFloat, action.variableName, action.variableID)); return;
                case 84: Debug.LogFormat("|Smart GameObject|: The value of {0} is: {1} ", action.variableName, GetComponentBooleanValue(action.componentBoolean, action.variableName, action.variableID)); return;
                case 85: if (focusTarget != null) { Seek(focusTarget.myTransform, action.customValue); } return;
                case 86: Richochet(); return;
                case 87: if (rigid != null) { rigid.AddForce(action.customVector, ForceMode.Force); } return;
                case 88: if (rigid != null) { rigid.AddForce(action.customVector, ForceMode.Impulse); } return;
                case 89: if (rigid != null) { rigid.AddForce(action.customVector, ForceMode.Acceleration); } return;
                case 90: if (rigid != null) { rigid.velocity = action.customVector; } return;
                case 91: if (rigid != null) { rigid.AddTorque(action.customVector, ForceMode.Force); } return;
                case 92: if (rigid != null) { rigid.AddTorque(action.customVector, ForceMode.Impulse); } return;
                case 93: if (rigid != null) { rigid.AddTorque(action.customVector, ForceMode.Acceleration); } return;
                case 94: if (rigid != null) { rigid.AddRelativeForce(action.customVector, ForceMode.Force); } return;
                case 95: if (rigid != null) { rigid.AddRelativeTorque(action.customVector, ForceMode.Force); } return;
                case 96: translationCurve = action.curve; return;
                case 97: rotationCurve = action.curve; return;
                case 98: scaleCurve = action.curve; return;
                case 99: pathCurve = action.curve; return;
                case 100: DisableBehavior(action.variableID); return;
                case 101: EnableBehavior(action.variableID); return;
                case 102: if (action.variableID < behaviors.Count && action.variableID >= 0) { behaviors[action.variableID].delaying = false; } return;
                case 103: StopAllCoroutines(); return;
                case 104: enabled = false; return;
                case 105: myTransform.position = action.customVector; return;
                case 106: myTransform.rotation = Quaternion.Euler(action.customVector); return;
                case 107: myTransform.localScale = action.customVector; return;
                case 108: if (focusTarget != null) { myTransform.position = focusTarget.myTransform.position; } return;
                case 109: if (focusTarget != null) { myTransform.rotation = focusTarget.myTransform.rotation; } return;
                case 110: if (focusTarget != null) { myTransform.localScale = focusTarget.myTransform.localScale; } return;
                case 111: if(focusTarget != null) { targets.Remove(focusTarget); } return;
                case 112: RemoveNullTargets(); return;
                case 113: targets.Reverse(); return;
                case 114: if(lastSpawned != null && lastSpawned.GetComponent<SmartGameObject>() != null) { focusTarget = lastSpawned.GetComponent<SmartGameObject>(); } return;
                case 115: if(rigid != null) { rigid.AddForce(myTransform.forward * action.customValue); } return;
                case 116: hits = new RaycastHit[0]; return;
                case 117: colliders.Clear(); return;
                case 118: if(lastSpawned != null) { myTransform.LookAt(lastSpawned.transform); } return;
                case 119: if(lastContact != null) { myTransform.LookAt(lastContact.transform); } return;
                case 120: if(hits.Length >= 1 && hits[hits.Length -1].collider != null) { myTransform.LookAt(hits[hits.Length-1].transform); Debug.Log(true); } return;
                case 121: if (lastCollision != null) { myTransform.LookAt(lastCollision.transform); } return;
                case 122: if (focusTarget != null && focusTarget.lastSpawned != null) { myTransform.LookAt(focusTarget.lastSpawned.transform); } return;
                case 123: if(focusTarget != null) { myTransform.LookAt(focusTarget.myTransform); } return;
            }
        }

        public void InvokeBooleanAction(Action action, string behaviorName)
        {
            by = GetComponentBooleanValue(action.componentBoolean2, action.variableName2, action.variableID2);

            // Get and set left side of the boolean operation.
            // Note: Large switch cases are converted to jump tables post compilation.
            switch (action.componentBoolean)
            {
                case 0:  return;
                case 1:  return;
                case 2:

                    if (booleanVariableLookup.ContainsKey(action.variableID) == true)
                    {
                        bx = booleanVariableLookup[action.variableID];
                        PerformBooleanOperation(ref bx, action.booleanComparison, by);
                        booleanVariableLookup[action.variableID] = bx;
                    }
                    else
                    {
                        // Debug.LogWarningFormat("|Smart GameObject|: Variable: {0} not found when executing one of {1}'s boolean actions.", action.variableID, behaviorName, gameObject);
                        return;
                    }

                    return;

                case 3: bx = gameObject.activeSelf; PerformBooleanOperation(ref bx, action.booleanComparison, by); gameObject.SetActive(bx);  return;
                case 4: bx = gameObject.isStatic; PerformBooleanOperation(ref bx, action.booleanComparison, by); gameObject.isStatic = bx;  return;
                case 5: return;
                case 6: return;
                case 7: return;
                case 8: return;
                case 9: if(rigid != null) { bx = rigid.isKinematic; PerformBooleanOperation(ref bx, action.booleanComparison, by); rigid.isKinematic = bx; }  return;
                case 10: return;
                case 11: if(agent != null) { bx = agent.enabled; PerformBooleanOperation(ref bx, action.booleanComparison, by); agent.enabled = bx; }  return;
                case 12: return;
                case 13: return;
                case 14: return;
                case 15: if(agent != null) { bx = agent.isStopped; PerformBooleanOperation(ref bx, action.booleanComparison, by); agent.isStopped = bx; } return;
                case 16: return;
                case 17: return;
                case 18: return;
                case 19: return;
                case 20: PerformBooleanOperation(ref usePath, action.booleanComparison, by);  return;
                case 21: PerformBooleanOperation(ref spin, action.booleanComparison, by);  return;
                case 22: PerformBooleanOperation(ref snappyScale, action.booleanComparison, by);  return;
                case 23: PerformBooleanOperation(ref useLocalSpace, action.booleanComparison, by);  return;
                case 24: PerformBooleanOperation(ref lookAtPath, action.booleanComparison, by);  return;
                case 25: return;
                case 26: if(audioSrc != null) { bx = audioSrc.enabled; PerformBooleanOperation(ref bx, action.booleanComparison, by); audioSrc.enabled = bx; }  return;
                case 27: return;
                case 28: return;
                case 29: if(audioSrc != null) { bx = audioSrc.loop; PerformBooleanOperation(ref bx, action.booleanComparison, by); audioSrc.loop = bx; } return;
                case 30: if (audioSrc != null) { bx = audioSrc.mute; PerformBooleanOperation(ref bx, action.booleanComparison, by); audioSrc.mute = bx; } return;
                case 31: if (audioSrc != null) { bx = audioSrc.spatialize; PerformBooleanOperation(ref bx, action.booleanComparison, by); audioSrc.spatialize = bx; } return;
                case 32: if (audioSrc != null) { bx = audioSrc.spatializePostEffects; PerformBooleanOperation(ref bx, action.booleanComparison, by); audioSrc.spatializePostEffects = bx; } return;
                case 33: return;
                case 34: if(animator != null) { bx = animator.enabled; PerformBooleanOperation(ref bx, action.booleanComparison, by); animator.enabled = bx; }  return;
                case 35: if (animator != null) { bx = animator.applyRootMotion; PerformBooleanOperation(ref bx, action.booleanComparison, by); animator.applyRootMotion = bx; } return;
                case 36: if (animator != null) { bx = animator.fireEvents; PerformBooleanOperation(ref bx, action.booleanComparison, by); animator.fireEvents = bx; } return;
                case 37: if (animator != null) { bx = animator.GetBool(action.variableName); PerformBooleanOperation(ref bx, action.booleanComparison, by); animator.SetBool(action.variableName, bx); } return;
                case 38: return;
                case 39: return;
                case 40: return;
                case 41: return;
                case 42: if (animator != null) { bx = animator.stabilizeFeet; PerformBooleanOperation(ref bx, action.booleanComparison, by); animator.stabilizeFeet = bx; } return;
                case 43: return;
                case 44: return;
                case 45: return;
                case 46: return;
                case 47: return;
                case 48:

                    if(focusTarget != null)
                    {
                        if (focusTarget.booleanVariableLookup.ContainsKey(action.variableID) == true)
                        {
                            bx = focusTarget.booleanVariableLookup[action.variableID];
                            PerformBooleanOperation(ref bx, action.booleanComparison, by);
                            focusTarget.booleanVariableLookup[action.variableID] = bx;
                        }
                        else
                        {
                            Debug.LogWarningFormat("|Smart GameObject|: Variable: {0} not found on focus target when executing one of {1}'s boolean actions.", action.variableID, behaviorName, focusTarget.gameObject);
                            return;
                        }
                    }
                    return;
                case 49: return;
                case 50: if (focusTarget != null) { bx = focusTarget.gameObject.activeSelf; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.gameObject.SetActive(bx); } return;
                case 51: if (focusTarget != null) { bx = focusTarget.gameObject.isStatic; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.gameObject.isStatic = bx; } return;
                case 52: return;
                case 53: return;
                case 54: return;
                case 55: return;
                case 56: if (focusTarget != null && focusTarget.rigid != null) { bx = focusTarget.rigid.isKinematic; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.rigid.isKinematic = bx; } return;
                case 57: return;
                case 58: if (focusTarget != null && focusTarget.agent != null) { bx = focusTarget.agent.enabled; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.agent.enabled = bx; } return;
                case 59: return;
                case 60: return;
                case 61: return;
                case 62: if (focusTarget != null && focusTarget.agent != null) { bx = focusTarget.agent.isStopped; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.agent.isStopped = bx; } return;
                case 63: return;
                case 64: return;
                case 65: return;
                case 66: return;
                case 67: if (focusTarget != null) { PerformBooleanOperation(ref focusTarget.usePath, action.booleanComparison, by); } return;
                case 68: if (focusTarget != null) { PerformBooleanOperation(ref focusTarget.spin, action.booleanComparison, by); } return;
                case 69: if (focusTarget != null) { PerformBooleanOperation(ref focusTarget.snappyScale, action.booleanComparison, by); } return;
                case 70: if (focusTarget != null) { PerformBooleanOperation(ref focusTarget.useLocalSpace, action.booleanComparison, by); } return;
                case 71: if (focusTarget != null) { PerformBooleanOperation(ref focusTarget.lookAtPath, action.booleanComparison, by); } return;
                case 72: return;
                case 73: if (focusTarget != null && focusTarget.audioSrc != null) { bx = focusTarget.audioSrc.enabled; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.audioSrc.enabled = bx; } return;
                case 74: return;
                case 75: return;
                case 76: if (focusTarget != null && focusTarget.audioSrc != null) { bx = focusTarget.audioSrc.loop; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.audioSrc.loop = bx; } return;
                case 77: if (focusTarget != null && focusTarget.audioSrc != null) { bx = focusTarget.audioSrc.mute; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.audioSrc.mute = bx; } return;
                case 78: if (focusTarget != null && focusTarget.audioSrc != null) { bx = focusTarget.audioSrc.spatialize; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.audioSrc.spatialize = bx; } return;
                case 79: if (focusTarget != null && focusTarget.audioSrc != null) { bx = focusTarget.audioSrc.spatializePostEffects; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.audioSrc.spatializePostEffects = bx; } return;
                case 80: return;
                case 81: if (focusTarget != null && focusTarget.animator != null) { bx = focusTarget.animator.enabled; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.animator.enabled = bx; } return;
                case 82: if (focusTarget != null && focusTarget.animator != null) { bx = focusTarget.animator.applyRootMotion; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.animator.applyRootMotion = bx; } return;
                case 83: if (focusTarget != null && focusTarget.animator != null) { bx = focusTarget.animator.fireEvents; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.animator.fireEvents = bx; } return;
                case 84: if (focusTarget != null && focusTarget.animator != null) { bx = focusTarget.animator.GetBool(action.variableName); PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.animator.SetBool(action.variableName, bx); } return;
                case 85: return;
                case 86: return;
                case 87: return;
                case 88: return;
                case 89: if (focusTarget != null && animator != null) { bx = animator.stabilizeFeet; PerformBooleanOperation(ref bx, action.booleanComparison, by); animator.stabilizeFeet = bx; } return;
                case 90: return;
                case 91: return;
                case 92: return;
                case 93: return;
                case 94: return;
                case 95: return;
                case 96: bx = directLook; PerformBooleanOperation(ref bx, action.booleanComparison, by); directLook = bx; return;
                case 97: bx = horizontalLook; PerformBooleanOperation(ref bx, action.booleanComparison, by); horizontalLook = bx; return;
                case 98: if (focusTarget != null) { bx = focusTarget.directLook; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.directLook = bx; } return;
                case 99: if (focusTarget != null) { bx = focusTarget.horizontalLook; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.horizontalLook = bx; } return;
                case 100: return;
                case 101: return;
                case 102: if (agent != null) { bx = agent.autoBraking; PerformBooleanOperation(ref bx, action.booleanComparison, by); agent.autoBraking = bx; } return;
                case 103: if (focusTarget != null && agent != null) { bx = focusTarget.agent.autoBraking; PerformBooleanOperation(ref bx, action.booleanComparison, by); focusTarget.agent.autoBraking = bx; } return;
                case 104: return;
                case 105: return;
                case 106: return;
                case 107: return;
                case 108: return;
                case 109: return;
                default: return;
            }
        }

        public void InvokeNumericalAction(Action action, string behaviorName)
        {
            // Set right side of the numerical operation.
            if (action.componentFloat2 == 0)
            {
                fy = action.customValue2;
            }
            else
            {
                fy = GetComponentFloatValue(action.componentFloat2, action.variableName2, action.variableID2);
            }

            // Get and set left side of the numerical operation.
            switch (action.componentFloat)
            {
                case 0: return;
                case 1:

                    if (floatVariableLookup.ContainsKey(action.variableID) == true)
                    {
                        fx = floatVariableLookup[action.variableID];
                        PerformNumericalOperation(ref fx, action.numericalOperator, fy);
                        floatVariableLookup[action.variableID] = fx;
                    }
                    else
                    {
                       //  Debug.LogWarningFormat("|Smart GameObject|: Variable: {0} not found when executing one of {1}'s numerical actions.", action.variableName2, behaviorName, gameObject);
                        return;
                    }
                    return; 
                case 2: return;
                case 3: return;
                case 4: return;
                case 5: return;
                case 6: return;
                case 7: return;
                case 8: return;
                case 9: return;
                case 10: tempVector = myTransform.position; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); myTransform.position = tempVector; return;
                case 11: tempVector = myTransform.position; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); myTransform.position = tempVector; return;
                case 12: tempVector = myTransform.position; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); myTransform.position = tempVector; return;
                case 13: tempVector = myTransform.localPosition; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); myTransform.localPosition = tempVector; return;
                case 14: tempVector = myTransform.localPosition; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); myTransform.localPosition = tempVector; return;
                case 15: tempVector = myTransform.localPosition; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); myTransform.localPosition = tempVector; return;
                case 16: tempVector = myTransform.rotation.eulerAngles; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); myTransform.rotation = Quaternion.Euler(tempVector); return;
                case 17: tempVector = myTransform.rotation.eulerAngles; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); myTransform.rotation = Quaternion.Euler(tempVector); return;
                case 18: tempVector = myTransform.rotation.eulerAngles; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); myTransform.rotation = Quaternion.Euler(tempVector); return;
                case 19: tempVector = myTransform.localRotation.eulerAngles; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); myTransform.localRotation = Quaternion.Euler(tempVector); return;
                case 20: tempVector = myTransform.localRotation.eulerAngles; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); myTransform.localRotation = Quaternion.Euler(tempVector); return;
                case 21: tempVector = myTransform.localRotation.eulerAngles; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); myTransform.localRotation = Quaternion.Euler(tempVector); return;
                case 22: return;
                case 23: return;
                case 24: return;
                case 25: tempVector = myTransform.localScale; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); myTransform.localScale = tempVector; return;
                case 26: tempVector = myTransform.localScale; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); myTransform.localScale = tempVector; return;
                case 27: tempVector = myTransform.localScale; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); myTransform.localScale = tempVector; return;
                case 28: return;
                case 29: return;
                case 30: if (rigid != null) { fx = rigid.maxAngularVelocity; PerformNumericalOperation(ref fx, action.numericalOperator, fy); rigid.maxAngularVelocity = fx; } return;
                case 31: if (rigid != null) { fx = rigid.mass; PerformNumericalOperation(ref fx, action.numericalOperator, fy); rigid.mass = fx; } return;
                case 32: if (rigid != null) { fx = rigid.drag; PerformNumericalOperation(ref fx, action.numericalOperator, fy); rigid.drag = fx; } return;
                case 33: if (rigid != null) { fx = rigid.angularDrag; PerformNumericalOperation(ref fx, action.numericalOperator, fy); rigid.angularDrag = fx; } return;
                case 34: if (rigid != null) { fx = rigid.maxDepenetrationVelocity; PerformNumericalOperation(ref fx, action.numericalOperator, fy); rigid.maxDepenetrationVelocity = fx; } return;
                case 35: return;
                case 36: if(agent != null) { fx = agent.acceleration; PerformNumericalOperation(ref fx, action.numericalOperator, fy); agent.acceleration = fx; } return;
                case 37: if (agent != null) { fx = agent.angularSpeed; PerformNumericalOperation(ref fx, action.numericalOperator, fy); agent.angularSpeed = fx; } return;
                case 38: if (agent != null) { fx = agent.baseOffset; PerformNumericalOperation(ref fx, action.numericalOperator, fy); agent.baseOffset = fx; } return;
                case 39: return;
                case 40: if (agent != null) { fx = agent.speed; PerformNumericalOperation(ref fx, action.numericalOperator, fy); agent.speed = fx; } return;
                case 41: if (agent != null) { fx = agent.height; PerformNumericalOperation(ref fx, action.numericalOperator, fy); agent.radius = fx; } return;
                case 42: if (agent != null) { fx = agent.radius; PerformNumericalOperation(ref fx, action.numericalOperator, fy); agent.radius = fx; } return;
                case 43: if (agent != null) { fx = agent.stoppingDistance; PerformNumericalOperation(ref fx, action.numericalOperator, fy); agent.stoppingDistance = fx; } return;
                case 44: if (audioSrc != null) { fx = audioSrc.dopplerLevel; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.dopplerLevel = fx; } return;
                case 45: if (audioSrc != null) { fx = audioSrc.minDistance; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.minDistance = fx; } return;
                case 46: if (audioSrc != null) { fx = audioSrc.maxDistance; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.maxDistance = fx; } return;
                case 47: if (audioSrc != null) { fx = audioSrc.volume; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.volume = fx; } return;
                case 48: if (audioSrc != null) { fx = audioSrc.panStereo; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.panStereo = fx; } return;
                case 49: if (audioSrc != null) { fx = audioSrc.pitch; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.pitch = fx; } return;
                case 50: if (audioSrc != null) { fx = audioSrc.reverbZoneMix; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.reverbZoneMix = fx; } return;
                case 51: if (audioSrc != null) { fx = audioSrc.spread; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.spread = fx; } return;
                case 52: if (audioSrc != null) { fx = audioSrc.spatialBlend; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.spatialBlend = fx; } return;
                case 53: if (audioSrc != null) { fx = audioSrc.time; PerformNumericalOperation(ref fx, action.numericalOperator, fy); audioSrc.time = fx; } return;
                case 54: if(animator != null) { fx = animator.GetFloat(action.variableName); PerformNumericalOperation(ref fx, action.numericalOperator, fy); animator.SetFloat(action.variableName, fx); } return;
                case 55: if (animator != null) { fx = animator.GetInteger(action.variableName); PerformNumericalOperation(ref fx, action.numericalOperator, fy); animator.SetInteger(action.variableName, (int)fx); } return;
                case 56: if (animator != null) { fx = animator.speed; PerformNumericalOperation(ref fx, action.numericalOperator, fy); animator.speed = fx; } return;
                case 57: return;
                case 58: if (animator != null) { fx = animator.playbackTime; PerformNumericalOperation(ref fx, action.numericalOperator, fy); animator.playbackTime = fx; } return;
                case 59: return;
                case 60: return;
                case 61: return;
                case 62: return;
                case 63: return;
                case 64: return;
                case 65: return;
                case 66: return;
                case 67: return;
                case 68: return;
                case 69: return;
                case 70: return;
                case 71: return;
                case 72: return;
                case 73: return;
                case 74: return;
                case 75: return;
                case 76: return;
                case 77: return;
                case 78: return;
                case 79: fx = maxRayDistance; PerformNumericalOperation(ref fx, action.numericalOperator, fy); maxRayDistance = fx; return;
                case 80: fx = raySpacing; PerformNumericalOperation(ref fx, action.numericalOperator, fy); raySpacing = fx; return;
                case 81: fx = verticalScatter; PerformNumericalOperation(ref fx, action.numericalOperator, fy); verticalScatter = fx; return;
                case 82: fx = horizontalScatter; PerformNumericalOperation(ref fx, action.numericalOperator, fy); horizontalScatter = fx; return;
                case 83: fx = maxCollisions; PerformNumericalOperation(ref fx, action.numericalOperator, fy); maxCollisions = (int)fx; return;
                case 85:
                    if (focusTarget != null)
                    {
                        if (focusTarget.floatVariableLookup.ContainsKey(action.variableID) == true)
                        {
                            fx = focusTarget.floatVariableLookup[action.variableID];
                            PerformNumericalOperation(ref fx, action.numericalOperator, fy);
                            focusTarget.floatVariableLookup[action.variableID] = fx;
                        }
                        else
                        {
                            Debug.LogWarningFormat("|Smart GameObject|: Variable: {0} not found when executing one of {1}'s numerical actions.", action.variableID2, behaviorName, focusTarget.gameObject);
                            return;
                        }
                        return;
                    }
                    else
                    { 
                        return; 
                    }
                case 86: if (focusTarget != null) { tempVector = focusTarget.myTransform.position; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); focusTarget.myTransform.position = tempVector; } return;
                case 87: if (focusTarget != null) { tempVector = focusTarget.myTransform.position; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); focusTarget.myTransform.position = tempVector; } return;
                case 88: if (focusTarget != null) { tempVector = focusTarget.myTransform.position; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); focusTarget.myTransform.position = tempVector; } return;
                case 89: if (focusTarget != null) { tempVector = focusTarget.myTransform.localPosition; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); focusTarget.myTransform.localPosition = tempVector; } return;
                case 90: if (focusTarget != null) { tempVector = focusTarget.myTransform.localPosition; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); focusTarget.myTransform.localPosition = tempVector; } return;
                case 91: if (focusTarget != null) { tempVector = focusTarget.myTransform.localPosition; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); focusTarget.myTransform.localPosition = tempVector; } return;
                case 92: if (focusTarget != null) { tempVector = focusTarget.myTransform.rotation.eulerAngles; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); focusTarget.myTransform.rotation = Quaternion.Euler(tempVector); } return;
                case 93: if (focusTarget != null) { tempVector = focusTarget.myTransform.rotation.eulerAngles; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); focusTarget.myTransform.rotation = Quaternion.Euler(tempVector); } return;
                case 94: if (focusTarget != null) { tempVector = focusTarget.myTransform.rotation.eulerAngles; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); focusTarget.myTransform.rotation = Quaternion.Euler(tempVector); } return;
                case 95: if (focusTarget != null) { tempVector = focusTarget.myTransform.localRotation.eulerAngles; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); focusTarget.myTransform.localRotation = Quaternion.Euler(tempVector); } return;
                case 96: if (focusTarget != null) { tempVector = focusTarget.myTransform.localRotation.eulerAngles; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); focusTarget.myTransform.localRotation = Quaternion.Euler(tempVector); } return;
                case 97: if (focusTarget != null) { tempVector = focusTarget.myTransform.localRotation.eulerAngles; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); focusTarget.myTransform.localRotation = Quaternion.Euler(tempVector); } return;
                case 98: return;
                case 99: return;
                case 100: return;
                case 101: if (focusTarget != null) { tempVector = focusTarget.myTransform.localScale; PerformNumericalOperation(ref tempVector.x, action.numericalOperator, fy); focusTarget.myTransform.localScale = tempVector; } return;
                case 102: if (focusTarget != null) { tempVector = focusTarget.myTransform.localScale; PerformNumericalOperation(ref tempVector.y, action.numericalOperator, fy); focusTarget.myTransform.localScale = tempVector; } return;
                case 103: if (focusTarget != null) { tempVector = focusTarget.myTransform.localScale; PerformNumericalOperation(ref tempVector.z, action.numericalOperator, fy); focusTarget.myTransform.localScale = tempVector; } return;
                case 104: return;
                case 105: return;
                case 106: if (focusTarget != null && focusTarget.rigid != null) { fx = focusTarget.rigid.maxAngularVelocity; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.rigid.maxAngularVelocity = fx; } return; 
                case 107: if (focusTarget != null && focusTarget.rigid != null) { fx = focusTarget.rigid.mass; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.rigid.mass = fx; } return; 
                case 108: if (focusTarget != null && focusTarget.rigid != null) { fx = focusTarget.rigid.drag; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.rigid.drag = fx; } return; 
                case 109: if (focusTarget != null && focusTarget.rigid != null) { fx = focusTarget.rigid.angularDrag; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.rigid.angularDrag = fx; } return; 
                case 110: if (focusTarget != null && focusTarget.rigid != null) { fx = focusTarget.rigid.maxDepenetrationVelocity; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.rigid.maxDepenetrationVelocity = fx; } return; 
                case 111: return; 
                case 112: if (focusTarget != null && focusTarget.agent != null) { fx = focusTarget.agent.acceleration; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.agent.acceleration = fx; } return; 
                case 113: if (focusTarget != null && focusTarget.agent != null) { fx = focusTarget.agent.angularSpeed; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.agent.angularSpeed = fx; } return; 
                case 114: if (focusTarget != null && focusTarget.agent != null) { fx = focusTarget.agent.baseOffset; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.agent.baseOffset = fx; } return; 
                case 115: return; 
                case 116: if (focusTarget != null && focusTarget.agent != null) { fx = focusTarget.agent.speed; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.agent.speed = fx; } return; 
                case 117: if (focusTarget != null && focusTarget.agent != null) { fx = focusTarget.agent.height; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.agent.height = fx; } return; 
                case 118: if (focusTarget != null && focusTarget.agent != null) { fx = focusTarget.agent.radius; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.agent.radius = fx; } return; 
                case 119: if (focusTarget != null && focusTarget.agent != null) { fx = focusTarget.agent.stoppingDistance; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.agent.stoppingDistance = fx; } return; 
                case 120: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.dopplerLevel; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.dopplerLevel = fx; } return; 
                case 121: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.minDistance; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.minDistance = fx; } return; 
                case 122: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.maxDistance; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.maxDistance = fx; } return; 
                case 123: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.volume; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.volume = fx; } return; 
                case 124: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.panStereo; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.panStereo = fx; } return; 
                case 125: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.pitch; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.pitch = fx; } return; 
                case 126: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.reverbZoneMix; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.reverbZoneMix = fx; } return; 
                case 127: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.spread; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.spread = fx; } return; 
                case 128: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.spatialBlend; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.spatialBlend = fx; } return; 
                case 129: if (focusTarget != null && focusTarget.audioSrc != null) { fx = focusTarget.audioSrc.time; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.audioSrc.time = fx; } return; 
                case 130: if (focusTarget != null && focusTarget.animator != null) { fx = focusTarget.animator.GetFloat(action.variableName); PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.animator.SetFloat(action.variableName, fx); } return; 
                case 131: if (focusTarget != null && focusTarget.animator != null) { fx = focusTarget.animator.GetInteger(action.variableName); PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.animator.SetInteger(action.variableName, (int)fx); } return; 
                case 132: if (focusTarget != null && focusTarget.animator != null) { fx = focusTarget.animator.speed; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.animator.speed = fx; } return; 
                case 133: return; 
                case 134: if (focusTarget != null && focusTarget.animator != null) { fx = focusTarget.animator.playbackTime; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.animator.playbackTime = fx; } return; 
                case 135: return; 
                case 136: return; 
                case 137: return; 
                case 138: if (focusTarget != null) { fx = focusTarget.maxRayDistance; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.maxRayDistance = fx; } return; 
                case 139: if (focusTarget != null) { fx = focusTarget.raySpacing; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.raySpacing = fx; } return; 
                case 140: if (focusTarget != null) { fx = focusTarget.rayCount; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.rayCount = (int)fx; } return; 
                case 141: if (focusTarget != null) { fx = focusTarget.verticalScatter; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.verticalScatter = fx; } return; 
                case 142: if (focusTarget != null) { fx = focusTarget.horizontalScatter; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.horizontalScatter = fx; } return; 
                case 143: if (focusTarget != null) { fx = focusTarget.maxCollisions; PerformNumericalOperation(ref fx, action.numericalOperator, fy); focusTarget.maxCollisions = (int)fx; } return;
                case 144: return;
                case 145: return;
                case 146: return;
                case 147: return;
                case 148: return;
                case 149: return;
                case 150: return;
                case 151: return;
                case 152: return;
                case 153: return;
                case 154: return;
                case 155: return;
                case 156: return;
                case 157: return;
                default: return;
            }
        }

        public bool EvaluateCondition(Condition condition)
        {
            switch (condition.conditionType)
            {
                case ConditionTypes.Numerical:

                    if(condition.componentFloat == 0)
                    {
                        fx = condition.customValue;
                    }
                    else
                    {
                        fx = GetComponentFloatValue(condition.componentFloat, condition.variableName, condition.variableID);
                    }

                    if (condition.componentFloat2 == 0)
                    {
                        fy = condition.customValue2;
                    }
                    else
                    {
                        fy = GetComponentFloatValue(condition.componentFloat2, condition.variableName2, condition.variableID2);
                    }
               

                    switch (condition.numericalComparison)
                    {
                        case NumericalComparisons.EqualTo:

                            return fx == fy;

                        case NumericalComparisons.NotEqual:

                            return fx != fy;

                        case NumericalComparisons.GreaterThan:

                            return fx > fy;

                        case NumericalComparisons.GreaterThanEqualTo:

                            return fx >= fy;

                        case NumericalComparisons.LessThan:

                            return fx < fy;

                        case NumericalComparisons.LessThanEqualTo:

                            return fx <= fy;

                        default:

                            return false;
                    }

                case ConditionTypes.Boolean:

                    bx = GetComponentBooleanValue(condition.componentBoolean, condition.variableName, condition.variableID);
                    by = GetComponentBooleanValue(condition.componentBoolean2, condition.variableName2, condition.variableID2);
                    switch (condition.booleanComparison)
                    {
                        case BooleanComparisons.EqualTo:

                            return bx == by;

                        case BooleanComparisons.NotEqualTo:

                            return bx != by;

                        default:

                            return false;
                    }
            }

            return false;
        }

        public float GetComponentFloatValue(int index, string variableName = "", int variableID = 0)
        {
            // Note: Large switch cases are converted to hash maps post compilation.
            switch (index)
            {
                case 0: return variableID;
                case 1:
                    if (floatVariableLookup.ContainsKey(variableID))
                    {
                        return floatVariableLookup[variableID];
                    }
                    else
                    {
                        Debug.LogWarning("|Smart GameObject|: Tried to get an undefined float variable.", gameObject);
                        return 0;
                    }
                case 2: return Mathf.PI;
                case 3: return phi;
                case 4: return Mathf.Epsilon;
                case 5: return Mathf.Infinity;
                case 6: return Mathf.NegativeInfinity;
                case 7: return float.MaxValue;
                case 8: return Mathf.Deg2Rad;
                case 9: return Mathf.Rad2Deg;
                case 10: return myTransform.position.x;
                case 11: return myTransform.position.y;
                case 12: return myTransform.position.z;
                case 13: return myTransform.localPosition.x;
                case 14: return myTransform.localPosition.y;
                case 15: return myTransform.localPosition.z;
                case 16: return myTransform.rotation.x;
                case 17: return myTransform.rotation.y;
                case 18: return myTransform.rotation.z;
                case 19: return myTransform.localRotation.x;
                case 20: return myTransform.localRotation.y;
                case 21: return myTransform.localRotation.z;
                case 22: return myTransform.lossyScale.x;
                case 23: return myTransform.lossyScale.y;
                case 24: return myTransform.lossyScale.z;
                case 25: return myTransform.localScale.x;
                case 26: return myTransform.localScale.y;
                case 27: return myTransform.localScale.z;
                case 28: return myTransform.childCount;
                case 29: if (rigid != null) { return rigid.angularVelocity.magnitude; } else return 0;
                case 30: if (rigid != null) { return rigid.maxAngularVelocity; } else return 0;
                case 31: if (rigid != null) { return rigid.mass; } else return 0;
                case 32: if (rigid != null) { return rigid.drag; } else return 0;
                case 33: if (rigid != null) { return rigid.angularDrag; } else return 0;
                case 34: if (rigid != null) { return rigid.maxDepenetrationVelocity; } else return 0;
                case 35: if (rigid != null) { return rigid.velocity.magnitude; } else return 0;
                case 36: if (agent != null) { return agent.acceleration; } else return 0;
                case 37: if (agent != null) { return agent.angularSpeed; } else return 0;
                case 38: if (agent != null) { return agent.baseOffset; } else return 0;
                case 39: if (agent != null) { return agent.velocity.magnitude; } else return 0;
                case 40: if (agent != null) { return agent.speed; } else return 0;
                case 41: if (agent != null) { return agent.height; } else return 0;
                case 42: if (agent != null) { return agent.radius; } else return 0;
                case 43: if (agent != null) { return agent.stoppingDistance; } else return 0;
                case 44: if (audioSrc != null) { return audioSrc.dopplerLevel; } else return 0;
                case 45: if (audioSrc != null) { return audioSrc.minDistance; } else return 0;
                case 46: if (audioSrc != null) { return audioSrc.maxDistance; } else return 0;
                case 47: if (audioSrc != null) { return audioSrc.volume; } else return 0;
                case 48: if (audioSrc != null) { return audioSrc.panStereo; } else return 0;
                case 49: if (audioSrc != null) { return audioSrc.pitch; } else return 0;
                case 50: if (audioSrc != null) { return audioSrc.reverbZoneMix; } else return 0;
                case 51: if (audioSrc != null) { return audioSrc.spread; } else return 0;
                case 52: if (audioSrc != null) { return audioSrc.spatialBlend; } else return 0;
                case 53: if (audioSrc != null) { return audioSrc.time; } else return 0;
                case 54: if (animator != null) { return animator.GetFloat(variableName); } else return 0;
                case 55: if (animator != null) { return animator.GetInteger(variableName); } else return 0;
                case 56: if (animator != null) { return animator.speed; } else return 0;
                case 57: if (animator != null) { return animator.pivotWeight; } else return 0;
                case 58: if (animator != null) { return animator.playbackTime; } else return 0;
                case 59: if (animator != null) { return animator.leftFeetBottomHeight; } else return 0;
                case 60: if (animator != null) { return animator.rightFeetBottomHeight; } else return 0;
                case 61: if (animator != null) { return animator.angularVelocity.magnitude; } else return 0;
                case 62: if (focusTarget != null) { return Vector3.Distance(myTransform.position, focusTarget.myTransform.position); } else return 0;
                case 63: if (targets.Count > 0) { if (targets[0] != null) { return Vector3.Distance(myTransform.position, targets[0].myTransform.position); } else { RemoveNullTargets(); return 0; } } else return 0;
                case 64: if (targets.Count > 0) { if (targets[targets.Count - 1] != null) { return Vector3.Distance(myTransform.position, targets[targets.Count - 1].myTransform.position); } else { RemoveNullTargets(); return 0; } } else return 0; 
                case 65: if (targets.Count > 0) { if (targets[targets.Count - 1] != null) { return Vector3.Distance(myTransform.position, targets[UnityEngine.Random.Range(0, targets.Count - 1)].myTransform.position); } else { RemoveNullTargets(); return 0; } } else return 0;
                case 66:

                    if(nearestTarget != null)
                    {
                        return Vector3.Distance(myTransform.position, nearestTarget.myTransform.position);
                    }
                    else
                    {
                        if (targets.Count > 0)
                        {
                            GetNearestTarget();
                            return Vector3.Distance(myTransform.position, nearestTarget.myTransform.position);
                        }
                        else return 0;
                    }

                case 67:

                    if (farthestTarget != null)
                    {
                        return Vector3.Distance(myTransform.position, farthestTarget.myTransform.position);
                    }
                    else
                    {
                        if (targets.Count > 0)
                        {
                            GetFarthestTarget();
                            return Vector3.Distance(myTransform.position, farthestTarget.myTransform.position);
                        }
                        else return 0;
                    }

                case 68: return Vector3.Distance(myTransform.position, translationTarget);
                case 69: if (hits.Length > 0) { return Vector3.Distance(myTransform.position, hits[0].point); } else return 0;
                case 70: if(hits.Length > 0) { return Vector3.Distance(myTransform.position, hits[hits.Length - 1].point); }  else return 0;
                case 71: if (hits.Length > 0) { return Vector3.Distance(myTransform.position, hits[UnityEngine.Random.Range(0, hits.Length -1)].point); } else return 0;
                case 72: if (lastCollision != null) { return Vector3.Distance(myTransform.position, lastCollision.transform.position); } else return 0;
                case 73: if(lastContact != null) { return Vector3.Distance(myTransform.position, lastContact.transform.position); } else return 0;
                case 74:

                    if (splines.Count > 0)
                    {
                        if (path.Count > 0)
                        {
                            return Vector3.Distance(myTransform.position, path[0]);
                        }
                        else
                        {
                            RecalculatePath();
                            return Vector3.Distance(myTransform.position, path[0]);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("|Smart GameObject|: Tried to get distance to the start of a path but no splines were defined. Returning 0.", gameObject);
                        return 0;
                    }

                case 75: return Time.deltaTime;
                case 76: return Time.unscaledDeltaTime;
                case 77: return Time.fixedDeltaTime;
                case 78: return Time.fixedUnscaledDeltaTime;
                case 79: return maxRayDistance;
                case 80: return raySpacing;
                case 81: return rayCount;
                case 82: return verticalScatter;
                case 83: return horizontalScatter;
                case 84: return maxCollisions;
                case 85:
                    if (focusTarget != null)
                    {
                        if (focusTarget.floatVariableLookup.ContainsKey(variableID))
                        {
                            return focusTarget.floatVariableLookup[variableID];
                        }
                        else
                        {
                            Debug.LogWarning("|Smart GameObject|: Tried to get an undefined float variable.", focusTarget.gameObject);
                            return 0;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                case 86: if (focusTarget != null) { return focusTarget.myTransform.position.x; } else return 0;
                case 87: if (focusTarget != null) { return focusTarget.myTransform.position.y; } else return 0;
                case 88: if (focusTarget != null) { return focusTarget.myTransform.position.z; } else return 0;
                case 89: if (focusTarget != null) { return focusTarget.myTransform.localPosition.x; } else return 0;
                case 90: if (focusTarget != null) { return focusTarget.myTransform.localPosition.y; } else return 0;
                case 91: if (focusTarget != null) { return focusTarget.myTransform.localPosition.z; } else return 0;
                case 92: if (focusTarget != null) { return focusTarget.myTransform.rotation.x; } else return 0;
                case 93: if (focusTarget != null) { return focusTarget.myTransform.rotation.y; } else return 0;
                case 94: if (focusTarget != null) { return focusTarget.myTransform.rotation.z; } else return 0;
                case 95: if (focusTarget != null) { return focusTarget.myTransform.localRotation.x; } else return 0;
                case 96: if (focusTarget != null) { return focusTarget.myTransform.localRotation.y; } else return 0;
                case 97: if (focusTarget != null) { return focusTarget.myTransform.localRotation.z; } else return 0;
                case 98: if (focusTarget != null) { return focusTarget.myTransform.lossyScale.x; } else return 0;
                case 99: if (focusTarget != null) { return focusTarget.myTransform.lossyScale.y; } else return 0;
                case 100: if (focusTarget != null) { return focusTarget.myTransform.lossyScale.z; } else return 0;
                case 101: if (focusTarget != null) { return focusTarget.myTransform.localScale.x; } else return 0;
                case 102: if (focusTarget != null) { return focusTarget.myTransform.localScale.y; } else return 0;
                case 103: if (focusTarget != null) { return focusTarget.myTransform.localScale.z; } else return 0;
                case 104: if (focusTarget != null) { return myTransform.childCount; } else return 0;
                case 105: if (focusTarget != null && focusTarget.rigid != null) { return focusTarget.rigid.angularVelocity.magnitude; } else return 0;
                case 106: if (focusTarget != null && focusTarget.rigid != null) { return focusTarget.rigid.maxAngularVelocity; } else return 0;
                case 107: if (focusTarget != null && focusTarget.rigid != null) { return focusTarget.rigid.mass; } else return 0;
                case 108: if (focusTarget != null && focusTarget.rigid != null) { return focusTarget.rigid.drag; } else return 0;
                case 109: if (focusTarget != null && focusTarget.rigid != null) { return focusTarget.rigid.angularDrag; } else return 0;
                case 110: if (focusTarget != null && focusTarget.rigid != null) { return focusTarget.rigid.maxDepenetrationVelocity; } else return 0;
                case 111: if (focusTarget != null && focusTarget.rigid != null) { return focusTarget.rigid.velocity.magnitude; } else return 0;
                case 112: if (focusTarget != null && focusTarget.agent != null) { return focusTarget.agent.acceleration; } else return 0;
                case 113: if (focusTarget != null && focusTarget.agent != null) { return focusTarget.agent.angularSpeed; } else return 0;
                case 114: if (focusTarget != null && focusTarget.agent != null) { return focusTarget.agent.baseOffset; } else return 0;
                case 115: if (focusTarget != null && focusTarget.agent != null) { return focusTarget.agent.velocity.magnitude; } else return 0;
                case 116: if (focusTarget != null && focusTarget.agent != null) { return focusTarget.agent.speed; } else return 0;
                case 117: if (focusTarget != null && focusTarget.agent != null) { return focusTarget.agent.height; } else return 0;
                case 118: if (focusTarget != null && focusTarget.agent != null) { return focusTarget.agent.radius; } else return 0;
                case 119: if (focusTarget != null && focusTarget.agent != null) { return focusTarget.agent.stoppingDistance; } else return 0;
                case 120: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.dopplerLevel; } else return 0;
                case 121: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.minDistance; } else return 0;
                case 122: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.maxDistance; } else return 0;
                case 123: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.volume; } else return 0;
                case 124: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.panStereo; } else return 0;
                case 125: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.pitch; } else return 0;
                case 126: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.reverbZoneMix; } else return 0;
                case 127: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.spread; } else return 0;
                case 128: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.spatialBlend; } else return 0;
                case 129: if (focusTarget != null && focusTarget.audioSrc != null) { return focusTarget.audioSrc.time; } else return 0;
                case 130: if (focusTarget != null && focusTarget.animator != null) { return focusTarget.animator.GetFloat(variableName); } else return 0;
                case 131: if (focusTarget != null && focusTarget.animator != null) { return focusTarget.animator.GetInteger(variableName); } else return 0;
                case 132: if (focusTarget != null && focusTarget.animator != null) { return focusTarget.animator.speed; } else return 0;
                case 133: if (focusTarget != null && focusTarget.animator != null) { return focusTarget.animator.pivotWeight; } else return 0;
                case 134: if (focusTarget != null && focusTarget.animator != null) { return focusTarget.animator.playbackTime; } else return 0;
                case 135: if (focusTarget != null && focusTarget.animator != null) { return focusTarget.animator.leftFeetBottomHeight; } else return 0;
                case 136: if (focusTarget != null && focusTarget.animator != null) { return focusTarget.animator.rightFeetBottomHeight; } else return 0;
                case 137: if (focusTarget != null && focusTarget.animator != null) { return focusTarget.animator.angularVelocity.magnitude; } else return 0;
                case 138: if (focusTarget != null) { return focusTarget.maxRayDistance; } else return 0;
                case 139: if (focusTarget != null) { return focusTarget.raySpacing; } else return 0;
                case 140: if (focusTarget != null) { return focusTarget.rayCount; } else return 0;
                case 141: if (focusTarget != null) { return focusTarget.verticalScatter; } else return 0;
                case 142: if (focusTarget != null) { return focusTarget.horizontalScatter; } else return 0;
                case 143: if (focusTarget != null) { return focusTarget.maxCollisions; } else return 0;
                case 144: if (agent != null) { return agent.remainingDistance; } else return 0;
                case 145:  if (focusTarget != null && focusTarget.agent != null) { return focusTarget.agent.remainingDistance; } else return 0;
                case 146: return hits[hits.Length].normal.x;
                case 147: return hits[hits.Length].normal.y;
                case 148: return hits[hits.Length].normal.z;
                case 149: if (focusTarget != null) { return focusTarget.hits[focusTarget.hits.Length].normal.x; } else return 0;
                case 150: if (focusTarget != null) { return focusTarget.hits[focusTarget.hits.Length].normal.y; } else return 0;
                case 151: if (focusTarget != null) { return focusTarget.hits[focusTarget.hits.Length].normal.z; } else return 0;
                case 152: return myTransform.position.magnitude;
                case 153: if (focusTarget != null) { return focusTarget.myTransform.position.magnitude; } else return 0;
                case 154: return myTransform.localScale.magnitude;
                case 155: if (focusTarget != null) { return focusTarget.myTransform.localScale.magnitude; } else return 0;
                case 156: return myTransform.rotation.x + myTransform.rotation.y + myTransform.rotation.z;
                case 157: if (focusTarget != null) { return focusTarget.myTransform.rotation.x + focusTarget.myTransform.rotation.y + focusTarget.myTransform.rotation.z; } else return 0;
                default: return 0;
            }
        }

        public bool GetComponentBooleanValue(int index, string variableName = "", int variableID = 0)
        {
            // Note: Large switch cases are converted to lookup tables post compilation.
            // Note: Visual studio 2019+ line combine binding: Shift + Alt + L + J

            switch (index)
            {
                case 0: return true;
                case 1: return false;
                case 2:

                    if (booleanVariableLookup.ContainsKey(variableID) == true)
                    {
                        return booleanVariableLookup[variableID];
                    }
                    else
                    {
                       // Debug.LogWarningFormat("|Smart GameObject|: Variable: {0} not found when looking up a custom boolean variable.", variableID, gameObject);
                        return false;
                    }

                case 3: return gameObject.activeSelf;
                case 4: return gameObject.isStatic;
                case 5: return gameObject.CompareTag(variableName);
                case 6: return myTransform.parent == null;
                case 7: return rigid == null;
                case 8: return rigid != null && rigid.IsSleeping();
                case 9: return rigid != null && rigid.isKinematic;
                case 10: return agent == null;
                case 11: return agent != null && agent.enabled;
                case 12: return agent != null && agent.isOnNavMesh;
                case 13: return agent != null && agent.isOnOffMeshLink;
                case 14: return agent != null && agent.isPathStale;
                case 15: return agent != null && agent.isStopped;
                case 16: return agent != null && agent.pathPending;
                case 17: return rotating;
                case 18: return translating;
                case 19: return scaling;
                case 20: return usePath;
                case 21: return spin;
                case 22: return snappyScale;
                case 23: return useLocalSpace;
                case 24: return lookAtPath;
                case 25: return audioSrc == null;
                case 26: return audioSrc != null && audioSrc.enabled;
                case 27: return audioSrc != null && audioSrc.isPlaying;
                case 28: return audioSrc != null && audioSrc.isVirtual;
                case 29: return audioSrc != null && audioSrc.loop;
                case 30: return audioSrc != null && audioSrc.mute;
                case 31: return audioSrc != null && audioSrc.spatialize;
                case 32: return audioSrc != null && audioSrc.spatializePostEffects;
                case 33: return animator == null;
                case 34: return animator != null && animator.enabled;
                case 35: return animator != null && animator.applyRootMotion;
                case 36: return animator != null && animator.fireEvents;
                case 37: return animator != null && animator.GetBool(variableName);
                case 38: return animator != null && animator.isHuman;
                case 39: return animator != null && animator.isInitialized;
                case 40: return animator != null && animator.isMatchingTarget;
                case 41: return animator != null && animator.isOptimizable;
                case 42: return animator != null && animator.stabilizeFeet;
                case 43: return animator != null && animator.IsInTransition(variableID);
                case 44: return targets.Count > 0;
                case 45:

                    foreach(SmartGameObject SmartGO in targets)
                    {
                        if(SmartGO.gameObject.CompareTag(variableName) == true)
                        {
                            return true;
                        }
                    }
                    return false;

                case 46: return colliders.Count > 0;
                case 47: return hits.Length > 0;
                case 48:

                    if (focusTarget != null)
                    {
                        if (focusTarget.booleanVariableLookup.ContainsKey(variableID) == true)
                        {
                            return focusTarget.booleanVariableLookup[variableID];
                        }
                        else
                        {
                           // Debug.LogWarningFormat("|Smart GameObject|: Variable ID: {0} not found when looking up a custom boolean variable.", variableID, gameObject);
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                case 49: return focusTarget == null;
                case 50: return focusTarget != null && focusTarget.gameObject.activeSelf;
                case 51: return focusTarget != null && focusTarget.gameObject.isStatic;
                case 52: return focusTarget != null && focusTarget.gameObject.CompareTag(variableName);
                case 53: return focusTarget != null && focusTarget.myTransform.parent == null;
                case 54: return focusTarget != null && focusTarget.rigid == null;
                case 55: return focusTarget != null && focusTarget.rigid != null && focusTarget.rigid.IsSleeping();
                case 56: return focusTarget != null && focusTarget.rigid != null && focusTarget.rigid.isKinematic;
                case 57: return focusTarget != null && focusTarget.agent == null;
                case 58: return focusTarget != null && agent != null && focusTarget.agent.enabled;
                case 59: return focusTarget != null && agent != null && focusTarget.agent.isOnNavMesh;
                case 60: return focusTarget != null && agent != null && focusTarget.agent.isOnOffMeshLink;
                case 61: return focusTarget != null && agent != null && focusTarget.agent.isPathStale;
                case 62: return focusTarget != null && agent != null && focusTarget.agent.isStopped;
                case 63: return focusTarget != null && agent != null && focusTarget.agent.pathPending;
                case 64: return focusTarget != null && focusTarget.rotating;
                case 65: return focusTarget != null && focusTarget.translating;
                case 66: return focusTarget != null && focusTarget.scaling;
                case 67: return focusTarget != null && focusTarget.usePath;
                case 68: return focusTarget != null && focusTarget.spin;
                case 69: return focusTarget != null && focusTarget.snappyScale;
                case 70: return focusTarget != null && focusTarget.useLocalSpace;
                case 71: return focusTarget != null && focusTarget.lookAtPath;
                case 72: return focusTarget != null && focusTarget.audioSrc == null;
                case 73: return focusTarget != null && focusTarget.audioSrc != null && focusTarget.audioSrc.enabled;
                case 74: return focusTarget != null && focusTarget.audioSrc != null && focusTarget.audioSrc.isPlaying;
                case 75: return focusTarget != null && focusTarget.audioSrc != null && focusTarget.audioSrc.isVirtual;
                case 76: return focusTarget != null && focusTarget.audioSrc != null && focusTarget.audioSrc.loop;
                case 77: return focusTarget != null && focusTarget.audioSrc != null && focusTarget.audioSrc.mute;
                case 78: return focusTarget != null && focusTarget.audioSrc != null && focusTarget.audioSrc.spatialize;
                case 79: return focusTarget != null && focusTarget.audioSrc != null && focusTarget.audioSrc.spatializePostEffects;
                case 80: return focusTarget != null && focusTarget.animator == null;
                case 81: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.enabled;
                case 82: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.applyRootMotion;
                case 83: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.fireEvents;
                case 84: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.GetBool(variableName);
                case 85: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.isHuman;
                case 86: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.isInitialized;
                case 87: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.isMatchingTarget;
                case 88: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.isOptimizable;
                case 89: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.stabilizeFeet;
                case 90: return focusTarget != null && focusTarget.animator != null && focusTarget.animator.IsInTransition(variableID);
                case 91: return focusTarget != null && focusTarget.targets.Count > 0;
                case 92:

                    if (focusTarget != null)
                    {
                        foreach (SmartGameObject SmartGO in focusTarget.targets)
                        {
                            if (SmartGO.gameObject.CompareTag(variableName) == true)
                            {
                                return true;
                            }
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }

                case 93: return focusTarget != null && focusTarget.colliders.Count > 0;
                case 94: return focusTarget != null && focusTarget.hits.Length > 0;
                case 95: return focusTarget != null && focusTarget.focusTarget == null;
                case 96: return directLook;
                case 97: return horizontalLook;
                case 98: return focusTarget != null && directLook;
                case 99: return focusTarget != null && horizontalLook;
                case 100: return lookAtTarget == null;
                case 101: return focusTarget != null && lookAtTarget == null;
                case 102: return agent != null && agent.autoBraking;
                case 103: return focusTarget != null && agent != null && focusTarget.agent.autoBraking;
                case 104: return agent != null && Vector3.Distance(agent.destination, agent.transform.position) <= agent.stoppingDistance && (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
                case 105: return focusTarget != null && agent != null && Vector3.Distance(focusTarget.agent.destination, focusTarget.agent.transform.position) <= focusTarget.agent.stoppingDistance && (!focusTarget.agent.hasPath || focusTarget.agent.velocity.sqrMagnitude == 0f);
                case 106: return floatVariableLookup.ContainsKey(variableID);
                case 107: return focusTarget != null && focusTarget.floatVariableLookup.ContainsKey(variableID);
                case 108: return booleanVariableLookup.ContainsKey(variableID);
                case 109: return focusTarget != null && focusTarget.booleanVariableLookup.ContainsKey(variableID);
                default: return false;
            }
        }

        public void PerformBooleanOperation(ref bool leftSide, BooleanComparisons booleanComparison, bool rightSide)
        {
            switch(booleanComparison)
            {
                case BooleanComparisons.EqualTo:

                    leftSide = rightSide;

                    break;

                case BooleanComparisons.NotEqualTo:

                    leftSide = !rightSide;

                    break;
            }
        }

        public void PerformNumericalOperation(ref float leftSide, NumericalOperators numericalOperator, float rightSide)
        {
            switch (numericalOperator)
            {
                case NumericalOperators.SetEqualTo:

                    leftSide = rightSide;

                    break;

                case NumericalOperators.Power:

                    leftSide = Mathf.Pow(leftSide, rightSide);

                    break;

                case NumericalOperators.Multiply:

                    leftSide *= rightSide;

                    break;


                case NumericalOperators.Divide:

                    if(rightSide != 0)
                    {
                        leftSide /= rightSide;
                    }
                    else
                    {
                        Debug.LogWarning("|Smart GameObject|: Attempted to divide by zero. Skipping operation...", gameObject);
                    }

                    break;

                case NumericalOperators.Add:

                    leftSide += rightSide;

                    break;


                case NumericalOperators.Subtract:

                    leftSide -= rightSide;

                    break;
            }
        }

        public void DisableBehavior(int index)
        {
            if(index >= 0 && index < behaviors.Count)
            {
                behaviors[index].enabled = false;
            }
        }
        public void EnableBehavior(int index)
        {
            if (index >= 0 && index < behaviors.Count)
            {
                behaviors[index].enabled = true;
            }
        }
        public void EnableAllBehaviors()
        {
            foreach(SmartBehavior b in behaviors)
            {
                b.enabled = true;
            }
        }
        public void AddBehavior(SmartBehavior behavior)
        {
            behaviors.Add(behavior);
        }
        public void AddBehavior(SmartBehaviorScriptableObject behaviorSO)
        {
            behaviors.Add(behaviorSO.behavior);
        }
        public SmartBehavior GetBehavior(int index)
        {
            if(index < behaviors.Count && index >= 0)
            {
                return behaviors[index];
            }

            return behaviors[0];
        }

        public void SetBehavior(int index, SmartBehavior behavior)
        {
            if (index < behaviors.Count && index >= 0)
            {
                behaviors[index] = behavior;
            }
        }

        public void SetBehavior(int index, SmartBehaviorScriptableObject behaviorSO)
        {
            if (index < behaviors.Count && index >= 0)
            {
                behaviors[index] = behaviorSO.behavior;
            }
        }

        #endregion

        #region Variable Methods:

        public void AddFloatVariable(string varName, float initialValue)
        {
            tempFloatVar.name = varName;
            tempFloatVar.hash = tempFloatVar.name.GetHashCode();
            if (!floatVariableLookup.ContainsKey(tempFloatVar.hash))
            {
                tempFloatVar.initialValue = initialValue;
                floatVariableLookup.Add(tempFloatVar.hash, tempFloatVar.initialValue);
            }
            else
            {
                Debug.LogWarning("|Smart GameObject|: Tried to add new float variable but a variable of the same name already exists.", gameObject);
            }
        }

        public float GetFloatVariableValueByName(string name)
        {
            if (floatVariableLookup.ContainsKey(name.GetHashCode()))
            {
                return floatVariableLookup[name.GetHashCode()];
            }
            else
            {
                Debug.LogWarning("|Smart GameObject|: Tried to get a variable by name but the Smart GameObject didn't have a variable by that name; returning zero.", gameObject);
                return 0;
            }
        }

        public void SetFloatVariableByName(string name, float value)
        {
            if (floatVariableLookup.ContainsKey(name.GetHashCode()))
            {
                floatVariableLookup[name.GetHashCode()] = value;
            }
            else
            {
                Debug.LogWarning("|Smart GameObject|: Tried to set a variable by name but the Smart GameObject didn't have a variable by that name.", gameObject);
            }
        }

        public void ResetAllFloatVariableValues()
        {
            foreach (FloatVariable floatVar in floatVariables)
            {
                if (floatVariableLookup.ContainsKey(floatVar.hash))
                {
                    floatVariableLookup[floatVar.hash] = floatVar.initialValue;
                }
            }
        }

        public void ResetAllBooleanVariableValues()
        {
            foreach (BooleanVariable boolVar in booleanVariables)
            {
                if (booleanVariableLookup.ContainsKey(boolVar.hash))
                {
                    booleanVariableLookup[boolVar.hash] = boolVar.initialValue;
                }
            }
        }

        private void CreateBooleanVariableDictionary()
        {
            if (booleanVariables.Count > 0)
            {
                booleanVariableLookup.Clear();
                for (int i = 0; i < booleanVariables.Count; i++)
                {
                    if (!string.IsNullOrEmpty(booleanVariables[i].name))
                    {
                        tempBoolVar.initialValue = booleanVariables[i].initialValue;
                        tempBoolVar.name = booleanVariables[i].name;
                        tempBoolVar.hash = tempBoolVar.name.GetHashCode();

                        if (!booleanVariableLookup.ContainsKey(tempBoolVar.hash))
                        {
                            booleanVariableLookup.Add(tempBoolVar.hash, tempBoolVar.initialValue);
                            booleanVariables[i] = tempBoolVar;
                        }
                        else
                        {
                            Debug.LogWarning("|Smart GameObject|: duplicate boolean variable name detected, ignoring.", gameObject);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("|Smart GameObject|: boolean variable has unassigned display name, ignoring.", gameObject);
                    }
                }
            }
        }

        private void CreateFloatVariableDictionary()
        {
            if (floatVariables.Count > 0)
            {
                floatVariableLookup.Clear();
                for (int i = 0; i < floatVariables.Count; i++)
                {
                    if (!string.IsNullOrEmpty(floatVariables[i].name))
                    {
                        tempFloatVar.initialValue = floatVariables[i].initialValue;
                        tempFloatVar.name = floatVariables[i].name;
                        tempFloatVar.hash = tempFloatVar.name.GetHashCode();

                        if (!floatVariableLookup.ContainsKey(tempFloatVar.hash))
                        {
                            floatVariableLookup.Add(tempFloatVar.hash, tempFloatVar.initialValue);
                            floatVariables[i] = tempFloatVar;
                        }
                        else
                        {
                            Debug.LogWarning("|Smart GameObject|: duplicate float variable name detected, ignoring.", gameObject);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("|Smart GameObject|: float variable has unassigned display name, ignoring.", gameObject);
                    }
                }
            }
        }

        #endregion

        #region Interaction Methods:

        /// <summary>
        /// Tells the physics engine to ignore/detect collisions between a list of colliders and this GameObject's collider.
        /// </summary>
        /// <param name="ignoreList">Colliders to ignore/detect.</param>
        /// <param name="ignore">Should the colliders in the ignore list be ignored or detected? (true/false)</param>
        public void IgnoreColliders(List<Collider> ignoreList, bool ignore)
        {
            // Note: Prior to Unity 5.5.0b3 calling this could reset the trigger state of a collider in PhysX.
            // See: https://forum.unity.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/

#if UNITY_5_5_OR_NEWER
            if(col != null)
            {
                foreach (Collider ignored in ignoreList)
                {
                    Physics.IgnoreCollision(col, ignored, ignore);
                }
            }
#else

// Do nothing... 
// TODO: is backward compatibility to support triggers possible?

#endif
        }

        public void TargetLastContact()
        {
            if(lastContact != null)
            {
                lastTargeted = lastContact.gameObject.GetComponent<SmartGameObject>();
                if (!targets.Contains(lastTargeted))
                {
                    targets.Add(lastTargeted);

                }
            }
        }

        public void TargetLastCollision()
        {
            if(lastCollision != null)
            {
                lastTargeted = lastCollision.gameObject.GetComponent<SmartGameObject>();
                if (!targets.Contains(lastTargeted))
                {
                    targets.Add(lastTargeted);
                }
            }
        }

        public void TargetColliders()
        {
            foreach (Collider collider in colliders)
            {
                lastTargeted = collider.gameObject.GetComponent<SmartGameObject>();
                if (lastTargeted != null && !targets.Contains(lastTargeted))
                {
                    targets.Add(lastTargeted);
                }
            }
        }

        public void RemoveNullTargets()
        {
            while(targets.Contains(null))
            {
                targets.Remove(null);
            }
        }

        public void TargetCollidersWithTag(string requiredTag)
        {
            foreach (Collider collider in colliders)
            {
                if(collider.CompareTag(requiredTag) == true)
                {
                    lastTargeted = collider.gameObject.GetComponent<SmartGameObject>();
                    if (lastTargeted != null && !targets.Contains(lastTargeted))
                    {
                        targets.Add(lastTargeted);
                    }
                }
            }
        }

        public void TargetRaycastHitsWithTag(string requiredTag)
        {
            foreach (RaycastHit hit in hits)
            {
                if(hit.collider.CompareTag(requiredTag) == true)
                {
                    lastTargeted = hit.collider.gameObject.GetComponent<SmartGameObject>();
                    if (lastTargeted != null && !targets.Contains(lastTargeted))
                    {
                        targets.Add(lastTargeted);
                    }
                }
            }
        }

        public void TargetRaycastHits()
        {
            foreach (RaycastHit hit in hits)
            {
                lastTargeted = hit.collider.gameObject.GetComponent<SmartGameObject>();
                if (lastTargeted != null && !targets.Contains(lastTargeted))
                {
                    targets.Add(lastTargeted);
                }
            }
        }

        public void FocusOnSmartGameObject(SmartGameObject smartGO)
        {
            focusTarget = smartGO;
        }

        public void TargetSmartGameObject(SmartGameObject smartGO)
        {
            if(smartGO != null && !targets.Contains(smartGO))
            {
                targets.Add(smartGO);
            }
        }

        public void GetNearestTarget()
        {
            nearestTarget = null;
            float nearDistance = float.PositiveInfinity;
            foreach (SmartGameObject smartGO in targets)
            {
                tempFloat = Vector3.Distance(myTransform.position, smartGO.myTransform.position);
                if (tempFloat < nearDistance)
                {
                    nearestTarget = smartGO;
                    nearDistance = tempFloat;
                }
            }
        }

        public void GetNearestTargetWithTag(string tag)
        {
            nearestTarget = null;
            float nearDistance = float.PositiveInfinity;
            foreach (SmartGameObject smartGO in targets)
            {
                if (smartGO.CompareTag(tag) == true && Vector3.Distance(myTransform.position, smartGO.myTransform.position) < nearDistance)
                {
                    nearestTarget = smartGO;
                }
            }
        }

        public void GetFarthestTarget()
        {
            farthestTarget = null;
            float farDistance = float.NegativeInfinity;
            foreach (SmartGameObject smartGO in targets)
            {
                tempFloat = Vector3.Distance(myTransform.position, smartGO.myTransform.position);
                if (tempFloat > farDistance)
                {
                    farthestTarget = smartGO;
                    farDistance = tempFloat;
                }
            }
        }

        public void GetFarthestTargetWithTag(string tag)
        {
            farthestTarget = null;
            float farDistance = float.NegativeInfinity;
            foreach (SmartGameObject smartGO in targets)
            {
                if (smartGO.CompareTag(tag) == true && Vector3.Distance(myTransform.position, smartGO.myTransform.position) > farDistance)
                {
                    farthestTarget = smartGO;
                }
            }
        }

        public void Richochet()
        {
            if(lastCollision != null)
            {
                rigid.velocity = Vector3.zero;
                Vector3 richochet = -lastCollision.GetContact(0).normal;
                myTransform.LookAt(richochet);
                rigid.AddForce(richochet, ForceMode.Impulse);
            }
        }

        public void Seek(Transform target, float speed)
        {
            if(rigid != null)
            {
                myTransform.LookAt(target);
                rigid.AddForce((target.position - myTransform.position).normalized * speed);
            }
        }

        public void UpdateRaycastHitCache()
        {
            if(hits.Length != rayCount)
            {
                hits = new RaycastHit[rayCount];
            }
        }

        public void RaycastSingle()
        {
            tempRay.origin = myTransform.position;
            tempRay.direction = myTransform.forward;
            Physics.RaycastNonAlloc(tempRay, hits, maxRayDistance, layerMask, triggerInteraction);
        }

        public void RaycastAll()
        {
            tempRay.origin = myTransform.position;
            tempRay.direction = myTransform.forward;
            hits = Physics.RaycastAll(tempRay);
        }

        public void SphereCast()
        {
            tempRay.origin = myTransform.position;
            tempRay.direction = myTransform.forward;
            Physics.SphereCastNonAlloc(tempRay, (raySpacing / 2), hits);
        }

        public void SphereCastAll()
        {
            tempRay.origin = myTransform.position;
            tempRay.direction = myTransform.forward;
            hits = Physics.SphereCastAll(tempRay, (raySpacing / 2));
        }

        public void BoxCastAll()
        {
            hits = Physics.BoxCastAll(myTransform.position, Vector3.one * (raySpacing / 2), myTransform.forward, Quaternion.identity, maxRayDistance, layerMask, triggerInteraction);
        }

        public void BoxCast()
        {
            Physics.BoxCastNonAlloc(myTransform.position, Vector3.one * (raySpacing / 2), myTransform.forward, hits, Quaternion.identity, maxRayDistance, layerMask, triggerInteraction);
        }

        public void CapsuleCast()
        {
            Physics.CapsuleCastNonAlloc(myTransform.position, myTransform.up, (raySpacing / 2), Vector3.forward, hits);
        }

        public void LineCast()
        {
            Physics.Linecast(myTransform.position, Vector3.forward * maxRayDistance, layerMask, triggerInteraction);
        }

        public void RaycastScatter()
        {
            UpdateRaycastHitCache();
            for (int i = 0; i < rayCount; i++)
            {
                tempRay.origin = myTransform.position;
                tempRay.direction = Quaternion.Euler(UnityEngine.Random.Range(-verticalScatter, verticalScatter), UnityEngine.Random.Range(-horizontalScatter, horizontalScatter), 0) * myTransform.forward;
                Physics.RaycastNonAlloc(tempRay, hits, maxRayDistance, layerMask, triggerInteraction);
            }
        }

        public void RaycastRow()
        {
            UpdateRaycastHitCache();
            for (int i = 0; i < rayCount; i++)
            {
                tempRay.origin = myTransform.position + ((i - (rayCount - 1) / 2f) * raySpacing * myTransform.right);
                tempRay.direction = myTransform.forward;
                Physics.RaycastNonAlloc(tempRay, hits, maxRayDistance, layerMask, triggerInteraction);
            }
        }

        public void RaycastGrid()
        {
            int gridSize = (int)Mathf.Sqrt(rayCount);
            hits = new RaycastHit[gridSize * gridSize];
            Vector3[] rayOrigins = new Vector3[gridSize * gridSize];

            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    rayOrigins[i * gridSize + j] = myTransform.position + new Vector3((i - (gridSize - 1) / 2f) * raySpacing, (j - (gridSize - 1) / 2f) * raySpacing, 0);
                }
            }

            for (int i = 0; i < rayOrigins.Length; i++)
            {
                tempRay.origin = rayOrigins[i];
                tempRay.direction = myTransform.forward;
                Physics.RaycastNonAlloc(tempRay, hits, maxRayDistance, layerMask, triggerInteraction);
            }
        }

        public void RaycastCircle()
        {
            UpdateRaycastHitCache();
            for (int i = 0; i < rayCount; i++)
            {
                tempRay.origin = myTransform.position + Quaternion.LookRotation(myTransform.up, myTransform.forward) * Quaternion.Euler(0, i * (360f / rayCount), 0) * myTransform.right * rayCount * raySpacing;
                tempRay.direction = myTransform.forward;
                Physics.RaycastNonAlloc(tempRay, hits, maxRayDistance, layerMask, triggerInteraction);
            }
        }

        public void RaycastFan()
        {
            UpdateRaycastHitCache();
            for (int i = 0; i < rayCount; i++)
            {
                tempRay.origin = myTransform.position;
                tempRay.direction = Quaternion.AngleAxis(Mathf.Clamp(180f / rayCount * i - 90f, -180f, 180f), myTransform.up) * myTransform.forward;
                Physics.RaycastNonAlloc(tempRay, hits, maxRayDistance, layerMask, triggerInteraction);
            }
        }

        #endregion

        #region Motion Methods:

        public void ClearTransformValues()
        {
            myTransform.position = Vector3.zero;
            myTransform.rotation = Quaternion.identity;
            myTransform.localScale = Vector3.one;
        }
        public void SetTransformValuesToInitial()
        {
            myTransform.position = initialPos;
            myTransform.rotation = initialRot;
            myTransform.localScale = initialScale;
        }

        public void StartTranslation()
        {
            if(translating == false)
            {
                previousPosition = myTransform.position;
                initialTranslationTarget = translationTarget;
                translating = true;
            }
            else
            {
                Debug.LogWarning("|Smart GameObject|: Tried to start a translation while translating, stop the translation first or pause the translation instead.", gameObject);
            }
        }

        public void PauseTranslation()
        {
            translationPaused = true;
        }

        public void ResumeTranslation()
        {
            translationPaused = false;
        }

        public void StopTranslation()
        {
            translating = false;
            translationPaused = false;
            translationTime = 0;
            translationTarget = initialTranslationTarget;
            previousPosition = Vector3.negativeInfinity;
        }
        public void StartRotation()
        {
            if (rotating == false)
            {
                previousRotation = myTransform.rotation;
                initialRotationTarget = rotationTarget;
                rotating = true;
            }
            else
            {
                Debug.LogWarning("|Smart GameObject|: Tried to start a rotation while rotateing, stop the rotation first or pause the rotation instead.", gameObject);
            }
        }

        public void PauseRotation()
        {
            rotationPaused = true;
        }

        public void ResumeRotation()
        {
            rotationPaused = false;
        }

        public void StopRotation()
        {
            rotating = false;
            rotationPaused = false;
            rotationTime = 0;
            rotationTarget = initialRotationTarget;
            previousRotation = Quaternion.identity;
        }

        public void StartScale()
        {
            if (scaling == false)
            {
                previousScale = myTransform.localScale;
                initialScaleTarget = scaleTarget;
                scaling = true;
            }
            else
            {
                Debug.LogWarning("|Smart GameObject|: Tried to start a scale while scaling, stop the scale first or pause the scale instead.", gameObject);
            }
        }

        public void PauseScale()
        {
            scalingPaused = true;
        }

        public void ResumeScale()
        {
            scalingPaused = false;
        }

        public void StopScale()
        {
            scaling = false;
            scalingPaused = false;
            scaleTime = 0;
            scaleTarget = initialScaleTarget;
            previousScale = Vector3.negativeInfinity;
        }

        public void StartPathTranslation()
        {
            if(splines.Count > 0 && useLocalSpace == false)
            {
                if(path.Count > 0)
                {
                    usePath = true;
                    translationTime = 0;
                    currentPathPoint = 0;
                    translationTarget = path[0];
                    translating = true;
                }
                else
                {
                    Debug.LogWarning("|Smart GameObject|: Tried to start a path translation but a path has not been generated, try calling RecalculatePath().", gameObject);
                }
            }
            else
            {
                Debug.LogWarning("|Smart GameObject|: Tried to start a path translation but no splines have been defined to create a path from.", gameObject);
            }
        }

        public void StopPathTranslation()
        {
            translating = false;
            translationPaused = false;
            translationTime = 0;
            currentPathPoint = 0;
            translationTarget = path[0];
            pathReversed = false;
        }

        public void RecalculatePath()
        {
            if (splines.Count > 0 && path.Count == 0)
            {
                path.Clear();
                foreach (Spline spline in splines)
                {
                    CreateSplinePath(spline);
                }
            }

            if (currentPathPoint > path.Count)
            {
                currentPathPoint = path.Count - 1;
            }
        }

        private void UpdateTweens()
        {
            if (useLocalSpace == true)
            {
                UpdatePathPosition();
            }

            if (lookAtTarget != null)
            {
                // Billboard:
                if (lookAtPos != lookAtTarget.position || currentPosition != transform.position || transform.rotation != currentRotation)
                {
                    if (directLook == true)
                    {
                        lookAtPos = 2 * transform.position - lookAtTarget.position;
                        if (horizontalLook == true)
                        {
                            lookAtPos.y = transform.position.y;
                        }
                    }
                    else
                    {
                        if (horizontalLook == false)
                        {
                            lookAtPos = lookAtTarget.position;
                        }
                        else
                        {
                            lookAtPos = new Vector3(lookAtTarget.position.x, transform.position.y, lookAtTarget.position.z);
                        }
                    }
                    transform.LookAt(lookAtPos);
                    currentPosition = transform.position;
                    currentRotation = transform.rotation;
                }
            }
            else
            {
                // Rotate:
                if (rotating == true && rotationPaused == false)
                {
                    if (spin == false)
                    {
                        if (rotationTime < rotationCurve.keys[rotationCurve.length - 1].time)
                        {
                            rotationTime += Time.deltaTime;
                            nextRotation = Quaternion.Lerp(transform.rotation, rotationTarget, rotationCurve.Evaluate(rotationTime));
                            transform.rotation = nextRotation;
                        }
                        else
                        {
                            switch (rotationCurve.postWrapMode)
                            {
                                case WrapMode.PingPong:

                                    if (rotationTarget == previousRotation)
                                    {
                                        rotationTarget = initialRotationTarget;
                                    }
                                    else
                                    {
                                        rotationTarget = previousRotation;
                                    }
                                    rotationTime = 0;

                                    break;


                                case WrapMode.Loop:

                                    myTransform.rotation = initialRotationTarget;
                                    rotationTime = 0;

                                    break;


                                case WrapMode.Once:

                                    myTransform.rotation = initialRotationTarget;
                                    StopRotation();

                                    break;

                                case WrapMode.ClampForever:

                                    myTransform.rotation = rotationTarget;

                                    break;

                                case WrapMode.Default:

                                    rotationTime = 0;

                                    break;

                                default:

                                    rotationTime = 0;

                                    break;
                            }
                        }
                    }
                    else
                    {
                        transform.Rotate(rotationTarget.eulerAngles, rotationCurve.keys[rotationCurve.keys.Length - 1].value * rotationCurve.keys[rotationCurve.keys.Length - 1].time);
                    }
                }
            }

            // Translate:
            if (translating == true && translationPaused == false)
            {
                if (usePath == false)
                {
                    if (translationTime < translationCurve.keys[translationCurve.length - 1].time)
                    {
                        translationTime += Time.deltaTime;
                        nextPosition = Vector3.Lerp(myTransform.position, translationTarget, translationCurve.Evaluate(translationTime));
                        myTransform.position = nextPosition;
                    }
                    else
                    {
                        switch (translationCurve.postWrapMode)
                        {
                            case WrapMode.PingPong:

                                if (translationTarget == previousPosition)
                                {
                                    translationTarget = initialTranslationTarget;
                                }
                                else
                                {
                                    translationTarget = previousPosition;
                                }
                                translationTime = 0;

                                break;


                            case WrapMode.Loop:

                                myTransform.position = initialTranslationTarget;
                                translationTime = 0;

                                break;


                            case WrapMode.Once:

                                myTransform.position = initialTranslationTarget;
                                StopTranslation();

                                break;

                            case WrapMode.ClampForever:

                                myTransform.position = translationTarget;

                                break;

                            case WrapMode.Default:

                                translationTime = 0;

                                break;

                            default:

                                translationTime = 0;

                                break;
                        }
                    }
                }
                else
                {
                    // Translate along path:
                    if (translationTime < pathCurve.keys[pathCurve.length -1].time)
                    {
                        translationTime += Time.deltaTime;
                        nextPosition = Vector3.Lerp(myTransform.position, path[currentPathPoint], pathCurve.Evaluate(translationTime));
                        if(lookAtPath == true)
                        {
                            myTransform.LookAt(path[currentPathPoint]);
                        }
                        myTransform.position = nextPosition;
                    }
                    else
                    {
                        if (pathReversed == false)
                        {
                            if (currentPathPoint == path.Count - 1)
                            {
                                switch (pathCurve.postWrapMode)
                                {
                                    case WrapMode.PingPong:

                                        pathReversed = true;

                                        break;

                                    case WrapMode.Clamp:

                                        StopPathTranslation();

                                        break;

                                    case WrapMode.Loop:

                                        currentPathPoint = (currentPathPoint + 1) % path.Count;

                                        break;
                                }
                            }
                            else
                            {
                                currentPathPoint = (currentPathPoint + 1) % path.Count;
                            }
                        }
                        else
                        {
                            if (currentPathPoint == 0)
                            {
                                pathReversed = false;
                            }
                            else
                            {
                                currentPathPoint--;
                            }
                        }
                        translationTime = 0;
                    }

                }
            }

            // Scale:
            if (scaling == true && scalingPaused == false)
            {
                if (scaleTime < scaleCurve.keys[scaleCurve.length - 1].time)
                {
                    scaleTime += Time.deltaTime;
                    nextScale = Vector3.Lerp(transform.localScale, scaleTarget, scaleCurve.Evaluate(scaleTime));
                    if (snappyScale == true)
                    {
                        nextScale = new Vector3(Mathf.RoundToInt(nextScale.x), Mathf.RoundToInt(nextScale.y), Mathf.RoundToInt(nextScale.z));
                    }
                    transform.localScale = nextScale;
                }
                else
                {
                    switch (scaleCurve.postWrapMode)
                    {
                        case WrapMode.PingPong:

                            if (scaleTarget == previousScale)
                            {
                                scaleTarget = initialScaleTarget;
                            }
                            else
                            {
                                scaleTarget = previousScale;
                            }
                            scaleTime = 0;

                            break;


                        case WrapMode.Loop:

                            myTransform.localScale = initialScaleTarget;
                            scaleTime = 0;

                            break;


                        case WrapMode.Once:

                            myTransform.localScale = initialScaleTarget;
                            StopScale();

                            break;

                        case WrapMode.ClampForever:

                            myTransform.localScale = scaleTarget;

                            break;

                        case WrapMode.Default:

                            scaleTime = 0;

                            break;

                        default:

                            scaleTime = 0;

                            break;
                    }
                }
            }
        }
        public void SetTranslationTarget(Vector3 v3)
        {
            translationTarget = v3;
        }   

        public void UpdatePathPosition()
        {
            for(int i = 0; i < path.Count; i++)
            {
                path[i] = path[i] += myTransform.localPosition;
            }
        }

        private void CreateSplinePath(Spline spline)
        {
            tempPath.Clear();
            for (float t = 0f; t <= 1.001f; t += 0.05f)
            {
                switch (spline.splineType)
                {
                    case SplineTypes.Bezier:

                        tempVector = SmartSplines.CalculateBezierPoint(t, spline.startPoint, spline.controlPoint2, spline.controlPoint1, spline.endPoint);

                        break;

                    case SplineTypes.Hermite:

                        tempVector = SmartSplines.CalculateHermitePoint(t, spline.startPoint, spline.endPoint, spline.controlPoint1, spline.controlPoint2);

                        break;

                    case SplineTypes.CatmullRom:

                        tempVector = SmartSplines.CalculateCatmullRomPoint(t, spline.controlPoint1, spline.startPoint, spline.endPoint, spline.controlPoint2);

                        break;

                    case SplineTypes.BSpline:

                        tempVector = SmartSplines.CalculateBSplinePoint(t, spline.controlPoint1, spline.controlPoint2, spline.startPoint, spline.endPoint);

                        break;

                    case SplineTypes.Linear:

                        tempVector = SmartSplines.CalculateLinearPoint(t, spline.startPoint, spline.endPoint);

                        break;

                    case SplineTypes.Raw:

                        if (useLocalSpace == true)
                        {
                            tempVector = myTransform.localPosition;
                            tempPath.Add(spline.startPoint + tempVector);
                            tempPath.Add(spline.controlPoint1 + tempVector);
                            tempPath.Add(spline.controlPoint2 + tempVector);
                            tempVector = spline.endPoint + myTransform.localPosition;
                        }
                        else
                        {
                            tempPath.Add(spline.startPoint);
                            tempPath.Add(spline.controlPoint1);
                            tempPath.Add(spline.controlPoint2);
                            tempVector = spline.endPoint;
                        }

                        break;
                }

                if (useLocalSpace == true)
                {
                    tempVector += myTransform.localPosition;
                }
                tempPath.Add(tempVector);
            }

            if (spline.equalize == true)
            {
                if(spline.equalizedSegments == 0)
                {
                    spline.equalizedSegments = tempPath.Count  * 2;
                }

                if (tempPath.Count >= 2 && spline.equalizedSegments > 1)
                {
                    tempPath = SmartSplines.GetEvenlySpacedPoints(tempPath, spline.equalizedSegments);
                }
                else
                {
                    Debug.LogError("|Smart GameObject|: Failed to equalize path, path points and resolution should be equal to or greater than 2.", gameObject);
                }
            }

            foreach (Vector3 point in tempPath)
            {
                path.Add(point);
            }
        }

        #endregion

        #region AI Methods:

        public void AIWander(float wanderRange)
        {
            if(agent != null)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition((UnityEngine.Random.insideUnitSphere * wanderRange) + myTransform.position, out hit, wanderRange, -1) && gameObject.activeSelf == true)
                {
                    agent.SetDestination(hit.position);
                }
            }
        }

        public void AIFleeFocusTarget(float fleeDistance)
        {
            if(agent != null && focusTarget != null)
            {
                agent.SetDestination(focusTarget.myTransform.forward * fleeDistance);
            }
        }

        public void AIIdle()
        {
            if(agent != null)
            {
                agent.autoBraking = true;
                agent.SetDestination(myTransform.position);
            }
        }

        public void AIFollowFocusTarget(float followDistance)
        {
            if (agent != null && focusTarget != null && (followDistance <= Vector3.Distance(myTransform.position, focusTarget.myTransform.position)))
            {
                agent.SetDestination(focusTarget.myTransform.position + (-focusTarget.myTransform.forward * followDistance));
                agent.transform.LookAt(focusTarget.myTransform.position);
                agent.stoppingDistance = agent.radius * followDistance;
            }
        }

        public void AIFollowPath()
        {
            if(agent != null && path.Count > 0)
            {
                if(agent.remainingDistance <= Mathf.Epsilon && pathProgress < path.Count)
                {
                    agent.SetDestination(path[pathProgress++]);
                }
                else
                {
                    if (pathProgress >= path.Count)
                    {
                        pathProgress = 0;
                    }
                    agent.SetDestination(path[pathProgress]);
                }       
            }
        }

        public void AIFollowFocusTargetPath()
        {
            if (agent != null && focusTarget != null && focusTarget.path.Count > 0)
            {
                if (agent.remainingDistance <= Mathf.Epsilon && pathProgress < focusTarget.path.Count)
                {
                    agent.SetDestination(focusTarget.path[pathProgress++]);
                }
                else
                {
                    if (pathProgress >= path.Count)
                    {
                        pathProgress = 0;
                    }
                    agent.SetDestination(focusTarget.path[pathProgress]);
                }
            }
        }

        #endregion

        #region Spawn Methods:

        public void SpawnAtTransform(Transform t, string poolName)
        {
            if(t != null)
            {
                lastSpawned = SmartManager.Singleton.GetPooledObject(poolName);
                if (lastSpawned != null)
                {
                    lastSpawned.transform.position = t.position;
                    lastSpawned.transform.rotation = t.rotation;
                    lastSpawned.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("|Smart GameObject|: Tried to spawn a GameObject but failed to get one from the pool. Make sure the provided name is correct and there is an adequate amount available.", gameObject);
                }
            }
        }

        public void SpawnAtRaycastHit(RaycastHit hit, string poolName)
        {
            if(hit.collider != null)
            {
                lastSpawned = SmartManager.Singleton.GetPooledObject(poolName);
                if (lastSpawned != null)
                {
                    lastSpawned.transform.position = hit.point;
                    lastSpawned.transform.rotation = Quaternion.Euler(hit.normal);
                    lastSpawned.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("|Smart GameObject|: Tried to spawn a GameObject but failed to get one from the pool. Make sure the provided name is correct and there is an adequate amount available.", gameObject);
                }
            }
        }

        public void SpawnAtRaycastHits(string poolName)
        {
            foreach (RaycastHit hit in hits)
            {
                SpawnAtRaycastHit(hit, poolName);
            }
        }

        public void SpawnAtColliders(string poolName)
        {
            foreach (Collider collider in colliders)
            {
                lastSpawned = SmartManager.Singleton.GetPooledObject(poolName);
                if(lastSpawned != null)
                {
                    lastSpawned.transform.position = collider.ClosestPoint(myTransform.position);
                    lastSpawned.transform.rotation = Quaternion.identity;
                    lastSpawned.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("|Smart GameObject|: Tried to spawn a GameObject but failed to get one from the pool. Make sure the provided name is correct and there is an adequate amount available.", gameObject);
                }
            }
        }

        public void SpawnAtTargets(string poolName)
        {
            foreach (SmartGameObject target in targets)
            {
                lastSpawned = SmartManager.Singleton.GetPooledObject(poolName);
                if(lastSpawned != null)
                {
                    lastSpawned.transform.position = target.myTransform.position;
                    lastSpawned.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("|Smart GameObject|: Tried to spawn a GameObject but failed to get one from the pool. Make sure the provided name is correct and there is an adequate amount available.", gameObject);
                }
            }
        }

        #endregion

        #region Debug Methods:

        public void DebugRootDirection()
        {
            float angle = myTransform.root.rotation.eulerAngles.y;
            if ((angle > 315 && angle < 360) || (angle > 0 && angle < 45))
            {
                Debug.Log("|Smart GameObject|: Root transform facing forward.", gameObject);
            }
            else if (angle > 45 && angle < 135)
            {
                Debug.Log("|Smart GameObject|: Root transform facing right.", gameObject);
            }
            else if (angle > 135 && angle < 225)
            {
                Debug.Log("|Smart GameObject|: Root transform facing back", gameObject);
            }
            else if (angle > 225 && angle < 315)
            {
                Debug.Log("|Smart GameObject|: Root transform facing left.", gameObject);
            }
        }

        public void DebugCurrentBehavior()
        {
            DebugBehavior(behaviors[currentBehavior]);
        }

        public void DebugBehavior(SmartBehavior behavior)
        {
            currentWeight = 0;
            foreach (Condition condition in behavior.conditions)
            {
                if (EvaluateCondition(condition) == true)
                {
                    currentWeight += condition.weight;
                }
            }
            Debug.LogFormat("|Smart GameObject|: {0} 's current weight is {1} out of {2}.", behavior.name, currentWeight, behavior.weightThreshold, gameObject);
        }

        #endregion
    }

    #region Enums:

    public enum NumericalComparisons { EqualTo, NotEqual, LessThan, GreaterThan, LessThanEqualTo, GreaterThanEqualTo }
    public enum BooleanComparisons { EqualTo, NotEqualTo }

    public enum NumericalOperators { SetEqualTo = 0, Power = 1, Multiply = 2, Divide = 3, Add = 4, Subtract = 5 }

    public enum ConditionTypes { Numerical = 0, Boolean = 1 }

    public enum ActionTypes { SmartAction = 0, Numerical = 1, Boolean = 2, UnityEvent = 3, DelayNextAction = 4 }

    public enum SplineTypes { Bezier, Hermite, CatmullRom, BSpline, Linear, Raw }

    public enum RaycastTypes { None, Raycast, RaycastRow, CircleCast, GridCast, ScatterCast, FanCast, SphereCast, BoxCast, CapsuleCast, LineCast }

    #endregion

    #region Structs:

    [Serializable]
    public struct BooleanVariable
    {
        [Tooltip("Case sensitive if a variable of the same name is used in another smart gameobject.")]
        public string name;
        public bool initialValue;
        [HideInInspector]
        public int hash;
    }

    [Serializable]
    public struct FloatVariable
    {
        [Tooltip("Case sensitive if a variable of the same name is used in another smart gameobject.")]
        public string name;
        public float initialValue;
        [HideInInspector]
        public int hash;
    }

    [Serializable]
    public struct Spline
    {
        public SplineTypes splineType;
        public Vector3 startPoint;
        public Vector3 controlPoint1;
        public Vector3 controlPoint2;
        public Vector3 endPoint;
        [Tooltip("If enabled the path will have its segments equally spaced. This may reduce acceleration around corners.")]
        public bool equalize;
        [Min(2), Tooltip("Determines the amount of segments of the equalized path.")]
        public int equalizedSegments;
    }

    #endregion
}