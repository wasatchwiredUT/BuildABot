using System.Collections.Generic;
using Action = SC2APIProtocol.Action;
using SC2APIProtocol;
using Ancestors;

namespace TerranBot
{
    class TheTerranBot : Bot
    {
        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentID)
        { }
    
        public IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            List<Action> actions = new List<Action>();

            return actions;
        }
        
        public void OnEnd(ResponseObservation observation, Result result)
        { }
    }
}
