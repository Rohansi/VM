using System;
using System.IO;

namespace Assembler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: assembler input output");
                return;
            }

            var input = args[0];
            var output = args[1];

            try
            {
                var a = new Assembler(File.ReadAllText(input));
                File.WriteAllBytes(output, a.Binary);
                Console.WriteLine("Assembled to {0} bytes", a.Binary.Length);
            }
            catch (AssemblerException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
