using System;
using System.Linq;
using System.Collections.Generic;
using QuickGraph.Predicates;
using QuickGraph.Collections;

namespace QuickGraph.Algorithms.Ranking
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public sealed class PageRankAlgorithm<TVertex, TEdge> :
        AlgorithmBase<IBidirectionalGraph<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        private IDictionary<TVertex, double> ranks = new Dictionary<TVertex, double>();

        private int maxIterations = 60;
        private double tolerance = 2 * double.Epsilon;
        private double damping = 0.85;

        public PageRankAlgorithm(IBidirectionalGraph<TVertex, TEdge> visitedGraph)
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
            IDictionary<TVertex, double> tempRanks = new Dictionary<TVertex, double>();

            // create filtered graph
            var fg = new FilteredBidirectionalGraph<TVertex, TEdge, IBidirectionalGraph<TVertex, TEdge>>(
                this.VisitedGraph,
                new InDictionaryVertexPredicate<TVertex, double>(this.ranks).Test,
                e => true
            );
            var inEdges = fg.Vertices.ToDictionary(v => v, v => fg.InEdges(v).ToArray());
            var outDegrees = fg.Vertices.ToDictionary(v => v, v => fg.OutDegree(v));

            int iter = 0;
            double error = 0;
            do
            {
                if (cancelManager.IsCancelling)
                    return;

                // compute page ranks
                error = 0;
                var ranksArr = this.Ranks.ToArray();
                var ranksArrInx = ranksArr.Count() - 1;
                while (ranksArrInx > -1 && !cancelManager.IsCancelling)
                {
                    var de = ranksArr[ranksArrInx--];

                    // compute ARi
                    double r = 0;
                    var edges = inEdges[de.Key];
                    int edgesInx = edges.Count() - 1;
                    while (edgesInx > -1)
                    {
                        var e = edges[edgesInx--];
                        r += this.ranks[e.Source] / outDegrees[e.Source];
                    }

                    // add sourceRank and store
                    double newRank = (1 - this.damping) + this.damping * r;
                    tempRanks[de.Key] = newRank;

                    // compute deviation
                    error += Math.Abs(de.Value - newRank);
                }

                // swap ranks
                var temp = ranks;
                ranks = tempRanks;
                tempRanks = temp;

                iter++;
            }
            while (error > this.tolerance && iter < this.maxIterations);

            Console.WriteLine("{0}, {1}", iter, error);
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
