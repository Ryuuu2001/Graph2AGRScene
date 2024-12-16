using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

public class GA
{
    public class Population
    {
        public double[][] decs;
        public double[] objs;
    }

    public static (double[][], double[], double[], double[][]) GeneticAlgorithm(double width, double height, double[] lower, double[] upper)
    {
        int D = 2 * Global.outsidePoints.Count + 4 * Global.insidePoints.Count;
        int N = 50;
        int G = 100;
        int MaxFEx = G * N;
        int FEx = 0;

        // 初始化种群
        var (Population, UpdatedFEx) = Initialization(D, lower, upper, N, FEx, width, height);
        FEx = UpdatedFEx;

        while (FEx < MaxFEx)
        {
            int[] MatingPool = TournamentSelection(2, N, FitnessSingle(Population));
            var SelectedPopulation = new Population
            {
                decs = MatingPool.Select(idx => Population.decs[idx]).ToArray(),
                objs = MatingPool.Select(idx => Population.objs[idx]).ToArray()
            };

            double[][] Offspring = OperatorGA(SelectedPopulation, lower, upper);
            double[] OffObj = AreaLoss(Offspring, width, height);
            FEx += OffObj.Length;

            double minValue = OffObj.Min();

            string filePath = @"C:\Users\Summcry\Desktop\IUI\Python\Exp\xiaorong.txt";
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, append: true)) // 续写模式
                {
                    // 写入最小值
                    writer.WriteLine(minValue);
                }
                Console.WriteLine("Minimum value written to file successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            string destinationFilePath = @"C:\Users\Summcry\Desktop\IUI\Python\Exp\50.txt";

            try
            {
                // 读取源文件的最后一行
                string lastLine = ReadLastLine(filePath);

                if (lastLine != null)
                {
                    // 将最后一行数据追加到目标文件中
                    AppendToFile(destinationFilePath, lastLine);
                    Console.WriteLine("Last data from source file appended to destination file successfully.");
                }
                else
                {
                    Console.WriteLine("Source file is empty or could not read the last line.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            static string ReadLastLine(string filePath)
            {
                string lastLine = null;

                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lastLine = line; // 每次读到新行时更新 lastLine
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while reading the file: {ex.Message}");
                }

                return lastLine;
            }

            static void AppendToFile(string filePath, string data)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(filePath, append: true)) // 续写模式
                    {
                        writer.WriteLine(data);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while writing to the file: {ex.Message}");
                }
            }

            double[][] CombinedDecs = Population.decs.Concat(Offspring).ToArray();
            double[] CombinedObjs = Population.objs.Concat(OffObj).ToArray();

            var CombinedPopulation = new Population
            {
                decs = CombinedDecs,
                objs = CombinedObjs
            };

            var sortedIndices = FitnessSingle(CombinedPopulation)
                .Select((value, index) => new { Value = value, Index = index })
                .OrderBy(pair => pair.Value)
                .Select(pair => pair.Index)
                .ToArray();

            Population.decs = sortedIndices.Take(N).Select(idx => CombinedPopulation.decs[idx]).ToArray();
            Population.objs = sortedIndices.Take(N).Select(idx => CombinedPopulation.objs[idx]).ToArray();
        }

        double[][] X = Population.decs;
        double[] Y = Population.objs;

        double[] R = X[0];

        double[][] PS = new double[D / 2][];
        for (int i = 0; i < D / 2; i++)
        {
            PS[i] = new double[] { Math.Round(R[i * 2], 2), Math.Round(R[i * 2 + 1], 2) };
        }

        for (int i = 0; i < D / 2; i++)
        {
            for (int j = i + 1; j < D / 2; j++)
            {
                if (Math.Abs(PS[i][0] - PS[j][0]) < 5)
                {
                    PS[j][0] = PS[i][0];
                }
                if (Math.Abs(PS[i][1] - PS[j][1]) < 5)
                {
                    PS[j][1] = PS[i][1];
                }
            }
        }

        for (int i = 0; i < D / 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                PS[i][j] = Math.Round(PS[i][j]);
            }
        }

        Global.PScopy = new double[D / 2][];
        for (int i = 0; i < D / 2; i++)
        {
            Global.PScopy[i] = new double[PS[i].Length];
            Array.Copy(PS[i], Global.PScopy[i], PS[i].Length);
        }

        return (X, Y, R, PS);
    }

    private static (Population, int) Initialization(int D, double[] lower, double[] upper, int N, int FEx, double width, double height)
    {
        double[][] PopDec = new double[N][];
        double[,] Boundary = new double[Global.nodes.Count, 4];

        int count = 0;

        while (count < N)
        {
            double[] newIndividual = new double[D];
            Random rand = new Random();

            for (int i = 0; i < D; i++)
            {
                double init = (upper[i] - lower[i]) * rand.NextDouble();
                if (i >= 2 * Global.outsidePoints.Count)
                {
                    while (init < 30)
                    {
                        init = (upper[i] - lower[i]) * rand.NextDouble();
                    }
                }

                newIndividual[i] = lower[i] + init;
            }

            bool IsArea = true;
            for (int j = 0; j < Global.outsidePoints.Count; j++)
            {
                if (Global.pointsMode[j] == 1)
                {
                    if (j == 0)
                    {
                        Boundary[j, 0] = 0;
                        Boundary[j, 1] = newIndividual[2 * j + 1];
                        Boundary[j, 2] = newIndividual[2 * j];
                        Boundary[j, 3] = 0;

                        if (newIndividual[2 * j] < 30 || newIndividual[2 * j + 1] < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                    else
                    {
                        Boundary[j, 0] = 0;
                        Boundary[j, 1] = newIndividual[2 * j + 1];
                        Boundary[j, 2] = newIndividual[2 * j];
                        Boundary[j, 3] = newIndividual[2 * (j - 1) + 1];

                        if (newIndividual[2 * j] < 30 || (newIndividual[2 * j + 1] - newIndividual[2 * (j - 1) + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                }
                else if (Global.pointsMode[j] == 2)
                {
                    if (Global.pointsMode[j - 1] == 1)
                    {
                        newIndividual[2 * j + 1] = newIndividual[2 * (j - 1) + 1];

                        Boundary[j, 0] = 0;
                        Boundary[j, 1] = height;
                        Boundary[j, 2] = newIndividual[2 * j];
                        Boundary[j, 3] = newIndividual[2 * j + 1];

                        if (newIndividual[2 * j] < 30 || (height - newIndividual[2 * j + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                    else
                    {
                        Boundary[j, 0] = newIndividual[2 * (j - 1)];
                        Boundary[j, 1] = height;
                        Boundary[j, 2] = newIndividual[2 * j];
                        Boundary[j, 3] = newIndividual[2 * j + 1];

                        if ((newIndividual[2 * j] - newIndividual[2 * (j - 1)]) < 30 || (height - newIndividual[2 * j + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                }
                else if (Global.pointsMode[j] == 3)
                {
                    if (Global.pointsMode[j - 1] == 2)
                    {
                        newIndividual[2 * j] = newIndividual[2 * (j - 1)];

                        Boundary[j, 0] = newIndividual[2 * j];
                        Boundary[j, 1] = height;
                        Boundary[j, 2] = width;
                        Boundary[j, 3] = newIndividual[2 * j + 1];

                        if ((width - newIndividual[2 * j]) < 30 || (height - newIndividual[2 * j + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                    else
                    {
                        Boundary[j, 0] = newIndividual[2 * j];
                        Boundary[j, 1] = newIndividual[2 * (j - 1) + 1];
                        Boundary[j, 2] = width;
                        Boundary[j, 3] = newIndividual[2 * j + 1];

                        if ((width - newIndividual[2 * j]) < 30 || (newIndividual[2 * (j - 1) + 1] - newIndividual[2 * j + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                }
                else if (Global.pointsMode[j] == 4)
                {
                    if (Global.pointsMode[j - 1] == 3)
                    {
                        newIndividual[2 * j + 1] = newIndividual[2 * (j - 1) + 1];

                        Boundary[j, 0] = newIndividual[2 * j];
                        Boundary[j, 1] = newIndividual[2 * j + 1];
                        Boundary[j, 2] = width;
                        Boundary[j, 3] = 0;

                        if ((width - newIndividual[2 * j]) < 30 || newIndividual[2 * j + 1] < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                    else
                    {
                        if (j == Global.outsidePoints.Count - 1)
                        {
                            newIndividual[2 * j] = newIndividual[0];
                        }

                        Boundary[j, 0] = newIndividual[2 * j];
                        Boundary[j, 1] = newIndividual[2 * j + 1];
                        Boundary[j, 2] = newIndividual[2 * (j - 1)];
                        Boundary[j, 3] = 0;

                        if ((newIndividual[2 * (j - 1)] - newIndividual[2 * j]) < 30 || newIndividual[2 * j + 1] < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                }
            }

            for (int j = 0; j < Global.insidePoints.Count; j++)
            {
                Boundary[Global.outsidePoints.Count + j, 0] = newIndividual[2 * Global.outsidePoints.Count + 4 * j];
                Boundary[Global.outsidePoints.Count + j, 1] = newIndividual[2 * Global.outsidePoints.Count + 4 * j + 1];
                Boundary[Global.outsidePoints.Count + j, 2] = newIndividual[2 * Global.outsidePoints.Count + 4 * j + 2];
                Boundary[Global.outsidePoints.Count + j, 3] = newIndividual[2 * Global.outsidePoints.Count + 4 * j + 3];

                if ((newIndividual[2 * Global.outsidePoints.Count + 4 * j + 2] - newIndividual[2 * Global.outsidePoints.Count + 4 * j]) < 30 || (newIndividual[2 * Global.outsidePoints.Count + 4 * j + 1] - newIndividual[2 * Global.outsidePoints.Count + 4 * j + 3]) < 30)
                {
                    IsArea = false;
                    break;
                }
            }

            bool IsNotOverlapping = true;
            for (int j = 0; j < Global.nodes.Count; j++)
            {
                for (int k = j + 1; k < Global.nodes.Count; k++)
                {
                    IsNotOverlapping = CheckNoOverlap(Boundary, j, k);

                    if (!IsNotOverlapping)
                        break;
                }

                if (!IsNotOverlapping)
                    break;
            }

            if (IsNotOverlapping && IsArea)
            {
                PopDec[count] = newIndividual;
                count++;
            }
        }

        double[] PopObj = AreaLoss(PopDec, width, height);

        // 找到 PopObj 中的最小值
        double minValue = PopObj.Min();

        string filePath = @"C:\Users\Summcry\Desktop\IUI\Python\Exp\xiaorong.txt";
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, append: true)) // 续写模式
            {
                // 写入最小值
                writer.WriteLine(minValue);
            }
            Console.WriteLine("Minimum value written to file successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        Population population = new Population
        {
            decs = PopDec,
            objs = PopObj
        };

        FEx += PopObj.Length;

        return (population, FEx);
    }

    private static double[][] OperatorGA(Population parent, double[] lower, double[] upper)
    {
        int N = parent.decs.Length;
        int D = parent.decs[0].Length;
        double proC = 1, disC = 20, proM = 1, disM = 20;

        double[][] Parent1 = new double[N / 2][];
        double[][] Parent2 = new double[N / 2][];
        Array.Copy(parent.decs, 0, Parent1, 0, N / 2);
        Array.Copy(parent.decs, N / 2, Parent2, 0, N / 2);

        var offspringList = new List<double[]>();

        double[,] Boundary = new double[Global.nodes.Count, 4];

        while (offspringList.Count < N)
        {
            double[][] newOffspring = GAreal(Parent1, Parent2, lower, upper, proC, disC, proM, disM);

            for (int i = 0; i < newOffspring.Length; i++)
            {
                bool IsArea = true;
                for (int j = 0; j < Global.outsidePoints.Count; j++)
                {
                    if (Global.pointsMode[j] == 1)
                    {
                        if (j == 0)
                        {
                            Boundary[j, 0] = 0;
                            Boundary[j, 1] = newOffspring[i][2 * j + 1];
                            Boundary[j, 2] = newOffspring[i][2 * j];
                            Boundary[j, 3] = 0;

                            if (newOffspring[i][2 * j] < 30 || newOffspring[i][2 * j + 1] < 30)
                            {
                                IsArea = false;
                                break;
                            }
                        }
                        else
                        {
                            Boundary[j, 0] = 0;
                            Boundary[j, 1] = newOffspring[i][2 * j + 1];
                            Boundary[j, 2] = newOffspring[i][2 * j];
                            Boundary[j, 3] = newOffspring[i][2 * (j - 1) + 1];

                            if (newOffspring[i][2 * j] < 30 || (newOffspring[i][2 * j + 1] - newOffspring[i][2 * (j - 1) + 1]) < 30)
                            {
                                IsArea = false;
                                break;
                            }
                        }
                    }
                    else if (Global.pointsMode[j] == 2)
                    {
                        if (Global.pointsMode[j - 1] == 1)
                        {
                            newOffspring[i][2 * j + 1] = newOffspring[i][2 * (j - 1) + 1];

                            Boundary[j, 0] = 0;
                            Boundary[j, 1] = Global.height;
                            Boundary[j, 2] = newOffspring[i][2 * j];
                            Boundary[j, 3] = newOffspring[i][2 * j + 1];

                            if (newOffspring[i][2 * j] < 30 || (Global.height - newOffspring[i][2 * j + 1]) < 30)
                            {
                                IsArea = false;
                                break;
                            }
                        }
                        else
                        {
                            Boundary[j, 0] = newOffspring[i][2 * (j - 1)];
                            Boundary[j, 1] = Global.height;
                            Boundary[j, 2] = newOffspring[i][2 * j];
                            Boundary[j, 3] = newOffspring[i][2 * j + 1];

                            if ((newOffspring[i][2 * j] - newOffspring[i][2 * (j - 1)]) < 30 || (Global.height - newOffspring[i][2 * j + 1]) < 30)
                            {
                                IsArea = false;
                                break;
                            }
                        }
                    }
                    else if (Global.pointsMode[j] == 3)
                    {
                        if (Global.pointsMode[j - 1] == 2)
                        {
                            newOffspring[i][2 * j] = newOffspring[i][2 * (j - 1)];

                            Boundary[j, 0] = newOffspring[i][2 * j];
                            Boundary[j, 1] = Global.height;
                            Boundary[j, 2] = Global.width;
                            Boundary[j, 3] = newOffspring[i][2 * j + 1];

                            if ((Global.width - newOffspring[i][2 * j]) < 30 || (Global.height - newOffspring[i][2 * j + 1]) < 30)
                            {
                                IsArea = false;
                                break;
                            }
                        }
                        else
                        {
                            Boundary[j, 0] = newOffspring[i][2 * j];
                            Boundary[j, 1] = newOffspring[i][2 * (j - 1) + 1];
                            Boundary[j, 2] = Global.width;
                            Boundary[j, 3] = newOffspring[i][2 * j + 1];

                            if ((Global.width - newOffspring[i][2 * j]) < 30 || (newOffspring[i][2 * (j - 1) + 1] - newOffspring[i][2 * j + 1]) < 30)
                            {
                                IsArea = false;
                                break;
                            }
                        }
                    }
                    else if (Global.pointsMode[j] == 4)
                    {
                        if (Global.pointsMode[j - 1] == 3)
                        {
                            newOffspring[i][2 * j + 1] = newOffspring[i][2 * (j - 1) + 1];

                            Boundary[j, 0] = newOffspring[i][2 * j];
                            Boundary[j, 1] = newOffspring[i][2 * j + 1];
                            Boundary[j, 2] = Global.width;
                            Boundary[j, 3] = 0;

                            if ((Global.width - newOffspring[i][2 * j]) < 30 || newOffspring[i][2 * j + 1] < 30)
                            {
                                IsArea = false;
                                break;
                            }
                        }
                        else
                        {
                            if (j == Global.outsidePoints.Count - 1)
                            {
                                newOffspring[i][2 * j] = newOffspring[i][0];
                            }

                            Boundary[j, 0] = newOffspring[i][2 * j];
                            Boundary[j, 1] = newOffspring[i][2 * j + 1];
                            Boundary[j, 2] = newOffspring[i][2 * (j - 1)];
                            Boundary[j, 3] = 0;

                            if ((newOffspring[i][2 * (j - 1)] - newOffspring[i][2 * j]) < 30 || newOffspring[i][2 * j + 1] < 30)
                            {
                                IsArea = false;
                                break;
                            }
                        }
                    }
                }

                for (int j = 0; j < Global.insidePoints.Count; j++)
                {
                    Boundary[Global.outsidePoints.Count + j, 0] = newOffspring[i][2 * Global.outsidePoints.Count + 4 * j];
                    Boundary[Global.outsidePoints.Count + j, 1] = newOffspring[i][2 * Global.outsidePoints.Count + 4 * j + 1];
                    Boundary[Global.outsidePoints.Count + j, 2] = newOffspring[i][2 * Global.outsidePoints.Count + 4 * j + 2];
                    Boundary[Global.outsidePoints.Count + j, 3] = newOffspring[i][2 * Global.outsidePoints.Count + 4 * j + 3];

                    if ((newOffspring[i][2 * Global.outsidePoints.Count + 4 * j + 2] - newOffspring[i][2 * Global.outsidePoints.Count + 4 * j]) < 30 || (newOffspring[i][2 * Global.outsidePoints.Count + 4 * j + 1] - newOffspring[i][2 * Global.outsidePoints.Count + 4 * j + 3]) < 30)
                    {
                        IsArea = false;
                        break;
                    }
                }

                bool IsNotOverlapping = true;
                for (int j = 0; j < Global.nodes.Count; j++)
                {
                    for (int k = j + 1; k < Global.nodes.Count; k++)
                    {
                        IsNotOverlapping = CheckNoOverlap(Boundary, j, k);

                        if (!IsNotOverlapping)
                            break;
                    }

                    if (!IsNotOverlapping)
                        break;
                }

                if (IsNotOverlapping && IsArea)
                {
                    offspringList.Add(newOffspring[i]);
                }
            }

            if (offspringList.Count >= N)
                break;
        }

        return offspringList.Take(N).ToArray();
    }

    private static double[] AreaLoss(double[][] PopDec, double width, double height)
    {
        int N = PopDec.Length;
        double[] r = new double[N];

        for (int i = 0; i < N; i++)
        {
            double[] x = PopDec[i];
            double y = 0;

            for (int j = 0; j < Global.outsidePoints.Count; j++)
            {
                if (Global.pointsMode[j] == 1)
                {
                    if (j == 0)
                    {
                        y += x[2 * j] * x[2 * j + 1];
                    }
                    else
                    {
                        y += x[2 * j] * (x[2 * j + 1] - x[2 * (j - 1) + 1]);
                    }
                }
                else if (Global.pointsMode[j] == 2)
                {
                    if (Global.pointsMode[j - 1] == 1)
                    {
                        y += x[2 * j] * (height - x[2 * j + 1]);
                    }
                    else
                    {
                        y += (x[2 * j] - x[2 * (j - 1)]) * (height - x[2 * j + 1]);
                    }
                }
                else if (Global.pointsMode[j] == 3)
                {
                    if (Global.pointsMode[j - 1] == 2)
                    {
                        y += (width - x[2 * j]) * (height - x[2 * j + 1]);
                    }
                    else
                    {
                        y += (width - x[2 * j]) * (x[2 * (j - 1) + 1] - x[2 * j + 1]);
                    }
                }
                else if (Global.pointsMode[j] == 4)
                {
                    if (Global.pointsMode[j - 1] == 3)
                    {
                        y += (width - x[2 * j]) * x[2 * j + 1];
                    }
                    else
                    {
                        y += (x[2 * (j - 1)] - x[2 * j]) * x[2 * j + 1];
                    }
                }
            }

            for (int j = 0; j < Global.insidePoints.Count; j++)
            {
                y += (x[2 * Global.outsidePoints.Count + 4 * j + 2] - x[2 * Global.outsidePoints.Count + 4 * j]) * (x[2 * Global.outsidePoints.Count + 4 * j + 1] - x[2 * Global.outsidePoints.Count + 4 * j + 3]);
            }

            r[i] = Math.Abs(y - height * width);
        }

        return r;
    }

    private static int[] TournamentSelection(int K, int N, double[] fitness)
    {
        // 将适应度数组转换为列向量形式
        var reshapedArray = fitness.Select(x => new double[] { x }).ToArray();

        // 获取唯一的行和它们的原始位置
        var uniqueRows = reshapedArray.Distinct().ToArray();
        var locations = reshapedArray.Select(x => Array.IndexOf(uniqueRows, x)).ToArray();

        // 按行排序并获取排序后的索引
        var sortedIndices = uniqueRows
            .Select((val, idx) => new { Value = val, Index = idx })
            .OrderBy(x => x.Value[0]) // 假设按适应度值排序
            .Select((x, idx) => new { x.Index, Rank = idx })
            .OrderBy(x => x.Index)
            .Select(x => x.Rank)
            .ToArray();

        // 生成随机父代索引矩阵
        var random = new Random();
        var parents = new int[K, N];
        for (int i = 0; i < K; i++)
        {
            for (int j = 0; j < N; j++)
            {
                parents[i, j] = random.Next(fitness.Length);
            }
        }

        // 找到每列中最好的父代
        var bestParents = new int[N];
        for (int j = 0; j < N; j++)
        {
            var bestIndex = 0;
            var bestRank = int.MaxValue;
            for (int i = 0; i < K; i++)
            {
                var currentRank = sortedIndices[locations[parents[i, j]]];
                if (currentRank < bestRank)
                {
                    bestRank = currentRank;
                    bestIndex = parents[i, j];
                }
            }
            bestParents[j] = bestIndex;
        }

        // 返回最优父代的索引
        return bestParents;
    }

    private static double[] FitnessSingle(Population population)
    {
        int N = population.decs.Length;
        double[] PopCon = new double[N];
        double[] Feasible = new double[N];

        double[,] Boundary = new double[Global.nodes.Count, 4];

        for (int i = 0; i < N; i++)
        {
            bool IsArea = true;
            for (int j = 0; j < Global.outsidePoints.Count; j++)
            {
                if (Global.pointsMode[j] == 1)
                {
                    if (j == 0)
                    {
                        Boundary[j, 0] = 0;
                        Boundary[j, 1] = population.decs[i][2 * j + 1];
                        Boundary[j, 2] = population.decs[i][2 * j];
                        Boundary[j, 3] = 0;

                        if (population.decs[i][2 * j] < 30 || population.decs[i][2 * j + 1] < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                    else
                    {
                        Boundary[j, 0] = 0;
                        Boundary[j, 1] = population.decs[i][2 * j + 1];
                        Boundary[j, 2] = population.decs[i][2 * j];
                        Boundary[j, 3] = population.decs[i][2 * (j - 1) + 1];

                        if (population.decs[i][2 * j] < 30 || (population.decs[i][2 * j + 1] - population.decs[i][2 * (j - 1) + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                }
                else if (Global.pointsMode[j] == 2)
                {
                    if (Global.pointsMode[j - 1] == 1)
                    {
                        population.decs[i][2 * j + 1] = population.decs[i][2 * (j - 1) + 1];

                        Boundary[j, 0] = 0;
                        Boundary[j, 1] = Global.height;
                        Boundary[j, 2] = population.decs[i][2 * j];
                        Boundary[j, 3] = population.decs[i][2 * j + 1];

                        if (population.decs[i][2 * j] < 30 || (Global.height - population.decs[i][2 * j + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                    else
                    {
                        Boundary[j, 0] = population.decs[i][2 * (j - 1)];
                        Boundary[j, 1] = Global.height;
                        Boundary[j, 2] = population.decs[i][2 * j];
                        Boundary[j, 3] = population.decs[i][2 * j + 1];

                        if ((population.decs[i][2 * j] - population.decs[i][2 * (j - 1)]) < 30 || (Global.height - population.decs[i][2 * j + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                }
                else if (Global.pointsMode[j] == 3)
                {
                    if (Global.pointsMode[j - 1] == 2)
                    {
                        population.decs[i][2 * j] = population.decs[i][2 * (j - 1)];

                        Boundary[j, 0] = population.decs[i][2 * j];
                        Boundary[j, 1] = Global.height;
                        Boundary[j, 2] = Global.width;
                        Boundary[j, 3] = population.decs[i][2 * j + 1];

                        if ((Global.width - population.decs[i][2 * j]) < 30 || (Global.height - population.decs[i][2 * j + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                    else
                    {
                        Boundary[j, 0] = population.decs[i][2 * j];
                        Boundary[j, 1] = population.decs[i][2 * (j - 1) + 1];
                        Boundary[j, 2] = Global.width;
                        Boundary[j, 3] = population.decs[i][2 * j + 1];

                        if ((Global.width - population.decs[i][2 * j]) < 30 || (population.decs[i][2 * (j - 1) + 1] - population.decs[i][2 * j + 1]) < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                }
                else if (Global.pointsMode[j] == 4)
                {
                    if (Global.pointsMode[j - 1] == 3)
                    {
                        population.decs[i][2 * j + 1] = population.decs[i][2 * (j - 1) + 1];

                        Boundary[j, 0] = population.decs[i][2 * j];
                        Boundary[j, 1] = population.decs[i][2 * j + 1];
                        Boundary[j, 2] = Global.width;
                        Boundary[j, 3] = 0;

                        if ((Global.width - population.decs[i][2 * j]) < 30 || population.decs[i][2 * j + 1] < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                    else
                    {
                        if (j == Global.outsidePoints.Count - 1)
                        {
                            population.decs[i][2 * j] = population.decs[i][0];
                        }

                        Boundary[j, 0] = population.decs[i][2 * j];
                        Boundary[j, 1] = population.decs[i][2 * j + 1];
                        Boundary[j, 2] = population.decs[i][2 * (j - 1)];
                        Boundary[j, 3] = 0;

                        if ((population.decs[i][2 * (j - 1)] - population.decs[i][2 * j]) < 30 || population.decs[i][2 * j + 1] < 30)
                        {
                            IsArea = false;
                            break;
                        }
                    }
                }
            }

            for (int j = 0; j < Global.insidePoints.Count; j++)
            {
                Boundary[Global.outsidePoints.Count + j, 0] = population.decs[i][2 * Global.outsidePoints.Count + 4 * j];
                Boundary[Global.outsidePoints.Count + j, 1] = population.decs[i][2 * Global.outsidePoints.Count + 4 * j + 1];
                Boundary[Global.outsidePoints.Count + j, 2] = population.decs[i][2 * Global.outsidePoints.Count + 4 * j + 2];
                Boundary[Global.outsidePoints.Count + j, 3] = population.decs[i][2 * Global.outsidePoints.Count + 4 * j + 3];

                if ((population.decs[i][2 * Global.outsidePoints.Count + 4 * j + 2] - population.decs[i][2 * Global.outsidePoints.Count + 4 * j]) < 30 || (population.decs[i][2 * Global.outsidePoints.Count + 4 * j + 1] - population.decs[i][2 * Global.outsidePoints.Count + 4 * j + 3]) < 30)
                {
                    IsArea = false;
                    break;
                }
            }

            bool IsNotOverlapping = true;
            if (IsArea)
            {
                for (int j = 0; j < Global.nodes.Count; j++)
                {
                    for (int k = j + 1; k < Global.nodes.Count; k++)
                    {
                        IsNotOverlapping = CheckNoOverlap(Boundary, j, k);

                        if (!IsNotOverlapping)
                            break;
                    }

                    if (!IsNotOverlapping)
                        break;
                }
            }

            if (IsArea && IsNotOverlapping)
            {
                PopCon[i] = 0;
            }
            else
            {
                PopCon[i] = 1;
            }

            Feasible[i] = PopCon[i] <= 0 ? 1 : 0;
        }

        double[] Fitness = new double[N];
        for (int i = 0; i < N; i++)
        {
            Fitness[i] = Feasible[i] * population.objs[i] + (1 - Feasible[i]) * (PopCon[i] + 1e10);
        }

        return Fitness;
    }

    private static bool CheckNoOverlap(double[,] Boundary, int i, int j)
    {
        double A_left = Boundary[i, 0];
        double A_top = Boundary[i, 1];
        double A_right = Boundary[i, 2];
        double A_bottom = Boundary[i, 3];

        double B_left = Boundary[j, 0];
        double B_top = Boundary[j, 1];
        double B_right = Boundary[j, 2];
        double B_bottom = Boundary[j, 3];

        // 判断不重叠的条件
        bool IsNotOverlapping = (A_right <= B_left) ||
                                (A_left >= B_right) ||
                                (A_bottom >= B_top) ||
                                (A_top <= B_bottom);

        return IsNotOverlapping;
    }

    private static double[][] GAreal(double[][] Parent1, double[][] Parent2, double[] lower, double[] upper, double proC, double disC, double proM, double disM)
    {
        int N = Parent1.Length;
        int D = Parent1[0].Length;
        double[][] beta = CreateMatrix(N, D);
        Random rand = new Random();
        double[][] mu = CreateMatrix(N, D);

        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < D; j++)
            {
                mu[i][j] = rand.NextDouble();
                if (mu[i][j] <= 0.5)
                    beta[i][j] = Math.Pow(2 * mu[i][j], 1 / (disC + 1));
                else
                    beta[i][j] = Math.Pow(2 - 2 * mu[i][j], -1 / (disC + 1));
                beta[i][j] *= rand.Next(2) == 0 ? -1 : 1;
                if (rand.NextDouble() < 0.5)
                    beta[i][j] = 1;
            }
        }

        for (int i = 0; i < N; i++)
        {
            if (rand.NextDouble() > proC)
            {
                for (int j = 0; j < D; j++)
                {
                    beta[i][j] = 1;
                }
            }
        }

        double[][] Offspring1 = CreateMatrix(N, D);
        double[][] Offspring2 = CreateMatrix(N, D);
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < D; j++)
            {
                Offspring1[i][j] = (Parent1[i][j] + Parent2[i][j]) / 2 + beta[i][j] * (Parent1[i][j] - Parent2[i][j]) / 2;
                Offspring2[i][j] = (Parent1[i][j] + Parent2[i][j]) / 2 - beta[i][j] * (Parent1[i][j] - Parent2[i][j]) / 2;
            }
        }

        double[][] Offspring = CreateMatrix(2 * N, D);
        Array.Copy(Offspring1, 0, Offspring, 0, Offspring1.Length);
        Array.Copy(Offspring2, 0, Offspring, Offspring1.Length, Offspring2.Length);

        double[][] Lower = CreateMatrix(2 * N, D, lower);
        double[][] Upper = CreateMatrix(2 * N, D, upper);

        bool[][] Site = CreateBoolMatrix(2 * N, D);
        mu = CreateMatrix(2 * N, D);

        for (int i = 0; i < 2 * N; i++)
        {
            for (int j = 0; j < D; j++)
            {
                Site[i][j] = rand.NextDouble() < proM / D;
                mu[i][j] = rand.NextDouble();
            }
        }

        for (int i = 0; i < 2 * N; i++)
        {
            for (int j = 0; j < D; j++)
            {
                Offspring[i][j] = Math.Min(Math.Max(Offspring[i][j], Lower[i][j]), Upper[i][j]);
            }
        }

        for (int i = 0; i < 2 * N; i++)
        {
            for (int j = 0; j < D; j++)
            {
                if (Site[i][j] && mu[i][j] <= 0.5)
                {
                    Offspring[i][j] += (Upper[i][j] - Lower[i][j]) * (Math.Pow(2 * mu[i][j] + (1 - 2 * mu[i][j]) * Math.Pow(1 - (Offspring[i][j] - Lower[i][j]) / (Upper[i][j] - Lower[i][j]), disM + 1), 1 / (disM + 1)) - 1);
                }
                else if (Site[i][j] && mu[i][j] > 0.5)
                {
                    Offspring[i][j] += (Upper[i][j] - Lower[i][j]) * (1 - Math.Pow(2 * (1 - mu[i][j]) + 2 * (mu[i][j] - 0.5) * Math.Pow(1 - (Upper[i][j] - Offspring[i][j]) / (Upper[i][j] - Lower[i][j]), disM + 1), 1 / (disM + 1)));
                }
            }
        }

        return Offspring;
    }

    private static double[][] CreateMatrix(int rows, int cols, double[] initialValues = null)
    {
        double[][] matrix = new double[rows][];
        for (int i = 0; i < rows; i++)
        {
            matrix[i] = new double[cols];
            if (initialValues != null)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i][j] = initialValues[j];
                }
            }
        }
        return matrix;
    }

    private static bool[][] CreateBoolMatrix(int rows, int cols)
    {
        bool[][] matrix = new bool[rows][];
        for (int i = 0; i < rows; i++)
        {
            matrix[i] = new bool[cols];
        }
        return matrix;
    }
}
