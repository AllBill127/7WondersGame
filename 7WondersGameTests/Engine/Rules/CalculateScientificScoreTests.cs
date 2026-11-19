using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGame.Engine.Wonders.Enums;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Engine.Rules;

/// <summary>Science points, with wildcards placed on the symbol that gives the most.</summary>
[TestClass]
public class CalculateScientificScoreTests
{
    /// <summary>
    /// Wildcards come from the Scientists Guild and Babylon A stage 2.
    /// Each one must go on whichever symbol gives the most VP.
    /// </summary>
    [DataTestMethod]
    // k = 0
    [DataRow(0, 0, 0, 0, 0)]
    [DataRow(2, 1, 1, 0, 13)]
    [DataRow(3, 3, 3, 0, 48)]
    // k = 1
    [DataRow(0, 0, 0, 1, 1)]
    [DataRow(2, 0, 0, 1, 9)]
    [DataRow(1, 1, 0, 1, 10)]
    [DataRow(3, 2, 1, 1, 31)]   // code-comment case: the wildcard completes a second set (old code gave 28)
    // k = 2
    [DataRow(0, 0, 0, 2, 4)]
    [DataRow(2, 2, 0, 2, 26)]
    [DataRow(3, 1, 0, 2, 26)]   // both on gear
    [DataRow(2, 1, 0, 2, 18)]   // split: one gear, one tablet (old code gave 17)
    public void CalculateScientificScore_PlacesWildcardsBest(
        int gear,
        int compass,
        int tablet,
        int wildcards,
        int expected)
    {
        List<WonderBoardProgress> wonders = new()
        {
            new WonderBoardProgress(WonderId.BabylonA),
            new WonderBoardProgress(WonderId.GizahA),
            new WonderBoardProgress(WonderId.GizahA)
        };
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        PlayerState player = players[0];
        player.Resources[Resource.Gear] = gear;
        player.Resources[Resource.Compass] = compass;
        player.Resources[Resource.Tablet] = tablet;
        if (wildcards >= 1)
        {
            player.BuiltCards.Add(CardsDataset.GameCards[CardId.Scientists]);
        }

        if (wildcards >= 2)
        {
            player.Wonder.BuiltStageCount = 2;
        }

        Assert.AreEqual(expected, Scoring.CalculateScientificScore(player));
    }

    [TestMethod]
    public void BestScienceScore_MatchesEveryPlacementByHand()
    {
        for (int gears = 0; gears <= 4; gears++)
        {
            for (int compasses = 0; compasses <= 4; compasses++)
            {
                for (int tablets = 0; tablets <= 4; tablets++)
                {
                    int bestWithOneWildcard = BestScoreWithOneWildcardByHand(gears, compasses, tablets);
                    Assert.AreEqual(bestWithOneWildcard, Scoring.BestScienceScore(gears, compasses, tablets, 1),
                                    $"{gears},{compasses},{tablets} k=1");
                    Assert.IsTrue(
                        Scoring.BestScienceScore(gears, compasses, tablets, 2) >= bestWithOneWildcard,
                        $"{gears},{compasses},{tablets} k=2 never worse than k=1");
                }
            }
        }
    }

    // the wildcard tried on each symbol in turn
    private static int BestScoreWithOneWildcardByHand(int gear, int compass, int tablet)
    {
        int moreGears = gear + 1;
        int moreCompasses = compass + 1;
        int moreTablets = tablet + 1;
        int onGear = Scoring.ScienceScore(moreGears, compass, tablet);
        int onCompass = Scoring.ScienceScore(gear, moreCompasses, tablet);
        int onTablet = Scoring.ScienceScore(gear, compass, moreTablets);
        return Math.Max(onGear, Math.Max(onCompass, onTablet));
    }
}
