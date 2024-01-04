namespace AnyPath.Native
{
    public interface IEdgeMod<TNode>
    {
        /// <summary>
        /// Allows for modification of the cost of a path segment, or totally discarding it as a possibility.
        /// This is called during the A* algorithm for every segment between locations A* encounters.
        /// </summary>
        /// <param name="from">The source node for this edge</param>
        /// <param name="to">The other end of the edge</param>
        /// <param name="cost">Modify this parameter to alter the cost of the segment. Be careful as to not make the cost lower than
        /// a value that the heuristic function of the graph would calculate, as this can result in sub optimal paths. For some graph types
        /// this may not be immediately noticable but for true graph structures, providing a lower cost than the heuristic may cause the path
        /// to contain detours that look strange.</param>
        /// <returns>When you return false, the edge is discarded as a possibility. This can be used for instance to simulate a closed door.</returns>
        /// <remarks>The default NoProcessing does not touch the cost and returns true. Keeping the original graph as is.
        /// The burst compiler is smart enough to detect this and totally discard this method</remarks>
        bool ModifyCost(in TNode from, in TNode to, ref float cost);
    }

    /// <summary>
    /// Default edge mod that does nothing
    /// </summary>
    public struct NoEdgeMod<TSeg> : IEdgeMod<TSeg>
    {
        public bool ModifyCost(in TSeg @from, in TSeg to, ref float cost)
        {
            return true;
        }
    }
}