using ASGLib.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASGLib
{
    public abstract class PlayASG
    {
        protected char[,] board = new char[8,8]; // board YAY!

        protected ASGOutput outputMode;

        protected string gameEvent;
        protected string gameSite;
        protected string gameDate;
        protected string gameRound;
        protected string gameResult;
        protected Dictionary<string, string> gamePlayers;

        protected string gameType; //always

        protected Dictionary<string, string> gameTags; //ok nerd

        //Move stuff
        protected List<string> moveStrings;
        protected int currentTurn;
        protected bool secondPlayerToMove; //false : first players turn

        // --- CONSTRUCTOR ---
        private static string TagCheck(string value) => 
            string.IsNullOrWhiteSpace(value) ? "Unknown" : value;

        protected PlayASG(
            ASGOutput output = ASGOutput.None,
            string gameEvent = null,
            string gameSite = null,
            string gameDate = null,
            string gameRound = null,
            string playerOne = null,
            string playerTwo = null,
            Dictionary<string, string> extraTags = null)
        {
            outputMode = output;

            this.gameEvent = TagCheck(gameEvent);
            this.gameSite = TagCheck(gameSite);
            this.gameDate = string.IsNullOrWhiteSpace(gameDate)
                ? DateTime.Today.ToString("yyyy.MM.dd") : gameDate;
            this.gameRound = TagCheck(gameRound);
            this.gameResult = "*";
            gamePlayers = new Dictionary<string, string> 
            {
                { PlayerOneName, TagCheck(playerOne) },
                { PlayerTwoName, TagCheck(playerTwo) }
            };

            gameTags = extraTags ?? new Dictionary<string, string>();
            moveStrings = new List<string>();
            currentTurn = 1;
            secondPlayerToMove = false;

            InitBoard();
        }

        // --- CONTRACTS ---
        protected abstract string PlayerOneName { get; }
        protected abstract string PlayerTwoName { get; }
        protected abstract string GameTypeValue { get; }
        protected abstract void InitBoard();
        public abstract bool IsValidMove(string input);
        protected abstract string FormatMoveToken(string input);
        protected abstract void ApplyMove(string input);

        // --- PlayASG API ---
        public bool MakeMove(string input, string comment = "")
        {
            if (!IsValidMove(input))
            {
                Emit($"Illegal move: {input}");
                return false;
            }

            string token = FormatMoveToken(input);
            string dots = secondPlayerToMove ? "..." : ".";
            string formatted = $"{currentTurn}{dots}{token}{{{comment}}}";

            ApplyMove(input);
            moveStrings.Add(formatted);

            Emit($"Move played: {formatted}");

            if (secondPlayerToMove) currentTurn++;
            secondPlayerToMove = !secondPlayerToMove;

            return true;
        }

        public string EndGame(string result = "*")
        {
            gameResult = result;
            string pgn = ToPGN();
            Emit("\n--- Game Over ---\n" + pgn);
            return pgn;
        }

        public string ToPGN()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"[Event \"{gameEvent}\"]");
            sb.AppendLine($"[Site \"{gameSite}\"]");
            sb.AppendLine($"[Date \"{gameDate}\"]");
            sb.AppendLine($"[Round \"{gameRound}\"]");

            foreach (KeyValuePair<string,string> kv in gamePlayers)
                sb.AppendLine($"[{kv.Key} \"{kv.Value}\"]");

            sb.AppendLine($"[Result \"{gameResult}\"]");

            sb.AppendLine($"[GameType \"{GameTypeValue}\"]");

            foreach (KeyValuePair<string,string> kv in gameTags)
                sb.AppendLine($"[{kv.Key} \"{kv.Value}\"]");

            sb.AppendLine();

            const int lineWidth = 80;
            StringBuilder line = new StringBuilder();

            foreach (string mv in moveStrings)
            {
                string token = mv + " ";
                if (line.Length + token.Length > lineWidth)
                {
                    sb.AppendLine(line.ToString().TrimEnd());
                    line.Clear();
                }
                line.Append(token);
            }

            if (line.Length > 0)
                sb.Append(line.ToString().TrimEnd());

            sb.AppendLine(" " + gameResult);
            return sb.ToString();
        }

        // --- OUTPUT ---
        protected void Emit(string message)
        {
            switch (outputMode)
            {
                case ASGOutput.Console:
                    System.Console.WriteLine(message);
                    break;

                case ASGOutput.DataStream:
                    // Reserved for future implementation.
                    throw new NotImplementedException(
                        "DataStream output is not yet implemented.");

                case ASGOutput.None:
                default:
                    break;
            }
        }

        // --- UTILITIES ---
        protected static int FileIndex(char file) => file - 'a';
        protected static int RankIndex(char rank) => '8' - rank;
        protected static bool InBounds(int row, int col) =>
            row >= 0 && row < 8 && col >= 0 && col < 8;
    }
}
