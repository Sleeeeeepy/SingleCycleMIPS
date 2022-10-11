namespace SingleCycleMIPS
{
    public struct ResolveResult
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
    }
}
