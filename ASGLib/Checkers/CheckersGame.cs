using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//Nested Checkers Namespace : Implementable Checkers Match Class
namespace ASGLib.Checkers
{
    //Class for Checkers Game : Can be Constructed from PDN File
    //Targets English/American Checkers (8x8) : PDN GameType 20 Required, Assumed if Missing
    public class CheckersGame : ASG<CheckersGame, CheckersMove, CheckersMoveDTO>
    {
        //FOR DESERIALIZATION :: DO NOT USE WITHOUT INVESTIGATION
        private CheckersGame() : base("") { }

        //Checkers Move DTO Reconstructor : Re-Parses Move from Stored Move String
        protected override CheckersMove MoveFromDTO(CheckersMoveDTO dto)
        {
            return new CheckersMove(dto.MoveString);
        }

        //Checkers Move DTO Converter : Maps Move List to CheckersMoveDTO List for Serialization
        protected override List<CheckersMoveDTO> MovesToDTO()
        {
            return moves.Select(m => m.ToDTO()).ToList();
        }

        //New Match from PDN File
        public CheckersGame(string path) : base(path)
        {
            ParsePDNFile(path);
        }

        //Public Deserializer : Entry Point for Json Deserialization via Private Constructor Factory
        public static CheckersGame Deserialize(string json)
        {
            return FromJson(json, () => new CheckersGame());
        }

        //Public Serializer : Entry Point for Json Serialization
        public string Serialize()
        {
            return ToJson();
        }

        //PDN File Parser : Reads File, Validates English/American GameType, and Splits into Tag and Move Sections
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

            //Validate GameType Tag : PDN GameType 20 Designates English/American Checkers
            //GameType Tag is Optional : Assumed to be 20 if Missing
            if (gameTags.TryGetValue("GameType", out string gameType) && gameType != "20")
                throw new Exception("Error: PDN file " + path + " has unsupported GameType '" + gameType + "'. Expected GameType 20 (English/American Checkers).");

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

                //Unknown Tag : Stored in Optional Tag Dictionary
                gameTags[tag] = value;
            }
        }

        //PDN Move Parser : Tokenizes Move Section and Constructs CheckersMove List
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

                tokens.Add(val);
            }

            int currentTurn = 0;
            bool isBlackMove = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                //Turn Number Token : Update Current Turn and Active Player
                Match turnMatch = Regex.Match(token, @"^(\d+)(\.\.\.|\.)$");
                if (turnMatch.Success)
                {
                    currentTurn = int.Parse(turnMatch.Groups[1].Value);
                    isBlackMove = turnMatch.Groups[2].Value == "...";
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
                string dots = isBlackMove ? "..." : ".";

                string formattedMove =
                    currentTurn.ToString() +
                    dots +
                    moveText +
                    commentText;

                moves.Add(new CheckersMove(formattedMove));

                isBlackMove = !isBlackMove;
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

            foreach (CheckersMove move in moves)
            {
                moveIndex++;
                Console.WriteLine($"\nMove #{moveIndex}");

                Console.WriteLine("Raw: " + move.DEBUG_GetMove());

                Dictionary<string, object> row = CheckersMove.GetMoveData(move);

                //Turn Info
                Console.WriteLine("Player Turn: " + row["Player"]);
                Console.WriteLine("Turn Number: " + row["Turn"]);

                //Algebraic Steps
                Console.WriteLine("Path: " + string.Join(" -> ", (List<string>)row["Steps"]));

                //Move Flags
                Console.WriteLine("Is Jump: " + row["IsJump"]);
                Console.WriteLine("Became King: " + row["BecameKing"]);

                //Comment
                string comment = CheckersMove.GetMoveComment(move);
                Console.WriteLine("Comment: " + (string.IsNullOrEmpty(comment) ? "None" : comment));
            }

            Console.WriteLine("\n===== END OF GAME =====");
        }
    }
}
