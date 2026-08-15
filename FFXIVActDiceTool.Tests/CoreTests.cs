using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVActDiceTool.Models;
using FFXIVActDiceTool.Services;
using NUnit.Framework;

namespace FFXIVActDiceTool.Tests
{
    [TestFixture]
    public class CoreTests
    {
        [Test]
        public void Parser_ParsesKoreanBracketLog()
        {
            var result = new DiceParser().Parse("[23:33]메이데이 님이 주사위를 굴려 721이 나왔습니다!");
            Assert.That(result, Is.Not.Null); Assert.That(result.PlayerName, Is.EqualTo("메이데이")); Assert.That(result.RollValue, Is.EqualTo(721));
        }
        [Test]
        public void Session_CalculatesHighestLowestAndCounts()
        {
            var manager = new DiceSessionManager(); manager.StartSession();
            manager.AddRoll(Roll("Alice", 100)); manager.AddRoll(Roll("Bob", 300)); manager.AddRoll(Roll("Carol", 200));
            var result = manager.StopSession();
            Assert.That(result.HighestRolls.Single().PlayerName, Is.EqualTo("Bob")); Assert.That(result.LowestRolls.Single().PlayerName, Is.EqualTo("Alice"));
            Assert.That(result.TotalRollCount, Is.EqualTo(3)); Assert.That(result.UniquePlayerCount, Is.EqualTo(3));
        }
        [Test]
        public void Rank_UsesDenseRankAndReturnsAllTies()
        {
            var rolls = new List<DiceRollEntry> { Roll("Alice", 900), Roll("Bob", 900), Roll("Carol", 700), Roll("Dave", 500) };
            var calculator = new RankCalculator();
            CollectionAssert.AreEquivalent(new[] { "Alice", "Bob" }, calculator.QueryRank(rolls, 1, RankDirection.Highest).Entries.Select(x => x.PlayerName));
            Assert.That(calculator.QueryRank(rolls, 2, RankDirection.Highest).Entries.Single().PlayerName, Is.EqualTo("Carol"));
            Assert.That(calculator.QueryRank(rolls, 3, RankDirection.Highest).Entries.Single().PlayerName, Is.EqualTo("Dave"));
        }
        [Test]
        public void DuplicateFilter_IsBoundedAndCanBeCleared()
        {
            var filter = new DiceDuplicateFilter(2); var first = Roll("Alice", 100); first.RawLogLine = "one";
            Assert.That(filter.IsDuplicate(first), Is.False); Assert.That(filter.IsDuplicate(first), Is.True); filter.Clear(); Assert.That(filter.IsDuplicate(first), Is.False);
        }
        private static DiceRollEntry Roll(string player, int value) { return new DiceRollEntry { Timestamp = new DateTime(2026, 1, 1), PlayerName = player, RollValue = value, RawLogLine = player + value }; }
    }
}
