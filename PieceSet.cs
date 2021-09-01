using System.Windows.Controls;

namespace SrcChess2 {
    /// <summary>
    /// Defines a set of chess pieces. A piece set is a set of xaml which defines the representation of each pieces
    /// </summary>
    public abstract class PieceSet {

        /// <summary>
        /// List of standard pieces
        /// </summary>
        protected enum ChessPiece {
            /// <summary>No Piece</summary>
            None            = -1,
            /// <summary>Black Pawn</summary>
            Black_Pawn      = 0,
            /// <summary>Black Rook</summary>
            Black_Rook      = 1,
            /// <summary>Black Bishop</summary>
            Black_Bishop    = 2,
            /// <summary>Black Knight</summary>
            Black_Knight    = 3,
            /// <summary>Black Queen</summary>
            Black_Queen     = 4,
            /// <summary>Black King</summary>
            Black_King      = 5,
            /// <summary>White Pawn</summary>
            White_Pawn      = 6,
            /// <summary>White Rook</summary>
            White_Rook      = 7,
            /// <summary>White Bishop</summary>
            White_Bishop    = 8,
            /// <summary>White Knight</summary>
            White_Knight    = 9,
            /// <summary>White Queen</summary>
            White_Queen     = 10,
            /// <summary>White King</summary>
            White_King      = 11
        };

        
        /// <summary>Name of the piece set</summary>
        public      string              Name { get; private set; }

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="strName">  Piece set Name</param>
        protected PieceSet(string strName) {
            Name                = strName;
        }

        /// <summary>
        /// Transform a ChessBoard piece into a ChessPiece enum
        /// </summary>
        /// <param name="ePiece"></param>
        /// <returns></returns>
        private static ChessPiece GetChessPieceFromPiece(ChessBoard.PieceE ePiece) {
            ChessPiece  eRetVal;

            switch (ePiece) {
            case ChessBoard.PieceE.Pawn | ChessBoard.PieceE.White:
                eRetVal = ChessPiece.White_Pawn;
                break;
            case ChessBoard.PieceE.Knight | ChessBoard.PieceE.White:
                eRetVal = ChessPiece.White_Knight;
                break;
            case ChessBoard.PieceE.Bishop | ChessBoard.PieceE.White:
                eRetVal = ChessPiece.White_Bishop;
                break;
            case ChessBoard.PieceE.Rook | ChessBoard.PieceE.White:
                eRetVal = ChessPiece.White_Rook;
                break;
            case ChessBoard.PieceE.Queen | ChessBoard.PieceE.White:
                eRetVal = ChessPiece.White_Queen;
                break;
            case ChessBoard.PieceE.King | ChessBoard.PieceE.White:
                eRetVal = ChessPiece.White_King;
                break;
            case ChessBoard.PieceE.Pawn | ChessBoard.PieceE.Black:
                eRetVal = ChessPiece.Black_Pawn;
                break;
            case ChessBoard.PieceE.Knight | ChessBoard.PieceE.Black:
                eRetVal = ChessPiece.Black_Knight;
                break;
            case ChessBoard.PieceE.Bishop | ChessBoard.PieceE.Black:
                eRetVal = ChessPiece.Black_Bishop;
                break;
            case ChessBoard.PieceE.Rook | ChessBoard.PieceE.Black:
                eRetVal = ChessPiece.Black_Rook;
                break;
            case ChessBoard.PieceE.Queen | ChessBoard.PieceE.Black:
                eRetVal = ChessPiece.Black_Queen;
                break;
            case ChessBoard.PieceE.King | ChessBoard.PieceE.Black:
                eRetVal = ChessPiece.Black_King;
                break;
            default:
                eRetVal = ChessPiece.None;
                break;
            }
            return(eRetVal);
        }

        /// <summary>
        /// Load a new piece
        /// </summary>
        /// <param name="eChessPiece">  Chess Piece</param>
        protected abstract UserControl LoadPiece(ChessPiece eChessPiece);

        /// <summary>
        /// Gets the specified piece
        /// </summary>
        /// <param name="ePiece"></param>
        /// <returns>
        /// User control expressing the piece
        /// </returns>
        public UserControl this[ChessBoard.PieceE ePiece] {
            get {
                UserControl userControlRetVal;
                ChessPiece  eChessPiece;

                eChessPiece         = GetChessPieceFromPiece(ePiece);
                userControlRetVal   = (eChessPiece == ChessPiece.None) ? null : LoadPiece(eChessPiece);
                return(userControlRetVal);
            }
        }
    } // Class PieceSet
} // Namespace
