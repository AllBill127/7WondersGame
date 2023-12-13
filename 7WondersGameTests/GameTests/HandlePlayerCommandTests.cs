using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGameTests.GameTests
{
    [TestClass]
    public class HandlePlayerCommandTests
    {
        // TODO:    test when
        //          - build_structure -> playedCards contain built structure and handCards dont contain
        //          - build_hand_free -> playedCards contain built structure and handCards dont contain, FreeCardOnce == false
        //          - build_wonder -> handCards, playedCards and discardedCards dont contain used card
        //          - skip_move -> inconcludive (check logger output) happens when ChooseMoveCard() is called with an empty list of cards to choose from and return null
        //          - discard -> discardedCards contain card and handCards dont contain
        //          
        //          test when
        //          - fail any action and auto discard ->  discardedCards contain card and handCards dont contain, (check logger output for error 'SHOULD NOT HAPPEN')
    }
}
