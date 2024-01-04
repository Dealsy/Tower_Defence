using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Native.Heuristics
{
    /// <summary>
    /// ALT heuristic provider that works on any type of graph. This can significantly speed up A* queries on large and complex graphs.
    /// Based on this research:
    /// https://www.microsoft.com/en-us/research/publication/computing-the-shortest-path-a-search-meets-graph-theory/?from=http%3A%2F%2Fresearch.microsoft.com%2Fpubs%2F154937%2Fsoda05.pdf
    /// </summary>
    /// <remarks>For best results, landmarks should be placed "behind" frequent start and goal locations.</remarks>
    public struct ALT<TNode> : IHeuristicProvider<TNode>, INativeDisposable
        where TNode : unmanaged, IEquatable<TNode>
    {
        private NativeHashMap<TNode, FixedList128Bytes<float>> fromLandmarks;
        private NativeHashMap<TNode, FixedList128Bytes<float>> toLandmarks;
        private NativeList<TNode> landmarks;
        private NativeReference<bool> isDirected;

        
        /// <summary>
        /// Computes this ALT heuristic provider in parallel using Unity's Job system. This is the fastest way to compute ALT heuristics.
        /// </summary>
        /// <param name="graph"></param>
        /// <typeparam name="TGraph"></typeparam>
        /// <returns>A jobhandle that must be completed before this heuristic provider can be used</returns>
        public JobHandle ScheduleComputeUndirected<TGraph>(ref TGraph graph, NativeArray<TNode> landmarks, JobHandle dependsOn = default) where TGraph : struct, IGraph<TNode>
        {
            CheckLengthAndThrow(landmarks.Length);
            
            // using persistent always because this might take more than 4 frames
            // and the queue can potentially grow very large
            var queue = new NativeQueue<LandmarkDistance>(Allocator.Persistent);
            
            var clearJob = new ClearJob()
            {
                @from = this.fromLandmarks,
                to = this.toLandmarks,
                landmarks = this.landmarks,
                
                newLandmarks = landmarks,
                isDirected = false,
                isDirectedRef = this.isDirected
            };
          
            var enqueueJob = new EnqueueJob<TGraph>()
            {
                graph = graph,
                landmarks = landmarks,
                queue = queue.AsParallelWriter()
            };

            var dequeueJob = new DequeueJob()
            {
                landmarkCount = landmarks.Length,
                fromToLandmarks = fromLandmarks,
                queue = queue
            };

            // fire
            var clearHandle = clearJob.Schedule(dependsOn);
            var enqueueHandle = enqueueJob.Schedule(landmarks.Length, 1, clearHandle);
            var populateHandle = dequeueJob.Schedule(enqueueHandle);
          
            queue.Dispose(populateHandle);
            return populateHandle;
        }
        
        /// <summary>
        /// Computes this ALT heuristic provider in parallel using Unity's Job system. This is the fastest way to compute ALT heuristics.
        /// </summary>
        /// <param name="graph"></param>
        /// <typeparam name="TGraph"></typeparam>
        /// <returns>A jobhandle that must be completed before this heuristic provider can be used</returns>
        public JobHandle ScheduleComputeDirected<TGraph>(ref TGraph graph, ref ReversedGraph<TNode> reversedGraph, NativeArray<TNode> landmarks, JobHandle dependsOn = default) where TGraph : struct, IGraph<TNode>
        {
            CheckLengthAndThrow(landmarks.Length);

            // using persistent always because this might take more than 4 frames
            // and the queue can potentially grow very large
            var queue1 = new NativeQueue<LandmarkDistance>(Allocator.Persistent);
            var queue2 = new NativeQueue<LandmarkDistance>(Allocator.Persistent);

            var clearJob = new ClearJob()
            {
                @from = fromLandmarks,
                to = toLandmarks,
                isDirected = true,
                isDirectedRef = this.isDirected,
                newLandmarks = landmarks,
                landmarks = this.landmarks
            };
          
            var enqueueJob1 = new EnqueueJob<TGraph>()
            {
                graph = graph,
                landmarks = landmarks,
                queue = queue1.AsParallelWriter()
            };
            
            var enqueueJob2 = new EnqueueJob<ReversedGraph<TNode>>()
            {
                graph = reversedGraph,
                landmarks = landmarks,
                queue = queue2.AsParallelWriter()
            };

            var dequeueJob1 = new DequeueJob()
            {
                landmarkCount = landmarks.Length,
                fromToLandmarks = fromLandmarks,
                queue = queue1
            };
            
            var dequeueJob2 = new DequeueJob()
            {
                landmarkCount = landmarks.Length,
                fromToLandmarks = toLandmarks,
                queue = queue2
            };

            // fire
            var clearHandle = clearJob.Schedule(dependsOn);
            var enqueueHandle1 = enqueueJob1.Schedule(landmarks.Length, 1, clearHandle);
            var enqueueHandle2 = enqueueJob2.Schedule(landmarks.Length, 1, clearHandle);

            var populateHandle1 = dequeueJob1.Schedule(enqueueHandle1);
            var populateHandle2 = dequeueJob2.Schedule(enqueueHandle2);

            queue1.Dispose(populateHandle1);
            queue2.Dispose(populateHandle2);
            
            return JobHandle.CombineDependencies(populateHandle1, populateHandle2);
        }

        /// <summary>
        /// Computes ALT heuristics for a set of landmarks. Use this version if your graph contains directed edges.
        /// </summary>
        /// <param name="graph">The graph to compute ALt heuristics for</param>
        /// <param name="reversedGraph">The reversed version of the graph. <see cref="ReversedGraph{TNode}"/>.</param>
        /// <param name="landmarks">Array containing the landmarks. Ideally, landmarks should be placed "behind" frequent
        /// starting and goal locations in the graph. Currently a maximum of 31 landmarks is supported.</param>
        /// <typeparam name="TGraph"></typeparam>
        /// <remarks>The computation can be resource intensive for large graphs. This operation is done in parallel and using
        /// Unity's Burst compiler to maximize performance.</remarks>
        public void ComputeDirected<TGraph>(ref TGraph graph, ref ReversedGraph<TNode> reversedGraph, TNode[] landmarks)
            where TGraph : struct, IGraph<TNode>
        {
            var arr = new NativeArray<TNode>(landmarks, Allocator.TempJob);
            var handle = ScheduleComputeDirected(ref graph, ref reversedGraph, arr);
            arr.Dispose(handle);
            handle.Complete();
        }
        
        public void ComputeUndirected<TGraph>(ref TGraph graph, TNode[] landmarks)
            where TGraph : struct, IGraph<TNode>
        {
            var arr = new NativeArray<TNode>(landmarks, Allocator.TempJob);
            var handle = ScheduleComputeUndirected(ref graph, arr);
            arr.Dispose(handle);
            handle.Complete();
        }

 
        
        /// <summary>
        /// Dequeues source into the hashmap that contains either all distances to or from each landmark.
        /// </summary>
        private static void DequeueFromTo(NativeQueue<LandmarkDistance> sourceQueue, int landmarkCount, NativeHashMap<TNode, FixedList128Bytes<float>> destFromOrToLandmarks)
        {
            while (sourceQueue.TryDequeue(out var entry))
            {
                if (!destFromOrToLandmarks.TryGetValue(entry.node, out var list))
                {
                    list.Length = landmarkCount;
                    for (int i = 0; i < landmarkCount; i++)
                        list[i] = float.PositiveInfinity;
                }
                    
                // this could potentially be faster if we had a direct pointer to the memory location of the index
                // copying a 128 byte struct now each time...
                
                list[entry.landmarkIndex] = entry.distance; // set correct cost for this landmark at it's index
                destFromOrToLandmarks[entry.node] = list; // reassign
            }
        }

        
        /// <summary>
        /// Computes all distances from a landmark to every node in the graph and enqueues them at the destination queue.
        /// </summary>
        /// <remarks>For undirected graphs, this method using the original graph is sufficient.
        /// For directed graphs however, a <see cref="ReversedGraph{TNode}"/> has te be created in order to find the distances
        /// from every node to the landmark.</remarks>
        private static void ComputeDistancesFromLandmark<TGraph>(ref TGraph graph, NativeArray<TNode> landmarks, int landmarkIndex,
            ref AStar<TNode> aStar, ref NativeQueue<LandmarkDistance>.ParallelWriter writer)
            where TGraph : struct, IGraph<TNode>
        {
            var landmark = landmarks[landmarkIndex];
            aStar.Dijkstra(ref graph, landmark, default(NoEdgeMod<TNode>));
            using var enumerator = aStar.cameFrom.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var kv = enumerator.Current;
                writer.Enqueue(new LandmarkDistance(kv.Key, landmarkIndex, kv.Value.g));
            }
        }
        
        /// <summary>
        /// Allocates an ALT heuristic provider. 
        /// </summary>
        /// <param name="allocator">Allocator for the ALT heuristics.</param>
        public ALT(Allocator allocator)
        {
            this.fromLandmarks = new NativeHashMap<TNode, FixedList128Bytes<float>>(4, allocator);
            
            // we always allocate the to landmarks with zero capacity, even though it's not used for undirected graphs.
            // this makes constructing this struct a lot easier and allows for the ALT struct to be re-used between directed and undirected graphs.
            // also, we don't know upon construction if we're going to be used for a directed or undirected graph.
            this.toLandmarks = new NativeHashMap<TNode, FixedList128Bytes<float>>(0, allocator);
            this.landmarks = new NativeList<TNode>(allocator);
            this.isDirected = new NativeReference<bool>(allocator);
            
            // the will be initialized as soon as A* begins
            this.fromLandmarksT = default;
            this.toLandmarksT = default;
            this.t = default;
        }

        
        private FixedList128Bytes<float> fromLandmarksT;
        private FixedList128Bytes<float> toLandmarksT;
        private TNode t; // our current goal

        public void SetGoal(TNode goal)
        {
            // This gets called before A* begins
            // we cache the from/to landmarks for this goal as this saves us 2 hashmap lookups per node
            // since the goal will always be the same.
            // this severily increases the performance, as the heuristc function is called many many times
            fromLandmarks.TryGetValue(goal, out this.fromLandmarksT);
            toLandmarks.TryGetValue(goal, out this.toLandmarksT);
            this.t = goal;
        }
        
        /// <summary>
        /// Returns a cost estimate of a path between two nodes. Depending on the location of the landmarks, this estimate can
        /// be significantly better than a traditional heuristic, resulting in much less expanded nodes and thus faster pathfinding.
        /// </summary>
        /// <remarks>In order for the algorithm to work correctly, the edge cost's may not be negative.</remarks>
        public float Heuristic(TNode u)
        {
            if (u.Equals(t))
                return 0;

            if (!fromLandmarks.TryGetValue(u, out var fromLandmarksU)) return 0;

            float maxEstimate = 0;
            if (isDirected.Value)
            {
                // Directed
                if (!toLandmarks.TryGetValue(u, out var toLandmarksU)) return 0;

                for (int i = 0; i < fromLandmarksU.Length; i++)
                {
                    float fromU = fromLandmarksU[i];
                    float fromT = fromLandmarksT[i];
                    float toU = toLandmarksU[i];
                    float toT = toLandmarksT[i];
                    maxEstimate = max(max(toU - toT, fromT - fromU), maxEstimate);
                }
            }
            else
            {
                // Undirected
                for (int i = 0; i < fromLandmarksU.Length; i++)
                {
                    float fromU = fromLandmarksU[i];
                    float fromT = fromLandmarksT[i];
                    maxEstimate = max(abs(fromU - fromT), maxEstimate);
                }
            }

            return maxEstimate;
        }

        [ExcludeFromDocs]
        public void Dispose()
        {
            landmarks.Dispose();
            fromLandmarks.Dispose();
            toLandmarks.Dispose();
            isDirected.Dispose();
        }

        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            NativeArray<JobHandle> tmp = new NativeArray<JobHandle>(4, Allocator.Temp);
            tmp[0] = isDirected.Dispose(inputDeps);
            tmp[1] = landmarks.Dispose(inputDeps);
            tmp[2] = fromLandmarks.Dispose(inputDeps);
            tmp[3] = toLandmarks.Dispose(inputDeps);
            var handle = JobHandle.CombineDependencies(tmp);
            tmp.Dispose();
            return handle;
        }
        
        /// <summary>
        /// Returns the internal native containers which can be used to serialize the data.
        /// </summary>
        public void GetNativeContainers(
            out NativeHashMap<TNode, FixedList128Bytes<float>> fromLandmarks,
            out NativeHashMap<TNode, FixedList128Bytes<float>> toLandmarks,
            out NativeList<TNode> landmarks,
            out NativeReference<bool> isDirected)
        {
            fromLandmarks = this.fromLandmarks;
            toLandmarks = this.toLandmarks;
            landmarks = this.landmarks;
            isDirected = this.isDirected;
        }

        private static void CheckLengthAndThrow(int length)
        {
            int maxLandmarks = new FixedList128Bytes<float>().Capacity;
            if (length == 0 || length > maxLandmarks)
                throw new ArgumentOutOfRangeException($"Landmarks length must be greater than zero and less than or equal to {maxLandmarks}");
        }

        /// <summary>
        /// Returns wether this ALT heuristic provider was made for a directed graph.
        /// </summary>
        public bool IsDirected => isDirected.Value;
        
        /// <summary>
        /// Returns the amount of landmarks in this provider.
        /// </summary>
        public int LandmarkCount => landmarks.Length;
        
        /// <summary>
        /// Returns the location of the landmark at a given index.
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The landmark location</returns>
        public TNode GetLandmarkLocation(int index) => landmarks[index];
        
        /// <summary>
        /// Returns key value arrays containing the graph distances to every landmark. This can be useful if you want to serialize the data.
        /// For undirected graphs, this data will be the same as <see cref="GetFromKeyValueArrays"/> so doesn't need to be serialized.
        /// </summary>
        /// <param name="allocator">Allocator to use for the key value array</param>
        public NativeKeyValueArrays<TNode, FixedList128Bytes<float>> GetToKeyValueArrays(Allocator allocator) => toLandmarks.GetKeyValueArrays(Allocator.Persistent);
        
        /// <summary>
        /// Returns key value arrays containing the graph distances from every landmark. This can be useful if you want to serialize the data.
        /// If you know your graph is undirected, it is sufficient to only serialize this data.
        /// </summary>
        /// <param name="allocator">Allocator to use for the key value array</param>
        public NativeKeyValueArrays<TNode, FixedList128Bytes<float>> GetFromKeyValueArrays(Allocator allocator) => fromLandmarks.GetKeyValueArrays(Allocator.Persistent);
        
        /// <summary>
        /// Intermediate struct to allow for parallel computation of distances to and from landmarks.
        /// This can either represent a distance from a landmark to a node or a distance from a node to a landmark, which
        /// are not neccessarily equal in directed graphs.
        /// </summary>
        private readonly struct LandmarkDistance
        {
            /// <summary>
            /// The index of the landmark this distance belongs to
            /// </summary>
            public readonly int landmarkIndex;
            
            /// <summary>
            /// The (graph) distance
            /// </summary>
            public readonly float distance;
            
            /// <summary>
            /// The node that this distance belongs to
            /// </summary>
            public readonly TNode node;
            
            [ExcludeFromDocs]
            public LandmarkDistance(TNode node, int index, float distance)
            {
                this.node = node;
                this.landmarkIndex = index;
                this.distance = distance;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct EnqueueJob<TGraph> : IJobParallelFor where TGraph : struct, IGraph<TNode>
        {
            [ReadOnly] public TGraph graph;
            [ReadOnly] public NativeArray<TNode> landmarks;
            public NativeQueue<LandmarkDistance>.ParallelWriter queue;
            
            public void Execute(int index)
            {
                var memory = new AStar<TNode>(Allocator.Temp);
                ComputeDistancesFromLandmark(ref graph, landmarks, index, ref memory, ref queue);
            }
        }
        
        [BurstCompile]
        private struct ClearJob : IJob
        {
            public bool isDirected;
            [ReadOnly] public NativeArray<TNode> newLandmarks;
            
            public NativeHashMap<TNode, FixedList128Bytes<float>> from;
            public NativeHashMap<TNode, FixedList128Bytes<float>> to;
            public NativeReference<bool> isDirectedRef;
            public NativeList<TNode> landmarks;

            public void Execute()
            {
                @from.Clear();
                to.Clear();
                landmarks.Clear();
                isDirectedRef.Value = isDirected;
                
                landmarks.AddRange(newLandmarks);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct DequeueJob : IJob
        {
            public int landmarkCount;
            public NativeQueue<LandmarkDistance> queue;
            public NativeHashMap<TNode, FixedList128Bytes<float>> fromToLandmarks;
            
            public void Execute()
            {
                DequeueFromTo(queue, landmarkCount, fromToLandmarks);
            }
        }
    }
}