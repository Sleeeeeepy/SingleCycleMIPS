using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SingleCycleMIPS.Exceptions;

namespace SingleCycleMIPS
{
    public class ALU
    {
        // 0 add
        // 1 sub
        // 2 and
        // 3 or
        // 4 nor
        // 5 slt
        // 6 shift left logical     input2 is ignored.     
        // 7 shift right logical    input2 is ignored.
        // 8 lui

        private static (int result, bool zero, bool overflow) Calculate(int ALUOp, int shamt, int input1, int input2)
        {
            // zero: 연산 결과가 0이면 set
            // overflow: 오버플로우가 발생하면 set

            bool zero = false;
            bool overflow = false;
            int result = 0;

            switch (ALUOp)
            {
                case 0:
                    try
                    {
                        checked { result = input1 + input2; }
                    } 
                    catch (OverflowException)
                    {
                        overflow = true;
                    }
                    zero = (result == 0);
                    break;
                case 1:
                    try
                    {
                        checked { result = input1 - input2; }
                    } 
                    catch (OverflowException)
                    {
                        overflow = true;
                    }
                    zero = (result == 0);
                    break;
                case 2:
                    result = input1 & input2;
                    break;
                case 3:
                    result = input1 | input2;
                    break;
                case 4:
                    result = ~(input1 | input2);
                    break;
                case 5:
                    result = (input1 < input2) ? 1 : 0;
                    break;
                case 6:
                    result = input2 << shamt;
                    break;
                case 7:
                    result = input2 >> shamt;
                    break;
                case 8:
                    result = input2 << 16;
                    break;
            }
            return (result, zero, overflow);
        }

        private static int CalculateFunct(int funct, int shamt, int input1, int input2)
        {
            (int result, bool zero, bool overflow) cal;
            switch (funct)
            {
                case 0b100000: // add
                    cal = Calculate(0, shamt, input1, input2);
                    if (cal.overflow)
                        throw new OverflowException();
                    return cal.result;
                case 0b100001: // addu
                    return Calculate(0, shamt, input1, input2).result;
                case 0b100100: // and
                    return Calculate(2, shamt, input1, input2).result;
                case 0b001101: // break
                case 0b011010: // div
                case 0b011011: //divu
                    break;
                case 0b001000: // jr
                    return 0; // do nothing
                case 0b001001: // jalr
                    return input1;
                case 0b010000: // mfhi
                case 0b010010: // mflo
                case 0b010001: // mthi
                case 0b010011: // mtlo
                case 0b011000: // mult
                case 0b011001: // multu
                    break;
                case 0b100111: // nor
                    return Calculate(4, shamt, input1, input2).result;
                case 0b100101: // or
                    return Calculate(3, shamt, input1, input2).result;
                case 0b000000: // sll
                    return Calculate(6, shamt, input1, input2).result;
                case 0b000100: // sllv
                case 0b101010: // slt
                    return Calculate(5, shamt, input1, input2).result;
                case 0b101011: // sltu
                    return Calculate(5, shamt, input1, input2).result;
                case 0b000011: // sra
                case 0b000111: // srav
                    break;
                case 0b000010: // srl
                    return Calculate(7, shamt, input1, input2).result;
                case 0b000110: // srlv
                    break;
                case 0b100010: // sub
                    cal = Calculate(1, shamt, input1, input2);
                    if (cal.overflow)
                        throw new OverflowException();
                    return cal.result;
                case 0b100011: // subu
                    return Calculate(1, shamt, input1, input2).result;
                case 0b001100: // syscall
                    break;
                case 0b100110: // xor
                    break;
            }
            throw new UnsupportedFunctException($"{funct}");
        }
        // ALU OP
        // 0 ADD for lw and sw and addiu
        // 1 SUB for beq and subiu
        // 2 FUNCT
        // 3 ADDI
        // 4 SUBI
        // 5 ANDI
        // 6 ORI
        // 7 SLTI
        // 8 SLTIU
        // 9 LUI

        public static int Calculate(ControlLogic control, ResolveResult resolveResult, int input1, int input2)
        {
            int ALUOp = control.ALUOp;
            int shamt = resolveResult.Shamt;
            int funct = resolveResult.Funct;
            (int result, bool zero, bool overflow) cal;

            switch (ALUOp)
            {
                case 0: // add for lw and sw
                    return input1 + input2;
                case 1: // sub for beq
                    return input1 - input2;
                case 2:
                    return CalculateFunct(funct, shamt, input1, input2);
                case 3: // addi
                    cal = Calculate(0, shamt, input1, input2);
                    if (cal.overflow)
                        throw new OverflowException();
                    return cal.result;
                case 4: // subi
                    cal = Calculate(1, shamt, input1, input2);
                    if (cal.overflow)
                        throw new OverflowException();
                    return cal.result;
                case 5: // andi
                    cal = Calculate(2, shamt, input1, input2);
                    return cal.result;
                case 6: // ori
                    cal = Calculate(3, shamt, input1, input2);
                    return cal.result;
                case 7: // slti
                    cal = Calculate(5, shamt, input1, input2);
                    return cal.result;
                case 8: // sltiu
                    cal = Calculate(5, shamt, input1, input2);
                    return cal.result;
                case 9: // lui
                    cal = Calculate(8, shamt, input1, input2);
                    return cal.result;
                case 10:
                    int ret = (input1 >= 0) ? 1 : 0;
                    return ret;
            }
            throw new UnsupportedFunctException($"{control.ALUOp}");
        }
    }
}
