using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkovBot
{
    [Serializable()]
    public class Brain
    {
        public Int32 Order = 4;

        public TreeNode Forwards;
        public TreeNode Backwards;

        public List<String> KnownWords;
        public List<String> Banned;
        public List<String> Auxiliaries;
        public List<Tuple<String, String>> Swaps = new List<Tuple<String, String>>();

        

    }
}
