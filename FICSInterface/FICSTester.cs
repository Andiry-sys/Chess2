using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace SrcChess2.FICSInterface {

        
    /// <summary>
    /// Test FICS interface
    /// </summary>
    public class FICSTester {

        /// <summary>
        /// Testing version of the game interface
        /// </summary>
        private class GameIntfTest : GameIntf {
            /// <summary>Stream use to log error and message</summary>
            private System.IO.StreamWriter  m_streamLog;

            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="game">                 Game</param>
            /// <param name="chessBoardControl">    Chess board control if any</param>
            /// <param name="streamLog">            Stream where to send the log information</param>
            /// <param name="eventWaitHandle">      Use to inform background tester the game is terminated</param>
            /// <param name="iMoveTimeOut">         Move timeout in second</param>
            /// <param name="actionMoveTimeOut">    Action to call if move timeout</param>
            public GameIntfTest(FICSGame                    game,
                                SrcChess2.ChessBoardControl chessBoardControl,
                                System.IO.StreamWriter      streamLog,
                                EventWaitHandle             eventWaitHandle,
                                int                         iMoveTimeOut,
                                Action<GameIntf>            actionMoveTimeOut) : base(game, chessBoardControl, iMoveTimeOut, actionMoveTimeOut, null /*actionGameTerminated*/) {
                m_streamLog         = streamLog;
                EventWaitHandle     = eventWaitHandle;
            }

            /// <summary>
            /// Use to inform background runner the game is terminating
            /// </summary>
            public EventWaitHandle EventWaitHandle {
                get; private set;
            }

            /// <summary>
            /// Send an error message to the log file
            /// </summary>
            /// <param name="strError"> Error message</param>
            public override void ShowError(string strError) {
                lock(m_streamLog) {
                    m_streamLog.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ": " + "*** Error -  GameId: " + Game.GameId.ToString() + " " + strError);
                    m_streamLog.Flush();
                }
                //if (ChessBoardCtl != null) {
                //    base.ShowError(strError);
                //}
            }

            /// <summary>
            /// Send a message to the log file
            /// </summary>
            /// <param name="strMsg">   Message</param>
            protected override void ShowMessage(string strMsg) {
                lock(m_streamLog) {
                    m_streamLog.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ": " + "*** Info -  GameId: " + Game.GameId.ToString() + " " + strMsg);
                    m_streamLog.Flush();
                }
            }

            /// <summary>
            /// Set the termination code and the error if any
            /// </summary>
            /// <param name="eTermination">             Termination code</param>
            /// <param name="strTerminationComment">    Termination comment</param>
            /// <param name="strError">                 Error</param>
            public override void SetTermination(TerminationE eTermination, string strTerminationComment, string strError) {
                base.SetTermination(eTermination, strTerminationComment, strError);
                if (EventWaitHandle != null) {
                    EventWaitHandle.Set();
                }
            }
        }

        /// <summary>
        /// Write a message to a log and to the debugger output
        /// </summary>
        /// <param name="writer">   Writer</param>
        /// <param name="strMsg">   Message</param>
        private static void LogWrite(System.IO.StreamWriter writer, string strMsg) {
            writer.WriteLine(strMsg);
            System.Diagnostics.Debug.WriteLine(strMsg);

        }
        /// <summary>
        /// Start a background game
        /// </summary>
        /// <param name="conn">         Connection to FICS server</param>
        /// <param name="chessBoardCtl">Chess board control</param>
        private static void BackgroundGame(FICSConnection conn, SrcChess2.ChessBoardControl chessBoardCtl) {
            string                  strError;
            List<FICSGame>          games;
            FICSGame                game;
            EventWaitHandle         eventWaitHandle;
            GameIntfTest            gameIntf;
            bool                    bGameFound;
            int                     iLastGameId = -1;

            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            bGameFound      = false;
            System.IO.FileStream stream = System.IO.File.Open("c:\\tmp\\chesslog.txt", System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write, System.IO.FileShare.Read);
            using (stream) {
                stream.Seek(0, System.IO.SeekOrigin.End);
                System.IO.StreamWriter writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8);
                using (writer) {
                    writer.WriteLine();
                    writer.WriteLine("Starting new session at " + DateTime.Now.ToString("HH:mm:ss"));
                    writer.WriteLine("-----------------------------------------");
                    writer.WriteLine();
                    do {
                        games = conn.GetGameList(true, 10);
                        game  = games.FirstOrDefault(x => (x.GameId   !=  iLastGameId                                                            &&
                                                           !x.IsPrivate                                                                          &&
                                                           x.GameType == FICSGame.GameTypeE.Lightning || x.GameType == FICSGame.GameTypeE.Blitz) &&
                                                           x.PlayerTimeInMin < 3 && x.IncTimeInSec < 5);
                        if (game != null) {
                            bGameFound  = true;
                            iLastGameId = game.GameId;
                            LogWrite(writer, DateTime.Now.ToString("HH:mm:ss") + ": " + "Found game: " + game.ToString());
                            writer.Flush();
                            gameIntf = new GameIntfTest(game, chessBoardCtl, writer, eventWaitHandle, 30 /*iMoveTimeOut*/, conn.GetTimeOutAction());
                            eventWaitHandle.Reset();
                            if (conn.ObserveGame(gameIntf, 10, out strError)) {
                                eventWaitHandle.WaitOne();
                                lock(writer) {
                                    writer.WriteLine("PGN Game");
                                    writer.WriteLine("----------------------");
                                   
                                    writer.WriteLine("----------------------");
                                }
                                if (gameIntf.Termination == TerminationE.TerminatedWithErr) {
                                    lock(writer) {
                                        LogWrite(writer, DateTime.Now.ToString("HH:mm:ss") + ": " + "Game " + gameIntf.Game.GameId.ToString() + " terminated with error - " + gameIntf.TerminationError);
                                    }
                                } else {
                                    lock(writer) {
                                       LogWrite(writer, DateTime.Now.ToString("HH:mm:ss") + ": " + "Game finished - " + gameIntf.Termination.ToString());
                                    }
                                }
                                lock(writer) {
                                    writer.Flush();
                                }
                                bGameFound = false;
                            } else {
                                lock(writer) {
                                    LogWrite(writer, "Games failed to start - " + strError ?? "???");
                                    writer.Flush();
                                }
                                Thread.Sleep(5000);
                            }
                        } else {
                            lock(writer) {
                                LogWrite(writer, "No games found - trying again in 5 sec.");
                                writer.Flush();
                            }
                            System.Threading.Thread.Sleep(5000);
                        }
                    } while (!bGameFound);
                    writer.WriteLine("Session end at " + DateTime.Now.ToString("HH:mm:ss"));
                }
            }
        }

        /// <summary>
        /// Start a background game
        /// </summary>
        /// <param name="conn">         Connection with FICS server</param>
        /// <param name="chessBoardCtl">Chess board control use to display the games</param>
        public static void StartBackgroundGame(FICSConnection conn, SrcChess2.ChessBoardControl chessBoardCtl) {
            Action  action;

            action = () => {BackgroundGame(conn, chessBoardCtl); };
            Task.Factory.StartNew(action);
        }
    }
}
