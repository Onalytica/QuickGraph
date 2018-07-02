using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuickGraph.Predicates;
using QuickGraph.Collections;
using System.Buffers;

namespace QuickGraph.Algorithms.Ranking
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public sealed class PageRankAlgorithmX<TVertex, TEdge> :
        AlgorithmBase<IBidirectionalGraph<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        private IDictionary<TVertex, double> ranks = new Dictionary<TVertex, double>();

        private int maxIterations = 60;
        private double tolerance = 2 * double.Epsilon;
        private double damping = 0.85;

        public PageRankAlgorithmX(IBidirectionalGraph<TVertex, TEdge> visitedGraph)
            : base(visitedGraph)
        { }

        public IDictionary<TVertex, double> Ranks
        {
            get
            {
                return this.ranks;
            }
        }

        public double Damping
        {
            get
            {
                return this.damping;
            }
            set
            {
                this.damping = value;
            }
        }

        public double Tolerance
        {
            get
            {
                return this.tolerance;
            }
            set
            {
                this.tolerance = value;
            }
        }

        public int MaxIteration
        {
            get
            {
                return this.maxIterations;
            }
            set
            {
                this.maxIterations = value;
            }
        }

        public void InitializeRanks()
        {
            this.ranks = this.VisitedGraph.Vertices.ToDictionary(v => v, v => 0D);
            //foreach (var v in this.VisitedGraph.Vertices)
            //{
            //    this.ranks.Add(v, 0);
            //}

            //            this.RemoveDanglingLinks();
        }
        /*
                public void RemoveDanglingLinks()
                {
                    VertexCollection danglings = new VertexCollection();
                    do
                    {
                        danglings.Clear();

                        // create filtered graph
                        IVertexListGraph fg = new FilteredVertexListGraph(
                            this.VisitedGraph,
                            new InDictionaryVertexPredicate(this.ranks)
                            );

                        // iterate over of the vertices in the rank map
                        foreach (IVertex v in this.ranks.Keys)
                        {
                            // if v does not have out-edge in the filtered graph, remove
                            if (fg.OutDegree(v) == 0)
                                danglings.Add(v);
                        }

                        // remove from ranks
                        foreach (IVertex v in danglings)
                            this.ranks.Remove(v);
                        // iterate until no dangling was removed
                    } while (danglings.Count != 0);
                }
        */
        protected override void InternalCompute()
        {
            var cancelManager = this.Services.CancelManager;
            IDictionary<TVertex, double> tempRanks = new Dictionary<TVertex, double>(ranks);

            // create filtered graph
            var fg = new FilteredBidirectionalGraph<TVertex, TEdge, IBidirectionalGraph<TVertex, TEdge>>(
                this.VisitedGraph,
                new InDictionaryVertexPredicate<TVertex, double>(ranks).Test,
                e => true
            );
            int index = 0;
            var edgesBuffer = ArrayPool<TEdge>.Shared.Rent(fg.EdgeCount);
            var allEdges = edgesBuffer.AsSpan();
            var inEdges = fg.Vertices.ToDictionary(
                v => v, 
                v => 
                {
                    var edges = fg.InEdges(v);
                    var currentSpan = allEdges.Slice(index, edges.Count());
                    for (int i = 0; i < currentSpan.Length; i++)
                        currentSpan[i] = edges.ElementAt(i);
                    index += currentSpan.Length;
                    return currentSpan;
                }
            );
            var outDegrees = fg.Vertices.ToDictionary(v => v, v => fg.OutDegree(v));

            int iter = 0;
            double error = 0;
            do
            {
                // compute page ranks
                error = 0;
                Parallel.ForEach(tempRanks.ToArray(), de =>
                {
                    // compute ARi
                    double r = 0;
                    var edges = inEdges[de.Key];
                    int edgesInx = edges.Length - 1;
                    while (edgesInx > -1)
                    {
                        var e = edges[edgesInx--];
                        r += tempRanks[e.Source] / outDegrees[e.Source];
                    }

                    // add sourceRank and store
                    double newRank = (1 - this.damping) + this.damping * r;
                    tempRanks[de.Key] = newRank;

                    // compute deviation
                    InterlockedAdd(ref error, Math.Abs(de.Value - newRank));
                });

                iter++;
            }
            while (error > this.tolerance && iter < this.maxIterations && !cancelManager.IsCancelling);

            ArrayPool<TEdge>.Shared.Return(edgesBuffer);
            ranks = tempRanks;
        }

        private double InterlockedAdd(ref double location1, double value)
        {
            double newCurrentValue = location1; // non-volatile read, so may be stale
            while (true)
            {
                double currentValue = newCurrentValue;
                double newValue = currentValue + value;
                newCurrentValue = Interlocked.CompareExchange(ref location1, newValue, currentValue);
                if (newCurrentValue == currentValue)
                    return newValue;
            }
        }

        public double GetRanksSum()
        {
            return this.ranks.Values.Sum();
        }

        public double GetRanksMean()
        {
            return GetRanksSum() / this.ranks.Count;
        }
    }
}
