using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Text;
using SrcChess2.FICSInterface;
using System.Windows;

namespace SrcChess2 {

    /// <summary>
    /// Transfer object setting from/to the properties setting
    /// </summary>
    internal class SettingAdaptor {
        /// <summary>Properties setting</summary>
        private Properties.Settings     m_settings;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="settings"> Properties setting</param>
        public SettingAdaptor(Properties.Settings settings) {
            m_settings = settings;
        }

        /// <summary>
        /// Settings
        /// </summary>
        public Properties.Settings Settings {
            get {
                return(m_settings);
            }
        }

        /// <summary>
        /// Convert a color name to a color
        /// </summary>
        /// <param name="strName">  Name of the color or hexa representation of the color</param>
        /// <returns>
        /// Color
        /// </returns>
        private Color NameToColor(string strName) {
            Color   colRetVal;
            int     iVal;
            
            if (strName.Length == 8 && (Char.IsLower(strName[0]) || Char.IsDigit(strName[0])) &&
                Int32.TryParse(strName, System.Globalization.NumberStyles.HexNumber, null, out iVal)) { 
                colRetVal = Color.FromArgb((byte)((iVal >> 24) & 255), (byte)((iVal >> 16) & 255), (byte)((iVal >> 8) & 255), (byte)(iVal & 255));
            } else {
                colRetVal = (Color)ColorConverter.ConvertFromString(strName);
            }
            return(colRetVal);    
        }

        /// <summary>
        /// Load the FICS connection setting from the properties setting
        /// </summary>
        /// <param name="ficsSetting">  FICS connection setting</param>
        public void LoadFICSConnectionSetting(FICSConnectionSetting ficsSetting) {
            ficsSetting.HostName    = m_settings.FICSHostName;
            ficsSetting.HostPort    = m_settings.FICSHostPort;
            ficsSetting.UserName    = m_settings.FICSUserName;
            ficsSetting.Anonymous   = string.Compare(m_settings.FICSUserName, "guest", true) == 0;
        }

        /// <summary>
        /// Save the connection settings to the property setting
        /// </summary>
        /// <param name="ficsSetting">  Copy the FICS connection setting to the properties setting</param>
        public void SaveFICSConnectionSetting(FICSConnectionSetting ficsSetting) {
            m_settings.FICSHostName = ficsSetting.HostName;
            m_settings.FICSHostPort = ficsSetting.HostPort;
            m_settings.FICSUserName = ficsSetting.Anonymous ? "Guest" : ficsSetting.UserName;
        }

        /// <summary>
        /// Load the chess board control settings from the property setting
        /// </summary>
        /// <param name="chessCtl"> Chess board control</param>
        public void LoadChessBoardCtl(ChessBoardControl chessCtl) {
            chessCtl.LiteCellColor      = NameToColor(m_settings.LiteCellColor);
            chessCtl.DarkCellColor      = NameToColor(m_settings.DarkCellColor);
            chessCtl.WhitePieceColor    = NameToColor(m_settings.WhitePieceColor);
            chessCtl.BlackPieceColor    = NameToColor(m_settings.BlackPieceColor);
            chessCtl.MoveFlashing       = m_settings.FlashPiece;
        }

        /// <summary>
        /// Save the chess board control settings to the property setting
        /// </summary>
        /// <param name="chessCtl"> Chess board control</param>
        public void SaveChessBoardCtl(ChessBoardControl chessCtl) {
            m_settings.WhitePieceColor  = chessCtl.WhitePieceColor.ToString();
            m_settings.BlackPieceColor  = chessCtl.BlackPieceColor.ToString();
            m_settings.LiteCellColor    = chessCtl.LiteCellColor.ToString();
            m_settings.DarkCellColor    = chessCtl.DarkCellColor.ToString();
            m_settings.FlashPiece       = chessCtl.MoveFlashing;
        }

        /// <summary>
        /// Load main window settings from the property setting
        /// </summary>
        /// <param name="mainWnd"> Main window</param>
        /// <param name="listPieceSet"> List of available piece sets</param>
        public void LoadMainWindow(MainWindow mainWnd, SortedList<string,PieceSet> listPieceSet) {
            WindowState     eWindowState;

            mainWnd.m_colorBackground   = NameToColor(m_settings.BackgroundColor);
            mainWnd.Background          = new SolidColorBrush(mainWnd.m_colorBackground);
            mainWnd.PieceSet            = listPieceSet[m_settings.PieceSet];
            if (!Enum.TryParse(m_settings.WndState, out eWindowState)) {
                eWindowState = WindowState.Normal;
            }
            mainWnd.WindowState     = eWindowState;
            mainWnd.Height          = m_settings.WndHeight;
            mainWnd.Width           = m_settings.WndWidth;
            if (m_settings.WndLeft != Double.NaN) {
                mainWnd.Left        = m_settings.WndLeft;
            }
            if (m_settings.WndTop != Double.NaN) {
                mainWnd.Top         = m_settings.WndTop;
            }
            mainWnd.m_arrPuzzleMask[0]  = m_settings.PuzzleDoneLow;
            mainWnd.m_arrPuzzleMask[1]  = m_settings.PuzzleDoneHigh;
        }

        /// <summary>
        /// Save main window settings from the property setting
        /// </summary>
        /// <param name="mainWnd"> Main window</param>
        public void SaveMainWindow(MainWindow mainWnd) {
            m_settings.BackgroundColor  = mainWnd.m_colorBackground.ToString();
            m_settings.PieceSet         = mainWnd.PieceSet.Name;
            m_settings.WndState         = mainWnd.WindowState.ToString();
            m_settings.WndHeight        = mainWnd.Height;
            m_settings.WndWidth         = mainWnd.Width;
            m_settings.WndLeft          = mainWnd.Left;
            m_settings.WndTop           = mainWnd.Top;
            m_settings.PuzzleDoneLow    = mainWnd.m_arrPuzzleMask[0];
            m_settings.PuzzleDoneHigh   = mainWnd.m_arrPuzzleMask[1];
        }

        /// <summary>
        /// Save the chess board control settings to the property setting
        /// </summary>
        /// <param name="chessCtl"> Chess board control</param>
        public void FromChessBoardCtl(ChessBoardControl chessCtl) {
            m_settings.WhitePieceColor    = chessCtl.WhitePieceColor.ToString();
            m_settings.BlackPieceColor    = chessCtl.BlackPieceColor.ToString();
            m_settings.LiteCellColor      = chessCtl.LiteCellColor.ToString();
            m_settings.DarkCellColor      = chessCtl.DarkCellColor.ToString();
        }

        /// <summary>
        /// Load search setting from property settings
        /// </summary>
        /// <param name="boardEvalUtil">Board evaluation utility</param>
        /// <param name="searchMode">   Search mode setting</param>
        public void LoadSearchMode(BoardEvaluationUtil boardEvalUtil, SettingSearchMode searchMode) {
            int                                 iTransTableSize;

            iTransTableSize                 = (m_settings.TransTableSize < 5 || m_settings.TransTableSize > 256) ? 32 : m_settings.TransTableSize;
            TransTable.TranslationTableSize = iTransTableSize / 32 * 1000000;
            searchMode.Option               = m_settings.UseAlphaBeta ? SearchMode.OptionE.UseAlphaBeta : SearchMode.OptionE.UseMinMax;
            switch(m_settings.DifficultyLevel) {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
                searchMode.DifficultyLevel = (SettingSearchMode.DifficultyLevelE)m_settings.DifficultyLevel;
                break;
            default:
                searchMode.DifficultyLevel = SettingSearchMode.DifficultyLevelE.Manual;
                break;
            }
            switch((SettingSearchMode.BookModeE)m_settings.BookType) {
            case SettingSearchMode.BookModeE.NoBook:
            case SettingSearchMode.BookModeE.Unrated:
                searchMode.BookMode = (SettingSearchMode.BookModeE)m_settings.BookType;
                break;
            default:
                searchMode.BookMode = SettingSearchMode.BookModeE.ELOGT2500;
                break;
            } 
            if (m_settings.UseTransTable) {
                searchMode.Option |= SearchMode.OptionE.UseTransTable;
            }
            if (m_settings.UsePlyCountIterative) {
                searchMode.Option |= SearchMode.OptionE.UseIterativeDepthSearch;
            }
            switch(m_settings.UseThread) {
            case 0:
                searchMode.ThreadingMode = SearchMode.ThreadingModeE.Off;
                break;
            case 1:
                searchMode.ThreadingMode = SearchMode.ThreadingModeE.DifferentThreadForSearch;
                break;
            default:
                searchMode.ThreadingMode = SearchMode.ThreadingModeE.OnePerProcessorForSearch;
                break;
            }
            searchMode.WhiteBoardEvaluation = boardEvalUtil.FindBoardEvaluator(m_settings.WhiteBoardEval) ?? boardEvalUtil.BoardEvaluators[0];
            searchMode.BlackBoardEvaluation = boardEvalUtil.FindBoardEvaluator(m_settings.BlackBoardEval) ?? boardEvalUtil.BoardEvaluators[0];
            searchMode.SearchDepth          = m_settings.UsePlyCount | m_settings.UsePlyCountIterative ? ((m_settings.PlyCount > 1 && m_settings.PlyCount < 9) ? m_settings.PlyCount : 6) : 0;
            searchMode.TimeOutInSec         = m_settings.UsePlyCount | m_settings.UsePlyCountIterative ? 0 : (m_settings.AverageTime > 0 && m_settings.AverageTime < 1000) ? m_settings.AverageTime : 15;
            searchMode.RandomMode           = (m_settings.RandomMode >= 0 && m_settings.RandomMode <= 2) ? (SearchMode.RandomModeE)m_settings.RandomMode : SearchMode.RandomModeE.On;
        }

        /// <summary>
        /// Save the search mode to properties setting
        /// </summary>
        /// <param name="searchMode">   Search mode</param>
        public void SaveSearchMode(SettingSearchMode searchMode) {
            m_settings.UseAlphaBeta         = (searchMode.Option & SearchMode.OptionE.UseAlphaBeta) != 0;
            m_settings.UseTransTable        = (searchMode.Option & SearchMode.OptionE.UseTransTable) != 0;
            m_settings.UsePlyCountIterative = (searchMode.Option & SearchMode.OptionE.UseIterativeDepthSearch) != 0;
            m_settings.UsePlyCount          = (searchMode.Option & SearchMode.OptionE.UseIterativeDepthSearch) == 0 && searchMode.SearchDepth != 0;
            m_settings.DifficultyLevel      = (searchMode.DifficultyLevel == SettingSearchMode.DifficultyLevelE.Manual) ? 0 : (int)searchMode.DifficultyLevel;
            m_settings.PlyCount             = searchMode.SearchDepth;
            m_settings.AverageTime          = searchMode.TimeOutInSec;
            m_settings.BookType             = (int)searchMode.BookMode;
            m_settings.UseThread            = (int)searchMode.ThreadingMode;
            m_settings.RandomMode           = (int)searchMode.RandomMode;
            m_settings.TransTableSize       = TransTable.TranslationTableSize * 32 / 1000000;
            m_settings.WhiteBoardEval       = searchMode.WhiteBoardEvaluation.Name;
            m_settings.BlackBoardEval       = searchMode.BlackBoardEvaluation.Name;
        }

        /// <summary>
        /// Load move viewer setting from properties setting
        /// </summary>
        /// <param name="moveViewer">   Move viewer</param>
        public void LoadMoveViewer(MoveViewer moveViewer) {
            moveViewer.DisplayMode  = (m_settings.MoveNotation == 0) ? MoveViewer.DisplayModeE.MovePos : MoveViewer.DisplayModeE.PGN;
        }

        /// <summary>
        /// Save move viewer setting to properties setting
        /// </summary>
        /// <param name="moveViewer">   Move viewer</param>
        public void SaveMoveViewer(MoveViewer moveViewer) {
            m_settings.MoveNotation = (moveViewer.DisplayMode == MoveViewer.DisplayModeE.MovePos) ? 0 : 1;
        }

        /// <summary>
        /// Load FICS search criteria from properties setting
        /// </summary>
        /// <param name="searchCriteria">   Search criteria</param>
        public void LoadFICSSearchCriteria(FICSInterface.SearchCriteria searchCriteria) {
            searchCriteria.PlayerName           = m_settings.FICSSPlayerName;
            searchCriteria.BlitzGame            = m_settings.FICSSBlitz;
            searchCriteria.LightningGame        = m_settings.FICSSLightning;
            searchCriteria.UntimedGame          = m_settings.FICSSUntimed;
            searchCriteria.StandardGame         = m_settings.FICSSStandard;
            searchCriteria.IsRated              = m_settings.FICSSRated;
            searchCriteria.MinRating            = SearchCriteria.CnvToNullableIntValue(m_settings.FICSSMinRating);
            searchCriteria.MinTimePerPlayer     = SearchCriteria.CnvToNullableIntValue(m_settings.FICSSMinTimePerPlayer);
            searchCriteria.MaxTimePerPlayer     = SearchCriteria.CnvToNullableIntValue(m_settings.FICSSMaxTimePerPlayer);
            searchCriteria.MinIncTimePerMove    = SearchCriteria.CnvToNullableIntValue(m_settings.FICSSMinIncTimePerMove);
            searchCriteria.MaxIncTimePerMove    = SearchCriteria.CnvToNullableIntValue(m_settings.FICSSMaxIncTimePerMove);
            searchCriteria.MaxMoveDone          = m_settings.FICSSMaxMoveDone;
            searchCriteria.MoveTimeOut          = SearchCriteria.CnvToNullableIntValue(m_settings.FICSMoveTimeOut);
        }

        /// <summary>
        /// Save FICS search criteria to properties setting
        /// </summary>
        /// <param name="searchCriteria">   Search criteria</param>
        public void SaveFICSSearchCriteria(FICSInterface.SearchCriteria searchCriteria) {
            m_settings.FICSSPlayerName          = searchCriteria.PlayerName;
            m_settings.FICSSBlitz               = searchCriteria.BlitzGame;
            m_settings.FICSSLightning           = searchCriteria.LightningGame;
            m_settings.FICSSUntimed             = searchCriteria.UntimedGame;
            m_settings.FICSSStandard            = searchCriteria.StandardGame;
            m_settings.FICSSRated               = searchCriteria.IsRated;
            m_settings.FICSSMinRating           = searchCriteria.MinRating.ToString();
            m_settings.FICSSMinTimePerPlayer    = searchCriteria.MinTimePerPlayer.ToString();
            m_settings.FICSSMaxTimePerPlayer    = searchCriteria.MaxTimePerPlayer.ToString();
            m_settings.FICSSMinIncTimePerMove   = searchCriteria.MinIncTimePerMove.ToString();
            m_settings.FICSSMaxIncTimePerMove   = searchCriteria.MaxIncTimePerMove.ToString();
            m_settings.FICSSMaxMoveDone         = searchCriteria.MaxMoveDone;
            m_settings.FICSMoveTimeOut          = searchCriteria.MoveTimeOut.ToString();
        }


    }
}
