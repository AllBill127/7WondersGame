using _7Wonders.Agents.Matches;
using _7Wonders.Agents.Matches.Records;
using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Services;
using _7Wonders.Engine.Games.Enums;
using _7Wonders.Engine.Games.Events;
using _7Wonders.Engine.Moves.Enums;
using Serilog;

namespace _7Wonders.Cli.Logs;

/// <summary>
/// The step-by-step trace of a match: writes each entry of its record, as it is added, as lines of the log (Debug
/// for the playout, Verbose for detail).
/// </summary>
internal sealed class MatchTraceWriter : IMatchListener
{
    private readonly ILogger _log;

    /// <summary>A writer into <paramref name="log"/>.</summary>
    public MatchTraceWriter(ILogger log)
    {
        _log = log;
    }

    /// <summary>Writes the entry's lines.</summary>
    public void EntryAdded(MatchEntry entry)
    {
        if (entry is MatchSetUp setUp)
        {
            WriteSetUp(setUp);
        }
        else if (entry is PhaseStarted phaseStarted)
        {
            WritePhaseStarted(phaseStarted);
        }
        else if (entry is SeatShown shown)
        {
            WriteShown(shown);
        }
        else if (entry is SeatChose chose)
        {
            WriteChoice(chose);
        }
        else if (entry is FinalScores scores)
        {
            WriteScores(scores);
        }
        else if (entry is GameEventEntry gameEventEntry)
        {
            WriteGameEvent(gameEventEntry.Event);
        }
    }

    private void WriteSetUp(MatchSetUp setUp)
    {
        _log.Debug("Game seed {Seed} (0x{SeedHex}), {Count} players",
            setUp.Seed,
            setUp.Seed.ToString("x16"),
            setUp.Seats.Count);
        foreach (SeatSetUp seat in setUp.Seats)
        {
            _log.Debug("Seat {Seat}: {Agent} {Configuration} on {Wonder}",
                seat.Seat,
                seat.AgentName,
                seat.ConfigurationJson,
                seat.Wonder);
        }
        for (int era = 0; era < setUp.Decks.Count; era++)
        {
            _log.Verbose("Era {Era} deck: {Cards}", era + 1, CardNames.Join(setUp.Decks[era]));
        }
    }

    /// <summary>A turn's header, or an extra move's, named by the effect that gives it.</summary>
    private void WritePhaseStarted(PhaseStarted phaseStarted)
    {
        if (phaseStarted.Phase == Phase.SeventhCard)
        {
            _log.Debug("--- Era {Era}, extra move: play the last card of the hand ---", phaseStarted.Era);
        }
        else if (phaseStarted.Phase == Phase.DiscardPileBuild)
        {
            _log.Debug("--- Era {Era}, extra move: free build from the discard pile ---", phaseStarted.Era);
        }
        else
        {
            _log.Debug("--- Era {Era}, turn {Turn} ---", phaseStarted.Era, phaseStarted.Turn);
        }
    }

    /// <summary>The seat's city and the cards it chooses from: its hand, or the discard pile to build from.</summary>
    private void WriteShown(SeatShown shown)
    {
        string source = "hand";
        IReadOnlyList<Card> cards = shown.Hand;
        if (shown.Phase == Phase.DiscardPileBuild)
        {
            source = "discard pile";
            cards = shown.DiscardPile;
        }
        _log.Debug("Seat {Seat}: {Coins} coins, {Shields} shields, {Wonder} stage {Stage}; {Source}: {Cards}",
            shown.Seat,
            shown.Coins,
            shown.Shields,
            shown.Wonder,
            shown.BuiltStageCount,
            source,
            CardNames.Join(cards));
        _log.Verbose("Seat {Seat} can build: {Playable}; can build a wonder stage: {CanBuildWonder}",
            shown.Seat,
            CardNames.Join(shown.BuildableCards),
            shown.CanBuildWonder);
    }

    private void WriteChoice(SeatChose chose)
    {
        string cardName = "-";
        if (chose.Move.Card != null)
        {
            cardName = chose.Move.Card.Name;
        }
        _log.Debug("Seat {Seat} chooses {Action} {Card}", chose.Seat, chose.Move.Action, cardName);
        if (chose.Note != null)
        {
            _log.Debug("Seat {Seat} {Agent}: {Note}", chose.Seat, chose.AgentName, chose.Note);
        }
    }

    private void WriteScores(FinalScores scores)
    {
        foreach (SeatScore seat in scores.Seats)
        {
            _log.Debug("Seat {Seat} {Agent}: {Total} VP = military {Military} + coins {Money} + wonder {Wonder} + " +
                       "civilian {Civilian} + commercial {Commercial} + guilds {Guilds} + science {Science}; " +
                       "{Coins} coins left",
                seat.Seat,
                seat.AgentName,
                seat.VictoryPoints,
                seat.MilitaryPoints,
                seat.CoinPoints,
                seat.WonderPoints,
                seat.CivilianPoints,
                seat.CommercialPoints,
                seat.GuildPoints,
                seat.SciencePoints,
                seat.CoinsLeft);
        }
    }

    // ================= What the rules did =================

    private void WriteGameEvent(GameEvent gameEvent)
    {
        if (gameEvent is EraStarted eraStarted)
        {
            WriteEraStarted(eraStarted);
        }
        else if (gameEvent is MoveResolved moveResolved)
        {
            WriteMoveResolved(moveResolved);
        }
        else if (gameEvent is HandsPassed handsPassed)
        {
            WriteHandsPassed(handsPassed);
        }
        else if (gameEvent is EraEnded eraEnded)
        {
            _log.Debug("End of era {Era}: last cards discarded: {Cards}",
                eraEnded.Era,
                CardNames.Join(eraEnded.LastCards));
        }
        else if (gameEvent is DiscardPileBuilt discardPileBuilt)
        {
            _log.Debug("Seat {Seat} builds {Card} from the discard pile: coins {Before} -> {After}",
                discardPileBuilt.Seat,
                discardPileBuilt.Card.Name,
                discardPileBuilt.CoinsBefore,
                discardPileBuilt.CoinsAfter);
        }
        else if (gameEvent is DiscardPileBuildSkipped discardPileBuildSkipped)
        {
            _log.Debug("Seat {Seat} builds nothing from the discard pile", discardPileBuildSkipped.Seat);
        }
        else if (gameEvent is BattleFought battleFought)
        {
            WriteBattleFought(battleFought);
        }
        else if (gameEvent is GuildCopied guildCopied)
        {
            _log.Debug("Seat {Seat} copies {Guild} from a neighbour",
                guildCopied.Seat,
                guildCopied.Guild.Name);
        }
        else if (gameEvent is GameEnded)
        {
            _log.Debug("=== Game over ===");
        }
    }

    private void WriteEraStarted(EraStarted eraStarted)
    {
        _log.Debug("=== Era {Era} ===", eraStarted.Era);
        foreach (int seat in eraStarted.FreeBuildSeats)
        {
            _log.Debug("Seat {Seat} may build one card for free this era", seat);
        }
    }

    private void WriteHandsPassed(HandsPassed handsPassed)
    {
        string direction = "east";
        if (handsPassed.PassedWest)
        {
            direction = "west";
        }
        _log.Debug("Hands passed {Direction}", direction);
    }

    /// <summary>
    /// What a move changed: coins (own, and paid to each neighbour and the bank), wonder stage and effect flags.
    /// </summary>
    private void WriteMoveResolved(MoveResolved moveResolved)
    {
        List<string> details = new();
        if (moveResolved.PaidToWest > 0)
        {
            details.Add($"paid {moveResolved.PaidToWest} to seat {moveResolved.WestSeat} (west)");
        }
        if (moveResolved.PaidToEast > 0)
        {
            details.Add($"paid {moveResolved.PaidToEast} to seat {moveResolved.EastSeat} (east)");
        }
        if (moveResolved.PaidToBank > 0)
        {
            details.Add($"paid {moveResolved.PaidToBank} to the bank");
        }
        if (moveResolved.Gained.Count > 0)
        {
            details.Add("gains " + string.Join(", ", moveResolved.Gained));
        }
        if (moveResolved.UsedUp.Count > 0)
        {
            details.Add("uses up " + string.Join(", ", moveResolved.UsedUp));
        }

        string detailsText = "";
        if (details.Count > 0)
        {
            detailsText = "; " + string.Join("; ", details);
        }
        _log.Debug("Seat {Seat} {Move}: coins {Before} -> {After}{Details}",
            moveResolved.Seat,
            DescribeMove(moveResolved),
            moveResolved.CoinsBefore,
            moveResolved.CoinsAfter,
            detailsText);
    }

    /// <summary>The move in words: "builds Loom", "discards Loom", "skips the move"...</summary>
    private static string DescribeMove(MoveResolved moveResolved)
    {
        if (moveResolved.Card == null)
        {
            return "skips the move";
        }

        string name = moveResolved.Card.Name;
        switch (moveResolved.Action)
        {
            case MoveAction.BuildWithFreeBuild:
                return $"builds {name} with the era's free build";
            case MoveAction.BuildWonderStage:
                return $"builds wonder stage {moveResolved.BuiltStageCount} using {name}";
            case MoveAction.Discard:
                return $"discards {name}";
            default:
                return $"builds {name}";
        }
    }

    private void WriteBattleFought(BattleFought battleFought)
    {
        int tokensForWin = battleFought.VictoryTokensForWin;
        _log.Debug("Seat {Seat} battles with {Shields} shields: vs seat {West} (west, {WestShields}) {WestResult}, " +
                   "vs seat {East} (east, {EastShields}) {EastResult}",
            battleFought.Seat,
            battleFought.Shields,
            battleFought.WestSeat,
            battleFought.WestShields,
            DescribeBattleOutcome(battleFought.Shields, battleFought.WestShields, tokensForWin),
            battleFought.EastSeat,
            battleFought.EastShields,
            DescribeBattleOutcome(battleFought.Shields, battleFought.EastShields, tokensForWin));
    }

    /// <summary>The token a battle gave: "+3" for a win in era II, "-1" for a loss, or "draw".</summary>
    private static string DescribeBattleOutcome(int shields, int neighbourShields, int tokensForWin)
    {
        if (shields > neighbourShields)
        {
            return $"+{tokensForWin}";
        }
        if (shields < neighbourShields)
        {
            return "-1";
        }
        return "draw";
    }
}
