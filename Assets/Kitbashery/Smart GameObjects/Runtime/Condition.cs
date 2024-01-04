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
    [Serializable]
    public class Condition : IComparable<Condition>
    {
        public ConditionTypes conditionType;

        [Range(0, 100)]
        public int weight;

        // Numerical comparison:
        public NumericalComparisons numericalComparison;
        public int componentFloat;
        public int componentFloat2;
        public float customValue;
        public float customValue2;

        // Boolean comparison:
        public BooleanComparisons booleanComparison;
        public int componentBoolean;
        public int componentBoolean2;

        public string variableName;
        public string variableName2;

        public int variableID;
        public int variableID2;

        public Condition(ConditionTypes cType, int w, NumericalComparisons comparison, int f1, int f2, int c1, int c2, int b1, int b2, string varName, string varName2)
        {
            conditionType = cType;
            weight = w;
            numericalComparison = comparison;
            componentFloat = f1;
            componentFloat2 = f2;
            customValue = c1;
            customValue2 = c2;
            variableName = varName;
            variableName2 = varName2;
            variableID = variableName.GetHashCode();
            variableID2 = variableName2.GetHashCode();
        }

        public Condition()
        {
            conditionType = ConditionTypes.Numerical;
            weight = 0;
            numericalComparison = NumericalComparisons.EqualTo;
            componentFloat = 0;
            componentFloat2 = 0;
            customValue = 0;
            customValue2 = 0;
            variableName = "Variable Name (Case Sensitive)";
            variableName2 = "Variable Name (Case Sensitive)";
            variableID = variableName.GetHashCode();
            variableID2 = variableName2.GetHashCode();
        }

        // Required by IComparable.
        public int CompareTo(Condition other)
        {
            if (other == null)
            {
                return 1;
            }

            return 0;
        }
    }
}