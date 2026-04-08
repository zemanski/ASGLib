using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//Nested Chess Namespace : Implementable Chess Match Class
namespace ASGLib.Chess
{
    internal class ChessGame : ASG<ChessGame,ChessMove,ChessMoveDTO>
    {
        // --- CONSTRTUCTORS & FACTORIES ---
        private ChessGame() : base("") { } //NOT YOURS; DONT TOUCH

        internal ChessGame(string path) : base(path)
        {
            ParsePGNFile(path);
        }

        // --- SERIALIZATION ---
        protected override List<ChessMoveDTO> MovesToDTO()
        {
            return moves.Select(m => m.ToDTO()).ToList();
        }

        // --- DESERIALIZATION ---
        protected override ChessMove MoveFromDTO(ChessMoveDTO dto)
        {
            return new ChessMove(dto.MoveString);
        }

        // --- PARSING ---
        private void ParsePGNFile(string path)
        {
            string fileText = File.ReadAllText(path);

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
    }
}
