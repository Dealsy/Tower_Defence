using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    [Serializable]
    public class Action : IComparable<Action>
    {
        public ActionTypes actionType;

        public int actionIndex;

        // Properties used by the left side of an operator:
        public int variableID;
        /// <summary>
        /// The name of a Smart variable (may also be used to pass other strings).
        /// </summary>
        public string variableName;
        public int componentFloat;
        public int componentBoolean;
        public float customValue;

        // Operator options:
        public NumericalOperators numericalOperator;
        public BooleanComparisons booleanComparison;

        // Properties used by the right side of an operator:
        public float customValue2;
        public int componentBoolean2;
        public string variableName2;
        public int componentFloat2;
        public int variableID2;

        // Additional parameters that may be passed depending on the action index/ action type:
        public UnityEvent unityEvent;
        public AnimationCurve curve;
        public Vector3 customVector;

        // TODO: Add constructor.

        // Required by IComparable.
        public int CompareTo(Action other)
        {
            if (other == null)
            {
                return 1;
            }

            return 0;
        }
    }
}