using SC2APIProtocol;
using System.Collections.Generic;

namespace BuildABot.Services
    {
    /// <summary>
    /// Manages and creates debug drawing commands for real-time, in-game visualization.
    /// This service correctly packages commands into a RequestDebug object.
    /// </summary>
    public class DebugService
        {
        private readonly DebugDraw _drawCommands;

        public DebugService()
            {
            _drawCommands = new DebugDraw();
            }

        /// <summary>
        /// Clears commands from the previous frame. Call this at the start of OnFrame.
        /// </summary>
        public void NewFrame()
            {
            _drawCommands.Spheres.Clear();
            _drawCommands.Lines.Clear();
            _drawCommands.Text.Clear();
            _drawCommands.Boxes.Clear();
            }

        public void DrawText(string text, Point worldPos, Color color, uint size = 12)
            {
            _drawCommands.Text.Add(new DebugText { Text = text, WorldPos = worldPos, Color = color, Size = size });
            }

        public void DrawSphere(Point worldPos, float radius, Color color)
            {
            _drawCommands.Spheres.Add(new DebugSphere { P = worldPos, R = radius, Color = color });
            }

        public void DrawLine(Point p1, Point p2, Color color)
            {
            _drawCommands.Lines.Add(new DebugLine { Line = new Line { P0 = p1, P1 = p2 }, Color = color });
            }

        /// <summary>
        /// Creates the final Request object containing all debug commands for the frame.
        /// </summary>
        /// <returns>A Request object ready to be sent to the game, or null if there's nothing to draw.</returns>
        public Request CreateDebugRequest()
            {
            if (_drawCommands.Spheres.Count == 0 && _drawCommands.Lines.Count == 0 && _drawCommands.Text.Count == 0)
                {
                return null;
                }

            // This is the correct structure.
            // A new Request with its Debug field set to a new RequestDebug object.
            var request = new Request();
            var debugRequest = new RequestDebug();
            debugRequest.Debug.Add(new DebugCommand { Draw = _drawCommands });
            request.Debug = debugRequest;

            return request;
            }

        public static Color CreateColor(int r, int g, int b)
            {
            var color = new Color();
            color.R = (byte)r;
            color.G = (byte)g;
            color.B = (byte)b;
            return color;
            }
        }
    }