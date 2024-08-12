using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace Five_Word_Five_Letters
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // User input
            Console.Write("Enter the number of words: ");
            int numberOfWords = int.Parse(Console.ReadLine());

            Console.Write("Enter the number of letters per word: ");
            int numberOfLetters = int.Parse(Console.ReadLine());

            Stopwatch stopwatch = Stopwatch.StartNew();

            // File reading
            string filePath = "C:\\Users\\HFGF\\Desktop\\Five Word Five Letters\\words_alpha.txt";
            string[] fileData = File.ReadAllLines(filePath);

            ConcurrentBag<(string word, int bitmask)> validWords = new ConcurrentBag<(string, int)>(); // gemmer ordene og deres bitmask
            Dictionary<char, int> charFrequency = new Dictionary<char, int>(); // tjekker hyppighed af hvert bogstav 
            ConcurrentDictionary<string, bool> uniqueWords = new ConcurrentDictionary<string, bool>(); // sikre at kun unikke ord bliver sorteret


            // tjekekr længde og hyppighed
            foreach (var line in fileData)
            {
                foreach (var word in line.Split(' '))
                {
                    if (word.Length == numberOfLetters)
                    {
                        foreach (char c in word)
                        {
                            if (charFrequency.ContainsKey(c))
                                charFrequency[c]++;
                            else
                                charFrequency[c] = 1;
                        }
                    }
                }
            }

            // Parallel Processing af ord
            Parallel.ForEach(fileData, line =>
            {
                string[] wordsInLine = line.Split(' ');
                foreach (var word in wordsInLine)
                {
                    if (word.Length == numberOfLetters) // tjekker om ordet er rigtig længde
                    {
                        string sortedWord = new string(word.OrderBy(c => c).ToArray());
                        if (uniqueWords.TryAdd(sortedWord, true)) // sortere ordende og tilføjer til uniqueWords 
                        {
                            int bitmask = 0;
                            bool isValid = true;
                            foreach (char c in word)
                            {
                                int bit = 1 << (c - 'a');
                                if ((bitmask & bit) != 0)
                                {
                                    isValid = false;
                                    break;
                                }
                                bitmask |= bit;
                            }
                            if (isValid)
                            {
                                validWords.Add((word, bitmask)); // laver bitmask og tifløjer til validWords
                            }
                        }
                    }
                }
            });

            var validWordsArray = validWords.ToArray(); // laver validWords til array
            int[] bitmasks = new int[validWordsArray.Length]; // putter bitmask i sit eget array
            string[] wordsArray = new string[validWordsArray.Length]; // putter ord i set eget array 
            for (int i = 0; i < validWordsArray.Length; i++)
            {
                bitmasks[i] = validWordsArray[i].bitmask;
                wordsArray[i] = validWordsArray[i].word;
            }

            // sorter index af ord baseret på hyppighed
            var sortedIndices = Enumerable.Range(0, wordsArray.Length)
                .OrderBy(i => wordsArray[i].Sum(c => charFrequency[c]))
                .ThenBy(i => wordsArray[i].Min(c => charFrequency[c]))
                .ToArray();


            // find de rigtige kombinationer
            int numberOfCombinations = 0;
            object lockObj = new object();

            Parallel.For(0, sortedIndices.Length, i =>
            {
                int index = sortedIndices[i];
                FindCombinations(bitmasks, wordsArray, index, 1, bitmasks[index], new string[numberOfWords], ref numberOfCombinations, lockObj, numberOfWords, numberOfLetters);
            });

            stopwatch.Stop();
            Console.WriteLine("There are " + numberOfCombinations + " valid combinations.");
            Console.WriteLine("Execution time: " + (stopwatch.ElapsedMilliseconds / 1000.0) + " seconds");
        }


        static void FindCombinations(int[] bitmasks, string[] wordsArray, int start, int depth, int combinedBitmask, string[] currentCombination, ref int numberOfCombinations, object lockObj, int numberOfWords, int numberOfLetters)
        {
            if (depth == numberOfWords)
            {
                if (BitOperations.PopCount((uint)combinedBitmask) == numberOfWords * numberOfLetters)
                {
                    lock (lockObj)
                    {
                        numberOfCombinations++;
                    }
                }
                return;
            }

            for (int i = start + 1; i < bitmasks.Length; i++)
            {
                if ((combinedBitmask & bitmasks[i]) == 0)
                {
                    currentCombination[depth] = wordsArray[i];
                    FindCombinations(bitmasks, wordsArray, i, depth + 1, combinedBitmask | bitmasks[i], currentCombination, ref numberOfCombinations, lockObj, numberOfWords, numberOfLetters);
                }
            }
        }
    }
}