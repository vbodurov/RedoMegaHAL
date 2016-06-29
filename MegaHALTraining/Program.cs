using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarkovBot;

namespace MegaHALTraining
{
    class Program
    {
        static void Main(string[] args)
        {
            Markov bot = new Markov();
            //*
            var sw = Stopwatch.StartNew();
            bot.LoadAuxiliaries("../../Files/Auxiliaries.txt");
            sw.Stop();
            Console.WriteLine($"Auxiliaries {sw.Elapsed.TotalMilliseconds.ToString("N0")} ms");

            sw = Stopwatch.StartNew();
            bot.LoadBanList("../../Files/Bans.txt");
            sw.Stop();
            Console.WriteLine($"Bans {sw.Elapsed.TotalMilliseconds.ToString("N0")} ms");

            sw = Stopwatch.StartNew();
            bot.LoadSwapList("../../Files/Swaps.txt");
            sw.Stop();
            Console.WriteLine($"Swaps {sw.Elapsed.TotalMilliseconds.ToString("N0")} ms");

            sw = Stopwatch.StartNew();
            bot.Train("../../Files/Training.txt");
            sw.Stop();
            Console.WriteLine($"Training {sw.Elapsed.TotalMilliseconds.ToString("N0")} ms");

            sw = Stopwatch.StartNew();
            bot.SaveBrain("../../Files/BRAIN.dat");
            sw.Stop();
            Console.WriteLine($"BRAIN {sw.Elapsed.TotalMilliseconds.ToString("N0")} ms");

            /*/
            Bot.LoadBrain("BRAIN");
            //*/
            while (true) {
                Console.Write("> ");
                String text = null;
                while (text == null)
                    text = Console.ReadLine();

                Console.WriteLine(bot.GenerateReply(text));
            }
        }
    }
}
