using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//Nested Chess Namespace : Implementable Chess Match Class
namespace ASGLib.Chess
{
    //Class for Chess Game : Can be Constructed from PGN or PDN File
    internal class ChessGame : ASG<ChessGame,ChessMove,ChessMoveDTO>
    {
        //FOR DESERIALIZATION :: DO NOT USE WITHOUT INVESTIGATION
        private ChessGame() : base("") { }

        //Chess Move Deseriialization Re-Parser
        protected override ChessMove MoveFromDTO(ChessMoveDTO dto)
        {
            return new ChessMove(dto.MoveString);
        }

        protected override List<ChessMoveDTO> MovesToDTO()
        {
            return moves.Select(m => m.ToDTO()).ToList();
        }

        //New Match from File
        internal ChessGame(string path, Data.ASGFileType type) : base(path)
        {
            switch (type) {
                case Data.ASGFileType.PDNFile:
                    ParsePDNFile(path);
                    break;
                case Data.ASGFileType.PGNFile: 
                    ParsePGNFile(path); break;
                default:
                    throw new Exception("Error: FileType Unsupported for file " + path + ". Json files are deserialized statically.");
            }
        }

        //PDN File Parser
        private void ParsePDNFile(string path)
        {
            string fileText = File.ReadAllText(path);

            fileText = fileText.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] sections = fileText.Split(new string[] { "\n\n" }, 2, StringSplitOptions.None);

            if (sections.Length > 0) 
            {
                ParseTags(sections[0]);
            }

            if (!gameTags.TryGetValue("GameType", out string gameType))
                throw new Exception("Error: PDN file " + path + " is missing required GameType tag.");

            if (gameType != "21")
                throw new Exception("Error: PDN file " + path + " has unsupported GameType '" + gameType + "'. Expected GameType 21 (Chess).");

            if (sections.Length > 1)
            {
                ParsePDNMoves(sections[1]);
            }
        }

        //PGN File Parser
        private void ParsePGNFile(string path)
        {
            string fileText = File.ReadAllText(path);

            // Normalize Line Endings Before Splitting
            fileText = fileText.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] sections = fileText.Split(new string[] { "\n\n" }, 2, StringSplitOptions.None);

            if (sections.Length > 0)
            {
                ParseTags(sections[0]);
            }

            if (sections.Length > 1)
            {
                ParsePGNMoves(sections[1]);
            }
        }

        //Tag Parser : Extracts Known and User-Defined Tag Pairs from Tag Section
        private void ParseTags(string tagSection)
        {
            string[] lines = tagSection.Split('\n');

            foreach (string line in lines)
            {
                Match match = Regex.Match(line, @"\[(\w+)\s+""(.*)""\]");

                if (!match.Success) continue;

                string tag = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                switch (tag)
                {
                    case "Event":
                        gameEvent = value;
                        continue;
                    case "Site":
                        gameSite = value;
                        continue;
                    case "Date":
                        gameDate = value;
                        continue;
                    case "White":
                        gamePlayers["White"] = value;
                        continue;
                    case "Black":
                        gamePlayers["Black"] = value;
                        continue;
                    case "Result":
                        gameResult = value;
                        continue;
                }

                gameTags[tag] = value;
            }
        }

        private void ParsePDNMoves(string moveSection)
        {
            moveSection = moveSection.Replace("\n", " ");
            moveSection = moveSection.Replace("\r", " ");

            MatchCollection matches = Regex.Matches(moveSection, @"\{[^}]*\}|\[[^\]]*\]|[^\s]+");

            List<string> tokens = new List<string>();

            foreach (Match match in matches)
            {
                string val = match.Value;

                if (val.StartsWith("[") && val.EndsWith("]"))
                {
                    tokens.Add("{" + val.Substring(1, val.Length - 2) + "}");
                    continue;
                }

                Match embedded = Regex.Match(val, @"^(\d+)(\.{1}|\.\.\.)([a-zA-Z].*)$");
                if (embedded.Success)
                {
                    tokens.Add(embedded.Groups[1].Value + embedded.Groups[2].Value);
                    tokens.Add(embedded.Groups[3].Value);
                    continue;
                }

                tokens.Add(val);
            }

            int currentTurn = 0;
            bool isBlackMove = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                Match turnMatch = Regex.Match(token, @"^(\d+)(\.\.\.|\.)$");
                if (turnMatch.Success)
                {
                    currentTurn = int.Parse(turnMatch.Groups[1].Value);
                    isBlackMove = turnMatch.Groups[2].Value == "...";
                    continue;
                }

                if (token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*")
                {
                    continue;
                }

                string moveText = token;
                string commentText = "";

                if (i + 1 < tokens.Count && tokens[i + 1].StartsWith("{"))
                {
                    commentText = tokens[i + 1];
                    i++;
                }

                if (string.IsNullOrEmpty(commentText))
                {
                    commentText = "{}";
                }

                string dots = isBlackMove ? "..." : ".";

                string formattedMove =
                    currentTurn.ToString() +
                    dots +
                    moveText +
                    commentText;

                moves.Add(new ChessMove(formattedMove));

                isBlackMove = !isBlackMove;
            }
        }

        //PGN Move Parser : Tokenizes Move Section and Constructs ChessMove List
        private void ParsePGNMoves(string moveSection)
        {
            moveSection = moveSection.Replace("\n", " ");
            moveSection = moveSection.Replace("\r", " ");

            MatchCollection matches = Regex.Matches(moveSection, @"\{[^}]*\}|[^\s]+");

            List<string> tokens = new List<string>();

            foreach (Match match in matches)
            {
                string val = match.Value;

                // Split tokens like "3...a6" or "1.e4" into ["3...", "a6"] or ["1.", "e4"]
                Match embedded = Regex.Match(val, @"^(\d+)(\.{1}|\.\.\.)([a-zA-Z].*)$");
                if (embedded.Success)
                {
                    tokens.Add(embedded.Groups[1].Value + embedded.Groups[2].Value);
                    tokens.Add(embedded.Groups[3].Value);
                }
                else
                {
                    tokens.Add(val);
                }
            }

            int currentTurn = 0;
            bool isBlackMove = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                Match turnMatch = Regex.Match(token, @"^(\d+)(\.\.\.|\.)$");
                if (turnMatch.Success)
                {
                    currentTurn = int.Parse(turnMatch.Groups[1].Value);
                    isBlackMove = turnMatch.Groups[2].Value == "...";
                    continue;
                }

                if (token == "1-0" || token == "0-1" || token == "1/2-1/2")
                {
                    continue;
                }

                string moveText = token;

                string commentText = "";
                if (i + 1 < tokens.Count && tokens[i + 1].StartsWith("{"))
                {
                    commentText = tokens[i + 1];
                    i++;
                }

                if (string.IsNullOrEmpty(commentText))
                {
                    commentText = "{}";
                }

                string dots = isBlackMove ? "..." : ".";

                string formattedMove =
                    currentTurn.ToString() +
                    dots +
                    moveText +
                    commentText;

                ChessMove move = new ChessMove(formattedMove);
                moves.Add(move);

                isBlackMove = !isBlackMove;
            }
        }

        //TEMPORARY: Console Printer
        public void PrintDebug()
        {
            // ----------------------------
            // 1. Print Game Metadata
            // ----------------------------
            Console.WriteLine("===== GAME INFO =====");
            Console.WriteLine("Event: " + gameEvent);
            Console.WriteLine("Site: " + gameSite);
            Console.WriteLine("Date: " + gameDate);
            Console.WriteLine("Result: " + gameResult);

            Console.WriteLine("\n--- Players ---");
            foreach (KeyValuePair<string, string> player in gamePlayers)
            {
                Console.WriteLine(player.Key + ": " + player.Value);
            }

            Console.WriteLine("\n--- All Tags ---");
            foreach (KeyValuePair<string, string> tag in gameTags)
            {
                Console.WriteLine(tag.Key + ": " + tag.Value);
            }

            // ----------------------------
            // 2. Print Moves
            // ----------------------------
            Console.WriteLine("\n===== MOVES =====");

            int moveIndex = 0;

            foreach (ChessMove move in moves)
            {
                moveIndex++;
                Console.WriteLine($"\nMove #{moveIndex}");

                // --- Raw move string from base class ---
                Console.WriteLine("Raw: " + move.DEBUG_GetMove());

                Dictionary<string, object> row = ChessMove.GetMoveData(move);

                // --- Turn info ---
                Console.WriteLine("Player Turn: " + row["Player"]);
                Console.WriteLine("Turn Number: " + row["Turn"]);

                // --- Piece, destination, disambiguation ---
                Console.WriteLine("Piece: " + row["Piece"]);
                Console.WriteLine("Destination: " + row["Destination"]);
                Console.WriteLine("Disambiguation: " + (row["Disambiguation"] ?? "None"));

                // --- Move flags ---
                Console.WriteLine("Capture: " + ChessMove.GetMoveData(move, "capture"));
                Console.WriteLine("Promotion: " + ChessMove.GetMoveData(move, "promotion"));
                Console.WriteLine("Check: " + ChessMove.GetMoveData(move, "check"));
                Console.WriteLine("Checkmate: " + ChessMove.GetMoveData(move, "checkmate"));

                // --- Comment ---
                string comment = ChessMove.GetMoveComment(move);
                Console.WriteLine("Comment: " + (string.IsNullOrEmpty(comment) ? "None" : comment));
            }

            Console.WriteLine("\n===== END OF GAME =====");
        }

    }
}
