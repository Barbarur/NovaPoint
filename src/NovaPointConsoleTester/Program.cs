using NovaPointLibrary;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Net.NetworkInformation;
using System.Xml.Linq;

namespace NovaPointConsoleTester
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            await new SolutionsTester(UILog).TestSolution();

            Console.WriteLine("Byebye, World!");

        }

        static void UILog(LogInfo logInfo)
        {
            if (!string.IsNullOrEmpty(logInfo.MainClassInfo)) { Console.WriteLine($"{logInfo.MainClassInfo}\n"); }
            if (logInfo.PercentageProgress != -1)
            {
                Console.WriteLine($"Progress: {logInfo.PercentageProgress}\n");
                Console.WriteLine($"{logInfo.PendingTime}\n");
            }

        }
    }
}