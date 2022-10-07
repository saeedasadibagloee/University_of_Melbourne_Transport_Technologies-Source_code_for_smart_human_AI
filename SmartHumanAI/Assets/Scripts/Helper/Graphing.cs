using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Helper
{
    public class Graph<T>
    {
        private NodeList<T> _nodeSet;

        public Graph() : this(null) { }
        public Graph(NodeList<T> nodeSet)
        {
            _nodeSet = nodeSet ?? new NodeList<T>();
        }

        public override string ToString()
        {
            string str = "GRAPH:" + Environment.NewLine;

            foreach (GraphNode<T> node in _nodeSet)
            {
                str += "Node " + node.Value.ToString() + ": ";

                foreach (GraphNode<T> nodeNeighbour in node.Neighbors)
                {
                    str += nodeNeighbour.Value.ToString() + " ";
                }

                str += Environment.NewLine;
            }

            return str;
        }

        public void AddNode(GraphNode<T> node)
        {
            // adds a node to the graph
            _nodeSet.Add(node);
        }

        public void AddNode(T value)
        {
            // adds a node to the graph
            _nodeSet.Add(new GraphNode<T>(value));
        }

        public void AddDirectedEdge(GraphNode<T> from, GraphNode<T> to, bool traversed)
        {
            from.Neighbors.Add(to);
            from.Traversed.Add(traversed);
        }

        public void AddUndirectedEdge(GraphNode<T> from, GraphNode<T> to, bool traversed)
        {
            if (!from.Neighbors.Contains(to))
            {
                from.Neighbors.Add(to);
                from.Traversed.Add(traversed);
            }

            if (to.Neighbors.Contains(@from)) return;
            to.Neighbors.Add(@from);
            to.Traversed.Add(traversed);
        }

        public bool Contains(T value)
        {
            return _nodeSet.FindByValue(value) != null;
        }

        public GraphNode<T> GetNode(T value)
        {
            return (GraphNode<T>)_nodeSet.FindByValue(value);
        }

        public bool Remove(T value)
        {
            // first remove the node from the nodeset
            GraphNode<T> nodeToRemove = (GraphNode<T>)_nodeSet.FindByValue(value);
            if (nodeToRemove == null)
                // node wasn't found
                return false;

            // otherwise, the node was found
            _nodeSet.Remove(nodeToRemove);

            // enumerate through each node in the nodeSet, removing edges to this node
            foreach (GraphNode<T> gnode in _nodeSet)
            {
                int index = gnode.Neighbors.IndexOf(nodeToRemove);
                if (index == -1) continue;
                // remove the reference to the node and associated cost
                gnode.Neighbors.RemoveAt(index);
                gnode.Traversed.RemoveAt(index);
            }

            return true;
        }

        public NodeList<T> Nodes
        {
            get
            {
                return _nodeSet;
            }
        }

        public int Count
        {
            get { return _nodeSet.Count; }
        }

        internal void AddNodesAndConnect(T v1, T v2)
        {
            GraphNode<T> g1 = new GraphNode<T>(v1);
            GraphNode<T> g2 = new GraphNode<T>(v2);

            if (Contains(v1))
                g1 = GetNode(v1);
            else
                _nodeSet.Add(g1);

            if (Contains(v2))
                g2 = GetNode(v2);
            else
                _nodeSet.Add(g2);

            AddUndirectedEdge(g1, g2, false);
        }

        internal void SetTraversedFrom(T node1, T node2)
        {
            try
            {
                var nA = (GraphNode<T>)_nodeSet.FindByValue(node1);
                var nB = (GraphNode<T>)_nodeSet.FindByValue(node2);
                int i = nA.Neighbors.IndexOf(nB);
                nA.Traversed[i] = true;
            }
            catch (Exception e)
            {
                Debug.LogError("Could not SetTraversedFrom: " + e.ToString());
            }
        }
    }

    public class GraphNode<T> : Node<T>
    {
        private List<bool> _traversed;

        public GraphNode()
        { }
        public GraphNode(T value) : base(value) { }
        public GraphNode(T value, NodeList<T> neighbors) : base(value, neighbors) { }

        new public NodeList<T> Neighbors
        {
            get { return base.Neighbors ?? (base.Neighbors = new NodeList<T>()); }
        }

        public List<bool> Traversed
        {
            get { return _traversed ?? (_traversed = new List<bool>()); }
        }
    }

    public class Node<T>
    {
        // Private member-variables
        private T _data;
        private NodeList<T> _neighbors = null;

        public Node() { }
        public Node(T data) : this(data, null) { }
        public Node(T data, NodeList<T> neighbors)
        {
            _data = data;
            _neighbors = neighbors;
        }

        public T Value
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        protected NodeList<T> Neighbors
        {
            get
            {
                return _neighbors;
            }
            set
            {
                _neighbors = value;
            }
        }
    }

    public class NodeList<T> : Collection<Node<T>>
    {
        public NodeList() : base() { }

        public NodeList(int initialSize)
        {
            // Add the specified number of items
            for (int i = 0; i < initialSize; i++)
                Items.Add(default(Node<T>));
        }

        public Node<T> FindByValue(T value)
        {
            // search the list for the value
            foreach (Node<T> node in Items)
                if (node.Value.Equals(value))
                    return node;

            // if we reached here, we didn't find a matching node
            return null;
        }
    }
}
