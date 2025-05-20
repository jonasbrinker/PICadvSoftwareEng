using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Pic_Simulator
{
    public class BitOperationHandler : IBitOperations
    {
        private readonly int[,] ram;
        private int bank;

        public BitOperationHandler(int[,] ram, int bank)
        {
            this.ram = ram;
            this.bank = bank;
        }

        public int BCF(int address)
        {
            if ((address & 0x7F) == 0) address = (address & 0xFF80) | ram[bank, 4];
            int bit = (address & 0x380) >> 7;
            int rotated = (0x01 << bit) ^ 0xFF;
            int tmp1 = ram[bank, address & 0x7F];
            ram[bank, address & 0x7F] = ram[bank, address & 0x7F] & rotated;
            int tmp = ram[bank, address & 0x7F];
            if ((ram[bank, 3] & 0x20) == 0x0) bank = 0;
            return 1;
        }

        public int BSF(int address)
        {
            if ((address & 0x7F) == 0) address = (address & 0xFF80) | ram[bank, 4];
            int bit = (address & 0x380) >> 7;
            int rotated = 0x01 << bit;
            ram[bank, address & 0x7F] = ram[bank, address & 0x7F] | rotated;
            int tmp = ram[bank, 0x3] & 0x20;
            if ((ram[bank, 0x3] & 0x20) == 0x20) bank = 1;
            return 1;
        }

        public int BTFSC(int address, StackPanel stack)
        {
            if ((address & 0x7F) == 0) address = (address & 0xFF80) | ram[bank, 4];
            int bit = (address & 0x380) >> 7;
            int rotated = (ram[bank, address & 0x7F] >> bit) & 0x1;
            if (rotated == 1) return 1;
            LST_File.JumpToLine(stack, ram[bank, 2] + 1);
            return 2;
        }

        public int BTFSS(int address, StackPanel stack)
        {
            if ((address & 0x7F) == 0) address = (address & 0xFF80) | ram[bank, 4];
            int bit = (address & 0x380) >> 7;
            int rotated = (ram[bank, address & 0x7F] >> bit) & 0x1;
            if (rotated == 0) return 1;
            LST_File.JumpToLine(stack, ram[bank, 2] + 1);
            return 2;
        }
    }
}
