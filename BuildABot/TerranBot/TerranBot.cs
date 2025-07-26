using System.Collections.Generic;
using Action = SC2APIProtocol.Action;
using SC2APIProtocol;
using Ancestors;
using Controllers;

namespace TerranBot
{
    public class TheTerranBot : Bot
    {
        private BotController _botController;

        public TheTerranBot()
        {
            _botController = new BotController();
        }
        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentID)
        {
            _botController.OnStart(gameInfo,data,pingResponse,observation,playerId,opponentID);      
        }
    
        public IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            List<Action> actions = new List<Action>();
            actions.AddRange(_botController.OnFrame(observation));
            return actions;
        }
        
        public void OnEnd(ResponseObservation observation, Result result)
        { }

        public Request GetDebugRequest()
        {
            return _botController.GetDebugRequest();
        }

    }
}
