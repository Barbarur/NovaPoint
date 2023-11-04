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

            await new SolutionsTester().TestSolution();

            Console.WriteLine("Byebye, World!");

        }
    }
}