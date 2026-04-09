using ASGLib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ASGLib.Chess
{
    // Coordinate pair: "<fromSquare><toSquare>"  e.g. "e2e4", "g1f3", "e1h1"
    // Promotion:       "<from><to><piece>"       e.g. "e7e8Q"
    // Castling:        "0-0"  or  "0-0-0"        (zeros, not letter O)
    //
    // Uppercase = White piece, lowercase = Black piece.
    //   K/k Queen  Q/q  Rook R/r  Bishop B/b  Knight N/n  Pawn P/p
    //   '.' = empty square
    //
    // GameType tag value: "0"
    public class PlayChess : PlayASG
    {
        // --- CASTLING ---
        private bool whiteKingside = true;
        private bool whiteQueenside = true;
        private bool blackKingside = true;
        private bool blackQueenside = true;

        // --- EN-PASSANT ---
        private string enPassant = null;

        // --- IDENTITY ---
        protected override string PlayerOneName => "White";
        protected override string PlayerTwoName => "Black";
        protected override string GameTypeValue => "0";

        // --- CONSTRUCTOR ---
        public PlayChess(
            ASGOutput output = ASGOutput.None,
            string white = null,
            string black = null,
            string gameEvent = null,
            string gameSite = null,
            string gameDate = null,
            string gameRound = null,
            Dictionary<string, string> extraTags = null)
            : base(output, gameEvent, gameSite, gameDate, gameRound,
                   white, black, extraTags)
        { }

        // --- BOARD MAKE ---
        protected override void InitBoard()
        {
            char[] backRank = { 'R', 'N', 'B', 'Q', 'K', 'B', 'N', 'R' };
            for (int c = 0; c < 8; c++)
            {
                board[0, c] = char.ToLower(backRank[c]); // Black back rank
                board[1, c] = 'p';                        // Black pawns
                board[6, c] = 'P';                        // White pawns
                board[7, c] = backRank[c];                // White back rank
            }
            for (int r = 2; r <= 5; r++)
                for (int c = 0; c < 8; c++)
                    board[r, c] = '.';
        }

        // --- MOVE VALIDATE ---
        public override bool IsValidMove(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string mv = input.Trim();

            if (mv == "0-0")
                return ValidateKingsideCastle();
            if (mv == "0-0-0")
                return ValidateQueensideCastle();

            string promoTarget = null;
            if (Regex.IsMatch(mv, @"^[a-h][1-8][a-h][1-8][QRBN]$"))
            {
                promoTarget = mv.Substring(4, 1);
                mv = mv.Substring(0, 4);
            }

            if (!Regex.IsMatch(mv, @"^[a-h][1-8][a-h][1-8]$")) return false;

            int srcCol = FileIndex(mv[0]), srcRow = RankIndex(mv[1]);
            int dstCol = FileIndex(mv[2]), dstRow = RankIndex(mv[3]);

            if (!InBounds(srcRow, srcCol) || !InBounds(dstRow, dstCol)) return false;
            if (srcRow == dstRow && srcCol == dstCol) return false;

            char piece = board[srcRow, srcCol];
            if (piece == '.') return false;

            // Must be moving own piece
            bool pieceIsWhite = char.IsUpper(piece);
            if (pieceIsWhite == secondPlayerToMove) return false; // wrong side

            char dest = board[dstRow, dstCol];
            // Cannot capture own piece
            if (dest != '.' && char.IsUpper(dest) == char.IsUpper(piece)) return false;

            return CanReach(piece, srcRow, srcCol, dstRow, dstCol, promoTarget);
        }

        private bool ValidateKingsideCastle()
        {
            if (!secondPlayerToMove) // White
                return whiteKingside
                    && board[7, 4] == 'K' && board[7, 7] == 'R'
                    && board[7, 5] == '.' && board[7, 6] == '.';
            else                     // Black
                return blackKingside
                    && board[0, 4] == 'k' && board[0, 7] == 'r'
                    && board[0, 5] == '.' && board[0, 6] == '.';
        }

        private bool ValidateQueensideCastle()
        {
            if (!secondPlayerToMove) // White
                return whiteQueenside
                    && board[7, 4] == 'K' && board[7, 0] == 'R'
                    && board[7, 1] == '.' && board[7, 2] == '.' && board[7, 3] == '.';
            else                     // Black
                return blackQueenside
                    && board[0, 4] == 'k' && board[0, 0] == 'r'
                    && board[0, 1] == '.' && board[0, 2] == '.' && board[0, 3] == '.';
        }

        private bool CanReach(char piece, int sr, int sc, int dr, int dc,
                               string promoTarget)
        {
            char p = char.ToUpper(piece);
            int deltaR = dr - sr, deltaC = dc - sc;

            switch (p)
            {
                case 'P':
                    return ValidatePawnMove(piece, sr, sc, dr, dc);
                case 'N':
                    return (Math.Abs(deltaR) == 2 && Math.Abs(deltaC) == 1) ||
                                 (Math.Abs(deltaR) == 1 && Math.Abs(deltaC) == 2);
                case 'K':
                    return Math.Abs(deltaR) <= 1 && Math.Abs(deltaC) <= 1;
                case 'R':
                    return (deltaR == 0 || deltaC == 0) && SliderClear(sr, sc, dr, dc);
                case 'B':
                    return Math.Abs(deltaR) == Math.Abs(deltaC) && SliderClear(sr, sc, dr, dc);
                case 'Q':
                    return ((deltaR == 0 || deltaC == 0) || Math.Abs(deltaR) == Math.Abs(deltaC))
                                 && SliderClear(sr, sc, dr, dc);
            }
            return false;
        }

        private bool ValidatePawnMove(char piece, int sr, int sc, int dr, int dc)
        {
            bool isWhite = char.IsUpper(piece);
            int dir = isWhite ? -1 : 1;   // White moves up (decreasing row), Black down
            int start = isWhite ? 6 : 1;

            int deltaR = dr - sr, deltaC = dc - sc;

            if (deltaR == dir && deltaC == 0)   // Single push
                return board[dr, dc] == '.';

            if (deltaR == 2 * dir && deltaC == 0 && sr == start) // Double push
                return board[sr + dir, sc] == '.' && board[dr, dc] == '.';

            if (deltaR == dir && Math.Abs(deltaC) == 1) // Capture
            {
                string destSq = $"{(char)('a' + dc)}{(char)('8' - dr)}";
                char target = board[dr, dc];
                if (target != '.' && char.IsUpper(target) != char.IsUpper(piece))
                    return true;                    // Normal diagonal capture
                if (destSq == enPassant) return true; // En-passant
            }
            return false;
        }

        private bool SliderClear(int sr, int sc, int dr, int dc)
        {
            int stepR = Math.Sign(dr - sr), stepC = Math.Sign(dc - sc);
            int r = sr + stepR, c = sc + stepC;
            while (r != dr || c != dc)
            {
                if (board[r, c] != '.') return false;
                r += stepR; c += stepC;
            }
            return true;
        }

        // --- FORMAT MOVE ---
        protected override string FormatMoveToken(string input)
        {
            string mv = input.Trim();
            if (mv == "0-0") return "O-O";
            if (mv == "0-0-0") return "O-O-O";

            string promo = "";
            if (mv.Length == 5) { promo = "=" + mv[4]; mv = mv.Substring(0, 4); }

            string from = mv.Substring(0, 2);
            string to = mv.Substring(2, 2);

            char piece = board[RankIndex(from[1]), FileIndex(from[0])];
            char dest = board[RankIndex(to[1]), FileIndex(to[0])];

            bool isCapture = dest != '.' ||
                             (char.ToUpper(piece) == 'P' && to == enPassant);

            string piecePrefix = char.ToUpper(piece) switch
            {
                'K' => "K",
                'Q' => "Q",
                'R' => "R",
                'B' => "B",
                'N' => "N",
                _ => ""
            };

            string captureStr = isCapture ? "x" : "";

            // Pawn captures need the source file
            string disambig = "";
            if (char.ToUpper(piece) == 'P' && isCapture)
                disambig = from[0].ToString();

            return $"{piecePrefix}{disambig}{captureStr}{to}{promo}";
        }

        // --- APPLY MOVE ---
        protected override void ApplyMove(string input)
        {
            string mv = input.Trim();
            string prevEnPassant = enPassant;
            enPassant = null;

            // ── Castling ──────────────────────────────────────────────────────
            if (mv == "0-0") { ApplyKingsideCastle(); return; }
            if (mv == "0-0-0") { ApplyQueensideCastle(); return; }

            // ── Promotion suffix ──────────────────────────────────────────────
            char? promoPiece = null;
            if (mv.Length == 5) { promoPiece = mv[4]; mv = mv.Substring(0, 4); }

            int srcCol = FileIndex(mv[0]), srcRow = RankIndex(mv[1]);
            int dstCol = FileIndex(mv[2]), dstRow = RankIndex(mv[3]);

            char piece = board[srcRow, srcCol];
            board[srcRow, srcCol] = '.';

            // ── En-passant capture ────────────────────────────────────────────
            string destSq = mv.Substring(2, 2);
            if (char.ToUpper(piece) == 'P' && destSq == prevEnPassant)
            {
                int capturedRow = dstRow + (secondPlayerToMove ? -1 : 1);
                board[capturedRow, dstCol] = '.';
            }

            // ── Promotion ─────────────────────────────────────────────────────
            if (promoPiece.HasValue)
            {
                piece = secondPlayerToMove
                    ? char.ToLower(promoPiece.Value)
                    : promoPiece.Value;
            }

            board[dstRow, dstCol] = piece;

            // ── Record en-passant square after double pawn push ───────────────
            if (char.ToUpper(piece) == 'P' && Math.Abs(dstRow - srcRow) == 2)
            {
                int epRow = srcRow + (dstRow - srcRow) / 2;
                enPassant = $"{mv[2]}{(char)('8' - epRow)}";
            }

            // ── Update castling rights ────────────────────────────────────────
            if (piece == 'K') { whiteKingside = false; whiteQueenside = false; }
            if (piece == 'k') { blackKingside = false; blackQueenside = false; }
            if (srcRow == 7 && srcCol == 7) whiteKingside = false;
            if (srcRow == 7 && srcCol == 0) whiteQueenside = false;
            if (srcRow == 0 && srcCol == 7) blackKingside = false;
            if (srcRow == 0 && srcCol == 0) blackQueenside = false;
        }

        private void ApplyKingsideCastle()
        {
            if (!secondPlayerToMove)
            {
                board[7, 4] = '.'; board[7, 5] = 'R'; board[7, 6] = 'K'; board[7, 7] = '.';
                whiteKingside = false; whiteQueenside = false;
            }
            else
            {
                board[0, 4] = '.'; board[0, 5] = 'r'; board[0, 6] = 'k'; board[0, 7] = '.';
                blackKingside = false; blackQueenside = false;
            }
        }

        private void ApplyQueensideCastle()
        {
            if (!secondPlayerToMove)
            {
                board[7, 4] = '.'; board[7, 3] = 'R'; board[7, 2] = 'K'; board[7, 0] = '.';
                whiteKingside = false; whiteQueenside = false;
            }
            else
            {
                board[0, 4] = '.'; board[0, 3] = 'r'; board[0, 2] = 'k'; board[0, 0] = '.';
                blackKingside = false; blackQueenside = false;
            }
        }
    }
}
