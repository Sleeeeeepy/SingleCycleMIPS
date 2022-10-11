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
    }
}
