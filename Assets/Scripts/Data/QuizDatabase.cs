using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    // Scalable question bank for the Wisdom Tree quiz — add questions here
    // (Inspector or another asset built the same way) without touching
    // QuizManager's code, per the master spec's "Quiz Database" requirement.
    [CreateAssetMenu(fileName = "QuizDatabase", menuName = "Radiant Orchard/Quiz Database")]
    public class QuizDatabase : ScriptableObject
    {
        public List<QuizQuestion> questions = new List<QuizQuestion>();
    }
}
