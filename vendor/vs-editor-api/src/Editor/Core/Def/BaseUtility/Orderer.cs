//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Performs a topological sort of orderable extension parts.
    /// </summary>
    public static class Orderer
    {
        // This is really sad but, because we actually convert all before/after attributes to upper case, we need upper case versions
        // of the highest and lowest priorities in order to find them. A better approach would be to use a case insensitive comparison
        // for the map but that could cause a behavior change.
        private readonly static string HighestUC = DefaultOrderings.Highest.ToUpperInvariant();
        private readonly static string HighUC = DefaultOrderings.High.ToUpperInvariant();
        private readonly static string DefaultUC = DefaultOrderings.Default.ToUpperInvariant();
        private readonly static string LowUC = DefaultOrderings.Low.ToUpperInvariant();
        private readonly static string LowestUC = DefaultOrderings.Lowest.ToUpperInvariant();

        /// <summary>
        /// Orders a list of items that are all orderable, that is, items that implement the IOrderable interface. 
        /// </summary>
        /// <param name="itemsToOrder">The list of items to sort.</param>
        /// <returns>The list of sorted items.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="itemsToOrder"/> is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006")]
        public static IList<Lazy<TValue, TMetadata>> Order<TValue, TMetadata>(IEnumerable<Lazy<TValue, TMetadata>> itemsToOrder)
            where TValue : class
            where TMetadata : IOrderable
        {
            if (itemsToOrder == null)
            {
                throw new ArgumentNullException(nameof(itemsToOrder));
            }

#if false && DEBUG
            Debug.WriteLine("Before ordering");
            DumpGraph(itemsToOrder);
#endif

            var roots = new Queue<Node<TValue, TMetadata>>();
            var unsortedItems = new List<Node<TValue, TMetadata>>();

            Orderer.PrepareGraph(itemsToOrder, roots, unsortedItems);
            IList<Lazy<TValue, TMetadata>> sortedItems = Orderer.TopologicalSort(roots, unsortedItems);

#if false && DEBUG
            Debug.WriteLine("After ordering");
            DumpGraph(sortedItems);
#endif

            return sortedItems;
        }

        private static void PrepareGraph<TValue, TMetadata>(IEnumerable<Lazy<TValue, TMetadata>> items, Queue<Node<TValue, TMetadata>> roots, List<Node<TValue, TMetadata>> unsortedItems)
            where TValue : class
            where TMetadata : IOrderable
        {
            Dictionary<string, Node<TValue, TMetadata>> map = new Dictionary<string, Node<TValue, TMetadata>>();
            foreach (Lazy<TValue, TMetadata> item in items)
            {
                if ((item != null) && (item.Metadata != null))
                {
                    var node = new Node<TValue, TMetadata>(item);

                    if (node.Name.Length != 0)
                    {
                        if (map.ContainsKey(node.Name))
                        {
                            //Nodes with duplicate names are ignored.
#if DEBUG
                            Debug.WriteLine("Duplicate name in Orderer.Order: " + node.Name);
#endif
                        }
                        else
                        {
                            map.Add(node.Name, node);
                            unsortedItems.Add(node);
                        }
                    }
                    else
                    {
                        //Even unnamed item are added to the unsortedItems. They can't be referred to but they still exist (and can affect ordering).
                        unsortedItems.Add(node);
                    }
                }
            }

            // Only resolve the exported nodes by counting down. Placeholders don't need to be resolved since they have no explicit
            // after or before (and, when created as a side-effect of resolving an exported node, are added to the end of the list).
            for (int i = unsortedItems.Count - 1; (i >= 0); --i)
            {
                unsortedItems[i].Resolve(map, unsortedItems);   //Placeholders are added to the end of unsorted items (and do not need to be resolved).
            }

            // Do the special handling for the highest and lowest placeholders. Specifically, unless something says that it is after Highest
            // then it gets the implicit constraint that it is before Highest.
            //
            // Note that if you have a situation like:
            //  if A declares it is after Highest & B declares that is is after A then, you need to put B in the "after highest" group (otherwise it
            //  will get the before highest constraint, which will cause a cycle).
            if (map.TryGetValue(HighestUC, out Node<TValue, TMetadata>  highest) && (highest.Before.Count != 0))
            {
                // There are one or more nodes that explicitly state that they come after highest
                // collect all of those nodes (and the nodes that come after them) into a single list.
                var afterHighest = new HashSet<Node<TValue, TMetadata>>();
                AddToAfterHighest(highest.Before, afterHighest);

                // Go through all of the nodes and, unless they are explicitly in the afterHighest group, add a constraint
                // that they are explicitly before highest.
                for (int i = unsortedItems.Count - 1; (i >= 0); --i)
                {
                    var n = unsortedItems[i];
                    if ((n != highest) && !afterHighest.Contains(n))
                    {
                        n.Before.Add(highest);
                        highest.After.Add(n);
                    }
                }
            }

            // Give lowest the same handling as highest, inverting the logic as appropriate.
            if (map.TryGetValue(LowestUC, out Node<TValue, TMetadata> lowest) && (lowest.After.Count != 0))
            {
                var beforeLowest = new HashSet<Node<TValue, TMetadata>>();
                AddToBeforeLowest(lowest.After, beforeLowest);

                for (int i = unsortedItems.Count - 1; (i >= 0); --i)
                {
                    var n = unsortedItems[i];
                    if ((n != lowest) && !beforeLowest.Contains(n))
                    {
                        n.After.Add(lowest);
                        lowest.Before.Add(n);
                    }
                }
            }

            AddPlaceHolders(map, LowestUC, LowUC, DefaultUC, HighUC, HighestUC);

            List<Node<TValue, TMetadata>> initialRoots = new List<Node<TValue, TMetadata>>();
            for (int i = unsortedItems.Count - 1; (i >= 0); --i)
            {
                var node = unsortedItems[i];
                if (node.After.Count == 0)
                {
                    initialRoots.Add(node);
                }
            }

            AddToRoots(roots, initialRoots);
        }

        private static void AddPlaceHolders<TValue, TMetadata>(Dictionary<string, Node<TValue, TMetadata>> map,
                                                               params string[] names)
            where TValue : class
            where TMetadata : IOrderable
        {
            // Make sure there's an explicit ordering where the node for name[0] come before the node for name[1], etc.
            //
            // If the node for a name doesn't exist, just skip it (no one else was using it so we don't need to order it
            // with respect to anything).
            Node<TValue, TMetadata> previousNode = null;
            for (int i = 0; (i < names.Length); ++i)
            {
                Node<TValue, TMetadata> node;
                if (map.TryGetValue(names[i], out node))
                {
                    if (previousNode != null)
                    {
                        previousNode.Before.Add(node);
                        node.After.Add(previousNode);
                    }

                    previousNode = node;
                }
            }
        }

        // We need to find all the nodes that are after Highest (or after a node that is after Highest, ad infinitum).
        private static void AddToAfterHighest<TValue, TMetadata>(IEnumerable<Node<TValue, TMetadata>> nodes, HashSet<Node<TValue, TMetadata>> afterHighest)
            where TValue : class
            where TMetadata : IOrderable
        {
            foreach (var n in nodes)
            {
                if (afterHighest.Add(n) && (n.Before.Count != 0))
                {
                    AddToAfterHighest(n.Before, afterHighest);
                }
            }
        }

        // The Before/Lowest analog of AddToAfterHighest.
        private static void AddToBeforeLowest<TValue, TMetadata>(IEnumerable<Node<TValue, TMetadata>> nodes, HashSet<Node<TValue, TMetadata>> beforeLowest)
            where TValue : class
            where TMetadata : IOrderable
        {
            foreach (var n in nodes)
            {
                if (beforeLowest.Add(n) && (n.After.Count != 0))
                {
                    AddToBeforeLowest(n.After, beforeLowest);
                }
            }
        }

        private static IList<Lazy<TValue, TMetadata>> TopologicalSort<TValue, TMetadata>(Queue<Node<TValue, TMetadata>> roots, List<Node<TValue, TMetadata>> unsortedItems)
            where TValue : class
            where TMetadata : IOrderable
        {
            List<Lazy<TValue, TMetadata>> sortedItems = new List<Lazy<TValue, TMetadata>>();

            while (unsortedItems.Count > 0)
            {
                Node<TValue, TMetadata> node = (roots.Count == 0) ? Orderer.BreakCircularReference(unsortedItems) : roots.Dequeue();

                Debug.Assert(node.After.Count == 0);

                if (node.Item != null)
                {
                    sortedItems.Add(node.Item);
                }

                unsortedItems.Remove(node);
                node.ClearBefore(roots);
            }

            return sortedItems;
        }

        private static void AddToRoots<TValue, TMetadata>(Queue<Node<TValue, TMetadata>> roots, List<Node<TValue, TMetadata>> newRoots)
            where TValue : class
            where TMetadata : IOrderable
        {
            newRoots.Sort((l, r) => string.CompareOrdinal(l.Name, r.Name));
            for (int i = 0; (i < newRoots.Count); ++i)
            {
                roots.Enqueue(newRoots[i]);
            }
        }

        private static Node<TValue, TMetadata> BreakCircularReference<TValue, TMetadata>(List<Node<TValue, TMetadata>> unsortedItems)
            where TValue : class
            where TMetadata : IOrderable
        {
            //We have a circular reference in the unsortedItems.
            //This is an error in the definition that we need to handle gracefully.

            //Find & report the cycle.
            List<List<Node<TValue, TMetadata>>> cycles = Orderer.FindCycles(unsortedItems);
            Debug.Assert(cycles.Count > 0);

#if DEBUG
            Debug.WriteLine("Orderer found cycles:");
            foreach (List<Node<TValue, TMetadata>> cycle in cycles)
            {
                foreach (Node<TValue, TMetadata> node in cycle)
                {
                    Debug.Write("\t" + node.Name);
                }
                Debug.WriteLine("");
            }
#endif

            //Find the cycle with the fewest inbound links from other cycles.
            int bestInwardLinkCount = int.MaxValue;
            List<Node<TValue, TMetadata>> bestCycle = null;
            for (int i = 0; (i < cycles.Count); ++i)
            {
                var cycle = cycles[i];
                int inwardLinkCount = 0;
                for (int j = 0; (j < cycle.Count); ++j)
                {
                    var node = cycle[j];
                    foreach (Node<TValue, TMetadata> child in node.After)
                    {
                        if (child.LowIndex != node.LowIndex)
                        {
                            ++inwardLinkCount;
                            break;
                        }
                    }
                }

                if (inwardLinkCount < bestInwardLinkCount)
                {
                    bestCycle = cycle;
                    bestInwardLinkCount = inwardLinkCount;
                }
            }

            //Given the best cycle we can find, pick the node that would break the smallest number of "after" constraints. 
            Node<TValue, TMetadata> bestNode;
            if (bestCycle == null)
            {
                //Odd, no cycles were found so we need to guess at random.
                bestNode = unsortedItems[0];
                Debug.Fail("Orderer was unable to find a cycle to break");
            }
            else
            {
                bestNode = bestCycle[0];
                for (int i = 1; (i < bestCycle.Count); ++i)
                {
                    Node<TValue, TMetadata> node = bestCycle[i];

                    if (node.After.Count < bestNode.After.Count)
                    {
                        bestNode = node;
                    }
                }
            }

            foreach (Node<TValue, TMetadata> a in bestNode.After)
            {
                a.Before.Remove(bestNode);
            }
            bestNode.After.Clear();

            return bestNode;
        }

        private static List<List<Node<TValue, TMetadata>>> FindCycles<TValue, TMetadata>(List<Node<TValue, TMetadata>> unsortedItems)
            where TValue : class
            where TMetadata : IOrderable
        {
            for (int i = 0; (i < unsortedItems.Count); ++i)
            {
                var n = unsortedItems[i];
                n.Index = -1;
                n.LowIndex = -1;
                n.ContainedInKnownCycle = false;
            }

            List<List<Node<TValue, TMetadata>>> cycles = new List<List<Node<TValue, TMetadata>>>();

            Stack<Node<TValue, TMetadata>> stack = new Stack<Node<TValue, TMetadata>>(unsortedItems.Count);
            int index = 0;
            for (int i = 0; (i < unsortedItems.Count); ++i)
            {
                var node = unsortedItems[i];
                if (node.Index == -1)
                {
                    Orderer.FindCycles(node, stack, ref index, cycles);
                    Debug.Assert(stack.Count == 0);
                }
            }

            return cycles;
        }

        private static void FindCycles<TValue, TMetadata>(Node<TValue, TMetadata> node, Stack<Node<TValue, TMetadata>> stack, ref int index, List<List<Node<TValue, TMetadata>>> cycles)
            where TValue : class
            where TMetadata : IOrderable
        {
            node.Index = index;
            node.LowIndex = index;
            ++index;

            stack.Push(node);

            foreach (Node<TValue, TMetadata> child in node.Before)
            {
                if (child.Index == -1)
                {
                    Orderer.FindCycles(child, stack, ref index, cycles);
                    node.LowIndex = Math.Min(node.LowIndex, child.LowIndex);
                }
                else if (!child.ContainedInKnownCycle)
                {
                    node.LowIndex = Math.Min(node.LowIndex, child.Index);
                }
            }

            if (node.Index == node.LowIndex)
            {
                List<Node<TValue, TMetadata>> cycle = new List<Node<TValue, TMetadata>>();
                while (stack.Count > 0)
                {
                    Node<TValue, TMetadata> child = stack.Pop();
                    cycle.Add(child);
                    child.ContainedInKnownCycle = true;

                    if (child == node)
                    {
                        //Single unit cycles aren't interesting (since we are preventing node from linking to themselves in the Resolve code below).
                        if (cycle.Count > 1)
                        {
                            cycles.Add(cycle);
                        }
                        break;
                    }

                    Debug.Assert(stack.Count > 0);
                }
            }
        }

#if DEBUG
        private static void DumpGraph<TValue, TMetadata>(IEnumerable<Lazy<TValue, TMetadata>> items)
            where TValue : class
            where TMetadata : IOrderable
        {
            int index = 0;
            foreach (Lazy<TValue, TMetadata> i in items)
            {
                if ((i != null) && (i.Metadata != null))
                {
                    Debug.WriteLine("\t{0}:{1}", ++index, i.Metadata.Name);
                    if (i.Metadata.After != null)
                    {
                        Debug.WriteLine("\t\tAfter:");
                        foreach (string a in i.Metadata.After)
                            if (!string.IsNullOrWhiteSpace(a))
                                Debug.WriteLine("\t\t\t" + a);
                    }

                    if (i.Metadata.Before != null)
                    {
                        Debug.WriteLine("\t\tBefore:");
                        foreach (string a in i.Metadata.Before)
                            if (!string.IsNullOrWhiteSpace(a))
                                Debug.WriteLine("\t\t\t" + a);
                    }
                }
            }
        }
#endif

        class Node<TValue, TMetadata>
            where TValue : class
            where TMetadata : IOrderable
        {
            public readonly string Name;
            public readonly Lazy<TValue, TMetadata> Item;

            private HashSet<Node<TValue, TMetadata>> _after = new HashSet<Node<TValue, TMetadata>>();
            public HashSet<Node<TValue, TMetadata>> After { get { return _after; } }

            private HashSet<Node<TValue, TMetadata>> _before = new HashSet<Node<TValue, TMetadata>>();
            public HashSet<Node<TValue, TMetadata>> Before { get { return _before; } }

            //Used to identify cycles
            public int Index = -1;
            public int LowIndex = -1;
            public bool ContainedInKnownCycle = false;

            public Node(Lazy<TValue, TMetadata> item)
            {
                string name = item.Metadata.Name;

                this.Name = string.IsNullOrEmpty(name) ? string.Empty : name.ToUpperInvariant();
                this.Item = item;
            }

            public Node(string name)
            {
                Debug.Assert(!string.IsNullOrEmpty(name));
                this.Name = name;
            }

            public void Resolve(Dictionary<string, Node<TValue, TMetadata>> map, List<Node<TValue, TMetadata>> unsortedItems)
            {
                this.Resolve(map, this.Item.Metadata.After, _after, unsortedItems);
                this.Resolve(map, this.Item.Metadata.Before, _before, unsortedItems);

                foreach (Node<TValue, TMetadata> b in _before)
                {
                    b._after.Add(this);
                }

                foreach (Node<TValue, TMetadata> a in _after)
                {
                    a._before.Add(this);
                }
            }

            public void ClearBefore(Queue<Node<TValue, TMetadata>> roots)
            {
                List<Node<TValue, TMetadata>> newRoots = new List<Node<TValue, TMetadata>>();
                foreach (Node<TValue, TMetadata> child in this.Before)
                {
                    child.After.Remove(this);

                    if (child.After.Count == 0)
                    {
                        newRoots.Add(child);
                    }
                }
                this.Before.Clear();

                Orderer.AddToRoots(roots, newRoots);
            }

            public override string ToString()
            {
                return this.Name;
            }

            private void Resolve(Dictionary<string, Node<TValue, TMetadata>> map, IEnumerable<string> links, HashSet<Node<TValue, TMetadata>> results, List<Node<TValue, TMetadata>> unsortedItems)
            {
                if (links != null)
                {
                    foreach (string link in links)
                    {
                        if (!string.IsNullOrEmpty(link))
                        {
                            string name = link.ToUpperInvariant();

                            Node<TValue, TMetadata> node;
                            if (!map.TryGetValue(name, out node))
                            {
                                //We need place-holder to handle the case where A comes before B and C comes after B but B is never defined.
                                //We still want C to come after A though so we need to create a "B".
                                //
                                //B doesn't show up in the output.
                                node = new Node<TValue, TMetadata>(name);

                                map.Add(name, node);
                                unsortedItems.Add(node);
                            }

                            //Ignore links directly back to itself
                            if (node != this)
                            {
                                results.Add(node);
                            }
                            else
                            {
                                Debug.WriteLine("Orderer.Node links to itself: " + node.Name);
                            }
                        }
                    }
                }
            }
        }
    }
}
