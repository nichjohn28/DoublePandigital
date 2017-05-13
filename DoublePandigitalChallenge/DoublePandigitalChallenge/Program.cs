using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Combinatorics.Collections;


namespace DoublePandigitalChallenge
{
    // This solution uses the divide by eleven rule, which is a number is divisible by eleven when the alternating sum of 
    // its digits is divisible by eleven. This was accomplished by finding the difference between the sum of even and odd digits
    // of the number. For instance, 4473700 would be (4 + 7 + 7 + 0) - (4 + 3 + 0) = 11 which is divisible by 11. 
    // This rule, combined with finding the different combinations of numbers that equal the difference, and getting the permutations of those 
    // combinations is the approach I took.
    class Program
    {
        private const int DIVISOR = 11;
        private static int m_inputTotal;
        private static double m_total;
        private static Stopwatch _watch1 = new Stopwatch();

        static void Main(string[] args)
        {
            _watch1.Start();

            // input list
            var inputRange = new List<int> { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9 };

            var midIndex = inputRange.Count / 2;

            // finds the sum of all elements in the inputRange
            foreach (var ele in inputRange)
            {
                m_inputTotal += ele;
            }

            // find differences divisible by eleven in between the min and max differences
            var m_differences = GetDifferencesDivisibleByEleven(inputRange, midIndex);

            // create every possible 10 digit combination using numbers from inputRange
            var m_combos = CreateCombinations(inputRange, midIndex);

            foreach (var diff in m_differences)
            {
                // hack of the linear system equation for x - y = diff, x = (even placements), y = (odd placement) - solve for x
                var temp = diff + m_inputTotal;
                var oddDiff = temp / 2;

                // get the list of odd placement combinations            
                var m_oddPlacementCombinations = GetOddPlacementCombinations(m_combos, oddDiff);

                if (m_oddPlacementCombinations.Count > 0)
                {
                    var combinations = new List<ListDictionary>();
                    foreach (var oddCombo in m_oddPlacementCombinations)
                    {
                        var evenCombo = GetEvenPlacementCombination(inputRange, oddCombo);

                        // x - y == diff
                        // key: 1 = x combination for left-hand side of subtraction operator
                        // key: 2 = y combination for right-hand side of operator
                        var m_combinationPair = new ListDictionary();
                        m_combinationPair.Add(1, evenCombo);
                        m_combinationPair.Add(2, oddCombo);
                        combinations.Add(m_combinationPair);
                    }

                    foreach (var combinationPair in combinations)
                    {
                        // even placement numbers
                        var Xcombo = combinationPair[2] as List<int>;
                        // odd placement numbers
                        var Ycombo = combinationPair[1] as List<int>;

                        // get all of the possible permutations for each combination
                        var xPerms = new Permutations<int>(Xcombo as List<int>, GenerateOption.WithoutRepetition);
                        var yPerms = new Permutations<int>(Ycombo as List<int>, GenerateOption.WithoutRepetition);

                        // get number of zeros in the x combination
                        var zeros = Xcombo.Where(r => r == 0).Count();

                        // get number elements of the combination
                        var numElements = Xcombo.Count();

                        // calculate how many permutations of the even placement numbers that start without zero: 
                        // divide number of zeros by total elements in the list to get percentage with zeros,
                        // subtract from 1.0 to get the percentage without zeros, then multiply by the total permutations                
                        // 1.0 - (1 zero / 4 elements) = .75 * 24 perms = 18 perms without zero
                        if (zeros > 0)
                        {
                            // subtract from 1.0 to get the total percentage without zero
                            var percent = 1.0 - (double)zeros / numElements;

                            // multiply the permutation counts for the even and odd placement numbers
                            // 10 xPerms * 10 yPerms = 100 total possible permutations of that number
                            m_total += (percent * xPerms.Count) * yPerms.Count;
                        }
                        else
                        {
                            // multiply the permutation counts for the even and odd placement numbers
                            m_total += xPerms.Count * yPerms.Count;
                        }
                    }
                }
            }

            _watch1.Stop();
            var elapsedSeconds = (double)_watch1.ElapsedMilliseconds / 1000;

            Console.WriteLine("Number range:");
            var str = "";
            for (int i = 0; i < 10; i++)
            {
                str = i < 9 ? string.Format("{0}, {0}, ", (i).ToString()) : string.Format("{0}, {0}\n\n", (i).ToString());
                Console.Write(str);
            }
            Console.WriteLine("Total Pandigitals divisible by {0}: {1}\n", DIVISOR.ToString(), m_total.ToString());
            Console.WriteLine(string.Format("Elapsed Seconds: {0} ms", elapsedSeconds.ToString()));
            Console.ReadKey();
        }
        /// <summary>
        /// Gets all differences divisible by eleven between the min and max differences for that number
        /// </summary>
        /// <param name="inputRange"></param>
        /// <param name="midIndex"></param>
        /// <returns></returns>
        public static List<int> GetDifferencesDivisibleByEleven(List<int> inputRange, int midIndex)
        {
            var result = new List<int>();
            var diffs = new List<int>();
            var min = 0;
            var max = 0;

            SetMinAndMaxDifference(inputRange, midIndex, out min, out max);

            // find if the alternating sum of inputRange is even or odd
            bool isEvenTotal = m_inputTotal % 2 == 0 ? true : false;

            //find the numbers between the min and max sum that are divisible by 11
            for (int i = min; i < max; i++)
            {
                // test for divisibity by 11
                if (i % DIVISOR == 0)
                {
                    // weed out the odd differences, because we have an even alternating sum of the input range (90),
                    // both sides of the equation have to be of the same parity, and as a consequence the
                    // difference of x - y must be even as well.
                    if (isEvenTotal && i % 2 == 0)
                    {
                        result.Add(i);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the minimum and maximum differences possible for the input range
        /// </summary>
        /// <param name="inputRange"></param>
        /// <param name="midIndex"></param>
        /// <param name="minDiff"></param>
        /// <param name="maxDiff"></param>
        public static void SetMinAndMaxDifference(List<int> inputRange, int midIndex, out int minDiff, out int maxDiff)
        {
            var menuend1 = 0;
            var subtrahend1 = 0;

            // add up both sides of the inputRange to the middle index, adding numbers left to right and right to left
            // subtracting the two to get the min and max differences
            for (int i = 0; i < midIndex; i++)
            {

                menuend1 += inputRange[i];
                subtrahend1 += inputRange[inputRange.Count - (i + 1)];
            }

            minDiff = menuend1 - subtrahend1;
            maxDiff = subtrahend1 - menuend1;
        }

        public static IEnumerable<List<int>> CreateCombinations(List<int> inputRange, int midIndex)
        {
            var intialCombos = new Combinations<int>(inputRange, midIndex, GenerateOption.WithoutRepetition);

            // create list of Combinations
            var combos = new List<List<int>>();
            foreach (var cs in intialCombos)
            {
                combos.Add(cs.ToList());
            }

            // duplicate combos were created, so only return the distict values
            return combos.Distinct(new CustComparer());
        }
        /// <summary>
        /// Returns the sum of numbers in the combination that equal the difference
        /// </summary>
        /// <param name="combos"></param>
        /// <param name="diff"></param>
        /// <returns></returns>
        public static List<List<int>> GetOddPlacementCombinations(IEnumerable<List<int>> combos, int diff)
        {
            var oddPlacementCombinations = new List<List<int>>();            
            foreach (var combo in combos)
            {
                int sum = 0;
                for (int i = 0; i < combo.Count; i++)
                {
                    sum += combo[i];
                }
                if (sum == diff)
                {
                    oddPlacementCombinations.Add(combo);
                }
            }

            return oddPlacementCombinations;
        }
        /// <summary>
        /// Removes the odd number placement comination from the input range, remaining elements will be returned and used as the even number placement combinations
        /// </summary>
        /// <param name="inputRange"></param>
        /// <param name="oddCombo"></param>
        /// <returns></returns>
        public static List<int> GetEvenPlacementCombination(List<int> inputRange, List<int> oddCombo)
        {
            var evenPlacementNumbers = new List<int>(inputRange.Count);
            evenPlacementNumbers.AddRange(inputRange);

            // loop through and remove each number in the combination from the myValues
            foreach (var num in oddCombo)
            {
                for (int t = 0; t < evenPlacementNumbers.Count; t++)
                {
                    if (num == evenPlacementNumbers[t])
                    {
                        evenPlacementNumbers.RemoveAt(t);
                        break;
                    }
                }
            }

            return evenPlacementNumbers;
        }
    }

    public class CustComparer : IEqualityComparer<List<int>>
    {
        public bool Equals(List<int> x, List<int> y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(List<int> obj)
        {
            int hashCode = 0;

            for (var index = 0; index < obj.Count; index++)
            {
                hashCode ^= new { Index = index, Item = obj[index] }.GetHashCode();
            }

            return hashCode;
        }
    }

}
