using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SrcChess2;

namespace SrcChess2.FICSInterface {

    /// <summary>
    /// Termination
    /// </summary>
    public enum TerminationE {
        /// <summary>On going</summary>
        None                = 0,
        /// <summary>White win</summary>
        WhiteWin            = 1,
        /// <summary>Black win</summary>
        BlackWin            = 2,
        /// <summary>Draw</summary>
        Draw                = 3,
        /// <summary>Terminated</summary>
        Terminated          = 4,
        /// <summary>Terminated with error</summary>
        TerminatedWithErr   = 5
    }


    /// <summary>
    /// Represent a parsed line of observed game move in style 12 (raw for interface)
    /// </summary>
    public class Style12MoveLine {

        /// <summary>Relation with the game</summary>
        public enum RelationWithGameE {
            /// <summary>isolated position, such as for "ref 3" or the "sposition" command</summary>
            IsolatedPosition = -3,
            /// <summary>I am observing game being examined</summary>
            ObservingExaminedGame = -2,
            /// <summary>I am the examiner of this game</summary>
            Examiner = 2,
            /// <summary>I am playing, it is my opponent's move</summary>
            PlayerOpponentMove = -1,
            /// <summary>I am playing and it is my move</summary>
            PlayerMyMove = 1,
            /// <summary>I am observing a game being played</summary>
            Observer = 0
        }

        /// <summary>Board represented by the line</summary>
        public ChessBoard.PieceE[]          Board { get; private set; }
        /// <summary>Color of the next player</summary>
        public ChessBoard.PlayerE           NextMovePlayer { get; private set; }
        /// <summary>Board state mask</summary>
        public ChessBoard.BoardStateMaskE   BoardStateMask { get; private set; }
        /// <summary>Number of irreversible moves</summary>
        public int                          IrreversibleMoveCount { get; private set; }
        /// <summary>Game ID</summary>
        public int                          GameId { get; private set; }
        /// <summary>Name of white player</summary>
        public string                       WhitePlayerName { get; private set; }
        /// <summary>Name of black player</summary>
        public string                       BlackPlayerName { get; private set; }
        /// <summary>Relation with the game</summary>
        public RelationWithGameE            RelationWithGame { get; private set; }
        /// <summary>Initial time</summary>
        public int                          InitialTime { get; private set; }
        /// <summary>Incremented time</summary>
        public int                          IncrementTime { get; private set; }
        /// <summary>White material strength</summary>
        public int                          WhiteMaterial { get; private set; }
        /// <summary>Black material strength</summary>
        public int                          BlackMaterial { get; private set; }
        /// <summary>White remaining time in second</summary>
        public int                          WhiteRemainingTime { get; private set; }
        /// <summary>Black remaining time in second</summary>
        public int                          BlackRemainingTime { get; private set; }
        /// <summary>Move number</summary>
        public int                          MoveNumber { get; private set; }
        /// <summary>Last move represent in verbose mode ( PIECE '/' StartPosition - EndingPosition )</summary>
        public string                       LastMoveVerbose { get; private set; }
        /// <summary>Time used to make this move</summary>
        public TimeSpan                     LastMoveSpan { get; private set; }
        /// <summary>Last move represent using SAN</summary>
        public string                       LastMoveSAN { get; private set; }
        /// <summary>true if black in the bottom</summary>
        public bool                         IsFlipped { get; private set; }
        /// <summary>true if clock is ticking</summary>
        public bool                         IsClockTicking;
        /// <summary>Lag in millisecond</summary>
        public int                          LagInMS;

        /// <summary>
        /// Ctor
        /// </summary>
        public Style12MoveLine() {
            Board   = new ChessBoard.PieceE[64];
        }

        /// <summary>
        /// Number of half move count
        /// </summary>
        public int HalfMoveCount {
            get {
                int iRetVal;

                iRetVal = (MoveNumber * 2) - (NextMovePlayer == ChessBoard.PlayerE.White ? 2 : 1);
                return(iRetVal);
            }
        }

        /// <summary>
        /// Gets line part
        /// </summary>
        /// <param name="strLine"></param>
        /// <returns>
        /// Parts
        /// </returns>
        static private string[] GetLineParts(string strLine) {
            string[]    arrRetVal = null;

            if (strLine.StartsWith("<12> ")) {
                arrRetVal = strLine.Split(' ');
                if (arrRetVal.Length < 31) {
                    arrRetVal = null;
                }
            }
            return(arrRetVal);
        }

        /// <summary>
        /// Returns if the line text represent a style 12 move line
        /// </summary>
        /// <param name="strLine">  Line to check</param>
        /// <returns>
        /// true or false
        /// </returns>
        static public bool IsStyle12Line(string strLine) {
            bool        bRetVal;

            bRetVal = (GetLineParts(strLine) != null);
            return(bRetVal);
        }

        /// <summary>
        /// Decode the piece represent by a character
        /// </summary>
        /// <param name="chr">      Character to decode</param>
        /// <param name="ePiece">   Resulting piece</param>
        /// <returns>
        /// true if succeed, false if error
        /// </returns>
        public static bool DecodePiece(char chr, out ChessBoard.PieceE ePiece) {
            bool    bRetVal = true;

            switch(chr) {
            case '-':
                ePiece  = ChessBoard.PieceE.None;
                break;
            case 'P':
                ePiece  = ChessBoard.PieceE.Pawn | ChessBoard.PieceE.White;
                break;
            case 'N':
                ePiece  = ChessBoard.PieceE.Knight | ChessBoard.PieceE.White;
                break;
            case 'B':
                ePiece  = ChessBoard.PieceE.Bishop | ChessBoard.PieceE.White;
                break;
            case 'R':
                ePiece  = ChessBoard.PieceE.Rook | ChessBoard.PieceE.White;
                break;
            case 'Q':
                ePiece  = ChessBoard.PieceE.Queen | ChessBoard.PieceE.White;
                break;
            case 'K':
                ePiece  = ChessBoard.PieceE.King | ChessBoard.PieceE.White;
                break;
            case 'p':
                ePiece  = ChessBoard.PieceE.Pawn | ChessBoard.PieceE.Black;
                break;
            case 'n':
                ePiece  = ChessBoard.PieceE.Knight | ChessBoard.PieceE.Black;
                break;
            case 'b':
                ePiece  = ChessBoard.PieceE.Bishop | ChessBoard.PieceE.Black;
                break;
            case 'r':
                ePiece  = ChessBoard.PieceE.Rook | ChessBoard.PieceE.Black;
                break;
            case 'q':
                ePiece  = ChessBoard.PieceE.Queen | ChessBoard.PieceE.Black;
                break;
            case 'k':
                ePiece  = ChessBoard.PieceE.King | ChessBoard.PieceE.Black;
                break;
            default:
                ePiece  = ChessBoard.PieceE.None;
                bRetVal = false;
                break;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Set a board state mask depending on the passed value
        /// </summary>
        /// <param name="strValue"> Value (must be 0 or 1)</param>
        /// <param name="eMask">    Mask to add if 1</param>
        /// <returns>
        /// true if ok, false if error
        /// </returns>
        private bool SetBoardStateMask(string strValue, ChessBoard.BoardStateMaskE eMask) {
            bool    bRetVal;

            switch (strValue) {
            case "0":
                bRetVal         = true;
                break;
            case "1":
                bRetVal         = true;
                BoardStateMask |= eMask;
                break;
            default:
                bRetVal         = false;
                break;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Check if an move termination as been issued
        /// </summary>
        /// <param name="strLine">                  Line to parse</param>
        /// <param name="iGameId">                  Game id</param>
        /// <param name="strTerminationComment">    Termination comment if any</param>
        /// <param name="strError">                 Error if any</param>
        /// <returns></returns>
        static public TerminationE IsMoveTermination(string strLine, out int iGameId, out string strTerminationComment, out string strError) {
            TerminationE    eRetVal = TerminationE.None;
            int             iStartIndex;
            int             iEndIndex;
            string[]        arrParts;
            //{Game 378 (OlegM vs. Chessnull) Chessnull forfeits on time} 1-0
            strTerminationComment   = "";
            iGameId                 = 0;
            strError                = null;
            strLine                 = strLine.Trim();
            if (strLine.StartsWith("{Game ")) {
                arrParts = strLine.Split(' ');
                if (Int32.TryParse(arrParts[1], out iGameId)) {
                    switch (arrParts[arrParts.Length - 1]) {
                    case "1/2-1/2":
                        eRetVal     = TerminationE.Draw;
                        break;
                    case "1-0":
                        eRetVal     = TerminationE.WhiteWin;
                        break;
                    case "0-1":
                        eRetVal     = TerminationE.BlackWin;
                        break;
                    case "*":
                        eRetVal     = TerminationE.Terminated;
                        break;
                    default:
                        eRetVal     = TerminationE.TerminatedWithErr;
                        strError    = "Unknown termination character '" + arrParts[arrParts.Length - 1] + "'";
                        break;
                    }
                    if (eRetVal != TerminationE.TerminatedWithErr) {
                        iStartIndex = strLine.IndexOf(") ");
                        iEndIndex   = strLine.IndexOf("}");
                        if (iStartIndex != -1 && iEndIndex != -1) {
                            strTerminationComment = strLine.Substring(iStartIndex + 2, iEndIndex - iStartIndex - 2);
                        }
                    }
                }
            } else if (strLine.StartsWith("Removing game ") && Int32.TryParse((arrParts = strLine.Split(' '))[2], out iGameId)) {
                eRetVal = TerminationE.Terminated;
            }
            return(eRetVal);
        }

        /// <summary>
        /// Parse a line
        /// </summary>
        /// <param name="strLine">                  Line to parse</param>
        /// <param name="iGameId">                  Game ID</param>
        /// <param name="eTermination">             Termination code if error or if game has ended</param>
        /// <param name="strTerminationComment">    Termination comment if any</param>
        /// <param name="strError">                 Returned error if any. null if no error detected</param>
        /// <returns>
        /// Line or null if not a style12 line or error
        /// </returns>
        static public Style12MoveLine ParseLine(string strLine, out int iGameId, out TerminationE eTermination, out string strTerminationComment, out string strError) {
            Style12MoveLine     lineRetVal;
            string[]            arrParts;
            string              strFENLine;
            ChessBoard.PieceE   ePiece;
            int                 iLine;
            int                 iPos;
            int[]               arrIntVal;

            eTermination = IsMoveTermination(strLine, out iGameId, out strTerminationComment, out strError);
            if (eTermination != TerminationE.None) {
                lineRetVal = null;
            } else {
                arrParts = GetLineParts(strLine);
                if (arrParts == null) {
                    lineRetVal = null;
                } else {
                    lineRetVal  = new Style12MoveLine();
                    iPos        = 63;
                    iLine       = 0;
                    arrIntVal   = new int[11];
                    while (iLine < 8 && strError == null) {
                        strFENLine = arrParts[iLine + 1];
                        if (strFENLine.Length != 8) {
                            strError   = "Illegal board definition - bad FEN line size";
                        } else {
                            foreach (char chr in strFENLine) {
                                if (DecodePiece(chr, out ePiece)) {
                                    lineRetVal.Board[iPos--] = ePiece;
                                } else {
                                    strError   = "Illegal board definition - Unknown piece specification '" + chr + "'";
                                    break;
                                }
                            }
                        }
                        iLine++;
                    }
                    if (strError == null) {
                        switch(arrParts[9]) {
                        case "B":
                            lineRetVal.NextMovePlayer = ChessBoard.PlayerE.Black;
                            break;
                        case "W":
                            lineRetVal.NextMovePlayer = ChessBoard.PlayerE.White;
                            break;
                        default:
                            strError   = "Next move player not 'B' or 'W'";
                            break;
                        }
                        if (strError == null) {
                            if (!lineRetVal.SetBoardStateMask(arrParts[11], ChessBoard.BoardStateMaskE.WRCastling) ||
                                !lineRetVal.SetBoardStateMask(arrParts[12], ChessBoard.BoardStateMaskE.WLCastling) ||
                                !lineRetVal.SetBoardStateMask(arrParts[13], ChessBoard.BoardStateMaskE.BRCastling) ||
                                !lineRetVal.SetBoardStateMask(arrParts[14], ChessBoard.BoardStateMaskE.BLCastling) ||
                                !Int32.TryParse(arrParts[15], out arrIntVal[0])                                    ||
                                !Int32.TryParse(arrParts[16], out arrIntVal[1])                                    ||
                                !Int32.TryParse(arrParts[19], out arrIntVal[2])                                    ||
                                !Int32.TryParse(arrParts[20], out arrIntVal[3])                                    ||
                                !Int32.TryParse(arrParts[21], out arrIntVal[4])                                    ||
                                !Int32.TryParse(arrParts[22], out arrIntVal[5])                                    ||
                                !Int32.TryParse(arrParts[23], out arrIntVal[6])                                    ||
                                !Int32.TryParse(arrParts[24], out arrIntVal[7])                                    ||
                                !Int32.TryParse(arrParts[25], out arrIntVal[8])                                    ||
                                !Int32.TryParse(arrParts[26], out arrIntVal[9])                                    ||
                                !Int32.TryParse(arrParts[30], out arrIntVal[10])) {
                                strError   = "Illegal value in field.";
                            } else if (arrIntVal[2] < -3 ||
                                       arrIntVal[2] > 2  ||
                                       arrIntVal[3] < 0  ||
                                       arrIntVal[9] < 0  ||
                                       arrIntVal[10] < 0 ||
                                       arrIntVal[10] > 1) {
                                strError   = "Field value out of range.";
                            } else {
                                lineRetVal.WhitePlayerName       = arrParts[17];
                                lineRetVal.BlackPlayerName       = arrParts[18];
                                lineRetVal.IrreversibleMoveCount = arrIntVal[0];
                                lineRetVal.GameId                = arrIntVal[1];
                                lineRetVal.RelationWithGame      = (RelationWithGameE)arrIntVal[2];
                                lineRetVal.InitialTime           = arrIntVal[3];
                                lineRetVal.IncrementTime         = arrIntVal[4];
                                lineRetVal.WhiteMaterial         = arrIntVal[5];
                                lineRetVal.BlackMaterial         = arrIntVal[6];
                                lineRetVal.WhiteRemainingTime    = arrIntVal[7];
                                lineRetVal.BlackRemainingTime    = arrIntVal[8];
                                lineRetVal.MoveNumber            = arrIntVal[9];
                                lineRetVal.LastMoveVerbose       = arrParts[27];
                                lineRetVal.LastMoveSpan          = FICSGame.ParseTime(arrParts[28].Replace("(", "").Replace(")",""));
                                lineRetVal.LastMoveSAN           = arrParts[29];
                                lineRetVal.IsFlipped             = (arrIntVal[9] == 1);
                                iGameId                          = lineRetVal.GameId;
                            }
                        }
                        if (strError == null) {
                            if (arrParts.Length >= 33                           &&
                                Int32.TryParse(arrParts[31], out arrIntVal[0])  &&
                                Int32.TryParse(arrParts[32], out arrIntVal[1])) {
                                lineRetVal.IsClockTicking   = (arrIntVal[0] == 1);
                                lineRetVal.LagInMS          = arrIntVal[1];   
                            } else {
                                lineRetVal.IsClockTicking   = true;
                                lineRetVal.LagInMS          = 0;
                            }
                        }
                    }
                    if (strError != null) {
                        lineRetVal   = null;
                        eTermination = TerminationE.TerminatedWithErr;
                    }
                }
            }
            return(lineRetVal);
        }

        /// <summary>
        /// Parse the receiving line info
        /// </summary>
        /// <param name="iGameId">                  ID of the game being listened to</param>
        /// <param name="lines">                    List of lines to parse</param>
        /// <param name="queueLine">                Queue where to register parsed lines</param>
        /// <param name="strTerminationComment">    Termination comment if any</param>
        /// <param name="strError">                 Error if any, null if none</param>
        /// <returns>
        /// Termination code
        /// </returns>
        static public TerminationE ParseStyle12Lines(int iGameId, List<string> lines, Queue<Style12MoveLine> queueLine, out string strTerminationComment, out string strError) {
            TerminationE    eRetVal = TerminationE.None;
            int             iFoundGameId;
            Style12MoveLine line;

            strError                = null;
            strTerminationComment   = "";
            foreach (string strLine in lines) {
                line = ParseLine(strLine, out iFoundGameId, out eRetVal, out strTerminationComment, out strError);
                if (iFoundGameId == iGameId) {
                    if (line != null) {
                        queueLine.Enqueue(line);
                    } else if (eRetVal != TerminationE.None) {
                        break;
                    }
                }
            }
            return(eRetVal);
        }
    }
}
