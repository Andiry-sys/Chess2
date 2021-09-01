using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SrcChess2 {
    /// <summary>Base class for Search Engine</summary>
    public abstract class SearchEngine {

        #region Inner Class
        /// <summary>Interface to implement to do a search</summary>
        public interface ITrace {
            /// <summary>
            /// Search trace
            /// </summary>
            /// <param name="iDepth">       Depth of the move</param>
            /// <param name="ePlayerColor"> Player's color</param>
            /// <param name="movePos">      Move position</param>
            /// <param name="iPts">         Points for the board</param>
            void TraceSearch(int iDepth, ChessBoard.PlayerE ePlayerColor, Move movePos, int iPts);
        };

        private struct IndexPoint : IComparable<IndexPoint> {
            public int     iIndex;
            public int     iPoints;

            public int CompareTo(IndexPoint Other) {
                int     iRetVal;
            
                if (iPoints < Other.iPoints) {
                    iRetVal = 1;
                } else if (iPoints > Other.iPoints) {
                    iRetVal = -1;
                } else {
                    iRetVal = (iIndex < Other.iIndex) ? -1 : 1;
                }
                return(iRetVal);
            }
        }

        #endregion

        #region Members
        /// <summary>Working search engine</summary>
        private static SearchEngine             m_searchEngineWorking = null;
        /// <summary>true to cancel the search</summary>
        protected static bool                   m_bCancelSearch = false;
        /// <summary>Object where to redirect the trace if any</summary>
        private ITrace                          m_trace;
        /// <summary>Random number generator</summary>
        private Random                          m_rnd;
        /// <summary>Random number generator (repetitive, seed = 0)</summary>
        private Random                          m_rndRep;
        #endregion

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="trace">    Trace object or null</param>
        /// <param name="rnd">      Random object</param>
        /// <param name="rndRep">   Repetitive random object</param>
        protected SearchEngine(ITrace trace, Random rnd, Random rndRep) {
            m_trace         =   trace;
            m_rnd           =   rnd;
            m_rndRep        =   rndRep;
        }

        /// <summary>
        /// Debugging routine
        /// </summary>
        /// <param name="iDepth">       Actual search depth</param>
        /// <param name="ePlayerColor"> Color doing the move</param>
        /// <param name="move">         Move</param>
        /// <param name="iPts">         Points for this move</param>
        protected void TraceSearch(int iDepth, ChessBoard.PlayerE ePlayerColor, Move move, int iPts) {
            if (m_trace != null) {
                m_trace.TraceSearch(iDepth, ePlayerColor, move, iPts);
            }
        }

        /// <summary>
        /// Cancel the search
        /// </summary>
        public static void CancelSearch() {
            m_bCancelSearch = true;
        }

        /// <summary>
        /// Return true if search engine is busy
        /// </summary>
        public static bool IsSearchEngineBusy {
            get {
                return(m_searchEngineWorking != null);
            }
        }

        /// <summary>
        /// Return true if the search has been canceled
        /// </summary>
        public static bool IsSearchHasBeenCanceled {
            get {
                return(m_bCancelSearch);
            }
        }

        /// <summary>
        /// Sort move list using the specified point array so the highest point move come first
        /// </summary>
        /// <param name="moveList"> Source move list to sort</param>
        /// <param name="arrPoints">Array of points for each move</param>
        /// <returns>
        /// Sorted move list
        /// </returns>
        protected static List<Move> SortMoveList(List<Move> moveList, int[] arrPoints) {
            List<Move>      moveListRetVal;
            IndexPoint[]    arrIndexPoint;
            
            moveListRetVal = new List<Move>(moveList.Count);
            arrIndexPoint  = new IndexPoint[arrPoints.Length];
            for (int iIndex = 0; iIndex < arrIndexPoint.Length; iIndex++) {
                arrIndexPoint[iIndex].iPoints = arrPoints[iIndex];
                arrIndexPoint[iIndex].iIndex  = iIndex;
            }
            Array.Reverse(arrIndexPoint);
            Array.Sort<IndexPoint>(arrIndexPoint);
            for (int iIndex = 0; iIndex < arrIndexPoint.Length; iIndex++) {
                moveListRetVal.Add(moveList[arrIndexPoint[iIndex].iIndex]);
            }
            return(moveListRetVal);
        }

        /// <summary>
        /// Find the best move using a specific search method
        /// </summary>
        /// <param name="chessBoard">       Chess board</param>
        /// <param name="searchMode">       Search mode</param>
        /// <param name="ePlayer">          Player doing the move</param>
        /// <param name="moveList">         Move list</param>
        /// <param name="arrIndex">         Order of evaluation of the moves</param>
        /// <param name="posInfo">          Position information</param>
        /// <param name="moveBest">         Best move found</param>
        /// <param name="iPermCount">       Total permutation evaluated</param>
        /// <param name="iCacheHit">        Number of moves found in the translation table cache</param>
        /// <param name="iMaxDepth">        Maximum depth to use</param>
        /// <returns>
        /// true if a move has been found
        /// </returns>
        protected abstract bool FindBestMove(ChessBoard             chessBoard,
                                             SearchMode             searchMode,
                                             ChessBoard.PlayerE     ePlayer,
                                             List<Move>             moveList,
                                             int[]                  arrIndex,
                                             ChessBoard.PosInfoS    posInfo,
                                             ref Move               moveBest,
                                             out int                iPermCount,
                                             out int                iCacheHit,
                                             out int                iMaxDepth);

        /// <summary>
        /// Find the best move for a player using a specific method
        /// </summary>
        /// <param name="board">            Board</param>
        /// <param name="searchMode">       Search mode</param>
        /// <param name="ePlayer">          Player making the move</param>
        /// <param name="dispatcher">       Dispatcher of the main thread if function is called on a background thread</param>
        /// <param name="actionFoundMove">  Action to call with the found move</param>
        /// <param name="cookie">           Cookie to pass to the action</param>
        private void FindBestMove<T>(ChessBoard         board,
                                     SearchMode         searchMode,
                                     ChessBoard.PlayerE ePlayer,
                                     Dispatcher         dispatcher,
                                     Action<T,MoveExt>  actionFoundMove,
                                     T                  cookie) {
            bool                bRetVal = false;
            List<Move>          moveList;
            MoveExt             moveExt;
            Move                move;
            int[]               arrIndex;
            int                 iSwapIndex;
            int                 iTmp;
            int                 iPermCount;
            int                 iCacheHit;
            int                 iMaxDepth;
            Random              rnd;
            ChessBoard.PosInfoS posInfo;
            
            moveList    = board.EnumMoveList(ePlayer, true /*bMoveList*/, out posInfo);
            arrIndex    = new int[moveList.Count];
            for (int iIndex = 0; iIndex < moveList.Count; iIndex++) {
                arrIndex[iIndex] = iIndex;
            }
            if (searchMode.m_eRandomMode != SearchMode.RandomModeE.Off) {
                rnd = (searchMode.m_eRandomMode == SearchMode.RandomModeE.OnRepetitive) ? m_rndRep : m_rnd;
                for (int iIndex = 0; iIndex < moveList.Count; iIndex++) {
                    iSwapIndex           = rnd.Next(moveList.Count);
                    iTmp                 = arrIndex[iIndex];
                    arrIndex[iIndex]     = arrIndex[iSwapIndex];
                    arrIndex[iSwapIndex] = iTmp;
                }
            }
            move.StartPos           = 0;
            move.EndPos             = 0;
            move.OriginalPiece      = ChessBoard.PieceE.None;
            move.Type               = Move.TypeE.Normal;
            bRetVal                 = FindBestMove(board, searchMode, ePlayer, moveList, arrIndex, posInfo, ref move, out iPermCount, out iCacheHit, out iMaxDepth);
            moveExt                 = new MoveExt(move, "", iPermCount, iMaxDepth, iMaxDepth, 0);
            m_searchEngineWorking   = null;
            if (dispatcher != null) {
                dispatcher.Invoke(actionFoundMove, cookie, moveExt);
            } else {
                actionFoundMove(cookie, moveExt);
            }
        }

        /// <summary>
        /// Find the best move for the given player
        /// </summary>
        /// <param name="trace">            Trace object or null</param>
        /// <param name="rnd">              Random object</param>
        /// <param name="rndRep">           Repetitive random object</param>
        /// <param name="board">            Board</param>
        /// <param name="searchMode">       Search mode</param>
        /// <param name="ePlayer">          Player making the move</param>
        /// <param name="dispatcher">       Main thread dispatcher</param>
        /// <param name="actionMoveFound">  Action to execute when the find best move routine is done</param>
        /// <param name="cookie">           Cookie to pass to the actionMoveFound action</param>
        /// <returns>
        /// true if search has started, false if search engine is busy
        /// </returns>
        public static bool FindBestMove<T>(ITrace               trace,
                                           Random               rnd,
                                           Random               rndRep,
                                           ChessBoard           board,
                                           SearchMode           searchMode,
                                           ChessBoard.PlayerE   ePlayer,
                                           Dispatcher           dispatcher,
                                           Action<T,MoveExt>    actionMoveFound,
                                           T                    cookie) {
            bool            bRetVal;
            bool            bMultipleThread;
            SearchEngine    searchEngine;

            if (IsSearchEngineBusy) { 
                bRetVal = false;
            } else {
                bRetVal         = true;
                m_bCancelSearch = false;
                if (searchMode.m_eOption == SearchMode.OptionE.UseMinMax) {
                    searchEngine = new SearchEngineMinMax(trace, rnd, rndRep);
                } else {
                    searchEngine = new SearchEngineAlphaBeta(trace, rnd, rndRep);
                }
                bMultipleThread       = (searchMode.m_eThreadingMode == SearchMode.ThreadingModeE.DifferentThreadForSearch ||
                                         searchMode.m_eThreadingMode == SearchMode.ThreadingModeE.OnePerProcessorForSearch);
                m_searchEngineWorking = searchEngine;
                if (bMultipleThread) {
                    Task.Factory.StartNew(() => searchEngine.FindBestMove(board,
                                                                          searchMode,
                                                                          ePlayer,
                                                                          dispatcher,
                                                                          actionMoveFound,
                                                                          cookie));
                } else {
                    searchEngine.FindBestMove(board,
                                              searchMode,
                                              ePlayer,
                                              null /*dispatcher*/,
                                              actionMoveFound,
                                              cookie);
                }
            }
            return(bRetVal);
        }
    } // Class SearchEngine
} // Namespace
