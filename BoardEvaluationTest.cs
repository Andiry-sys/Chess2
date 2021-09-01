using System;

namespace SrcChess2 {
    /// <summary>Test board evaluation function</summary>
    public class BoardEvaluationTest : BoardEvaluationBasic {

        /// <summary>
        /// Class constructor
        /// </summary>
        public BoardEvaluationTest() {
        }

        /// <summary>
        /// Name of the evaluation method
        /// </summary>
        public override string Name {
            get {
                return("Test Version");
            }
        }

        /// <summary>
        /// Evaluates a board. The number of point is greater than 0 if white is in advantage, less than 0 if black is.
        /// </summary>
        /// <param name="pBoard">           Board.</param>
        /// <param name="piPiecesCount">    Number of each pieces</param>
        /// <param name="posInfo">          Information about pieces position</param>
        /// <param name="iWhiteKingPos">    Position of the white king</param>
        /// <param name="iBlackKingPos">    Position of the black king</param>
        /// <param name="bWhiteCastle">     White has castled</param>
        /// <param name="bBlackCastle">     Black has castled</param>
        /// <param name="iMoveCountDelta">  Number of possible white move - Number of possible black move</param>
        /// <returns>
        /// Points
        /// </returns>
        public override int Points(ChessBoard.PieceE[]   pBoard,
                                   int[]                 piPiecesCount,
                                   ChessBoard.PosInfoS   posInfo,
                                   int                   iWhiteKingPos,
                                   int                   iBlackKingPos,
                                   bool                  bWhiteCastle,
                                   bool                  bBlackCastle,
                                   int                   iMoveCountDelta) {
            int     iRetVal = 0;
            
            for (int iIndex = 0; iIndex < piPiecesCount.Length; iIndex++) {
                iRetVal += s_piPiecePoint[iIndex] * piPiecesCount[iIndex];
            }
            if (pBoard[12] == ChessBoard.PieceE.Pawn) {
                iRetVal -= 4;
            }
            if (pBoard[52] == (ChessBoard.PieceE.Pawn | ChessBoard.PieceE.Black)) {
                iRetVal += 4;
            }
            if (bWhiteCastle) {
                iRetVal += 10;
            }
            if (bBlackCastle) {
                iRetVal -= 10;
            }
            iRetVal += iMoveCountDelta;
            return(iRetVal);
        }
    } // Class BoardEvaluationTest
} // Namespace
