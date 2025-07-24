
using SC2APIProtocol;
using Ancestors;
using TerranBot;
using BotController;

namespace BuildABot
{
    public class Program
    {
        // Settings for your bot.
        private static Bot bot = new BotController.BotController();
        private static Race race = Race.Terran;

        // Settings for single player mode.
       
        private static Race opponentRace = Race.Random;
        private static Difficulty opponentDifficulty = Difficulty.VeryEasy;

        /* The main entry point for the bot.
         * This will start the Stacraft 2 instance and connect to it.
         * The program can run in single player mode against the standard Blizzard AI, or it can be run against other bots through the ladder.
         */
        public static void Main(string[] args)
        {

            var mapFolder = @"C:\Program Files (x86)\StarCraft II\Maps"; // Your maps folder

            var mapFiles = Directory.GetFiles(mapFolder, "*.SC2Map");
            if (mapFiles.Length == 0)
            {
                Console.WriteLine($"No .SC2Map files found in '{mapFolder}'.");
                return;
            }
            var random = new Random();
            var randomMap = mapFiles[random.Next(mapFiles.Length)];

            if (args.Length == 0)
                new GameConnection().RunSinglePlayer(bot, randomMap, race, opponentRace, opponentDifficulty).Wait();
            else
                new GameConnection().RunLadder(bot, race, args).Wait();
        }
    }
}
