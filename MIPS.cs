using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SingleCycleMIPS.Exceptions;
using SingleCycleMIPS.Cache;
using SingleCycleMIPS.Cache.Replacement;
using SingleCycleMIPS.Cache.Write;

namespace SingleCycleMIPS
{
    public class MIPS
    {
        public Memory Memory { get; private set; }
        public IMemoryComponent MemoryHierarchy { get; private set; }
        public Register Registers { get; private set; }
        public int ProgramCounter { get; private set; }
        public ControlLogic ControllLogic { get; private set; }
        public ExecutionStatistics Stats { get; private set; }
        private readonly int max_pc;
        private const int EstimatedMemoryAccessCycle = 1000;
        private const int EstimatedCacheAccessCycle = 1;
        public long CumulativeExecutionCycle { get; private set; } = 0;
        private CacheFactory.WritePolicyType writePolicyType;
        public Action? OnBeforeStep { get; set; }
        
        public MIPS(IEnumerable<int> code)
        {
            this.ProgramCounter = 0;
            this.Memory = new Memory(0x400000);
            this.Registers = new Register(0x1000000);
            this.ControllLogic = new ControlLogic();
            this.Stats = ExecutionStatistics.GetInstance();
            this.max_pc = code.Count() * 4;
            this.MemoryHierarchy = Memory;
           
            // load program
            for (int i = 0; i < code.Count(); i++)
            {
                this.Memory.ForceSetValue(i*4, code.ElementAt(i));
            }
        }
        
        public void SetCacheArguement(CacheArgument arg)
        {
            writePolicyType = arg.WritePolicy;
            if (arg.CacheType == "SA")
            {
                this.MemoryHierarchy = CacheFactory.GetSetAssociativeCacheInstance(arg.CacheSize, arg.CacheLineSize, arg.Way, Memory, arg.WritePolicy, arg.Replacement);
            }
            else
            {
                this.MemoryHierarchy = CacheFactory.GetFullyAssociativeCacheInstance(arg.CacheSize, arg.CacheLineSize, Memory, arg.WritePolicy, arg.Replacement);
            }
        }

        public void Run()
        {
            while (this.ProgramCounter != -1)
            {
                if (this.ProgramCounter == max_pc)
                {
                    break;
                }
                Step();
                OnBeforeStep?.Invoke();
                CumulativeExecutionCycle++;
            }
        }

        // execute one instruction.
        public void Step()
        {
            // FETCH
            var inst = Fetch(this.ProgramCounter);

            // DECODE
            var resolve = Decode(inst);
            var controlLogic = BuildControlLogic(resolve.result);
            var reg = DecideWriteReg(controlLogic, resolve.result);

            // EXECUTE
            var exec = Execute(controlLogic, resolve.result, resolve.input1, resolve.input2);

            // MEMORY ACCESS
            var mAcc = MemoryAccess(controlLogic, exec.output, exec.writeValue);

            // WRITE BACK
            int valueToUpdate = DecideValueToReg(controlLogic, mAcc, exec.output);
            WriteBack(controlLogic, reg, valueToUpdate);

            // UPDATE PC
            UpdatePC(controlLogic, resolve.result, resolve.input1, exec.output);
        }

        private int DecideWriteReg(ControlLogic control, ResolveResult resolve)
        {
            int register = -1;

            if (control.RegDst == 0) // I Type
            {
                register = resolve.Rt;
            }
            else if (control.RegDst == 1) // R type
            {
                register = resolve.Rd;
            }
            else if (control.RegDst == 2) // JAL
            {
                register = 31;
            }

            return register;
        }

        private int Fetch(int pc)
        {
            int instruction = 0;
            try
            {
                var memResult = MemoryHierarchy.GetValue(pc, out instruction);
                Stats.MemoryRead++;
                RecordMemoryReadResultStat(memResult);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"PC:{this.ProgramCounter}\n{ex}");
                Console.Error.WriteLine($"EX:{this.Stats.ExecutedInstruction}");
                Environment.Exit(-1);
            }
            //Console.WriteLine($"[{this.ProgramCounter - 4:X2}]0x{instruction:x8} 명령어를 가져왔습니다.");
            return instruction;
        }

        private (ResolveResult result, int input1, int input2) Decode(int instruction)
        {
            // build resolve results
            ResolveResult result = new(instruction);

            // get data from register
            int input1 = Registers.GetValue(result.Rs);
            int input2 = Registers.GetValue(result.Rt);
            return (result, input1, input2);
        }

        private ControlLogic BuildControlLogic(ResolveResult result)
        {
            var ret = new ControlLogic(result);
            switch (result.Opcode)
            {
                case 0x00:
                    Stats.RType += 1;
                    break;
                case 0x02:
                case 0x03:
                    Stats.JType += 1;
                    break;
                default:
                    Stats.IType += 1;
                    break;
            }
            
            return ret;
        }

        private (int output, int writeValue) Execute(ControlLogic control, ResolveResult decodeResult, int input1, int input2)
        {
            Stats.ExecutedInstruction += 1;
            int output = 0;
            if (control.ALUSrc == 0)
            {
                output = ALU.Calculate(control, decodeResult, input1, input2);
            }
            else if (control.ALUSrc == 1)
            {
                if (control.ExtImm == 0)
                {
                    output = ALU.Calculate(control, decodeResult, input1, decodeResult.SignExtImm);
                }
                else if (control.ExtImm == 1)
                {
                    output = ALU.Calculate(control, decodeResult, input1, decodeResult.ZeroExtImm);
                }
                else if (control.ExtImm == 2)
                {
                    output = ALU.Calculate(control, decodeResult, input1, decodeResult.Imm);
                }
            }

            int writeValue = input2;
            return (output, writeValue);
        }

        private int MemoryAccess(ControlLogic control, int address, int writeValue)
        {
            if (control.MemRead == 1)
            {
                var result = MemoryHierarchy.GetValue(address, out int val);
                RecordMemoryReadResultStat(result);
                Stats.MemoryRead++;
                return val;
            }
            else if (control.MemWrite == 1)
            {
                var result = MemoryHierarchy.SetValue(address, writeValue);
                RecordMemoryWriteResultStat(result);
                Stats.MemoryWrite++;
            }
            return 0;
        }
        private void RecordMemoryWriteResultStat(CacheWriteMissType cacheWriteMissType)
        {
            switch (cacheWriteMissType)
            {
                case CacheWriteMissType.HIT:
                    if (writePolicyType == CacheFactory.WritePolicyType.WriteThroughNoAllocate)
                    {
                        CumulativeExecutionCycle += EstimatedMemoryAccessCycle;
                    } 
                    else if (writePolicyType == CacheFactory.WritePolicyType.WriteThrough)
                    {
                        CumulativeExecutionCycle += EstimatedMemoryAccessCycle;
                    }
                    else
                    {
                        CumulativeExecutionCycle += EstimatedCacheAccessCycle;
                    }
                    Stats.CacheWriteHit++;
                    break;
                case CacheWriteMissType.NO_ALLOCATE:
                    CumulativeExecutionCycle += EstimatedMemoryAccessCycle;
                    Stats.CacheWriteNoAllocateMiss++;
                    break;
                case CacheWriteMissType.ALLOCATE:
                    CumulativeExecutionCycle += EstimatedMemoryAccessCycle + EstimatedCacheAccessCycle;
                    Stats.CacheWriteAllocateMiss++;
                    break;
                case CacheWriteMissType.NONE:
                default:
                    break;
            }
        }
        private void RecordMemoryReadResultStat(CacheReadMissType cacheReadMissType)
        {
            switch (cacheReadMissType)
            {
                case CacheReadMissType.HIT:
                    CumulativeExecutionCycle += EstimatedCacheAccessCycle;
                    Stats.CacheReadHit++;
                    break;
                case CacheReadMissType.CAPACITY: // fully associative cache
                    CumulativeExecutionCycle += EstimatedMemoryAccessCycle;
                    Stats.CacheReadCapacityMiss++;
                    break;
                case CacheReadMissType.CONFLICT: // set associative cache
                    CumulativeExecutionCycle += EstimatedMemoryAccessCycle;
                    Stats.CacheReadConflictMiss++;
                    break;
                case CacheReadMissType.COLD:
                    CumulativeExecutionCycle += EstimatedMemoryAccessCycle;
                    Stats.CacheReadColdMiss++;
                    break;
                case CacheReadMissType.NONE:
                default:
                    break;
            }
        }

        private int DecideValueToReg(ControlLogic control, int memResult, int execResult)
        {
            // MUST SUCCESS TO BE EXECUTED
            if (control.Jump == 1 && control.PcToReg == 1)
            {
                return this.ProgramCounter + 8;
            }

            if (control.MemToReg == 1)
                return memResult;

            if (control.MemToReg == 0)
                return execResult;

            Console.Error.WriteLine($"Fail to decide value to register.\nPC={this.ProgramCounter}\nJump={control.Jump}\nPcToReg={control.PcToReg}\nMemToReg={control.MemToReg}\n");
            throw new UndefinedBehaviorException("Fail to decide value to register.");
        }

        private void WriteBack(ControlLogic control, int writeReg, int writeValue)
        {
            if (control.RegWrite == 1)
            {
                Registers.SetValue(writeReg, writeValue);
            }
        }

        private void UpdatePC(ControlLogic control, ResolveResult decodeResult, int rsValue, int ALUResult)
        {
            if (control.JR == 1)
            {
                this.ProgramCounter = rsValue;
                return;
            }

            if (control.Branch == 1)
            {
                if (ALUResult == 0)
                {
                    // PC = PC + 4 + branchAddr
                    // branchAddr = Imm * 4
                    var branchAddr = decodeResult.Imm * 4;
                    this.ProgramCounter += branchAddr + 4;
                    Stats.Branch += 1;
                    return;
                }
            }
            else if (control.Branch == 2)
            {
                if (ALUResult != 0)
                {
                    // PC = PC + 4 + branchAddr
                    // branchAddr = Imm * 4
                    var branchAddr = decodeResult.Imm * 4;
                    this.ProgramCounter += branchAddr + 4;
                    Stats.Branch += 1;
                    return;
                }
            }

            if (control.Jump == 1)
            {
                var jumpAddr = (int)(this.ProgramCounter & 0xFC000000) | decodeResult.Address * 4;
                this.ProgramCounter = jumpAddr;
                return;
            }

            // Nothing
            this.ProgramCounter += 4;
        }
    }
}
