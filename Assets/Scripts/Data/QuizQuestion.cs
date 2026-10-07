using System;

namespace RadiantOrchard
{
    [Serializable]
    public class QuizQuestion
    {
        public string questionText;
        public string[] options = new string[3];
        public int correctIndex;
    }
}
