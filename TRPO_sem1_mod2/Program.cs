using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace trpo_sem1_mod2;

public partial class Program
{
    static int[,] matrix;
    static object lockObject = new object();
    static bool[] resultsReady = new bool[3] { false, false, false };
    static int firstNonNegativeColumn = -1;
    static int[] sortedRowsIndices;
    static int negativeRowsSum = 0;

    static void Main()
    {
        matrix = new int[,] {
            { 1, -2, 3, 4 },
            { 2, 2, 3, 3 },
            { -1, 5, 6, 6 },
            { 4, 4, 4, 4 }
        };

        PrintMatrix("Початкова матриця:");

        Thread thread1 = new Thread(FindFirstNonNegativeColumn);
        Thread thread2 = new Thread(SortRowsByDuplicates);
        Thread thread3 = new Thread(SumRowsWithNegative);

        thread1.Start();
        thread2.Start();
        thread3.Start();

        thread1.Join();
        thread2.Join();
        thread3.Join();

        Console.WriteLine($"\nПерший стовпець без вiд'ємних елементiв: {firstNonNegativeColumn + 1}");

        Console.WriteLine("\nВiдсортованi рядки за кiлькiстю однакових елементiв:");
        foreach (int rowIndex in sortedRowsIndices)
        {
            PrintRow(rowIndex);
        }

        Console.WriteLine($"\nСума елементiв у рядках з вiд'ємними числами: {negativeRowsSum}");
    }

    static void FindFirstNonNegativeColumn()
    {
        int columns = matrix.GetLength(1);
        int rows = matrix.GetLength(0);

        for (int col = 0; col < columns; col++)
        {
            bool hasNegative = false;
            for (int row = 0; row < rows; row++)
            {
                if (matrix[row, col] < 0)
                {
                    hasNegative = true;
                    break;
                }
            }

            if (!hasNegative)
            {
                lock (lockObject)
                {
                    firstNonNegativeColumn = col;
                    resultsReady[0] = true;
                    Monitor.PulseAll(lockObject);
                }
                return;
            }
        }

        lock (lockObject)
        {
            firstNonNegativeColumn = -1;
            resultsReady[0] = true;
            Monitor.PulseAll(lockObject);
        }
    }

    static void SortRowsByDuplicates()
    {
        int rows = matrix.GetLength(0);
        var duplicateCounts = new Dictionary<int, int>();

        for (int i = 0; i < rows; i++)
        {
            var rowElements = GetRow(i);
            duplicateCounts[i] = rowElements.GroupBy(x => x).Sum(g => g.Count() - 1);
        }

        lock (lockObject)
        {
            sortedRowsIndices = duplicateCounts.OrderBy(x => x.Value)
                                             .Select(x => x.Key)
                                             .ToArray();
            resultsReady[1] = true;
            Monitor.PulseAll(lockObject);
        }
    }

    static void SumRowsWithNegative()
    {
        int rows = matrix.GetLength(0);
        int sum = 0;

        for (int i = 0; i < rows; i++)
        {
            var rowElements = GetRow(i);
            if (rowElements.Any(x => x < 0))
            {
                sum += rowElements.Sum();
            }
        }

        lock (lockObject)
        {
            negativeRowsSum = sum;
            resultsReady[2] = true;
            Monitor.PulseAll(lockObject);
        }
    }

    static int[] GetRow(int rowIndex)
    {
        int columns = matrix.GetLength(1);
        int[] row = new int[columns];
        for (int j = 0; j < columns; j++)
        {
            row[j] = matrix[rowIndex, j];
        }
        return row;
    }

    static void PrintMatrix(string message)
    {
        Console.WriteLine(message);
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Console.Write($"{matrix[i, j],4}");
            }
            Console.WriteLine();
        }
    }

    static void PrintRow(int rowIndex)
    {
        int columns = matrix.GetLength(1);
        for (int j = 0; j < columns; j++)
        {
            Console.Write($"{matrix[rowIndex, j],4}");
        }
        Console.WriteLine();
    }
}