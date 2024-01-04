using System;
using System.Collections.Generic;
using AnyPath.Native;
using UnityEngine;

namespace AnyPath.Managed.CodeGen
{
    public class GraphDefinition
    {
        public string graphName;
        public string nodeName;
        
        public bool isValid;
        public Type nodeType;

        public GraphDefinition(Type graphType)
        {
            if (graphType.GetGenericArguments().Length > 0)
            {
                if (graphType.Name != @"ReversedGraph`1")
                    Debug.Log($"Graph type {graphType.Name} has open generic parameters. This is not supported for code generation.");
                return;
            }
            
            foreach(Type intType in graphType.GetInterfaces()) 
            {
                if (intType.IsGenericType && intType.GetGenericTypeDefinition() == typeof(IGraph<>))
                {
                    var genericArguments = intType.GetGenericArguments();
                    if (genericArguments.Length != 1)
                        continue;
                    
                    // Skip open defined graphs (like ReversedGraph)
                    if (genericArguments[0].IsGenericParameter)
                        continue;
                    
                    graphName = CodeGeneratorUtil.GetRealTypeName(graphType);
                    nodeName = CodeGeneratorUtil.GetRealTypeName(genericArguments[0]);
                    nodeType = genericArguments[0];
                    isValid = true;
                    return;
                }
            }
        }
        
        public override string ToString()
        {
            return isValid ? $"{graphName} : IGraph<{nodeName}>" : "?";
        }

        public string ToLabelString() => graphName;
        
        public static void FindAll(List<GraphDefinition> definitions)
        {
            foreach (var graphType in CodeGeneratorUtil.GetAllTypesImplementingOpenGenericType(typeof(IGraph<>)))
            {
                var definition = new GraphDefinition(graphType);
                if (definition.isValid)
                    definitions.Add(definition);
            }
        }
    }
}