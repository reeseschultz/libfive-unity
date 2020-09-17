using System;
using System.Collections.Generic;

namespace libfivesharp
{
    /// <summary>The static container for libfive contexts.</summary>
    public static class LFContext
    {
        static Context activeContext = null;

        /// <summary>The current active libfive context; any LFTrees 
        /// created inside a `using` block with one of these will 
        /// automatically get disposed of upon the completion of that block
        /// </summary>
        public static Context Active
        {
            get => activeContext;
            set
            {
                if (value != null) value.priorContext = activeContext;
                activeContext = value;
            }
        }
    }

    /// <summary>A libfive context; any LFTrees created inside a `using` 
    /// block with one of these will automatically get disposed of upon 
    /// the completion of that block.</summary>
    public class Context : IDisposable
    {
        protected List<LFTree> trees = new List<LFTree>();

        /// <summary>The Context that was active before this one.</summary>
        public Context priorContext = null;

        /// <summary>A Context which will contain LFTrees created inside of it for later disposal.</summary>
        public Context() { }

        /// <summary>Adds an LFTree to this context (to be disposed of when this is).</summary>
        public void AddTreeToContext(LFTree tree) => trees.Add(tree);

        /// <summary>Removes an LFTree from this context (so it is NOT disposed when this context is).</summary>
        public void RemoveTreeFromContext(LFTree tree) => trees.Remove(tree);

        /// <summary>Disposes all of the LFTrees created within this context.</summary>
        public void Dispose()
        {
            foreach (var tree in trees) if (tree != null) tree.Dispose();
            LFContext.Active = priorContext;
        }
    }
}
