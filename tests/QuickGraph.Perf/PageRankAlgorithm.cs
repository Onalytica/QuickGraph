using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using QuickGraph.Algorithms.Ranking;
using System;
using System.Linq;

namespace QuickGraph.Perf
{
    [RankColumn]
    public class PageRankAlgorithm
    {
        private readonly BidirectionalGraph<int, Edge<int>> _graph = new BidirectionalGraph<int, Edge<int>>();

        [Params(
            100000
            ,500000
            ,1500000
        )]
        public int EdgesCount { get; set; }

        [Params(
            10000
            ,50000
        )]
        public int PoolSize { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var rand = new Random((int)DateTime.Now.Ticks);
            _graph.AddVerticesAndEdgeRange(Enumerable.Range(0, EdgesCount).Select(_ => new Edge<int>(rand.Next(PoolSize), rand.Next(PoolSize))));
        }

        [Benchmark]
        public void Compute_Original()
        {
            var pageRankSource = new PageRankAlgorithm<int, Edge<int>>(_graph);
            pageRankSource.InitializeRanks();
            pageRankSource.Compute();
        }

        [Benchmark]
        public void Compute_Improved()
        {
            var pageRankSource = new PageRankAlgorithmX<int, Edge<int>>(_graph);
            pageRankSource.InitializeRanks();
            pageRankSource.Compute();
        }
    }
}
