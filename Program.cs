using System;
using System.Diagnostics;

namespace SingleCycleMIPS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool ReverseByteOrder = false;
            bool StepByStep = false;
            bool Output = false;
            bool Stats = false;
            if (args.Length == 0)
            {
                var domain = AppDomain.CurrentDomain.FriendlyName;
                Console.WriteLine(String.Format("usage: {0} <input-file>", domain));
                Console.WriteLine(String.Format("usage: {0} <input-file> [-step] [-r] [-no] [-stat]", domain));
                Console.WriteLine("--- Option List ---");
                Console.WriteLine("-r\tChange binary order");
                Console.WriteLine("-step\tStep by step execution by pressing any key");
                Console.WriteLine("-output\tShow accesses to memory and registers");
                Console.WriteLine("-stat\tShow execution statistics");
                return;
            }

            if (args.Contains("-r"))
            {
                ReverseByteOrder = true;
            }

            if (args.Contains("-step")) {
                StepByStep = true;
            }

            if (args.Contains("-output"))
            {
                Output = true;
            }

            if (args.Contains("-stat"))
            {
                Stats = true;
            }

            List<int> code = new();
            try
            {
                var path = args[0];
                var read = File.ReadAllBytes(path);
                if (read.Length % 4 != 0)
                {
                    Console.WriteLine("[WARNING] The number of bytes in the file is incorrect. Any remaining bytes are discarded.");
                }

                int leftByte = read.Length % 4;
                for (int i = 0; i < read.Length - leftByte; i += 4)
                {
                    int integer = BitConverter.ToInt32(read, i);

                    if (BitConverter.IsLittleEndian && !ReverseByteOrder)
                        code.Add(ChangeByteOrderInt32(integer));
                    else
                        code.Add(integer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Error.WriteLine(String.Format("Fail to read {0}.", args[0]));
                return;
            }

            if (code.Count == 0)
            {
                Console.Error.WriteLine("The binary file is empty.");
                return;
            }

            MIPS mips = new(code);
            if (StepByStep)
            {
                mips.OnBeforeStep = () => { Console.ReadLine(); };
            }

            if (Output)
            {
                Console.WriteLine("[Warning] Using the output feature will make execution very slow.");
                Thread.Sleep(3000);
                mips.Memory.OnWrite = (address, value) => { Console.WriteLine($"WriteMem 0x{address:X8} 0x{value:X8}"); };
                mips.Memory.OnRead = (address, value) => { Console.WriteLine($"ReadMem 0x{address:X8} 0x{value:X8}"); };
                mips.Registers.OnWrite = (address, value) => { Console.WriteLine($"WriteReg 0x{address:X8} 0x{value:X8}"); };
                mips.Registers.OnRead = (address, value) => { Console.WriteLine($"ReadReg 0x{address:X8} 0x{value:X8}"); };
            }
            Stopwatch sw = new();
            sw.Start();
            mips.Run();
            sw.Stop();

            mips.Registers.OnRead = null;

            Console.WriteLine("--- Result ---");
            Console.WriteLine(String.Format("{0, 9} {1}", "$2", $"0x{mips.Registers.GetValue(2):X8}"));
            Console.WriteLine(String.Format("{0, 9} {1}", "Elasped", $"{sw.ElapsedMilliseconds}ms"));

            if (Stats)
            {
                Console.WriteLine("--- Statistics ---");
                Console.WriteLine(String.Format("{0, 19} {1}", "IType", mips.Stats.IType));
                Console.WriteLine(String.Format("{0, 19} {1}", "RType", mips.Stats.RType));
                Console.WriteLine(String.Format("{0, 19} {1}", "JType", mips.Stats.JType));
                Console.WriteLine(String.Format("{0, 19} {1}", "MemoryWrite", mips.Stats.MemoryWrite));
                Console.WriteLine(String.Format("{0, 19} {1}", "MemoryRead", mips.Stats.MemoryRead));
                Console.WriteLine(String.Format("{0, 19} {1}", "ExecutedInstruction", mips.Stats.ExecutedInstruction));
                Console.WriteLine(String.Format("{0, 19} {1}", "Branch", mips.Stats.Branch));
            }
        }

        public static int ChangeByteOrderInt32(int integer)
        {
            var ret = ((integer & 0xFF) << 24)
                    | ((integer & 0xFF00) << 8)
                    | ((integer >> 8) & 0xFF00)
                    | ((integer >> 24) & 0xFF);
            return ret;
        }
    }
}
