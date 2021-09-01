using System;

namespace SrcChess2 {
    /// <summary>
    /// Zobrist key implementation.
    /// </summary>
    public static class ZobristKey {

        /// <summary>Random value for each piece/position</summary>
        static private Int64[]              s_pi64RndTable;

        /// <summary>
        /// Static constructor. Use to create the random value for each case of the board.
        /// </summary>
        static ZobristKey() {
            Random  rnd;
            long    lPart1;
            long    lPart2;
            long    lPart3;
            long    lPart4;
            
            rnd            = new Random(0);
            s_pi64RndTable = new Int64[64 * 16];
            for (int i = 0; i < 64 * 16; i++) {
                lPart1            = (long)rnd.Next(65536);
                lPart2            = (long)rnd.Next(65536);
                lPart3            = (long)rnd.Next(65536);
                lPart4            = (long)rnd.Next(65536);
                s_pi64RndTable[i] = (lPart1 << 48) | (lPart2 << 32) | (lPart3 << 16) | lPart4;
            }
        }

        /// <summary>
        /// Update the Zobrist key using the specified move
        /// </summary>
        /// <param name="i64ZobristKey">Zobrist key</param>
        /// <param name="iPos">         Piece position</param>
        /// <param name="eOldPiece">    Old value</param>
        /// <param name="eNewPiece">    New value</param>
        public static long UpdateZobristKey(long i64ZobristKey, int iPos, ChessBoard.PieceE eOldPiece, ChessBoard.PieceE eNewPiece) {
            int     iBaseIndex;
            
            iBaseIndex     = iPos << 4;
            i64ZobristKey ^= s_pi64RndTable[iBaseIndex + ((int)eOldPiece)] ^
                             s_pi64RndTable[iBaseIndex + ((int)eNewPiece)];
            return(i64ZobristKey);                             
        }

        /// <summary>
        /// Update the Zobrist key using the specified move
        /// </summary>
        /// <param name="i64ZobristKey">Zobrist key</param>
        /// <param name="iPos1">        Piece position</param>
        /// <param name="eOldPiece1">   Old value</param>
        /// <param name="eNewPiece1">   New value</param>
        /// <param name="iPos2">        Piece position</param>
        /// <param name="eOldPiece2">   Old value</param>
        /// <param name="eNewPiece2">   New value</param>
        public static long UpdateZobristKey(long               i64ZobristKey,
                                            int                iPos1,
                                            ChessBoard.PieceE  eOldPiece1,
                                            ChessBoard.PieceE  eNewPiece1,
                                            int                iPos2,
                                            ChessBoard.PieceE  eOldPiece2,
                                            ChessBoard.PieceE  eNewPiece2) {
            int     iBaseIndex1;
            int     iBaseIndex2;
            
            iBaseIndex1    = iPos1 << 4;
            iBaseIndex2    = iPos2 << 4;
            i64ZobristKey ^= s_pi64RndTable[iBaseIndex1 + ((int)eOldPiece1)] ^
                             s_pi64RndTable[iBaseIndex1 + ((int)eNewPiece1)] ^
                             s_pi64RndTable[iBaseIndex2 + ((int)eOldPiece2)] ^
                             s_pi64RndTable[iBaseIndex2 + ((int)eNewPiece2)];
            return(i64ZobristKey);                             
        }

        /// <summary>
        /// Compute the zobrist key for a board
        /// </summary>
        /// <param name="peBoard">      Board</param>
        public static long ComputeBoardZobristKey(ChessBoard.PieceE[] peBoard) {
            long    lRetVal = 0;
            
            for (int iIndex = 0; iIndex < 64; iIndex++) {
                lRetVal ^= s_pi64RndTable[(iIndex << 4) + (int)peBoard[iIndex]];
            }
            return(lRetVal);
        }
    } // Class ZobristKey
} // Namespace
