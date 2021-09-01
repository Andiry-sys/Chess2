using System;
using System.Collections.Generic;

namespace SrcChess2.FICSInterface {

    /// <summary>
    /// Interface between a Chess Server and a chess control board
    /// </summary>
    public class GameIntf {
        /// <summary>Game being observed</summary>
        public  FICSGame                                Game { get; private set; }
        /// <summary>Chess board control</summary>
        public  SrcChess2.ChessBoardControl             ChessBoardCtl { get; private set; }
        /// <summary>true if board has already been created (move list has been received)</summary>
        public  bool                                    BoardCreated { get; private set; }
        /// <summary>Termination code</summary>
        public  TerminationE                            Termination { get; private set; }
        /// <summary>Termination error</summary>
        public  string                                  TerminationError { get; private set; }
        /// <summary>List of moves received before the game was created</summary>
        private Queue<Style12MoveLine>                  m_queueMove;
        /// <summary>Board used to convert move</summary>
        private SrcChess2.ChessBoard                    m_chessBoard;
        
       
        /// <summary>Total time used by white player</summary>
        private TimeSpan                                m_spanTotalWhiteTime;
        /// <summary>Total time used by black player</summary>
        private TimeSpan                                m_spanTotalBlackTime;
        /// <summary>List of initial moves</summary>
        private List<SrcChess2.MoveExt>                 m_listInitialMoves;
        /// <summary>Timer to handle move time out if any</summary>
        private System.Threading.Timer                  m_timerMoveTimeout;
        /// <summary>Move time out in seconds</summary>
        private int                                     m_iMoveTimeOut;
        /// <summary>Original maximum time allowed to both player</summary>
        private TimeSpan?                               m_spanOriginalMaxTime;
        /// <summary>Action to call when the game is terminating</summary>
        private Action<GameIntf,TerminationE,string>                    m_actionGameFinished;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="game">                 FICS Game</param>
        /// <param name="chessBoardCtl">        Chess board control</param>
        /// <param name="iMoveTimeout">         Move timeout in second</param>
        /// <param name="actionMoveTimeOut">    Action to call if move timeout</param>
        /// <param name="actionGameFinished">   Action to do when game is finished</param>
        public GameIntf(FICSGame                                game,
                        SrcChess2.ChessBoardControl             chessBoardCtl,
                        int?                                    iMoveTimeout,
                        Action<GameIntf>                        actionMoveTimeOut,
                        Action<GameIntf,TerminationE,string>    actionGameFinished) {
            Game                    = game;
            ChessBoardCtl           = chessBoardCtl;
            BoardCreated            = false;
            m_chessBoard            = new SrcChess2.ChessBoard();
           
            m_queueMove             = new Queue<Style12MoveLine>(16);
            m_spanTotalWhiteTime    = TimeSpan.Zero;
            m_spanTotalBlackTime    = TimeSpan.Zero;
            m_spanOriginalMaxTime   = (game.PlayerTimeInMin == 0) ? (TimeSpan?)null : TimeSpan.FromMinutes(game.PlayerTimeInMin);
            m_listInitialMoves      = new List<SrcChess2.MoveExt>(128);
            m_actionGameFinished    = actionGameFinished;
            Termination             = TerminationE.None;
            m_iMoveTimeOut          = iMoveTimeout.HasValue ? iMoveTimeout.Value : int.MaxValue;
           
            if (game.PlayerTimeInMin != 0 && chessBoardCtl != null) {
                chessBoardCtl.GameTimer.MaxWhitePlayTime = TimeSpan.FromMinutes(game.PlayerTimeInMin);
                chessBoardCtl.GameTimer.MaxBlackPlayTime = TimeSpan.FromMinutes(game.PlayerTimeInMin);
                chessBoardCtl.GameTimer.MoveIncInSec     = game.IncTimeInSec;
            }
            if (iMoveTimeout != 0 && actionMoveTimeOut != null) {
                m_timerMoveTimeout = new System.Threading.Timer(TimerCallback, actionMoveTimeOut, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
        }

        /// <summary>
        /// Called when a timeout occurs
        /// </summary>
        /// <param name="state"></param>
        private void TimerCallback(object state) {
            Action<GameIntf>    action;

            action = (Action<GameIntf>)state;
            if (Termination == TerminationE.None) {
                action(this);
            }
        }

        /// <summary>
        /// Send a message to the chess board control
        /// </summary>
        /// <param name="strMsg">   Message string</param>
        protected virtual void ShowMessage(string strMsg) {
            ChessBoardCtl.Dispatcher.Invoke((Action)(() => { ChessBoardCtl.ShowMessage(strMsg); }));
        }

        /// <summary>
        /// Send an error to the chess board control
        /// </summary>
        /// <param name="strError"> Error string</param>
        public virtual void ShowError(string strError) {
            ChessBoardCtl.Dispatcher.Invoke((Action)(() => { ChessBoardCtl.ShowError(strError); }));
        }

        /// <summary>
        /// Set the termination code and the error if any
        /// </summary>
        /// <param name="eTermination">             Termination code</param>
        /// <param name="strTerminationComment">    Termination comment</param>
        /// <param name="strError">                 Error</param>
        public virtual void SetTermination(TerminationE eTermination, string strTerminationComment, string strError) {
            string  strMsg = null;

            if (m_timerMoveTimeout != null) {
                m_timerMoveTimeout.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
            Termination = eTermination;
            switch (eTermination) {
            case TerminationE.None:
                throw new ArgumentException("Cannot terminate with none");
            case TerminationE.WhiteWin:
                strMsg = "White Win";
                break;
            case TerminationE.BlackWin:
                strMsg = "Black Win";
                break;
            case TerminationE.Draw:
                strMsg = "Draw";
                break;
            case TerminationE.Terminated:
                strMsg = "Game finished";
                break;
            case TerminationE.TerminatedWithErr:
                strMsg                  = strError ?? strError;
                strTerminationComment   = "";
                TerminationError        = strError;
                break;
            default:
                break;
            }
            if (!String.IsNullOrEmpty(strTerminationComment)) {
                strMsg += " - " + strTerminationComment;
            }
            if (m_actionGameFinished != null) {
                m_actionGameFinished(this, eTermination, strMsg);
            } else {
                if (eTermination != TerminationE.TerminatedWithErr) {
                    ShowMessage(strMsg);
                } else {
                    ShowError(strMsg);
                }
            }
        }

        /// <summary>
        /// Set the board content
        /// </summary>
        private void SetBoardControl() {
            Action                  del;

            del = () => { ChessBoardCtl.CreateGameFromMove(null /*chessBoardStarting*/,
                                                           m_listInitialMoves,
                                                           m_chessBoard.CurrentPlayer,
                                                           Game.WhitePlayer,
                                                           Game.BlackPlayer,
                                                          
                                                           TimeSpan.Zero,
                                                           TimeSpan.Zero);
                            ChessBoardCtl.GameTimer.ResetTo(Game.NextMovePlayer, Game.WhiteTimeSpan.Ticks, Game.BlackTimeSpan.Ticks);
                            ChessBoardCtl.Refresh();
                        };
            ChessBoardCtl.Dispatcher.Invoke(del);
        }

        /// <summary>
        /// Do a move
        /// </summary>
        /// <param name="move"> Move to be done</param>
        private void DoMove(SrcChess2.MoveExt move) {
            int                             iIncrementTimeInSec;
            int                             iMoveCount;
            TimeSpan                        span;
            SrcChess2.ChessBoard.PlayerE    ePlayer;

            lock(m_chessBoard) {
                ePlayer = ChessBoardCtl.NextMoveColor;
                ChessBoardCtl.DoMove(move);
                if (m_spanOriginalMaxTime.HasValue && Game.IncTimeInSec != 0) {
                    iMoveCount          = ChessBoardCtl.Board.MovePosStack.Count  + 1 / 2;
                    iIncrementTimeInSec = Game.IncTimeInSec * iMoveCount;
                    span                = m_spanOriginalMaxTime.Value + TimeSpan.FromSeconds(iIncrementTimeInSec);
                    if (ePlayer == SrcChess2.ChessBoard.PlayerE.Black) {
                        ChessBoardCtl.GameTimer.MaxBlackPlayTime = span;
                    } else {
                        ChessBoardCtl.GameTimer.MaxWhitePlayTime = span;
                    }
                }
                if (!ChessBoardCtl.SignalActionDone.WaitOne(0)) {
                    ChessBoardCtl.SignalActionDone.WaitOne();
                }
            }
        }

        
        

       
        
        

       

        

       
    }
}
