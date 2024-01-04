using System;
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
    /// Contains conditions and actions that define behaviour.
    /// </summary>
    [Serializable]
    public class SmartBehavior : IComparable<SmartBehavior>
    {
        #region Variables:

        /// <summary>
        /// If true this behavior should be evaluated.
        /// </summary>
        public bool enabled = true;

        public string name = "New Behaviour";

        [SerializeField]
        public List<Condition> conditions = new List<Condition>();

        /// <summary>
        /// When the total weight of a behavior's evaluated conditions exceeds this threshold its actions will be invoked.
        /// </summary>
        [Min(0), Tooltip("When the total weight of a behavior's evaluated conditions exceeds this threshold its actions will be invoked. If not then fallback actions will be invoked.")]
        public int weightThreshold;

        [SerializeField]
        public List<Action> actions = new List<Action>();

        [SerializeField]
        public List<Action> fallbackActions = new List<Action>();

        /// <summary>
        /// True if the coroutine that evaluates behaviors is delaying this one.
        /// </summary>
        [HideInInspector]
        public bool delaying = false;

        /// <summary>
        /// True if the behavior contains an action that delays a coroutine.
        /// </summary>
        [HideInInspector]
        public bool containsDelay = false;

        #endregion

        public SmartBehavior(string behaviorName, List<Condition> behaviourConditions, List<Action> behaviorActions, List<Action> behaviorFallbackActions, bool startEnabled = false)
        {
            actions = behaviorActions;
            fallbackActions = behaviorFallbackActions;
            conditions = behaviourConditions;
            name = behaviorName;
            weightThreshold = 0;
            delaying = false;
            enabled = startEnabled;
        }

        public SmartBehavior(string behaviorName, List<Condition> behaviorConditions, List<Action> behaviorActions, List<Action> behaviorFallbackActions, int scoreThreshold, bool startEnabled = false)
        {
            actions = behaviorActions;
            fallbackActions = behaviorFallbackActions;
            conditions = behaviorConditions;
            name = behaviorName;
            weightThreshold = scoreThreshold;
            delaying = false;
            enabled = startEnabled;
        }

        public bool ContainsDelay()
        {
            foreach (Action action in actions)
            {
                if (action.actionType == ActionTypes.DelayNextAction)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateContainsDelayState()
        {
            containsDelay = ContainsDelay();
        }

        // Required by IComparable.
        public int CompareTo(SmartBehavior other)
        {
            if (other == null)
            {
                return 1;
            }

            return 0;
        }
    }
}