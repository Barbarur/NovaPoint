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
            Console.WriteLine("Hello, World!\n");

            try
            {
                await new SolutionsTester().TestSolution();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}\n");
            }

            Console.WriteLine("Byebye, World!");

        }
    }
}