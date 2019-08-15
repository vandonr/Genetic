﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Genetic
{
    class Runner
    {
        public const int NbCities = 1000;
        public const int PopSize = 1000;

        private readonly List<Tuple<ISolver, Action<int, float>>> _solvers;
        public volatile bool DoRun = true;

        public Runner(List<Tuple<ISolver, Action<int, float>>> solvers)
        {
            _solvers = solvers;
        }

        private class Instance
        {
            public ISolver Solver { get; }
            public List<Solution> Pop { get; set; }
            public Action<int, float> ScoreCallback { get; }
            public float Best { get; set; }

            public Instance(ISolver solver, List<Solution> pop, Action<int, float> scoreCallback)
            {
                Solver = solver;
                Pop = pop;
                ScoreCallback = scoreCallback;
                Best = float.MaxValue;
            }
        }

        public void Run(Random rand)
        {
            var problem = Problem.Generate(rand, NbCities);
            var initialPop = GeneratePop(problem, rand);
            var instances = _solvers.Select(s => new Instance(s.Item1, initialPop, s.Item2)).ToList();
            int generation = 0;
            while (DoRun)
            {
                generation++;
                foreach (var instance in instances)
                {
                    var score = instance.Pop.Min(s => s.Score);
                    if (score < instance.Best)
                    {
                        instance.Best = score;
                        instance.ScoreCallback(generation - 1, score);
                    }

                    var newBorns = new List<Solution>(instance.Solver.NbChildrenWanted);
                    for (int i = 0; i < instance.Solver.NbChildrenWanted; i++)
                    {
                        var parents = instance.Solver.SelectParents(instance.Pop);
                        var child = instance.Solver.Crossover(parents);
                        instance.Solver.Mutate(child);
                        newBorns.Add(new Solution(child, generation, problem));
                    }
                    instance.Pop.AddRange(newBorns);
                    instance.Pop = instance.Solver.Extinction(instance.Pop, generation);
                }
            }
        }

        private List<Solution> GeneratePop(Problem p, Random r)
        {
            var ordered = new int[NbCities];
            for (int i = 0; i < NbCities; i++)
            {
                ordered[i] = i;
            }

            var pop = new List<Solution>(PopSize);
            for (int i = 0; i < PopSize; i++)
            {
                var copy = new int[NbCities];
                Array.Copy(ordered, copy, NbCities);
                copy.Shuffle(r);
                pop.Add(new Solution(copy, 0, p));
            }

            return pop;
        }
    }
}
