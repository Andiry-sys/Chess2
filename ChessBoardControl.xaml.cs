using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace SrcChess2 {
    /// <summary>
    /// Defines a Chess Board Control
    /// </summary>
    public partial class ChessBoardControl : UserControl, SearchEngine.ITrace, IXmlSerializable {

        #region Inner Class
        /// <summary>
        /// Integer Point
        /// </summary>
        public struct IntPoint {
            /// <summary>
            /// Class Ctor
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public  IntPoint(int x, int y) { X = x; Y = y; }
            /// <summary>X point</summary>
            public  int X;
            /// <summary>Y point</summary>
            public  int Y;
        }

        /// <summary>
        /// Arguments for the Reset event
        /// </summary>
        public class NewMoveEventArgs : EventArgs {
            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="move">                 Move</param>
            /// <param name="eMoveResult">          Move result</param>
            public NewMoveEventArgs(MoveExt move, ChessBoard.GameResultE eMoveResult) {
                Move                = move;
                MoveResult          = eMoveResult;
            }

            /// <summary>
            /// Move which has been done
            /// </summary>
            public MoveExt Move {
                get;
                private set;
            }

            /// <summary>
            /// Move result
            /// </summary>
            public ChessBoard.GameResultE MoveResult {
                get;
                private set;
            }
        }
        
        /// <summary>
        /// Interface implemented by the UI which show the lost pieces.
        /// This interface is called each time the chess board need an update on the lost pieces UI.
        /// </summary>
        public interface IUpdateCmd {
            /// <summary>Update the lost pieces</summary>
            void        Update();
        }

        /// <summary>
        /// Show a piece moving from starting to ending point
        /// </summary>
        private class SyncFlash {
            /// <summary>Chess Board Control</summary>
            private ChessBoardControl           m_chessBoardControl;
            /// <summary>Solid Color Brush to flash</summary>
            private SolidColorBrush             m_brush;
            /// <summary>First Flash Color</summary>
            private Color                       m_colorStart;
            /// <summary>Second Flash Color</summary>
            private Color                       m_colorEnd;
            /// <summary>Dispatcher Frame. Wait for flash</summary>
            private DispatcherFrame             m_dispatcherFrame;

            /// <summary>
            /// Class Ctor
            /// </summary>
            /// <param name="chessBoardControl">    Chess Board Control</param>
            /// <param name="brush">                Solid Color Brush to flash</param>
            /// <param name="colorStart">           First flashing color</param>
            /// <param name="colorEnd">             Second flashing color</param>
            public SyncFlash(ChessBoardControl chessBoardControl, SolidColorBrush brush, Color colorStart, Color colorEnd) {
                m_chessBoardControl = chessBoardControl;
                m_brush             = brush;
                m_colorStart        = colorStart;
                m_colorEnd          = colorEnd;
            }

            /// <summary>
            /// Flash the specified cell
            /// </summary>
            /// <param name="iCount">                   Flash count</param>
            /// <param name="dSec">                     Flash duration</param>
            /// <param name="eventHandlerTerminated">   Event handler to call when flash is finished</param>
            private void FlashCell(int iCount, double dSec, EventHandler eventHandlerTerminated) {
                ColorAnimation                  colorAnimation;
            
                colorAnimation                  = new ColorAnimation(m_colorStart, m_colorEnd, new Duration(TimeSpan.FromSeconds(dSec)));
                colorAnimation.AutoReverse      = true;
                colorAnimation.RepeatBehavior   = new RepeatBehavior(iCount / 2);
                if (eventHandlerTerminated != null) {
                    colorAnimation.Completed   += new EventHandler(eventHandlerTerminated);
                }
                m_brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            }

            /// <summary>
            /// Show the move
            /// </summary>
            public void Flash() {
                m_chessBoardControl.IsEnabled       = false;
                FlashCell(4, 0.15, new EventHandler(FirstFlash_Completed));
                m_dispatcherFrame   = new DispatcherFrame();
                Dispatcher.PushFrame(m_dispatcherFrame);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void FirstFlash_Completed(object sender, EventArgs e) {
                m_chessBoardControl.IsEnabled   = true;
                m_dispatcherFrame.Continue      = false;

            }
        } // Class SyncFlash

        /// <summary>Event argument for the MoveSelected event</summary>
        public class MoveSelectedEventArgs : System.EventArgs {
            /// <summary>Move position</summary>
            public MoveExt  Move;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="move">     Move position</param>
            public MoveSelectedEventArgs(MoveExt move) { Move = move; }
        }

        /// <summary>Event argument for the QueryPiece event</summary>
        public class QueryPieceEventArgs : System.EventArgs {
            /// <summary>Position of the square</summary>
            public int                  Pos;
            /// <summary>Piece</summary>
            public ChessBoard.PieceE    Piece;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="iPos">     Position of the square</param>
            /// <param name="ePiece">   Piece</param>
            public QueryPieceEventArgs(int iPos, ChessBoard.PieceE ePiece) { Pos = iPos; Piece = ePiece; }
        }

        /// <summary>Event argument for the QueryPawnPromotionType event</summary>
        public class QueryPawnPromotionTypeEventArgs : System.EventArgs {
            /// <summary>Promotion type (Queen, Rook, Bishop, Knight or Pawn)</summary>
            public Move.TypeE                           PawnPromotionType;
            /// <summary>Possible pawn promotions in the current context</summary>
            public ChessBoard.ValidPawnPromotionE       ValidPawnPromotion;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="eValidPawnPromotion">  Possible pawn promotions in the current context</param>
            public QueryPawnPromotionTypeEventArgs(ChessBoard.ValidPawnPromotionE eValidPawnPromotion) { ValidPawnPromotion = eValidPawnPromotion; PawnPromotionType = Move.TypeE.Normal; }
        }

        /// <summary>Cookie for FindBestMove method</summary>
        /// <typeparam name="T">Original cookie type</typeparam>
        private class FindBestMoveCookie<T> {
            public FindBestMoveCookie(Action<T, MoveExt> oriAction, T oriCookie) {
                m_oriAction         = oriAction;
                m_oriCookie         = oriCookie;
                m_dtStartFinding    = DateTime.Now;
            }
            public Action<T,MoveExt>    m_oriAction;
            public T                    m_oriCookie;
            public DateTime             m_dtStartFinding;
        }
        #endregion

        #region Members
        /// <summary>Lite Cell Color property</summary>
        public static readonly DependencyProperty       LiteCellColorProperty;
        /// <summary>Dark Cell Color property</summary>
        public static readonly DependencyProperty       DarkCellColorProperty;
        /// <summary>White Pieces Color property</summary>
        public static readonly DependencyProperty       WhitePieceColorProperty;
        /// <summary>Black Pieces Color property</summary>
        public static readonly DependencyProperty       BlackPieceColorProperty;
        /// <summary>Determine if a move is flashing</summary>
        public static readonly  DependencyProperty      MoveFlashingProperty;

        /// <summary>Called when a user select a valid move to be done</summary>
        public event EventHandler<MoveSelectedEventArgs>MoveSelected;
        /// <summary>Triggered when the board is being reset</summary>
        public event EventHandler<EventArgs>            BoardReset;
        /// <summary>Called when a new move has been done</summary>
        public event EventHandler<NewMoveEventArgs>     NewMove;
        /// <summary>Called when the redo position has been changed</summary>
        public event EventHandler                       RedoPosChanged;
        /// <summary>Delegate for the QueryPiece event</summary>
        public delegate void                            QueryPieceEventHandler(object sender, QueryPieceEventArgs e);
        /// <summary>Called when chess control in design mode need to know which piece to insert in the board</summary>
        public event QueryPieceEventHandler             QueryPiece;
        /// <summary>Delegate for the QueryPawnPromotionType event</summary>
        public delegate void                            QueryPawnPromotionTypeEventHandler(object sender, QueryPawnPromotionTypeEventArgs e);
        /// <summary>Called when chess control needs to know which type of pawn promotion must be done</summary>
        public event QueryPawnPromotionTypeEventHandler QueryPawnPromotionType;
        /// <summary>Called to refreshed the command state (menu, toolbar etc.)</summary>
        public event System.EventHandler                UpdateCmdState;
        /// <summary>Triggered when find move begin</summary>
        public event System.EventHandler                FindMoveBegin;
        /// <summary>Triggered when find move end</summary>
        public event System.EventHandler                FindMoveEnd;

        /// <summary>Message for Control is busy exception</summary>
        private const string                            m_strCtlIsBusy = "Control is busy";
        /// <summary>Piece Set to use</summary>
        private PieceSet                                m_pieceSet;
        /// <summary>Board</summary>
        private ChessBoard                              m_board;
        /// <summary>Array of frames containing the chess piece</summary>
        private Border[]                                m_arrBorder;
        /// <summary>Array containing the current piece</summary>
        private ChessBoard.PieceE[]                     m_arrPiece;
        /// <summary>true to have white in the bottom of the screen, false to have black</summary>
        private bool                                    m_bWhiteInBottom = true;
        ///// <summary>Font use to draw coordinate on the side of the control</summary>
        //private Font                                  m_fontCoord;  TODO
        /// <summary>Currently selected cell</summary>
        private IntPoint                                m_ptSelectedCell;
        /// <summary>Timer for both player</summary>
        private GameTimer                               m_gameTimer;
        /// <summary>Not zero when board is flashing and reentrance can be a problem</summary>
        private int                                     m_iBusy;
        /// <summary>Signal that a move has been completed</summary>
        private System.Threading.EventWaitHandle        m_signalActionDone;
        #endregion

        #region Board creation
        /// <summary>
        /// Static Ctor
        /// </summary>
        static ChessBoardControl() {
            LiteCellColorProperty   = DependencyProperty.Register("LiteCellColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.Moccasin,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            DarkCellColorProperty   = DependencyProperty.Register("DarkCellColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.SaddleBrown,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            WhitePieceColorProperty = DependencyProperty.Register("WhitePieceColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.White,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            BlackPieceColorProperty = DependencyProperty.Register("BlackPieceColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.Black,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            MoveFlashingProperty    = DependencyProperty.Register("MoveFlashing", 
                                                                  typeof(bool),
                                                                  typeof(ChessBoardControl), 
                                                                  new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Class Ctor
        /// </summary>
        public ChessBoardControl() {
            InitializeComponent();
            m_iBusy             = 0;
            m_signalActionDone  = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
            m_board             = new ChessBoard(this);
            m_ptSelectedCell    = new IntPoint(-1, -1);
            AutoSelection       = true;
            m_gameTimer         = new GameTimer();
            m_gameTimer.Enabled = false;
            m_gameTimer.Reset(m_board.CurrentPlayer);
            WhitePlayerName     = "Player 1";
            BlackPlayerName     = "Player 2";
            
            SearchMode          = new SearchMode(new BoardEvaluationBasic(),
                                                 new BoardEvaluationBasic(),
                                                 SearchMode.OptionE.UseIterativeDepthSearch,
                                                 SearchMode.ThreadingModeE.OnePerProcessorForSearch,
                                                 6 /*plyCount*/,
                                                 15 /*iTimeOutInSec*/,
                                                 SearchMode.RandomModeE.On,
                                                 SearchMode.Book2500,
                                                 SearchMode.Book2500);
           InitCell();
            IsDirty             = false;
        }

        /// <summary>
        /// Returns the XML schema if any
        /// </summary>
        /// <returns>
        /// null
        /// </returns>
        public System.Xml.Schema.XmlSchema GetSchema() {
 	        return(null);
        }

        /// <summary>
        /// Deserialized the control from a XML reader
        /// </summary>
        /// <param name="reader">   Reader</param>
        public void ReadXml(XmlReader reader) {
            string      strWhitePlayerName;
            string      strBlackPlayerName;
            long        lWhiteTicksCount;
            long        lBlackTicksCount;

            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "SrcChess2") {
                throw new SerializationException("Unknown format");
            } else if (reader.GetAttribute("Version") != "1.00") {
                throw new SerializationException("Unknown version");
            } else {
                strWhitePlayerName  = reader.GetAttribute("WhitePlayerName");
                strBlackPlayerName  = reader.GetAttribute("BlackPlayerName");
                lWhiteTicksCount    = Int64.Parse(reader.GetAttribute("WhiteTicksCount"));
                lBlackTicksCount    = Int64.Parse(reader.GetAttribute("BlackTicksCount"));
                reader.ReadStartElement();
                ((IXmlSerializable)m_board).ReadXml(reader);
                InitAfterLoad(strWhitePlayerName, strBlackPlayerName, lWhiteTicksCount, lBlackTicksCount);
                reader.ReadEndElement();
            }
        }

        /// <summary>
        /// Serialize the control into a XML writer
        /// </summary>
        /// <param name="writer">   XML writer</param>
        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement("SrcChess2");
            writer.WriteAttributeString("Version",         "1.00");
            writer.WriteAttributeString("WhitePlayerName", WhitePlayerName);
            writer.WriteAttributeString("BlackPlayerName", BlackPlayerName);
            writer.WriteAttributeString("WhiteTicksCount", m_gameTimer.WhitePlayTime.Ticks.ToString());
            writer.WriteAttributeString("BlackTicksCount", m_gameTimer.BlackPlayTime.Ticks.ToString());
            ((IXmlSerializable)m_board).WriteXml(writer);
            writer.WriteEndElement();
            IsDirty = false;
        }

        /// <summary>
        /// Refresh the board color
        /// </summary>
        private void RefreshBoardColor() {
            int     iPos;
            Border  border;
            Brush   brushDark;
            Brush   brushLite;

            iPos        = 63;
            brushDark   = new SolidColorBrush(DarkCellColor); // m_colorInfo.m_colDarkCase);
            brushLite   = new SolidColorBrush(LiteCellColor); // m_colorInfo.m_colLiteCase);
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    border              = m_arrBorder[iPos];
                    border.Background   = (((x + y) & 1) == 0) ? brushLite : brushDark;
                    iPos--;
                }
            }
        }

        /// <summary>
        /// Initialize the cell
        /// </summary>
        private void InitCell() {
            int     iPos;
            Border  border;
            Brush   brushDark;
            Brush   brushLite;

            m_arrBorder = new Border[64];
            m_arrPiece  = new ChessBoard.PieceE[64];
            iPos        = 63;
            brushDark   = new SolidColorBrush(DarkCellColor);   // m_colorInfo.m_colDarkCase);
            brushLite   = new SolidColorBrush(LiteCellColor);   // m_colorInfo.m_colLiteCase);
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    border                  = new Border();
                    border.Name             = "Cell" + (iPos.ToString());
                    border.BorderThickness  = new Thickness(0);
                    border.Background       = (((x + y) & 1) == 0) ? brushLite : brushDark;
                    border.BorderBrush      = border.Background;
                    border.SetValue(Grid.ColumnProperty, x);
                    border.SetValue(Grid.RowProperty, y);
                    m_arrBorder[iPos]       = border;
                    m_arrPiece[iPos]        = ChessBoard.PieceE.None;
                    CellContainer.Children.Add(border);
                    iPos--;
                }
            }
        }

        /// <summary>
        /// Set the chess piece control
        /// </summary>
        /// <param name="iBoardPos">    Board position</param>
        /// <param name="ePiece">       Piece</param>
        private void SetPieceControl(int iBoardPos, ChessBoard.PieceE ePiece) {
            Border      border;
            UserControl userControlPiece;

            border              = m_arrBorder[iBoardPos];
            userControlPiece    = m_pieceSet[ePiece];
            if (userControlPiece != null) {
                userControlPiece.Margin  = (border.BorderThickness.Top == 0) ? new Thickness(3) : new Thickness(1);
            }
            m_arrPiece[iBoardPos]   = ePiece;
            border.Child            = userControlPiece;
        }

        /// <summary>
        /// Refresh the specified cell
        /// </summary>
        /// <param name="iBoardPos">    Board position</param>
        /// <param name="bFullRefresh"> true to refresh even if its the same piece</param>
        private void RefreshCell(int iBoardPos, bool bFullRefresh) {
            ChessBoard.PieceE   ePiece;

            if (m_board != null && m_pieceSet != null) {
                ePiece = m_board[iBoardPos];
                if (ePiece != m_arrPiece[iBoardPos] || bFullRefresh) {
                    SetPieceControl(iBoardPos, ePiece);
                }
            }
        }

        /// <summary>
        /// Refresh the specified cell
        /// </summary>
        /// <param name="iBoardPos">    Board position</param>
        private void RefreshCell(int iBoardPos) {
            RefreshCell(iBoardPos, false);  // bFullRefresh
        }

        /// <summary>
        /// Refresh the board
        /// </summary>
        /// <param name="bFullRefresh"> Refresh even if its the same piece</param>
        private void Refresh(bool bFullRefresh) {
            if (m_board != null && m_pieceSet != null) {
                for (int iBoardPos = 0; iBoardPos < 64; iBoardPos++) {
                    RefreshCell(iBoardPos, bFullRefresh);
                }
            }
        }

        /// <summary>
        /// Refresh the board
        /// </summary>
        public void Refresh() {
            Refresh(false); // bFullRefresh
        }

        /// <summary>
        /// Reset the board to the initial condition
        /// </summary>
        public void ResetBoard() {
            m_board.ResetBoard();
            SelectedCell    = new IntPoint(-1, -1);
            OnBoardReset(EventArgs.Empty);
            OnUpdateCmdState(System.EventArgs.Empty);
            m_gameTimer.Reset(m_board.CurrentPlayer);
            m_gameTimer.Enabled = false;
            Refresh(false); // bForceRefresh
            IsDirty = false;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Called when Image property changed
        /// </summary>
        private static void ColorInfoChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            ChessBoardControl   me;

            me = obj as ChessBoardControl;
            if (me != null && e.OldValue != e.NewValue) {
                me.RefreshBoardColor();
            }
        }

        /// <summary>
        /// Return the search mode
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public SearchMode SearchMode {
            get;
            set;
        }

        /// <summary>
        /// Return true if board control has been changed
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsDirty {
            get;
            set;
        }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("Lite Cell Color")]
        public Color LiteCellColor {
            get {
                return ((Color)GetValue(LiteCellColorProperty));
            }
            set {
                SetValue(LiteCellColorProperty, value);
            }
        }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("Dark Cell Color")]
        public Color DarkCellColor {
            get {
                return ((Color)GetValue(DarkCellColorProperty));
            }
            set {
                SetValue(DarkCellColorProperty, value);
            }
        }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("White Pieces Color")]
        public Color WhitePieceColor {
            get {
                return ((Color)GetValue(WhitePieceColorProperty));
            }
            set {
                SetValue(WhitePieceColorProperty, value);
            }
        }

        /// <summary>
        /// Image displayed to the button
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("Black Pieces Color")]
        public Color BlackPieceColor {
            get {
                return ((Color)GetValue(BlackPieceColorProperty));
            }
            set {
                SetValue(BlackPieceColorProperty, value);
            }
        }

        /// <summary>
        /// Determine if a move is flashing
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Category("General")]
        [Description("Determine if a move is flashing")]
        public bool MoveFlashing  {
            get {
                return (bool)GetValue(MoveFlashingProperty);
            }
            set {
                SetValue(MoveFlashingProperty, value);
            }
        }

        /// <summary>
        /// Current piece set
        /// </summary>
        public PieceSet PieceSet {
            get {
                return(m_pieceSet);
            }
            set {
                if (m_pieceSet != value) {
                    m_pieceSet = value;
                    Refresh(true);  // bForceRefresh
                }
            }
        }

        /// <summary>
        /// Current chess board
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ChessBoard Board {
            get {
                return(m_board);
            }
            set {
                if (m_board != value) {
                    m_board = value;
                    Refresh(false); // bForceRefresh
                }
            }
        }

        /// <summary>
        /// Signal used to determine if the called action has been done
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public System.Threading.EventWaitHandle SignalActionDone {
            get {
                return(m_signalActionDone);
            }
        }

        /// <summary>
        /// Name of the player playing white piece
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string WhitePlayerName {
            get;
            set;
        }

        /// <summary>
        /// Name of the player playing black piece
        /// </summary>
        public string BlackPlayerName {
            get;
            set;
        }

        /// <summary>
        /// Type of player playing white piece
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
       

        

        
        public ChessBoard ChessBoard {
            get {
                return(m_board);
            }
        }

        /// <summary>
        /// Determine if the White are in the top or bottom of the draw board
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool WhiteInBottom {
            get {
                return(m_bWhiteInBottom);
            }
            set {
                if (value != m_bWhiteInBottom) {
                    m_bWhiteInBottom = value;
                    Refresh(false);  // bForceRefresh
                }
            }
        }

        /// <summary>
        /// Enable or disable the auto selection mode
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool AutoSelection {
            get;
            set;
        }

        /// <summary>
        /// Determine the board design mode
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool BoardDesignMode {
            get {
                return(m_board.DesignMode);
            }
            set {
                MessageBoxResult    eRes;
                ChessBoard.PlayerE  eNextMoveColor;
                
                if (m_board.DesignMode != value) {
                    if (value) {
                        m_board.OpenDesignMode();
                        m_board.MovePosStack.Clear();
                        OnBoardReset(EventArgs.Empty);
                        m_gameTimer.Enabled = false;
                        OnUpdateCmdState(System.EventArgs.Empty);
                    } else {
                        eRes = MessageBox.Show("Is the next move to the white?", "SrcChess", MessageBoxButton.YesNo);
                        eNextMoveColor = (eRes == MessageBoxResult.Yes) ? ChessBoard.PlayerE.White : ChessBoard.PlayerE.Black;
                        if (m_board.CloseDesignMode(eNextMoveColor, (ChessBoard.BoardStateMaskE)0, 0 /*iEnPassant*/)) {
                            OnBoardReset(EventArgs.Empty);
                            m_gameTimer.Reset(m_board.CurrentPlayer);
                            m_gameTimer.Enabled = true;
                            IsDirty             = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of move which can be undone
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int UndoCount {
            get {
                return(m_board.MovePosStack.PositionInList + 1);
            }
        }

        /// <summary>
        /// Gets the number of move which can be redone
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int RedoCount {
            get {
                int iCurPos;
                int iCount;
                
                iCurPos = m_board.MovePosStack.PositionInList;
                iCount  = m_board.MovePosStack.Count;
                return(iCount - iCurPos - 1);
            }
        }

        /// <summary>
        /// Current color to play
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ChessBoard.PlayerE NextMoveColor {
            get {
                return(m_board.CurrentPlayer);
            }
        }

        /// <summary>
        /// List of played moves
        /// </summary>
        private MoveExt[] MoveList {
            get {
                MoveExt[]   arrMoveList;
                int         iMoveCount;
                
                iMoveCount  = m_board.MovePosStack.PositionInList + 1;
                arrMoveList = new MoveExt[iMoveCount];
                if (iMoveCount != 0) {
                    m_board.MovePosStack.List.CopyTo(0, arrMoveList, 0, iMoveCount);
                }
                return(arrMoveList);
            }
        }

        /// <summary>
        /// Game timer
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public GameTimer GameTimer {
            get {
                return(m_gameTimer);
            }
        }

        /// <summary>
        /// Currently selected case
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public IntPoint SelectedCell {
            get {
                return(m_ptSelectedCell);
            }
            set {
                SetCellSelectionState(m_ptSelectedCell, false);
                m_ptSelectedCell    = value;
                SetCellSelectionState(m_ptSelectedCell, true);
            }
        }

        /// <summary>
        /// true if a cell is selected
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsCellSelected {
            get {
                return(SelectedCell.X != -1 || SelectedCell.Y != -1);
            }
        }

        /// <summary>
        /// Return true if board is flashing and we must not let the control be reentered
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsBusy {
            get {
                return(m_iBusy != 0);
            }
        }

        /// <summary>
        /// Return true if we're observing a game from a chess server
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsObservingAGame {
            get;
            set;
        }

        /// <summary>
        /// Returns if the search engine is busy
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsSearchEngineBusy {
            get {
                return(SearchEngine.IsSearchEngineBusy);
            }
        }

        /// <summary>
        /// Returns if the running search for best move has been canceled
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsSearchCancel {
            get {
                return(SearchEngine.IsSearchHasBeenCanceled);
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Trigger the FindMoveBegin event.
        /// </summary>
        /// <param name="e">    Event argument</param>
        protected void OnFindMoveBegin(System.EventArgs e) {
            if (FindMoveBegin != null) {
                FindMoveBegin(this, e);
            }
        }

        /// <summary>
        /// Trigger the FindMoveEnd event.
        /// </summary>
        /// <param name="e">    Event argument</param>
        protected void OnFindMoveEnd(System.EventArgs e) {
            if (FindMoveEnd != null) {
                FindMoveEnd(this, e);
            }
        }

        /// <summary>
        /// Trigger the UpdateCmdState event. Called when command state need to be reevaluated.
        /// </summary>
        /// <param name="e">    Event argument</param>
        protected void OnUpdateCmdState(System.EventArgs e) {
            if (UpdateCmdState != null) {
                UpdateCmdState(this, e);
            }
        }

        /// <summary>
        /// Trigger the BoardReset event
        /// </summary>
        /// <param name="e">        Event arguments</param>
        protected void OnBoardReset(EventArgs e) {
            if (BoardReset != null) {
                BoardReset(this, e);
            }
        }

        /// <summary>
        /// Trigger the RedoPosChanged event
        /// </summary>
        /// <param name="e">        Event arguments</param>
        protected void OnRedoPosChanged(EventArgs e) {
            if (RedoPosChanged != null) {
                RedoPosChanged(this, e);
            }
        }

        /// <summary>
        /// Trigger the NewMove event
        /// </summary>
        /// <param name="e">        Event arguments</param>
        protected void OnNewMove(NewMoveEventArgs e) {
            if (NewMove != null) {
                NewMove(this, e);
            }
        }

        /// <summary>
        /// Trigger the MoveSelected event
        /// </summary>
        /// <param name="e">    Event arguments</param>
        protected virtual void OnMoveSelected(MoveSelectedEventArgs e) {
            if (MoveSelected != null) {
                MoveSelected(this, e);
            }
        }

        /// <summary>
        /// OnQueryPiece:       Trigger the QueryPiece event
        /// </summary>
        /// <param name="e">    Event arguments</param>
        protected virtual void OnQueryPiece(QueryPieceEventArgs e) {
            if (QueryPiece != null) {
                QueryPiece(this, e);
            }
        }

        /// <summary>
        /// OnQweryPawnPromotionType:   Trigger the QueryPawnPromotionType event
        /// </summary>
        /// <param name="e">            Event arguments</param>
        protected virtual void OnQueryPawnPromotionType(QueryPawnPromotionTypeEventArgs e) {
            if (QueryPawnPromotionType != null) {
                QueryPawnPromotionType(this, e);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Show an error message
        /// </summary>
        /// <param name="strError"> Error message</param>
        public void ShowError(string strError) {
            MessageBox.Show(Window.GetWindow(this), strError, "...", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Show a message
        /// </summary>
        /// <param name="strMsg">   Message</param>
        public void ShowMessage(string strMsg) {
            MessageBox.Show(Window.GetWindow(this), strMsg, "...", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Set the cell selection  appearance
        /// </summary>
        /// <param name="ptCell"></param>
        /// <param name="bSelected"></param>
        private void SetCellSelectionState(IntPoint ptCell, bool bSelected) {
            Border  border;
            Control ctl;
            int     iPos;

            if (ptCell.X != -1 && ptCell.Y != -1) {
                iPos                    = ptCell.X + ptCell.Y * 8;
                border                  = m_arrBorder[iPos];
                border.BorderBrush      = (bSelected) ? Brushes.Black : border.Background;
                border.BorderThickness  = (bSelected) ? new Thickness(1) : new Thickness(0);
                ctl                     = border.Child as Control;
                if (ctl != null) {
                    ctl.Margin  = (bSelected) ? new Thickness(1) : new Thickness(3);
                }
            }
        }

        /// <summary>
        /// Save the current game into a file
        /// </summary>
        /// <param name="writer">   Binary writer</param>
        public virtual void SaveGame(BinaryWriter writer) {
            string                  strVersion;
            
            strVersion = "SRCBC095";
            writer.Write(strVersion);
            m_board.SaveBoard(writer);
            writer.Write(WhitePlayerName);
            writer.Write(BlackPlayerName);
            writer.Write(m_gameTimer.WhitePlayTime.Ticks);
            writer.Write(m_gameTimer.BlackPlayTime.Ticks);
            IsDirty = false;
        }

        /// <summary>
        /// Initialize the board control after a board has been loaded
        /// </summary>
        private void InitAfterLoad(string strWhitePlayerName, string strBlackPlayerName, long lWhiteTicks, long lBlackTicks) {
            OnBoardReset(EventArgs.Empty);
            OnUpdateCmdState(System.EventArgs.Empty);
            Refresh(false); // bForceRefresh
            WhitePlayerName     = strWhitePlayerName;
            BlackPlayerName     = strBlackPlayerName;
            IsDirty             = false;
            m_gameTimer.ResetTo(m_board.CurrentPlayer, lWhiteTicks, lBlackTicks);
            m_gameTimer.Enabled = true;
        }

        /// <summary>
        /// Load a game from a stream
        /// </summary>
        /// <param name="reader">   Binary reader</param>
        public virtual bool LoadGame(BinaryReader reader) {
            bool                        bRetVal;
            string                      strVersion;
            string                      strWhitePlayerName;
            string                      strBlackPlayerName;
            long                        lWhiteTicks;
            long                        lBlackTicks;
            
            strVersion = reader.ReadString();
            if (strVersion != "SRCBC095") {
                bRetVal = false;
            } else {
                bRetVal = m_board.LoadBoard(reader);
                if (bRetVal) {
                    strWhitePlayerName  = reader.ReadString();
                    strBlackPlayerName  = reader.ReadString();
                    lWhiteTicks         = reader.ReadInt64();
                    lBlackTicks         = reader.ReadInt64();
                    InitAfterLoad(strWhitePlayerName, strBlackPlayerName, lWhiteTicks, lBlackTicks);
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Save the board content into a snapshot string.
        /// </summary>
        /// <returns>
        /// Snapshot
        /// </returns>
        public string TakeSnapshot() {
            StringBuilder       strbRetVal;
            XmlWriter           writer;
            XmlWriterSettings   xmlSettings;

            strbRetVal          = new StringBuilder(16384);
            xmlSettings         = new XmlWriterSettings();
            xmlSettings.Indent  = true;
            writer              = XmlWriter.Create(strbRetVal, xmlSettings);
            ((IXmlSerializable)this).WriteXml(writer);
            writer.Close();
            return(strbRetVal.ToString());
        }

        /// <summary>
        /// Restore the snapshot
        /// </summary>
        /// <param name="strSnapshot">  Snapshot</param>
        public void RestoreSnapshot(string strSnapshot) {
            TextReader  textReader;
            XmlReader   reader;

            textReader  = new StringReader(strSnapshot);
            reader      = XmlReader.Create(textReader);
            ((IXmlSerializable)this).ReadXml(reader);
        }

        

        /// <summary>
        /// Save a board to a file selected by the user
        /// </summary>
        /// <returns>
        /// true if the game has been saved
        /// </returns>
        public bool SaveToFile() {
            bool            bRetVal = false;
            SaveFileDialog  saveDlg;
            Stream          stream;
            
            saveDlg = new SaveFileDialog();
            saveDlg.AddExtension        = true;
            saveDlg.CheckPathExists     = true;
            saveDlg.DefaultExt          = "che";
            saveDlg.Filter              = "Chess Files (*.che)|*.che";
            saveDlg.OverwritePrompt     = true;
            if (saveDlg.ShowDialog() == true) {
                try {
                    stream = saveDlg.OpenFile();
                } catch(System.Exception) {
                    MessageBox.Show("Unable to open the file - " + saveDlg.FileName);
                    stream = null;
                }
                if (stream != null) {
                    try {
                        SaveGame(new BinaryWriter(stream));
                        bRetVal = true;
                        IsDirty = false;
                    } catch(SystemException) {
                        MessageBox.Show("Unable to write to the file '" + saveDlg.FileName + "'.");
                    }
                    stream.Dispose();
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Save the board to a file selected by the user in PGN format
        /// </summary>
        public void SavePGNToFile() {
            SaveFileDialog      saveDlg;
            Stream              stream;
            StreamWriter        writer;
            MessageBoxResult    eResult;
            
            saveDlg = new SaveFileDialog();
            saveDlg.AddExtension        = true;
            saveDlg.CheckPathExists     = true;
            saveDlg.DefaultExt          = "pgn";
            saveDlg.Filter              = "PGN Chess Files (*.pgn)|*.pgn";
            saveDlg.OverwritePrompt     = true;
            if (saveDlg.ShowDialog() == true) {
                if (m_board.MovePosStack.PositionInList + 1 != m_board.MovePosStack.List.Count) {
                    eResult = MessageBox.Show("Do you want to save the undone moves?", "Saving to PGN File", MessageBoxButton.YesNoCancel);
                } else {
                    eResult = MessageBoxResult.Yes;
                }
                if (eResult != MessageBoxResult.Cancel) {
                    try {
                        stream = saveDlg.OpenFile();
                    } catch(System.Exception) {
                        MessageBox.Show("Unable to open the file - " + saveDlg.FileName);
                        stream = null;
                    }
                    if (stream != null) {
                        writer = new StreamWriter(stream, Encoding.GetEncoding(1252));
                        try {
                            using (writer) {
                                
                                IsDirty = false;
                            }
                        } catch(SystemException) {
                            MessageBox.Show("Unable to write to the file '" + saveDlg.FileName + "'.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save the board to a file selected by the user in PGN format
        /// </summary>
        public void SaveSnapshot() {
            SaveFileDialog  saveDlg;
            string          strSnapshot;
            
            saveDlg                     = new SaveFileDialog();
            saveDlg.AddExtension        = true;
            saveDlg.CheckPathExists     = true;
            saveDlg.DefaultExt          = "pgn";
            saveDlg.Filter              = "Debugging Snapshot File (*.cbsnp)|*.cbsnp";
            saveDlg.OverwritePrompt     = true;
            if (saveDlg.ShowDialog() == true) {
                strSnapshot = TakeSnapshot();
                try {
                    System.IO.File.WriteAllText(saveDlg.FileName, strSnapshot, Encoding.Unicode);
                } catch(SystemException) {
                    MessageBox.Show("Unable to write to the file '" + saveDlg.FileName + "'.");
                }
            }
        }

        /// <summary>
        /// Create a new game using the specified list of moves
        /// </summary>
        /// <param name="chessBoardStarting">   Starting board or null if standard board</param>
        /// <param name="listMove">             List of moves</param>
        /// <param name="eNextMoveColor">       Color starting to play</param>
        /// <param name="strWhitePlayerName">   Name of the player playing white pieces</param>
        /// <param name="strBlackPlayerName">   Name of the player playing black pieces</param>
        
        /// <param name="spanPlayerWhite">      Timer for white</param>
        /// <param name="spanPlayerBlack">      Timer for black</param>
        public virtual void CreateGameFromMove(ChessBoard           chessBoardStarting,
                                               List<MoveExt>        listMove,
                                               ChessBoard.PlayerE   eNextMoveColor,
                                               string               strWhitePlayerName,
                                               string               strBlackPlayerName,
                                              
                                               TimeSpan             spanPlayerWhite,
                                               TimeSpan             spanPlayerBlack) {
            m_board.CreateGameFromMove(chessBoardStarting,
                                       listMove,
                                       eNextMoveColor);
            OnBoardReset(EventArgs.Empty);
            WhitePlayerName = strWhitePlayerName;
            BlackPlayerName = strBlackPlayerName;
            
            OnUpdateCmdState(System.EventArgs.Empty);
            m_gameTimer.ResetTo(m_board.CurrentPlayer,
                                spanPlayerWhite.Ticks,
                                spanPlayerBlack.Ticks);
            m_gameTimer.Enabled = true;
            IsDirty             = false;
            Refresh(false); // bForceRefresh
        }

        

        
         


        

        /// <summary>
        /// Set the piece in a case. Can only be used in design mode.
        /// </summary>
        public void SetCaseValue(int iPos, ChessBoard.PieceE ePiece) {

            if (BoardDesignMode) {
                m_board[iPos] = ePiece;
                RefreshCell(iPos);
            }
        }

        /// <summary>
        /// Find a move from the opening book
        /// </summary>
        /// <param name="book"> Book</param>
        /// <param name="move"> Found move</param>
        /// <returns>
        /// true if succeed, false if no move found in book
        /// </returns>
        public bool FindBookMove(Book book, out Move move) {
            bool        bRetVal;
            MoveExt[]   arrMoves;
            
            if (!m_board.StandardInitialBoard) {
                move.OriginalPiece  = ChessBoard.PieceE.None;
                move.StartPos       = 255;
                move.EndPos         = 255;
                move.Type           = Move.TypeE.Normal;
                bRetVal             = false;
            } else {
                arrMoves = MoveList;
                bRetVal  = m_board.FindBookMove(book, SearchMode, m_board.CurrentPlayer, arrMoves, out move);
            }
            return(bRetVal);
        }

        /// <summary>
        /// Call back the original action after update the move time counter
        /// </summary>
        /// <typeparam name="T">            Type of the original cookie</typeparam>
        /// <param name="cookieCallBack">   Call back cookie</param>
        /// <param name="move">             Found move</param>
        private void FindBestMoveEnd<T>(FindBestMoveCookie<T> cookieCallBack, MoveExt move) {

            if (move != null) {
                move.TimeToCompute = DateTime.Now - cookieCallBack.m_dtStartFinding;
            }
            OnFindMoveEnd(EventArgs.Empty);
            cookieCallBack.m_oriAction(cookieCallBack.m_oriCookie, move);
        }

        
       

        /// <summary>
        /// Called when the best move routine is done
        /// </summary>
        /// <param name="cookieCallBack">   Call back cookie</param>
        /// <param name="move">             Found move</param>
        private void ShowHintEnd(FindBestMoveCookie<bool> cookieCallBack, MoveExt move) {
            if (move != null) {
                move.TimeToCompute = DateTime.Now - cookieCallBack.m_dtStartFinding;
            }
            cookieCallBack.m_oriAction(true, move);
            ShowBeforeMove(move, true);
            m_board.DoMoveNoLog(move.Move);
            ShowAfterMove(move, true);
            m_board.UndoMoveNoLog(move.Move);
            ShowAfterMove(move, false);
            cookieCallBack.m_oriAction(false, move);
        }

       

        /// <summary>
        /// Cancel search
        /// </summary>
        public void CancelSearch() {
            m_board.CancelSearch();
        }

        /// <summary>
        /// Search trace
        /// </summary>
        /// <param name="iDepth">       Search depth</param>
        /// <param name="ePlayerColor"> Color who play</param>
        /// <param name="movePos">      Move position</param>
        /// <param name="iPts">         Points</param>
        public void TraceSearch(int iDepth, ChessBoard.PlayerE ePlayerColor, Move movePos, int iPts) {
            //
        }

        /// <summary>
        /// Gets the position express in a human form
        /// </summary>
        /// <param name="ptStart">      Starting Position</param>
        /// <param name="ptEnd">        Ending position</param>
        /// <returns>
        /// Human form position
        /// </returns>
        static public string GetHumanPos(IntPoint ptStart, IntPoint ptEnd) {
            return(ChessBoard.GetHumanPos(ptStart.X + (ptStart.Y << 3)) + "-" + ChessBoard.GetHumanPos(ptEnd.X + (ptEnd.Y << 3)));
        }

        /// <summary>
        /// Gets the cell position from a mouse event
        /// </summary>
        /// <param name="e">        Mouse event argument</param>
        /// <param name="ptCell">   Resulting cell</param>
        /// <returns>
        /// true if succeed, false if mouse don't point to a cell
        /// </returns>
        public bool GetCellFromPoint(MouseEventArgs e, out IntPoint ptCell) {
            bool        bRetVal;
            Point       pt;
            int         iCol;
            int         iRow;
            double      dActualWidth;
            double      dActualHeight;

            pt              = e.GetPosition(CellContainer);
            dActualHeight   = CellContainer.ActualHeight;
            dActualWidth    = CellContainer.ActualWidth;
            iCol            = (int)(pt.X * 8 / dActualWidth);
            iRow            = (int)(pt.Y * 8 / dActualHeight);
            if (iCol >= 0 && iCol < 8 && iRow >= 0 && iRow < 8) {
                ptCell  = new IntPoint(7 - iCol, 7 - iRow);
                bRetVal = true;
            } else {
                ptCell  = new IntPoint(-1, -1);
                bRetVal = false;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Flash the specified cell
        /// </summary>
        /// <param name="ptCell">   Cell to flash</param>
        public void FlashCell(IntPoint ptCell) {
            int             iCellPos;
            Border          border;
            Brush           brush;
            Color           colorStart;
            Color           colorEnd;
            SyncFlash       syncFlash;
            
            m_iBusy++;  // When flashing, a message loop is processed which can cause reentrance problem
            try { 
                iCellPos = ptCell.X + ptCell.Y * 8;
                if (((ptCell.X + ptCell.Y) & 1) != 0) {
                    colorStart  = DarkCellColor;    // m_colorInfo.m_colDarkCase;
                    colorEnd    = LiteCellColor;    // m_colorInfo.m_colLiteCase;
                } else {
                    colorStart  = LiteCellColor;    // m_colorInfo.m_colLiteCase;
                    colorEnd    = DarkCellColor;    // m_colorInfo.m_colDarkCase;
                }
                border                          = m_arrBorder[iCellPos];
                brush                           = border.Background.Clone();
                border.Background               = brush;
                syncFlash                       = new SyncFlash(this, brush as SolidColorBrush, colorStart, colorEnd);
                syncFlash.Flash();
            } finally {
                m_iBusy--;
            }
        }

        /// <summary>
        /// Flash the specified cell
        /// </summary>
        /// <param name="iStartPos">    Cell position</param>
        private void FlashCell(int iStartPos) {
            IntPoint    pt;

            pt  = new IntPoint(iStartPos & 7, iStartPos / 8);
            FlashCell(pt);
        }

        /// <summary>
        /// Get additional position to update when doing or undoing a special move
        /// </summary>
        /// <param name="movePos">  Position of the move</param>
        /// <returns>
        /// Array of position to undo
        /// </returns>
        private int[] GetPosToUpdate(Move movePos) {
            List<int>   arrRetVal = new List<int>(2);

            if ((movePos.Type & Move.TypeE.MoveTypeMask) == Move.TypeE.Castle) {
                switch(movePos.EndPos) {
                case 1:
                    arrRetVal.Add(0);
                    arrRetVal.Add(2);
                    break;
                case 5:
                    arrRetVal.Add(7);
                    arrRetVal.Add(4);
                    break;
                case 57:
                    arrRetVal.Add(56);
                    arrRetVal.Add(58);
                    break;
                case 61:
                    arrRetVal.Add(63);
                    arrRetVal.Add(60);
                    break;
                default:
                    MessageBox.Show("Oops!");
                    break;
                }
            } else if ((movePos.Type & Move.TypeE.MoveTypeMask) == Move.TypeE.EnPassant) {
                arrRetVal.Add((movePos.StartPos & 56) + (movePos.EndPos & 7));
            }
            return(arrRetVal.ToArray());
        }

        /// <summary>
        /// Show before move is done
        /// </summary>
        /// <param name="movePos">      Position of the move</param>
        /// <param name="bFlash">       true to flash the from and destination pieces</param>
        private void ShowBeforeMove(MoveExt movePos, bool bFlash) {
            if (bFlash) {
                FlashCell(movePos.Move.StartPos);
            }
        }

        /// <summary>
        /// Show after move is done
        /// </summary>
        /// <param name="movePos">      Position of the move</param>
        /// <param name="bFlash">       true to flash the from and destination pieces</param>
        private void ShowAfterMove(MoveExt movePos, bool bFlash) {
            int[]       arrPosToUpdate;

            RefreshCell(movePos.Move.StartPos);
            RefreshCell(movePos.Move.EndPos);
            if (bFlash) {
                FlashCell(movePos.Move.EndPos);
            }
            arrPosToUpdate = GetPosToUpdate(movePos.Move);
            foreach (int iPos in arrPosToUpdate) {
                if (bFlash) {
                    FlashCell(iPos);
                }
                RefreshCell(iPos);
            }
        }

        /// <summary>
        /// Play the specified move
        /// </summary>
        /// <param name="move">         Position of the move</param>
        /// <param name="bFlashing">    true to flash when doing the move</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Tie, Check, Mate
        /// </returns>
        public ChessBoard.GameResultE DoMove(MoveExt move, bool bFlashing) {
            ChessBoard.GameResultE  eRetVal;

            if (m_iBusy != 0) { 
                throw new MethodAccessException(m_strCtlIsBusy);
            }
            if (!m_board.IsMoveValid(move.Move)) {
                throw new ArgumentException("Try to make an illegal move");
            }
            m_signalActionDone.Reset();
            ShowBeforeMove(move, bFlashing);
            eRetVal = m_board.DoMove(move);
            ShowAfterMove(move, bFlashing);
            OnNewMove(new NewMoveEventArgs(move, eRetVal));
            OnUpdateCmdState(System.EventArgs.Empty);
            m_gameTimer.PlayerColor = m_board.CurrentPlayer;
            m_gameTimer.Enabled     = (eRetVal == ChessBoard.GameResultE.OnGoing || eRetVal == ChessBoard.GameResultE.Check);
            m_signalActionDone.Set();
            return(eRetVal);
        }

        /// <summary>
        /// Play the specified move
        /// </summary>
        /// <param name="move">         Position of the move</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Tie, Check, Mate
        /// </returns>
        public ChessBoard.GameResultE DoMove(MoveExt move) {
            bool    bFlashing;

            bFlashing = MoveFlashing;
            return(DoMove(move, bFlashing));
        }

        /// <summary>
        /// Play the specified move
        /// </summary>
        /// <param name="move">         Position of the move</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Tie, Check, Mate
        /// </returns>
        public ChessBoard.GameResultE DoUserMove(MoveExt move) {
            ChessBoard.GameResultE  eRetVal;

            eRetVal = DoMove(move);
            IsDirty = true;
            return(eRetVal);
        }

        /// <summary>
        /// Undo the last move
        /// </summary>
        /// <param name="bPlayerAgainstPlayer"> true if player against player</param>
        /// <param name="bFlashing">            true to flash the from and destination pieces</param>
        private void UndoMove(bool bPlayerAgainstPlayer, bool bFlashing) {
            MoveExt move;
            int[]   arrPosToUpdate;
            int     iCount;

            if (m_iBusy != 0) { 
                throw new MethodAccessException(m_strCtlIsBusy);
            }
            iCount = bPlayerAgainstPlayer ? 1 : 2;
            if (iCount <= UndoCount) {
                for (int iIndex = 0; iIndex < iCount; iIndex++) {
                    move = m_board.MovePosStack.CurrentMove;
                    if (bFlashing) {
                        FlashCell(move.Move.EndPos);
                    }
                    m_board.UndoMove();
                    RefreshCell(move.Move.EndPos);
                    RefreshCell(move.Move.StartPos);
                    if (bFlashing) {
                        FlashCell(move.Move.StartPos);
                    }
                    arrPosToUpdate = GetPosToUpdate(move.Move);
                    Array.Reverse(arrPosToUpdate);
                    foreach (int iPos in arrPosToUpdate) {
                        if (bFlashing) {
                            FlashCell(iPos);
                        }
                        RefreshCell(iPos);
                    }
                    OnRedoPosChanged(EventArgs.Empty);
                    OnUpdateCmdState(System.EventArgs.Empty);
                    m_gameTimer.PlayerColor = m_board.CurrentPlayer;
                    m_gameTimer.Enabled     = true;
                }
            }
        }

        /// <summary>
        /// Undo the last move
        /// </summary>
        /// <param name="bPlayerAgainstPlayer"> true if player against player</param>
        /// <param name="eComputer">            Color played by the computer if any</param>
        public void UndoMove(bool bPlayerAgainstPlayer, ChessBoard.PlayerE eComputer) {
            bool    bFlashing;

            if (!bPlayerAgainstPlayer && eComputer == NextMoveColor) {
                bPlayerAgainstPlayer = true;
            }
            m_signalActionDone.Reset();
            bFlashing = MoveFlashing;
            UndoMove(bPlayerAgainstPlayer, bFlashing);
            m_signalActionDone.Set();
        }

        /// <summary>
        /// Redo the most recently undone move
        /// </summary>
        /// <param name="bPlayerAgainstPlayer"> true if player against player</param>
        /// <param name="bFlashing">            true to flash while doing the move</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Check, Mate
        /// </returns>
        private ChessBoard.GameResultE RedoMove(bool bPlayerAgainstPlayer, bool bFlashing) {
            ChessBoard.GameResultE  eRetVal = SrcChess2.ChessBoard.GameResultE.OnGoing;
            MoveExt                 move;
            int                     iCount;
            int                     iRedoCount;

            if (m_iBusy != 0) { 
                throw new MethodAccessException(m_strCtlIsBusy);
            }
            iCount      = bPlayerAgainstPlayer ? 1 : 2;
            iRedoCount  = RedoCount;
            if (iCount > iRedoCount) {
                iCount = iRedoCount;
            }
            for (int iIndex = 0; iIndex < iCount; iIndex++) {
                move    = m_board.MovePosStack.NextMove;
                ShowBeforeMove(move, bFlashing);
                eRetVal = m_board.RedoMove();
                ShowAfterMove(move, bFlashing);
                OnRedoPosChanged(EventArgs.Empty);
                OnUpdateCmdState(System.EventArgs.Empty);
                m_gameTimer.PlayerColor = m_board.CurrentPlayer;
                m_gameTimer.Enabled     = (eRetVal == ChessBoard.GameResultE.OnGoing || eRetVal == ChessBoard.GameResultE.Check);
            }
            return(eRetVal);
        }

        /// <summary>
        /// Redo the most recently undone move
        /// </summary>
        /// <param name="bPlayerAgainstPlayer"> true if player against player</param>
        /// <returns>
        /// NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Check, Mate
        /// </returns>
        public ChessBoard.GameResultE RedoMove(bool bPlayerAgainstPlayer) {
            ChessBoard.GameResultE  eRetVal;
            bool                    bFlashing;

            m_signalActionDone.Reset();
            bFlashing = MoveFlashing;
            eRetVal   = RedoMove(bPlayerAgainstPlayer, bFlashing);
            m_signalActionDone.Set();
            return(eRetVal);
        }

        /// <summary>
        /// Select a move by index using undo/redo buffer to move
        /// </summary>
        /// <param name="iIndex">   Index of the move. Can be -1</param>
        /// <param name="bSucceed"> true if index in range</param>
        /// <returns>
        /// Repeat result
        /// </returns>
        public ChessBoard.GameResultE SelectMove(int iIndex, out bool bSucceed) {
            ChessBoard.GameResultE  eRetVal = ChessBoard.GameResultE.OnGoing;
            int                     iCurPos;
            int                     iCount;

            if (m_iBusy != 0) {
                throw new MethodAccessException(m_strCtlIsBusy);
            }
            m_signalActionDone.Reset();
            iCurPos = m_board.MovePosStack.PositionInList;
            iCount  = m_board.MovePosStack.Count;
            if (iIndex >= -1 && iIndex < iCount) {
                bSucceed = true;
                if (iCurPos < iIndex) {
                    while (iCurPos != iIndex) {
                        eRetVal = RedoMove(true /*bPlayerAgainstPlayer*/, false /*bFlashing*/);
                        iCurPos++;
                    }
                } else if (iCurPos > iIndex) {
                    while (iCurPos != iIndex) {
                        UndoMove(true /*bPlayerAgainstPlayer*/, false /*bFlashing*/);
                        iCurPos--;
                    }
                }
            } else {
                bSucceed = false;
            }
            m_signalActionDone.Set();
            return(eRetVal);
        }

        /// <summary>
        /// Intercept Mouse click
        /// </summary>
        /// <param name="e">    Event Parameter</param>
        protected override void OnMouseDown(MouseButtonEventArgs e) {
            IntPoint                        pt;
            Move                            tMove;
            ChessBoard.ValidPawnPromotionE  eValidPawnPromotion;
            QueryPieceEventArgs             eQueryPieceEventArgs;
            int                             iPos;
            ChessBoard.PieceE               ePiece;
            QueryPawnPromotionTypeEventArgs eventArg;
            bool                            bWhiteToMove;
            bool                            bWhitePiece;
            
            base.OnMouseDown(e);
            if (m_iBusy == 0 && !IsSearchEngineBusy && !IsObservingAGame) {
                if (BoardDesignMode) {
                    if (GetCellFromPoint(e, out pt)) {
                        iPos                 = pt.X + (pt.Y << 3);
                        eQueryPieceEventArgs = new QueryPieceEventArgs(iPos, ChessBoard[iPos]);
                        OnQueryPiece(eQueryPieceEventArgs);
                        ChessBoard[iPos]    = eQueryPieceEventArgs.Piece;
                        RefreshCell(iPos);
                    }
                } else if (AutoSelection) {
                    if (GetCellFromPoint(e, out pt)) {
                        iPos = pt.X + (pt.Y << 3);
                        if (SelectedCell.X == -1 || SelectedCell.Y == -1) {
                            ePiece          = m_board[iPos];
                            bWhiteToMove    = (m_board.CurrentPlayer == SrcChess2.ChessBoard.PlayerE.White);
                            bWhitePiece     = (ePiece & SrcChess2.ChessBoard.PieceE.Black) == 0;
                            if (ePiece != SrcChess2.ChessBoard.PieceE.None && bWhiteToMove == bWhitePiece) {
                                SelectedCell = pt;
                            } else {
                                System.Console.Beep();
                            }
                        } else {
                            if (SelectedCell.X == pt.X  && SelectedCell.Y == pt.Y) {
                                SelectedCell = new IntPoint(-1, -1);
                            } else {
                                tMove = ChessBoard.FindIfValid(m_board.CurrentPlayer,
                                                               SelectedCell.X + (SelectedCell.Y << 3),
                                                               iPos);
                                if (tMove.StartPos != 255) {                                                           
                                    eValidPawnPromotion = ChessBoard.FindValidPawnPromotion(m_board.CurrentPlayer, 
                                                                                            SelectedCell.X + (SelectedCell.Y << 3),
                                                                                            iPos);
                                    if (eValidPawnPromotion != ChessBoard.ValidPawnPromotionE.None) {
                                        eventArg = new QueryPawnPromotionTypeEventArgs(eValidPawnPromotion);
                                        OnQueryPawnPromotionType(eventArg);
                                        if (eventArg.PawnPromotionType == Move.TypeE.Normal) {
                                            tMove.StartPos = 255;
                                        } else {
                                            tMove.Type &= ~Move.TypeE.MoveTypeMask;
                                            tMove.Type |= eventArg.PawnPromotionType;
                                        }
                                    }
                                }
                                SelectedCell = new IntPoint(-1, -1);
                                if (tMove.StartPos == 255) {
                                    System.Console.Beep();
                                } else {
                                    OnMoveSelected(new MoveSelectedEventArgs(new MoveExt(tMove)));
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

    } // Class ChessBoardControl
} // Namespace
