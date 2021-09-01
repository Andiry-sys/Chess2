using System;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace SrcChess2 {
    /// <summary>
    /// Move Item
    /// </summary>
    public class MoveItem {
        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="step">     Move step</param>
        /// <param name="who">      Who did the move</param>
        /// <param name="move">     Move</param>
        public          MoveItem(string step, string who, string move) { Step = step; Who = who; Move = move; }
        /// <summary>Step</summary>
        public  string  Step { get; set; }
        /// <summary>Who did the move</summary>
        public  string  Who { get; set; }
        /// <summary>Move</summary>
        public  string  Move { get; set; }
    }

    /// <summary>List of moves</summary>
    public class MoveItemList : ObservableCollection<MoveItem> {
    }

    /// <summary>
    /// User interface displaying the list of moves
    /// </summary>
    public partial class MoveViewer : UserControl {

        /// <summary>How the move are displayed: Move position (E2-E4) or PGN (e4)</summary>
        public enum DisplayModeE {
            /// <summary>Display move using starting-ending position</summary>
            MovePos,
            /// <summary>Use PGN notation</summary>
            PGN
        }
        
        /// <summary>Argument for the NewMoveSelected event</summary>
        public class NewMoveSelectedEventArg : System.ComponentModel.CancelEventArgs {
            /// <summary>New selected index in the list</summary>
            public int  NewIndex;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="iNewIndex">    New index</param>
            public NewMoveSelectedEventArg(int iNewIndex) : base(false) {
                NewIndex = iNewIndex;
            }
        }
        
        /// <summary>Called when a move has been selected by the control</summary>
        public event EventHandler<NewMoveSelectedEventArg>      NewMoveSelected;
        /// <summary>Chess board control associated with the move viewer</summary>
        private ChessBoardControl                               m_chessCtl;
        /// <summary>Display Mode</summary>
        private DisplayModeE                                    m_eDisplayMode;
        /// <summary>true to ignore change</summary>
        private bool                                            m_bIgnoreChg;
        /// <summary>List of moves</summary>
        public  MoveItemList                                    MoveList { get; private set; }

        /// <summary>
        /// Class Ctor
        /// </summary>
        public MoveViewer() {
            InitializeComponent();
            m_eDisplayMode                      = DisplayModeE.MovePos;
            m_bIgnoreChg                        = false;
            MoveList                            = listViewMoveList.ItemsSource as MoveItemList;
            listViewMoveList.SelectionChanged  += new SelectionChangedEventHandler(listViewMoveList_SelectionChanged);
        }

        /// <summary>
        /// Chess board control associated with move viewer
        /// </summary>
        public ChessBoardControl ChessControl {
            get {
                return(m_chessCtl);
            }
            set {
                if (m_chessCtl != value) {
                    if (m_chessCtl != null) { 
                        m_chessCtl.BoardReset      -= m_chessCtl_BoardReset;
                        m_chessCtl.NewMove         -= m_chessCtl_NewMove;
                        m_chessCtl.RedoPosChanged  -= m_chessCtl_RedoPosChanged;
                    }
                    m_chessCtl = value;
                    if (m_chessCtl != null) { 
                        m_chessCtl.BoardReset     += m_chessCtl_BoardReset;
                        m_chessCtl.NewMove        += m_chessCtl_NewMove;
                        m_chessCtl.RedoPosChanged += m_chessCtl_RedoPosChanged;
                    }
                }
            }
        }

        
        

       
        private void Redisplay() {
            
            int             iMoveCount;
            MovePosStack    movePosStack;
            MoveExt         move;
            string          strMove;
            string          strMoveIndex;
            MoveItem        moveItem;
            ChessBoard      chessBoard;

            chessBoard = m_chessCtl.Board;
            if (chessBoard != null) {
                movePosStack    = chessBoard.MovePosStack;
                iMoveCount      = movePosStack.Count;
                if (iMoveCount != 0) {
                    
                    for (int iIndex = 0; iIndex < iMoveCount; iIndex++) {
                        move = movePosStack[iIndex];
                        if (m_eDisplayMode == DisplayModeE.MovePos) {
                            strMove         = ChessBoard.GetHumanPos(move);
                            
                        } else {
                            
                            strMoveIndex    = (iIndex / 2 + 1).ToString() + ((Char)('a' + (iIndex & 1))).ToString();
                        }
                        moveItem            = MoveList[iIndex];
                       
                    }
                }
            }
        }

       
        private void AddCurrentMove() {
            MoveItem            moveItem;
            string              strMove;
            string              strMoveIndex;
            int                 iMoveCount;
            int                 iItemCount;
            int                 iIndex;
            MoveExt             move;
            ChessBoard.PlayerE  ePlayerToMove;
            ChessBoard          chessBoard;

            chessBoard      = m_chessCtl.Board;            
            m_bIgnoreChg    = true;
            move            = chessBoard.MovePosStack.CurrentMove;
            ePlayerToMove   = chessBoard.LastMovePlayer;
            chessBoard.UndoMove();
            iMoveCount      = chessBoard.MovePosStack.Count;
            iItemCount      = listViewMoveList.Items.Count;
            while (iItemCount >= iMoveCount) {
                iItemCount--;
                MoveList.RemoveAt(iItemCount);
            }
            
            chessBoard.RedoMove();
            iIndex          = iItemCount;
            strMoveIndex    = (m_eDisplayMode == DisplayModeE.MovePos) ? (iIndex + 1).ToString() : (iIndex / 2 + 1).ToString() + ((Char)('a' + (iIndex & 1))).ToString();
            
           
            m_bIgnoreChg    = false;
        }

        /// <summary>
        /// Select the current move
        /// </summary>
        private void SelectCurrentMove() {
            int             iIndex;
            
            ChessBoard      chessBoard;

            chessBoard   = m_chessCtl.Board;
            m_bIgnoreChg = true;
            iIndex       = chessBoard.MovePosStack.PositionInList;
            if (iIndex == -1) {
                listViewMoveList.SelectedItem = null;
            } else {
               
               
                
            }
            m_bIgnoreChg = false;
        }

        /// <summary>
        /// Display Mode (Position or PGN)
        /// </summary>
        public DisplayModeE DisplayMode {
            get {
                return(m_eDisplayMode);
            }
            set {
                if (value != m_eDisplayMode) {
                    m_eDisplayMode = value;
                    Redisplay();
                }
            }
        }

        /// <summary>
        /// Reset the control so it represents the specified chessboard
        /// </summary>
        private void Reset() {
            int         iCurPos;
            int         iCount;
            ChessBoard  chessBoard;
            
            MoveList.Clear();
            chessBoard      = m_chessCtl.Board;
            iCurPos         = chessBoard.MovePosStack.PositionInList;
            iCount          = chessBoard.MovePosStack.Count;
            chessBoard.UndoAllMoves();
            for (int iIndex = 0; iIndex < iCount; iIndex++) {
                chessBoard.RedoMove();
                AddCurrentMove();
            }
            SelectCurrentMove();
        }

        /// <summary>
        /// Trigger the NewMoveSelected argument
        /// </summary>
        /// <param name="e">        Event arguments</param>
        protected void OnNewMoveSelected(NewMoveSelectedEventArg e) {
            if (NewMoveSelected != null) {
                NewMoveSelected(this, e);
            }
        }

        private void m_chessCtl_RedoPosChanged(object sender, EventArgs e) {
            SelectCurrentMove();
        }

        private void m_chessCtl_NewMove(object sender, ChessBoardControl.NewMoveEventArgs e) {
            AddCurrentMove();
            SelectCurrentMove();
        }

        private void m_chessCtl_BoardReset(object sender, EventArgs e) {
            Reset();
        }

        /// <summary>
        /// Called when the user select a move
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void listViewMoveList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            NewMoveSelectedEventArg evArg;
            int                     iCurPos;
            int                     iNewPos;
            ChessBoard              chessBoard;
            
            if (!m_bIgnoreChg && !m_chessCtl.IsBusy && !m_chessCtl.IsSearchEngineBusy) {
                m_bIgnoreChg    = true;
                chessBoard      = m_chessCtl.Board;
                iCurPos         = chessBoard.MovePosStack.PositionInList;
                if (e.AddedItems.Count != 0) {
                    iNewPos = listViewMoveList.SelectedIndex;
                    if (iNewPos != iCurPos) {
                        evArg = new NewMoveSelectedEventArg(iNewPos);
                        OnNewMoveSelected(evArg);
                        if (evArg.Cancel) {
                            if (iCurPos == -1) {
                                listViewMoveList.SelectedItems.Clear();
                            } else {
                                listViewMoveList.SelectedIndex  = iCurPos;
                            }
                        }
                    }
                }
                m_bIgnoreChg = false;
            }
        }
    } // Class MoveViewer
} // Namespace
