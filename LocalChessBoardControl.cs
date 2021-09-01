using System;
using System.Collections.Generic;
using System.IO;

namespace SrcChess2 {
    /// <summary>
    /// Override chess control to add information to the saved board
    /// </summary>
    internal class LocalChessBoardControl : ChessBoardControl {
        /// <summary>Father Window</summary>
        public  MainWindow  Father { get; set; }

        /// <summary>
        /// Class constructor
        /// </summary>
        public LocalChessBoardControl() : base() {
        }

        /// <summary>
        /// Load the game board
        /// </summary>
        /// <param name="reader">   Binary reader</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public override bool LoadGame(BinaryReader reader) {
            bool                        bRetVal;
            string                      strVersion;
            MainWindow.PlayingModeE     ePlayingMode;
                
            strVersion = reader.ReadString();
            if (strVersion == "SRCCHESS095") {
                bRetVal = base.LoadGame(reader);
                if (bRetVal) {
                    ePlayingMode            = (MainWindow.PlayingModeE)reader.ReadInt32();
                    
                } else {
                    bRetVal = false;
                }
            } else {
                bRetVal = false;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Save the game board
        /// </summary>
        /// <param name="writer">   Binary writer</param>
        public override void SaveGame(BinaryWriter writer) {
            writer.Write("SRCCHESS095");
            base.SaveGame(writer);
           
        }

        
       
    }
}
