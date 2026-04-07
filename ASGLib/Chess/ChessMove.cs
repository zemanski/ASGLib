using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//Nested Chess Namespace : Implementable Chess Match Class
namespace ASGLib.Chess
{

    //Chess Move Data Transfer Object
    public class ChessMoveDTO : Data.ASGMoveDTO
    {
        public string Piece { get; set; } = "";
        public string Destination { get; set; } = "";
        public string Disambiguation { get; set; } = "";
        public bool IsCapture { get; set; }
        public bool IsPromotion { get; set; }
        public bool IsCheck { get; set; }
        public bool IsCheckmate { get; set; }
        public string Comment { get; set; } = "";
    }

    //Class for Chess Move : Parses Standardized Chess Move Format
    public class ChessMove : ASGMove<ChessMoveDTO>
    {

        //Chess Move to DTO
        internal override ChessMoveDTO ToDTO()
        {
            return new ChessMoveDTO
            {
                MoveString      = GetMove(),
                TurnNum         = turnNum,
                TurnPlayer      = turnPlayer,
                Piece           = piece,
                Destination     = destination,
                Disambiguation  = disambiguation,
                IsCapture       = isCapture,
                IsPromotion     = isPromotion,
                IsCheck         = isCheck,
                IsCheckmate     = isCheckmate,
                Comment         = comment
            };
        }

        //Local Properties
        private bool isCapture;
        private bool isPromotion;
        private bool isCheck;
        private bool isCheckmate;
        private string comment;

        //Move Logic
        private string piece;
        private string destination;
        private string disambiguation;

        //Constructor
        internal ChessMove(string moveString) : base(moveString)
        {
            comment         = "";
            piece           = "";
            destination     = "";
            disambiguation  = "";

            MoveValidator();
            SelfParse();
        }

        //PARSING//
            //Extract Move Metadata
            private void SelfParse()
            {
                //Find Move Match (Pre-Validated)
                var match = Regex.Match(GetMove(), @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$");
                if (!match.Success) throw new Exception("Failed to parse move.");

                //Use Groups to Parse Metadata
                turnNum = int.Parse(match.Groups[1].Value);

                string dots = match.Groups[2].Value; // "." or "..."
                if (dots == ".") turnPlayer = "White";
                else if (dots == "...") turnPlayer = "Black";

                string move = match.Groups[3].Value.Trim();
                comment = match.Groups[4].Value;

                isCapture = move.Contains('x');
                isCheckmate = move.Contains('#');
                isCheck = !isCheckmate && move.Contains('+');
                isPromotion = move.Contains('=');
                
                //Parse Move Action
                ParseMove(move);
            }
            
            //Extract Move Action
            private void ParseMove(String move)
            {
                //Remove Processed Metadata
                move = move.Replace("+", "");
                move = move.Replace("#", "");

                // Handle castling
                if (move.Contains("O-O-O"))
                {
                    piece = "King";
                    destination = "CASTLE";
                    disambiguation = "LONG";
                    return;
                }
                if (move.Contains("O-O"))
                {
                    piece = "King";
                    destination = "CASTLE";
                    disambiguation = "SHORT";
                    return;
                }

                //MIGHT BE REDUNDANT: Validation
                var match = Regex.Match(move, @"^([KQRBN])?([a-h1-8]?)x?([a-h][1-8])(=([QRBN]))?$");
                if (!match.Success) throw new Exception($"Invalid move structure: {move}");

                //Parse By Groups
                string pieceLetter = match.Groups[1].Value;
                string disamb = match.Groups[2].Value;
                string dest = match.Groups[3].Value;

                piece = pieceLetter switch
                {
                    "K" => "King",
                    "Q" => "Queen",
                    "R" => "Rook",
                    "B" => "Bishop",
                    "N" => "Knight",
                    "" => "Pawn",
                    _ => throw new ArgumentException("Unknown piece type")
                };

                disambiguation = disamb;

                destination = dest;
            }

        //Validator for Move Format
        private void MoveValidator()
        {
            string move = GetMove();
            if (string.IsNullOrWhiteSpace(move)) throw new Exception("Move string cannot be empty.");
            if (!Regex.IsMatch(move, @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$")) throw new Exception($"Invalid move format: {move}");
        }

        //Accessor for Move Metadata : Bool Overload
        public static bool GetMoveData(ChessMove move, string property)
        {
            switch (property.ToLower())
            {
                case "capture":
                    return move.isCapture;
                case "promotion":
                    return move.isPromotion;
                case "check":
                    return move.isCheck;
                case "checkmate":
                    return move.isCheckmate;
                default:
                    throw new Exception("Invalid property name");
            }
        }

        //Accessor for Move Logic (subject to change pending controller design)
        public static Dictionary<string, object> GetMoveData(ChessMove move)
        {
            return new Dictionary<string, object>
            {
                { "Turn",           move.turnNum       },
                { "Player",         move.turnPlayer    },
                { "Piece",          move.piece         },
                { "Destination",    move.destination   },
                { "Disambiguation", move.disambiguation}
            };
        }

        //Accessor for comment
        public static string GetMoveComment(ChessMove move)
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
