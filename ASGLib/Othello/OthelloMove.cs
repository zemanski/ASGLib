using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//Nested Othello Namespace : Othello Move and Move Data Transfer Object
namespace ASGLib.Othello
{

    //Othello Move Data Transfer Object
    public class OthelloMoveDTO : Data.ASGMoveDTO
    {
        public string Square { get; set; } = "";
        public bool IsPass { get; set; }
        public string Comment { get; set; } = "";
    }

    //Class for Othello Move : Parses Standardized Othello Move Format
    public class OthelloMove : ASGMove<OthelloMoveDTO>
    {
        
        // --- OTHELLO MOVE DATA ---
        private const string PassToken = "PA";

        // --- OTHELLO MOVE MEMBERS ---
        private string square;
        private bool isPass;
        private string comment;

        // --- OTHELLO MOVE CONSTRUCTOR
        internal OthelloMove(string moveString) : base(moveString)
        {
            square = "";
            isPass = false;
            comment = "";

            MoveValidator();
            SelfParse();
        }

        // --- OTHELLO MOVE VALIDATION ---
        private void MoveValidator()
        {
            string move = GetMove();
            if (string.IsNullOrWhiteSpace(move)) throw new Exception("Move string cannot be empty.");
            if (!Regex.IsMatch(move, @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$")) throw new Exception("Invalid Othello move format: " + move);
        }

        // --- OTHELLO MOVE SERIALIZER ---
        internal override OthelloMoveDTO ToDTO()
        {
            return new OthelloMoveDTO
            {
                MoveString = GetMove(),
                TurnNum = turnNum,
                TurnPlayer = turnPlayer,
                Square = square,
                IsPass = isPass,
                Comment = comment
            };
        }

        // --- OTHELLO MOVE PARSING ---
        private void SelfParse()
        { 
            System.Text.RegularExpressions.Match match = Regex.Match(GetMove(), @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$");

            turnNum = int.Parse(match.Groups[1].Value); //int

            string dots = match.Groups[2].Value;        // . or ...
            if (dots == ".") turnPlayer = "Black";
            else if (dots == "...") turnPlayer = "White";

            string move = match.Groups[3].Value.Trim();
            comment = match.Groups[4].Value;

            ParseMove(move);
        }

        private void ParseMove(string move)
        {
            if (move == PassToken) //Pass
            {
                isPass = true;
                square = PassToken;
                return;
            }
            
            System.Text.RegularExpressions.Match match = Regex.Match(move, @"^([a-h][1-8])$");

            isPass = false;
            square = match.Groups[1].Value;
        }

        // --- OTHELLO MOVE ACCESSORS ---
        public static Dictionary<string, object> GetMoveData(OthelloMove move)
        {
            return new Dictionary<string, object>
            {
                { "Turn",   move.turnNum    },
                { "Player", move.turnPlayer },
                { "Square", move.square     },
                { "IsPass", move.isPass     }
            };
        }

        public static string GetMoveComment(OthelloMove move)
        {
            return move.comment;
        }
    }
}

