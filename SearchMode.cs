using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SrcChess2 {

    /// <summary>Search Options</summary>
    public class SearchMode {

        /// <summary>Threading mode</summary>
        public enum ThreadingModeE {
            /// <summary>No threading at all. User interface share the search one.</summary>
            Off                         = 0,
            /// <summary>Use a different thread for search and user interface</summary>
            DifferentThreadForSearch    = 1,
            /// <summary>Use one thread for each processor for search and one for user inetrface</summary>
            OnePerProcessorForSearch    = 2
        }
        
        /// <summary>Random mode</summary>
        public enum RandomModeE {
            /// <summary>No random</summary>
            Off                         = 0,
            /// <summary>Use a repetitive random</summary>
            OnRepetitive                = 1,
            /// <summary>Use random with time seed</summary>
            On                          = 2
        }
        
        /// <summary>Search options</summary>
        [Flags]
        public enum OptionE {
            /// <summary>Use MinMax search</summary>
            UseMinMax                   = 0,
            /// <summary>Use Alpha-Beta prunning function</summary>
            UseAlphaBeta                = 1,
            /// <summary>Use transposition table</summary>
            UseTransTable               = 2,
            /// <summary>Use iterative depth-first search on a fix ply count</summary>
            UseIterativeDepthSearch     = 8
        }

        /// <summary>Opening book create using EOL greater than 2500</summary>
        private static Book     m_book2500;
        /// <summary>Opening book create using unrated games</summary>
        private static Book     m_bookUnrated;
        /// <summary>Board evaluation for the white</summary>
        public IBoardEvaluation m_boardEvaluationWhite;
        /// <summary>Board evaluation for the black</summary>
        public IBoardEvaluation m_boardEvaluationBlack;
        /// <summary>Search option</summary>
        public OptionE          m_eOption;
        /// <summary>Threading option</summary>
        public ThreadingModeE   m_eThreadingMode;
        /// <summary>Maximum search depth (or 0 to use iterative deepening depth-first search with time out)</summary>
        public int              m_iSearchDepth;
        /// <summary>Time out in second if using iterative deepening depth-first search</summary>
        public int              m_iTimeOutInSec;
        /// <summary>Random mode</summary>
        public RandomModeE      m_eRandomMode;
        /// <summary>Book to use for player hint</summary>
        public Book             m_bookPlayer;
        /// <summary>Book to use for computer move</summary>
        public Book             m_bookComputer;


        /// <summary>
        /// Try to read a book from a file or resource if file is not found
        /// </summary>
        /// <param name="strBookName">  Book name</param>
        /// <returns>
        /// Book
        /// </returns>
        private static Book ReadBook(string strBookName) {
            Book    bookRetVal;
            bool    bSucceed = false;

            bookRetVal  = new Book();
            try {
                if (bookRetVal.ReadBookFromFile(strBookName + ".bin")) {
                    bSucceed = true;
                }
            } catch (Exception) {
            }
            if (!bSucceed) {
                try {
                    if (!bookRetVal.ReadBookFromResource("SrcChess2." + strBookName + ".bin")) {
                        bookRetVal = null;
                    }
                } catch (Exception) {
                    bookRetVal = null;
                }
            }
            return(bookRetVal);
        }

        /// <summary>
        /// Static Ctor
        /// </summary>
        static SearchMode() {
            m_book2500      = ReadBook("Book2500");
            m_bookUnrated   = ReadBook("BookUnrated");
        }
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="boardEvalWhite">   Board evaluation for white player</param>
        /// <param name="boardEvalBlack">   Board evaluation for black player</param>
        /// <param name="eOption">          Search options</param>
        /// <param name="eThreadingMode">   Threading mode</param>
        /// <param name="iSearchDepth">     Search depth</param>
        /// <param name="iTimeOutInSec">    Timeout in second</param>
        /// <param name="eRandomMode">      Random mode</param>
        /// <param name="bookPlayer">       Book use for player</param>
        /// <param name="bookComputer">     Book use for human</param>
        public SearchMode(IBoardEvaluation  boardEvalWhite,
                          IBoardEvaluation  boardEvalBlack,
                          OptionE           eOption,
                          ThreadingModeE    eThreadingMode,
                          int               iSearchDepth,
                          int               iTimeOutInSec,
                          RandomModeE       eRandomMode,
                          Book              bookPlayer,
                          Book              bookComputer) {
            m_eOption               = eOption;
            m_eThreadingMode        = eThreadingMode;
            m_iSearchDepth          = iSearchDepth;
            m_iTimeOutInSec         = iTimeOutInSec;
            m_eRandomMode           = eRandomMode;
            m_boardEvaluationWhite  = boardEvalWhite;
            m_boardEvaluationBlack  = boardEvalBlack;
            m_bookPlayer            = bookPlayer;
            m_bookComputer          = bookComputer;
        }

        /// <summary>
        /// Book builds from games of player having ELO greater or equal to 2500
        /// </summary>
        public static Book Book2500 {
            get {
                return(m_book2500);

            }
        }

        /// <summary>
        /// Book builds from games of unrated player
        /// </summary>
        public static Book BookUnrated {
            get {
                return(m_bookUnrated);
            }
        }

        /// <summary>
        /// Gets human search mode
        /// </summary>
        /// <returns>
        /// Search mode
        /// </returns>
        public string HumanSearchMode() {
            StringBuilder   strb = new StringBuilder();

            if ((m_eOption & SearchMode.OptionE.UseAlphaBeta) == SearchMode.OptionE.UseAlphaBeta) {
                strb.Append("Alpha-Beta ");
            } else {
                strb.Append("Min-Max ");
            }
            if (m_iSearchDepth == 0) {
                strb.Append("(Iterative " + m_iTimeOutInSec.ToString() + " secs) ");
            } else if ((m_eOption & SearchMode.OptionE.UseIterativeDepthSearch) == SearchMode.OptionE.UseIterativeDepthSearch) {
                strb.Append("(Iterative " + m_iSearchDepth.ToString() + " ply) ");
            } else {
                strb.Append(m_iSearchDepth.ToString() + " ply) ");
            }
            if (m_eThreadingMode == SearchMode.ThreadingModeE.OnePerProcessorForSearch) {
                strb.Append("using " + Environment.ProcessorCount.ToString() + " processor");
                if (Environment.ProcessorCount > 1) {
                    strb.Append('s');
                }
                strb.Append(". ");
            } else {
                strb.Append("using 1 processor. ");
            }
            return(strb.ToString());
        }
    } // Class SearchMode

    /// <summary>
    /// Global search mode setting. Keep the value of manual setting even if hard coded one is used
    /// </summary>
    public class SettingSearchMode {

        /// <summary>Opening book used by the computer</summary>
        public enum BookModeE {
            /// <summary>No opening book</summary>
            NoBook                      = 0,
            /// <summary>Use a book built from unrated games</summary>
            Unrated                     = 1,
            /// <summary>Use a book built from games by player with EOL greater then 2500</summary>
            ELOGT2500                   = 2
        }

        /// <summary>Difficulty level</summary>
        public enum DifficultyLevelE {
            /// <summary>Manual</summary>
            Manual                      = 0,
            /// <summary>Very easy: 2 ply, (no book, weak board evaluation for computer)</summary>
            VeryEasy                    = 1,
            /// <summary>Easy: 2 ply, (no book, normal board evaluation for computer)</summary>
            Easy                        = 2,
            /// <summary>Intermediate: 4 ply, (unrated book, normal board evaluation for computer)</summary>
            Intermediate                = 3,
            /// <summary>Hard: 4 ply, (ELO 2500 book, normal board evaluation for computer)</summary>
            Hard                        = 4,
            /// <summary>Hard: 6 ply, (ELO 2500 book, normal board evaluation for computer)</summary>
            VeryHard                    = 5
        }

        /// <summary>Evaluation method to be used</summary>
        public enum EvaluationModeE {
            /// <summary>Weak evaluation method to be used for very easy game</summary>
            Weak    = 0,
            /// <summary>Weak evaluation method to be used for everything but very easy game</summary>
            Basic   = 1
        }

        /// <summary>Difficulty level</summary>
        public DifficultyLevelE             DifficultyLevel { get; set; }
        /// <summary>Board evaluation for the white</summary>
        public BookModeE                    BookMode { get; set; }
        /// <summary>Search option</summary>
        public SearchMode.OptionE           Option { get; set; }
        /// <summary>Threading option</summary>
        public SearchMode.ThreadingModeE    ThreadingMode { get; set; }
        /// <summary>Maximum search depth (or 0 to use iterative deepening depth-first search with time out)</summary>
        public int                          SearchDepth { get; set; }
        /// <summary>Time out in second if using iterative deepening depth-first search</summary>
        public int                          TimeOutInSec { get; set; }
        /// <summary>Random mode</summary>
        public SearchMode.RandomModeE       RandomMode { get; set; }
        /// <summary>Board evaluation method for white player</summary>
        public IBoardEvaluation             WhiteBoardEvaluation { get; set; }
        /// <summary>Board evaluation method for black player</summary>
        public IBoardEvaluation             BlackBoardEvaluation { get; set; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="eDifficultyLevel"> Difficulty level</param>
        /// <param name="boardEvalWhite">   Board evaluation for white player</param>
        /// <param name="boardEvalBlack">   Board evaluation for black player</param>
        /// <param name="eOption">          Search options</param>
        /// <param name="eThreadingMode">   Threading mode</param>
        /// <param name="iSearchDepth">     Search depth</param>
        /// <param name="iTimeOutInSec">    Timeout in second</param>
        /// <param name="eRandomMode">      Random mode</param>
        /// <param name="eBookMode">        Book mode</param>
        public SettingSearchMode(DifficultyLevelE           eDifficultyLevel,
                                 IBoardEvaluation           boardEvalWhite,
                                 IBoardEvaluation           boardEvalBlack,
                                 SearchMode.OptionE         eOption,
                                 SearchMode.ThreadingModeE  eThreadingMode,
                                 int                        iSearchDepth,
                                 int                        iTimeOutInSec,
                                 SearchMode.RandomModeE     eRandomMode,
                                 BookModeE                  eBookMode) {
            DifficultyLevel         = eDifficultyLevel;
            WhiteBoardEvaluation    = boardEvalWhite;
            BlackBoardEvaluation    = boardEvalBlack;
            Option                  = eOption;
            ThreadingMode           = eThreadingMode;
            SearchDepth             = iSearchDepth;
            TimeOutInSec            = iTimeOutInSec;
            RandomMode              = eRandomMode;
            BookMode                = eBookMode;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="eDifficultyLevel"> Difficuly level</param>
        public SettingSearchMode(DifficultyLevelE eDifficultyLevel) : this(eDifficultyLevel, null, null, SearchMode.OptionE.UseAlphaBeta, SearchMode.ThreadingModeE.OnePerProcessorForSearch, 2, 0, SearchMode.RandomModeE.On, BookModeE.ELOGT2500) {
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public SettingSearchMode() : this(DifficultyLevelE.Easy) {
        }

        /// <summary>
        /// Gets the mode tooltip description
        /// </summary>
        /// <param name="eLevel">   Difficulty level</param>
        public static string ModeTooltip(DifficultyLevelE eLevel) {
            SettingSearchMode   searchMode = new SettingSearchMode(eLevel);

            return(searchMode.HumanSearchMode());
        }

        /// <summary>
        /// Gets the active computer book mode
        /// </summary>
        public BookModeE ActiveComputerBookMode {
            get {
                BookModeE   eRetVal;

                switch (DifficultyLevel) {
                case DifficultyLevelE.Manual:
                    eRetVal = BookMode;
                    break;
                case DifficultyLevelE.VeryEasy:
                case DifficultyLevelE.Easy:
                    eRetVal = BookModeE.NoBook;
                    break;
                case DifficultyLevelE.Intermediate:
                    eRetVal = BookModeE.Unrated;
                    break;
                case DifficultyLevelE.Hard:
                case DifficultyLevelE.VeryHard:
                default:
                    eRetVal = BookModeE.ELOGT2500;
                    break;
                }
                return(eRetVal);
            }
        }

        /// <summary>
        /// Gets the search mode from the setting
        /// </summary>
        /// <returns></returns>
        public SearchMode GetSearchMode() {
            SearchMode                  searchModeRetVal;
            Book                        bookComputer;
            SearchMode.OptionE          eOption;
            SearchMode.ThreadingModeE   eThreadingMode;
            int                         iTimeOutInSec;


            eOption         = SearchMode.OptionE.UseAlphaBeta | SearchMode.OptionE.UseIterativeDepthSearch | SearchMode.OptionE.UseTransTable;
            eThreadingMode  = SearchMode.ThreadingModeE.OnePerProcessorForSearch;
            iTimeOutInSec   = 0;
            switch(ActiveComputerBookMode) {
            case BookModeE.NoBook:
                bookComputer = null;
                break;
            case BookModeE.Unrated:
                bookComputer = SearchMode.BookUnrated;
                break;
            default:
                bookComputer = SearchMode.Book2500;
                break;
            }
            switch (DifficultyLevel) {
            case DifficultyLevelE.VeryEasy:
                searchModeRetVal    = new SearchMode(new BoardEvaluationWeak(),
                                                     new BoardEvaluationWeak(),
                                                     eOption,
                                                     eThreadingMode,
                                                     2 /*iSearchDepth*/,
                                                     iTimeOutInSec,
                                                     SearchMode.RandomModeE.On,
                                                     SearchMode.Book2500,
                                                     bookComputer);
                break;
            case DifficultyLevelE.Easy:
                searchModeRetVal    = new SearchMode(new BoardEvaluationBasic(),
                                                     new BoardEvaluationBasic(),
                                                     eOption,
                                                     eThreadingMode,
                                                     2 /*iSearchDepth*/,
                                                     iTimeOutInSec,
                                                     SearchMode.RandomModeE.On,
                                                     SearchMode.Book2500,
                                                     bookComputer);
                break;
            case DifficultyLevelE.Intermediate:
                searchModeRetVal    = new SearchMode(new BoardEvaluationBasic(),
                                                     new BoardEvaluationBasic(),
                                                     eOption,
                                                     eThreadingMode,
                                                     4 /*iSearchDepth*/,
                                                     iTimeOutInSec,
                                                     SearchMode.RandomModeE.On,
                                                     SearchMode.Book2500,
                                                     bookComputer);
                break;
            case DifficultyLevelE.Hard:
                searchModeRetVal    = new SearchMode(new BoardEvaluationBasic(),
                                                     new BoardEvaluationBasic(),
                                                     eOption,
                                                     eThreadingMode,
                                                     4 /*iSearchDepth*/,
                                                     iTimeOutInSec,
                                                     SearchMode.RandomModeE.On,
                                                     SearchMode.Book2500,
                                                     bookComputer);
                break;
            case DifficultyLevelE.VeryHard:
                searchModeRetVal    = new SearchMode(WhiteBoardEvaluation,
                                                     BlackBoardEvaluation,
                                                     eOption,
                                                     eThreadingMode,
                                                     6 /*iSearchDepth*/,
                                                     iTimeOutInSec,
                                                     SearchMode.RandomModeE.On,
                                                     SearchMode.Book2500,
                                                     bookComputer);
                break;
            default:
                searchModeRetVal = new SearchMode(WhiteBoardEvaluation,
                                                  BlackBoardEvaluation,
                                                  Option,
                                                  ThreadingMode,
                                                  SearchDepth,
                                                  TimeOutInSec,
                                                  RandomMode,
                                                  SearchMode.Book2500,
                                                  bookComputer);
                break;
            }
            return(searchModeRetVal);
        }

        /// <summary>
        /// Convert the search setting to a human form
        /// </summary>
        /// <returns>
        /// Search mode description
        /// </returns>
        public string HumanSearchMode() {
            StringBuilder   strb = new StringBuilder();
            SearchMode      searchMode = GetSearchMode();

            strb.Append(searchMode.HumanSearchMode());
            switch(ActiveComputerBookMode) {
            case BookModeE.NoBook:
                strb.Append("No opening book. ");
                break;
            case BookModeE.Unrated:
                strb.Append("Using unrated opening book. ");
                break;
            default:
                strb.Append("Using master opening book. ");
                break;
            }
            if (DifficultyLevel == DifficultyLevelE.VeryEasy) {
                strb.Append("Using weak board evaluation");
            }
            return(strb.ToString());
        }
    } // Class SettingSearchMode
} // Namespace
