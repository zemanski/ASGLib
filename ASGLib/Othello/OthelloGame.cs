using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//Nested Othello Namespace : Implementable Othello Match Class
namespace ASGLib.Othello
{
    //Class for Othello Game : Can be Constructed from PDN File
    //Targets Standard Othello (8x8) : PDN GameType 50 Required
    //Black always moves first in standard Othello
    public class OthelloGame : ASG<OthelloGame, OthelloMove, OthelloMoveDTO>
    {
        //FOR DESERIALIZATION :: DO NOT USE WITHOUT INVESTIGATION
        private OthelloGame() : base("") { }

        //Othello Move DTO Reconstructor : Re-Parses Move from Stored Move String
        protected override OthelloMove MoveFromDTO(OthelloMoveDTO dto)
        {
            return new OthelloMove(dto.MoveString);
        }

        //Othello Move DTO Converter : Maps Move List to OthelloMoveDTO List for Serialization
        protected override List<OthelloMoveDTO> MovesToDTO()
        {
            return moves.Select(m => m.ToDTO()).ToList();
        }

        //New Match from PDN File
        public OthelloGame(string path) : base(path)
        {
            ParsePDNFile(path);
        }

        //Public Deserializer : Entry Point for Json Deserialization via Private Constructor Factory
        public static OthelloGame Deserialize(string json)
        {
            return FromJson(json, () => new OthelloGame());
        }

        //Public Serializer : Entry Point for Json Serialization
        public string Serialize()
        {
            return ToJson();
        }

        //PDN File Parser : Reads File, Validates Othello GameType, and Splits into Tag and Move Sections
        private void ParsePDNFile(string path)
        {
            string fileText = File.ReadAllText(path);

            //Normalize Line Endings Before Splitting
            fileText = fileText.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] sections = fileText.Split(new string[] { "\n\n" }, 2, StringSplitOptions.None);

            if (sections.Length > 0)
            {
                ParseTags(sections[0]);
            }

            //Validate GameType Tag : PDN GameType 50 Designates Standard Othello
            //GameType Tag is Required for Othello PDN Files
            if (!gameTags.TryGetValue("GameType", out string gameType))
                throw new Exception("Error: PDN file " + path + " is missing required GameType tag.");

            if (gameType != "50")
                throw new Exception("Error: PDN file " + path + " has unsupported GameType '" + gameType + "'. Expected GameType 50 (Othello).");

            if (sections.Length > 1)
            {
                ParsePDNMoves(sections[1]);
            }
        }

        //PDN Tag Parser : Extracts Known and User-Defined Tag Pairs from Tag Section
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
                    case "Black":
                        gamePlayers["Black"] = value;
                        continue;
                    case "White":
                        gamePlayers["White"] = value;
                        continue;
                    case "Result":
                        gameResult = value;
                        continue;
                }

                //Unknown Tag : Stored in Optional Tag Dictionary
                gameTags[tag] = value;
            }
        }

        //PDN Move Parser : Tokenizes Move Section and Constructs OthelloMove List
        //Othello PDN uses algebraic notation (A) : moves are single squares e.g. "f5"
        //Pass moves are recorded as "PA" when a player has no legal placement
        //Black always moves first : turn 1 dot = Black, triple-dot = White
        private void ParsePDNMoves(string moveSection)
        {
            //Normalize Whitespace
            moveSection = moveSection.Replace("\n", " ");
            moveSection = moveSection.Replace("\r", " ");

            //Match PDN Block Comments in Both [...] and {...} Styles and Non-Whitespace Tokens
            MatchCollection matches = Regex.Matches(moveSection, @"\{[^}]*\}|\[[^\]]*\]|[^\s]+");

            List<string> tokens = new List<string>();

            foreach (Match match in matches)
            {
                string val = match.Value;

                //Normalize PDN Block Comments from [...] to {...} Style
                if (val.StartsWith("[") && val.EndsWith("]"))
                {
                    tokens.Add("{" + val.Substring(1, val.Length - 2) + "}");
                    continue;
                }

                //Split Embedded Turn+Move Tokens : e.g. "1.f5" -> ["1.", "f5"]
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
            bool isWhiteMove = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                //Turn Number Token : Update Current Turn and Active Player
                //Single dot = Black move, Triple dot = White move
                Match turnMatch = Regex.Match(token, @"^(\d+)(\.\.\.|\.)$");
                if (turnMatch.Success)
                {
                    currentTurn = int.Parse(turnMatch.Groups[1].Value);
                    isWhiteMove = turnMatch.Groups[2].Value == "...";
                    continue;
                }

                //Result Token : Skip : PDN Permits '*' for Unfinished Games
                if (token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*")
                {
                    continue;
                }

                string moveText = token;
                string commentText = "";

                //Lookahead : Consume Inline Comment if Present
                if (i + 1 < tokens.Count && tokens[i + 1].StartsWith("{"))
                {
                    commentText = tokens[i + 1];
                    i++;
                }

                //Default to Empty Comment Block if None Found
                if (string.IsNullOrEmpty(commentText))
                {
                    commentText = "{}";
                }

                //Format Move String : TurnNum + Dots + MoveText + Comment
                //Single dot = Black (first mover), Triple dot = White
                string dots = isWhiteMove ? "..." : ".";

                string formattedMove =
                    currentTurn.ToString() +
                    dots +
                    moveText +
                    commentText;

                moves.Add(new OthelloMove(formattedMove));

                isWhiteMove = !isWhiteMove;
            }
        }

        //TEMPORARY : Console Debug Printer
        public void PrintDebug()
        {
            //Print Game Metadata
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

            //Print Moves
            Console.WriteLine("\n===== MOVES =====");

            int moveIndex = 0;

            foreach (OthelloMove move in moves)
            {
                moveIndex++;
                Console.WriteLine($"\nMove #{moveIndex}");

                Console.WriteLine("Raw: " + move.DEBUG_GetMove());

                Dictionary<string, object> row = OthelloMove.GetMoveData(move);

                //Turn Info
                Console.WriteLine("Player Turn: " + row["Player"]);
                Console.WriteLine("Turn Number: " + row["Turn"]);

                //Placement or Pass
                Console.WriteLine("Square: " + row["Square"]);
                Console.WriteLine("Is Pass: " + row["IsPass"]);

                //Comment
                string comment = OthelloMove.GetMoveComment(move);
                Console.WriteLine("Comment: " + (string.IsNullOrEmpty(comment) ? "None" : comment));
            }

            Console.WriteLine("\n===== END OF GAME =====");
        }
    }
}