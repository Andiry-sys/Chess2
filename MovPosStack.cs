using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace SrcChess2 {
    /// <summary>Maintains the list of moves which has been done on a board. The undo moves are kept up to when a new move is done.</summary>
    public class MovePosStack : IXmlSerializable {
        /// <summary>List of move position</summary>
        private List<MoveExt>               m_listMovePos;
        /// <summary>Position of the current move in the list</summary>
        private int                         m_iPosInList;

        /// <summary>
        /// Class constructor
        /// </summary>
        public MovePosStack() {
            m_listMovePos = new List<MoveExt>(512);
            m_iPosInList  = -1;
        }

        /// <summary>
        /// Class constructor (copy constructor)
        /// </summary>
        private MovePosStack(MovePosStack movePosList) {
            m_listMovePos = new List<MoveExt>(movePosList.m_listMovePos);
            m_iPosInList  = movePosList.m_iPosInList;
        }

        /// <summary>
        /// Clone the stack
        /// </summary>
        /// <returns>
        /// Move list
        /// </returns>
        public MovePosStack Clone() {
            return(new MovePosStack(this));
        }

        /// <summary>
        /// Save to the specified binary writer
        /// </summary>
        /// <param name="writer">   Binary writer</param>
        public void SaveToWriter(System.IO.BinaryWriter writer) {
            writer.Write(m_listMovePos.Count);
            writer.Write(m_iPosInList);
            foreach (MoveExt move in m_listMovePos) {
                writer.Write((byte)move.Move.OriginalPiece);
                writer.Write(move.Move.StartPos);
                writer.Write(move.Move.EndPos);
                writer.Write((byte)move.Move.Type);
            }
        }

        /// <summary>
        /// Load from reader
        /// </summary>
        /// <param name="reader">   Binary Reader</param>
        public void LoadFromReader(System.IO.BinaryReader reader) {
            int     iMoveCount;
            Move    move;
            
            m_listMovePos.Clear();
            iMoveCount      = reader.ReadInt32();
            m_iPosInList    = reader.ReadInt32();
            for (int iIndex = 0; iIndex < iMoveCount; iIndex++) {
                move.OriginalPiece   = (ChessBoard.PieceE)reader.ReadByte();
                move.StartPos        = reader.ReadByte();
                move.EndPos          = reader.ReadByte();
                move.Type            = (Move.TypeE)reader.ReadByte();
                m_listMovePos.Add(new MoveExt(move));
            }
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
        /// Deserialize from XML
        /// </summary>
        /// <param name="reader">   XML reader</param>
        public void ReadXml(System.Xml.XmlReader reader) {
            Move    move;
            bool    bIsEmpty;

            m_listMovePos.Clear();
            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "MoveList") {
                throw new SerializationException("Unknown format");
            } else {
                bIsEmpty     = reader.IsEmptyElement;
                m_iPosInList = Int32.Parse(reader.GetAttribute("PositionInList"));
                if (bIsEmpty) {
                    reader.Read();
                } else {
                    if (reader.ReadToDescendant("Move")) {
                        while (reader.IsStartElement()) {
                            move                = new Move();
                            move.OriginalPiece  = (ChessBoard.PieceE)Enum.Parse(typeof(ChessBoard.SerPieceE), reader.GetAttribute("OriginalPiece"));
                            move.StartPos       = (byte)Int32.Parse(reader.GetAttribute("StartingPosition"));
                            move.EndPos         = (byte)Int32.Parse(reader.GetAttribute("EndingPosition"));
                            move.Type           = (Move.TypeE)Enum.Parse(typeof(Move.TypeE), reader.GetAttribute("MoveType"));
                            m_listMovePos.Add(new MoveExt(move));
                            reader.ReadStartElement("Move");
                        }
                    }
                    reader.ReadEndElement();
                }
            }
        }

        /// <summary>
        /// Serialize the move list to an XML writer
        /// </summary>
        /// <param name="writer">   XML writer</param>
        public void WriteXml(System.Xml.XmlWriter writer) {
            writer.WriteStartElement("MoveList");
            writer.WriteAttributeString("PositionInList", m_iPosInList.ToString());
            foreach (MoveExt move in m_listMovePos) {
                writer.WriteStartElement("Move");
                writer.WriteAttributeString("OriginalPiece",     ((ChessBoard.SerPieceE)move.Move.OriginalPiece).ToString());
                writer.WriteAttributeString("StartingPosition",  ((int)move.Move.StartPos).ToString());
                writer.WriteAttributeString("EndingPosition",    ((int)move.Move.EndPos).ToString());
                writer.WriteAttributeString("MoveType",          move.Move.Type.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// Count
        /// </summary>
        public int Count {
            get {
                return(m_listMovePos.Count);
            }
        }

        /// <summary>
        /// Indexer
        /// </summary>
        public MoveExt this[int iIndex] {
            get {
                return(m_listMovePos[iIndex]);
            }
        }

        /// <summary>
        /// Get the list of moves
        /// </summary>
        public List<MoveExt> List {
            get {
                return(m_listMovePos);
            }
        }

        /// <summary>
        /// Add a move to the stack. All redo move are discarded
        /// </summary>
        /// <param name="move"> New move</param>
        public void AddMove(MoveExt move) {
            int     iCount;
            int     iPos;
            
            iCount = Count;
            iPos   = m_iPosInList + 1;
            while (iCount != iPos) {
                m_listMovePos.RemoveAt(--iCount);
            }
            m_listMovePos.Add(move);
            m_iPosInList = iPos;
        }

        /// <summary>
        /// Current move (last done move)
        /// </summary>
        public MoveExt CurrentMove {
            get {
                return(this[m_iPosInList]);
            }
        }

        /// <summary>
        /// Next move in the redo list
        /// </summary>
        public MoveExt NextMove {
            get {
                return(this[m_iPosInList+1]);
            }
        }

        /// <summary>
        /// Move to next move
        /// </summary>
        public void MoveToNext() {
            int iMaxPos;
            
            iMaxPos = Count - 1;
            if (m_iPosInList < iMaxPos) {
                m_iPosInList++;
            }
        }

        /// <summary>
        /// Move to previous move
        /// </summary>
        public void MoveToPrevious() {
            if (m_iPosInList > -1) {
                m_iPosInList--;
            }
        }

        /// <summary>
        /// Current move index
        /// </summary>
        public int PositionInList {
            get {
                return(m_iPosInList);
            }
        }

        /// <summary>
        /// Removes all move in the list
        /// </summary>
        public void Clear() {
            m_listMovePos.Clear();
            m_iPosInList = -1;
        }
    } // Class MovePosStack
} // Namespace
