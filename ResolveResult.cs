namespace SingleCycleMIPS
{
    public class ResolveResult
    {
        public int Opcode { get; set; }
        public int Rd { get; set; }
        public int Rs { get; set; }
        public int Rt { get; set; }
        public int Funct { get; set; }
        public short Imm { get; set; }
        public int SignExtImm { get; set; }
        public int ZeroExtImm { get; set; }
        public int Shamt { get; set; }
        public int Address { get; set; }

        public ResolveResult(int instruction)
        {
            this.Opcode = (instruction >> 26) & 0x3F;
            this.Rd = (instruction >> 11) & 0x1F;
            this.Rs = (instruction >> 21) & 0x1F;
            this.Rt = (instruction >> 16) & 0x1F;
            this.Funct = instruction & 0x3F;
            this.Shamt = (instruction >> 6) & 0x1F;
            this.Imm = (short)(instruction & 0x0000FFFF);
            this.SignExtImm = (int)this.Imm;
            this.ZeroExtImm = 0x0000ffff & (int)this.Imm;
            this.Address = instruction & 0x3FFFFFF;
        }
    }
}
