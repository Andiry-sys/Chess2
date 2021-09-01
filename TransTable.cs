using System;

namespace SrcChess2 {

    /// <summary>Type of transposition entry</summary>
    public enum TransEntryTypeE {
        /// <summary>Exact move value</summary>
        Exact   = 0,
        /// <summary>Alpha cut off value</summary>
        Alpha   = 1,
        /// <summary>Beta cut off value</summary>
        Beta    = 2
    };

    /// <summary>
    /// Implements a transposition table. Transposition table is used to cache already computed board 
    /// </summary>
    public class TransTable {

        /// <summary>Entry in the transposition table</summary>
        private struct TransEntry {
            public Int64                        m_i64Key;       // 64 bits key compute with Zobrist algorithm. Defined a probably unique board position.
            public int                          m_iGen;         // Generation of the entry
            public ChessBoard.BoardStateMaskE   m_eExtraInfo;   // Board extra info. Defined board extra information
            public int                          m_iDepth;       // Depth of the move (reverse)
            public TransEntryTypeE              m_eType;        // Type of the entry
            public int                          m_iValue;       // Value of the entry
        };
        
        /// <summary>Array of static transposition table</summary>
        static TransTable[]                     s_arrTransTable;
        /// <summary>Size of the translation tables</summary>
        static int                              s_iTransTableSize = 1000000;
        /// <summary>Hashlist of entries</summary>
        private TransEntry[]                    m_arrTransEntry;
        /// <summary>Number of cache hit</summary>
        private int                             m_iCacheHit;
        /// <summary>Current generation</summary>
        private int                             m_iGen;

        /// <summary>
        /// Static constructor. Use to create the random value for each case of the board.
        /// </summary>
        static TransTable() {
            s_arrTransTable = new TransTable[Environment.ProcessorCount];
        }

        /// <summary>
        /// Size of the translation table
        /// </summary>
        static public int TranslationTableSize {
            get {
                return(s_iTransTableSize);
            }
            set {
                if (s_iTransTableSize != value) {
                    s_iTransTableSize = value;
                    Array.Clear(s_arrTransTable, 0, s_arrTransTable.Length);
                }
            }
        }

        /// <summary>
        /// Gets one of the static translation table
        /// </summary>
        /// <param name="iIndex">           Index of the table (0..ProcessorCount-1)</param>
        /// <returns>
        /// Translation table
        /// </returns>
        static public TransTable GetTransTable(int iIndex) {
            if (s_arrTransTable[iIndex] == null) {
                s_arrTransTable[iIndex] = new TransTable(TranslationTableSize);
            }
            return(s_arrTransTable[iIndex]);
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="iTransTableSize">  Size of the transposition table.</param>
        public TransTable(int iTransTableSize) {
            m_arrTransEntry = new TransEntry[iTransTableSize];
            m_iCacheHit     = 0;
            m_iGen          = 0;
        }

        /// <summary>
        /// Record a new entry in the table
        /// </summary>
        /// <param name="i64ZobristKey">    Zobrist key. Probably unique for this board position.</param>
        /// <param name="eExtraInfo">       Extra information about the board not contains in the Zobrist key</param>
        /// <param name="iDepth">           Current depth (reverse)</param>
        /// <param name="iValue">           Board evaluation</param>
        /// <param name="eType">            Type of the entry</param>
        public void RecordEntry(long i64ZobristKey, ChessBoard.BoardStateMaskE eExtraInfo, int iDepth, int iValue, TransEntryTypeE eType) {
            TransEntry  entry;

            i64ZobristKey      ^= (int)eExtraInfo;
            entry.m_i64Key      = i64ZobristKey;
            entry.m_iGen        = m_iGen;
            entry.m_eExtraInfo  = eExtraInfo;
            entry.m_iDepth      = iDepth;
            entry.m_iValue      = iValue;
            entry.m_eType       = eType;
            m_arrTransEntry[(UInt64)i64ZobristKey % (UInt64)m_arrTransEntry.Length] = entry;
        }

        /// <summary>
        /// Try to find if the current board has already been evaluated
        /// </summary>
        /// <param name="i64ZobristKey">    Zobrist key. Probably unique for this board position.</param>
        /// <param name="eExtraInfo">       Extra information about the board not contains in the Zobrist key</param>
        /// <param name="iDepth">           Current depth (reverse)</param>
        /// <param name="iAlpha">           Alpha cut off</param>
        /// <param name="iBeta">            Beta cut off</param>
        /// <returns>
        /// Int32.MaxValue if no valid value found, else value of the board.
        /// </returns>
        public int ProbeEntry(long i64ZobristKey, ChessBoard.BoardStateMaskE eExtraInfo, int iDepth, int iAlpha, int iBeta) {
            int         iRetVal = Int32.MaxValue;
            TransEntry  entry;
            
            i64ZobristKey ^= (int)eExtraInfo;
            entry          = m_arrTransEntry[(UInt64)i64ZobristKey % (UInt64)m_arrTransEntry.Length];
            if (entry.m_i64Key == i64ZobristKey && entry.m_iGen == m_iGen && entry.m_eExtraInfo == eExtraInfo) {
                if (entry.m_iDepth >= iDepth) {
                    switch(entry.m_eType) {
                    case TransEntryTypeE.Exact:
                        iRetVal = entry.m_iValue;
                        break;
                    case TransEntryTypeE.Alpha:
                        if (entry.m_iValue <= iAlpha) {
                            iRetVal = iAlpha;
                        }
                        break;
                    case TransEntryTypeE.Beta:
                        if (entry.m_iValue >= iBeta) {
                            iRetVal = iBeta;
                        }
                        break;
                    }
                    m_iCacheHit++;
                }
            }
            return(iRetVal);
        }            

        /// <summary>
        /// Number of cache hit
        /// </summary>
        public int CacheHit {
            get {
                return(m_iCacheHit);
            }
            set {
                m_iCacheHit = value;
            }
        }

        /// <summary>
        /// Reset the cache
        /// </summary>
        public void Reset() {
            m_iCacheHit     = 0;
            m_iGen++;
        }
    } // Class TransTable
} // Namespace
