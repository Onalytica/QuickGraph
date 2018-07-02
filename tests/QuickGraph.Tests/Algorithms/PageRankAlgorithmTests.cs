using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickGraph.Algorithms.Ranking;
using System;
using System.Linq;

namespace QuickGraph.Tests.Algorithms
{
    [TestClass]
    public class PageRankAlgorithmTests
    {
        [TestMethod]
        public void Page_Rank_Implementations_Should_Produce_The_Same_Results()
        {
            var rand = new Random((int)DateTime.Now.Ticks);
            var graph = new BidirectionalGraph<int, Edge<int>>();
            graph.AddVerticesAndEdgeRange(Enumerable.Range(0, 100000).Select(_ => new Edge<int>(rand.Next(10000), rand.Next(10000))));

            var pageRankSource = new PageRankAlgorithm<int, Edge<int>>(graph);
            pageRankSource.InitializeRanks();
            pageRankSource.Compute();

            var pageRankTarget = new PageRankAlgorithmX<int, Edge<int>>(graph);
            pageRankTarget.InitializeRanks();
            pageRankTarget.Compute();

            Assert.IsTrue(pageRankSource.Ranks.SequenceEqual(pageRankSource.Ranks));
        }
    }
}
