using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThreadedPrimes {
	class Program {
		// Number of threads to make.
		static int cores = 8;

		// Requested Limit of the numbers to be tested.
		static int reqLimit = 100000000;

		// Upper Limit of the numbers to be tested.
		// Increased by 2 because testing a number sometimes also requires testing the number 2
		//   greater than it. This is done for shorter code and less branching.
		static int upLimit = reqLimit + (2);

		// Path for the output file. 
		static string path = "./primes.txt";

		// Simple Primality test that works on numbers >= 5.
		// Works using known properties of primes to reduce the number of operations and branching.
		// Returns True when n is prime.
		static bool IsPrime(int n) {
			int i = 5;
			while (i * i <= n) {
				if ((n % i == 0) || (n % (i + 2) == 0)) {
					return false;
				}
				i += 6;
			}
			return true;
		}

		// Method to be used in spawned threads.
		// The startPos int is used to indicate the first number to be tested by each thread.
		// All prime numbers >= to 5 are of the form 6i +- 1. Therefore, only the numbers immediately
		//   above and below multiples of 6 need to be tested. To spread the workload evenly, the starting
		//   numbers are staggered by 6 and the loop is incremented by 6 * the number of threads.
		// Returns a BitArray with a partial list of primes.
		static BitArray PrimeThread(int startPos) {
			BitArray threadPrimes = new BitArray(upLimit + 1);

			int n = startPos * 6;

			if (n >= upLimit) {
				return threadPrimes;
			}

			for (int i = startPos * 6; i < upLimit; i += (6 * cores)) {
				threadPrimes[i - 1] = IsPrime(i - 1);
				threadPrimes[i + 1] = IsPrime(i + 1);
			}

			return threadPrimes;
		}


		static void Main(string[] args) {
			// Creating and initializing an Array to track the primes.
			// The index number itself will be used so 1 is added to the upper limit.
			// 2 and 3 are hard coded as primes to reduce the amount of branching and operations
			//   in the IsPrime method.
			BitArray primeArray = new BitArray(upLimit + 1) {
				[2] = true,
				[3] = true
			};

			// A BitArray is needed for each thread because writing to a BitArray is not threadsafe
			BitArray[] parts = new BitArray[cores];

			// List to be used to hold the created threads.
			List<Thread> threadList = new List<Thread>();

			// Creating a Stopwatch to measure the execution time of the threads
			Stopwatch watch = new Stopwatch();
			watch.Start();

			// Creating, starting, and storing each thread in a threadList while storing the returned 
			//   BitArrays in the parts array.
			// The loop counter needs to be copied to a separate variable to prevent race conditions.
			// The loop counter value is used to determine the starting number to be tested in the thread
			//   as well as the index in parts to store the result.
			for (int i = 1; i <= cores; i++) {
				int j = i;
				Thread t = new Thread(() => { parts[j - 1] = PrimeThread(j); });
				t.Start();
				threadList.Add(t);
			}

			// Making sure each thread has finished.
			foreach (var thread in threadList) {
				thread.Join();
			}

			// All threads have finished so stopping the Stopwatch to track the time.
			watch.Stop();
			TimeSpan ts = watch.Elapsed;

			/*
			// Combining the results of the threads into primeArray. Starting at 4 because 2 and 3 are hardcoded.
			for (int i = 4; i < upLimit; i++) {
				for (int j = 0; j < cores; j++) {
					if (parts[j][i]) {
						primeArray[i] = true;
						break;
					}
				}
			}
			*/

			// Combining the results of the threads into primeArray.
			// Because of the offsets and increments, each prime is only read once.
			for (int i = 0; i < cores; i++) {
				int j = (i + 1) * 6;
				for (; j < upLimit; j += (cores * 6)) {
					if (parts[i][j - 1]) {
						primeArray[j - 1] = true;
					}
					if (parts[i][j + 1]) {
						primeArray[j + 1] = true;
					}
				}
			}

			// Making sure the text file is blank.
			File.Create(path).Close();
			StreamWriter file = new StreamWriter(path, true);

			// Formatting the execution time.
			string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
			ts.Hours, ts.Minutes, ts.Seconds,
			ts.Milliseconds / 10);

			// Counting and summing the primes.
			int count = 0;
			long sum = 0;
			for (int i = 0; i <= reqLimit; i++) {
				if (primeArray[i]) {
					count++;
					sum += i;
				}
			}

			// Getting the top ten primes from lowest to highest
			int cDown = 9;
			string[] topTen = new string[10];
			for (int i = reqLimit; i > 0; i--) {
				if (primeArray[i]) {
					topTen[cDown] = i.ToString();
					cDown--;
					if (cDown < 0) {
						break;
					}
				}
			}


			file.WriteLine("Execution Time - " + elapsedTime);
			file.WriteLine("Primes Found - " + count);
			file.WriteLine("Sum of all Primes - " + sum);
			file.WriteLine("Top Ten Biggest Primes: ");
			file.Close();
			File.AppendAllLines(path, topTen);

			Console.WriteLine("Done");

			Console.ReadKey();
		}
	}
}
