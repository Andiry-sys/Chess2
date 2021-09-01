using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SrcChess2 {
    
    public partial class MainWindow : Window {

        #region Types
        /// <summary>Getting computer against computer playing statistic</summary>
        private class BoardEvaluationStat {
            public BoardEvaluationStat(int iGameCount) {
                m_timeSpanMethod1   = TimeSpan.Zero;
                m_timeSpanMethod2   = TimeSpan.Zero;
                m_eResult           = ChessBoard.GameResultE.OnGoing;
                m_iMethod1MoveCount = 0;
                m_iMethod2MoveCount = 0;
                m_iMethod1WinCount  = 0;
                m_iMethod2WinCount  = 0;
                m_bUserCancel       = false;
                m_iGameIndex        = 0;
                m_iGameCount        = iGameCount;
            }
            public  TimeSpan                m_timeSpanMethod1;
            public  TimeSpan                m_timeSpanMethod2;
            public  ChessBoard.GameResultE  m_eResult;
            public  int                     m_iMethod1MoveCount;
            public  int                     m_iMethod2MoveCount;
            public  int                     m_iMethod1WinCount;
            public  int                     m_iMethod2WinCount;
            public  bool                    m_bUserCancel;
            public  int                     m_iGameIndex;
            public  int                     m_iGameCount;
            public  SearchMode              m_searchModeOri;
            public  MessageModeE            m_eMessageModeOri;
        };
        
        /// <summary>Use for computer move</summary>
        public enum MessageModeE {
            /// <summary>No message</summary>
            Silent      = 0,
            /// <summary>Only messages for move which are terminating the game</summary>
            CallEndGame = 1,
            /// <summary>All messages</summary>
            Verbose     = 2
        };
        
        /// <summary>Current playing mode</summary>
        public enum PlayingModeE {
            /// <summary>Player plays against another player</summary>
            PlayerAgainstPlayer,
            /// <summary>Computer play the white against a human black</summary>
            ComputerPlayWhite,
            /// <summary>Computer play the black against a human white</summary>
            ComputerPlayBlack,
            /// <summary>Computer play against computer</summary>
            ComputerPlayBoth,
            /// <summary>Design mode.</summary>
            DesignMode,
            /// <summary>Test evaluation methods. Computer play against itself in loop using two different evaluation methods</summary>
            TestEvaluationMethod
        };
        #endregion

        #region Command
        /// <summary>Command: New Game</summary>
        public static readonly RoutedUICommand              NewGameCommand              = new RoutedUICommand("_New Game...",                   "NewGame",              typeof(MainWindow));
        /// <summary>Command: Load Game</summary>
        public static readonly RoutedUICommand              LoadGameCommand             = new RoutedUICommand("_Load Game...",                  "LoadGame",             typeof(MainWindow));
        /// <summary>Command: Load Game</summary>
        public static readonly RoutedUICommand              LoadPuzzleCommand           = new RoutedUICommand("Load a Chess _Puzzle...",        "LoadPuzzle",           typeof(MainWindow));
        /// <summary>Command: Create Game</summary>
        public static readonly RoutedUICommand              CreateGameCommand           = new RoutedUICommand("_Create Game from PGN...",       "CreateGame",           typeof(MainWindow));
        /// <summary>Command: Save Game</summary>
        public static readonly RoutedUICommand              SaveGameCommand             = new RoutedUICommand("_Save Game...",                  "SaveGame",             typeof(MainWindow));
        /// <summary>Command: Save Game in PGN</summary>
        public static readonly RoutedUICommand              SaveGameInPGNCommand        = new RoutedUICommand("Save Game _To PGN...",           "SaveGameToPGN",        typeof(MainWindow));
        /// <summary>Command: Save Game in PGN</summary>
        public static readonly RoutedUICommand              CreateSnapshotCommand       = new RoutedUICommand("Create a _Debugging Snapshot...","CreateSnapshot",       typeof(MainWindow));
        /// <summary>Command: Connect to FICS Server</summary>
        public static readonly RoutedUICommand              ConnectToFICSCommand        = new RoutedUICommand("Connect to _FICS Server...",     "ConnectToFICS",        typeof(MainWindow));
        /// <summary>Command: Connect to FICS Server</summary>
        public static readonly RoutedUICommand              DisconnectFromFICSCommand   = new RoutedUICommand("_Disconnect from FICS Server",  "DisconnectFromFICS",   typeof(MainWindow));
        /// <summary>Command: Connect to FICS Server</summary>
        public static readonly RoutedUICommand              ObserveFICSGameCommand      = new RoutedUICommand("_Observe a FICS Game...",        "ObserveFICSGame",      typeof(MainWindow));
        /// <summary>Command: Quit</summary>
        public static readonly RoutedUICommand              QuitCommand                 = new RoutedUICommand("_Quit",                          "Quit",                 typeof(MainWindow));

        /// <summary>Command: Hint</summary>
        public static readonly RoutedUICommand              HintCommand                 = new RoutedUICommand("_Hint",                          "Hint",                 typeof(MainWindow));
        /// <summary>Command: Undo</summary>
        public static readonly RoutedUICommand              UndoCommand                 = new RoutedUICommand("_Undo",                          "Undo",                 typeof(MainWindow));
        /// <summary>Command: Redo</summary>
        public static readonly RoutedUICommand              RedoCommand                 = new RoutedUICommand("_Redo",                          "Redo",                 typeof(MainWindow));
        /// <summary>Command: Refresh</summary>
        public static readonly RoutedUICommand              RefreshCommand              = new RoutedUICommand("Re_fresh",                       "Refresh",              typeof(MainWindow));
        /// <summary>Command: Select Players</summary>
        public static readonly RoutedUICommand              SelectPlayersCommand        = new RoutedUICommand("_Select Players...",             "SelectPlayers",        typeof(MainWindow));
        /// <summary>Command: Automatic Play</summary>
        public static readonly RoutedUICommand              AutomaticPlayCommand        = new RoutedUICommand("_Automatic Play",                "AutomaticPlay",        typeof(MainWindow));
        /// <summary>Command: Fast Automatic Play</summary>
        public static readonly RoutedUICommand              FastAutomaticPlayCommand    = new RoutedUICommand("_Fast Automatic Play",           "FastAutomaticPlay",    typeof(MainWindow));
        /// <summary>Command: Cancel Play</summary>
        public static readonly RoutedUICommand              CancelPlayCommand           = new RoutedUICommand("_Cancel Play",                   "CancelPlay",           typeof(MainWindow));
        /// <summary>Command: Design Mode</summary>
        public static readonly RoutedUICommand              DesignModeCommand           = new RoutedUICommand("_Design Mode",                   "DesignMode",           typeof(MainWindow));

        /// <summary>Command: Search Mode</summary>
        public static readonly RoutedUICommand              SearchModeCommand           = new RoutedUICommand("_Search Mode...",                "SearchMode",           typeof(MainWindow));
        /// <summary>Command: Flash Piece</summary>
        public static readonly RoutedUICommand              FlashPieceCommand           = new RoutedUICommand("_Flash Piece",                   "FlashPiece",           typeof(MainWindow));
        /// <summary>Command: PGN Notation</summary>
        public static readonly RoutedUICommand              PGNNotationCommand          = new RoutedUICommand("_PGN Notation",                  "PGNNotation",          typeof(MainWindow));
        /// <summary>Command: Board Settings</summary>
        public static readonly RoutedUICommand              BoardSettingCommand         = new RoutedUICommand("_Board Settings...",             "BoardSettings",         typeof(MainWindow));
        
        /// <summary>Command: Create a Book</summary>
        public static readonly RoutedUICommand              CreateBookCommand           = new RoutedUICommand("_Create a Book...",              "CreateBook",           typeof(MainWindow));
        /// <summary>Command: Filter a PGN File</summary>
        public static readonly RoutedUICommand              FilterPGNFileCommand        = new RoutedUICommand("_Filter a PGN File...",          "FilterPGNFile",        typeof(MainWindow));
        /// <summary>Command: Test Board Evaluation</summary>
        public static readonly RoutedUICommand              TestBoardEvaluationCommand  = new RoutedUICommand("_Test Board Evaluation...",      "TestBoardEvaluation",  typeof(MainWindow));

        /// <summary>Command: Test Board Evaluation</summary>
        

        /// <summary>List of all supported commands</summary>
        private static readonly RoutedUICommand[]           m_arrCommands = new RoutedUICommand[] { NewGameCommand,
                                                                                                    LoadGameCommand,
                                                                                                    LoadPuzzleCommand,
                                                                                                    CreateGameCommand,
                                                                                                    SaveGameCommand,
                                                                                                    SaveGameInPGNCommand,
                                                                                                    CreateSnapshotCommand,
                                                                                                    ConnectToFICSCommand,
                                                                                                    DisconnectFromFICSCommand,
                                                                                                    ObserveFICSGameCommand,
                                                                                                    QuitCommand,
                                                                                                    HintCommand,
                                                                                                    UndoCommand,
                                                                                                    RedoCommand,
                                                                                                    RefreshCommand,
                                                                                                    SelectPlayersCommand,
                                                                                                    AutomaticPlayCommand,
                                                                                                    FastAutomaticPlayCommand,
                                                                                                    CancelPlayCommand,
                                                                                                    DesignModeCommand,
                                                                                                    SearchModeCommand,
                                                                                                    FlashPieceCommand,
                                                                                                    PGNNotationCommand,
                                                                                                    BoardSettingCommand,
                                                                                                    CreateBookCommand,
                                                                                                    FilterPGNFileCommand,
                                                                                                    TestBoardEvaluationCommand,
                                                                                                     };
        #endregion

        #region Members        
       
        /// <summary>Color played by the computer</summary>
        public ChessBoard.PlayerE                           m_eComputerPlayingColor;
        /// <summary>Utility class to handle board evaluation objects</summary>
        private BoardEvaluationUtil                         m_boardEvalUtil;
        /// <summary>List of piece sets</summary>
        private SortedList<string,PieceSet>                 m_listPieceSet;
        /// <summary>Currently selected piece set</summary>
        private PieceSet                                    m_pieceSet;
        /// <summary>Color use to create the background brush</summary>
        internal Color                                      m_colorBackground;
        /// <summary>Dispatcher timer</summary>
        private DispatcherTimer                             m_dispatcherTimer;
        /// <summary>Current message mode</summary>
        private MessageModeE                                m_eMessageMode;
        /// <summary>Search mode</summary>
        private SettingSearchMode                           m_settingSearchMode;
        /// <summary>Connection to FICS Chess Server</summary>
        private FICSInterface.FICSConnection                m_ficsConnection;
        /// <summary>Setting to connect to the FICS server</summary>
        private FICSInterface.FICSConnectionSetting         m_ficsConnectionSetting;
        /// <summary>Convert properties settings to/from object setting</summary>
        private SettingAdaptor                              m_settingAdaptor;
        /// <summary>Search criteria to use to find FICS game</summary>
        private FICSInterface.SearchCriteria                m_searchCriteria;
        
       
        /// <summary>Mask of puzzle which has been solved</summary>
        internal long[]                                     m_arrPuzzleMask;
        #endregion

        #region Ctor
        /// <summary>
        /// Static Ctor
        /// </summary>
        static MainWindow() {
            NewGameCommand.InputGestures.Add(               new KeyGesture(Key.N,           ModifierKeys.Control));
            LoadGameCommand.InputGestures.Add(              new KeyGesture(Key.O,           ModifierKeys.Control));
            SaveGameCommand.InputGestures.Add(              new KeyGesture(Key.S,           ModifierKeys.Control));
            ConnectToFICSCommand.InputGestures.Add(         new KeyGesture(Key.C,           ModifierKeys.Shift | ModifierKeys.Control));
            ObserveFICSGameCommand.InputGestures.Add(       new KeyGesture(Key.O,           ModifierKeys.Shift | ModifierKeys.Control));
            DisconnectFromFICSCommand.InputGestures.Add(    new KeyGesture(Key.D,           ModifierKeys.Shift | ModifierKeys.Control));
            QuitCommand.InputGestures.Add(                  new KeyGesture(Key.F4,          ModifierKeys.Alt));
            HintCommand.InputGestures.Add(                  new KeyGesture(Key.H,           ModifierKeys.Control));
            UndoCommand.InputGestures.Add(                  new KeyGesture(Key.Z,           ModifierKeys.Control));
            RedoCommand.InputGestures.Add(                  new KeyGesture(Key.Y,           ModifierKeys.Control));
            RefreshCommand.InputGestures.Add(               new KeyGesture(Key.F5));
            SelectPlayersCommand.InputGestures.Add(         new KeyGesture(Key.P,           ModifierKeys.Control));
            AutomaticPlayCommand.InputGestures.Add(         new KeyGesture(Key.F2,          ModifierKeys.Control));
            FastAutomaticPlayCommand.InputGestures.Add(     new KeyGesture(Key.F3,          ModifierKeys.Control));
            CancelPlayCommand.InputGestures.Add(            new KeyGesture(Key.C,           ModifierKeys.Control));
            DesignModeCommand.InputGestures.Add(            new KeyGesture(Key.D,           ModifierKeys.Control));
            SearchModeCommand.InputGestures.Add(            new KeyGesture(Key.M,           ModifierKeys.Control));
            
        }

        /// <summary>
        /// Class Ctor
        /// </summary>
        public MainWindow() {
           
            CanExecuteRoutedEventHandler    onCanExecuteCmd;

            InitializeComponent();
            m_settingAdaptor                    = new SettingAdaptor(Properties.Settings.Default);
            m_listPieceSet                      = PieceSetStandard.LoadPieceSetFromResource();
          
            m_eMessageMode                      = MessageModeE.CallEndGame;
            m_lostPieceBlack.ChessBoardControl  = m_chessCtl;
            m_lostPieceBlack.Color              = true;
            m_lostPieceWhite.ChessBoardControl  = m_chessCtl;
            m_lostPieceWhite.Color              = false;
            m_ficsConnectionSetting             = new FICSInterface.FICSConnectionSetting();
            m_boardEvalUtil                     = new BoardEvaluationUtil();
            m_settingSearchMode                 = new SettingSearchMode();
            m_searchCriteria                    = new FICSInterface.SearchCriteria();
            m_arrPuzzleMask                     = new long[2];
           
            m_settingAdaptor.LoadChessBoardCtl(m_chessCtl);
            m_settingAdaptor.LoadMainWindow(this, m_listPieceSet);
            m_settingAdaptor.LoadFICSConnectionSetting(m_ficsConnectionSetting);
           
            m_settingAdaptor.LoadFICSSearchCriteria(m_searchCriteria);
            m_chessCtl.SearchMode               = m_settingSearchMode.GetSearchMode();
            m_chessCtl.UpdateCmdState          += m_chessCtl_UpdateCmdState;
           
           
            m_chessCtl.MoveSelected            += m_chessCtl_MoveSelected;
            m_chessCtl.NewMove                 += m_chessCtl_NewMove;
            m_chessCtl.QueryPiece              += m_chessCtl_QueryPiece;
            m_chessCtl.QueryPawnPromotionType  += m_chessCtl_QueryPawnPromotionType;
            
           
            m_dispatcherTimer                   = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, new EventHandler(dispatcherTimer_Tick), Dispatcher);
            m_dispatcherTimer.Start();
            SetCmdState();
            
           
            m_ficsConnection                    = null;
            
            onCanExecuteCmd                     = new CanExecuteRoutedEventHandler(OnCanExecuteCmd);
            
            Closed                             += MainWindow_Closed;
            
        }

        

        /// <summary>
        /// Called when the main window has been closed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void MainWindow_Closed(object sender, EventArgs e) {
            m_settingAdaptor.SaveChessBoardCtl(m_chessCtl);
            m_settingAdaptor.SaveMainWindow(this);
            m_settingAdaptor.SaveFICSConnectionSetting(m_ficsConnectionSetting);
            m_settingAdaptor.SaveSearchMode(m_settingSearchMode);
           
            m_settingAdaptor.SaveFICSSearchCriteria(m_searchCriteria);
            m_settingAdaptor.Settings.Save();
            if (m_ficsConnection != null) {
                m_ficsConnection.Dispose();
                m_ficsConnection = null;
            }
        }

        #endregion

        #region Command Handling
        

        /// <summary>
        /// Determine if a command can be executed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Routed event argument</param>
        public virtual void OnCanExecuteCmd(object sender, CanExecuteRoutedEventArgs e) {
          
            bool    bIsBusy;
            bool    bIsSearchEngineBusy;
            bool    bIsObservingGame;

           
            bIsBusy             = m_chessCtl.IsBusy;
            bIsSearchEngineBusy = m_chessCtl.IsSearchEngineBusy;
            bIsObservingGame    = m_chessCtl.IsObservingAGame;
            if (e.Command == NewGameCommand                     ||
                e.Command == CreateGameCommand                  ||
                e.Command == LoadGameCommand                    ||
                e.Command == LoadPuzzleCommand                  ||
                e.Command == SaveGameCommand                    ||
                e.Command == SaveGameInPGNCommand               ||
                e.Command == CreateSnapshotCommand              ||
                e.Command == HintCommand                        ||
                e.Command == RefreshCommand                     ||
                e.Command == SelectPlayersCommand               ||
                e.Command == AutomaticPlayCommand               ||
                e.Command == FastAutomaticPlayCommand           ||
                e.Command == CreateBookCommand                  ||
                e.Command == FilterPGNFileCommand) {
              
            } else if (e.Command == QuitCommand                 ||
                       e.Command == SearchModeCommand           ||
                       e.Command == FlashPieceCommand           ||
                       e.Command == DesignModeCommand           ||
                       e.Command == PGNNotationCommand          ||
                       e.Command == BoardSettingCommand         
                      
                       ) {
                e.CanExecute    = !(bIsSearchEngineBusy || bIsBusy || bIsObservingGame);
            }    else if (e.Command == ObserveFICSGameCommand) {
                e.CanExecute    = (m_ficsConnection != null && !bIsObservingGame);
            } else {
                e.Handled   = false;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Used piece set
        /// </summary>
        public PieceSet PieceSet {
            get {
                return(m_pieceSet);
            }
            set {
                if (m_pieceSet != value) {
                    m_pieceSet                  = value;
                    m_chessCtl.PieceSet         = value;
                    m_lostPieceBlack.PieceSet   = value;
                    m_lostPieceWhite.PieceSet   = value;
                }
            }
        }

        

       
        #endregion

        #region Methods
        

        

        
        

       
        

       

        

        /// <summary>
        /// Determine which menu item is enabled
        /// </summary>
        public void SetCmdState() {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Unlock the chess board when asynchronous computing is finished
        /// </summary>
        private void UnlockBoard() {
            Cursor = Cursors.Arrow;
            SetCmdState();
        }

        
       
        

        /// <summary>
        /// Show the test result of a computer playing against a computer
        /// </summary>
        /// <param name="stat">             Statistic.</param>
        private void TestShowResult(BoardEvaluationStat stat) {
            string      strMsg;
            string      strMethod1;
            string      strMethod2;
            int         iTimeMethod1;
            int         iTimeMethod2;
            SearchMode  searchMode;

            searchMode      = m_chessCtl.SearchMode;
            strMethod1      = searchMode.m_boardEvaluationWhite.Name;
            strMethod2      = searchMode.m_boardEvaluationBlack.Name;
            iTimeMethod1    = (stat.m_iMethod1MoveCount == 0) ? 0 : stat.m_timeSpanMethod1.Milliseconds / stat.m_iMethod1MoveCount;
            iTimeMethod2    = (stat.m_iMethod2MoveCount == 0) ? 0 : stat.m_timeSpanMethod2.Milliseconds / stat.m_iMethod2MoveCount;
            strMsg          = stat.m_iGameCount.ToString() + " game(s) played.\r\n" +
                              stat.m_iMethod1WinCount.ToString() + " win(s) for method #1 (" + strMethod1 + "). Average time = " + stat.m_iMethod1WinCount.ToString() + " ms per move.\r\n" + 
                              stat.m_iMethod2WinCount.ToString() + " win(s) for method #2 (" + strMethod2 + "). Average time = " + stat.m_iMethod2WinCount.ToString() + " ms per move.\r\n" + 
                              (stat.m_iGameCount - stat.m_iMethod1WinCount - stat.m_iMethod2WinCount).ToString() + " draw(s).";
            MessageBox.Show(strMsg);
        }

        private void TestBoardEvaluation_StartNewGame(BoardEvaluationStat stat) {
            SearchMode          searchMode;
            IBoardEvaluation    boardEvaluation;

            m_chessCtl.ResetBoard();
            searchMode                          = m_chessCtl.SearchMode;
            boardEvaluation                     = searchMode.m_boardEvaluationWhite;
            searchMode.m_boardEvaluationWhite   = searchMode.m_boardEvaluationBlack;
            searchMode.m_boardEvaluationBlack   = boardEvaluation;
          
        }

        /// <summary>
        /// Play the next move when doing a board evaluation
        /// </summary>
        /// <param name="stat"> Board evaluation statistic</param>
        /// <param name="move"> Move to be done</param>
        private void TestBoardEvaluation_PlayNextMove(BoardEvaluationStat stat, MoveExt move) {
            ChessBoard.GameResultE  eResult;
            bool                    bIsSearchCancel;
            bool                    bEven;

            bEven           = ((stat.m_iGameIndex & 1) == 0);
            bIsSearchCancel = m_chessCtl.IsSearchCancel;
            if (move == null || bIsSearchCancel) {
                eResult = ChessBoard.GameResultE.TieNoMove;
            } else if (m_chessCtl.Board.MovePosStack.Count > 250) {
                eResult = ChessBoard.GameResultE.TieNoMatePossible;
            } else {
                if ((m_chessCtl.Board.CurrentPlayer == ChessBoard.PlayerE.White && bEven) ||
                    (m_chessCtl.Board.CurrentPlayer == ChessBoard.PlayerE.Black && !bEven)) {
                    stat.m_timeSpanMethod1 += move.TimeToCompute;
                    stat.m_iMethod1MoveCount++;
                } else {
                    stat.m_timeSpanMethod2 += move.TimeToCompute;
                    stat.m_iMethod2MoveCount++;
                }
                eResult = m_chessCtl.DoMove(move, false /*bFlashing*/);
            }
            if (eResult == ChessBoard.GameResultE.OnGoing || eResult == ChessBoard.GameResultE.Check) {
               
            } else {
                if (eResult == ChessBoard.GameResultE.Mate) {
                    if ((m_chessCtl.NextMoveColor == ChessBoard.PlayerE.Black && bEven) ||
                        (m_chessCtl.NextMoveColor == ChessBoard.PlayerE.White && !bEven)) {
                        stat.m_iMethod1WinCount++;
                    } else {
                        stat.m_iMethod2WinCount++;
                    }
                }
                stat.m_iGameIndex++;
                if (stat.m_iGameIndex < stat.m_iGameCount && !bIsSearchCancel) {
                    TestBoardEvaluation_StartNewGame(stat);
                } else {
                    TestShowResult(stat);
                   
                    m_chessCtl.SearchMode   = stat.m_searchModeOri;
                    m_eMessageMode          = stat.m_eMessageModeOri;
                    UnlockBoard();
                }
            }
        }

        /// <summary>
        /// Tests the computer playing against itself. Can be called asynchronously by a secondary thread.
        /// </summary>
        /// <param name="iGameCount">       Number of games to play.</param>
        /// <param name="searchMode">       Search mode</param>
        private void TestBoardEvaluation(int iGameCount, SearchMode searchMode) {
            BoardEvaluationStat     stat;

            stat                    = new BoardEvaluationStat(iGameCount);
            stat.m_searchModeOri    = m_chessCtl.SearchMode;
            stat.m_eMessageModeOri  = m_eMessageMode;
            m_eMessageMode          = MessageModeE.Silent;
            m_chessCtl.SearchMode   = searchMode;
           
            TestBoardEvaluation_StartNewGame(stat);
        }

       
        

        
        
        







        /// <summary>
        /// Observe a FICS Game
        /// </summary>
        

        

        /// <summary>
        /// Disconnect from the FICS Chess Server
        /// </summary>
        private void DisconnectFromFICS() {
            if (m_ficsConnection != null) {
                m_ficsConnection.Dispose();
                m_ficsConnection = null;
            }
        }

       








        #endregion
        

       
        


        

        
        

        #region Sink
        /// <summary>
        /// Called each second for timer click
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            GameTimer   gameTimer;
            
            gameTimer                               = m_chessCtl.GameTimer;
           
           
            if (gameTimer.MaxWhitePlayTime.HasValue) {
                m_toolbar.labelWhiteLimitPlayTime.Content = "(" + GameTimer.GetHumanElapse(gameTimer.MaxWhitePlayTime.Value) + "/" + gameTimer.MoveIncInSec.ToString() + ")";
            }
            if (gameTimer.MaxBlackPlayTime.HasValue) {
                m_toolbar.labelBlackLimitPlayTime.Content = "(" + GameTimer.GetHumanElapse(gameTimer.MaxBlackPlayTime.Value) + "/" + gameTimer.MoveIncInSec.ToString() + ")";
            }
        }

        /// <summary>
        /// Called to gets the selected piece for design mode
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void m_chessCtl_QueryPiece(object sender, ChessBoardControl.QueryPieceEventArgs e) {
            e.Piece = m_lostPieceBlack.SelectedPiece;
        }

        /// <summary>
        /// Called to gets the type of pawn promotion for the current move
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void m_chessCtl_QueryPawnPromotionType(object sender, ChessBoardControl.QueryPawnPromotionTypeEventArgs e) {
            frmQueryPawnPromotionType   frm;
            
            frm         = new frmQueryPawnPromotionType(e.ValidPawnPromotion);
            frm.Owner   = this;
            frm.ShowDialog();
            e.PawnPromotionType = frm.PromotionType;
        }

        
        /// <summary>
        /// Called when a new move has been done in the chessboard control
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void m_chessCtl_NewMove(object sender, ChessBoardControl.NewMoveEventArgs e) {
            MoveExt             move;
            ChessBoard.PlayerE  eMoveColor;

            move        = e.Move;
            eMoveColor  = m_chessCtl.ChessBoard.LastMovePlayer;
            
           
        }

       
        

        /// <summary>
        /// Called when the user has selected a valid move
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        void m_chessCtl_MoveSelected(object sender, ChessBoardControl.MoveSelectedEventArgs e) {
            m_chessCtl.DoUserMove(e.Move);
        }

        /// <summary>
        /// Called when the state of the commands need to be refreshed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void m_chessCtl_UpdateCmdState(object sender, EventArgs e) {
            m_lostPieceBlack.Refresh();
            m_lostPieceWhite.Refresh();
            SetCmdState();
        }


        #endregion

        
    } // Class MainWindow
} // Namespace
