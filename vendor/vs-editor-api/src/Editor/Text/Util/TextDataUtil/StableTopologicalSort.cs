using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Orders elements of a linearized directed graph in a dependency order, while preserving the original order
    /// between elements with no dependencies.
    /// </summary>
    /// <remarks>This is an implementation of the Gapotchenko's stable topological sort algorithm,
    /// https://blog.gapotchenko.com/stable-topological-sort.
    /// </remarks>
    internal static class StableTopologicalSort
    {
        /// <summary>
        /// An element dependency function.
        /// </summary>
        /// <returns>
        /// <c>true</c> if x depends on y, <c>false</c> otherwise.
        /// </returns>
        public delegate bool TopologicalDependencyFunction<in T>(T x, T y);

        /// <summary>
        /// Orders elements of a linearized directed graph in a dependency order, while preserving the original order
        /// between elements with no dependencies.
        /// </summary>
        public static IEnumerable<T> Order<T>(
            IEnumerable<T> itemsToOrder,
            TopologicalDependencyFunction<T> dependencyFunction)
        {
            if (itemsToOrder == null)
                throw new ArgumentNullException(nameof(itemsToOrder));
            if (dependencyFunction == null)
                throw new ArgumentNullException(nameof(dependencyFunction));

            var itemsToOrderList = itemsToOrder.ToList();
            if (itemsToOrderList.Count < 2)
            {
                return itemsToOrder;
            }

            int itemsToOrderCount = itemsToOrderList.Count;

            var graph = DependencyGraph<T>.TryCreate(itemsToOrderList, dependencyFunction, EqualityComparer<T>.Default);
            if (graph == null)
            {
                return itemsToOrder;
            }

            Restart:
            for (int i = 0; i < itemsToOrderCount; ++i)
            {
                for (int j = 0; j < i; ++j)
                {
                    if (graph.DoesXHaveDirectDependencyOnY(itemsToOrderList[j], itemsToOrderList[i]))
                    {
                        bool jDependsOnI = graph.DoesXHaveTransientDependencyOnY(itemsToOrderList[j], itemsToOrderList[i]);
                        bool iDependsOnJ = graph.DoesXHaveTransientDependencyOnY(itemsToOrderList[i], itemsToOrderList[j]);

                        bool circularDependency = jDependsOnI && iDependsOnJ;

                        if (!circularDependency)
                        {
                            var t = itemsToOrderList[i];
                            itemsToOrderList.RemoveAt(i);

                            itemsToOrderList.Insert(j, t);
                            goto Restart;
                        }
                    }
                }
            }

            return itemsToOrderList;
        }

        private class DependencyGraph<T>
        {
            private IEqualityComparer<T> EqualityComparer { get; }

            public IDictionary<T, Node> Nodes { get; }

            public DependencyGraph(IEqualityComparer<T> equalityComparer, int n)
            {
                EqualityComparer = equalityComparer ?? throw new ArgumentNullException(nameof(equalityComparer));
                this.Nodes = new Dictionary<T, Node>(n, equalityComparer);
            }

            public class Node
            {
                private IList<T> _children = new FrugalList<T>();

                public IList<T> Children => _children ?? (_children = new FrugalList<T>());
            }

            public static DependencyGraph<T> TryCreate(
                IList<T> items,
                TopologicalDependencyFunction<T> dependencyFunction,
                IEqualityComparer<T> equalityComparer)
            {
                var graph = new DependencyGraph<T>(equalityComparer, items.Count);

                bool hasDependencies = false;

                for (int position = 0; position < items.Count; ++position)
                {
                    var element = items[position];

                    if (!graph.Nodes.TryGetValue(element, out Node node))
                    {
                        node = new Node();
                        graph.Nodes.Add(element, node);
                    }

                    foreach (var anotherElement in items)
                    {
                        if (equalityComparer.Equals(element, anotherElement))
                        {
                            continue;
                        }

                        if (dependencyFunction(element, anotherElement))
                        {
                            node.Children.Add(anotherElement);
                            hasDependencies = true;
                        }
                    }
                }

                if (!hasDependencies)
                {
                    return null;
                }

                return graph;
            }

            public bool DoesXHaveDirectDependencyOnY(T x, T y)
            {
                if (Nodes.TryGetValue(x, out Node node))
                {
                    if (node.Children.Contains(y, EqualityComparer))
                    {
                        return true;
                    }
                }

                return false;
            }

            private class DependencyWalker
            {
                private readonly DependencyGraph<T> _graph;
                private readonly HashSet<T> _visitedNodes;

                public DependencyWalker(DependencyGraph<T> graph)
                {
                    _graph = graph;
                    _visitedNodes = new HashSet<T>(graph.EqualityComparer);
                }

                public bool DoesXHaveTransientDependencyOnY(T x, T y)
                {
                    if (!_visitedNodes.Add(x))
                    {
                        return false;
                    }

                    if (_graph.Nodes.TryGetValue(x, out Node node))
                    {
                        if (node.Children.Contains(y, _graph.EqualityComparer))
                        {
                            return true;
                        }

                        foreach (var i in node.Children)
                        {
                            if (DoesXHaveTransientDependencyOnY(i, y))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }
            }

            public bool DoesXHaveTransientDependencyOnY(T x, T y)
            {
                var traverser = new DependencyWalker(this);
                return traverser.DoesXHaveTransientDependencyOnY(x, y);
            }
        }
    }
}
