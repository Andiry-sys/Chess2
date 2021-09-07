using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Reflection;

namespace SrcChess2 {
    /// <summary>Handle the book opening.</summary>
    public class Book {
    
        /// <summary>Entry in the book entries</summary>
        private struct BookEntry {        
            /// <summary>Position of this entry (Start + (End * 256))</summary>
            public short    Pos;
            /// <summary>How many move for this entry at the index</summary>
            public short    Size;
            /// <summary>Index in the table for the entry</summary>
            public int      Index;
            /// <summary>How many child book entries this one has</summary>
            public int      Weight;
        };

        /// <summary>Comparer use to sort array of short</summary>
        private class CompareShortArray : IComparer<short[]> {

            /// <summary>
            /// Comparer of Array of short
            /// </summary>
            /// <param name="x">    First move list</param>
            /// <param name="y">    Second move list</param>
            /// <returns>
            /// -1 if g1 less than g2, 1  if g1 greater than g2, 0 if g1 = g2
            /// </returns>
            public int Compare(short[] x, short[] y) {
                int     iRetVal = 0;
                int     iIndex;
                int     iMinSize;

                iMinSize = x.Length;
                if (y.Length < iMinSize) {
                    iMinSize = y.Length;
                }
                iIndex = 0;
                while (iIndex < iMinSize && iRetVal == 0) {
                    if (x[iIndex] < y[iIndex]) {
                        iRetVal--;
                    } else if (x[iIndex] > y[iIndex]) {
                        iRetVal++;
                    } else {
                        iIndex++;
                    }
                }
                if (iRetVal == 0) {
                    if (x.Length < y.Length) {
                        iRetVal--;
                    } else if (x.Length > y.Length) {
                        iRetVal++;
                    }
                }
                return(iRetVal);
            }
        } // Class CompareShortArray
        
        /// <summary>List of book entries</summary>
        private BookEntry[] m_bookEntries;

        /// <summary>
        /// Class constructor
        /// </summary>
        public Book() {
            m_bookEntries           = new BookEntry[1];
            m_bookEntries[0].Size   = 0;
            m_bookEntries[0].Pos    = 0;
            m_bookEntries[0].Index  = 1;
            m_bookEntries[0].Weight = 0;
        }

        /// <summary>
        /// Compute the number of child for each child moves
        /// </summary>
        /// <param name="iParent">  Parent move</param>
        /// <returns>
        /// Nb of child
        /// </returns>
        private int ComputeWeight(int iParent) {
            int     iRetVal;
            int     iStart;
            int     iEnd;
            
            iStart  = m_bookEntries[iParent].Index;
            iRetVal = m_bookEntries[iParent].Size;
            iEnd    = iStart + iRetVal;
            for (int iIndex = iStart; iIndex < iEnd; iIndex++) {
                iRetVal += ComputeWeight(iIndex);
            }
            m_bookEntries[iParent].Weight += iRetVal;
            return(iRetVal);
        }            

        /// <summary>
        /// Compute the number of child for each child moves
        /// </summary>
        private void ComputeWeight() {
            ComputeWeight(0);
        }

        /// <summary>
        /// Read the book from a binary file
        /// </summary>
        private bool ReadBookFromReader(BinaryReader reader) {
            bool            bRetVal = false;
            string          strSignature;
            int             iSize;
            
            strSignature    = reader.ReadString();
            if (strSignature == "BOOK090") {
                iSize           = reader.ReadInt32();
                m_bookEntries   = new BookEntry[iSize];
                for (int iIndex = 0; iIndex < iSize; iIndex++) {
                    m_bookEntries[iIndex].Pos   = reader.ReadInt16();
                    m_bookEntries[iIndex].Size  = reader.ReadInt16();
                    m_bookEntries[iIndex].Index = reader.ReadInt32();
                }
                bRetVal = true;
            }
            ComputeWeight();
            return(bRetVal);
        }

        /// <summary>
        /// Read the book from a binary file
        /// </summary>
        /// <param name="strFileName">  File Name</param>
        public bool ReadBookFromFile(string strFileName) {
            bool            bRetVal = false;
            FileStream      fileStream;
            BinaryReader    reader;
            
            if (File.Exists(strFileName)) {
                using(fileStream = File.OpenRead(strFileName)) {
                    reader          = new BinaryReader(fileStream);
                    bRetVal = ReadBookFromReader(reader);
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Read the book from the specified resource
        /// </summary>
        /// <param name="asm">          Assembly</param>
        /// <param name="strResName">   Resource Name</param>
        public bool ReadBookFromResource(Assembly asm, string strResName) {
            bool            bRetVal = false;
            BinaryReader    reader;
            Stream          stream;

            if (asm == null) {
                asm = GetType().Assembly;
            }
            stream  = asm.GetManifestResourceStream(strResName);
            using(stream) {
                reader  = new BinaryReader(stream);
                bRetVal = ReadBookFromReader(reader);
            }
            return(bRetVal);
        }

        /// <summary>
        /// Read the book from the specified resource
        /// </summary>
        /// <param name="strResName">   Resource Name</param>
        public bool ReadBookFromResource(string strResName) {
            bool            bRetVal;
            Assembly        asm;

            asm     = GetType().Assembly;
            bRetVal = ReadBookFromResource(asm, strResName);
            return(bRetVal);
        }

        /// <summary>
        /// Save the book to a binary file
        /// </summary>
        public void SaveBookToFile(string strFileName) {
            FileStream      fileStream;
            BinaryWriter    writer;
            string          strSignature = "BOOK090";
            int             iSize;
            
            using(fileStream = File.Create(strFileName)) {
                writer          = new BinaryWriter(fileStream);
                writer.Write(strSignature);
                iSize   = m_bookEntries.Length;
                writer.Write(iSize);
                for (int iIndex = 0; iIndex < iSize; iIndex++) {
                   writer.Write(m_bookEntries[iIndex].Pos);
                   writer.Write(m_bookEntries[iIndex].Size);
                   writer.Write(m_bookEntries[iIndex].Index);
                }
            }
        }

        /// <summary>
        /// Find a move from the book
        /// </summary>
        /// <param name="arrPreviousMove">  List of previous moves</param>
        /// <param name="rnd">              Random to use to pickup a move from a list. Can be null</param>
        /// <returns>
        /// Move in the form of StartPos + (EndPos * 256) or -1 if none found
        /// </returns>
        public short FindMoveInBook(MoveExt[] arrPreviousMove, Random rnd) {
            short   nRetVal;
            bool    bFound = true;
            int     iMoveCount;
            int     iMoveIndex;
            MoveExt move;
            int[]   arrRnd;
            int     iStartIndex;
            int     iIndex;
            int     iSize;
            int     iBiggestRnd;
            short   nPos;
            
            iSize       = m_bookEntries[0].Size;
            iStartIndex = m_bookEntries[0].Index;
            iMoveCount  = arrPreviousMove.Length;
            iMoveIndex  = 0;
            while (iMoveIndex < iMoveCount && bFound) {
                move    = arrPreviousMove[iMoveIndex];
                nPos    = (short)(move.Move.StartPos + (move.Move.EndPos << 8));
                bFound  = false;
                iIndex  = 0;
                while (iIndex < iSize && !bFound) {
                    if (m_bookEntries[iStartIndex + iIndex].Pos == nPos) {
                        bFound = true;
                    } else {
                        iIndex++;
                    }
                }
                if (bFound) {
                    iSize       = m_bookEntries[iStartIndex + iIndex].Size;
                    iStartIndex = m_bookEntries[iStartIndex + iIndex].Index;
                    bFound      = (iSize != 0);
                    iMoveIndex++;
                }
            } 
            if (bFound && iSize != 0) {
                arrRnd  = new int[iSize];
                for (iIndex = 0; iIndex < iSize; iIndex++) {
                    arrRnd[iIndex] = (rnd == null) ?  m_bookEntries[iStartIndex + iIndex].Weight + 2 : rnd.Next(m_bookEntries[iStartIndex + iIndex].Weight + 2);
                }
                iIndex      = 0;
                iBiggestRnd = -1;
                for (int i = 0; i < iSize; i++) {
                    if (arrRnd[i] > iBiggestRnd) {
                        iBiggestRnd = arrRnd[i];
                        iIndex      = i;
                    }
                }
                nRetVal = m_bookEntries[iStartIndex + iIndex].Pos;
            } else {
                nRetVal = -1;
            }
            return(nRetVal);
        }

        /// <summary>
        /// Compare the begining of two lists
        /// </summary>
        /// <param name="pnFirst">      First list</param>
        /// <param name="pnSecond">     Second list</param>
        /// <param name="iMaxDepth">    Maximum depth to compare</param>
        /// <returns>
        /// true if begining is equal
        /// </returns>
        private static bool CompareList(short[] pnFirst, short[] pnSecond, int iMaxDepth) {
            bool    bRetVal = true;
            int     iIndex;
            
            iIndex = 0;
            while (iIndex < iMaxDepth && bRetVal) {
                if (pnFirst[iIndex] != pnSecond[iIndex]) {
                    bRetVal = false;
                }
                iIndex++;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Compare a key with a move list
        /// </summary>
        /// <param name="pnMoveList">   Move list</param>
        /// <param name="arrKey">       Key to compare</param>
        /// <returns>
        /// true if equal
        /// </returns>
        private static bool CompareKey(short[] pnMoveList, List<short> arrKey) {
            bool    bRetVal;
            int     iIndex;
            
            bRetVal = true;
            iIndex  = 0;
            while (iIndex < arrKey.Count && bRetVal) {
                if (arrKey[iIndex] != pnMoveList[iIndex]) {
                    bRetVal = false;
                }
                iIndex++;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Create entries in the book
        /// </summary>
        /// <param name="arrMoveList">  Array of move list</param>
        /// <param name="arrBookEntry"> Book entry to be filled</param>
        /// <param name="arrKey">       Current key</param>
        /// <param name="iPosIndex">    Current position in the list</param>
        /// <param name="iDepth">       Current depth.</param>

        /// <param name="cookie">       Cookie for callback</param>
        /// <returns>
        /// Nb of entries created
        /// </returns>
        private int CreateEntries(List<short[]> arrMoveList, List<BookEntry> arrBookEntry, List<short> arrKey, out int iPosIndex, int iDepth,  object cookie) {
            int         iRetVal = 0;
            int         iKeySize;
            short       nOldValue;
            List<short> arrValues;
            BookEntry   entry;
            
            iKeySize  = arrKey.Count;
            nOldValue = -1;
            arrValues = new List<short>(256);
            foreach (short[] pnMoveList in arrMoveList) {
                if (CompareKey(pnMoveList, arrKey)) {
                    if (pnMoveList[iKeySize] != nOldValue) {
                        nOldValue = pnMoveList[iKeySize];
                        arrValues.Add(nOldValue);
                    }
                }
            }
            iRetVal     = arrValues.Count;
            iPosIndex   = arrBookEntry.Count;
            for (int iIndex = 0; iIndex < iRetVal; iIndex++) {
                entry.Pos       = (short)arrValues[iIndex];
                entry.Size      = 0;
                entry.Index     = 0;
                entry.Weight    = 0;
                arrBookEntry.Add(entry);
            }
            if (iDepth != 0) {
                for (int iIndex = 0; iIndex < iRetVal; iIndex++) {
                    
                    arrKey.Add(arrValues[iIndex]);
                    
                  
                  
                    arrKey.RemoveAt(arrKey.Count - 1);
                }
                
            }
            return(iRetVal);
        }

        /// <summary>
        /// Create the book entries from a series of move list
        /// </summary>
        /// <param name="arrMoveList">      Array of move list</param>
        /// <param name="iMaxDepth">        Maximum depth of the moves.</param>
       
        /// <param name="cookie">           Cookie for callback</param>
        /// <returns>
        /// Nb of entries created
        /// </returns>
        private BookEntry[] CreateBookList(List<short[]> arrMoveList, int iMaxDepth,  object cookie) {
            List<BookEntry>     arrBookEntry;
            List<short>         arrKey;
            BookEntry           entry;
            
            arrKey          = new List<short>(iMaxDepth);
            arrBookEntry    = new List<BookEntry>(arrMoveList.Count * 10);
            entry.Pos       = -1;
            entry.Index     = 1;
            entry.Size      = 0;
            entry.Weight    = 0;
            arrBookEntry.Add(entry);
           
            arrBookEntry[0] = entry;
            return(arrBookEntry.ToArray());
        }          

        /// <summary>
        /// Create the book entries from a series of move list
        /// </summary>
        /// <param name="listMoveList">     List of PGN games</param>
        /// <param name="iMinMoveCount">    Minimum number of moves a move list must have to be consider</param>
        /// <param name="iMaxDepth">        Maximum depth of the moves.</param>
       
        /// <param name="cookie">           Cookie for callback</param>
        /// <returns>
        /// Nb of entries created
        /// </returns>
        public int CreateBookList(List<short[]> listMoveList, int iMinMoveCount, int iMaxDepth,object cookie) {
            short[]         pnLast = null;
            List<short[]>   arrUniqueMoveList;

            arrUniqueMoveList = new List<short[]>(listMoveList.Count);
            listMoveList.Sort(new CompareShortArray());
            foreach (short[] pnMoveList in listMoveList) {
                if (pnMoveList.Length >= iMinMoveCount) {
                    if (pnLast == null || !CompareList(pnMoveList, pnLast, iMaxDepth)) {
                        arrUniqueMoveList.Add(pnMoveList);
                        pnLast = pnMoveList;
                    }
                }
            }
            
            ComputeWeight();
            return(m_bookEntries.Length);
        }
    } // Class Book
} // Namespace
