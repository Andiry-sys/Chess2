using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;
using System.Windows.Media;

namespace SrcChess2 {
    /// <summary>
    /// Show a list of lost pieces
    /// </summary>
    public partial class LostPiecesControl : UserControl {
        /// <summary>Array of frame containing the piece visual</summary>
        private Border[]                m_arrBorder;
        /// <summary>Array containining the pieces</summary>
        private ChessBoard.PieceE[]     m_arrPiece;
        /// <summary>Chess Board Control</summary>
        private ChessBoardControl       m_chessBoardCtl;
        /// <summary>Piece Set to use to show the pieces</summary>
        private PieceSet                m_pieceSet;
        /// <summary>true if in design mode. In design mode, One of each possible pieces is shown and one can be selected.</summary>
        private bool                    m_bDesignMode;
        /// <summary>Piece currently selected in design mode.</summary>
        private int                     m_iSelectedPiece;
        /// <summary>Color being displayed. false = White, true = Black</summary>
        public bool                     Color { get; set; }
        
        /// <summary>
        /// Class Ctor
        /// </summary>
        public LostPiecesControl() {
            Border   border;

            InitializeComponent();
            m_iSelectedPiece    = -1;
            m_arrBorder         = new Border[16];
            m_arrPiece          = new ChessBoard.PieceE[16];
            for (int iIndex = 0; iIndex < 16; iIndex++) {
                border                  = new Border();
                border.Margin           = new Thickness(1);
                border.BorderThickness  = new Thickness(1);
                border.BorderBrush      = Background;
                m_arrBorder[iIndex]     = border;
                m_arrPiece[iIndex]      = ChessBoard.PieceE.None;
                CellContainer.Children.Add(border);
            }
        }

        /// <summary>
        /// Enumerate the pieces which must be shown in the control
        /// </summary>
        /// <returns>
        /// Array of pieces
        /// </returns>
        private ChessBoard.PieceE[] EnumPiece() {
            ChessBoard.PieceE[]     arrPieces;
            ChessBoard.PieceE[]     arrPossiblePiece;
            ChessBoard.PieceE       ePiece;
            int                     iEated;
            int                     iPos;
            
            arrPieces           = new ChessBoard.PieceE[16];
            for (int i = 0; i < 16; i++) {
                arrPieces[i]    = ChessBoard.PieceE.None;
            }
            arrPossiblePiece    = new ChessBoard.PieceE[] { ChessBoard.PieceE.King,
                                                            ChessBoard.PieceE.Queen,
                                                            ChessBoard.PieceE.Rook,
                                                            ChessBoard.PieceE.Bishop,
                                                            ChessBoard.PieceE.Knight,
                                                            ChessBoard.PieceE.Pawn };
            iPos    = 0;
            if (m_bDesignMode) {
                iPos++;
            }
            foreach (ChessBoard.PieceE ePossiblePiece in arrPossiblePiece) {
                if (m_bDesignMode) {
                    ePiece              = ePossiblePiece;
                    arrPieces[iPos++]   = ePiece;
                    ePiece             |= ChessBoard.PieceE.Black;
                    arrPieces[iPos++]   = ePiece;
                } else {                    
                    ePiece = ePossiblePiece;
                    if (Color) {
                        ePiece |= ChessBoard.PieceE.Black;
                    }
                    iEated = m_chessBoardCtl.ChessBoard.GetEatedPieceCount(ePiece);
                    for (int iIndex = 0; iIndex < iEated; iIndex++) {
                        arrPieces[iPos++] = ePiece;
                    }
                }
            }
            return(arrPieces);
        }

        /// <summary>
        /// Make the grid square
        /// </summary>
        /// <param name="size"> User control size</param>
        private Size MakeSquare(Size size) {
            double  dMinSize;

            dMinSize    = (size.Width < size.Height) ? size.Width : size.Height;
            size        = new Size(dMinSize, dMinSize);
            return(size);
        }
        
        /// <summary>
        /// Called when the Measure() method is called
        /// </summary>
        /// <param name="constraint">   Size constraint</param>
        /// <returns>
        /// Control size
        /// </returns>
        protected override Size MeasureOverride(Size constraint) {
            constraint = MakeSquare(constraint);
 	        return base.MeasureOverride(constraint);
        }

        /// <summary>
        /// Set the chess piece control
        /// </summary>
        /// <param name="iPos">         Piece position</param>
        /// <param name="ePiece">       Piece</param>
        private void SetPieceControl(int iPos, ChessBoard.PieceE ePiece) {
            Border      border;
            Control     controlPiece;
            Label       label;

            border              = m_arrBorder[iPos];
            controlPiece        = m_pieceSet[ePiece];
            if (controlPiece != null) {
                controlPiece.Margin = new Thickness(1);
            }
            m_arrPiece[iPos]    = ePiece;
            if (controlPiece == null) { // && m_bDesignMode) {
                label                               = new Label();
                label.Content                       = " ";
                label.FontSize                      = 0.1;
                controlPiece                        = label;
                controlPiece.HorizontalAlignment    = System.Windows.HorizontalAlignment.Stretch;
                controlPiece.VerticalAlignment      = System.Windows.VerticalAlignment.Stretch;
                //controlPiece.Background             = Brushes.Red;
            }
            border.Child = controlPiece;
        }

        /// <summary>
        /// Refresh the specified cell
        /// </summary>
        /// <param name="arrNewPieces"> New pieces value</param>
        /// <param name="iPos">         Piece position</param>
        /// <param name="bFullRefresh"> true to refresh even if its the same piece</param>
        private void RefreshCell(ChessBoard.PieceE[] arrNewPieces, int iPos, bool bFullRefresh) {
            ChessBoard.PieceE   ePiece;

            ePiece = arrNewPieces[iPos];
            if (ePiece != m_arrPiece[iPos] || bFullRefresh) {
                SetPieceControl(iPos, ePiece);
            }
        }

        /// <summary>
        /// Refresh the board
        /// </summary>
        /// <param name="bFullRefresh"> Refresh even if its the same piece</param>
        private void Refresh(bool bFullRefresh) {
            ChessBoard          chessBoard;
            ChessBoard.PieceE[] arrNewPieces;

            if (m_chessBoardCtl != null && m_chessBoardCtl.ChessBoard != null && m_pieceSet != null) {
                arrNewPieces    = EnumPiece();
                chessBoard      = m_chessBoardCtl.ChessBoard;
                if (chessBoard != null) {
                    for (int iPos = 0; iPos < 16; iPos++) {
                        RefreshCell(arrNewPieces, iPos, bFullRefresh);
                    }
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
        /// Chess Board Control associate with this control
        /// </summary>
        public ChessBoardControl ChessBoardControl {
            get {
                return(m_chessBoardCtl);
            }
            set {
                if (m_chessBoardCtl != value) {
                    m_chessBoardCtl = value;
                    Refresh(false); // bForceRefresh
                }
            }
        }

        /// <summary>
        /// Piece Set use to draw the visual pieces
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
        /// Select a piece (in design mode only)
        /// </summary>
        public int SelectedIndex {
            get {
                return(m_iSelectedPiece);
            }
            set {
                if (m_iSelectedPiece != value) {
                    if (value >= 0 && value < 13) {
                        if (m_iSelectedPiece != -1) {
                            m_arrBorder[m_iSelectedPiece].BorderBrush   = Background;
                        }
                        m_iSelectedPiece = value;
                        if (m_iSelectedPiece != -1) {
                            m_arrBorder[m_iSelectedPiece].BorderBrush   = MainBorder.BorderBrush;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the currently selected piece
        /// </summary>
        public ChessBoard.PieceE SelectedPiece {
            get {
                ChessBoard.PieceE   eRetVal = ChessBoard.PieceE.None;
                int                 iSelectedIndex;
                
                iSelectedIndex = SelectedIndex;
                if (iSelectedIndex > 0 && iSelectedIndex < 13) {
                    iSelectedIndex--;
                    if ((iSelectedIndex & 1) != 0) {
                        eRetVal |= ChessBoard.PieceE.Black;
                    }
                    iSelectedIndex >>= 1;
                    switch(iSelectedIndex) {
                    case 0:
                        eRetVal |= ChessBoard.PieceE.King;
                        break;
                    case 1:
                        eRetVal |= ChessBoard.PieceE.Queen;
                        break;
                    case 2:
                        eRetVal |= ChessBoard.PieceE.Rook;
                        break;
                    case 3:
                        eRetVal |= ChessBoard.PieceE.Bishop;
                        break;
                    case 4:
                        eRetVal |= ChessBoard.PieceE.Knight;
                        break;
                    case 5:
                        eRetVal |= ChessBoard.PieceE.Pawn;
                        break;
                    default:
                        eRetVal = ChessBoard.PieceE.None;
                        break;
                    }
                }
                return(eRetVal);
            }
        }

        /// <summary>
        /// Select the design mode
        /// </summary>
        public bool BoardDesignMode {
            get {
                return(m_bDesignMode);
            }
            set {
                if (m_bDesignMode != value) {
                    SelectedIndex = -1;
                    m_bDesignMode = value;
                    Refresh(false); // bForceRefresh
                    if (m_bDesignMode) {
                        SelectedIndex   = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Called when one of the mouse button is release
        /// </summary>
        /// <param name="e">        Event</param>
        protected override void OnMouseUp(MouseButtonEventArgs e) {
            Point   pt;
            int     iRowPos;
            int     iColPos;
            int     iPos;

            base.OnMouseUp(e);
            if (m_bDesignMode) {
                pt      = e.GetPosition(this);
                iRowPos = (int)(pt.Y * 4 / ActualHeight);
                iColPos = (int)(pt.X * 4 / ActualWidth);
                if (iRowPos >= 0 && iRowPos < 4 && iColPos >= 0 && iColPos < 4) {
                    iPos            = (iRowPos << 2) + iColPos;
                    SelectedIndex   = (iPos < 13) ? iPos : 0;
                }
            }
        }
    } // Class LostPiecesControl
} // Namespace
