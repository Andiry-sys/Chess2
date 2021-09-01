using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SrcChess2.FICSInterface {
    /// <summary>
    /// Interaction logic for frmFindBlitzGame.xaml
    /// </summary>
    public partial class frmFindBlitzGame : Window {

        /// <summary>Connection to the server</summary>
        private FICSConnection  m_conn;
        /// <summary>Actual search criteria</summary>
        private SearchCriteria  m_searchCriteria;

        /// <summary>
        /// Ctor
        /// </summary>
        public frmFindBlitzGame() {
            m_searchCriteria    = new SearchCriteria();
            InitializeComponent();
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="conn">             FICS Connection</param>
        /// <param name="searchCriteria">   Search criteria</param>
        public frmFindBlitzGame(FICSConnection conn, SearchCriteria searchCriteria) : this() {
            m_conn              = conn;
            m_searchCriteria    = searchCriteria;
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
            MoveTimeout         = searchCriteria.MoveTimeOut;
            m_searchCriteria    = searchCriteria;
        }

        /// <summary>
        /// Game selected
        /// </summary>
        public FICSGame Game {
            get;
            private set;
        }

        /// <summary>
        /// Search criteria used to find the game
        /// </summary>
        public SearchCriteria SearchCriteria {
            get {
                return(m_searchCriteria);
            }
        }

        /// <summary>
        /// Player name
        /// </summary>
        public string PlayerName {
            get {
                return(textBoxPlayerName.Text.Trim());
            }
            set {
                textBoxPlayerName.Text = value;
            }
        }

        /// <summary>
        /// true to allow blitz
        /// </summary>
        public bool BlitzGame {
            get {
                return(checkBlitz.IsChecked == true);
            }
            set {
                checkBlitz.IsChecked = value;
            }
        }

        /// <summary>
        /// true to allow lightning
        /// </summary>
        public bool LightningGame {
            get {
                return(checkLightning.IsChecked == true);
            }
            set {
                checkLightning.IsChecked = value;
            }
        }

        /// <summary>
        /// true to allow untimed game
        /// </summary>
        public bool UntimedGame {
            get {
                return(checkUntimed.IsChecked == true);
            }
            set {
                checkUntimed.IsChecked = value;
            }
        }

        /// <summary>
        /// true to allow standard game
        /// </summary>
        public bool StandardGame {
            get {
                return(checkStandard.IsChecked == true);
            }
            set {
                checkStandard.IsChecked = value;
            }
        }

        /// <summary>
        /// true to force only rated player
        /// </summary>
        public bool IsRated {
            get {
                return(checkRated.IsChecked == true);
            }
            set {
                checkRated.IsChecked = value;
            }
        }

        /// <summary>
        /// Minimum player rating
        /// </summary>
        public int? MinRating {
            get {
                int?    iRetVal;

                iRetVal = SearchCriteria.CnvToNullableIntValue(textBoxMinRating.Text);
                return(iRetVal);
            }
            set {
                textBoxMinRating.Text = value.HasValue ? value.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Minimum player time
        /// </summary>
        public int? MinTimePerPlayer {
            get {
                int?    iRetVal;

                iRetVal = SearchCriteria.CnvToNullableIntValue(textBoxMinTimePerPlayer.Text);
                return(iRetVal);
            }
            set {
                textBoxMinTimePerPlayer.Text = value.HasValue ? value.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Maximum player time
        /// </summary>
        public int? MaxTimePerPlayer {
            get {
                int?    iRetVal;

                iRetVal = SearchCriteria.CnvToNullableIntValue(textBoxMaxTimePerPlayer.Text);
                return(iRetVal);
            }
            set {
                textBoxMaxTimePerPlayer.Text = value.HasValue ? value.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Minimum move increment time
        /// </summary>
        public int? MinIncTimePerMove {
            get {
                int? iRetVal;

                iRetVal = SearchCriteria.CnvToNullableIntValue(textBoxMinIncTimePerMove.Text);
                return(iRetVal);
            }
            set {
                textBoxMinIncTimePerMove.Text = value.HasValue ? value.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Maximum move increment time
        /// </summary>
        public int? MaxIncTimePerMove {
            get {
                int? iRetVal;

                iRetVal = SearchCriteria.CnvToNullableIntValue(textBoxMaxIncTimePerMove.Text);
                return(iRetVal);
            }
            set {
                textBoxMaxIncTimePerMove.Text = value.HasValue ? value.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Maximum move done
        /// </summary>
        public int MaxMoveDone {
            get {
                int iRetVal;

                if (!Int32.TryParse(textBoxMaxMoveCount.Text, out iRetVal)) {
                    iRetVal = -1;
                }
                return(iRetVal);
            }
            set {
                textBoxMaxMoveCount.Text = value.ToString();
            }
        }

        /// <summary>
        /// Move timeout in seconds
        /// </summary>
        public int? MoveTimeout {
            get {
                int?    iRetVal;

                iRetVal = SearchCriteria.CnvToNullableIntValue(textBoxMoveTimeout.Text);
                return(iRetVal);
            }
            set {
                textBoxMoveTimeout.Text = value.HasValue ? value.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Update the search criteria
        /// </summary>
        private SearchCriteria CreateCriteria() {
            SearchCriteria  searchCriteria;

            searchCriteria                    = new SearchCriteria();
            searchCriteria.PlayerName         = PlayerName;
            searchCriteria.BlitzGame          = BlitzGame;
            searchCriteria.LightningGame      = LightningGame;
            searchCriteria.UntimedGame        = UntimedGame;
            searchCriteria.StandardGame       = StandardGame;
            searchCriteria.IsRated            = IsRated;
            searchCriteria.MinRating          = MinRating;
            searchCriteria.MinTimePerPlayer   = MinTimePerPlayer;
            searchCriteria.MaxTimePerPlayer   = MaxTimePerPlayer;
            searchCriteria.MinIncTimePerMove  = MinIncTimePerMove;
            searchCriteria.MaxIncTimePerMove  = MaxIncTimePerMove;
            searchCriteria.MaxMoveDone        = MaxMoveDone;
            searchCriteria.MoveTimeOut        = MoveTimeout;
            return(searchCriteria);
        }

        /// <summary>
        /// Update the state
        /// </summary>
        private void UpdateState() {
            SearchCriteria  searchCriteria;

            searchCriteria = CreateCriteria();
            butOk.IsEnabled = searchCriteria.IsValid();
        }

        /// <summary>
        /// Called when a text box has changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void textBox_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateState();
        }

        /// <summary>
        /// Called when a check box has changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void check_Checked(object sender, RoutedEventArgs e) {
            UpdateState();
        }

        /// <summary>
        /// Called when a text box has changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void butOk_Click(object sender, RoutedEventArgs e) {
            List<FICSGame>          games;
            FICSGame                game;
            int                     iMinValue;
            IEnumerable<FICSGame>   enumGame;

            m_searchCriteria        = CreateCriteria();
            games                   = m_conn.GetGameList(true, 3);
            enumGame                = games.Where(x => !x.IsPrivate && m_searchCriteria.IsGameMeetCriteria(x));
            if (enumGame.Count() == 0) {
                game = null;
            } else {
                iMinValue   = enumGame.Min(x => x.NextMoveCount);
                game        = enumGame.FirstOrDefault(x => x.NextMoveCount == iMinValue);
            }
            if (game == null) {
                MessageBox.Show("No game found matching these criteria");
            } else {
                Game            = game;
                DialogResult    = true;
            }
        }

        /// <summary>
        /// Called when a text box has changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void butCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }
}
