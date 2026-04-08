using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//Nested Chess Namespace : Chess Move and Move Data Transfer Object
namespace ASGLib.Chess
{

    //Chess Move Data Transfer Object
    internal class ChessMoveDTO : Data.ASGMoveDTO
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
    internal class ChessMove : ASGMove<ChessMoveDTO>
    {
        // --- CHESS MOVE MEMBERS ---
        private bool isCapture;
        private bool isPromotion;
        private bool isCheck;
        private bool isCheckmate;
        private string comment;
        private string piece;
        private string destination;
        private string disambiguation;

        // --- CHESS MOVE CONSTRUCTOR ---
        internal ChessMove(string moveString) : base(moveString)
        {
            comment = "";
            piece = "";
            destination = "";
            disambiguation = "";

            MoveValidator();
            SelfParse();
        }

        // --- CHESS MOVE VALIDATOR ---
        private void MoveValidator()
        {
            string move = GetMove();
            if (string.IsNullOrWhiteSpace(move)) throw new Exception("Move string cannot be empty.");
            if (!Regex.IsMatch(move, @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$")) throw new Exception($"Invalid move format: {move}");
        }

        // --- CHESS MOVE SERIALIZER ---
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

        // --- CHESS MOVE PARSING ---
        private void SelfParse() // Parse Metadata and Pass Move Action
        {
            System.Text.RegularExpressions.Match match = 
                Regex.Match(GetMove(), @"^(\d+)(\.\.\.|\.{1})([^{}]+)\{([^{}]*)\}$");

            turnNum = int.Parse(match.Groups[1].Value);     // turn int

            string dots = match.Groups[2].Value;            // "." or "..."
            if (dots == ".") turnPlayer = "White";
            else if (dots == "...") turnPlayer = "Black";

            string move = match.Groups[3].Value.Trim();     // comment (or empty)
            comment = match.Groups[4].Value;

            isCapture = move.Contains('x');
            isCheckmate = move.Contains('#');
            isCheck = !isCheckmate && move.Contains('+');
            isPromotion = move.Contains('=');
                
            ParseMove(move);
        }
           
        private void ParseMove(String move) // Extract Move Action
        {
            move = move.Replace("+", ""); //Preprocessed
            move = move.Replace("#", "");

            if (move.Contains("O-O-O"))   //Castling
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

            System.Text.RegularExpressions.Match match = Regex.Match(move, @"^([KQRBN])?([a-h1-8]?)x?([a-h][1-8])(=([QRBN]))?$");

            piece = match.Groups[1].Value switch
            {
                "K" => "King",
                "Q" => "Queen",
                "R" => "Rook",
                "B" => "Bishop",
                "N" => "Knight",
                "" => "Pawn",
                _ => throw new ArgumentException("Unknown piece type")
            };

            disambiguation = match.Groups[2].Value;

            destination = match.Groups[3].Value;
        }

        // --- CHESS MOVE ACCESSORS ---
        internal static bool GetMoveData(ChessMove move, string property) // boolean
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
        
        internal static Dictionary<string, object> GetMoveData(ChessMove move) // action
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

        public static string GetMoveComment(ChessMove move) // comment
        {
            return move.comment;
        }
    }
}
