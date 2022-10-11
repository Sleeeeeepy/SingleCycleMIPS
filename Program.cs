using System;
using System.Diagnostics;
using SingleCycleMIPS.Util;

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
            bool Cache = false;
            CacheArgument? cacheArg = null;
            if (args.Length == 0)
            {
                var domain = AppDomain.CurrentDomain.FriendlyName;
                Console.WriteLine(String.Format("usage: {0} <input-file>", domain));
                Console.WriteLine(String.Format("usage: {0} <input-file> [options]", domain));
                Console.WriteLine("\nOptions:");
                Console.WriteLine("--reverse\tChange binary order.");
                Console.WriteLine("--step\tStep by step execution by pressing any key.");
                Console.WriteLine("--output\tShow accesses to memory and registers.");
                Console.WriteLine("--stat\tShow execution statistics.");
                Console.WriteLine("--cache <cacheType> <cacheSize> <cacheLineSize> <writePolicy> <replacePolicy> [way]\tUse cache.");
                Console.WriteLine("\nArguements:");
                Console.WriteLine("input-file\tInput binary file.");
                Console.WriteLine("cacheType\tCache type. the following value can be accepted.");
                Console.WriteLine("\tSA\tSet associative cache.");
                Console.WriteLine("\tFA\tFully associative cache. (unstable)");
                Console.WriteLine("cacheSize\tSize of cache. it must be power of two.");
                Console.WriteLine("cacheLineSize\tSize of cache line. It must be power of two and less than or equal half of cacheSize.");
                Console.WriteLine("writePolicy\tWrite policy of cache. the following value can be accepted.");
                Console.WriteLine("\tWB\tuse write back.");
                Console.WriteLine("\tWT\tuse write through with allocation");
                Console.WriteLine("\tWT_NO\tuse write through without allocation.");
                Console.WriteLine("replacePolicy\tReplacement policy of cache. the following value can be accepted.");
                Console.WriteLine("\tLRU\tUse least recently used algorithm.");
                Console.WriteLine("\tRAND\tUse random algorithm.");
                Console.WriteLine("\tSCA\tUse second chance algorithm.");
                Console.WriteLine("way\t\tNumber of ways. it must be power of two and less than cacheLineSize.");
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--reverse":
                        ReverseByteOrder = true;
                        break;
                    case "--step":
                        StepByStep = true;
                        break;
                    case "--output":
                        Output = true;
                        break;
                    case "--stat":
                        Stats = true;
                        break;
                    case "--cache":
                        Cache = true;
                        if (i + 6 < args.Length)
                        {
                            cacheArg = new CacheArgument(args[i + 1], args[i + 2], args[i + 3], args[i + 4], args[i + 5], args[i + 6]);
                        }
                        else if (i + 5 < args.Length)
                        {
                            cacheArg = new CacheArgument(args[i + 1], args[i + 2], args[i + 3], args[i + 4], args[i + 5], "1");
                        } 
                        else
                        {
                            Console.Error.WriteLine("fatal error: cache argument");
                            Environment.Exit(-1);
                        }
                        break;
                }
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
                        code.Add(integer.ChangeByteOrder());
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
            if (Cache)
            {
                if (cacheArg == null)
                {
                    Console.Error.WriteLine("fatal error: cache argument");
                    Environment.Exit(-1);
                } 
                else
                {
                    mips.SetCacheArguement(cacheArg);
                }
            }

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
            try
            {
                Stopwatch sw = new();
                sw.Start();
                mips.Run();
                sw.Stop();

                mips.Registers.OnRead = null;
                Console.WriteLine($"Execution result of {args[0]}");
                Console.WriteLine("--- Result ---");
                Console.WriteLine(String.Format("{0, -19} {1}", "$2", $"0x{mips.Registers.GetValue(2):X8}"));
                Console.WriteLine(String.Format("{0, -19} {1:n0}", "ElaspedTime", $"{sw.ElapsedMilliseconds}ms"));

                if (Stats)
                {
                    var stat = mips.Stats;
                    var hitRate = 100 * (double)stat.CacheReadHit / (stat.CacheReadHit + stat.CacheReadCapacityMiss + stat.CacheReadColdMiss + stat.CacheReadConflictMiss);
                    Console.WriteLine("--- Statistics ---");
                    Console.WriteLine(String.Format("{0, -30} {1:n0}", "IType", stat.IType));
                    Console.WriteLine(String.Format("{0, -30} {1:n0}", "RType", stat.RType));
                    Console.WriteLine(String.Format("{0, -30} {1:n0}", "JType", stat.JType));
                    Console.WriteLine(String.Format("{0, -30} {1:n0}", "MemoryWrite", stat.MemoryWrite));
                    Console.WriteLine(String.Format("{0, -30} {1:n0}", "MemoryRead", stat.MemoryRead));
                    Console.WriteLine(String.Format("{0, -30} {1:n0}", "ExecutedInstruction", stat.ExecutedInstruction));
                    Console.WriteLine(String.Format("{0, -30} {1:n0}", "Branch", stat.Branch));
                    if (Cache)
                    {
                        Console.WriteLine("--- Cache Statistics ---");
                        Console.WriteLine(String.Format("{0, -30} {1:n0}", "ReadHit", stat.CacheReadHit));
                        Console.WriteLine(String.Format("{0, -30} {1:n0}", "ReadColdMiss", stat.CacheReadColdMiss));
                        Console.WriteLine(String.Format("{0, -30} {1:n0}", "ReadConflictMiss", stat.CacheReadConflictMiss));
                        Console.WriteLine(String.Format("{0, -30} {1:n0}", "ReadCapacityMiss", stat.CacheReadCapacityMiss));
                        Console.WriteLine(String.Format("{0, -30} {1:n0}", "WriteAllocate", stat.CacheWriteAllocateMiss));
                        Console.WriteLine(String.Format("{0, -30} {1:n0}", "WriteNoAllocate", stat.CacheWriteNoAllocateMiss));
                        Console.WriteLine(String.Format("{0, -30} {1:n0}", "WriteHit", stat.CacheWriteHit));
                        Console.WriteLine(String.Format("{0, -30} {1:n0} Unit", "TotalExcecutionUnit", mips.CumulativeExecutionCycle));
                        Console.WriteLine(String.Format("{0, -30} {1}%", "CacheReadHitRate", hitRate.ToString("N6")));
                        Console.WriteLine("--- Cache configuration ---");
                        Console.WriteLine($"CacheType: {cacheArg.CacheType}");
                        Console.WriteLine($"CacheSize: {cacheArg.CacheSize} byte");
                        Console.WriteLine($"CacheLineSize: {cacheArg.CacheLineSize} byte");
                        Console.WriteLine($"WritePolicy: {cacheArg.WritePolicy}");
                        Console.WriteLine($"ReplacementPolicy: {cacheArg.Replacement}");
                        if (cacheArg.CacheType == "SA")
                        {
                            Console.WriteLine($"Way: {cacheArg.Way} way");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured.");
                Console.WriteLine($"at instruction {mips.ProgramCounter:X8}");
                Console.WriteLine($"at {mips.Stats.ExecutedInstruction} times execution");
                Console.WriteLine(ex);
            }
        }
    }
}
