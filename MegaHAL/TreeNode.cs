using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkovBot
{
    [Serializable()]
    public class TreeNode
    {
        public List<TreeNode> SubNodes;
        public int Usage = 0;
        [NonSerialized()] public bool Visited = false;
        public String Item;

        public TreeNode(String ItemData)
        {
            Item = ItemData;
            SubNodes = new List<TreeNode>();
        }

        public TreeNode FindChild(String ItemData)
        {
            return SubNodes.Find((x) => x.Item == ItemData);
        }

        public TreeNode FindAddChild(String ItemData)
        {
            TreeNode T = FindChild(ItemData);
            if (T == null)
            {
                T = new TreeNode(ItemData);
                SubNodes.Add(T);
            }
            T.Usage++;
            return T;
        }

    }
}
