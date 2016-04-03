using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartialMixins
{
    public static class Enumerable
    {
        public static IEnumerable<TSource> OrderTopological<TSource>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TSource>> dependentOn)
        {
            // Uses https://en.wikipedia.org/w/index.php?title=Topological_sorting&oldid=710520157
            var allNods = source.ToArray(); // Cach the Values, could be expensive to iterate over source.

            // Generate dependency Graph
            var dependenceDictionary = new Dictionary<TSource, List<TSource>>();
            foreach (var n in allNods)
                dependenceDictionary[n] = new List<TSource>();
            foreach (var n in allNods)
                foreach (var dependendNode in dependentOn(n))
                    dependenceDictionary[dependendNode].Add(n);

            var notMarked = new HashSet<TSource>(allNods);
            var permanentlyMarked = new HashSet<TSource>();
            var temporaryMakred = new HashSet<TSource>();
            // L ← Empty list that will contain the sorted nodes
            var l = new Stack<TSource>();


            // while there are unmarked nodes do
            while (notMarked.Any())
            {
                // select an unmarked node n
                var n = notMarked.First();
                // visit(n)
                if (!Visit(n, notMarked, permanentlyMarked, temporaryMakred, dependenceDictionary, l))
                    throw new ArgumentException("Circle detected.", nameof(source));
            }

            return l;
        }

        private static bool Visit<TSource>(TSource n, HashSet<TSource> notMarked, HashSet<TSource> permanentlyMarked, HashSet<TSource> temporaryMakred, Dictionary<TSource, List<TSource>> dependenceDictionary, Stack<TSource> l)
        {
            // if n has a temporary mark then stop (not a DAG)
            if (temporaryMakred.Contains(n))
                return false;
            // if n is not marked(i.e.has not been visited yet) then
            if (notMarked.Contains(n))
            {
                // mark n temporarily
                temporaryMakred.Add(n);
                notMarked.Remove(n);

                // for each node m with an edge from n to m do
                foreach (var m in dependenceDictionary[n])
                    // visit(m)
                    if (!Visit(m, notMarked, permanentlyMarked, temporaryMakred, dependenceDictionary, l))
                        return false;

                // mark n permanently
                permanentlyMarked.Add(n);
                // unmark n temporarily
                temporaryMakred.Remove(n);
                // add n to head of L
                l.Push(n);
            }
            return true;
        }
    }

}
