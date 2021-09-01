using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SrcChess2 {
    /// <summary>
    /// Defines a chess move
    /// </summary>
    public struct Move {
        /// <summary>Type of possible move</summary>
        public enum TypeE : byte {
            /// <summary>Normal move</summary>
            Normal                  = 0,
            /// <summary>Pawn which is promoted to a queen</summary>
            PawnPromotionToQueen    = 1,
            /// <summary>Castling</summary>
            Castle                  = 2,
            /// <summary>Prise en passant</summary>
            EnPassant               = 3,
            /// <summary>Pawn which is promoted to a rook</summary>
            PawnPromotionToRook     = 4,
            /// <summary>Pawn which is promoted to a bishop</summary>
            PawnPromotionToBishop   = 5,
            /// <summary>Pawn which is promoted to a knight</summary>
            PawnPromotionToKnight   = 6,
            /// <summary>Pawn which is promoted to a pawn</summary>
            PawnPromotionToPawn     = 7,
            /// <summary>Piece type mask</summary>
            MoveTypeMask            = 15,
            /// <summary>The move eat a piece</summary>
            PieceEaten              = 16,
            /// <summary>Move coming from book opening</summary>
            MoveFromBook            = 32
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="eOriginalPiece">   Piece which has been eaten if any</param>
        /// <param name="iStartPos">        Starting position</param>
        /// <param name="iEndPos">          Ending position</param>
        /// <param name="eType">            Move type</param>
        public Move(ChessBoard.PieceE eOriginalPiece, int iStartPos, int iEndPos, TypeE eType) {
            OriginalPiece   = eOriginalPiece;
            StartPos        = (byte)iStartPos;
            EndPos          = (byte)iEndPos;
            Type            = eType;
        }

        /// <summary>Original piece if a piece has been eaten</summary>
        public ChessBoard.PieceE    OriginalPiece;
        /// <summary>Start position of the move (0-63)</summary>
        public byte                 StartPos;
        /// <summary>End position of the move (0-63)</summary>
        public byte                 EndPos;
        /// <summary>Type of move</summary>
        public TypeE                Type;
    }
}
