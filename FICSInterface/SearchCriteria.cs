using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SrcChess2.FICSInterface {
    /// <summary>
    /// Search Criteria
    /// </summary>
    public class SearchCriteria {
        /// <summary>Game played by this player or any player if empty</summary>
        public  string  PlayerName;
        /// <summary>Allow blitz game</summary>
        public  bool    BlitzGame;
        /// <summary>Allow lightning game</summary>
        public  bool    LightningGame;
        /// <summary>Allow untimed game</summary>
        public  bool    UntimedGame;
        /// <summary>Allow standard game</summary>
        public  bool    StandardGame;
        /// <summary>Allow only rated game or any game if false</summary>
        public  bool    IsRated;
        /// <summary>Minimum player rating or no minimum if null</summary>
        public  int?    MinRating;
        /// <summary>Minimum playing time per player or null if no minimum</summary>
        public  int?    MinTimePerPlayer;
        /// <summary>Maximum playing time per player or null for no maximum</summary>
        public  int?    MaxTimePerPlayer;
        /// <summary>Minimum increment time per move or null for no minimum</summary>
        public  int?    MinIncTimePerMove;
        /// <summary>Maximum increment time per move or null for no maximum</summary>
        public  int?    MaxIncTimePerMove;
        /// <summary>Maximum move count</summary>
        public  int     MaxMoveDone;
        /// <summary>Number of second between move before a timeout occurs. null for infinite</summary>
        public  int?    MoveTimeOut;

        /// <summary>
        /// Ctor
        /// </summary>
        public SearchCriteria() {
        }

        /// <summary>
        /// Copy ctor
        /// </summary>
        /// <param name="searchCriteria"></param>
        public SearchCriteria(SearchCriteria searchCriteria) {
            PlayerName          = searchCriteria.PlayerName;
            BlitzGame           = searchCriteria.BlitzGame;
            LightningGame       = searchCriteria.LightningGame;
            UntimedGame         = searchCriteria.UntimedGame;
            StandardGame        = searchCriteria.StandardGame;
            IsRated             = searchCriteria.IsRated;
            MinRating           = searchCriteria.MinRating;
            MinTimePerPlayer    = searchCriteria.MinTimePerPlayer;
            MaxTimePerPlayer    = searchCriteria.MaxTimePerPlayer;
            MinIncTimePerMove   = searchCriteria.MinIncTimePerMove;
            MaxIncTimePerMove   = searchCriteria.MaxIncTimePerMove;
            MaxMoveDone         = searchCriteria.MaxMoveDone;
            MoveTimeOut         = searchCriteria.MoveTimeOut;
        }

        /// <summary>
        /// Creates a default search criteria
        /// </summary>
        /// <returns></returns>
        public static SearchCriteria CreateDefault() {
            SearchCriteria  searchCriteria;

            searchCriteria = new SearchCriteria();
            searchCriteria.PlayerName           = "";
            searchCriteria.BlitzGame            = true;
            searchCriteria.LightningGame        = true;
            searchCriteria.UntimedGame          = false;
            searchCriteria.StandardGame         = false;
            searchCriteria.IsRated              = true;
            searchCriteria.MinRating            = 1000;
            searchCriteria.MinTimePerPlayer     = 0;
            searchCriteria.MaxTimePerPlayer     = 3;
            searchCriteria.MinIncTimePerMove    = 0;
            searchCriteria.MaxIncTimePerMove    = 4;
            searchCriteria.MaxMoveDone          = 20;
            searchCriteria.MoveTimeOut          = 30;
            return(searchCriteria);
        }

        /// <summary>
        /// Returns if input is valid
        /// </summary>
        /// <returns>
        /// true, false
        /// </returns>
        public bool IsValid() {
            bool    bRetVal = true;

            if (!BlitzGame     && 
                !LightningGame &&
                !UntimedGame   &&
                !StandardGame) {
                bRetVal = false;
            } else if (IsRated && !MinRating.HasValue) {
                bRetVal = false;
            } else if (MinRating    < 0 ||
                        MinTimePerPlayer      < 0 ||
                        MaxTimePerPlayer      < 0 ||
                        MinIncTimePerMove   < 0 ||
                        MaxIncTimePerMove   < 0 ||
                        MaxMoveDone < 0) {
                bRetVal = false;
            } else if (MinTimePerPlayer     > MaxTimePerPlayer ||
                       MinIncTimePerMove    > MaxIncTimePerMove) {
                bRetVal = false;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Convert a string to a nullable int value
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        public static int? CnvToNullableIntValue(string strText) {
            int?    iRetVal;
            int     iVal;

            strText = strText.Trim();
            if (string.IsNullOrEmpty(strText)) {
                iRetVal = null;
            } else {
                if (Int32.TryParse(strText, out iVal)) {
                    iRetVal = iVal;
                } else {
                    iRetVal = -1;
                }
            }
            return(iRetVal);
        }

        /// <summary>
        /// true if game meets the criteria
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public bool IsGameMeetCriteria(FICSGame game) {
            bool    bRetVal;

            bRetVal = (string.IsNullOrEmpty(PlayerName)                        || 
                       string.Compare(PlayerName, game.WhitePlayer, true) == 0 ||
                       string.Compare(PlayerName, game.BlackPlayer, true) == 0);
            if (bRetVal) {
                switch (game.GameType) {
                case FICSGame.GameTypeE.Blitz:
                    bRetVal = BlitzGame;
                    break;
                case FICSGame.GameTypeE.Lightning:
                    bRetVal = LightningGame;
                    break;
                case FICSGame.GameTypeE.Untimed:
                    bRetVal = UntimedGame;
                    break;
                case FICSGame.GameTypeE.Standard:
                    bRetVal = StandardGame;
                    break;
                default:
                    bRetVal = false;
                    break;
                }
            }
            if (bRetVal && IsRated) {
                bRetVal = game.WhiteRating >= MinRating && game.BlackRating >= MinRating;
            }
            if (bRetVal && MinTimePerPlayer.HasValue) {
                bRetVal = game.PlayerTimeInMin >= MinTimePerPlayer;
            }
            if (bRetVal && MaxTimePerPlayer.HasValue) {
                bRetVal = game.PlayerTimeInMin <= MaxTimePerPlayer;
            }
            if (bRetVal && MinIncTimePerMove.HasValue) {
                bRetVal = game.IncTimeInSec >= MinIncTimePerMove;
            }
            if (bRetVal && MaxIncTimePerMove.HasValue) {
                bRetVal = game.IncTimeInSec <= MaxIncTimePerMove;
            }
            if (bRetVal) {
                bRetVal = game.NextMoveCount <= MaxMoveDone;
            }
            return(bRetVal);
        }
    } // Class SearchCriteria
} // Namespace
