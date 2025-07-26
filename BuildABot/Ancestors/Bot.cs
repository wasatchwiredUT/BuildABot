using System;
using System.Collections.Generic;
using SC2APIProtocol;

namespace Ancestors
{
    public interface Bot
    {
        IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation);
        void OnEnd(ResponseObservation observation, Result result);
        void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, String opponentId);
    }

    /// <summary>
    /// Optional interface for bots that want to send SC2 debug drawing commands.
    /// Implementing this allows <see cref="GameConnection"/> to fetch a
    /// <see cref="SC2APIProtocol.Request"/> containing debug information each
    /// frame and send it to the game.
    /// </summary>
    public interface IDebugProvider
    {
        /// <summary>
        /// Retrieve the debug request for the current frame. Return
        /// <c>null</c> if there are no debug commands to send.
        /// </summary>
        Request? GetDebugRequest();
    }
}
