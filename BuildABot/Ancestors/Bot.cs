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
        /// <summary>
        /// Optional debug drawing request for this frame. Can return null when there is
        /// nothing to draw.
        /// </summary>
        Request GetDebugRequest();
    }
}
