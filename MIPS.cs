using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS
{
    public class MIPS
    {
        public Memory Memory { get; private set; }
        public Register Registers { get; private set; }
        public int ProgramCounter { get; private set; }
        public ControlLogic ControllLogic { get; private set; }
        public ExecutionStatistics Stats { get; private set; }
        private int max_pc;

        public Action? OnBeforeStep { get; set; }
        
        public MIPS(IEnumerable<int> code)
        {
            this.ProgramCounter = 0;
            this.Memory = new Memory(0x400000);
            this.Registers = new Register(0x1000000);
            this.ControllLogic = new ControlLogic();
            this.Stats = ExecutionStatistics.GetInstance();
            this.max_pc = code.Count() * 4;

            // load program
            for (int i = 0; i < code.Count(); i++)
            {
                this.Memory.ForceSetValue(i*4, code.ElementAt(i));
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
            WriteBack(controlLogic, reg, mAcc.val);

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
                instruction = Memory.ForceGetValue(pc);
            }
            catch (Exception)
            {
                Environment.Exit(0);
            }
            //Console.WriteLine($"[{this.ProgramCounter - 4:X2}]0x{instruction:x8} 명령어를 가져왔습니다.");
            return instruction;
        }

        private (ResolveResult result, int input1, int input2) Decode(int instruction)
        {
            // build resolve results
            ResolveResult result = new();
            result.Opcode = (instruction >> 26) & 0x3F;
            result.Rd = (instruction >> 11) & 0x1F;
            result.Rs = (instruction >> 21) & 0x1F;
            result.Rt = (instruction >> 16) & 0x1F;
            result.Funct = instruction & 0x3F;
            result.Shamt = (instruction >> 6) & 0x1F;
            result.Imm = (short)(instruction & 0x0000FFFF);
            result.SignExtImm = (int)result.Imm;
            result.ZeroExtImm = 0x0000ffff & (int)result.Imm;
            result.Address = instruction & 0x3FFFFFF;
            
            // get data from register
            int input1 = Registers.GetValue(result.Rs);
            int input2 = Registers.GetValue(result.Rt);
            return (result, input1, input2);
        }

        private ControlLogic BuildControlLogic(ResolveResult result)
        {
            var ret = new ControlLogic();
            switch (result.Opcode)
            {
                case 0x00: // R type
                    ret.RegWrite = 1;
                    ret.RegDst = 1;
                    ret.ALUOp = 2;
                    if (result.Funct == 0x08) // jr
                    {
                        ret.JR = 1;
                        ret.RegDst = 2;
                        ret.RegWrite = 0;
                    }
                    else if (result.Funct == 0x09) // jalr
                    {
                        ret.JR = 1;
                        ret.RegDst = 1;
                        ret.RegWrite = 1;
                    }
                    Stats.RType += 1;
                    return ret;
                case 0x01: //bgez for gcc tests -> 컴파일 시 왜인지 모르게 bgez를 사용하도록 출력됨.
                    ret.Branch = 2;
                    ret.ALUOp = 10;
                    ret.ALUSrc = 1;
                    break;
                case 0x23: // lw
                    ret.ALUSrc = 1;
                    ret.MemToReg = 1;
                    ret.RegWrite = 1;
                    ret.MemRead = 1;
                    ret.ALUOp = 0;
                    break;
                case 0x2b: // sw
                    ret.ALUSrc = 1;
                    ret.MemWrite = 1;
                    break;
                case 0x04: // beq
                    ret.Branch = 1;
                    ret.ALUOp = 1;
                    break;
                case 0x05: // bne
                    ret.Branch = 2;
                    ret.ALUOp = 1;
                    break;
                case 0x08: // addi
                    ret.RegWrite = 1;
                    ret.ALUSrc = 1;
                    ret.ALUOp = 3;
                    break;
                case 0x09: // addiu
                    ret.RegWrite = 1;
                    ret.ALUSrc = 1;
                    ret.ALUOp = 3;
                    break;
                case 0x02: // j
                    ret.Jump = 1;
                    return ret;
                case 0x03: // jal
                    ret.Jump = 1;
                    ret.RegWrite = 1;
                    ret.RegDst = 2;
                    ret.PcToReg = 1;
                    return ret;
                case 0x0d: // ori
                    ret.ALUSrc = 1;
                    ret.ALUOp = 6;
                    ret.RegWrite = 1;
                    ret.ExtImm = 1;
                    break;
                case 0x0a: // slti
                    ret.ALUSrc = 1;
                    ret.ALUOp = 7;
                    ret.RegWrite = 1;
                    break;
                case 0x0b: // sltiu
                    ret.ALUSrc = 1;
                    ret.ALUOp = 8;
                    ret.RegWrite = 1;
                    break;
                case 0x0c: // andi
                    ret.ALUSrc = 1;
                    ret.ALUOp = 5;
                    ret.RegWrite = 1;
                    ret.ExtImm = 1;
                    break;
                case 0x0f: // lui
                    ret.ALUSrc = 1;
                    ret.ExtImm = 2;
                    ret.RegWrite = 1;
                    ret.ALUOp = 9;
                    break;
                default:
                    Console.WriteLine($"This command is not supported. {result.Opcode:X8}");
                    throw new UnkownInstructionException($"{result.Opcode:X8}");
            }
            Stats.IType += 1;
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
            if (control.Jump == 1)
            {
                if (control.PcToReg == 1) // JAL이면 레지스터에 PC를 저장하여야 하므로...
                {
                    output = this.ProgramCounter + 8;
                }
            }
            return (output, writeValue);
        }

        private (int val, bool isRead) MemoryAccess(ControlLogic control, int address, int writeValue)
        {
            if (control.MemRead == 1)
            {
                int val = Memory.GetValue(address, control);
                return (val, true);
            }
            else if (control.MemWrite == 1)
            {
                Memory.SetValue(address, writeValue, control);
                return (0, false);
            }
            return (address, false);
        }

        private void WriteBack(ControlLogic control, int writeReg, int writeValue)
        {
            Registers.SetValue(writeReg, writeValue, control);
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
                Stats.JType += 1;
                return;
            }

            // Nothing
            this.ProgramCounter += 4;
        }
    }
}
