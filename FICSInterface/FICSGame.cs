
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SrcChess2;

namespace SrcChess2.FICSInterface {

    /// <summary>
    /// Game description
    /// </summary>
    public class FICSGame {

        /// <summary>
        /// Type of games supported by FICS server
        /// </summary>
        public enum GameTypeE {
            /// <summary>Blitz</summary>
            Blitz,
            /// <summary>Fast blitz</summary>
            Lightning,
            /// <summary>Untimed</summary>
            Untimed,
            /// <summary>Examined</summary>
            Examined,
            /// <summary>Standard game</summary>
            Standard,
            /// <summary>Wild variant</summary>
            Wild,
            /// <summary>Atomic variant</summary>
            Atomic,
            /// <summary>Crazyhouse variant</summary>
            Crazyhouse,
            /// <summary>Bughouse variant</summary>
            Bughouse,
            /// <summary>Losers variant</summary>
            Losers,
            /// <summary>Suicide variant</summary>
            Suicide,
            /// <summary>Non standard</summary>
            NonStandard
        }

        /// <summary>Game ID</summary>
        public int                  GameId { get; private set; }
        /// <summary>White Rating (-1 = unregistred, 0 = Unrated)</summary>
        public int                  WhiteRating { get; private set; }
        /// <summary>Name of the white player</summary>
        public string               WhitePlayer { get; private set; }
        /// <summary>Black Rating (-1 = unregistred, 0 = Unrated)</summary>
        public int                  BlackRating { get; private set; }
        /// <summary>Name of the black player</summary>
        public string               BlackPlayer { get; private set; }
        /// <summary>Game type</summary>
        public GameTypeE            GameType { get; private set; }
        /// <summary>true if rated game</summary>
        public bool                 IsRated { get; private set; }
        /// <summary>true if private</summary>
        public bool                 IsPrivate { get; private set; }
        /// <summary>Time for each player for the game</summary>
        public int                  PlayerTimeInMin { get; private set; }
        /// <summary>Time add to the total game per move</summary>
        public int                  IncTimeInSec { get; private set; }
        /// <summary>White time span</summary>
        public TimeSpan             WhiteTimeSpan { get; private set; }
        /// <summary>Black time span</summary>
        public TimeSpan             BlackTimeSpan { get; private set; }
        /// <summary>Current White material strength</summary>
        public int                  WhiteMaterialPoint { get; private set; }
        /// <summary>Current Black material strength</summary>
        public int                  BlackMaterialPoint { get; private set; }
        /// <summary>Player making the next move</summary>
        public ChessBoard.PlayerE   NextMovePlayer { get; private set; }
        /// <summary>Count for the next move</summary>
        public int                  NextMoveCount { get; private set; }

        /// <summary>
        /// Gets rating in human form
        /// </summary>
        /// <param name="iRating">  Rating</param>
        /// <returns>
        /// Rating
        /// </returns>
        public static string GetHumanRating(int iRating) {
            string  strRetVal;

            if (iRating == -1) {
                strRetVal = "Guest";
            } else if (iRating == 0) {
                strRetVal = "Not Rated";
            } else {
                strRetVal = iRating.ToString();
            }
            return(strRetVal);
        }

        /// <summary>
        /// Convert rating to string
        /// </summary>
        /// <param name="iRating">  Rating</param>
        /// <returns>
        /// String
        /// </returns>
        private static string CnvRating(int iRating) {
            string  strRetVal;

            if (iRating == -1) {
                strRetVal = "++++";
            } else if (iRating == 0) {
                strRetVal = "----";
            } else {
                strRetVal = iRating.ToString().PadLeft(4);
            }
            return(strRetVal);
        }

        /// <summary>
        /// Convert player's name
        /// </summary>
        /// <param name="strPlayer">    Player's name</param>
        /// <returns></returns>
        private static string CnvPlayerName(string strPlayer) {
            if (strPlayer.Length < 10) {
                return(strPlayer.PadRight(10));
            } else {
                return(strPlayer.Substring(0, 10));
            }
        }

        /// <summary>
        /// Convert time to string
        /// </summary>
        /// <param name="span"> Span</param>
        /// <returns>
        /// String
        /// </returns>
        private static string TimeToString(TimeSpan span) {
            string  strRetVal = "";

            if (span.Hours != 0) {
                strRetVal = span.Hours.ToString() + ":" + span.Minutes.ToString().PadLeft(2, '0') + ":" + span.Seconds.ToString().PadLeft(2, '0');
            } else {
                strRetVal = span.Minutes.ToString() + ":" + span.Seconds.ToString().PadLeft(2, '0');
            }
            return(strRetVal);
        }

        /// <summary>
        /// Convert the game into string
        /// </summary>
        /// <returns>
        /// String representation
        /// </returns>
        public override string ToString() {
            StringBuilder   strb;

            strb = new StringBuilder(128);
            strb.Append(GameId.ToString().PadLeft(3));
            strb.Append(' ');
            strb.Append(CnvRating(WhiteRating));
            strb.Append(' ');
            strb.Append(CnvPlayerName(WhitePlayer));
            strb.Append(' ');
            strb.Append(CnvRating(BlackRating));
            strb.Append(' ');
            strb.Append(CnvPlayerName(BlackPlayer));
            strb.Append(' ');
            strb.Append('[');
            strb.Append(IsPrivate ? 'p' : ' ');
            strb.Append(GameTypeToChar(GameType));
            strb.Append(IsRated ? 'r' : 'u');
            strb.Append(PlayerTimeInMin.ToString().PadLeft(3));
            strb.Append(' ');
            strb.Append(IncTimeInSec.ToString().PadLeft(3));
            strb.Append(']');
            strb.Append(' ');
            strb.Append(TimeToString(WhiteTimeSpan).PadLeft(6));
            strb.Append(" -");
            strb.Append(TimeToString(BlackTimeSpan).PadLeft(6));
            strb.Append(' ');
            strb.Append('(');
            strb.Append(WhiteMaterialPoint.ToString().PadLeft(2));
            strb.Append('-');
            strb.Append(BlackMaterialPoint.ToString().PadLeft(2));
            strb.Append(')');
            strb.Append(' ');
            strb.Append(NextMovePlayer == ChessBoard.PlayerE.White ? 'W' : 'B');
            strb.Append(':');
            strb.Append(NextMoveCount.ToString().PadLeft(3));;
            return(strb.ToString());
        }

        /// <summary>
        /// Skip the next character
        /// </summary>
        /// <param name="str">  String</param>
        /// <param name="iPos"> Position in the string</param>
        /// <returns>
        /// Character
        /// </returns>
        private static char GetNextChar(string str, ref int iPos) {
            char    cRetVal = '\0';
            int     iLength;

            iLength = str.Length;
            if (iPos < iLength) {
                cRetVal = str[iPos++];
            }
            return(cRetVal);
        }

        /// <summary>
        /// Skip the next non-white character
        /// </summary>
        /// <param name="str">  String</param>
        /// <param name="iPos"> Position in the string</param>
        /// <returns>
        /// Next non white character
        /// </returns>
        private static char GetNextNonWhiteChar(string str, ref int iPos) {
            char    cRetVal = '\0';
            int     iLength;

            iLength = str.Length;
            while (iPos < iLength && Char.IsWhiteSpace(str[iPos])) {
                iPos++;
            }
            if (iPos < iLength) {
                cRetVal = str[iPos++];
            }
            return(cRetVal);
        }

        /// <summary>
        /// Gets the next token
        /// </summary>
        /// <param name="str">  String</param>
        /// <param name="iPos"> Position in the string</param>
        /// <returns>
        /// Next string token. Can be empty
        /// </returns>
        private static string GetNextToken(string str, ref int iPos) {
            StringBuilder   strb;
            int             iLength;

            iLength = str.Length;
            strb    = new StringBuilder(32);
            while (iPos < iLength && Char.IsWhiteSpace(str[iPos])) {
                iPos++;
            }
            while (iPos < iLength && !Char.IsWhiteSpace(str[iPos])) {
                strb.Append(str[iPos++]);
            }
            return(strb.ToString());
        }

        /// <summary>
        /// Gets the next digit token
        /// </summary>
        /// <param name="str">  String</param>
        /// <param name="iPos"> Position in the string</param>
        /// <returns>
        /// Next string token. Can be empty
        /// </returns>
        private static string GetNextDigitToken(string str, ref int iPos) {
            StringBuilder   strb;
            int             iLength;

            iLength = str.Length;
            strb    = new StringBuilder(32);
            while (iPos < iLength && Char.IsWhiteSpace(str[iPos])) {
                iPos++;
            }
            while (iPos < iLength && Char.IsDigit(str[iPos])) {
                strb.Append(str[iPos++]);
            }
            return(strb.ToString());
        }

        /// <summary>
        /// Gets a token included between a starting and ending character
        /// </summary>
        /// <param name="str">          String</param>
        /// <param name="iPos">         Current position in string</param>
        /// <param name="cStartingChr"> Starting character</param>
        /// <param name="cEndingChr">   Ending character</param>
        /// <returns>
        /// Enclosed string or empty if none
        /// </returns>
        private static string GetNextEnclosedToken(string str, ref int iPos, char cStartingChr, char cEndingChr) {
            StringBuilder   strb;
            int             iLength;

            iLength = str.Length;
            strb    = new StringBuilder(32);
            while (iPos < iLength && Char.IsWhiteSpace(str[iPos])) {
                iPos++;
            }
            if (iPos < iLength && str[iPos] == cStartingChr) {
                iPos++;
                while (iPos < iLength && str[iPos] != cEndingChr) {
                    strb.Append(str[iPos++]);
                }
                if (iPos < iLength) {
                    iPos++;
                } else {
                    strb.Clear();
                }
            }
            return(strb.ToString());
        }

        /// <summary>
        /// Parse the type of the game
        /// </summary>
        /// <param name="chr">          Character specifying the game type</param>
        /// <param name="bSupported">   Return flase if the game type is not supported</param>
        /// <returns>
        /// Game type
        /// </returns>
        public static GameTypeE ParseGameType(char chr, ref bool bSupported) {
            GameTypeE   eRetVal;

            switch (chr) {
            case 'b':
                eRetVal     = GameTypeE.Blitz;
                break;
            case 'e':
                eRetVal     = GameTypeE.Examined;
                bSupported  = false;
                break;
            case 'l':
                eRetVal     = GameTypeE.Lightning;
                break;
            case 'n':
                eRetVal     = GameTypeE.NonStandard;
                break;
            case 's':
                eRetVal     = GameTypeE.Standard;
                break;
            case 'u':
                eRetVal     = GameTypeE.Untimed;
                break;
            case 'w':
                eRetVal     = GameTypeE.Wild;
                break;
            case 'x':
                eRetVal     = GameTypeE.Atomic;
                bSupported  = false;
                break;
            case 'z':
                eRetVal     = GameTypeE.Crazyhouse;
                bSupported  = false;
                break;
            case 'B':
                eRetVal     = GameTypeE.Bughouse;
                bSupported  = false;
                break;
            case 'L':
                eRetVal     = GameTypeE.Losers;
                bSupported  = false;
                break;
            case 'S':
                eRetVal     = GameTypeE.Suicide;
                bSupported  = false;
                break;
            default:
                eRetVal     = GameTypeE.NonStandard;
                bSupported  = false;
                break;
            }
            return(eRetVal);
        }

        /// <summary>
        /// Convert a game type to its corresponding character
        /// </summary>
        /// <param name="eGameType">    Character specifying the game type</param>
        /// <returns>
        /// Character representing this game type
        /// </returns>
        public static char GameTypeToChar(GameTypeE eGameType) {
            char    cRetVal;

            switch (eGameType) {
            case GameTypeE.Blitz:
                cRetVal = 'b';
                break;
            case GameTypeE.Examined:
                cRetVal = 'e';
                break;
            case GameTypeE.Lightning:
                cRetVal = 'l';
                break;
            case GameTypeE.NonStandard:
                cRetVal = 'n';
                break;
            case GameTypeE.Standard:
                cRetVal = 's';
                break;
            case GameTypeE.Untimed:
                cRetVal = 'u';
                break;
            case GameTypeE.Wild:
                cRetVal = 'w';
                break;
            case GameTypeE.Atomic:
                cRetVal = 'x';
                break;
            case GameTypeE.Crazyhouse:
                cRetVal = 'z';
                break;
            case GameTypeE.Bughouse:
                cRetVal = 'B';
                break;
            case GameTypeE.Losers:
                cRetVal = 'L';
                break;
            case GameTypeE.Suicide:
                cRetVal = 'S';
                break;
            default:
                cRetVal = 'n';
                break;
            }
            return(cRetVal);
        }

        /// <summary>
        /// Parsing player rating
        /// </summary>
        /// <param name="strRating">    Rating</param>
        /// <returns>
        /// Rating value
        /// </returns>
        private static int ParseRating(string strRating) {
            int iRetVal;

            if (strRating.StartsWith("+")) {
                iRetVal = -1;
            } else if (strRating.StartsWith("-")) {
                iRetVal = 0;
            } else {
                iRetVal = Int32.Parse(strRating);
            }
            return(iRetVal);
        }

        /// <summary>
        /// Parse a player clock time
        /// </summary>
        /// <param name="str">  String to parse</param>
        /// <returns>
        /// Time span
        /// </returns>
        public static TimeSpan ParseTime(string str) {
            TimeSpan    span;
            string[]    arr;
            string[]    arrMS;

            arr = str.Split(':');
            if (arr.Length == 3) {
                arrMS = arr[2].Split('.');
                if (arrMS.Length == 1) {
                    span = new TimeSpan(Int32.Parse(arr[0]), Int32.Parse(arr[1]), Int32.Parse(arr[2]));
                } else {
                    span = new TimeSpan(0, Int32.Parse(arr[0]), Int32.Parse(arr[1]), Int32.Parse(arrMS[0]), Int32.Parse(arrMS[1]));
                }
            } else {
                arrMS = arr[1].Split('.');
                if (arrMS.Length == 1) {
                    span = new TimeSpan(0, Int32.Parse(arr[0]), Int32.Parse(arr[1]));
                } else {
                    span = new TimeSpan(0, 0, Int32.Parse(arr[0]), Int32.Parse(arrMS[0]), Int32.Parse(arrMS[1]));
                }
            }
            return(span);
        }

        /// <summary>
        /// Parse the player
        /// </summary>
        /// <param name="chr">  Character specifying the player</param>
        /// <returns>
        /// Player
        /// </returns>
        private static ChessBoard.PlayerE ParsePlayer(char chr) {
            ChessBoard.PlayerE eRetVal;

            if (chr == 'B') {
                eRetVal = ChessBoard.PlayerE.Black;
            } else if (chr == 'W') {
                eRetVal = ChessBoard.PlayerE.White;
            } else {
                throw new ArgumentException("Invalid player - " + chr.ToString());
            }
            return(eRetVal);
        }

        /// <summary>
        /// Chesks if the line is the last line of a game list
        /// </summary>
        /// <param name="strLine">  Line to check</param>
        /// <returns>
        /// true / false
        /// </returns>
        public static bool IsLastGameLine(string strLine) {
            return(strLine.EndsWith(" games displayed."));
        }

        /// <summary>
        /// Parse a game string coming from the games command
        /// </summary>
        /// <param name="str">          Line containing the game information</param>
        /// <param name="bSupported">   Returned false if the game type is not actually supported</param>
        /// <returns>
        /// Game or null if cannot be parsed
        /// </returns>
        public static FICSGame ParseGameLine(string str, out bool bSupported) {
            FICSGame    gameRetVal;
            int         iPos;
            int         iEnclosedPos;
            int         iGameId;
            string      strTok;
            string      strEnclosedStr;
            char        chr;
            bool        bExam;

            bSupported = true;
            if (String.IsNullOrWhiteSpace(str)) {
                gameRetVal  = null;
                bSupported  = false;
            } else {
                try {
                    bExam   = false;
                    iPos    = 0;
                    strTok  = GetNextToken(str, ref iPos);
                    if (!Int32.TryParse(strTok, out iGameId)) {
                        gameRetVal  = null;
                    } else {
                        gameRetVal          = new FICSGame();
                        gameRetVal.GameId   = iGameId;
                        strTok              = GetNextToken(str, ref iPos);
                        bExam               = strTok.StartsWith("(");
                        if (bExam || strTok == "games") {
                            gameRetVal          = null;
                            bSupported          = false;
                        } else {
                            gameRetVal.WhiteRating  = ParseRating(strTok);
                            gameRetVal.WhitePlayer  = GetNextToken(str, ref iPos);
                            gameRetVal.BlackRating  = ParseRating(GetNextToken(str, ref iPos));
                            gameRetVal.BlackPlayer  = GetNextToken(str, ref iPos);
                            strEnclosedStr          = GetNextEnclosedToken(str, ref iPos, '[', ']');
                            if (String.IsNullOrEmpty(strEnclosedStr)) {
                                gameRetVal          = null;
                                bSupported          = false;
                            } else {
                                iEnclosedPos        = 0;
                                chr                 = GetNextChar(strEnclosedStr, ref iEnclosedPos);
                                if (chr != ' ' && chr != 'p') {
                                    gameRetVal  = null;
                                    bSupported  = false;
                                } else {
                                    gameRetVal.IsPrivate = (chr == 'p');
                                    gameRetVal.GameType         = ParseGameType(GetNextChar(strEnclosedStr, ref iEnclosedPos), ref bSupported);
                                    gameRetVal.IsRated          = GetNextChar(strEnclosedStr, ref iEnclosedPos) == 'r';
                                    gameRetVal.PlayerTimeInMin  = Int32.Parse(GetNextToken(strEnclosedStr, ref iEnclosedPos));
                                    gameRetVal.IncTimeInSec     = Int32.Parse(GetNextToken(strEnclosedStr, ref iEnclosedPos));
                                    gameRetVal.WhiteTimeSpan    = ParseTime(GetNextToken(str, ref iPos));
                                    GetNextNonWhiteChar(str, ref iPos);
                                    gameRetVal.BlackTimeSpan    = ParseTime(GetNextToken(str, ref iPos));
                                    strEnclosedStr              = GetNextEnclosedToken(str, ref iPos, '(', ')');
                                    if (String.IsNullOrEmpty(strEnclosedStr)) {
                                        gameRetVal  = null;
                                        bSupported  = false;
                                    } else {
                                        iEnclosedPos                    = 0;
                                        gameRetVal.WhiteMaterialPoint   = Int32.Parse(GetNextDigitToken(strEnclosedStr, ref iEnclosedPos));
                                        GetNextNonWhiteChar(strEnclosedStr, ref iEnclosedPos);
                                        gameRetVal.BlackMaterialPoint   = Int32.Parse(GetNextDigitToken(strEnclosedStr, ref iEnclosedPos));
                                        gameRetVal.NextMovePlayer       = ParsePlayer(GetNextToken(str, ref iPos)[0]);
                                        gameRetVal.NextMoveCount        = Int32.Parse(GetNextToken(str, ref iPos));
                                    }
                                }
                            }
                        }
                    }
                } catch(System.Exception) {
                    gameRetVal = null;
                }
            }
            return(gameRetVal);
        }

        /// <summary>
        /// Parse move found on a line
        /// </summary>
        /// <param name="iMoveIndex">   Move index</param>
        /// <param name="strLine">      Line of data</param>
        /// <param name="strWMove">     White move</param>
        /// <param name="spanWTime">    White time for the move</param>
        /// <param name="strBMove">     Black move if any</param>
        /// <param name="spanBTime">    Black move time if any</param>
        /// <param name="strError">     Error if any</param>
        /// <returns>
        /// true if succeed, false if error, null if eof
        /// </returns>
        public static bool? ParseMoveLine(int           iMoveIndex,
                                          string        strLine,
                                          out string    strWMove,
                                          out TimeSpan? spanWTime,
                                          out string    strBMove,
                                          out TimeSpan? spanBTime,
                                          out string    strError) {
            bool?   bRetVal;
            string  strTok;
            int     iVal;
            int     iPosInLine = 0;

            strWMove    = null;
            spanWTime   = null;
            strBMove    = null;
            spanBTime   = null;
            strError    = null;
            try {
                if (strLine.Trim().StartsWith("{")) {
                    bRetVal = null;
                } else {
                    strWMove    = null;
                    strBMove    = null;
                    strTok      = GetNextDigitToken(strLine, ref iPosInLine);
                    if (Int32.TryParse(strTok, out iVal) && iVal == iMoveIndex && GetNextChar(strLine, ref iPosInLine) == '.') {
                        strWMove    = GetNextToken(strLine, ref iPosInLine);
                        strTok      = GetNextToken(strLine, ref iPosInLine).Replace("(", "").Replace(")", "");
                        spanWTime   = ParseTime(strTok);
                        strTok      = GetNextToken(strLine, ref iPosInLine);
                        if (!String.IsNullOrEmpty(strTok)) {
                            strBMove    = strTok;
                            strTok      = GetNextToken(strLine, ref iPosInLine).Replace("(", "").Replace(")", "");
                            spanBTime   = ParseTime(strTok);
                        }
                        bRetVal     = true;
                    } else {
                        strError    = "Illegal move number";
                        bRetVal     = false;
                    }
                }
            } catch(System.Exception) {
                strError = "Unable to parse move line - " + strLine;
                bRetVal  = false;
            }
            return (bRetVal);
        }

        /// <summary>
        /// Parse a list of moves
        /// </summary>
        /// <param name="iGameId">      Game ID</param>
        /// <param name="lines">        List of lines containing the move list</param>
        /// <param name="listTimeSpan"> List of time span or null if not wanted</param>
        /// <param name="strError">     Error if any</param>
        /// <returns>
        /// List of moves or null if error
        /// </returns>
        public static List<String> ParseMoveList(int iGameId, List<string> lines, List<TimeSpan> listTimeSpan, out string strError) {
            List<String>    listRetVal;
            int             iMoveIndex;
            int             iLineCount;
            int             iLineIndex;
            string          strStartingWith;
            string          strWMove;
            string          strBMove;
            TimeSpan?       spanWTime;
            TimeSpan?       spanBTime;
            bool?           bResult;

            strError        = null;
            iLineCount      = lines.Count;
            iLineIndex      = 0;
            strStartingWith = String.Format("Movelist for game {0}:", iGameId);
            while (iLineIndex < iLineCount && !lines[iLineIndex].StartsWith(strStartingWith)) {
                iLineIndex++;
            }
            while (iLineIndex < iLineCount && !lines[iLineIndex].StartsWith("---- ")) {
                iLineIndex++;
            }
            if (iLineIndex == iLineCount) {
                listRetVal  = null;
                strError    = "Move list not found";
            } else {
                iLineIndex++;
                listRetVal  = new List<string>((iLineCount - iLineIndex + 1) * 2);
                iMoveIndex  = 0;
                bResult     = true;
                while (iLineIndex < iLineCount && bResult == true) {
                    bResult = ParseMoveLine(++iMoveIndex,
                                            lines[iLineIndex++],
                                            out strWMove,
                                            out spanWTime,
                                            out strBMove,
                                            out spanBTime,
                                            out strError);
                    if (bResult == true) {
                        listRetVal.Add(strWMove);
                        if (listTimeSpan != null) {
                            listTimeSpan.Add(spanWTime.Value);
                        }
                        if (strBMove != null) {
                            listRetVal.Add(strBMove);
                            if (listTimeSpan != null) {
                                listTimeSpan.Add(spanBTime.Value);
                            }
                        }
                    }
                }
                if (bResult == false) {
                    listRetVal = null;
                }
            }
            return(listRetVal);
        }
    }
}
