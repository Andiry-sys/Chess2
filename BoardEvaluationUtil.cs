using System;
using System.Collections.Generic;
using System.Reflection;

namespace SrcChess2 {
    /// <summary>Utility class creating and holding all board evaluator functions</summary>
    public class BoardEvaluationUtil {
        /// <summary>List of all board evaluator object</summary>
        private List<IBoardEvaluation>  m_listBoardEvaluator;

        /// <summary>
        /// Class constructor
        /// </summary>
        public BoardEvaluationUtil() {
            BuildBoardEvaluationList();
        }

        /// <summary>
        /// Creates all build evaluator objects using reflection to find them
        /// </summary>
        private void BuildBoardEvaluationList() {
            Assembly            assem;
            Type[]              arrType;
            IBoardEvaluation    boardEval;
            
            m_listBoardEvaluator = new List<IBoardEvaluation>(32);
            assem                = GetType().Assembly;
            arrType              = assem.GetTypes();
            foreach (Type type in arrType) {
                if (!type.IsInterface && type.GetInterface("IBoardEvaluation") != null) {
                     boardEval = (IBoardEvaluation)Activator.CreateInstance(type);
                     m_listBoardEvaluator.Add(boardEval);
                }
            }
        }

        /// <summary>
        /// Returns the list of board evaluators
        /// </summary>
        public List<IBoardEvaluation> BoardEvaluators {
            get {
                return(m_listBoardEvaluator);
            }
        }

        /// <summary>
        /// Find a board evaluator using its name
        /// </summary>
        /// <param name="strName">  Evaluation method name</param>
        /// <returns>
        /// Object
        /// </returns>
        public IBoardEvaluation FindBoardEvaluator(string strName) {
            IBoardEvaluation    boardEval = null;
            
            if (strName != null) {
                foreach (IBoardEvaluation boardEvalTmp in m_listBoardEvaluator) {
                    if (String.Compare(boardEvalTmp.Name, strName, true) == 0) {
                        boardEval = boardEvalTmp;
                        break;
                    }
                }
            }
            return(boardEval);
        }
    } // Class BoardEvaluationUtil
} // Namespace
