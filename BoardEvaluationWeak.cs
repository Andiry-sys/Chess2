
namespace SrcChess2 {
    /// <summary>Board evaluation function used for beginner</summary>
    public class BoardEvaluationWeak : IBoardEvaluation {
        /// <summary>Value of each piece/color.</summary>
        static protected int[]      s_piPiecePoint;

        /// <summary>
        /// Static constructor
        /// </summary>
        static BoardEvaluationWeak() {
            s_piPiecePoint                                                                = new int[16];
            s_piPiecePoint[(int)ChessBoard.PieceE.Pawn]                                   = 100;
            s_piPiecePoint[(int)ChessBoard.PieceE.Rook]                                   = 100;
            s_piPiecePoint[(int)ChessBoard.PieceE.Knight]                                 = 100;
            s_piPiecePoint[(int)ChessBoard.PieceE.Bishop]                                 = 100;
            s_piPiecePoint[(int)ChessBoard.PieceE.Queen]                                  = 100;
            s_piPiecePoint[(int)ChessBoard.PieceE.King]                                   = 1000000;
            s_piPiecePoint[(int)(ChessBoard.PieceE.Pawn | ChessBoard.PieceE.Black)]       = -100;
            s_piPiecePoint[(int)(ChessBoard.PieceE.Rook | ChessBoard.PieceE.Black)]       = -100;
            s_piPiecePoint[(int)(ChessBoard.PieceE.Knight | ChessBoard.PieceE.Black)]     = -100;
            s_piPiecePoint[(int)(ChessBoard.PieceE.Bishop | ChessBoard.PieceE.Black)]     = -100;
            s_piPiecePoint[(int)(ChessBoard.PieceE.Queen | ChessBoard.PieceE.Black)]      = -100;
            s_piPiecePoint[(int)(ChessBoard.PieceE.King | ChessBoard.PieceE.Black)]       = -1000000;
        }

        /// <summary>
        /// Name of the evaluation method
        /// </summary>
        public virtual string Name {
            get {
                return("Beginner");
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
        public virtual int Points(ChessBoard.PieceE[]   pBoard,
                                  int[]                 piPiecesCount,
                                  ChessBoard.PosInfoS   posInfo,
                                  int                   iWhiteKingPos,
                                  int                   iBlackKingPos,
                                  bool                  bWhiteCastle,
                                  bool                  bBlackCastle,
                                  int                   iMoveCountDelta) {
            int iRetVal = 0;
            
            for (int iIndex = 0; iIndex < piPiecesCount.Length; iIndex++) {
                iRetVal += s_piPiecePoint[iIndex] * piPiecesCount[iIndex];
            }
            return(iRetVal);
        }
    } // Class BoardEvaluationWeak
} // Namespace
