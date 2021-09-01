
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SrcChess2 {
    /// <summary>
    /// Defines a valid move with some extension
    /// </summary>
    public class MoveExt {

        #region Nag Definition
        /// <summary>
        /// List of standard NAG (Numeric Annotation Glyph) $nn)
        /// </summary>
        static private string[]     m_arrNag = {
                                        "null annotation",
                                        "good move (traditional \"!\")",
                                        "poor move (traditional \"?\") ",
                                        "very good move (traditional \"!!\")",
                                        "very poor move (traditional \"??\")",
                                        "speculative move (traditional \"!?\")",
                                        "questionable move (traditional \"?!\")",
                                        "forced move (all others lose quickly)",
                                        "singular move (no reasonable alternatives)",
                                        "worst move",
                                        "drawish position",
                                        "equal chances, quiet position",
                                        "equal chances, active position",
                                        "unclear position",
                                        "White has a slight advantage",
                                        "Black has a slight advantage",
                                        "White has a moderate advantage",
                                        "Black has a moderate advantage",
                                        "White has a decisive advantage",
                                        "Black has a decisive advantage",
                                        "White has a crushing advantage (Black should resign)",
                                        "Black has a crushing advantage (White should resign)",
                                        "White is in zugzwang",
                                        "Black is in zugzwang",
                                        "White has a slight space advantage",
                                        "Black has a slight space advantage",
                                        "White has a moderate space advantage",
                                        "Black has a moderate space advantage",
                                        "White has a decisive space advantage",
                                        "Black has a decisive space advantage",
                                        "White has a slight time (development) advantage",
                                        "Black has a slight time (development) advantage",
                                        "White has a moderate time (development) advantage",
                                        "Black has a moderate time (development) advantage",
                                        "White has a decisive time (development) advantage",
                                        "Black has a decisive time (development) advantage",
                                        "White has the initiative",
                                        "Black has the initiative",
                                        "White has a lasting initiative",
                                        "Black has a lasting initiative",
                                        "White has the attack",
                                        "Black has the attack",
                                        "White has insufficient compensation for material deficit",
                                        "Black has insufficient compensation for material deficit",
                                        "White has sufficient compensation for material deficit",
                                        "Black has sufficient compensation for material deficit",
                                        "White has more than adequate compensation for material deficit",
                                        "Black has more than adequate compensation for material deficit",
                                        "White has a slight center control advantage",
                                        "Black has a slight center control advantage",
                                        "White has a moderate center control advantage",
                                        "Black has a moderate center control advantage",
                                        "White has a decisive center control advantage",
                                        "Black has a decisive center control advantage",
                                        "White has a slight kingside control advantage",
                                        "Black has a slight kingside control advantage",
                                        "White has a moderate kingside control advantage",
                                        "Black has a moderate kingside control advantage",
                                        "White has a decisive kingside control advantage",
                                        "Black has a decisive kingside control advantage",
                                        "White has a slight queenside control advantage",
                                        "Black has a slight queenside control advantage",
                                        "White has a moderate queenside control advantage",
                                        "Black has a moderate queenside control advantage",
                                        "White has a decisive queenside control advantage",
                                        "Black has a decisive queenside control advantage",
                                        "White has a vulnerable first rank",
                                        "Black has a vulnerable first rank",
                                        "White has a well protected first rank",
                                        "Black has a well protected first rank",
                                        "White has a poorly protected king",
                                        "Black has a poorly protected king",
                                        "White has a well protected king",
                                        "Black has a well protected king",
                                        "White has a poorly placed king",
                                        "Black has a poorly placed king",
                                        "White has a well placed king",
                                        "Black has a well placed king",
                                        "White has a very weak pawn structure",
                                        "Black has a very weak pawn structure",
                                        "White has a moderately weak pawn structure",
                                        "Black has a moderately weak pawn structure",
                                        "White has a moderately strong pawn structure",
                                        "Black has a moderately strong pawn structure",
                                        "White has a very strong pawn structure",
                                        "Black has a very strong pawn structure",
                                        "White has poor knight placement",
                                        "Black has poor knight placement",
                                        "White has good knight placement",
                                        "Black has good knight placement",
                                        "White has poor bishop placement",
                                        "Black has poor bishop placement",
                                        "White has good bishop placement",
                                        "Black has good bishop placement",
                                        "White has poor rook placement",
                                        "Black has poor rook placement",
                                        "White has good rook placement",
                                        "Black has good rook placement",
                                        "White has poor queen placement",
                                        "Black has poor queen placement",
                                        "White has good queen placement",
                                        "Black has good queen placement",
                                        "White has poor piece coordination",
                                        "Black has poor piece coordination",
                                        "White has good piece coordination",
                                        "Black has good piece coordination",
                                        "White has played the opening very poorly",
                                        "Black has played the opening very poorly",
                                        "White has played the opening poorly",
                                        "Black has played the opening poorly",
                                        "White has played the opening well",
                                        "Black has played the opening well",
                                        "White has played the opening very well",
                                        "Black has played the opening very well",
                                        "White has played the middlegame very poorly",
                                        "Black has played the middlegame very poorly",
                                        "White has played the middlegame poorly",
                                        "Black has played the middlegame poorly",
                                        "White has played the middlegame well",
                                        "Black has played the middlegame well",
                                        "White has played the middlegame very well",
                                        "Black has played the middlegame very well",
                                        "White has played the ending very poorly",
                                        "Black has played the ending very poorly",
                                        "White has played the ending poorly",
                                        "Black has played the ending poorly",
                                        "White has played the ending well",
                                        "Black has played the ending well",
                                        "White has played the ending very well",
                                        "Black has played the ending very well",
                                        "White has slight counterplay",
                                        "Black has slight counterplay",
                                        "White has moderate counterplay",
                                        "Black has moderate counterplay",
                                        "White has decisive counterplay",
                                        "Black has decisive counterplay",
                                        "White has moderate time control pressure",
                                        "Black has moderate time control pressure",
                                        "White has severe time control pressure",
                                        "Black has severe time control pressure" };
        #endregion

        /// <summary>Numeric Annotation Glyph</summary>
        private int     m_iNag;
        

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="eOriginalPiece">   Piece which has been eaten if any</param>
        /// <param name="iStartPos">        Starting position</param>
        /// <param name="iEndPos">          Ending position</param>
        /// <param name="eType">            Move type</param>
        /// <param name="strComment">       Move comment</param>
        /// <param name="iPermutationCount">Number of permutations searched in the find for best move</param>
        /// <param name="iSearchDepth">     Maximum depth of the search in the find for best move</param>
        /// <param name="iCacheHit">        Number of cache hit in the find for best move</param>
        /// <param name="iNag">             Numeric Annotation Glyph</param>
        public MoveExt(ChessBoard.PieceE    eOriginalPiece,
                       int                  iStartPos,
                       int                  iEndPos,
                       Move.TypeE           eType,
                       string               strComment,
                       int                  iPermutationCount,
                       int                  iSearchDepth,
                       int                  iCacheHit,
                       int                  iNag) {
            Move.OriginalPiece  = eOriginalPiece;
            Move.StartPos       = (byte)iStartPos;
            Move.EndPos         = (byte)iEndPos;
            Move.Type           = eType;
            Comment             = strComment;
            PermutationCount    = iPermutationCount;
            SearchDepth         = iSearchDepth;
            CacheHit            = iCacheHit;
            NAG                 = iNag;
            TimeToCompute       = TimeSpan.Zero;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="move">             Base move</param>
        /// <param name="strComment">       Move comment</param>
        /// <param name="iPermutationCount">Number of permutations searched in the find for best move</param>
        /// <param name="iSearchDepth">     Maximum depth of the search in the find for best move</param>
        /// <param name="iCacheHit">        Number of cache hit in the find for best move</param>
        /// <param name="iNag">             Numeric Annotation Glyph</param>
        public MoveExt(Move     move,
                       string   strComment,
                       int      iPermutationCount,
                       int      iSearchDepth,
                       int      iCacheHit,
                       int      iNag)  {
            Move                = move;
            Comment             = strComment;
            PermutationCount    = iPermutationCount;
            SearchDepth         = iSearchDepth;
            CacheHit            = iCacheHit;
            NAG                 = iNag;
            TimeToCompute       = TimeSpan.Zero;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="move">             Base move</param>
        public MoveExt(Move move) : this(move, "", 0, 0, 0, 0) {
        }

        /// <summary>
        /// Associated move
        /// </summary>
        public Move Move;

        /// <summary>
        /// Time for the computer to find this move
        /// </summary>
        public TimeSpan TimeToCompute {
            get;
            set;
        }

        /// <summary>
        /// Move comment
        /// </summary>
        public string Comment {
            get;
            set;
        }

        /// <summary>
        /// Number of permutation searched for finding this move
        /// </summary>
        public int PermutationCount {
            get;
            set;
        }

        /// <summary>
        /// Depth search to find this move
        /// </summary>
        public int SearchDepth {
            get;
            set;
        }

        /// <summary>
        /// Number of time a cache hit
        /// </summary>
        public int CacheHit {
            get;
            set;
        }

        /// <summary>
        /// Move NAG (Numeric Annotation Glyph)
        /// </summary>
        public int NAG {
            get {
                return(m_iNag);
            }
            set {
                if (value < 0 || value >= m_arrNag.Length) {
                    throw new ArgumentException("Value out of range");
                }
                m_iNag = value;

            }
        }

        /// <summary>
        /// NAG Description
        /// </summary>
        public string NAGDescription {
            get {
                return(m_arrNag[m_iNag]);
            }
        }

        /// <summary>
        /// NAG Short Description
        /// </summary>
        public string NAGShortDescription {
            get {
                string  strRetVal;

                switch (m_iNag) {
                case 0:
                    strRetVal = "";
                    break;
                case 1:
                    strRetVal = "!";
                    break;
                case 2:
                    strRetVal = "?";
                    break;
                case 3:
                    strRetVal = "!!";
                    break;
                case 4:
                    strRetVal = "??";
                    break;
                case 5:
                    strRetVal = "!?";
                    break;
                case 6:
                    strRetVal = "?!";
                    break;
                case 7:
                    strRetVal = "*";
                    break;
                default:
                    strRetVal = "$" + m_iNag.ToString();
                    break;
                }
                return(strRetVal);
            }
        }
    }
}
