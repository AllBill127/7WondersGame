using _7WondersGame.Engine.Cards.Entities;

namespace _7WondersGame.Engine.Games.Knowledge;

/// <summary>
/// What one seat knows about the cards it can't see now (fix plan step 6, M1). Everything else hidden (the
/// cards it never saw, the shuffle of the later decks, the guilds in play) it doesn't know.
/// </summary>
/// <param name="InHand">
/// By seat: the cards this seat saw in the hand that seat holds now, minus the ones built from it since (a build is
/// public). Cards buried under a wonder or discarded are face down, so some of these may be gone: the list holds
/// every card still in the hand, and maybe more. Empty for a hand the seat hasn't seen this era; its own hand exactly.
/// </param>
/// <param name="InPile">Discard-pile cards the seat put there, or saw when it looked through the pile (Halikarnassos).</param>
/// <param name="Buried">The cards the seat buried under its own wonder stages.</param>
public sealed record SeatKnowledge(IReadOnlyList<IReadOnlyList<Card>> InHand, IReadOnlyList<Card> InPile, IReadOnlyList<Card> Buried);
