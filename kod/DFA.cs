using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OOP_Deneme
{
    public class DFA
    {
        public List<State> States_list { get; set; }
        public List<Alphabet> Alphabet_list { get; set; }
        public List<Transition> Transitions_list { get; set; }

        public void LoadFromXml(string filePath)
        {
            States_list = new List<State>();
            Alphabet_list = new List<Alphabet>();
            Transitions_list = new List<Transition>();

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            // Parse states
            XmlNodeList stateNodes = doc.SelectNodes("//States/state");
            foreach (XmlNode stateNode in stateNodes)
            {
                State state = new State
                {
                    StateId = stateNode.SelectSingleNode("state_id").InnerText,
                    Name = stateNode.SelectSingleNode("name").InnerText,
                    Initial = bool.Parse(stateNode.SelectSingleNode("initial").InnerText),
                    Final = bool.Parse(stateNode.SelectSingleNode("final").InnerText)
                };
                States_list.Add(state);
            }

            // Parse alphabet
            XmlNodeList alphabetNodes = doc.SelectNodes("//Alphabet/condition");
            foreach (XmlNode alphabetNode in alphabetNodes)
            {
                Alphabet alphabet = new Alphabet
                {
                    AlphabetId = alphabetNode.SelectSingleNode("alphabet_id").InnerText,
                    Symbol = alphabetNode.SelectSingleNode("message").InnerText,
                    Limit = alphabetNode.SelectSingleNode("sınır")?.InnerText
                };
                Alphabet_list.Add(alphabet);
            }

            // Parse transitions
            XmlNodeList transitionNodes = doc.SelectNodes("//Transitions/transition");
            foreach (XmlNode transitionNode in transitionNodes)
            {
                Transition transition = new Transition
                {
                    From = transitionNode.SelectSingleNode("from").InnerText,
                    To = transitionNode.SelectSingleNode("to").InnerText,
                    ConditionId = transitionNode.SelectSingleNode("condition_id").InnerText
                };
                Transitions_list.Add(transition);
            }
        }
    }
}
