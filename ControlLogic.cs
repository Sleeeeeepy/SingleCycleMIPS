using SingleCycleMIPS.Exceptions;

namespace SingleCycleMIPS
{
    public class ControlLogic
    {
        public int MemToReg { get; set; } = 0;
        public int PcToReg { get; set; } = 0;
        public int MemWrite { get; set; } = 0;
        public int MemRead { get; set; } = 0;
        public int Branch { get; set; } = 0;
        public int ALUSrc { get; set; } = 0;
        public int ALUOp { get; set; } = 0;
        public int RegDst { get; set; } = 0;
        public int RegWrite { get; set; } = 0;
        //public int ALUControl { get; set; } = 0;
        public int Jump { get; set; } = 0;
        public int JAL { get; set; } = 0;
        public int JR { get; set; } = 0;

        // 0 : SignExt
        // 1 : ZeroExt
        // 2 : NoExt
        public int ExtImm { get; set; } = 0;

        public ControlLogic()
        {

        }

        public ControlLogic(ResolveResult resolveResult)
        {
            switch (resolveResult.Opcode)
            {
                case 0x00: // R type
                    this.RegWrite = 1;
                    this.RegDst = 1;
                    this.ALUOp = 2;
                    if (resolveResult.Funct == 0x08) // jr
                    {
                        this.JR = 1;
                        this.RegDst = 2;
                        this.RegWrite = 0;
                    }
                    else if (resolveResult.Funct == 0x09) // jalr
                    {
                        this.JR = 1;
                        this.RegDst = 1;
                        this.RegWrite = 1;
                    }
                    break;
                case 0x01: //bgez for gcc tests -> 컴파일 시 왜인지 모르게 bgez를 사용하도록 출력됨.
                    this.Branch = 2;
                    this.ALUOp = 10;
                    this.ALUSrc = 1;
                    break;
                case 0x23: // lw
                    this.ALUSrc = 1;
                    this.MemToReg = 1;
                    this.RegWrite = 1;
                    this.MemRead = 1;
                    this.ALUOp = 0;
                    break;
                case 0x2b: // sw
                    this.ALUSrc = 1;
                    this.MemWrite = 1;
                    break;
                case 0x04: // beq
                    this.Branch = 1;
                    this.ALUOp = 1;
                    break;
                case 0x05: // bne
                    this.Branch = 2;
                    this.ALUOp = 1;
                    break;
                case 0x08: // addi
                    this.RegWrite = 1;
                    this.ALUSrc = 1;
                    this.ALUOp = 3;
                    break;
                case 0x09: // addiu
                    this.RegWrite = 1;
                    this.ALUSrc = 1;
                    this.ALUOp = 3;
                    break;
                case 0x02: // j
                    this.Jump = 1;
                    break;
                case 0x03: // jal
                    this.Jump = 1;
                    this.RegWrite = 1;
                    this.RegDst = 2;
                    this.PcToReg = 1;
                    break;   
                case 0x0d: // ori
                    this.ALUSrc = 1;
                    this.ALUOp = 6;
                    this.RegWrite = 1;
                    this.ExtImm = 1;
                    break;
                case 0x0a: // slti
                    this.ALUSrc = 1;
                    this.ALUOp = 7;
                    this.RegWrite = 1;
                    break;
                case 0x0b: // sltiu
                    this.ALUSrc = 1;
                    this.ALUOp = 8;
                    this.RegWrite = 1;
                    break;
                case 0x0c: // andi
                    this.ALUSrc = 1;
                    this.ALUOp = 5;
                    this.RegWrite = 1;
                    this.ExtImm = 1;
                    break;
                case 0x0f: // lui
                    this.ALUSrc = 1;
                    this.ExtImm = 2;
                    this.RegWrite = 1;
                    this.ALUOp = 9;
                    break;
                default:
                    Console.Error.WriteLine($"The following command is not supported. {resolveResult.Opcode:X8}");
                    throw new UnkownInstructionException($"{resolveResult.Opcode:X8}");
            }
        } 
    }
}
