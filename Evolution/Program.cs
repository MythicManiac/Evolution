using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evolution
{
    class Program
    {
        static void Main(string[] args)
        {
            var history = new List<double[]>();

            var population = Population.CreateInitialPopulation(64, 3);
            while (population.GetHighestFitness() < 300)
            {
                history.Add(new double[] { population.GetHighestFitness(), population.GetAverageFitness(), population.GetLowestFitness() });
                population = Population.CreateSelectionPopulation(population);
            }
            Console.WriteLine(population.GetHighestFitness());
            Console.ReadKey();
            history.Add(new double[] { population.GetHighestFitness(), population.GetAverageFitness(), population.GetLowestFitness() });
            OutputData(history);
        }

        static void OutputData(List<double[]> data)
        {
            var stream = new FileStream("graph.dat", FileMode.Create);
            var writer = new StreamWriter(stream);

            var template = @"
var data = {{
    labels: {0},
    datasets: [
        {{
            label: ""Population highest"",
            fillColor: ""rgba(52, 152, 219, 0.2)"",
            strokeColor: ""rgba(52, 152, 219, 1)"",
            pointColor: ""rgba(52, 152, 219, 1)"",
            pointStrokeColor: ""#fff"",
            pointHighlightColor: ""#fff"",
            pointHighlightFill: ""rgba(52, 152, 219, 1)"",
            data: {1},
        }},
        {{
            label: ""Population average"",
            fillColor: ""rgba(155, 89, 182, 0.2)"",
            strokeColor: ""rgba(155, 89, 182, 1)"",
            pointColor: ""rgba(155, 89, 182, 1)"",
            pointStrokeColor: ""#fff"",
            pointHighlightColor: ""#fff"",
            pointHighlightFill: ""rgba(155, 89, 182, 1)"",
            data: {2},
        }},
        {{
            label: ""Population lowest"",
            fillColor: ""rgba(46, 204, 113, 0.2)"",
            strokeColor: ""rgba(46, 204, 113, 1)"",
            pointColor: ""rgba(46, 204, 113, 1)"",
            pointStrokeColor: ""#fff"",
            pointHighlightColor: ""#fff"",
            pointHighlightFill: ""rgba(46, 204, 113, 1)"",
            data: {3},
        }}
    ]
}};
";

            var labels = "[";
            for(int i = 0; i < data.Count; i++)
            {
                labels += string.Format("{0}, ", i);
            }
            labels = labels.Substring(0, labels.Length - 2);
            labels += "]";

            var datastrings = new string[] { "", "", "" };
            for(int i = 0; i < 3; i++)
            {
                datastrings[i] = "[";
                for(int j = 0; j < data.Count; j++)
                {
                    datastrings[i] += string.Format("{0}, ", data[j][i]);
                }
                datastrings[i] = datastrings[i].Substring(0, datastrings[i].Length - 2);
                datastrings[i] += "]";
            }

            writer.Write(string.Format(template, labels, datastrings[0], datastrings[1], datastrings[2]));
            writer.Flush();
            writer.Close();
        }
    }

    public class Population
    {
        public int Size { get; protected set; }
        public Unit[] Units { get; protected set; }
        public int Generation { get; protected set; }

        private bool _sorted;

        private Population(int size, int generation)
        {
            Size = size;
            Units = new Unit[size];
            Generation = generation;
            _sorted = false;
        }

        public void SortByFitness()
        {
            if (_sorted) return;

            for (int i = 0; i < Units.Length; i++)
            {
                if (i == 0) continue;

                var j = i;
                while (Units[j].GetFitness() > Units[j - 1].GetFitness())
                {
                    var temp = Units[j - 1];
                    Units[j - 1] = Units[j];
                    Units[j] = temp;

                    j--;
                    if (j == 0) break;
                }
            }

            _sorted = true;
        }

        public int GetHighestFitness()
        {
            if (!_sorted) SortByFitness();
            return Units[0].GetFitness();
        }
        
        public double GetAverageFitness()
        {
            double total = 0;
            for(int i = 0; i < Units.Length; i++)
            {
                total += Units[i].GetFitness();
            }
            return total / Units.Length;
        }

        public int GetLowestFitness()
        {
            if (!_sorted) SortByFitness();
            return Units[Units.Length - 1].GetFitness();
        }

        public static Population CreateSelectionPopulation(Population old)
        {
            old.SortByFitness();
            var count = (int)Math.Floor((double)old.Units.Length / 2);
            var candidates = new Unit[count];
            Array.Copy(old.Units, candidates, count);

            var population = new Population(old.Size, old.Generation + 1);
            var random = new Random();
            for(int i = 0; i < old.Size; i++)
            {
                population.Units[i] = Unit.CreateCrossoverUnit(candidates[random.Next(candidates.Length)], candidates[random.Next(candidates.Length)]);
            }

            return population;
        }

        public static Population CreateInitialPopulation(int size, int geneCount)
        {
            var population = new Population(size, 0);
            for(int i = 0; i < population.Units.Length; i++)
            {
                population.Units[i] = Unit.CreateRandomUnit(geneCount);
            }
            return population;
        }
    }

    public class Unit
    {
        public static Random Rng = new Random();
        public static byte[] Target = new byte[] { (byte)Rng.Next(0, 256), (byte)Rng.Next(0, 256), (byte)Rng.Next(0, 256) };

        public byte[] Genes { get; protected set; }

        private Unit(byte[] genes)
        {
            Genes = genes;
        }

        public int GetFitness()
        {
            var fitness = 0;
            for (int i = 0; i < Target.Length; i++)
            {
                fitness += (int)((double)(255 - (int)Math.Abs(Target[i] - Genes[i])) / 255 * 100);
            }
            return fitness;
        }

        public static Unit CreateCrossoverUnit(Unit u1, Unit u2)
        {
            var result = new byte[u1.Genes.Length];
            var cutoff = Rng.Next(1, u1.Genes.Length);
            for (int i = 0; i < u1.Genes.Length; i++)
            {
                if (i < cutoff) result[i] = u1.Genes[i];
                else result[i] = u2.Genes[i];

                if (Rng.NextDouble() > 0.95) result[i] += 1;
                else if (Rng.NextDouble() < 0.05) result[i] -= 1;
            }
            return new Unit(result);
        }

        public static Unit CreateRandomUnit(int geneCount)
        {
            var genes = new byte[3];
            Rng.NextBytes(genes);
            return new Unit(genes);
        }
    }
}
