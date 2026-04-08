using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//Nested Othello Namespace : Implementable Othello Match Class
namespace ASGLib.Othello
{
    internal class OthelloGame : ASG<OthelloGame, OthelloMove, OthelloMoveDTO>
    {
        // --- CONSTRTUCTORS & FACTORIES ---
        private OthelloGame() : base("") { } //NOT YOURS; DONT TOUCH

        internal OthelloGame(string path) : base(path)
        {
            ParsePDNFile(path);
        }

        // --- SERIALIZATION ---
        protected override List<OthelloMoveDTO> MovesToDTO()
        {
            return moves.Select(m => m.ToDTO()).ToList();
        }

        // --- DESERIALIZATION ---
        protected override OthelloMove MoveFromDTO(OthelloMoveDTO dto)
        {
            return new OthelloMove(dto.MoveString);
        }

        // --- PARSING ---
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

            if (gameType != "50")
                throw new Exception("Error: PDN file " + path + " has unsupported GameType '" + gameType + "'. Expected GameType 50 (Othello).");

            if (sections.Length > 1)
            {
                ParsePDNMoves(sections[1]);
            }
        }

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
                    case "Round":
                        gameRound = value;
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
            bool isWhiteMove = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                Match turnMatch = Regex.Match(token, @"^(\d+)(\.\.\.|\.)$");
                if (turnMatch.Success)
                {
                    currentTurn = int.Parse(turnMatch.Groups[1].Value);
                    isWhiteMove = turnMatch.Groups[2].Value == "...";
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
    }
}