using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkovBot
{
    public class Context
    {
        private TreeNode[] Links;

        public Context(Int32 Order, TreeNode Start)
        {
            Links = new TreeNode[Order + 2];
            Links[0] = Start;

        }

        public void UpdateContext(String Word)
        {
            for (Int32 X = Links.Length - 1; X > 0; X--)
            {
                if (Links[X - 1] != null)
                    Links[X] = Links[X - 1].FindChild(Word);
            }
        }

        public void Impress(String[] Words)
        {
            foreach (String Word in Words)
                Impress(Word);
        }

        public void Impress(String Word)
        {
            for (Int32 X = Links.Length - 1; X > 0; X--)
            {
                if (Links[X - 1] != null)
                    Links[X] = Links[X - 1].FindAddChild(Word);
            }
        }

        public Int32 Branches()
        {
            return Links[0].SubNodes.Count;
        }

        public TreeNode LongestContext()
        {
            TreeNode Node = null;

            for (Int32 X = 0; X < Links.Length - 1; X++)
                if (Links[X] != null)
                    Node = Links[X];

            return Node;
        }

        public Boolean LinkValid(Int32 Link)
        {
            return Links[Link] != null;
        }

        public TreeNode FindSubNode(Int32 Link, String Item)
        {
            return Links[Link].SubNodes.Find((x) => x.Item == Item);
        }

        public TreeNode Node(Int32 Link)
        {
            return Links[Link];
        }

        public TreeNode Pick()
        {
            Random RNG = new Random();
            return Links[0].SubNodes[RNG.Next(0, Links[0].SubNodes.Count)];
        }
    }
}
