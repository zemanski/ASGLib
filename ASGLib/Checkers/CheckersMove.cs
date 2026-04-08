using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//Nested Checkers Namespace : Checkers Move and Move Data Transfer Object
namespace ASGLib.Checkers
{

    //Checkers Move Data Transfer Object
    public class CheckersMoveDTO : Data.ASGMoveDTO
    {
        public List<string> Steps { get; set; } = new();
        public bool IsJump { get; set; }
        public bool IsPromotion { get; set; }
        public string Comment { get; set; }
    }

    //Class for Checkers Move : Parses Standardized Checkers Move Format
    public class CheckersMove : ASGMove<CheckersMoveDTO>
    {

        // --- CHECKERS MOVE DATA ---
        private static readonly Dictionary<int, string> SquareMap = new Dictionary<int, string>
        {
            {  1, "b8" }, {  2, "d8" }, {  3, "f8" }, {  4, "h8" },
            {  5, "a7" }, {  6, "c7" }, {  7, "e7" }, {  8, "g7" },
            {  9, "b6" }, { 10, "d6" }, { 11, "f6" }, { 12, "h6" },
            { 13, "a5" }, { 14, "c5" }, { 15, "e5" }, { 16, "g5" },
            { 17, "b4" }, { 18, "d4" }, { 19, "f4" }, { 20, "h4" },
            { 21, "a3" }, { 22, "c3" }, { 23, "e3" }, { 24, "g3" },
            { 25, "b2" }, { 26, "d2" }, { 27, "f2" }, { 28, "h2" },
            { 29, "a1" }, { 30, "c1" }, { 31, "e1" }, { 32, "g1" }
        };
        private static readonly char WhiteKingRank = '1';
        private static readonly char BlackKingRank = '8';
        private static string SqrToAlg(string squareStr, string body)
        {
            if (!int.TryParse(squareStr, out int squareNum))
                throw new Exception("Error: Could not parse PDN square number '" + squareStr + "' in move body '" + body + "'.");

            if (!SquareMap.TryGetValue(squareNum, out string algebraic))
                throw new Exception("Error: PDN square number " + squareNum + " has no algebraic mapping in move body '" + body + "'.");

            return algebraic;
        }

        // --- CHECKERS MOVE MEMBERS ---
        private List<string> steps;
        private bool         isJump;
        private bool         isPromotion;
        private string       comment;

        // --- CHECKERS MOVE CONSTRUCTOR ---
        internal CheckersMove(string moveString) : base(moveString)
        {
            steps = new List<string>();
            comment = "";
            isJump = false;
            isPromotion = false;

            MoveValidator();
            SelfParse();
        }

        // --- CHECKERS MOVE SERIALIZER ---
        internal override CheckersMoveDTO ToDTO()
        {
            return new CheckersMoveDTO
            {
                MoveString = GetMove(),
                TurnNum = turnNum,
                TurnPlayer = turnPlayer,
                Steps = steps,
                IsJump = isJump,
                IsPromotion = isPromotion,
                Comment = comment
            };
        }

        // --- CHECKERS MOVE VALIDATOR ---
        private void MoveValidator()
        {
            string move = GetMove();
            if (string.IsNullOrWhiteSpace(move)) throw new Exception("Move string cannot be empty.");
            if (!Regex.IsMatch(move, @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$")) throw new Exception("Invalid checkers move format: " + move);
        }

        // --- CHECKERS MOVE PARSING ---
        private void SelfParse()
        {
            Match match = Regex.Match(GetMove(), @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$");

            turnNum = int.Parse(match.Groups[1].Value);
            turnPlayer = match.Groups[2].Value == "." ? "White" : "Black";
            string body = match.Groups[3].Value.Trim();
            comment = match.Groups[4].Value;

            ParseMove(body);
        }

        private void ParseMove(string body)
        {
            List<string> rawSteps;
            if (body.Contains('x'))
            {
                isJump = true;
                rawSteps = body.Split('x').Select(s => s.Trim()).ToList();
            }
            else if (body.Contains('-'))
            {
                isJump = false;
                rawSteps = body.Split('-').Select(s => s.Trim()).ToList();
            }
            else
            {
                throw new Exception("Invalid checkers move body — no separator found: " + body);
            }

            foreach (string step in rawSteps)
            {
                if (!Regex.IsMatch(step, @"^\d+$"))
                    throw new Exception("Invalid square number '" + step + "' in checkers move body: " + body);
            }

            if (!isJump && rawSteps.Count != 2)
                throw new Exception("Simple checkers move must have exactly two squares: " + body);

            if (isJump && rawSteps.Count < 2)
                throw new Exception("Jump checkers move must have at least two squares: " + body);

            steps = rawSteps.Select(s => SqrToAlg(s, body)).ToList();

            string destination = steps[steps.Count - 1];
            char destinationRank = destination[1];

            isPromotion = turnPlayer == "White"
                ? destinationRank == WhiteKingRank
                : destinationRank == BlackKingRank;
        }

        // --- CHECKERS MOVE ACCESSORS ---
        public static Dictionary<string, object> GetMoveData(CheckersMove move)
        {
            return new Dictionary<string, object>
            {
                { "Turn",       move.turnNum    },
                { "Player",     move.turnPlayer },
                { "Steps",      move.steps      },
                { "IsJump",     move.isJump     },
                { "BecameKing", move.isPromotion }
            };
        }

        public static string GetMoveComment(CheckersMove move)
        {
            return move.comment;
        }
    }
}
