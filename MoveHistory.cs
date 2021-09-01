using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace SrcChess2 {
    /// <summary>
    /// Maintains a move history to handle the fifty-move rule and the threefold repetition rule.
    /// 
    /// For the first rules, we just maintains one move count per series of move which doesn't eat a piece or move a pawn.
    /// For the second rules, we use two strategies, a fast but unreliable one and a second slower but exact.
    ///
    ///     A.  Use two 16KB table of counter address by table[Zobrist key of the board mod 16KB]. Collision can occurs so its
    ///         only a good indication that the board can be there more than 2 times.
    ///     B.  Keep a compressed representation of the board in an array to be able to count the number of identical boards.
    /// 
    /// </summary>
    public sealed class MoveHistory : IXmlSerializable {

        /// <summary>
        /// Each pawn can move a maximum of 6 times, there is 31 pieces which can be eaten. So no more than
        /// 127 times the AddCurrentMove can be called with bPawnMoveOrPieceEaten set without undo being done on it</summary>
        private const int IMax50CounterDepth = 130;
        
        /// <summary>
        /// Packed representation of a board. Each long contains 16 pieces (2 per bytes)
        /// </summary>
        public struct PackedBoard {
            /// <summary>Pieces from square 0-15</summary>
            public long                         m_lVal1;
            /// <summary>Pieces from square 16-31</summary>
            public long                         m_lVal2;
            /// <summary>Pieces from square 32-47</summary>
            public long                         m_lVal3;
            /// <summary>Pieces from square 48-63</summary>
            public long                         m_lVal4;
            /// <summary>Additional board info</summary>
            public ChessBoard.BoardStateMaskE   m_eInfo;
            /// <summary>
            /// Save the structure in a binary writer
            /// </summary>
            /// <param name="writer">   Binary writer</param>
            public void SaveToStream(System.IO.BinaryWriter writer) {
                writer.Write(m_lVal1);
                writer.Write(m_lVal2);
                writer.Write(m_lVal3);
                writer.Write(m_lVal4);
                writer.Write((int)m_eInfo);
            }
            /// <summary>
            /// Load the structure from a binary reader
            /// </summary>
            /// <param name="reader">   Binary reader</param>
            public void LoadFromStream(System.IO.BinaryReader reader) {
                m_lVal1 = reader.ReadInt64();
                m_lVal2 = reader.ReadInt64();
                m_lVal3 = reader.ReadInt64();
                m_lVal4 = reader.ReadInt64();
                m_eInfo = (ChessBoard.BoardStateMaskE)reader.ReadInt32();
            }
        };
        
        /// <summary>Current packed board representation</summary>
        private PackedBoard     m_packedBoardCurrent;
        /// <summary>Number of moves in the history</summary>
        private int             m_iMoveCount;
        /// <summary>Size of the packed board array</summary>
        private int             m_iPackedBoardArraySize;
        /// <summary>Array of packed boards</summary>
        private PackedBoard[]   m_arrPackedBoard;
        /// <summary>Array of byte containing the count of each board identified by a Zobrist key.</summary>
        private byte[]          m_arrHashCount;
        /// <summary>Depth of current count move. Up to IMax50CounterDepth - 1</summary>
        private int             m_iCountMoveDepth;
        /// <summary>Array of count move</summary>
        private short[]         m_arrCountMove;

        /// <summary>
        /// Class constructor
        /// </summary>
        public MoveHistory() {
            m_iMoveCount            = 0;
            m_iPackedBoardArraySize = 512;
            m_arrPackedBoard        = new PackedBoard[m_iPackedBoardArraySize];
            m_arrHashCount          = new byte[16384];
            m_iCountMoveDepth       = 0;
            m_arrCountMove          = new short[IMax50CounterDepth];
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="moveHistory">  MoveHistory template</param>
        private MoveHistory(MoveHistory moveHistory) {
            m_iMoveCount            = moveHistory.m_iMoveCount;
            m_iCountMoveDepth       = moveHistory.m_iCountMoveDepth;
            m_iPackedBoardArraySize = moveHistory.m_iPackedBoardArraySize;
            m_arrPackedBoard        = (PackedBoard[])moveHistory.m_arrPackedBoard.Clone();
            m_arrHashCount          = (byte[])moveHistory.m_arrHashCount.Clone();
            m_arrCountMove          = (short[])moveHistory.m_arrCountMove.Clone();
            m_packedBoardCurrent    = moveHistory.m_packedBoardCurrent;
        }

        /// <summary>
        /// Creates a clone of the MoveHistory
        /// </summary>
        /// <returns>
        /// A new clone of the MoveHistory
        /// </returns>
        public MoveHistory Clone() {
            return(new MoveHistory(this));
        }

        /// <summary>
        /// Returns the XML schema if any
        /// </summary>
        /// <returns>
        /// null
        /// </returns>
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {
            return(null);
        }

        /// <summary>
        /// Deserialize the object from a XML reader
        /// </summary>
        /// <param name="reader">   XML reader</param>
        void IXmlSerializable.ReadXml(System.Xml.XmlReader reader) {
            MemoryStream    memStream;
            BinaryReader    binReader;
            byte[]          bytes;
            int             iCount;

            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "MoveHistory") {
                throw new SerializationException("Unknown format");
            } else {
                memStream   = new MemoryStream(32768);
                bytes       = new byte[32768];
                do {
                    iCount = reader.ReadElementContentAsBinHex(bytes, 0, bytes.Length);
                    if (iCount != 0) {
                        memStream.Write(bytes, 0, iCount);
                    }
                } while (iCount != 0);
                memStream.Seek(0, SeekOrigin.Begin);
                binReader   = new BinaryReader(memStream);
                using (binReader) { 
                    LoadFromStream(binReader);
                }
            }
        }

        /// <summary>
        /// Serialize the object to a XML writer
        /// </summary>
        /// <param name="writer">   XML writer</param>
        void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) {
            MemoryStream    memStream;
            BinaryWriter    binWriter;
            byte[]          bytes;


            memStream   = new MemoryStream(32768);
            binWriter   = new BinaryWriter(memStream);
            SaveToStream(binWriter);
            bytes   = memStream.GetBuffer();
            writer.WriteStartElement("MoveHistory");
            writer.WriteBinHex(bytes, 0, (int)memStream.Length);
            writer.WriteEndElement();
        }


        /// <summary>
        /// Load from stream
        /// </summary>
        /// <param name="reader">   Binary reader</param>
        public void LoadFromStream(System.IO.BinaryReader reader) {
            int                 iNewSize;
            ChessBoard.PieceE[] peBoard;
            PackedBoard         packedBoard;
            long                l64ZobristKey;
            
            packedBoard.m_lVal1 = 0;
            packedBoard.m_lVal2 = 0;
            packedBoard.m_lVal3 = 0;
            packedBoard.m_lVal4 = 0;
            packedBoard.m_eInfo = (ChessBoard.BoardStateMaskE)0;
            Array.Clear(m_arrHashCount, 0, m_arrHashCount.Length);
            m_packedBoardCurrent.LoadFromStream(reader);
            peBoard         = new ChessBoard.PieceE[64];
            m_iMoveCount    = reader.ReadInt32();
            iNewSize        = m_iPackedBoardArraySize;
            while (m_iMoveCount > iNewSize) {
                iNewSize *= 2;
            }
            if (iNewSize != m_iPackedBoardArraySize) {
                m_arrPackedBoard        = new PackedBoard[iNewSize];
                m_iPackedBoardArraySize = iNewSize;
            }
            for (int iIndex = 0; iIndex < m_iMoveCount; iIndex++) {
                packedBoard.LoadFromStream(reader);
                m_arrPackedBoard[iIndex] = packedBoard;
                UnpackBoard(packedBoard, peBoard);
                l64ZobristKey = ZobristKey.ComputeBoardZobristKey(peBoard) ^ (int)packedBoard.m_eInfo;
                m_arrHashCount[l64ZobristKey & 16383]++;
            }
            m_iCountMoveDepth = reader.ReadInt32();
            for (int iIndex = 0; iIndex <= m_iCountMoveDepth; iIndex++) {
                m_arrCountMove[iIndex] = reader.ReadInt16();
            }
        }

        /// <summary>
        /// Save to stream
        /// </summary>
        /// <param name="writer">   Binary writer</param>
        public void SaveToStream(System.IO.BinaryWriter writer) {
            m_packedBoardCurrent.SaveToStream(writer);
            writer.Write(m_iMoveCount);
            for (int iIndex = 0; iIndex < m_iMoveCount; iIndex++) {
                m_arrPackedBoard[iIndex].SaveToStream(writer);
            }
            writer.Write(m_iCountMoveDepth);
            for (int iIndex = 0; iIndex <= m_iCountMoveDepth; iIndex++) {
                writer.Write(m_arrCountMove[iIndex]);
            }            
        }

        /// <summary>
        /// Determine if two boards are equal
        /// </summary>
        /// <param name="board1">   First board</param>
        /// <param name="board2">   Second board</param>
        /// <returns>
        /// true if equal, false if not
        /// </returns>
        private static bool IsTwoBoardEqual(PackedBoard board1, PackedBoard board2) {
            bool    bRetVal;
            
            if (((board1.m_eInfo | board2.m_eInfo) & ChessBoard.BoardStateMaskE.EnPassant) != 0) {
                bRetVal = false;
            } else {
                bRetVal = (board1.m_eInfo == board2.m_eInfo     &&
                           board1.m_lVal1 == board2.m_lVal1     &&
                           board1.m_lVal2 == board2.m_lVal2     &&
                           board1.m_lVal3 == board2.m_lVal3     &&
                           board1.m_lVal4 == board2.m_lVal4);
            }
            return(bRetVal);
        }

        /// <summary>
        /// Gets the number of time this board is in the history (for the same color)
        /// </summary>
        /// <param name="board">    Board</param>
        /// <returns>
        /// Count
        /// </returns>
        private int GetBoardCount(PackedBoard board) {
            int     iRetVal = 0;
            
            for (int iIndex = m_iMoveCount - 2; iIndex >= 0; iIndex -= 2) {
                if (IsTwoBoardEqual(board, m_arrPackedBoard[iIndex])) {
                    iRetVal++;
                }
            }
            return(iRetVal);
        }

        /// <summary>
        /// Add the current packed board to the history
        /// </summary>
        /// <param name="l64ZobristKey">            Zobrist key of the board</param>
        /// <param name="bPawnMoveOrPieceEaten">    true if a pawn has moved or a piece has been eaten</param>
        /// <returns>
        /// Result: NoRepeat, ThreeFoldRepeat or FiftyRuleRepeat
        /// </returns>
        public ChessBoard.RepeatResultE AddCurrentPackedBoard(long l64ZobristKey, bool bPawnMoveOrPieceEaten) {
            ChessBoard.RepeatResultE    eRetVal = ChessBoard.RepeatResultE.NoRepeat;
            int                         iHashIndex;
            int                         iNewArraySize;
            byte                        count;
            PackedBoard[]               arrNew;
            
            l64ZobristKey ^= (int)m_packedBoardCurrent.m_eInfo;
            if (m_iMoveCount >= m_iPackedBoardArraySize) {
                iNewArraySize           = m_iPackedBoardArraySize * 2;
                arrNew                  = new PackedBoard[iNewArraySize];
                Array.Copy(m_arrPackedBoard, arrNew, m_iPackedBoardArraySize);
                m_iPackedBoardArraySize = iNewArraySize;
            }
            iHashIndex                       = (int)(l64ZobristKey & 16383);
            count                            = ++m_arrHashCount[iHashIndex];
            m_arrHashCount[iHashIndex]       = count;
            if (bPawnMoveOrPieceEaten) {
                m_iCountMoveDepth++;
                m_arrCountMove[m_iCountMoveDepth] = 0;
            } else {
                if (++m_arrCountMove[m_iCountMoveDepth] >= 50) {
                    eRetVal = ChessBoard.RepeatResultE.FiftyRuleRepeat;
                } else {
                    // A count > 2 is only an indication that 3 or more identical board may exist
                    // because 2 non-identical board can share the same slot
                    if (count > 2 && GetBoardCount(m_packedBoardCurrent) >= 2) {
                        eRetVal = ChessBoard.RepeatResultE.ThreeFoldRepeat;
                    }
                }
            }
            m_arrPackedBoard[m_iMoveCount++] = m_packedBoardCurrent;
            return(eRetVal);
        }

        /// <summary>
        /// Add the current packed board to the history
        /// </summary>
        /// <param name="l64ZobristKey">    Zobrist key of the board</param>
        /// <returns>
        /// Result: NoRepeat, ThreeFoldRepeat or FiftyRuleRepeat
        /// </returns>
        public ChessBoard.RepeatResultE CurrentRepeatResult(long l64ZobristKey) {
            ChessBoard.RepeatResultE    eRetVal = ChessBoard.RepeatResultE.NoRepeat;
            int                         iHashIndex;
            byte                        count;
            
            iHashIndex  = (int)(l64ZobristKey & 16383);
            count = m_arrHashCount[iHashIndex];
            if (m_arrCountMove[m_iCountMoveDepth] >= 50) {
                eRetVal = ChessBoard.RepeatResultE.FiftyRuleRepeat;
            } else {
                // A count > 2 is only an indication that 3 or more identical board may exist
                // because 2 non-identical board can share the same slot
                if (count > 2 && GetBoardCount(m_packedBoardCurrent) >= 2) {
                    eRetVal = ChessBoard.RepeatResultE.ThreeFoldRepeat;
                }
            }
            return(eRetVal);
        }

        /// <summary>
        /// Get the current packed board count
        /// </summary>
        /// <returns>
        /// Count
        /// </returns>
        public int GetCurrentBoardCount(long l64ZobristKey) {
            int     iRetVal;
            int     iHashIndex;
            
            l64ZobristKey ^= (int)m_packedBoardCurrent.m_eInfo;
            iHashIndex     = (int)(l64ZobristKey & 16383);
            iRetVal        = m_arrHashCount[iHashIndex];
            if (iRetVal != 0) {
                iRetVal = GetBoardCount(m_packedBoardCurrent);
            }
            return(iRetVal);
        }

        /// <summary>
        /// Gets the current half move count (number of count since a pawn has been moved or a piece eaten)
        /// </summary>
        public int GetCurrentHalfMoveClock {
            get {
                return(m_arrCountMove[m_iCountMoveDepth]);
            }
        }
        
        /// <summary>
        /// Remove the last move from the history
        /// </summary>
        /// <param name="l64ZobristKey">    Zobrist key of the board</param>
        public void RemoveLastMove(long l64ZobristKey) {
            l64ZobristKey ^= (int)m_packedBoardCurrent.m_eInfo;
            m_arrHashCount[l64ZobristKey & 16383]--;
            m_iMoveCount--;
            if (m_arrCountMove[m_iCountMoveDepth] == 0) {
                m_iCountMoveDepth--;
            } else {
                m_arrCountMove[m_iCountMoveDepth]--;
            }
        }

        /// <summary>
        /// Compute a packed value of 16 pieces
        /// </summary>
        /// <param name="peBoard">              Board array</param>
        /// <param name="iStartPos">            Pieces starting position</param>
        /// <returns>
        /// Packed value of the 16 pieces
        /// </returns>
        private static long ComputePackedValue(ChessBoard.PieceE[] peBoard, int iStartPos) {
            long    lRetVal = 0;
            
            for (int iIndex = 0; iIndex < 16; iIndex++) {
                lRetVal |= ((long)peBoard[iStartPos + iIndex] & 15) << (iIndex << 2);
            }
            return(lRetVal);
        }

        /// <summary>
        /// Compute the packed representation of a board
        /// </summary>
        /// <param name="peBoard">              Board array</param>
        /// <param name="eInfo">                Board extra info</param>
        public static PackedBoard ComputePackedBoard(ChessBoard.PieceE[] peBoard, ChessBoard.BoardStateMaskE eInfo) {
            PackedBoard packedBoard;
            
            packedBoard.m_lVal1    = ComputePackedValue(peBoard, 0);
            packedBoard.m_lVal2    = ComputePackedValue(peBoard, 16);
            packedBoard.m_lVal3    = ComputePackedValue(peBoard, 32);
            packedBoard.m_lVal4    = ComputePackedValue(peBoard, 48);
            packedBoard.m_eInfo    = eInfo & ~ChessBoard.BoardStateMaskE.BlackToMove;
            return(packedBoard);
        }

        /// <summary>
        /// Compute the current packed representation of a board
        /// </summary>
        /// <param name="peBoard">              Board array</param>
        /// <param name="eInfo">                Board extra info</param>
        private void ComputeCurrentPackedBoard(ChessBoard.PieceE[] peBoard, ChessBoard.BoardStateMaskE eInfo) {
            m_packedBoardCurrent = ComputePackedBoard(peBoard, eInfo);
        }

        /// <summary>
        /// Unpack a packed board value to a board
        /// </summary>
        /// <param name="lVal">                 Packed board value</param>
        /// <param name="peBoard">              Board array</param>
        /// <param name="iStartPos">            Offset in the board</param>
        private static void UnpackBoardValue(long lVal, ChessBoard.PieceE[] peBoard, int iStartPos) {
            for (int iIndex = 0; iIndex < 16; iIndex++) {
                peBoard[iStartPos + iIndex] = (ChessBoard.PieceE)((lVal >> (iIndex << 2)) & 15);
            }
        }

        /// <summary>
        /// Unpack a packed board to a board
        /// </summary>
        /// <param name="packedBoard">          Packed board</param>
        /// <param name="peBoard">              Board array</param>
        public static void UnpackBoard(PackedBoard packedBoard, ChessBoard.PieceE[] peBoard) {
            UnpackBoardValue(packedBoard.m_lVal1, peBoard, 0);
            UnpackBoardValue(packedBoard.m_lVal2, peBoard, 16);
            UnpackBoardValue(packedBoard.m_lVal3, peBoard, 32);
            UnpackBoardValue(packedBoard.m_lVal4, peBoard, 48);
        }

        /// <summary>
        /// Reset the move history
        /// </summary>
        /// <param name="peBoard">              Board array</param>
        /// <param name="eInfo">                Board extra info</param>
        public void Reset(ChessBoard.PieceE[] peBoard, ChessBoard.BoardStateMaskE eInfo) {
            m_arrCountMove[0] = 0;
            m_iCountMoveDepth = 0;
            m_iMoveCount      = 0;
            Array.Clear(m_arrHashCount, 0, m_arrHashCount.Length);
            ComputeCurrentPackedBoard(peBoard, eInfo);
        }

        /// <summary>
        /// Update the current board packing
        /// </summary>
        /// <param name="iPos">                 Position of the new piece</param>
        /// <param name="eNewPiece">            New piece</param>
        public void UpdateCurrentPackedBoard(int iPos, ChessBoard.PieceE eNewPiece) {
            long    lNewPiece;
            long    lMask;
            int     iSlotInValue;
            
            iSlotInValue    = (iPos & 15) << 2;
            lNewPiece       = ((long)eNewPiece & 15) << iSlotInValue;
            lMask           = (long)15 << iSlotInValue;
            if (iPos < 16) {
                m_packedBoardCurrent.m_lVal1 = (m_packedBoardCurrent.m_lVal1 & ~lMask) | lNewPiece;
            } else if (iPos < 32) {
                m_packedBoardCurrent.m_lVal2 = (m_packedBoardCurrent.m_lVal2 & ~lMask) | lNewPiece;
            } else if (iPos < 48) {
                m_packedBoardCurrent.m_lVal3 = (m_packedBoardCurrent.m_lVal3 & ~lMask) | lNewPiece;
            } else {
                m_packedBoardCurrent.m_lVal4 = (m_packedBoardCurrent.m_lVal4 & ~lMask) | lNewPiece;
            }
        }

        /// <summary>
        /// Update the current board packing
        /// </summary>
        /// <param name="eInfo">        Board extra info</param>
        public void UpdateCurrentPackedBoard(ChessBoard.BoardStateMaskE eInfo) {
            m_packedBoardCurrent.m_eInfo = eInfo & ~ChessBoard.BoardStateMaskE.BlackToMove;
        }
    } // Class MoveHistory
} // Class name
