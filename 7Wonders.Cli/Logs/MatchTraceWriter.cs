using _7Wonders.Agents.Matches;
using _7Wonders.Agents.Matches.Records;
using _7Wonders.Engine.Cards.Services;
using _7Wonders.Engine.Games.Enums;
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
        else if (entry is TurnStarted turnStarted)
        {
            _log.Debug("--- Era {Era}, turn {Turn} ---", turnStarted.Era, turnStarted.Turn);
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
        _log.Debug("=== Era 1 ===");
    }

    private void WriteShown(SeatShown shown)
    {
        if (shown.Phase == Phase.ChooseMoves)
        {
            _log.Debug("Seat {Seat}: {Coins} coins, {Shields} shields, {Wonder} stage {Stage}; hand: {Hand}",
                shown.Seat,
                shown.Coins,
                shown.Shields,
                shown.Wonder,
                shown.BuiltStageCount,
                CardNames.Join(shown.Hand));
        }
        else if (shown.Phase == Phase.SeventhCard)
        {
            _log.Debug("Seat {Seat} plays the last card of the hand (Babylon B): {Hand}",
                shown.Seat,
                CardNames.Join(shown.Hand));
        }
        else
        {
            _log.Debug("Seat {Seat} may build from the discard pile for free (Halikarnassos)", shown.Seat);
            _log.Verbose("Discard pile: {Cards}", CardNames.Join(shown.DiscardPile));
        }
        _log.Verbose("Seat {Seat} can build: {Playable}; can build a wonder stage: {CanBuildWonder}",
            shown.Seat,
            CardNames.Join(shown.BuildableCards),
            shown.CanBuildWonder);
    }

    private void WriteChoice(SeatChose chose)
    {
        // the free build from the discard pile is written by the game's own entry for it
        if (chose.Phase == Phase.DiscardPileBuild)
        {
            return;
        }

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
}
