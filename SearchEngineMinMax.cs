using System;
using System.Collections.Generic;

namespace SrcChess2 {
    /// <summary>Base class for Search Engine</summary>
    public sealed class SearchEngineMinMax : SearchEngine {

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="trace">    Trace object or null</param>
        /// <param name="rnd">      Random object</param>
        /// <param name="rndRep">   Repetitive random object</param>
        public SearchEngineMinMax(ITrace trace, Random rnd, Random rndRep) : base(trace, rnd, rndRep) {
        }

        /// <summary>
        /// Minimum/maximum depth first search
        /// </summary>
        /// <param name="board">            Chess board</param>
        /// <param name="searchMode">       Search mode</param>
        /// <param name="ePlayer">          Player doing the move</param>
        /// <param name="iDepth">           Actual search depth</param>
        /// <param name="iWhiteMoveCount">  Number of moves white can do</param>
        /// <param name="iBlackMoveCount">  Number of moves black can do</param>
        /// <param name="iPermCount">       Total permutation evaluated</param>
        /// <returns>
        /// Points to give for this move
        /// </returns>
        private int MinMax(ChessBoard board, SearchMode searchMode, ChessBoard.PlayerE ePlayer, int iDepth, int iWhiteMoveCount, int iBlackMoveCount, ref int iPermCount) {
            int                         iRetVal;
            int                         iMoveCount;
            int                         iPts;
            List<Move>                  moveList;
            ChessBoard.RepeatResultE    eResult;
            
            if (board.IsEnoughPieceForCheckMate()) {
                if (iDepth == 0 || m_bCancelSearch) {
                    iRetVal = (ePlayer == ChessBoard.PlayerE.Black) ? -board.Points(searchMode, ePlayer, 0, iWhiteMoveCount - iBlackMoveCount, ChessBoard.s_posInfoNull, ChessBoard.s_posInfoNull) :
                                                                       board.Points(searchMode, ePlayer, 0, iWhiteMoveCount - iBlackMoveCount, ChessBoard.s_posInfoNull, ChessBoard.s_posInfoNull);
                    iPermCount++;
                } else {
                    moveList    = board.EnumMoveList(ePlayer);
                    iMoveCount  = moveList.Count;
                    if (ePlayer == ChessBoard.PlayerE.White) {
                        iWhiteMoveCount = iMoveCount;
                    } else {
                        iBlackMoveCount = iMoveCount;
                    }
                    if (iMoveCount == 0) {
                        if (board.IsCheck(ePlayer)) {
                            iRetVal = -1000000 - iDepth;
                        } else {
                            iRetVal = 0; // Draw
                        }
                    } else {
                        iRetVal  = Int32.MinValue;
                        foreach (Move move in moveList) {
                            eResult = board.DoMoveNoLog(move);
                            if (eResult == ChessBoard.RepeatResultE.NoRepeat) {
                                iPts = -MinMax(board,
                                               searchMode,
                                               (ePlayer == ChessBoard.PlayerE.Black) ? ChessBoard.PlayerE.White : ChessBoard.PlayerE.Black,
                                               iDepth - 1,
                                               iWhiteMoveCount,
                                               iBlackMoveCount,
                                               ref iPermCount);
                            } else {
                                iPts = 0;
                            }
                            board.UndoMoveNoLog(move);
                            if (iPts > iRetVal) {
                                iRetVal = iPts;
                            }
                        }
                    }
                }
            } else {
                iRetVal = 0;
            }
            return(iRetVal);
        }

        /// <summary>
        /// Find the best move for a player using minmax search
        /// </summary>
        /// <param name="board">        Chess board</param>
        /// <param name="searchMode">   Search mode</param>
        /// <param name="ePlayer">      Color doing the move</param>
        /// <param name="moveList">     Move list</param>
        /// <param name="arrIndex">     Order of evaluation of the moves</param>
        /// <param name="iDepth">       Maximum depth</param>
        /// <param name="moveBest">     Best move found</param>
        /// <param name="iPermCount">   Total permutation evaluated</param>
        /// <returns>
        /// true if a move has been found
        /// </returns>
        private bool FindBestMoveUsingMinMaxAtDepth(ChessBoard board, SearchMode searchMode, ChessBoard.PlayerE ePlayer, List<Move> moveList, int[] arrIndex, int iDepth, ref Move moveBest, out int iPermCount) {
            bool                        bRetVal = false;
            Move                        move;
            int                         iPts;
            int                         iWhiteMoveCount;
            int                         iBlackMoveCount;
            int                         iBestPts;
            ChessBoard.RepeatResultE    eResult;
            
            iPermCount  = 0;
            iBestPts    = Int32.MinValue;
            if (ePlayer == ChessBoard.PlayerE.White) {
                iWhiteMoveCount = moveList.Count;
                iBlackMoveCount = 0;
            } else {
                iWhiteMoveCount = 0;
                iBlackMoveCount = moveList.Count;
            }
            foreach (int iIndex in arrIndex) {
                move    = moveList[iIndex];
                eResult = board.DoMoveNoLog(move);
                if (eResult == ChessBoard.RepeatResultE.NoRepeat) {
                    iPts = -MinMax(board,
                                   searchMode,
                                   (ePlayer == ChessBoard.PlayerE.Black) ? ChessBoard.PlayerE.White : ChessBoard.PlayerE.Black,
                                   iDepth - 1,
                                   iWhiteMoveCount,
                                   iBlackMoveCount,
                                   ref iPermCount);
                } else {
                    iPts = 0;
                }                                   
                board.UndoMoveNoLog(move);
                if (iPts > iBestPts) {
                    TraceSearch(iDepth, ePlayer, move, iPts);
                    iBestPts = iPts;
                    moveBest = move;
                    bRetVal  = true;
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Find the best move for a player using minmax search
        /// </summary>
        /// <param name="board">        Chess board</param>
        /// <param name="searchMode">   Search mode</param>
        /// <param name="ePlayer">      Color doing the move</param>
        /// <param name="moveList">     Move list</param>
        /// <param name="arrIndex">     Order of evaluation of the moves</param>
        /// <param name="posInfo">      Information about pieces attacks</param>
        /// <param name="moveBest">     Best move found</param>
        /// <param name="iPermCount">   Nb of permutations evaluated</param>
        /// <param name="iCacheHit">    Nb of cache hit</param>
        /// <param name="iMaxDepth">    Maximum depth evaluated</param>
        /// <returns>
        /// true if a move has been found
        /// </returns>
        protected override bool FindBestMove(ChessBoard             board,
                                             SearchMode             searchMode,
                                             ChessBoard.PlayerE     ePlayer,
                                             List<Move>             moveList, 
                                             int[]                  arrIndex,
                                             ChessBoard.PosInfoS    posInfo,
                                             ref Move               moveBest,
                                             out int                iPermCount,
                                             out int                iCacheHit,
                                             out int                iMaxDepth) {
            bool                bRetVal = false;
            DateTime            dtTimeOut;
            int                 iDepth;
            int                 iPermCountAtLevel;
            
            iPermCount  = 0;
            iCacheHit   = 0;
            if (searchMode.m_iSearchDepth == 0) {
                dtTimeOut   = DateTime.Now + TimeSpan.FromSeconds(searchMode.m_iTimeOutInSec);
                iDepth      = 0;
                do {
                    bRetVal     = FindBestMoveUsingMinMaxAtDepth(board, searchMode, ePlayer, moveList, arrIndex, iDepth + 1, ref moveBest, out iPermCountAtLevel);
                    iPermCount += iPermCountAtLevel;
                    iDepth++;
                } while (DateTime.Now < dtTimeOut);
                iMaxDepth = iDepth;
            } else {
                iMaxDepth = searchMode.m_iSearchDepth;
                bRetVal   = FindBestMoveUsingMinMaxAtDepth(board, searchMode, ePlayer, moveList, arrIndex, iMaxDepth, ref moveBest, out iPermCount);
            }
            return(bRetVal);
        }
    } // Class SearchEngineMinMax
} // Namespace
