using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace SrcChess2 {
    /// <summary>Implementation of the chess board without any user interface.</summary>
    public sealed class ChessBoard : IXmlSerializable {
        
        /// <summary>Player color (black and white)</summary>
        public enum PlayerE {
            /// <summary>White player</summary>
            White   = 0,
            /// <summary>Black player</summary>
            Black   = 1
        }
        
        /// <summary>Same as PieceE, but easier serialization.</summary>
        public enum SerPieceE : byte {
            /// <summary>No piece</summary>
            Empty       = 0,
            /// <summary>Pawn</summary>
            WhitePawn   = 1,
            /// <summary>Knight</summary>
            WhiteKnight = 2,
            /// <summary>Bishop</summary>
            WhiteBishop = 3,
            /// <summary>Rook</summary>
            WhiteRook   = 4,
            /// <summary>Queen</summary>
            WhiteQueen  = 5,
            /// <summary>King</summary>
            WhiteKing   = 6,
            /// <summary>Not used</summary>
            NotUsed1    = 7,
            /// <summary>Not used</summary>
            NotUsed2    = 8,
            /// <summary>Pawn</summary>
            BlackPawn   = 9,
            /// <summary>Knight</summary>
            BlackKnight = 10,
            /// <summary>Bishop</summary>
            BlackBishop = 11,
            /// <summary>Rook</summary>
            BlackRook   = 12,
            /// <summary>Queen</summary>
            BlackQueen  = 13,
            /// <summary>King</summary>
            BlackKing   = 14,
            /// <summary>Not used</summary>
            NotUsed3    = 15,
        }
        
        /// <summary>Value of each piece on the board. Each piece is a combination of piece value and color (0 for white, 8 for black)</summary>
        [Flags]
        public enum PieceE : byte {
            /// <summary>No piece</summary>
            None      = 0,
            /// <summary>Pawn</summary>
            Pawn      = 1,
            /// <summary>Knight</summary>
            Knight    = 2,
            /// <summary>Bishop</summary>
            Bishop    = 3,
            /// <summary>Rook</summary>
            Rook      = 4,
            /// <summary>Queen</summary>
            Queen     = 5,
            /// <summary>King</summary>
            King      = 6,
            /// <summary>Mask to find the piece</summary>
            PieceMask = 7,
            /// <summary>Piece is black</summary>
            Black     = 8,
            /// <summary>White piece</summary>
            White     = 0,
        }
        
        /// <summary>List of valid pawn promotion</summary>
        [Flags]
        public enum ValidPawnPromotionE {
            /// <summary>No valid promotion</summary>
            None    =   0,
            /// <summary>Promotion to queen</summary>
            Queen   =   1,
            /// <summary>Promotion to rook</summary>
            Rook    =   2,
            /// <summary>Promotion to bishop</summary>
            Bishop  =   4,
            /// <summary>Promotion to knight</summary>
            Knight  =   8,
            /// <summary>Promotion to pawn</summary>
            Pawn    =   16
        };

        /// <summary>Mask for board extra info</summary>
        [Flags]
        public enum BoardStateMaskE {
            /// <summary>0-63 to express the EnPassant possible position</summary>
            EnPassant       =   63,
            /// <summary>black player is next to move</summary>
            BlackToMove     =   64,
            /// <summary>white left castling is possible</summary>
            WLCastling      =   128,
            /// <summary>white right castling is possible</summary>
            WRCastling      =   256,
            /// <summary>black left castling is possible</summary>
            BLCastling      =   512,
            /// <summary>black right castling is possible</summary>
            BRCastling      =   1024,
            /// <summary>Mask use to save the number of times the board has been repeated</summary>
            BoardRepMask    =   2048+4096+8192
        };

        /// <summary>Any repetition causing a draw?</summary>
        public enum RepeatResultE {
            /// <summary>No repetition found</summary>
            NoRepeat,
            /// <summary>3 times the same board</summary>
            ThreeFoldRepeat,
            /// <summary>50 times without moving a pawn or eating a piece</summary>
            FiftyRuleRepeat
        };
        
        /// <summary>Result of the current board. Game is finished unless OnGoing or Check</summary>
        public enum GameResultE {
            /// <summary>Game is going on</summary>
            OnGoing,
            /// <summary>3 times the same board</summary>
            ThreeFoldRepeat,
            /// <summary>50 times without moving a pawn or eating a piece</summary>
            FiftyRuleRepeat,
            /// <summary>No more move for the next player</summary>
            TieNoMove,
            /// <summary>Not enough pieces to do a check mate</summary>
            TieNoMatePossible,
            /// <summary>Check</summary>
            Check,
            /// <summary>Checkmate</summary>
            Mate
        }

        /// <summary>
        /// Position information. Positive value for white player, negative value for black player.
        /// All these informations are computed before the last move to improve performance.
        /// </summary>
        public struct PosInfoS {
            /// <summary>
            /// Class Ctor
            /// </summary>
            /// <param name="iAttackedPieces">  Number of pieces attacking this position</param>
            /// <param name="iPiecesDefending"> Number of pieces defending this position</param>
            public PosInfoS(int iAttackedPieces, int  iPiecesDefending) { m_iAttackedPieces = iAttackedPieces; m_iPiecesDefending = iPiecesDefending; }
            /// <summary>Number of pieces being attacked by player's pieces</summary>
            public int  m_iAttackedPieces;
            /// <summary>Number of pieces defending player's pieces</summary>
            public int  m_iPiecesDefending;
        }
        
        /// <summary>NULL position info</summary>
        public static readonly ChessBoard.PosInfoS  s_posInfoNull = new ChessBoard.PosInfoS(0, 0);
        /// <summary>Possible diagonal or linear moves for each board position</summary>
        static private int[][][]                    s_pppiCaseMoveDiagLine;
        /// <summary>Possible diagonal moves for each board position</summary>
        static private int[][][]                    s_pppiCaseMoveDiagonal;
        /// <summary>Possible linear moves for each board position</summary>
        static private int[][][]                    s_pppiCaseMoveLine;
        /// <summary>Possible knight moves for each board position</summary>
        static private int[][]                      s_ppiCaseMoveKnight;
        /// <summary>Possible king moves for each board position</summary>
        static private int[][]                      s_ppiCaseMoveKing;
        /// <summary>Possible board positions a black pawn can attack from each board position</summary>
        static private int[][]                      s_ppiCaseBlackPawnCanAttackFrom;
        /// <summary>Possible board positions a white pawn can attack from each board position</summary>
        static private int[][]                      s_ppiCaseWhitePawnCanAttackFrom;

        /// <summary>Chess board</summary>
        /// 63 62 61 60 59 58 57 56
        /// 55 54 53 52 51 50 49 48
        /// 47 46 45 44 43 42 41 40
        /// 39 38 37 36 35 34 33 32
        /// 31 30 29 28 27 26 25 24
        /// 23 22 21 20 19 18 17 16
        /// 15 14 13 12 11 10 9  8
        /// 7  6  5  4  3  2  1  0
        private PieceE[]                    m_pBoard;
        /// <summary>Position of the black king</summary>
        private int                         m_iBlackKingPos;
        /// <summary>Position of the white king</summary>
        private int                         m_iWhiteKingPos;
        /// <summary>Number of pieces of each kind/color</summary>
        private int[]                       m_piPiecesCount;
        /// <summary>Random number generator</summary>
        private Random                      m_rnd;
        /// <summary>Random number generator (repetitive, seed = 0)</summary>
        private Random                      m_rndRep;
        /// <summary>Number of time the right black rook has been moved. Used to determine if castle is possible</summary>
        private int                         m_iRBlackRookMoveCount;
        /// <summary>Number of time the left black rook has been moved. Used to determine if castle is possible</summary>
        private int                         m_iLBlackRookMoveCount;
        /// <summary>Number of time the black king has been moved. Used to determine if castle is possible</summary>
        private int                         m_iBlackKingMoveCount;
        /// <summary>Number of time the right white rook has been moved. Used to determine if castle is possible</summary>
        private int                         m_iRWhiteRookMoveCount;
        /// <summary>Number of time the left white rook has been moved. Used to determine if castle is possible</summary>
        private int                         m_iLWhiteRookMoveCount;
        /// <summary>Number of time the white king has been moved. Used to determine if castle is possible</summary>
        private int                         m_iWhiteKingMoveCount;
        /// <summary>White has castle if true</summary>
        private bool                        m_bWhiteCastle;
        /// <summary>Black has castle if true</summary>
        private bool                        m_bBlackCastle;
        /// <summary>Position behind the pawn which had just been moved from 2 positions</summary>
        private int                         m_iPossibleEnPassantAt;
        /// <summary>Stack of m_iPossibleEnPassantAt values</summary>
        private Stack<int>                  m_stackPossibleEnPassantAt;
        /// <summary>Current zobrist key value. Probably unique for the current board position</summary>
        private Int64                       m_i64ZobristKey;
        /// <summary>Object where to redirect the trace if any</summary>
        private SearchEngine.ITrace         m_trace;
        /// <summary>Move history use to handle the fifty-move rule and the threefold repetition rule.</summary>
        private MoveHistory                 m_moveHistory;
        /// <summary>The board is in design mode if true</summary>
        private bool                        m_bDesignMode;
        /// <summary>Stack of moves since the initial board</summary>
        private MovePosStack                m_moveStack;
        /// <summary>Player making the next move</summary>
        private PlayerE                     m_eCurrentPlayer;
        /// <summary>true if the initial board is the standard one</summary>
        private bool                        m_bStdInitialBoard;
        /// <summary>Information about pieces attack</summary>
        private PosInfoS                    m_posInfo;
        /// <summary>Opening book to use if any</summary>
        private Book                        m_book;

        /// <summary>
        /// Class static constructor. 
        /// Builds the list of possible moves for each piece type per position.
        /// Etablished the value of each type of piece for board evaluation.
        /// </summary>
        static ChessBoard() {
            List<int[]>     arrSquare;
            
            s_posInfoNull.m_iAttackedPieces     = 0;
            s_posInfoNull.m_iPiecesDefending    = 0;
            arrSquare                             = new List<int[]>(4);
            s_pppiCaseMoveDiagLine              = new int[64][][];
            s_pppiCaseMoveDiagonal              = new int[64][][];
            s_pppiCaseMoveLine                  = new int[64][][];
            s_ppiCaseMoveKnight                 = new int[64][];
            s_ppiCaseMoveKing                   = new int[64][];
            s_ppiCaseWhitePawnCanAttackFrom     = new int[64][];
            s_ppiCaseBlackPawnCanAttackFrom     = new int[64][];
            for (int iPos = 0; iPos < 64; iPos++) {
                GetAccessibleSquares(iPos, arrSquare, new int[] { -1, -1,  -1, 0,  -1, 1,  0, -1,  0, 1,  1, -1,  1, 0,  1, 1 }, true);
                s_pppiCaseMoveDiagLine[iPos] = arrSquare.ToArray();
                GetAccessibleSquares(iPos, arrSquare, new int[] { -1, -1,  -1, 1,  1, -1,  1, 1 }, true);
                s_pppiCaseMoveDiagonal[iPos] = arrSquare.ToArray();                
                GetAccessibleSquares(iPos, arrSquare, new int[] { -1, 0,  1, 0,  0, -1,  0, 1 }, true);
                s_pppiCaseMoveLine[iPos]     = arrSquare.ToArray();
                GetAccessibleSquares(iPos, arrSquare, new int[] { 1, 2,  1, -2,  2, -1,  2, 1,  -1, 2,  -1, -2,  -2, -1,  -2, 1}, false);
                s_ppiCaseMoveKnight[iPos]    = arrSquare[0];
                GetAccessibleSquares(iPos, arrSquare, new int[] { -1, -1,  -1, 0,  -1, 1,  0, -1,  0, 1,  1, -1,  1, 0,  1, 1 }, false);
                s_ppiCaseMoveKing[iPos]      = arrSquare[0];
                GetAccessibleSquares(iPos, arrSquare, new int[] { -1, -1, 1, -1 }, false);
                s_ppiCaseWhitePawnCanAttackFrom[iPos] = arrSquare[0];
                GetAccessibleSquares(iPos, arrSquare, new int[] { -1, 1,  1, 1 }, false);
                s_ppiCaseBlackPawnCanAttackFrom[iPos] = arrSquare[0];
            }
        }

        /// <summary>
        /// Get all squares which can be access by a specific piece positioned at iSquarePos
        /// </summary>
        /// <param name="iSquarePos">   Square position of the piece</param>
        /// <param name="arrSquare">    Array of square accessable by the piece</param>
        /// <param name="arrDelta">     List of delta (in tuple) used to list the accessible position</param>
        /// <param name="bRepeat">      True for Queen, Rook and Bishop. False for Knight, King and Pawn</param>
        static private void GetAccessibleSquares(int iSquarePos, List<int[]> arrSquare, int[] arrDelta, bool bRepeat) {
            int         iColPos;
            int         iRowPos;
            int         iColIndex;
            int         iRowIndex;
            int         iColDelta;
            int         iRowDelta;
            int         iPosOfs;
            int         iNewPos;
            List<int>   arrSquareOnLine;

            arrSquare.Clear();
            arrSquareOnLine = new List<int>(8);
            iColPos         = iSquarePos & 7;
            iRowPos         = iSquarePos >> 3;
            for (int iIndex = 0; iIndex < arrDelta.Length; iIndex += 2) {
                iColDelta   = arrDelta[iIndex];
                iRowDelta   = arrDelta[iIndex+1];
                iPosOfs     = (iRowDelta << 3) + iColDelta;
                iColIndex   = iColPos + iColDelta;
                iRowIndex   = iRowPos + iRowDelta;
                iNewPos     = iSquarePos + iPosOfs;
                if (bRepeat) {
                    arrSquareOnLine.Clear();
                    while (iColIndex >= 0 && iColIndex < 8 && iRowIndex >= 0 && iRowIndex < 8) {
                        arrSquareOnLine.Add(iNewPos);
                        iColIndex   += iColDelta;
                        iRowIndex   += iRowDelta;
                        iNewPos     += iPosOfs;
                    }
                    if (arrSquareOnLine.Count != 0) {
                        arrSquare.Add(arrSquareOnLine.ToArray());
                    }
                } else if (iColIndex >= 0 && iColIndex < 8 && iRowIndex >= 0 && iRowIndex < 8) {
                    arrSquareOnLine.Add(iNewPos);
                }
            }
            if (!bRepeat) {
                arrSquare.Add(arrSquareOnLine.ToArray());
            }
        }

        /// <summary>
        /// Class constructor. Build a board.
        /// </summary>
        public ChessBoard() {
            m_pBoard                    = new PieceE[64];
            m_piPiecesCount             = new int[16];
            m_book                      = null;
            m_rnd                       = new Random((int)DateTime.Now.Ticks);
            m_rndRep                    = new Random(0);
            m_stackPossibleEnPassantAt  = new Stack<int>(256);
            m_trace                     = null;
            m_moveHistory               = new MoveHistory();
            m_bDesignMode               = false;
            m_moveStack                 = new MovePosStack();
            ResetBoard();
        }

        /// <summary>
        /// Class constructor. Build a board.
        /// </summary>
        public ChessBoard(SearchEngine.ITrace trace) : this() {
            m_trace                 = trace;
        }

        /// <summary>
        /// Class constructor. Use to create a new clone
        /// </summary>
        /// <param name="chessBoard">   Board to copy from</param>
        private ChessBoard(ChessBoard chessBoard) : this() {
            CopyFrom(chessBoard);
        }

        /// <summary>
        /// Copy the state of the board from the specified one.
        /// </summary>
        /// <param name="chessBoard">   Board to copy from</param>
        public void CopyFrom(ChessBoard chessBoard) {
            int[]   arr;

            chessBoard.m_pBoard.CopyTo(m_pBoard, 0);
            chessBoard.m_piPiecesCount.CopyTo(m_piPiecesCount, 0);
            arr                         = chessBoard.m_stackPossibleEnPassantAt.ToArray();
            Array.Reverse(arr);
            m_stackPossibleEnPassantAt  = new Stack<int>(arr);
            m_book                      = chessBoard.m_book;
            m_iBlackKingPos             = chessBoard.m_iBlackKingPos;
            m_iWhiteKingPos             = chessBoard.m_iWhiteKingPos;
            m_rnd                       = chessBoard.m_rnd;
            m_rndRep                    = chessBoard.m_rndRep;
            m_iRBlackRookMoveCount      = chessBoard.m_iRBlackRookMoveCount;
            m_iLBlackRookMoveCount      = chessBoard.m_iLBlackRookMoveCount;
            m_iBlackKingMoveCount       = chessBoard.m_iBlackKingMoveCount;
            m_iRWhiteRookMoveCount      = chessBoard.m_iRWhiteRookMoveCount;
            m_iLWhiteRookMoveCount      = chessBoard.m_iLWhiteRookMoveCount;
            m_iWhiteKingMoveCount       = chessBoard.m_iWhiteKingMoveCount;
            m_bWhiteCastle              = chessBoard.m_bWhiteCastle;
            m_bBlackCastle              = chessBoard.m_bBlackCastle;
            m_iPossibleEnPassantAt      = chessBoard.m_iPossibleEnPassantAt;
            m_i64ZobristKey             = chessBoard.m_i64ZobristKey;
            m_trace                     = chessBoard.m_trace;
            m_moveStack                 = chessBoard.m_moveStack.Clone();
            m_moveHistory               = chessBoard.m_moveHistory.Clone();
            m_eCurrentPlayer            = chessBoard.m_eCurrentPlayer;
        }

        /// <summary>
        /// Clone the current board
        /// </summary>
        /// <returns>
        /// New copy of the board
        /// </returns>
        public ChessBoard Clone() {
            return(new ChessBoard(this));
        }

        /// <summary>
        /// Returns the XML serialization schema
        /// </summary>
        /// <returns>
        /// null
        /// </returns>
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {
            return(null);
        }

        /// <summary>
        /// Initialize the object using the specified XML reader
        /// </summary>
        /// <param name="reader">   XML reader</param>
        void IXmlSerializable.ReadXml(System.Xml.XmlReader reader) {
            bool    bIsEmpty;

            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "Board") {
                throw new SerializationException("Unknown format");
            } else if (reader.GetAttribute("Version") != "1.00") {
                throw new SerializationException("Unknown version");
            } else {
                reader.ReadStartElement();
                reader.ReadStartElement("Pieces");
                for (int iIndex = 0; iIndex < m_pBoard.Length; iIndex++) {
                    m_pBoard[iIndex] = (PieceE)Enum.Parse(typeof(SerPieceE), reader.ReadElementString("Piece"));
                }
                reader.ReadEndElement();
                m_iBlackKingPos = Int32.Parse(reader.ReadElementString("BlackKingPosition"));
                m_iWhiteKingPos = Int32.Parse(reader.ReadElementString("WhiteKingPosition"));
                reader.ReadStartElement("PieceCount");
                for (int iIndex = 1; iIndex < m_piPiecesCount.Length - 1; iIndex++) { 
                    m_piPiecesCount[iIndex] = Int32.Parse(reader.ReadElementString(((SerPieceE)iIndex).ToString()));
                }
                reader.ReadEndElement();
                m_iBlackKingMoveCount   = Int32.Parse(reader.ReadElementString("BlackKingMoveCount"));
                m_iWhiteKingMoveCount   = Int32.Parse(reader.ReadElementString("WhiteKingMoveCount"));
                m_iRBlackRookMoveCount  = Int32.Parse(reader.ReadElementString("RBlackRookMoveCount"));
                m_iLBlackRookMoveCount  = Int32.Parse(reader.ReadElementString("LBlackRookMoveCount"));
                m_iRWhiteRookMoveCount  = Int32.Parse(reader.ReadElementString("RWhiteRookMoveCount"));
                m_iLWhiteRookMoveCount  = Int32.Parse(reader.ReadElementString("LWhiteRookMoveCount"));
                m_bWhiteCastle          = Boolean.Parse(reader.ReadElementString("WhiteCastle"));
                m_bBlackCastle          = Boolean.Parse(reader.ReadElementString("BlackCastle"));
                m_iPossibleEnPassantAt  = Int32.Parse(reader.ReadElementString("EnPassant"));
                m_stackPossibleEnPassantAt.Clear();
                reader.MoveToContent();
                bIsEmpty = reader.IsEmptyElement;
                reader.ReadStartElement("EnPassantStack");
                if (!bIsEmpty) {
                    while (reader.IsStartElement()) {
                        m_stackPossibleEnPassantAt.Push(Int32.Parse(reader.ReadElementString("EP")));
                    }
                    reader.ReadEndElement();
                }
                ((IXmlSerializable)m_moveStack).ReadXml(reader);
                m_i64ZobristKey     = Int64.Parse(reader.ReadElementString("ZobristKey"));
                m_bDesignMode       = Boolean.Parse(reader.ReadElementString("DesignMode"));
                m_eCurrentPlayer    = (PlayerE)Enum.Parse(typeof(PlayerE), reader.ReadElementString("NextMoveColor"));
                m_bStdInitialBoard  = Boolean.Parse(reader.ReadElementString("StandardBoard"));
                ((IXmlSerializable)m_moveHistory).ReadXml(reader);
                reader.MoveToContent();
                m_posInfo.m_iAttackedPieces     = Int32.Parse(reader.GetAttribute("AttackedPieces"));
                m_posInfo.m_iPiecesDefending    = Int32.Parse(reader.GetAttribute("PiecesDefending"));
                reader.ReadStartElement("PositionInfo");
                reader.ReadEndElement();
            }            
        }

        /// <summary>
        /// Save the object into the XML writer
        /// </summary>
        /// <param name="writer">   XML writer</param>
        void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) {
            int[]           piStack;

            writer.WriteStartElement("Board");
            writer.WriteAttributeString("Version", "1.00");
            
            writer.WriteStartElement("Pieces");
            foreach (PieceE ePiece in m_pBoard) {
                writer.WriteElementString("Piece", ((SerPieceE)ePiece).ToString());
            }
            writer.WriteEndElement();
            
            writer.WriteElementString("BlackKingPosition", m_iBlackKingPos.ToString());
            writer.WriteElementString("WhiteKingPosition", m_iWhiteKingPos.ToString());
            
            writer.WriteStartElement("PieceCount");
            for (int iIndex = 1; iIndex < m_piPiecesCount.Length - 1; iIndex++) {
                writer.WriteElementString(((SerPieceE)iIndex).ToString() , m_piPiecesCount[iIndex].ToString());
            }
            writer.WriteEndElement();
            
            writer.WriteElementString("BlackKingMoveCount",  m_iBlackKingMoveCount.ToString());
            writer.WriteElementString("WhiteKingMoveCount",  m_iWhiteKingMoveCount.ToString());
            writer.WriteElementString("RBlackRookMoveCount", m_iRBlackRookMoveCount.ToString());
            writer.WriteElementString("LBlackRookMoveCount", m_iLBlackRookMoveCount.ToString());
            writer.WriteElementString("RWhiteRookMoveCount", m_iRWhiteRookMoveCount.ToString());
            writer.WriteElementString("LWhiteRookMoveCount", m_iLWhiteRookMoveCount.ToString());
            writer.WriteElementString("WhiteCastle",         m_bWhiteCastle.ToString());
            writer.WriteElementString("BlackCastle",         m_bBlackCastle.ToString());
            writer.WriteElementString("EnPassant",           m_iPossibleEnPassantAt.ToString());
            
            writer.WriteStartElement("EnPassantStack");
            piStack = m_stackPossibleEnPassantAt.ToArray();
            Array.Reverse(piStack);
            foreach (int iEnPassant in piStack) {
                writer.WriteElementString("EP",  iEnPassant.ToString());
            }
            writer.WriteEndElement();
            
            ((IXmlSerializable)m_moveStack).WriteXml(writer);
            writer.WriteElementString("ZobristKey",     m_i64ZobristKey.ToString());
            writer.WriteElementString("DesignMode",     m_bDesignMode.ToString());
            writer.WriteElementString("NextMoveColor",  m_eCurrentPlayer.ToString());
            writer.WriteElementString("StandardBoard",  m_bStdInitialBoard.ToString());
            ((IXmlSerializable)m_moveHistory).WriteXml(writer);
            writer.WriteStartElement("PositionInfo");
            writer.WriteAttributeString("AttackedPieces",  m_posInfo.m_iAttackedPieces.ToString());
            writer.WriteAttributeString("PiecesDefending", m_posInfo.m_iPiecesDefending.ToString());
            writer.WriteEndElement();
        }

        /// <summary>
        /// Stack of all moves done since initial board
        /// </summary>
        public MovePosStack MovePosStack {
            get {
                return(m_moveStack);
            }
        }

        /// <summary>
        /// Get the move history which handle the fifty-move rule and the threefold repetition rule
        /// </summary>
        public MoveHistory MoveHistory {
            get {
                return(m_moveHistory);
            }
        }

        /// <summary>
        /// Compute extra information about the board
        /// </summary>
        /// <param name="ePlayerToMove">        Player color to move</param>
        /// <param name="bAddRepetitionInfo">   true to add board repetition information</param>
        /// <returns>
        /// Extra information about the board to discriminate between two boards with sames pieces but
        /// different setting.
        /// </returns>
        public BoardStateMaskE ComputeBoardExtraInfo(PlayerE ePlayerToMove, bool bAddRepetitionInfo) {
            BoardStateMaskE  eRetVal;
            
            eRetVal = (BoardStateMaskE)m_iPossibleEnPassantAt;
            if (m_iWhiteKingMoveCount == 0) {
                if (m_iRWhiteRookMoveCount == 0) {
                    eRetVal |= BoardStateMaskE.WRCastling;
                }
                if (m_iLWhiteRookMoveCount == 0) {
                    eRetVal |= BoardStateMaskE.WLCastling;
                }
            }
            if (m_iBlackKingMoveCount == 0) {
                if (m_iRBlackRookMoveCount == 0) {
                    eRetVal |= BoardStateMaskE.BRCastling;
                }
                if (m_iLBlackRookMoveCount == 0) {
                    eRetVal |= BoardStateMaskE.BLCastling;
                }
            }
            if (ePlayerToMove == PlayerE.Black) {
                eRetVal |= BoardStateMaskE.BlackToMove;
            }
            if (bAddRepetitionInfo) {
                eRetVal = (BoardStateMaskE)((m_moveHistory.GetCurrentBoardCount(m_i64ZobristKey) & 7) << 11);
            }
            return(eRetVal);
        }

        /// <summary>
        /// Reset initial board info
        /// </summary>
        /// <param name="eNextMoveColor">   Next color moving</param>
        /// <param name="bInitialBoardStd"> true if its a standard board, false if coming from FEN or design mode</param>
        /// <param name="eMask">            Extra bord information</param>
        /// <param name="iEnPassant">       Position for en passant</param>
        private void ResetInitialBoardInfo(PlayerE eNextMoveColor, bool bInitialBoardStd, BoardStateMaskE eMask, int iEnPassant) {
            PieceE  ePiece;
            int     iEnPassantCol;

            Array.Clear(m_piPiecesCount, 0, m_piPiecesCount.Length);
            for (int iIndex = 0; iIndex < 64; iIndex++) {
                ePiece = m_pBoard[iIndex];
                switch(ePiece) {
                case PieceE.King | PieceE.White:
                    m_iWhiteKingPos = iIndex;
                    break;
                case PieceE.King | PieceE.Black:
                    m_iBlackKingPos = iIndex;
                    break;
                }
                m_piPiecesCount[(int)ePiece]++;
            }
            if (iEnPassant != 0) {
                iEnPassantCol   = (iEnPassant >> 3);
                if (iEnPassantCol != 2 && iEnPassantCol != 5) {
                    if (iEnPassantCol == 3) {   // Fixing old saved board which was keeping the en passant position at the position of the pawn instead of behind it
                        iEnPassant -= 8;    
                    } else if (iEnPassantCol == 4) {
                        iEnPassant += 8;
                    } else {
                        iEnPassant = 0;
                    }
                }
            }
            m_iPossibleEnPassantAt  = iEnPassant;
            m_iRBlackRookMoveCount  = ((eMask & BoardStateMaskE.BRCastling) == BoardStateMaskE.BRCastling) ? 0 : 1;
            m_iLBlackRookMoveCount  = ((eMask & BoardStateMaskE.BLCastling) == BoardStateMaskE.BLCastling) ? 0 : 1;
            m_iBlackKingMoveCount   = 0;
            m_iRWhiteRookMoveCount  = ((eMask & BoardStateMaskE.WRCastling) == BoardStateMaskE.WRCastling) ? 0 : 1;
            m_iLWhiteRookMoveCount  = ((eMask & BoardStateMaskE.WLCastling) == BoardStateMaskE.WLCastling) ? 0 : 1;
            m_iWhiteKingMoveCount   = 0;
            m_bWhiteCastle          = false;
            m_bBlackCastle          = false;
            m_i64ZobristKey         = ZobristKey.ComputeBoardZobristKey(m_pBoard);
            m_eCurrentPlayer        = eNextMoveColor;
            m_bDesignMode           = false;
            m_bStdInitialBoard      = bInitialBoardStd;
            m_moveHistory.Reset(m_pBoard, ComputeBoardExtraInfo(PlayerE.White, false));
            m_moveStack.Clear();
            m_stackPossibleEnPassantAt.Clear();
        }

        /// <summary>
        /// Reset the board to the initial configuration
        /// </summary>
        public void ResetBoard() {
            for (int iIndex = 0; iIndex < 64; iIndex++) {
                m_pBoard[iIndex] = PieceE.None;
            }
            for (int iIndex = 0; iIndex < 8; iIndex++) {
                m_pBoard[8+iIndex]  = PieceE.Pawn | PieceE.White;
                m_pBoard[48+iIndex] = PieceE.Pawn | PieceE.Black;
            }
            m_pBoard[0]                                             = PieceE.Rook   | PieceE.White;
            m_pBoard[7*8]                                           = PieceE.Rook   | PieceE.Black;
            m_pBoard[7]                                             = PieceE.Rook   | PieceE.White;
            m_pBoard[7*8+7]                                         = PieceE.Rook   | PieceE.Black;
            m_pBoard[1]                                             = PieceE.Knight | PieceE.White;
            m_pBoard[7*8+1]                                         = PieceE.Knight | PieceE.Black;
            m_pBoard[6]                                             = PieceE.Knight | PieceE.White;
            m_pBoard[7*8+6]                                         = PieceE.Knight | PieceE.Black;
            m_pBoard[2]                                             = PieceE.Bishop | PieceE.White;
            m_pBoard[7*8+2]                                         = PieceE.Bishop | PieceE.Black;
            m_pBoard[5]                                             = PieceE.Bishop | PieceE.White;
            m_pBoard[7*8+5]                                         = PieceE.Bishop | PieceE.Black;
            m_pBoard[3]                                             = PieceE.King   | PieceE.White;
            m_pBoard[7*8+3]                                         = PieceE.King   | PieceE.Black;
            m_pBoard[4]                                             = PieceE.Queen  | PieceE.White;
            m_pBoard[7*8+4]                                         = PieceE.Queen  | PieceE.Black;
            ResetInitialBoardInfo(PlayerE.White,
                                  true /*Standard board*/,
                                  BoardStateMaskE.BLCastling | BoardStateMaskE.BRCastling | BoardStateMaskE.WLCastling | BoardStateMaskE.WRCastling,
                                  0 /*iEnPassant*/);
        }

        /// <summary>
        /// Save the content of the board into the specified binary writer
        /// </summary>
        /// <param name="writer">   Binary writer</param>
        public void SaveBoard(BinaryWriter writer) {
            string                  strVersion;
            ChessBoard              chessBoardInitial;
            MoveHistory.PackedBoard packetBoard;
            
            strVersion  = "SRCBD095";
            writer.Write(strVersion);
            writer.Write(m_bStdInitialBoard);
            if (!m_bStdInitialBoard) {
                chessBoardInitial = Clone();
                chessBoardInitial.UndoAllMoves();
                packetBoard = MoveHistory.ComputePackedBoard(chessBoardInitial.m_pBoard, ComputeBoardExtraInfo(CurrentPlayer, false));
                writer.Write(packetBoard.m_lVal1);
                writer.Write(packetBoard.m_lVal2);
                writer.Write(packetBoard.m_lVal3);
                writer.Write(packetBoard.m_lVal4);
                writer.Write((int)packetBoard.m_eInfo);
                writer.Write(m_iPossibleEnPassantAt);
            }
            m_moveStack.SaveToWriter(writer);
        }

        /// <summary>
        /// Load the content of the board into the specified stream
        /// </summary>
        /// <param name="reader">   Binary reader</param>
        public bool LoadBoard(BinaryReader reader) {
            bool                        bRetVal;
            MoveHistory.PackedBoard     packetBoard;
            string                      strVersion;
            int                         iEnPassant;
            
            strVersion = reader.ReadString();
            if (strVersion != "SRCBD095") {
                bRetVal = false;
            } else {
                bRetVal = true;
                ResetBoard();
                m_bStdInitialBoard = reader.ReadBoolean();
                if (!m_bStdInitialBoard) {
                    packetBoard.m_lVal1 = reader.ReadInt64();
                    packetBoard.m_lVal2 = reader.ReadInt64();
                    packetBoard.m_lVal3 = reader.ReadInt64();
                    packetBoard.m_lVal4 = reader.ReadInt64();
                    packetBoard.m_eInfo = (BoardStateMaskE)reader.ReadInt32();
                    iEnPassant          = reader.ReadInt32();
                    MoveHistory.UnpackBoard(packetBoard, m_pBoard);
                    m_eCurrentPlayer = ((packetBoard.m_eInfo & BoardStateMaskE.BlackToMove) == BoardStateMaskE.BlackToMove) ? PlayerE.Black : PlayerE.White;
                    ResetInitialBoardInfo(m_eCurrentPlayer, m_bStdInitialBoard, packetBoard.m_eInfo, iEnPassant);
                }
                m_moveStack.LoadFromReader(reader);
                for (int iIndex = 0; iIndex <= m_moveStack.PositionInList; iIndex++) {
                    DoMoveNoLog(m_moveStack.List[iIndex].Move);
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Create a new game using the specified list of moves
        /// </summary>
        /// <param name="chessBoardStarting">   Starting board or null if standard board</param>
        /// <param name="listMove">             List of moves</param>
        /// <param name="eStartingColor">       Board starting color</param>
        public void CreateGameFromMove(ChessBoard chessBoardStarting, List<MoveExt> listMove, PlayerE eStartingColor) {
            BoardStateMaskE  eMask;
            
            if (chessBoardStarting != null) {
                CopyFrom(chessBoardStarting);
                eMask = chessBoardStarting.ComputeBoardExtraInfo(PlayerE.White, false);
                ResetInitialBoardInfo(eStartingColor, false /*bInitialBoardStd*/, eMask, chessBoardStarting.m_iPossibleEnPassantAt);
            } else {
                ResetBoard();
            }
            foreach (MoveExt move in listMove) {
                DoMove(move);
            }
        }

        /// <summary>
        /// Determine if the board is in design mode
        /// </summary>
        public bool DesignMode {
            get {
                return(m_bDesignMode);
            }
        }

        /// <summary>
        /// Open the design mode
        /// </summary>
        public void OpenDesignMode() {
            m_bDesignMode = true;
        }

        /// <summary>
        /// Try to close the design mode.
        /// </summary>
        /// <param name="eNextMoveColor">   Color of the next move</param>
        /// <param name="eBoardMask">       Board extra information</param>
        /// <param name="iEnPassant">       Position of en passant or 0 if none</param>
        /// <returns>
        /// true if succeed, false if board is invalid
        /// </returns>
        public bool CloseDesignMode(PlayerE eNextMoveColor, BoardStateMaskE eBoardMask, int iEnPassant) {
            bool    bRetVal;
            
            if (!m_bDesignMode) {
                bRetVal = true;
            } else {
                ResetInitialBoardInfo(eNextMoveColor, false, eBoardMask, iEnPassant);
                if (m_piPiecesCount[(int)(PieceE.King | PieceE.White)] == 1 &&
                    m_piPiecesCount[(int)(PieceE.King | PieceE.Black)] == 1) {
                    bRetVal = true;
                } else {
                    bRetVal = false;
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// true if the board is standard, false if initialized from design mode or FEN
        /// </summary>
        public bool StandardInitialBoard {
            get {
                return(m_bStdInitialBoard);
            }
        }

        /// <summary>
        /// Update the packed board representation and the value of the hash key representing the current board state.
        /// </summary>
        /// <param name="iPos1">        Position of the change</param>
        /// <param name="eNewPiece1">   New piece</param>
        private void UpdatePackedBoardAndZobristKey(int iPos1, PieceE eNewPiece1) {
            m_i64ZobristKey = ZobristKey.UpdateZobristKey(m_i64ZobristKey, iPos1, m_pBoard[iPos1], eNewPiece1);
            m_moveHistory.UpdateCurrentPackedBoard(iPos1, eNewPiece1);
        }

        /// <summary>
        /// Current Zobrist key value
        /// </summary>
        public long CurrentZobristKey {
            get {
                return(m_i64ZobristKey);
            }
        }

        /// <summary>
        /// Update the packed board representation and the value of the hash key representing the current board state. Use if two
        /// board positions are changed.
        /// </summary>
        /// <param name="iPos1">        Position of the change</param>
        /// <param name="eNewPiece1">   New piece</param>
        /// <param name="iPos2">        Position of the change</param>
        /// <param name="eNewPiece2">   New piece</param>
        private void UpdatePackedBoardAndZobristKey(int iPos1, PieceE eNewPiece1, int iPos2, PieceE eNewPiece2) {
            m_i64ZobristKey = ZobristKey.UpdateZobristKey(m_i64ZobristKey, iPos1, m_pBoard[iPos1], eNewPiece1, iPos2, m_pBoard[iPos2], eNewPiece2);
            m_moveHistory.UpdateCurrentPackedBoard(iPos1, eNewPiece1);
            m_moveHistory.UpdateCurrentPackedBoard(iPos2, eNewPiece2);
        }

        /// <summary>
        /// Player which play next
        /// </summary>
        public PlayerE CurrentPlayer {
            get {
                return(m_eCurrentPlayer);
            }
        }

        /// <summary>
        /// Player which did the last move
        /// </summary>
        public PlayerE LastMovePlayer {
            get {
                return((m_eCurrentPlayer == PlayerE.White) ? PlayerE.Black : PlayerE.White);
            }
        }

        /// <summary>
        /// Get a piece at the specified position. Position 0 = Lower right (H1), 63 = Higher left (A8)
        /// </summary>
        public PieceE this[int iPos] {
            get {
                return(m_pBoard[iPos]);
            }
            set {
                if (m_bDesignMode) {
                    if (m_pBoard[iPos] != value) {
                        m_piPiecesCount[(int)m_pBoard[iPos]]--;
                        m_pBoard[iPos] = value;
                        m_piPiecesCount[(int)m_pBoard[iPos]]++;
                    }
                } else {
                    throw new NotSupportedException("Cannot be used if not in design mode");
                }
            }
        }

        /// <summary>
        /// Get the number of the specified piece which has been eated
        /// </summary>
        /// <param name="ePiece">   Piece and color</param>
        /// <returns>
        /// Count
        /// </returns>
        public int GetEatedPieceCount(PieceE ePiece) {
            int     iRetVal;
            
            switch(ePiece & PieceE.PieceMask) {
            case PieceE.Pawn:
                iRetVal = 8 - m_piPiecesCount[(int)ePiece];
                break;
            case PieceE.Rook:
            case PieceE.Knight:
            case PieceE.Bishop:
                iRetVal = 2 - m_piPiecesCount[(int)ePiece];
                break;
            case PieceE.Queen:
            case PieceE.King:
                iRetVal = 1 - m_piPiecesCount[(int)ePiece];
                break;
            default:
                iRetVal = 0;
                break;
            }
            if (iRetVal < 0) {
                iRetVal = 0;
            }
            return(iRetVal);                
        }

        /// <summary>
        /// Check the integrity of the board. Use for debugging.
        /// </summary>
        public void CheckIntegrity() {
            int[]   piPiecesCount;
            int     iBlackKingPos = -1;
            int     iWhiteKingPos = -1;
            
            piPiecesCount = new int[16];
            for (int iIndex = 0; iIndex < 64; iIndex++) {
                piPiecesCount[(int)m_pBoard[iIndex]]++;
                if (m_pBoard[iIndex] == PieceE.King) {
                    iWhiteKingPos = iIndex;
                } else if (m_pBoard[iIndex] == (PieceE.King | PieceE.Black)) {
                    iBlackKingPos = iIndex;
                }
            }
            for (int iIndex = 1; iIndex < 16; iIndex++) {
                if (m_piPiecesCount[iIndex] != piPiecesCount[iIndex]) {
                    throw new ChessException("Piece count mismatch");
                }
            }
            if (iBlackKingPos != m_iBlackKingPos ||
                iWhiteKingPos != m_iWhiteKingPos) {
                throw new ChessException("King position mismatch");
            }
        }

        /// <summary>
        /// Do the move (without log)
        /// </summary>
        /// <param name="move">     Move to do</param>
        /// <returns>
        /// NoRepeat        No repetition
        /// ThreeFoldRepeat Three times the same board
        /// FiftyRuleRepeat Fifty moves without pawn move or piece eaten
        /// </returns>
        public RepeatResultE DoMoveNoLog(Move move) {
            RepeatResultE   eRetVal;
            PieceE          ePiece;
            PieceE          eOldPiece;
            int             iEnPassantVictimPos;
            int             iDelta;
            bool            bPawnMoveOrPieceEaten;
            
            m_stackPossibleEnPassantAt.Push(m_iPossibleEnPassantAt);
            m_iPossibleEnPassantAt  = 0;
            ePiece                  = m_pBoard[move.StartPos];
            bPawnMoveOrPieceEaten   = ((ePiece & PieceE.PieceMask) == PieceE.Pawn) |
                                      ((move.Type & Move.TypeE.PieceEaten) == Move.TypeE.PieceEaten);
            switch(move.Type & Move.TypeE.MoveTypeMask) {
            case Move.TypeE.Castle:
                UpdatePackedBoardAndZobristKey(move.EndPos, ePiece, move.StartPos, PieceE.None);
                m_pBoard[move.EndPos]    = ePiece;
                m_pBoard[move.StartPos]  = PieceE.None;
                eOldPiece                = PieceE.None;
                if ((ePiece & PieceE.Black) != 0) {
                    if (move.EndPos == 57) {
                        UpdatePackedBoardAndZobristKey(58, m_pBoard[56], 56, PieceE.None);
                        m_pBoard[58] = m_pBoard[56];
                        m_pBoard[56] = PieceE.None;
                    } else {
                        UpdatePackedBoardAndZobristKey(60, m_pBoard[63], 63, PieceE.None);
                        m_pBoard[60] = m_pBoard[63];
                        m_pBoard[63] = PieceE.None;
                    }
                    m_bBlackCastle  = true;
                    m_iBlackKingPos = move.EndPos;
                } else {
                    if (move.EndPos == 1) {
                        UpdatePackedBoardAndZobristKey(2, m_pBoard[0], 0, PieceE.None);
                        m_pBoard[2] = m_pBoard[0];
                        m_pBoard[0] = PieceE.None;
                    } else {
                        UpdatePackedBoardAndZobristKey(4, m_pBoard[7], 7, PieceE.None);
                        m_pBoard[4] = m_pBoard[7];
                        m_pBoard[7] = PieceE.None;
                    }
                    m_bWhiteCastle  = true;
                    m_iWhiteKingPos = move.EndPos;
                }
                break;
            case Move.TypeE.EnPassant:
                UpdatePackedBoardAndZobristKey(move.EndPos, ePiece, move.StartPos, PieceE.None);
                m_pBoard[move.EndPos]   = ePiece;
                m_pBoard[move.StartPos] = PieceE.None;
                iEnPassantVictimPos     = (move.StartPos & 56) + (move.EndPos & 7);
                eOldPiece               = m_pBoard[iEnPassantVictimPos];
                UpdatePackedBoardAndZobristKey(iEnPassantVictimPos, PieceE.None);
                m_pBoard[iEnPassantVictimPos]   = PieceE.None;
                m_piPiecesCount[(int)eOldPiece]--;
                break;
            default:
                // Normal
                // PawnPromotionTo???
                eOldPiece = m_pBoard[move.EndPos];
                switch(move.Type & Move.TypeE.MoveTypeMask) {
                case Move.TypeE.PawnPromotionToQueen:
                    m_piPiecesCount[(int)ePiece]--;
                    ePiece = PieceE.Queen | (ePiece & PieceE.Black);
                    m_piPiecesCount[(int)ePiece]++;
                    break;
                case Move.TypeE.PawnPromotionToRook:
                    m_piPiecesCount[(int)ePiece]--;
                    ePiece = PieceE.Rook | (ePiece & PieceE.Black);
                    m_piPiecesCount[(int)ePiece]++;
                    break;
                case Move.TypeE.PawnPromotionToBishop:
                    m_piPiecesCount[(int)ePiece]--;
                    ePiece = PieceE.Bishop | (ePiece & PieceE.Black);
                    m_piPiecesCount[(int)ePiece]++;
                    break;
                case Move.TypeE.PawnPromotionToKnight:
                    m_piPiecesCount[(int)ePiece]--;
                    ePiece = PieceE.Knight | (ePiece & PieceE.Black);
                    m_piPiecesCount[(int)ePiece]++;
                    break;
                case Move.TypeE.PawnPromotionToPawn:
                default:
                    break;
                }
                UpdatePackedBoardAndZobristKey(move.EndPos, ePiece, move.StartPos, PieceE.None);
                m_pBoard[move.EndPos]    = ePiece;
                m_pBoard[move.StartPos]  = PieceE.None;
                m_piPiecesCount[(int)eOldPiece]--;
                switch(ePiece) {
                case PieceE.King | PieceE.Black:
                    m_iBlackKingPos = move.EndPos;
                    if (move.StartPos == 59) {
                        m_iBlackKingMoveCount++;
                    }
                    break;
                case PieceE.King | PieceE.White:
                    m_iWhiteKingPos = move.EndPos;
                    if (move.StartPos == 3) {
                        m_iWhiteKingMoveCount++;
                    }
                    break;
                case PieceE.Rook | PieceE.Black:
                    if (move.StartPos == 56) {
                        m_iLBlackRookMoveCount++;
                    } else if (move.StartPos == 63) {
                        m_iRBlackRookMoveCount++;
                    }
                    break;
                case PieceE.Rook | PieceE.White:
                    if (move.StartPos == 0) {
                        m_iLWhiteRookMoveCount++;
                    } else if (move.StartPos == 7) {
                        m_iRWhiteRookMoveCount++;
                    }
                    break;
                case PieceE.Pawn | PieceE.White:
                case PieceE.Pawn | PieceE.Black:
                    iDelta = move.StartPos - move.EndPos;
                    if (iDelta == -16 || iDelta == 16) {
                        m_iPossibleEnPassantAt = move.EndPos + (iDelta >> 1); // Position behind the pawn
                    }
                    break;
                }
                break;
            }
            m_moveHistory.UpdateCurrentPackedBoard(ComputeBoardExtraInfo(PlayerE.White, false));
            eRetVal = m_moveHistory.AddCurrentPackedBoard(m_i64ZobristKey, bPawnMoveOrPieceEaten);
            m_eCurrentPlayer = (m_eCurrentPlayer == PlayerE.White) ? PlayerE.Black : PlayerE.White;
            return(eRetVal);
        }

        /// <summary>
        /// Undo a move (without log)
        /// </summary>
        /// <param name="move">     Move to undo</param>
        public void UndoMoveNoLog(Move move) {
            PieceE      ePiece;
            PieceE      eOriginalPiece;
            int         iOldPiecePos;
            
            m_moveHistory.RemoveLastMove(m_i64ZobristKey);
            ePiece = m_pBoard[move.EndPos];
            switch(move.Type & Move.TypeE.MoveTypeMask) {
            case Move.TypeE.Castle:
                UpdatePackedBoardAndZobristKey(move.StartPos, ePiece, move.EndPos, PieceE.None);
                m_pBoard[move.StartPos]   = ePiece;
                m_pBoard[move.EndPos]     = PieceE.None;
                if ((ePiece & PieceE.Black) != 0) {
                    if (move.EndPos == 57) {
                        UpdatePackedBoardAndZobristKey(56, m_pBoard[58], 58, PieceE.None);
                        m_pBoard[56] = m_pBoard[58];
                        m_pBoard[58] = PieceE.None;
                    } else {
                        UpdatePackedBoardAndZobristKey(63, m_pBoard[60], 60, PieceE.None);
                        m_pBoard[63] = m_pBoard[60];
                        m_pBoard[60] = PieceE.None;
                    }
                    m_bBlackCastle  = false;
                    m_iBlackKingPos = move.StartPos;
                } else {
                    if (move.EndPos == 1) {
                        UpdatePackedBoardAndZobristKey(0, m_pBoard[2], 2, PieceE.None);
                        m_pBoard[0] = m_pBoard[2];
                        m_pBoard[2] = PieceE.None;
                    } else {
                        UpdatePackedBoardAndZobristKey(7, m_pBoard[4], 4, PieceE.None);
                        m_pBoard[7] = m_pBoard[4];
                        m_pBoard[4] = PieceE.None;
                    }
                    m_bWhiteCastle  = false;
                    m_iWhiteKingPos = move.StartPos;
                }
                break;
            case Move.TypeE.EnPassant:
                UpdatePackedBoardAndZobristKey(move.StartPos, ePiece, move.EndPos, PieceE.None);
                m_pBoard[move.StartPos]  = ePiece;
                m_pBoard[move.EndPos]    = PieceE.None;
                eOriginalPiece              = PieceE.Pawn | (((ePiece & PieceE.Black) == 0) ? PieceE.Black : PieceE.White);
                iOldPiecePos                = (move.StartPos & 56) + (move.EndPos & 7);
                UpdatePackedBoardAndZobristKey(iOldPiecePos, eOriginalPiece);
                m_pBoard[iOldPiecePos]      = eOriginalPiece;
                m_piPiecesCount[(int)eOriginalPiece]++;
                break;
            default:
                // Normal
                // PawnPromotionTo???
                eOriginalPiece  = move.OriginalPiece;
                switch(move.Type & Move.TypeE.MoveTypeMask) {
                case Move.TypeE.PawnPromotionToQueen:
                case Move.TypeE.PawnPromotionToRook:
                case Move.TypeE.PawnPromotionToBishop:
                case Move.TypeE.PawnPromotionToKnight:
                    m_piPiecesCount[(int)ePiece]--;
                    ePiece = PieceE.Pawn | (ePiece & PieceE.Black);
                    m_piPiecesCount[(int)ePiece]++;
                    break;
                case Move.TypeE.PawnPromotionToPawn:
                default:
                    break;
                }
                UpdatePackedBoardAndZobristKey(move.StartPos, ePiece, move.EndPos, eOriginalPiece);
                m_pBoard[move.StartPos] = ePiece;
                m_pBoard[move.EndPos]   = eOriginalPiece;
                m_piPiecesCount[(int)eOriginalPiece]++;
                switch(ePiece) {
                case PieceE.King | PieceE.Black:
                    m_iBlackKingPos = move.StartPos;
                    if (move.StartPos == 59) {
                        m_iBlackKingMoveCount--;
                    }
                    break;
                case PieceE.King:
                    m_iWhiteKingPos = move.StartPos;
                    if (move.StartPos == 3) {
                        m_iWhiteKingMoveCount--;
                    }
                    break;
                case PieceE.Rook | PieceE.Black:
                    if (move.StartPos == 56) {
                        m_iLBlackRookMoveCount--;
                    } else if (move.StartPos == 63) {
                        m_iRBlackRookMoveCount--;
                    }
                    break;
                case PieceE.Rook:
                    if (move.StartPos == 0) {
                        m_iLWhiteRookMoveCount--;
                    } else if (move.StartPos == 7) {
                        m_iRWhiteRookMoveCount--;
                    }
                    break;
                }
                break;
            }
            m_iPossibleEnPassantAt = m_stackPossibleEnPassantAt.Pop();
            m_eCurrentPlayer       = (m_eCurrentPlayer == PlayerE.White) ? PlayerE.Black : PlayerE.White;
        }

        /// <summary>
        /// Check if there is enough pieces to make a check mate
        /// </summary>
        /// <returns>
        /// true            Yes
        /// false           No
        /// </returns>
        public bool IsEnoughPieceForCheckMate() {
            bool    bRetVal;
            int     iBigPieceCount;
            int     iWhiteBishop;
            int     iBlackBishop;
            int     iWhiteKnight;
            int     iBlackKnight;
            
            if  (m_piPiecesCount[(int)(PieceE.Pawn | PieceE.White)] != 0 ||
                 m_piPiecesCount[(int)(PieceE.Pawn | PieceE.Black)] != 0) {
                 bRetVal = true;
            } else {
                iBigPieceCount = m_piPiecesCount[(int)(PieceE.Queen  | PieceE.White)] +
                                 m_piPiecesCount[(int)(PieceE.Queen  | PieceE.Black)] +
                                 m_piPiecesCount[(int)(PieceE.Rook   | PieceE.White)] +
                                 m_piPiecesCount[(int)(PieceE.Rook   | PieceE.Black)];
                if (iBigPieceCount != 0) {
                    bRetVal = true;
                } else {
                    iWhiteBishop = m_piPiecesCount[(int)(PieceE.Bishop | PieceE.White)];
                    iBlackBishop = m_piPiecesCount[(int)(PieceE.Bishop | PieceE.Black)];
                    iWhiteKnight = m_piPiecesCount[(int)(PieceE.Knight | PieceE.White)];
                    iBlackKnight = m_piPiecesCount[(int)(PieceE.Knight | PieceE.Black)];
                    if ((iWhiteBishop + iWhiteKnight) >= 2 || (iBlackBishop + iBlackKnight) >= 2) {
                        // Two knights is typically impossible... but who knows!
                        bRetVal = true;
                    } else {
                        bRetVal = false;
                    }
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Gets the current board result
        /// </summary>
        /// <returns>
        /// NoRepeat        Yes
        /// Check           Yes, but the user is currently in check
        /// Tie             No, no move for the user
        /// Mate            No, user is checkmate
        /// </returns>
        public GameResultE GetCurrentResult(RepeatResultE eRepeatResult) {
            GameResultE     eRetVal;
            List<Move>      moveList;
            PlayerE         ePlayer;

            switch(eRepeatResult) {
            case RepeatResultE.ThreeFoldRepeat:
                eRetVal = GameResultE.ThreeFoldRepeat;
                break;
            case RepeatResultE.FiftyRuleRepeat:
                eRetVal = GameResultE.FiftyRuleRepeat;
                break;
            default:
                ePlayer     = CurrentPlayer;
                moveList    = EnumMoveList(ePlayer);
                if (IsCheck(ePlayer)) {
                    eRetVal = (moveList.Count == 0) ? GameResultE.Mate : GameResultE.Check;
                } else {
                    if (IsEnoughPieceForCheckMate()) {
                        eRetVal = (moveList.Count == 0) ? GameResultE.TieNoMove : GameResultE.OnGoing;
                    } else {
                        eRetVal = GameResultE.TieNoMatePossible;
                    }
                }
                break;
            }
            return(eRetVal);
        }

        /// <summary>
        /// Checks the current board result
        /// </summary>
        /// <returns>
        /// Board result
        /// </returns>
        public GameResultE GetCurrentResult() {
            GameResultE     eRetVal;
            RepeatResultE   eRepeatResult;

            eRepeatResult   = m_moveHistory.CurrentRepeatResult(m_i64ZobristKey);
            eRetVal         = GetCurrentResult(eRepeatResult);
            return(eRetVal);
        }

        /// <summary>
        /// Do the move
        /// </summary>
        /// <param name="move"> Move to do</param>
        /// <returns>
        /// NoRepeat        No repetition
        /// ThreeFoldRepeat Three times the same board
        /// FiftyRuleRepeat Fifty moves without pawn move or piece eaten
        /// </returns>
        public GameResultE DoMove(MoveExt move) {
            GameResultE     eRetVal;
            RepeatResultE   eRepeatResult;
            
            eRepeatResult = DoMoveNoLog(move.Move);
            eRetVal       = GetCurrentResult(eRepeatResult);
            m_moveStack.AddMove(move);
            return(eRetVal);
        }

        /// <summary>
        /// Undo a move
        /// </summary>
        public void UndoMove() {
            UndoMoveNoLog(m_moveStack.CurrentMove.Move);
            m_moveStack.MoveToPrevious();
        }

        /// <summary>
        /// Redo a move
        /// </summary>
        /// <returns>
        /// NoRepeat        No repetition
        /// ThreeFoldRepeat Three times the same board
        /// FiftyRuleRepeat Fifty moves without pawn move or piece eaten
        /// </returns>
        public GameResultE RedoMove() {
            GameResultE     eRetVal;
            RepeatResultE   eRepeatResult;
            
            eRepeatResult   = DoMoveNoLog(m_moveStack.NextMove.Move);
            eRetVal         = GetCurrentResult(eRepeatResult);
            m_moveStack.MoveToNext();
            return(eRetVal);
        }

        /// <summary>
        /// SetUndoRedoPosition:    Set the Undo/Redo position
        /// </summary>
        /// <param name="iPos">     New position</param>
        public void SetUndoRedoPosition(int iPos) {
            int     iCurPos;
            
            iCurPos = m_moveStack.PositionInList;
            while (iCurPos > iPos) {
                UndoMove();
                iCurPos--;
            }
            while (iCurPos < iPos) {
                RedoMove();
                iCurPos++;
            }
        }

        /// <summary>
        /// Gets the number of white pieces on the board
        /// </summary>
        public int WhitePieceCount {
            get {
                int iRetVal = 0;
                
                for (int iIndex = 1; iIndex < 7; iIndex++) {
                    iRetVal += m_piPiecesCount[iIndex];
                }
                return(iRetVal);
            }
        }

        /// <summary>
        /// Gets the number of black pieces on the board
        /// </summary>
        public int BlackPieceCount {
            get {
                int iRetVal = 0;
                
                for (int iIndex = 9; iIndex < 15; iIndex++) {
                    iRetVal += m_piPiecesCount[iIndex];
                }
                return(iRetVal);
            }
        }

        /// <summary>
        /// Enumerates the attacking position using arrays of possible position and two possible enemy pieces
        /// </summary>
        /// <param name="arrAttackPos">     Array to fill with the attacking position. Can be null if only the count is wanted</param>
        /// <param name="ppiCaseMoveList">  Array of array of position.</param>
        /// <param name="ePiece1">          Piece which can possibly attack this position</param>
        /// <param name="ePiece2">          Piece which can possibly attack this position</param>
        /// <returns>
        /// Count of attacker
        /// </returns>
        private int EnumTheseAttackPos(List<byte> arrAttackPos, int[][] ppiCaseMoveList, PieceE ePiece1, PieceE ePiece2) {
            int     iRetVal = 0;
            PieceE  ePiece;
            
            foreach (int[] piMoveList in ppiCaseMoveList) {
                foreach (int iNewPos in piMoveList) {
                    ePiece = m_pBoard[iNewPos];
                    if (ePiece != PieceE.None) {
                        if (ePiece == ePiece1 ||
                            ePiece == ePiece2) {
                            iRetVal++;
                            if (arrAttackPos != null) {
                                arrAttackPos.Add((byte)iNewPos);
                            }
                        }
                        break;
                    }                    
                }
            }
            return(iRetVal);
        }

        /// <summary>
        /// Enumerates the attacking position using an array of possible position and one possible enemy piece
        /// </summary>
        /// <param name="arrAttackPos">     Array to fill with the attacking position. Can be null if only the count is wanted</param>
        /// <param name="piCaseMoveList">   Array of position.</param>
        /// <param name="ePiece">           Piece which can possibly attack this position</param>
        /// <returns>
        /// Count of attacker
        /// </returns>
        private int EnumTheseAttackPos(List<byte> arrAttackPos, int[] piCaseMoveList, PieceE ePiece) {
            int     iRetVal = 0;
            
            foreach (int iNewPos in piCaseMoveList) {
                if (m_pBoard[iNewPos] == ePiece) {
                    iRetVal++;
                    if (arrAttackPos != null) {
                        arrAttackPos.Add((byte)iNewPos);
                    }
                }
            }
            return(iRetVal);
        }

        /// <summary>
        /// Enumerates all position which can attack a given position
        /// </summary>
        /// <param name="ePlayerColor">     Position to check for black or white</param>
        /// <param name="iPos">             Position to check.</param>
        /// <param name="arrAttackPos">     Array to fill with the attacking position. Can be null if only the count is wanted</param>
        /// <returns>
        /// Count of attacker
        /// </returns>
        private int EnumAttackPos(PlayerE ePlayerColor, int iPos, List<byte> arrAttackPos) {
            int     iRetVal;
            PieceE  eColor;
            PieceE  eEnemyQueen;
            PieceE  eEnemyRook;
            PieceE  eEnemyKing;
            PieceE  eEnemyBishop;
            PieceE  eEnemyKnight;
            PieceE  eEnemyPawn;
                                          
            eColor          = (ePlayerColor == PlayerE.Black) ? PieceE.White : PieceE.Black;
            eEnemyQueen     = PieceE.Queen  | eColor;
            eEnemyRook      = PieceE.Rook   | eColor;
            eEnemyKing      = PieceE.King   | eColor;
            eEnemyBishop    = PieceE.Bishop | eColor;
            eEnemyKnight    = PieceE.Knight | eColor;
            eEnemyPawn      = PieceE.Pawn   | eColor;
            iRetVal         = EnumTheseAttackPos(arrAttackPos, s_pppiCaseMoveDiagonal[iPos], eEnemyQueen, eEnemyBishop);
            iRetVal        += EnumTheseAttackPos(arrAttackPos, s_pppiCaseMoveLine[iPos],     eEnemyQueen, eEnemyRook);
            iRetVal        += EnumTheseAttackPos(arrAttackPos, s_ppiCaseMoveKing[iPos],      eEnemyKing);
            iRetVal        += EnumTheseAttackPos(arrAttackPos, s_ppiCaseMoveKnight[iPos],    eEnemyKnight);
            iRetVal        += EnumTheseAttackPos(arrAttackPos, (ePlayerColor == PlayerE.Black) ? s_ppiCaseWhitePawnCanAttackFrom[iPos] : s_ppiCaseBlackPawnCanAttackFrom[iPos], eEnemyPawn);
            return(iRetVal);
        }

        /// <summary>
        /// Determine if the specified king is attacked
        /// </summary>
        /// <param name="eColor">           King's color to check</param>
        /// <param name="iKingPos">         Position of the king</param>
        /// <returns>
        /// true if in check
        /// </returns>
        private bool IsCheck(PlayerE eColor, int iKingPos) {
            return(EnumAttackPos(eColor, iKingPos, null) != 0);
        }

        /// <summary>
        /// Determine if the specified king is attacked
        /// </summary>
        /// <param name="eColor">           King's color to check</param>
        /// <returns>
        /// true if in check
        /// </returns>
        public bool IsCheck(PlayerE eColor) {
            return(IsCheck(eColor, (eColor == PlayerE.Black) ? m_iBlackKingPos : m_iWhiteKingPos));
        }

        /// <summary>
        /// Evaluates a board. The number of point is greater than 0 if white is in advantage, less than 0 if black is.
        /// </summary>
        /// <param name="searchMode">       Search mode</param>
        /// <param name="ePlayerToPlay">    Color of the player to play</param>
        /// <param name="iDepth">           Depth of the search</param>
        /// <param name="iMoveCountDelta">  White move count - Black move count</param>
        /// <param name="posInfoWhite">     Information about pieces attack</param>
        /// <param name="posInfoBlack">     Information about pieces attack</param>
        /// <returns>
        /// Number of points for the current board
        /// </returns>
        public int Points(SearchMode searchMode, PlayerE ePlayerToPlay, int iDepth, int iMoveCountDelta, PosInfoS posInfoWhite, PosInfoS posInfoBlack) {
            int                 iRetVal;
            IBoardEvaluation    boardEval;
            PosInfoS            posInfoTmp;
            
            if (ePlayerToPlay == PlayerE.White) {
                boardEval   = searchMode.m_boardEvaluationWhite;
                posInfoTmp  = posInfoWhite;
            } else {
                boardEval                       = searchMode.m_boardEvaluationBlack;
                posInfoTmp.m_iAttackedPieces    = -posInfoBlack.m_iAttackedPieces;
                posInfoTmp.m_iPiecesDefending   = -posInfoBlack.m_iPiecesDefending;
            }
            iRetVal   = boardEval.Points(m_pBoard, m_piPiecesCount, posInfoTmp, m_iWhiteKingPos, m_iBlackKingPos, m_bWhiteCastle, m_bBlackCastle, iMoveCountDelta);
            return(iRetVal);
        }

        /// <summary>
        /// Add a move to the move list if the move doesn't provokes the king to be attacked.
        /// </summary>
        /// <param name="ePlayerColor">     Color doing the the move</param>
        /// <param name="iStartPos">        Starting position</param>
        /// <param name="iEndPos">          Ending position</param>
        /// <param name="eType">            type of the move</param>
        /// <param name="listMovePos">      List of moves</param>
        private void AddIfNotCheck(PlayerE ePlayerColor, int iStartPos, int iEndPos, Move.TypeE eType, List<Move> listMovePos) {
            PieceE      eNewPiece;
            PieceE      eOldPiece;
            Move        tMove;
            bool        bIsCheck;
            
            eOldPiece           = m_pBoard[iEndPos];
            eNewPiece           = m_pBoard[iStartPos];
            m_pBoard[iEndPos]   = eNewPiece;
            m_pBoard[iStartPos] = PieceE.None;
            bIsCheck            = ((eNewPiece & PieceE.PieceMask) == PieceE.King) ? IsCheck(ePlayerColor, iEndPos) : IsCheck(ePlayerColor);
            m_pBoard[iStartPos] = m_pBoard[iEndPos];
            m_pBoard[iEndPos]   = eOldPiece;
            if (!bIsCheck) {
                tMove.OriginalPiece  = m_pBoard[iEndPos];
                tMove.StartPos       = (byte)iStartPos;
                tMove.EndPos         = (byte)iEndPos;
                tMove.Type           = eType;
                if (m_pBoard[iEndPos] != PieceE.None || eType == Move.TypeE.EnPassant) {
                    tMove.Type |= Move.TypeE.PieceEaten;
                    m_posInfo.m_iAttackedPieces++;
                }
                if (listMovePos != null) {
                    listMovePos.Add(tMove);
                }
            }
        }

        /// <summary>
        /// Add a pawn promotion series of moves to the move list if the move doesn't provokes the king to be attacked.
        /// </summary>
        /// <param name="ePlayerColor">     Color doing the the move</param>
        /// <param name="iStartPos">        Starting position</param>
        /// <param name="iEndPos">          Ending position</param>
        /// <param name="listMovePos">      List of moves</param>
        private void AddPawnPromotionIfNotCheck(PlayerE ePlayerColor, int iStartPos, int iEndPos, List<Move> listMovePos) {
            AddIfNotCheck(ePlayerColor, iStartPos, iEndPos, Move.TypeE.PawnPromotionToQueen,  listMovePos);
            AddIfNotCheck(ePlayerColor, iStartPos, iEndPos, Move.TypeE.PawnPromotionToRook,   listMovePos);
            AddIfNotCheck(ePlayerColor, iStartPos, iEndPos, Move.TypeE.PawnPromotionToBishop, listMovePos);
            AddIfNotCheck(ePlayerColor, iStartPos, iEndPos, Move.TypeE.PawnPromotionToKnight, listMovePos);
            AddIfNotCheck(ePlayerColor, iStartPos, iEndPos, Move.TypeE.PawnPromotionToPawn,   listMovePos);
        }

        /// <summary>
        /// Add a move to the move list if the new position is empty or is an enemy
        /// </summary>
        /// <param name="ePlayerColor">     Color doing the the move</param>
        /// <param name="iStartPos">        Starting position</param>
        /// <param name="iEndPos">          Ending position</param>
        /// <param name="listMovePos">      List of moves</param>
        private bool AddMoveIfEnemyOrEmpty(PlayerE ePlayerColor, int iStartPos, int iEndPos, List<Move> listMovePos) {
            bool        bRetVal;
            PieceE      eOldPiece;
            
            bRetVal     = (m_pBoard[iEndPos] == PieceE.None);
            eOldPiece   = m_pBoard[iEndPos];
            if (bRetVal ||((eOldPiece & PieceE.Black) != 0) != (ePlayerColor == PlayerE.Black)) {
                AddIfNotCheck(ePlayerColor, iStartPos, iEndPos, Move.TypeE.Normal, listMovePos);
            } else {
                m_posInfo.m_iPiecesDefending++;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Enumerates the castling move
        /// </summary>
        /// <param name="ePlayerColor"> Color doing the the move</param>
        /// <param name="listMovePos">  List of moves</param>
        private void EnumCastleMove(PlayerE ePlayerColor, List<Move> listMovePos) {
            if (ePlayerColor == PlayerE.Black) {
                if (!m_bBlackCastle) {
                    if (m_iBlackKingMoveCount == 0) {
                        if (m_iLBlackRookMoveCount == 0   &&
                            m_pBoard[57] == PieceE.None   &&
                            m_pBoard[58] == PieceE.None   &&
                            m_pBoard[56] == (PieceE.Rook | PieceE.Black)) {
                            if (EnumAttackPos(ePlayerColor, 58, null) == 0 &&
                                EnumAttackPos(ePlayerColor, 59, null) == 0) {
                                AddIfNotCheck(ePlayerColor, 59, 57, Move.TypeE.Castle, listMovePos);
                            }
                        }
                        if (m_iRBlackRookMoveCount == 0   &&
                            m_pBoard[60] == PieceE.None   &&
                            m_pBoard[61] == PieceE.None   &&
                            m_pBoard[62] == PieceE.None   &&
                            m_pBoard[63] == (PieceE.Rook | PieceE.Black)) {
                            if (EnumAttackPos(ePlayerColor, 59, null) == 0 &&
                                EnumAttackPos(ePlayerColor, 60, null) == 0) {
                                AddIfNotCheck(ePlayerColor, 59, 61, Move.TypeE.Castle, listMovePos);
                            }
                        }
                    }
                }
            } else {
                if (!m_bWhiteCastle) {
                    if (m_iWhiteKingMoveCount == 0) {
                        if (m_iLWhiteRookMoveCount == 0  &&
                            m_pBoard[1] == PieceE.None   &&
                            m_pBoard[2] == PieceE.None   &&
                            m_pBoard[0] == (PieceE.Rook | PieceE.White)) {
                            if (EnumAttackPos(ePlayerColor, 2, null) == 0 &&
                                EnumAttackPos(ePlayerColor, 3, null) == 0) {
                                AddIfNotCheck(ePlayerColor, 3, 1, Move.TypeE.Castle, listMovePos);
                            }
                        }
                        if (m_iRWhiteRookMoveCount == 0  &&
                            m_pBoard[4] == PieceE.None   &&
                            m_pBoard[5] == PieceE.None   &&
                            m_pBoard[6] == PieceE.None   &&
                            m_pBoard[7] == (PieceE.Rook | PieceE.White)) {
                            if (EnumAttackPos(ePlayerColor, 3, null) == 0 &&
                                EnumAttackPos(ePlayerColor, 4, null) == 0) {
                                AddIfNotCheck(ePlayerColor, 3, 5, Move.TypeE.Castle, listMovePos);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the move a specified pawn can do
        /// </summary>
        /// <param name="ePlayerColor">     Color doing the the move</param>
        /// <param name="iStartPos">        Pawn position</param>
        /// <param name="listMovePos">      List of moves</param>
        private void EnumPawnMove(PlayerE ePlayerColor, int iStartPos, List<Move> listMovePos) {
            int         iDir;
            int         iNewPos;
            int         iNewColPos;
            int         iRowPos;
            bool        bCanMove2Case;
            
            iRowPos             = (iStartPos >> 3);
            bCanMove2Case       = (ePlayerColor == PlayerE.Black) ? (iRowPos == 6) : (iRowPos == 1);
            iDir                = (ePlayerColor == PlayerE.Black) ? -8 : 8;
            iNewPos             = iStartPos + iDir;
            if (iNewPos >= 0 && iNewPos < 64) {
                if (m_pBoard[iNewPos] == PieceE.None) {
                    iRowPos = (iNewPos >> 3);
                    if (iRowPos == 0 || iRowPos == 7) {
                        AddPawnPromotionIfNotCheck(ePlayerColor, iStartPos, iNewPos, listMovePos);
                    } else {
                        AddIfNotCheck(ePlayerColor, iStartPos, iNewPos, Move.TypeE.Normal, listMovePos);
                    }
                    if (bCanMove2Case && m_pBoard[iNewPos+iDir] == PieceE.None) {
                        AddIfNotCheck(ePlayerColor, iStartPos, iNewPos+iDir, Move.TypeE.Normal, listMovePos);
                    }
                }
            }
            iNewPos = iStartPos + iDir;
            if (iNewPos >= 0 && iNewPos < 64) {
                iNewColPos  = iNewPos & 7;
                iRowPos     = (iNewPos >> 3);
                if (iNewColPos != 0 && m_pBoard[iNewPos - 1] != PieceE.None) {
                    if (((m_pBoard[iNewPos - 1] & PieceE.Black) == 0) == (ePlayerColor == PlayerE.Black)) {
                        if (iRowPos == 0 || iRowPos == 7) {
                            AddPawnPromotionIfNotCheck(ePlayerColor, iStartPos, iNewPos - 1, listMovePos);
                        } else {
                            AddIfNotCheck(ePlayerColor, iStartPos, iNewPos - 1, Move.TypeE.Normal, listMovePos);
                        }
                    } else {
                        m_posInfo.m_iPiecesDefending++;
                    }
                }
                if (iNewColPos != 7 && m_pBoard[iNewPos + 1] != PieceE.None) {
                    if (((m_pBoard[iNewPos + 1] & PieceE.Black) == 0) == (ePlayerColor == PlayerE.Black)) {
                        if (iRowPos == 0 || iRowPos == 7) {
                            AddPawnPromotionIfNotCheck(ePlayerColor, iStartPos, iNewPos + 1, listMovePos);
                        } else {
                            AddIfNotCheck(ePlayerColor, iStartPos, iNewPos + 1, Move.TypeE.Normal, listMovePos);
                        }
                    } else {
                        m_posInfo.m_iPiecesDefending++;
                    }
                }
            }            
        }

        /// <summary>
        /// Enumerates the en passant move
        /// </summary>
        /// <param name="ePlayerColor">     Color doing the the move</param>
        /// <param name="listMovePos">      List of moves</param>
        private void EnumEnPassant(PlayerE ePlayerColor, List<Move> listMovePos) {
            int     iColPos;
            PieceE  eAttackingPawn;
            PieceE  ePawnInDanger;
            int     iPosBehindPawn;
            int     iPosPawnInDanger;
            
            if (m_iPossibleEnPassantAt != 0) {
                iPosBehindPawn      = m_iPossibleEnPassantAt;
                if (ePlayerColor == PlayerE.White) {
                    iPosPawnInDanger    = iPosBehindPawn - 8;
                    eAttackingPawn      = PieceE.Pawn | PieceE.White;
                } else {
                    iPosPawnInDanger    = iPosBehindPawn + 8;
                    eAttackingPawn      = PieceE.Pawn | PieceE.Black;
                }
                ePawnInDanger       = m_pBoard[iPosPawnInDanger];
                // Check if there is an attacking pawn at the left
                iColPos             = iPosPawnInDanger & 7;
                if (iColPos > 0 && m_pBoard[iPosPawnInDanger - 1] == eAttackingPawn) {
                    m_pBoard[iPosPawnInDanger] = PieceE.None;
                    AddIfNotCheck(ePlayerColor,
                                  iPosPawnInDanger - 1,
                                  iPosBehindPawn,
                                  Move.TypeE.EnPassant,
                                  listMovePos);
                    m_pBoard[iPosPawnInDanger] = ePawnInDanger;
                }
                if (iColPos < 7 && m_pBoard[iPosPawnInDanger+1] == eAttackingPawn) {
                    m_pBoard[iPosPawnInDanger] = PieceE.None;
                    AddIfNotCheck(ePlayerColor,
                                  iPosPawnInDanger + 1,
                                  iPosBehindPawn,
                                  Move.TypeE.EnPassant,
                                  listMovePos);
                    m_pBoard[iPosPawnInDanger] = ePawnInDanger;
                }
            }
        }

        /// <summary>
        /// Enumerates the move a specified piece can do using the pre-compute move array
        /// </summary>
        /// <param name="ePlayerColor">             Color doing the the move</param>
        /// <param name="iStartPos">                Starting position</param>
        /// <param name="ppiMoveListForThisCase">   Array of array of possible moves</param>
        /// <param name="listMovePos">              List of moves</param>
        private void EnumFromArray(PlayerE ePlayerColor, int iStartPos, int[][] ppiMoveListForThisCase, List<Move> listMovePos) {
            foreach (int[] piMovePosForThisDiag in ppiMoveListForThisCase) {
                foreach (int iNewPos in piMovePosForThisDiag) {
                    if (!AddMoveIfEnemyOrEmpty(ePlayerColor, iStartPos, iNewPos, listMovePos)) {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the move a specified piece can do using the pre-compute move array
        /// </summary>
        /// <param name="ePlayerColor">             Color doing the the move</param>
        /// <param name="iStartPos">                Starting position</param>
        /// <param name="piMoveListForThisCase">    Array of possible moves</param>
        /// <param name="listMovePos">              List of moves</param>
        private void EnumFromArray(PlayerE ePlayerColor, int iStartPos, int[] piMoveListForThisCase, List<Move> listMovePos) {
            foreach (int iNewPos in piMoveListForThisCase) {
                AddMoveIfEnemyOrEmpty(ePlayerColor, iStartPos, iNewPos, listMovePos);
            }
        }

        /// <summary>
        /// Enumerates all the possible moves for a player
        /// </summary>
        /// <param name="ePlayerColor">             Color doing the the move</param>
        /// <param name="bMoveList">                true to returns a MoveList</param>
        /// <param name="posInfo">                  Structure to fill with pieces information</param>
        /// <returns>
        /// List of possible moves or null
        /// </returns>
        public List<Move> EnumMoveList(PlayerE ePlayerColor, bool bMoveList, out PosInfoS posInfo) {
            PieceE      ePiece;
            List<Move>  listMovePos;
            bool        bBlackToMove;

            m_posInfo.m_iAttackedPieces   = 0;
            m_posInfo.m_iPiecesDefending  = 0;
            listMovePos                   = (bMoveList) ? new List<Move>(256) : null;
            bBlackToMove                  = (ePlayerColor == PlayerE.Black);
            for (int iIndex = 0; iIndex < 64; iIndex++) {
                ePiece = m_pBoard[iIndex];
                if (ePiece != PieceE.None && ((ePiece & PieceE.Black) != 0) == bBlackToMove) {
                    switch(ePiece & PieceE.PieceMask) {
                    case PieceE.Pawn:
                        EnumPawnMove(ePlayerColor, iIndex, listMovePos);
                        break;
                    case PieceE.Knight:
                        EnumFromArray(ePlayerColor, iIndex, s_ppiCaseMoveKnight[iIndex], listMovePos);
                        break;
                    case PieceE.Bishop:
                        EnumFromArray(ePlayerColor, iIndex, s_pppiCaseMoveDiagonal[iIndex], listMovePos);
                        break;
                    case PieceE.Rook:
                        EnumFromArray(ePlayerColor, iIndex, s_pppiCaseMoveLine[iIndex], listMovePos);
                        break;
                    case PieceE.Queen:
                        EnumFromArray(ePlayerColor, iIndex, s_pppiCaseMoveDiagLine[iIndex], listMovePos);
                        break;
                    case PieceE.King:
                        EnumFromArray(ePlayerColor, iIndex, s_ppiCaseMoveKing[iIndex], listMovePos);
                        break;
                    }
                }
            }
            EnumCastleMove(ePlayerColor, listMovePos);
            EnumEnPassant(ePlayerColor, listMovePos);
            posInfo = m_posInfo;
            return(listMovePos);
        }

        /// <summary>
        /// Enumerates all the possible moves for a player
        /// </summary>
        /// <param name="ePlayerColor">             Color doing the the move</param>
        /// <returns>
        /// List of possible moves
        /// </returns>
        public List<Move> EnumMoveList(PlayerE ePlayerColor) {
            PosInfoS    posInfo;
            
            return(EnumMoveList(ePlayerColor, true, out posInfo));
        }

        /// <summary>
        /// Enumerates all the possible moves for a player
        /// </summary>
        /// <param name="ePlayerColor">             Color doing the the move</param>
        /// <param name="posInfo">                  Structure to fill with pieces information</param>
        public void ComputePiecesCoverage(PlayerE ePlayerColor, out PosInfoS posInfo) {
            EnumMoveList(ePlayerColor, false, out posInfo);
        }

        /// <summary>
        /// Cancel search
        /// </summary>
        public void CancelSearch() {
            SearchEngine.CancelSearch();
        }

        /// <summary>
        /// Find the best move for the given player
        /// </summary>
        /// <param name="ePlayer">          Player making the move</param>
        /// <param name="searchMode">       Search mode</param>
        /// <param name="dispatcher">       Main thread dispatcher</param>
        /// <param name="actionMoveFound">  Action to execute when the best move is found</param>
        /// <param name="cookie">           Action cookie</param>
        /// <returns>
        /// true if search has started, false if search engine is busy
        /// </returns>
        public bool FindBestMove<T>(ChessBoard.PlayerE  ePlayer,
                                    SearchMode          searchMode,
                                    Dispatcher          dispatcher,
                                    Action<T,MoveExt>   actionMoveFound,
                                    T                   cookie) {
            bool    bRetVal;

            bRetVal = SearchEngine.FindBestMove(m_trace,
                                                m_rnd,
                                                m_rndRep,
                                                this,
                                                searchMode,
                                                ePlayer,
                                                dispatcher,
                                                actionMoveFound,
                                                cookie);
            return(bRetVal);
        }

        /// <summary>
        /// Find type of pawn promotion are valid for the specified starting/ending position
        /// </summary>
        /// <param name="ePlayerColor">     Color doing the move</param>
        /// <param name="iStartPos">        Position to start</param>
        /// <param name="iEndPos">          Ending position</param>
        /// <returns>
        /// None or a combination of Queen, Rook, Bishop, Knight and Pawn
        /// </returns>
        public ValidPawnPromotionE FindValidPawnPromotion(PlayerE ePlayerColor, int iStartPos, int iEndPos) {
            ValidPawnPromotionE eRetVal = ValidPawnPromotionE.None;
            List<Move>          moveList;

            moveList = EnumMoveList(ePlayerColor);
            foreach (Move move in moveList) {
                if (move.StartPos == iStartPos && move.EndPos == iEndPos) {
                    switch(move.Type & Move.TypeE.MoveTypeMask) {
                    case Move.TypeE.PawnPromotionToQueen:
                        eRetVal |= ValidPawnPromotionE.Queen;
                        break;
                    case Move.TypeE.PawnPromotionToRook:
                        eRetVal |= ValidPawnPromotionE.Rook;
                        break;
                    case Move.TypeE.PawnPromotionToBishop:
                        eRetVal |= ValidPawnPromotionE.Bishop;
                        break;
                    case Move.TypeE.PawnPromotionToKnight:
                        eRetVal |= ValidPawnPromotionE.Knight;
                        break;
                    case Move.TypeE.PawnPromotionToPawn:
                        eRetVal |= ValidPawnPromotionE.Pawn;
                        break;
                    default:
                        break;
                    }
                }
            }
            return(eRetVal);
        }        

        /// <summary>
        /// Find a move from the valid move list
        /// </summary>
        /// <param name="ePlayerColor">     Color doing the move</param>
        /// <param name="iStartPos">        Position to start</param>
        /// <param name="iEndPos">          Ending position</param>
        /// <returns>
        /// Move or -1
        /// </returns>
        public Move FindIfValid(PlayerE ePlayerColor, int iStartPos, int iEndPos) {
            Move        tMoveRetVal;
            List<Move>  moveList;
            int         iIndex;

            moveList    = EnumMoveList(ePlayerColor);
            iIndex      = moveList.FindIndex(x => x.StartPos == iStartPos && x.EndPos == iEndPos);
            if (iIndex == -1) {
                tMoveRetVal.StartPos        = 255;
                tMoveRetVal.EndPos          = 255;
                tMoveRetVal.OriginalPiece   = PieceE.None;
                tMoveRetVal.Type            = Move.TypeE.Normal;
            } else {
                tMoveRetVal                 = moveList[iIndex];
            }
            return(tMoveRetVal);
        }        

        /// <summary>
        /// Find a move from the valid move list
        /// </summary>
        /// <param name="ePlayerColor">     Color doing the move</param>
        /// <param name="move">             Move to validate</param>
        /// <returns>
        /// true if valid, false if not
        /// </returns>
        public bool IsMoveValid(PlayerE ePlayerColor, Move move) {
            bool        bRetVal;
            List<Move>  moveList;
            int         iIndex;

            moveList    = EnumMoveList(ePlayerColor);
            iIndex      = moveList.FindIndex(x => x.StartPos == move.StartPos && x.EndPos == move.EndPos);
            bRetVal     = (iIndex != -1);
            return(bRetVal);
        }        

        /// <summary>
        /// Find a move from the valid move list
        /// </summary>
        /// <param name="move">             Move to validate</param>
        /// <returns>
        /// true if valid, false if not
        /// </returns>
        public bool IsMoveValid(Move move) {
            bool        bRetVal;

            bRetVal = IsMoveValid(CurrentPlayer, move);
            return(bRetVal);
        }        

        /// <summary>
        /// Find a move from the opening book
        /// </summary>
        /// <param name="book">             Book to use</param>
        /// <param name="searchMode">       Search mode</param>
        /// <param name="ePlayerColor">     Color doing the move</param>
        /// <param name="arrPrevMove">      Previous move</param>
        /// <param name="move">             Found move</param>
        /// <returns>
        /// true if succeed, false if no move found in book
        /// </returns>
        public bool FindBookMove(Book book, SearchMode searchMode, PlayerE ePlayerColor, MoveExt[] arrPrevMove, out Move move) {
            bool        bRetVal;
            int         iMove;
            Random      rnd;
            
            if (searchMode.m_eRandomMode == SearchMode.RandomModeE.Off) {
                rnd = null;
            } else if (searchMode.m_eRandomMode == SearchMode.RandomModeE.OnRepetitive) {
                rnd = m_rndRep;
            } else {
                rnd = m_rnd;
            }
            move.OriginalPiece  = PieceE.None;
            move.StartPos       = 255;
            move.EndPos         = 255;
            move.Type           = Move.TypeE.Normal;
            iMove               = book.FindMoveInBook(arrPrevMove, rnd);
            if (iMove == -1) {
                bRetVal = false;
            } else {
                move        = FindIfValid(ePlayerColor, iMove & 255, iMove >> 8);
                move.Type  |= Move.TypeE.MoveFromBook;
                bRetVal     = (move.StartPos != 255);
            }
            return(bRetVal);
        }

        /// <summary>
        /// Undo all the specified move starting with the last move
        /// </summary>
        public void UndoAllMoves() {
            while (m_moveStack.PositionInList != -1) {
                UndoMove();
            }
        }

        /// <summary>
        /// Gets the position express in a human form
        /// </summary>
        /// <param name="iPos">     Position</param>
        /// <returns>
        /// Human form position
        /// </returns>
        static public string GetHumanPos(int iPos) {
            string  strRetVal;
            int     iColPos;
            int     iRowPos;
            
            iColPos     = 7 - (iPos & 7);
            iRowPos     = iPos >> 3;
            strRetVal   = ((Char)(iColPos + 'A')).ToString() + ((Char)(iRowPos + '1')).ToString();
            return(strRetVal);
        }

        /// <summary>
        /// Gets the position express in a human form
        /// </summary>
        /// <param name="move">     Move</param>
        /// <returns>
        /// Human form position
        /// </returns>
        static public string GetHumanPos(MoveExt move) {
            string  strRetVal;
            
            strRetVal  = GetHumanPos(move.Move.StartPos);
            strRetVal += ((move.Move.Type & Move.TypeE.PieceEaten) == Move.TypeE.PieceEaten) ? "x" : "-";
            strRetVal += GetHumanPos(move.Move.EndPos);

            if ((move.Move.Type & Move.TypeE.MoveFromBook) == Move.TypeE.MoveFromBook) {
                strRetVal = "(" + strRetVal + ")";
            }
            switch(move.Move.Type & Move.TypeE.MoveTypeMask) {
            case Move.TypeE.PawnPromotionToQueen:
                strRetVal += "=Q";
                break;
            case Move.TypeE.PawnPromotionToRook:
                strRetVal += "=R";
                break;
            case Move.TypeE.PawnPromotionToBishop:
                strRetVal += "=B";
                break;
            case Move.TypeE.PawnPromotionToKnight:
                strRetVal += "=N";
                break;
            case Move.TypeE.PawnPromotionToPawn:
                strRetVal += "=P";
                break;
            default:
                break;
            }
            return(strRetVal);
        }
    } // Class ChessBoard

    /// <summary>Chess exception</summary>
    [Serializable]
    public class ChessException : System.Exception {
        /// <summary>
        /// Class constructor
        /// </summary>
        public ChessException() : base() {
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="strError"> Error</param>
        public ChessException(string strError) : base(strError) {
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="strError"> Error</param>
        /// <param name="ex">       Inner exception</param>
        public ChessException(string strError, Exception ex) : base(strError, ex) {
        }

        /// <summary>
        /// Serialization Ctor
        /// </summary>
        /// <param name="info">     Serialization info</param>
        /// <param name="context">  Streaming context</param>
        protected ChessException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    } // Class ChessException
} // Namespace
