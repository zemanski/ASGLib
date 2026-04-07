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
    //Othello moves are a single algebraic square (e.g. "f5") or the pass token "PA"
    //Pass moves are legal when a player has no valid placement on the board
    //Black always moves first in standard Othello (GameType 50)
    public class OthelloMove : ASGMove<OthelloMoveDTO>
    {

        //Pass Token : Canonical representation of a forced pass in PDN Othello
        private const string PassToken = "PA";

        //Local Properties
        private string square;
        private bool isPass;
        private string comment;

        //Constructor
        internal OthelloMove(string moveString) : base(moveString)
        {
            square = "";
            isPass = false;
            comment = "";

            MoveValidator();
            SelfParse();
        }

        //PARSING//
        //Extract Move Metadata and Move Action
        private void SelfParse()
        {
            //Find Move Match (Pre-Validated)
            var match = Regex.Match(GetMove(), @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$");
            if (!match.Success) throw new Exception("Failed to parse Othello move: " + GetMove());

            //Use Groups to Parse Metadata
            turnNum = int.Parse(match.Groups[1].Value);

            string dots = match.Groups[2].Value;
            if (dots == ".") turnPlayer = "Black";
            else if (dots == "...") turnPlayer = "White";

            string move = match.Groups[3].Value.Trim();
            comment = match.Groups[4].Value;

            //Parse Move Action
            ParseMove(move);
        }

        //Extract Move Action : Square Placement or Pass
        private void ParseMove(string move)
        {
            //Pass Move : Player has no legal placement this turn
            if (move == PassToken)
            {
                isPass = true;
                square = PassToken;
                return;
            }

            //Standard Placement : Validate Algebraic Square Format
            var match = Regex.Match(move, @"^([a-h][1-8])$");
            if (!match.Success) throw new Exception("Invalid Othello move: " + move);

            isPass = false;
            square = match.Groups[1].Value;
        }

        //Validator for Move Format
        private void MoveValidator()
        {
            string move = GetMove();
            if (string.IsNullOrWhiteSpace(move)) throw new Exception("Move string cannot be empty.");
            if (!Regex.IsMatch(move, @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$")) throw new Exception("Invalid Othello move format: " + move);
        }

        //Othello Move to DTO
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

        //Accessor for Move Logic
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

        //Accessor for Move Metadata : Bool Overload
        public static bool GetMoveData(OthelloMove move, string property)
        {
            switch (property.ToLower())
            {
                case "pass":
                    return move.isPass;
                default:
                    throw new Exception("Invalid property name: " + property);
            }
        }

        //Accessor for Comment
        public static string GetMoveComment(OthelloMove move)
        {
            return move.comment;
        }

        //TEMPORARY: Expose Base MoveString Accessor
        public string DEBUG_GetMove()
        {
            return GetMove();
        }
    }
}

