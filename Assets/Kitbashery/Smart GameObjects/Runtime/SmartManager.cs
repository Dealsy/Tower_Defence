using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    /// Consolidates <see cref="SmartGameObject"/> update loops and manages object pooling improving game performance.
    /// </summary>
    [HelpURL("https://kitbashery.com/docs/smart-gameobjects/smart-manager.html")]
    [DefaultExecutionOrder(-21)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Kitbashery/Gameplay/Smart Manager")]
    public class SmartManager : MonoBehaviour
    {
        #region Properties:

        /// <summary>
        /// An instance of <see cref="SmartManager"/> in the current scene (there should only be one at a time).
        /// </summary>
        public static SmartManager Singleton;

        [SerializeField, Min(1), Tooltip("The amount of framerate frames between updates. (Does not apply to Smart GameObjects with rigidbodies, which update every fixed framerate frame).")]
        private int updateDelay = 1;
        [SerializeField, Tooltip("If enabled then a frame delay between Smart GameObject updates will be increased if the game's framerate is below the target framerate.")]
        private bool throttleUpdates = false;
        [SerializeField, Tooltip("The frames per second target the application will try to run at. (overrides Application.targetFrameRate)."), Range(14, 300)]
        private int targetFrameRate = 60;
        /// <summary>
        /// The current frame count used when throttling <see cref="SmartGameObject"/> updates from the <see cref="SmartManager"/> instance.
        /// </summary>
        private int frames = 0;
        [HideInInspector, Tooltip("The time between the manager's last frame update and the current one.")]
        public float currentFPS = 0;
        [SerializeField, Tooltip("If enabled pauses all Smart GameObject updates.")]
        private bool pauseUpdates = false;

        /// <summary>
        /// A non-reorderable dictionary of <see cref="Pool"/>s.
        /// </summary>
        [SerializeField, NonReorderable]
        private List<Pool> pools = new List<Pool>();
        /// <summary>
        /// A dictionary created from <see cref="pools"/> on Awake() so pools can be found via their prefab name as a key.
        /// </summary>
        private Dictionary<string, Pool> poolLookup = new Dictionary<string, Pool>();
        /// <summary>
        /// Temporary <see cref="GameObject"/> used when a <see cref="GameObject"/> is needed as a variable in an iteration loop.
        /// </summary>
        private GameObject tmp;
        /// <summary>
        /// Temporary <see cref="Pool"/> used when a <see cref="Pool"/> struct is needed as a variable in an interation loop.
        /// </summary>
        private Pool tempPool;

        [SerializeField, Tooltip("Managed Smart GameObjects.")]
        private List<SmartGameObject> smartGameObjects = new List<SmartGameObject>();
        /// <summary>
        /// This list is for Smart GameObjects that have rigidbodies, they will update every fixed framerate frame.
        /// </summary>
        [SerializeField, Tooltip("Managed Smart GameObjects with Rigidbodies.")]
        private List<SmartGameObject> smartPhysicsGameObjects = new List<SmartGameObject>();

        private const string space = " ";

        /// <summary>
        /// Time delay cache for coroutine yields.
        /// </summary>
        public Dictionary<float, WaitForSeconds> delays = new Dictionary<float, WaitForSeconds>();

        #endregion

        #region Initialization & Updates:

        private void Awake()
        {
            if (Singleton == null)
            {
                Singleton = this;
            }
            else
            {
                Destroy(gameObject);
            }

            Application.targetFrameRate = targetFrameRate;

            CreatePools();
        }

        // Update is called once per frame
        void Update()
        {
            if(pauseUpdates == false && smartGameObjects.Count > 0)
            {
                if (throttleUpdates == true)
                {
                    currentFPS = 1f / Time.unscaledDeltaTime;

                    // TODO: Should there should be a target delta to compare the current fps to instead of directly to the target frame rate?
                    if (currentFPS < Application.targetFrameRate && updateDelay < Application.targetFrameRate)
                    {
                        updateDelay = Application.targetFrameRate - (int)currentFPS + 1;
                    }
                    else
                    {
                        updateDelay = 1;
                    }

                    frames++;
                    if (frames >= updateDelay)
                    {
                        for (int i = smartGameObjects.Count - 1; i >= 0; i--)
                        {
                            smartGameObjects[i].UpdateSmartGameObject();
                        }
                        frames = 0;
                    }
                }
                else
                {
                    for (int i = smartGameObjects.Count - 1; i >= 0; i--)
                    {
                        smartGameObjects[i].UpdateSmartGameObject();
                    }
                }
            }
        }

        // FixedUpdate is called every fixed framerate frame
        private void FixedUpdate()
        {
            if (pauseUpdates == false && smartPhysicsGameObjects.Count > 0)
            {
                for (int i = smartPhysicsGameObjects.Count - 1; i >= 0; i--)
                {
                    smartPhysicsGameObjects[i].UpdateSmartGameObject();
                }
            }
        }

#endregion

        #region Core Methods:

        /// <summary>
        /// Registers a <see cref="SmartGameObject"/> with this <see cref="SmartManager"/> singleton instance.
        /// </summary>
        /// <param name="smartGO">The <see cref="SmartGameObject"/> to register.</param>
        public void Register(SmartGameObject smartGO)
        {
            if (smartGO.rigid != null)
            {
                if (smartPhysicsGameObjects.Contains(smartGO) == false)
                {
                    smartPhysicsGameObjects.Add(smartGO);
                }
            }
            else
            {
                if (smartGameObjects.Contains(smartGO) == false)
                {
                    smartGameObjects.Add(smartGO);
                }
            }
        }

        /// <summary>
        /// Unregisters a <see cref="SmartGameObject"/> with this <see cref="SmartManager"/> singleton instance.
        /// </summary>
        /// <param name="smartGO">The <see cref="SmartGameObject"/> to unregister.</param>
        public void Unregister(SmartGameObject smartGO)
        {
            if (smartGO.rigid != null)
            {
                smartPhysicsGameObjects.Remove(smartGO);
            }
            else
            {
                smartGameObjects.Remove(smartGO);
            }
        }

        public void RegisterAll()
        {
            smartGameObjects.Clear();
            smartPhysicsGameObjects.Clear();
            if (FindObjectOfType<SmartGameObject>() != null)
            {
                foreach (SmartGameObject smartGO in FindObjectsOfType<SmartGameObject>())
                {
                    if (smartGO.gameObject.activeSelf == true)
                    {
                        Register(smartGO);
                    }
                }
            }
        }


      /* 
       * TODO:
        public string SaveSmartGameObjects()
        {
            return string.Empty;
        }
      */

        /// <summary>
        /// Sets the <see cref="pauseUpdates"/> boolean to the specified state.
        /// </summary>
        /// <param name="state">The state to se <see cref="pauseUpdates"/> to.</param>
        public void PauseSmartGameObjects(bool state)
        {
            pauseUpdates = state;
        }

        /// <summary>
        /// Sets the <see cref="pauseUpdates"/> boolean to the opposite of its current state.
        /// </summary>
        public void TogglePauseSmartGameObjects()
        {
            pauseUpdates = !pauseUpdates;
        }

        /// <summary>
        /// Sets the <see cref="throttleUpdates"/> boolean to the specified state.
        /// </summary>
        /// <param name="state">The state to se <see cref="throttleUpdates"/> to.</param>
        public void ThrottleSmartGameObjectUpdates(bool state)
        {
            throttleUpdates = state;
        }

        /// <summary>
        /// Sets the <see cref="throttleUpdates"/> boolean to the opposite of its current state.
        /// </summary>
        public void ToggleSmartGameObjectUpdateThrottle()
        {
            throttleUpdates = !throttleUpdates;
        }

        #endregion

        #region Pooling Methods:

        /// <summary>
        /// This should be called after the <see cref="pools"/> list is modified.
        /// </summary>
        public void CreatePools()
        {
            if(pools.Count > 0)
            {
                for (int i = pools.Count - 1; i >= 0; i--)
                {
                    if(pools[i].prefab != null && pools[i].amount > 0)
                    {
                        if (!poolLookup.ContainsKey(pools[i].prefab.name))
                        {
                            PopulatePool(pools[i]);
                            poolLookup.Add(pools[i].prefab.name, pools[i]);
                        }
                        else
                        {
                            Debug.LogWarningFormat("|Smart Manager|: Duplicate pools for object named {0} detected, removing...", pools[i].prefab.name, gameObject);
                            pools.Remove(pools[i]);
                        }
                    }
                    else
                    {
                        pools.Remove(pools[i]);
                    }
                }
            }          
        }

        /// <summary>
        /// Instantiates GameObjects from the specified pool.
        /// </summary>
        /// <param name="pool">The pool to populate.</param>
        private void PopulatePool(Pool pool)
        {
            if (pool.pooledObjects.Count < pool.amount)
            {
                for (int i = 0; i < pool.amount; i++)
                {
                    tmp = Instantiate(pool.prefab);
                    tmp.SetActive(false);
                    tmp.gameObject.hideFlags = pool.hideFlags;
                    if (pool.sequencialNaming == true)
                    {
                        tmp.gameObject.name = pool.prefab.name + space + i;
                    }
                    pool.pooledObjects.Add(tmp);
                }
            }
        }

        /// <summary>
        /// Increases the size of a pool & instantiates the pool's prefab to meet the new amount.
        /// </summary>
        /// <param name="prefabName">The name of the pool to increase the amount of (same as the pool's prefab name).</param>
        /// <param name="amount">The amount to increase the pool's amount by (will only increase to the max amount set in the inspector).</param>
        public void ExpandPool(string prefabName, int amount)
        {
            if(poolLookup.ContainsKey(prefabName))
            {
                tempPool = poolLookup[prefabName];
                tempPool.amount += amount;
                if(tempPool.amount > tempPool.maxAmount)
                {
                    tempPool.amount = tempPool.maxAmount;
                }
                PopulatePool(tempPool);
                poolLookup[prefabName] = tempPool;
            }
        }

        /// <summary>
        /// Destroys all objects in all pools.
        /// </summary>
        /// <param name="omitActive">Preserve pooled GameObjects that are currently active in the scene.</param>
        public void DestroyPools(bool omitActive)
        {
            foreach (Pool pool in poolLookup.Values)
            {
                for (int i = pool.pooledObjects.Count; i > 0; i--)
                {
                    if (omitActive == false)
                    {
                        Destroy(pool.pooledObjects[i]);
                    }
                    else
                    {
                        if (pool.pooledObjects[i].activeSelf == false)
                        {
                            Destroy(pool.pooledObjects[i]);
                        }
                    }
                }

                pool.pooledObjects.Clear();
            }
        }

        /// <summary>
        /// Activates a pooled <see cref="GameObject"/> in a pool specified by the name of its prefab.
        /// </summary>
        /// <param name="prefabName">The name of the pool that contains the prefab to activate.</param>
        public void ActivatePooledObject(string prefabName)
        {
            if (!string.IsNullOrEmpty(prefabName) && poolLookup.ContainsKey(prefabName))
            {
                for (int i = 0; i < poolLookup[prefabName].amount; i++)
                {
                    if (!poolLookup[prefabName].pooledObjects[i].activeInHierarchy)
                    {
                        poolLookup[prefabName].pooledObjects[i].SetActive(true);
                    }
                }
            }
            else
            {
                Debug.LogWarningFormat("|Smart Manager|: failed to activate GameObject from pool ({0}) GameObject will be null, make sure the name is correct.", prefabName, gameObject);
            }
        }

        /// <summary>
        /// Gets an inactive pooled <see cref="GameObject"/> by its prefab name.
        /// </summary>
        /// <param name="prefabName">The name of the prefab to get an instance of.</param>
        /// <returns>The first inactive prefab instance.</returns>
        public GameObject GetPooledObject(string prefabName)
        {
            if (!string.IsNullOrEmpty(prefabName) && poolLookup.ContainsKey(prefabName))
            {
                for (int i = 0; i < poolLookup[prefabName].amount; i++)
                {
                    if (!poolLookup[prefabName].pooledObjects[i].activeInHierarchy)
                    {
                        return poolLookup[prefabName].pooledObjects[i];
                    }
                }
            }
            else
            {
                Debug.LogWarningFormat("|Smart Manager|: failed to get GameObject from pool ({0}) GameObject will be null, make sure the name is correct.", prefabName, gameObject);
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// Represents a pool of <see cref="GameObject"/>s & their instantiation criteria.
    /// </summary>
    [Serializable]
    public struct Pool
    {
        [Tooltip("The object to pool. The name of this object is also the name of the pool.")]
        public GameObject prefab;
        [Range(1, 5000), Tooltip("The next amount of GameObjects to instantiate.")]
        public int amount;
        [Range(1, 5000), Tooltip("The maximum size of the pool, pools can not exceed this amount when expanding.")]
        public int maxAmount;
        [Tooltip("HideFlags for prefabs instantiated via the pooling system. Useful for hiding pooled objects in the heirarchy.")]
        public HideFlags hideFlags;
        [Tooltip("Use numbered names for GameObjects instead of name(clone).")]
        public bool sequencialNaming;
        [Tooltip("The GameObjects currently pooled.")]
        public List<GameObject> pooledObjects;
    }
}