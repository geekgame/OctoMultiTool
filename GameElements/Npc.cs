using ConsoleApp1.Core;

namespace ConsoleApp1.GameElements
{
    public class Npc
    {
        public NpcMindset mindset = new NpcMindset();
    }

    public class NpcMindset
    {
        public enum OpinionType { V_NO, NO, Neutral, YES, V_YES, CONFUSED };
        public Dictionary<string, PonderatedRandomList<OpinionType>> opinions = new Dictionary<string, PonderatedRandomList<OpinionType>>();

        // Confusion stuff
        public Dictionary<string, OpinionType> lastOpinionForSubject = new Dictionary<string, OpinionType>();
        int confusionLen = 0;
        int confusionMaxLen = 1;
        int lastCombo = 0;

        public void AddOpinion(string about, OpinionType type, int mult = 1)
        {
            // If last opinion is the same, add more weight to it
            if (lastOpinionForSubject.ContainsKey(about) && lastOpinionForSubject[about] == type)
            {
                lastCombo++;
                mult *= lastCombo;
            }

            // Else, if opposite, make it confused
            else if (lastOpinionForSubject.ContainsKey(about) && lastOpinionForSubject[about] != type && lastOpinionForSubject[about] != OpinionType.CONFUSED)
            {
                Console.WriteLine("Confused");
                type = OpinionType.CONFUSED;

                // Set confusion length
                confusionMaxLen *= 2;
                confusionLen = confusionMaxLen;

                // Set last opinion to confused
                lastOpinionForSubject[about] = OpinionType.CONFUSED;

                // Add opinion
                opinions[about].Add(type, mult);
            }

            // Else, lower weight
            else if (!lastOpinionForSubject.ContainsKey(about))
                mult /= 2;

            else
                mult = 1;

            if (confusionLen > 0)
            {
                confusionLen--;
                if (confusionLen == 0)
                {
                    confusionMaxLen /= 2;
                }
            }



            opinions[about].Add(type, mult);
            lastOpinionForSubject[about] = type;
        }
    }

}
