// --------------------------------------------------------------
// BeebPerf - A BBC Micro Profiler
//
// Copyright (C) 2026  Mark John Leece
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the Free
// Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
// Boston, MA  02110-1301, USA.
// --------------------------------------------------------------

namespace BeebPerf.model
{
    //
    // Represents a tree node within a UX displayed tree. A tree
    // node has a parent, a set of children, a depth value, and
    // an expansion state indicating whether it is open or closed.
    //
    public class TreeNode<T> where T : TreeNode<T>
    {
        public TreeNode() 
        {
        }

        public void AddChild(T treeNode)
        {
            _Children.Add(treeNode);
            treeNode.Parent = (T)this;
            UpdateDepths(treeNode);
        }

        public int Depth
        {
            get => _Depth;
        }

        public int Count
        {
            get
            {
                int count = 1;
                foreach (var childNode in _Children)
                    count += childNode.Count;
                return count;
            }
        }

        public bool HasChildren
        {
            get => Children.Count > 0;
        }

        public IReadOnlyList<T> Children
        {
            get => _Children;
        }

        public void Sort(Comparison<T> comparison)
        {
            Sort(this, comparison);
        }

        private static void Sort(TreeNode<T> treeNode, Comparison<T> comparison)
        {
            var stack = new Stack<TreeNode<T>>();
            stack.Push(treeNode);

            while (stack.Count > 0)
            {
                treeNode = stack.Pop();

                if (treeNode._Children.Count > 1)
                    treeNode._Children.Sort(comparison);

                for (int i = treeNode._Children.Count - 1; i >= 0; i--)
                    stack.Push(treeNode._Children[i]);
            }
        }

        private static void UpdateDepths(TreeNode<T> treeNode)
        { 
            var stack = new Stack<TreeNode<T>>();
            stack.Push(treeNode);

            while (stack.Count > 0)
            {
                treeNode = stack.Pop();

                treeNode._Depth = treeNode.Parent!._Depth + 1;

                foreach (var child in treeNode.Children)
                    stack.Push(child);
            }
        }

        public enum ExpansionType
        {
            Closed,
            Open
        };

        public T? Parent;
        public ExpansionType Expansion;
        private int _Depth;
        private List<T> _Children = new();
    }
}