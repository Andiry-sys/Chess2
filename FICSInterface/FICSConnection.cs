using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SrcChess2.FICSInterface {
    /// <summary>
    /// Interface with FICS (Free Chess Interface Server)
    /// </summary>
    /// <remarks>
    /// Implements playing game with a human through FICS
    /// Implements chat?
    /// </remarks>
    public class FICSConnection : IDisposable {

        /// <summary>Current command executing</summary>
        private enum CmdExecutingE {
            /// <summary>No command executing</summary>
            None,
            /// <summary>Before login to the server</summary>
            PreLogin,
            /// <summary>Login to the server</summary>
            Login,
            /// <summary>Getting a game move list</summary>
            MoveList,
            /// <summary>Getting the list of games</summary>
            GameList,
            /// <summary>Get the date from the server</summary>
            Date,
            /// <summary>List of variables values</summary>
            VariableList
        }

        #region Inner Class
        /// <summary>
        /// State of the listening automaton
        /// </summary>
        private class AutomatonState : IDisposable {
            /// <summary>Command being executed</summary>
            public CmdExecutingE                    CmdExecuting { get; private set; }
            /// <summary>Execution phase. 0 for awaiting first part</summary>
            public int                              Phase { get; set; }
            /// <summary>true if listening to single move of at least one game</summary>
            public bool                             SingleMoveListening { get; private set; }
            /// <summary>Time at which the command started to be processed</summary>
            public DateTime                         TimeStarted;
            /// <summary>Game for which the command is being executed if any</summary>
            public GameIntf                         CurrentGameIntf { get; private set; }
            /// <summary>Last command error if any</summary>
            public string                           LastCmdError { get; private set; }
            /// <summary>List of active games</summary>
            private Dictionary<int,GameIntf>        m_dictGameIntf;
            /// <summary>List of games from the last 'games' command</summary>
            public List<FICSGame>                   GameList { get; private set; }
            /// <summary>Server date list</summary>
            public List<string>                     ServerDateList { get; private set; }
            /// <summary>List of variable settings</summary>
            public Dictionary<string,string>        VariableList { get; private set; }
            /// <summary>Signal use to indicate when a command finished executing</summary>
            public System.Threading.EventWaitHandle CmdSignal;
            /// <summary>User name</summary>
            public string                           UserName { get; set; }
            /// <summary>Password</summary>
            public string                           Password { get; set; }
            /// <summary>Text received in the login process</summary>
            public StringBuilder                    LoginText { get; set; }

            /// <summary>
            /// Ctor
            /// </summary>
            public AutomatonState() {
                CmdExecuting        = CmdExecutingE.PreLogin;
                Phase               = 0;
                SingleMoveListening = false;
                TimeStarted         = new DateTime(0);
                CurrentGameIntf     = null;
                m_dictGameIntf      = new Dictionary<int, GameIntf>(16);
                GameList            = new List<FICSGame>(512);
                ServerDateList      = new List<string>(6);
                VariableList        = new Dictionary<string, string>(128, StringComparer.OrdinalIgnoreCase);
                LastCmdError        = null;
                CmdSignal           = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset);
                LoginText           = new StringBuilder(2048);
            }

            /// <summary>
            /// Disposing the object
            /// </summary>
            public void Dispose() {
                if (CmdSignal != null) {
                    CmdSignal.Close();
                    CmdSignal = null;
                }
            }

            /// <summary>
            /// Add a game interface
            /// </summary>
            /// <param name="gameIntf"> Game interface</param>
            /// <returns>
            /// true if succeed, false if game is already defined
            /// </returns>
            public bool AddGameIntf(GameIntf gameIntf) {
                bool    bRetVal;
                int     iGameId;

                lock (m_dictGameIntf) {
                    iGameId = gameIntf.Game.GameId;
                    if (m_dictGameIntf.ContainsKey(iGameId)) {
                        bRetVal = false;
                    } else {
                        m_dictGameIntf.Add(iGameId, gameIntf);
                        bRetVal             = true;
                        SingleMoveListening = true;
                    }
                }
                return(bRetVal);
            }

            /// <summary>
            /// Remove a game interface
            /// </summary>
            /// <param name="iGameId">  Game interface id</param>
            /// <returns>
            /// true if succeed, false if game is not found
            /// </returns>
            private bool RemoveGameIntfInt(int iGameId) {
                bool    bRetVal;

                if (m_dictGameIntf.ContainsKey(iGameId)) {
                    m_dictGameIntf.Remove(iGameId);
                    SingleMoveListening = (m_dictGameIntf.Count != 0);
                    bRetVal             = true;
                } else {
                    bRetVal             = false;
                }
                return(bRetVal);
            }

            /// <summary>
            /// Remove a game interface
            /// </summary>
            /// <param name="iGameId">  Game id</param>
            /// <returns>
            /// true if succeed, false if game is not found
            /// </returns>
            public bool RemoveGameIntf(int iGameId) {
                bool    bRetVal;

                lock(m_dictGameIntf) {
                    bRetVal = RemoveGameIntfInt(iGameId);
                }
                return(bRetVal);
            }

            /// <summary>
            /// Terminate a game
            /// </summary>
            /// <param name="gameIntf">                 Game interface</param>
            /// <param name="eTermination">             Termination code</param>
            /// <param name="strTerminationComment">    Termination comment</param>
            /// <param name="strError">                 Error if any</param>
            public void TerminateGame(GameIntf gameIntf, TerminationE eTermination, string strTerminationComment, string strError) {
                lock(m_dictGameIntf) {
                    if (RemoveGameIntfInt(gameIntf.Game.GameId)) {
                        gameIntf.SetTermination(eTermination, strTerminationComment, strError);
                    }
                }
            }

            /// <summary>
            /// Find a game using its id
            /// </summary>
            /// <param name="iGameId">  Game id</param>
            /// <returns>
            /// Game or null if not found
            /// </returns>
            public GameIntf FindGameIntf(int iGameId) {
                GameIntf    gameIntf;

                lock(m_dictGameIntf) {
                    m_dictGameIntf.TryGetValue(iGameId, out gameIntf);
                }
                return(gameIntf);
            }

            /// <summary>
            /// Find a game using its attached chess board control
            /// </summary>
            /// <param name="chessBoardControl">  Chess board control</param>
            /// <returns>
            /// Game or null if not found
            /// </returns>
            public GameIntf FindGameIntf(SrcChess2.ChessBoardControl chessBoardControl) {
                GameIntf    gameIntfRetVal = null;

                lock(m_dictGameIntf) {
                    gameIntfRetVal = m_dictGameIntf.Values.FirstOrDefault(x => x.ChessBoardCtl == chessBoardControl);
                }
                return(gameIntfRetVal);
            }

            /// <summary>
            /// Gets the number of observed games
            /// </summary>
            /// <returns>
            /// Game count
            /// </returns>
            public int GameCount() {
                int iRetVal;

                lock(m_dictGameIntf) {
                    iRetVal = m_dictGameIntf.Count;
                }
                return(iRetVal);
            }

            /// <summary>
            /// Set the current command or reset it to none
            /// </summary>
            /// <param name="eCmd">     Command</param>
            /// <param name="gameIntf"> Associated game interface if any</param>
            public void SetCommand(CmdExecutingE eCmd, GameIntf gameIntf) {
                if (eCmd == CmdExecutingE.None) {
                    throw new ArgumentException("Use ResetCommand to set a command to none");
                }
                lock(this) {
                    if (CmdExecuting != CmdExecutingE.None && CmdExecuting != CmdExecutingE.PreLogin) {
                        throw new MethodAccessException("Cannot execute a command while another is executing");
                    }
                    CmdExecuting    = eCmd;
                    TimeStarted     = DateTime.Now;
                    CurrentGameIntf = gameIntf;
                    Phase           = 0;
                    LastCmdError    = null;
                    CmdSignal.Reset();
                }
            }

            /// <summary>
            /// Reset the current command to none
            /// </summary>
            /// <param name="strError"> Error message</param>
            public void ResetCommand(string strError) {
                lock(this) {
                    CmdExecuting    = CmdExecutingE.None;
                    TimeStarted     = new DateTime(0);
                    CurrentGameIntf = null;
                    LastCmdError    = strError;
                    Phase           = 0;
                    CmdSignal.Set();
                }
            }

            /// <summary>
            /// Reset the current command to none
            /// </summary>
            public void ResetCommand() {
                ResetCommand(null);
            }

        } // Class AutomatonState
        #endregion

        /// <summary>TELNET Connection with the server</summary>
        private TelnetConnection                            m_connection;
        /// <summary>State of the listening routine</summary>
        private AutomatonState                              m_state;
        /// <summary>Original parameter values</summary>
        private Dictionary<string,string>                   m_dictVariables;
        /// <summary>Set of setting which has been changed</summary>
        private HashSet<string>                             m_setChangedSettings;
        /// <summary>Window where to send some error message</summary>
        private SrcChess2.ChessBoardControl                 m_ctlMain;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="ctlMain">      Main chess board control</param>
        /// <param name="strHostname">  Host name</param>
        /// <param name="iPort">        Port number</param>
        /// <param name="bDebugTrace">  true to send trace to the debugging output</param>
        public FICSConnection(SrcChess2.ChessBoardControl ctlMain, string strHostname, int iPort, bool bDebugTrace) {
            m_state                         = new AutomatonState();
            m_dictVariables                 = new Dictionary<string, string>(512, StringComparer.OrdinalIgnoreCase);
            m_setChangedSettings            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            m_ctlMain                       = ctlMain;
            m_connection                    = new TelnetConnection(bDebugTrace);
            m_connection.NewTextReceived   += m_connection_NewTextReceived;
            m_connection.Connect(strHostname, iPort);
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="ctlMain">              Main chess board control</param>
        /// <param name="connectionSetting">    Connection setting</param>
        public FICSConnection(SrcChess2.ChessBoardControl ctlMain, FICSConnectionSetting connectionSetting) : this(ctlMain, connectionSetting.HostName, connectionSetting.HostPort, false /*bDebugTrace*/) {
            ConnectionSetting = connectionSetting;
        }

        /// <summary>
        /// Debugging trace
        /// </summary>
        public bool DebugTrace {
            get {
                bool    bRetVal;

                bRetVal = (m_connection != null) ? m_connection.DebugTrace : false;
                return(bRetVal);
            }
            set {
                if (m_connection != null) {
                    m_connection.DebugTrace = value;
                }
            }
        }

        /// <summary>
        /// Disposing the object
        /// </summary>
        /// <param name="bDisposing">   true for dispose, false for finallizing</param>
        protected virtual void Dispose(bool bDisposing) {
            if (m_connection != null) {
                try {
                    RestoreOldSetting();
                    m_connection.SendLine("quit");
                } catch(Exception) {
                }
                m_connection.Dispose();
                m_connection = null;
            }
            if (m_state != null) {
                m_state.Dispose();
                m_state = null;
            }
        }

        /// <summary>
        /// Dispose the connection to the FICS server
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Connection setting
        /// </summary>
        public FICSConnectionSetting ConnectionSetting {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of games which are observed
        /// </summary>
        /// <returns>
        /// Observed games count
        /// </returns>
        public int GetObservedGameCount() {
            return(m_state.GameCount());
        }

        /// <summary>
        /// Original settings
        /// </summary>
        public Dictionary<string,string> OriginalSettings {
            get {
                return(m_dictVariables);
            }
        }

        /// <summary>
        /// Change a server setting
        /// </summary>
        /// <param name="strSetting">   Name of the setting</param>
        /// <param name="strValue">     Value of the setting</param>
        /// <param name="bAddToSet">    true to add to the list of change setting</param>
        private void SetSetting(string strSetting, string strValue, bool bAddToSet) {
            if (!bAddToSet || m_dictVariables.ContainsKey(strSetting)) {
                m_connection.SendLine(String.Format("set {0} {1}", strSetting, strValue));
                if (bAddToSet &&
                    !m_setChangedSettings.Contains(strSetting) &&
                    m_dictVariables[strSetting] != strValue) {
                    m_setChangedSettings.Add(strSetting);
                }
            } else {
#if DEBUG
                throw new ArgumentException("Oops.. setting not found");
#endif
            }
        }

        /// <summary>
        /// Change a server setting (iVariable)
        /// </summary>
        /// <param name="strSetting">   Name of the setting</param>
        /// <param name="strValue">     Value of the setting</param>
        private void SetISetting(string strSetting, string strValue) {
            m_connection.SendLine(String.Format("iset {0} {1}", strSetting, strValue));
        }

        /// <summary>
        /// Restore the old settings
        /// </summary>
        public void RestoreOldSetting() {
            string  strOldValue;

            m_connection.FlushInput();
            foreach (string strSetting in m_setChangedSettings) {
                strOldValue = m_dictVariables[strSetting];
                SetSetting(strSetting, strOldValue, false /*bAddToSet*/);
            }
                m_connection.FlushInput();
        }

        /// <summary>
        /// Set a quiet mode
        /// </summary>
        private void SetQuietModeInt() {
            m_connection.FlushInput();
            SetISetting("defprompt", "1");      // Force using standard prompt
            //SetISetting("gameinfo", "1");     // Add game info when starting the observe
            SetISetting("ms", "1");             // Player's time contains millisecond
            SetISetting("startpos", "1");       // Add a board setting at the beginning of a move list if not a standard starting position
            //SetISetting("pendinfo", "1");     // Add information on pending offer 
            SetSetting("interface", "SrcChess", false);
            SetSetting("ptime", "0", true);
            SetISetting("lock", "1");           // No more internal setting before logging out
            SetSetting("shout",         "0", true /*bAddToSet*/);
            SetSetting("cshout",        "0", true /*bAddToSet*/);
            SetSetting("kibitz",        "0", true /*bAddToSet*/);
            SetSetting("pin",           "0", true /*bAddToSet*/);
            SetSetting("tell",          "0", true /*bAddToSet*/);
            SetSetting("ctell",         "0", true /*bAddToSet*/);
            SetSetting("gin",           "0", true /*bAddToSet*/);
            SetSetting("seek",          "0", true /*bAddToSet*/);
            SetSetting("showownseek",   "0", true /*bAddToSet*/);
            m_connection.FlushInput();
        }

        /// <summary>
        /// Set a quiet mode
        /// </summary>
        public void SetQuietMode() {
            m_connection.FlushInput();
            SetQuietModeInt();
            SetSetting("Style", "12", true /*bAddToSet*/);
            m_connection.FlushInput();
        }

        /// <summary>
        /// Login to the session
        /// </summary>
        /// <param name="strPassword">      User password</param>
        /// <param name="iTimeOut">         Timeout in seconds</param>
        /// <param name="strError">         Returned error if any</param>
        /// <returns>
        /// true if succeed, false if failed (bad password)
        /// </returns>
        public bool Login(string        strPassword,
                          int           iTimeOut,
                          out string    strError) {
            bool        bRetVal;

            strError            = null;
            m_state.UserName    = ConnectionSetting.UserName;
            m_state.Password    = strPassword;
            m_state.SetCommand(CmdExecutingE.Login, null /*gameIntf*/);
            if (m_state.CmdSignal.WaitOne(iTimeOut * 1000)) {
                if (m_state.LastCmdError == null) {
                    m_connection.NewLineReceived += m_connection_NewLineReceived;
                    if (GetVariableList(iTimeOut * 1000) > 20) { // At least 20 variables are defined
                        SetQuietMode();
                        bRetVal = true;
                    } else {
                        strError = "Unable to access user variables";
                        bRetVal  = false;
                    }
                } else {
                    strError    = m_state.LastCmdError;
                    m_state.ResetCommand();
                    bRetVal     = false;
                }
                if (!bRetVal) {
                    strError = "Error login to the chess server - " + strError;
                }
            } else {
                bRetVal             = false;
                strError            = "Login timeout";
                m_state.UserName    = null;
                m_state.Password    = null;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Start observing a game using a predefined game interface
        /// </summary>
        /// <param name="gameIntf">             Game to observe</param>
        /// <param name="iTimeOut">             Command timeout in second</param>
        /// <param name="strError">             Error if any</param>
        /// <returns>
        /// true if succeed, false if game is already defined
        /// </returns>
        public bool ObserveGame(GameIntf gameIntf, int iTimeOut, out string strError) {
            bool    bRetVal;
            int     iGameId;

            if (gameIntf.Game.IsPrivate) {
                throw new ArgumentException("Cannot listen to private game");
            }
            bRetVal  = m_state.AddGameIntf(gameIntf);
            if (bRetVal) {
                iGameId = gameIntf.Game.GameId;
                m_state.SetCommand(CmdExecutingE.MoveList, gameIntf);
                m_connection.SendLine("observe " + iGameId.ToString());
                m_connection.SendLine("moves " + iGameId.ToString());
                if (m_state.CmdSignal.WaitOne(iTimeOut * 1000)) {
                    strError    = m_state.LastCmdError;
                    bRetVal     = (strError == null);
                } else {
                    m_state.ResetCommand();
                    strError = "Timeout";
                }
            } else {
                strError = "Already defined";
            }
            return(bRetVal);
        }

        /// <summary>
        /// Gets the timeout action
        /// </summary>
        /// <returns>
        /// Timeout action
        /// </returns>
        public Action<GameIntf> GetTimeOutAction() {
            return((x) => { m_state.TerminateGame(x, TerminationE.TerminatedWithErr, "", "Move timeout"); });
        }

        /// <summary>
        /// Start to observe a game
        /// </summary>
        /// <param name="game">                 Game to observe</param>
        /// <param name="chessBoardControl">    Chess board control to associate with the game</param>
        /// <param name="iTimeOut">             Command timeout in second</param>
        /// <param name="iMoveTimeOut">         Command timeout in second</param>
        /// <param name="actionGameFinished">   Action to call when game is finished or null if none</param>
        /// <param name="strError">             Error if any</param>
        /// <returns>
        /// true if succeed, false if game is already defined
        /// </returns>
        public bool ObserveGame(FICSGame game, SrcChess2.ChessBoardControl chessBoardControl, int iTimeOut, int? iMoveTimeOut, Action<GameIntf,TerminationE,string> actionGameFinished, out string strError) {
            bool                bRetVal;
            GameIntf            gameIntf;
            Action<GameIntf>    actionMoveTimeOut;

            if (chessBoardControl == null) {
                throw new ArgumentNullException();
            }
            if (iMoveTimeOut == 0) {
                actionMoveTimeOut   = null;
            } else {
                actionMoveTimeOut   = GetTimeOutAction();
            }
            gameIntf = new GameIntf(game, chessBoardControl, iMoveTimeOut, actionMoveTimeOut, actionGameFinished);
#if DEBUG
            iTimeOut    = 3600;
#endif
            bRetVal  = ObserveGame(gameIntf, iTimeOut, out strError);
            return(bRetVal);
        }

        /// <summary>
        /// Terminate the game observation for the specified chess board control
        /// </summary>
        /// <param name="chessBoardControl">    Chess board control</param>
        /// <returns>
        /// true if found, false if not
        /// </returns>
        public bool TerminateObservation(SrcChess2.ChessBoardControl chessBoardControl) {
            bool        bRetVal;
            GameIntf    gameIntf;

            gameIntf    = m_state.FindGameIntf(chessBoardControl);
            if (gameIntf == null) {
                bRetVal = false;
            } else {
                bRetVal = true;
                m_state.TerminateGame(gameIntf, TerminationE.TerminatedWithErr, "", "Stop by user");
            }
            return(bRetVal);
        }

        /// <summary>
        /// Find the list of games
        /// </summary>
        /// <param name="bRefresh">     True to refresh the list</param>
        /// <param name="iTimeOut">     Command timeout in second</param>
        /// <returns>
        /// List of game
        /// </returns>
        public List<FICSGame> GetGameList(bool bRefresh, int iTimeOut) {
            List<FICSGame>  listRetVal;

#if DEBUG
            iTimeOut    = 3600;
#endif
            listRetVal  = m_state.GameList;
            if (bRefresh) {
                m_state.GameList.Clear();
                m_state.SetCommand(CmdExecutingE.GameList, null /*gameIntf*/);
                m_connection.SendLine("games");
                if (!m_state.CmdSignal.WaitOne(iTimeOut * 1000)) {
                    m_state.ResetCommand();
                }
            }
            return(listRetVal);
        }

        /// <summary>
        /// Gets the variable setting
        /// </summary>
        /// <param name="iTimeOut">     Command timeout in second</param>
        /// <returns>Setting count</returns>
        public int GetVariableList(int iTimeOut) {
            m_dictVariables.Clear();
            m_state.SetCommand(CmdExecutingE.VariableList, null /*gameIntf*/);
            m_connection.SendLine("variables");
            if (m_state.CmdSignal.WaitOne(iTimeOut * 1000)) {
                m_dictVariables = new Dictionary<string, string>(m_state.VariableList, StringComparer.OrdinalIgnoreCase);
            } else {
                m_state.ResetCommand();
            }
            return(m_dictVariables.Count);
        }

        /// <summary>
        /// Gets the date from the server
        /// </summary>
        /// <returns>
        /// List of date or null if timeout
        /// </returns>
        public List<string> GetServerDate(int iTimeOut) {
            List<string>    listRetVal;

            m_state.SetCommand(CmdExecutingE.Date, null /*gameIntf*/);
            m_connection.SendLine("date");
            if (m_state.CmdSignal.WaitOne(iTimeOut * 1000)) {
                listRetVal = m_state.ServerDateList;
            } else {
                listRetVal = null;
                m_state.ResetCommand();
            }
            return(listRetVal);
        }

        /// <summary>
        /// Process the line if it's a MoveList header
        /// </summary>
        /// <param name="strLine">  Line</param>
        /// <returns>
        /// true if it's a move list header, false if not
        /// </returns>
        private void ProcessMoveListHeader(string strLine) {
            string  strMoveListStartingWith = "Movelist for game ";

            strMoveListStartingWith = "Movelist for game " + m_state.CurrentGameIntf.Game.GameId.ToString();
            if (strLine.StartsWith(strMoveListStartingWith)) {
                m_state.Phase++;
            }
        }

        /// <summary>
        /// Skip the move list header
        /// </summary>
        /// <param name="strLine">  Line</param>
        /// <returns>
        /// true if found the last line of the header, false if not
        /// </returns>
        private void SkipMoveListHeader(string strLine) {
            if (strLine.StartsWith("---- ")) {
                m_state.Phase++;
            }
        }

        

       
        

        /// <summary>
        /// Process first game list line
        /// </summary>
        /// <param name="strLine">  Received line</param>
        private void ProcessFirstGameListLine(string strLine) {
            FICSGame    game;
            bool        bSupported;

            if (!String.IsNullOrWhiteSpace(strLine)) {
                if (FICSGame.IsLastGameLine(strLine)) {
                    m_state.ResetCommand();
                } else { 
                    game = FICSGame.ParseGameLine(strLine, out bSupported);
                    if (game != null) {
                        if (bSupported) {
                            m_state.GameList.Add(game);
                        }
                        m_state.Phase++;
                    }
                }
            }
        }

        /// <summary>
        /// Process first game list line
        /// </summary>
        /// <param name="strLine">  Received line</param>
        private void ProcessGameListLine(string strLine) {
            FICSGame    game;
            bool        bSupported;

            game = FICSGame.ParseGameLine(strLine, out bSupported);
            if (game != null) {
                if (bSupported) {
                    m_state.GameList.Add(game);
                }
            } else {
                m_state.ResetCommand();
            }
        }

        /// <summary>
        /// Identifies the variable part
        /// </summary>
        /// <param name="strLine">  Line</param>
        private void ProcessVariableListHeader(string strLine) {
            const string strStartingWith = "Variable settings of ";

            if (strLine.Contains(strStartingWith)) {
                m_state.VariableList.Clear();
                m_state.Phase++;
            }
        }

        /// <summary>
        /// Process variable list lines
        /// </summary>
        /// <param name="strLine">  Line</param>
        private void GettingVariableListValue(string strLine) {
            string[]    arrVars;
            string[]    arrSetting;
            string      strSettingName;
            string      strSettingValue;

            strLine = strLine.Trim();
            if (!String.IsNullOrEmpty(strLine)) {
                if (strLine.StartsWith("Formula:")) {
                    m_state.ResetCommand();
                } else if (strLine.Contains("=")) {
                    arrVars = strLine.Split(' ');
                    foreach (string strVar in arrVars) {
                        if (!String.IsNullOrEmpty(strVar) && strVar.Contains("=")) {
                            arrSetting = strVar.Split('=');
                            if (arrSetting.Length == 2) {
                                strSettingName  = arrSetting[0];
                                strSettingValue = arrSetting[1];
                                if (!m_state.VariableList.ContainsKey(strSettingName)) {
                                    m_state.VariableList.Add(strSettingName, strSettingValue);
                                }
                            }
                        }
                    }
                } else {    // Guest doesn't receive the Formula setting
                    m_state.ResetCommand();
                }
            }
        }

        /// <summary>
        /// Process an input line. Use to process command
        /// </summary>
        /// <param name="strLine">  Received line</param>
        private void ProcessLine(string strLine) {
            CmdExecutingE   eCmdExecuting;
            int             iPhase;
            bool            bSingleMoveListening;

            eCmdExecuting           = m_state.CmdExecuting;
            bSingleMoveListening    = m_state.SingleMoveListening;
            iPhase                  = m_state.Phase;
            if (!bSingleMoveListening ||
                iPhase != 0           
                ) {
                switch(m_state.CmdExecuting) {
                case CmdExecutingE.None:
                    break;
                case CmdExecutingE.MoveList:
                    switch(iPhase) {
                    case 0:
                        ProcessMoveListHeader(strLine);
                        break;
                    case 1:
                        SkipMoveListHeader(strLine);
                        break;
                   
                    default:
                        throw new NotImplementedException();
                    }
                    break;
                case CmdExecutingE.GameList:
                    switch(iPhase) {
                    case 0:
                        ProcessFirstGameListLine(strLine);
                        break;
                    case 1:
                        ProcessGameListLine(strLine);
                        break;
                    default:
                        throw new NotImplementedException();
                    }
                    break;
                case CmdExecutingE.VariableList:
                    switch(iPhase) {
                    case 0:
                        ProcessVariableListHeader(strLine);
                        break;
                    case 1:
                        GettingVariableListValue(strLine);
                        break;
                    default:
                        throw new NotImplementedException();
                    }
                    break;
                case CmdExecutingE.Date:
                    switch(iPhase) {
                    case 0:
                        if (strLine.StartsWith("Local time ") || strLine.StartsWith("fics% Local time")) {
                            m_state.ServerDateList.Clear();
                            m_state.ServerDateList.Add(strLine);
                            m_state.Phase++;
                        }
                        break;
                    case 1:
                        if (strLine.StartsWith("Server time ")) {
                            m_state.ServerDateList.Add(strLine);
                        } else if (strLine.StartsWith("GMT ")) {
                            m_state.ServerDateList.Add(strLine);
                            m_state.ResetCommand();
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new NotImplementedException();
                }
            }
        }


        /// <summary>
        /// Process input text. Use to process Login
        /// </summary>
        private void ProcessLoginText() {
            string  strText;

            if (m_state.CmdExecuting == CmdExecutingE.Login) {
                strText = m_state.LoginText.ToString();
                switch(m_state.Phase) {
                case 0:     // Set the user name
                    if (strText.EndsWith("login: ")) {
                        m_connection.SendLine(m_state.UserName);
                        m_state.Phase++;
                        m_state.LoginText.Clear();
                    }
                    break;
                case 1:     // Set the password
                    if (String.Compare(m_state.UserName, "Guest", true) == 0) {
                        m_connection.SendLine("");  // Accept guest
                        m_state.ResetCommand();
                    } else {
                        if (strText.Contains("Sorry, names can only")) {
                            m_state.ResetCommand("Invalid character in login name");
                        } else if (strText.Contains("is not a registred name.")) {
                            m_state.ResetCommand("Unknown login name");
                        } else if (!strText.EndsWith("password: ")) {
                            m_state.ResetCommand("Unknown error at login");
                        } else {
                            m_connection.SendLine(m_state.Password);
                            m_state.Phase++;
                        }
                    }
                    m_state.Password = null;
                    m_state.LoginText.Clear();
                    break;
                case 2:
                    if (strText.Contains("**** Starting FICS session as")) {
                        m_state.ResetCommand();
                    } else if (strText.Contains("**** Invalid password! ****")) {
                        m_state.ResetCommand("Invalid password");
                    } else {
                        m_state.ResetCommand("Unknown error with password");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Called when a new line has been received
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void m_connection_NewLineReceived(object sender, EventArgs e) {
            string  strLine;

            do {
                strLine = m_connection.GetNextReadLine();
                if (!String.IsNullOrEmpty(strLine)) {
                    ProcessLine(strLine);
                }
            } while (!String.IsNullOrEmpty(strLine));
        }


        /// <summary>
        /// Called when new text has been received
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void m_connection_NewTextReceived(object sender, EventArgs e) {
            string  strText;

            switch(m_state.CmdExecuting) {
            case CmdExecutingE.PreLogin:
            case CmdExecutingE.Login:
                do {
                    strText = m_connection.GetAllReadText();
                    if (!String.IsNullOrEmpty(strText)) {
                        m_state.LoginText.Append(strText);
                    }
                } while (!String.IsNullOrEmpty(strText));
                ProcessLoginText();
                break;
            }
        }
    }
}
