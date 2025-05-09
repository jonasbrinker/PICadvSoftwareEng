using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Pic_Simulator
{
    public class InstructionProcessor
    {
        private int[,] ram;
        private int bank;
        private int wReg;

        public InstructionProcessor(int[,] ram, int bank, int wReg)
        {
            this.ram = ram;
            this.bank = bank;
            this.wReg = wReg;
        }

        public int ANDWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int result = wReg & ram[bank, address & 0x7F];
            Command.Zeroflag(result);
            Command.DecideSaving(result, address);
            return 1;
        }

        public int ADDWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int result = ADD(ram[bank, address & 0x007F], wReg);
            Command.DecideSaving(result, address);
            return 1;
        }
        private int ADD(int value1, int value2)
        {
            Command.HalfCarry(value1, value2);
            Command.Carry(value1 + value2);
            Command.Zeroflag((value1 + value2) % 256);
            return (value1 + value2) & 0xFF; // Wird carry immer aktiv auf 0 gesetzt?
        }
        public int MOVLW(int literal)
        {
            wReg = literal;
            return 1;
        }

        public int MOVWF(int storageLocation)
        {
            if (storageLocation == 0) storageLocation = ram[bank, 4];
            ram[bank, storageLocation] = wReg;
            if (storageLocation == 1) Command.SetPrescaler();
            return 1;
        }
        public int ADDLW(int literal)
        {
            int result = ADD(literal, wReg);
            wReg = result;
            return 1;
        }

        public int ANDLW(int literal)
        {
            wReg = literal & wReg;
            Command.Zeroflag(wReg);
            return 1;
        }

        public int CLRF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            ram[bank, address] = 0;
            if (bank == 0 && address == 1) Command.SetPrescaler();
            Command.Zeroflag(ram[bank, address]);
            return 1;
        }

        public int CLRW()
        {
            wReg = 0;
            Command.Zeroflag(wReg);
            return 1;
        }
        public int COMF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int value = ram[bank, address & 0x7F];
            int kom = value ^ 0xFF;
            Command.Zeroflag(kom);
            Command.DecideSaving(kom, address);
            return 1;
        }

        public int CALL(int address, StackPanel stack)
        {
            if (Command.callPosition == 8)
            {
                MessageBox.Show("Some text", "Stack overflow", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
            Command.callStack[Command.callPosition] = ram[bank, 2] - 1;
            Command.ChangePCLATH(address);
            Command.callPosition++;
            LST_File.JumpToLine(stack, address);
            return 2;
        }

        public int DECF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int result = (ram[bank, address & 0x7F] + 0xFF) % 256;
            Command.Zeroflag(result);//SUB(ram[bank, address & 0x7F],1);
            Command.DecideSaving(result, address);
            return 1;
        }

        public int RETURN(StackPanel stack)
        {
            if (Command.callPosition <= 0)
            {
                //LST_File.pos++;
                MessageBox.Show("Some text", "Stack Underflow", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
            int address = Command.callStack[Command.callPosition - 1];
            Command.callStack[Command.callPosition - 1] = -1;
            Command.callPosition--;
            LST_File.JumpToLine(stack, address + 1);
            return 2;
        }
        public int RETFIE(StackPanel stack)
        {
            int address = Command.interruptPos;
            LST_File.JumpToLine(stack, address + 1);
            return 2;
        }

        public int DECFSZ(int address, StackPanel stack)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int result = (ram[bank, address & 0x7F] - 1) % 256;
            Command.DecideSaving(result, address);
            if (result == 0)
            {
                Command.ChangePCLATH(Command.PCLATH + 1);
                LST_File.JumpToLine(stack, ram[bank, 2]);
                return 2;
            }
            return 1;
        }
        public int INCF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int result = (ram[bank, address & 0x7F] + 1) % 256;
            Command.DecideSaving(result, address);
            Command.Zeroflag(result);
            return 1;
        }
        public int INCFSZ(int address, StackPanel stack)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int result = (ram[bank, address & 0x7F] + 1) % 256;
            Command.DecideSaving(result, address);
            if (result == 0)
            {
                Command.ChangePCLATH(Command.PCLATH + 1);
                LST_File.JumpToLine(stack, ram[bank, 2]);
                return 2;
            }
            return 1;
        }

        public int IORWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int result = wReg ^ ram[bank, address & 0x7F];
            Command.DecideSaving(result, address);
            Command.Zeroflag(result);
            return 1;
        }

        public int MOVF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int value = ram[bank, address & 0x7F];
            Command.DecideSaving(value, address);
            Command.Zeroflag(value);
            return 1;
        }

        public int NOP()
        {
            //Hier wird nichts ausgeführt
            return 1;
        }
        public int RLF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int firstBit = ram[bank, address & 0x7F] & 0x80;
            int carryValueOld = ram[bank, 3] & 0x1;
            if (firstBit == 128)
            {
                ram[bank, 3] = ram[bank, 3] | 0b00000001;
            }
            else
            {
                ram[bank, 3] = ram[bank, 3] & 0b11111110;
            }
            int result = (ram[bank, address & 0x7F] << 1) % 256;

            if (carryValueOld == 1)
            {
                result = result + 1;
            }
            Command.DecideSaving(result, address);
            return 1;
        }

        public int RRF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int LasttBit = ram[bank, address & 0x7F] & 0x1;
            int carryValueOld = ram[bank, 3] & 0x1;
            if (LasttBit == 1)
            {
                ram[bank, 3] = ram[bank, 3] | 0b00000001;
            }
            else
            {
                ram[bank, 3] = ram[bank, 3] & 0b11111110;
            }
            int result = (ram[bank, address & 0x7F] >> 1) % 256;

            if (carryValueOld == 1)
            {
                result = result + 128;
            }
            Command.DecideSaving(result, address);
            return 1;
        }

        public int XORWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int result = wReg ^ ram[bank, address & 0x7F];
            Command.DecideSaving(result, address);
            Command.Zeroflag(result);
            return 1;
        }

        public int XORLW(int literal)
        {
            wReg = wReg ^ literal;
            Command.Zeroflag(wReg);
            return 1;
        }

        public int GOTO(int address, StackPanel stack)
        {
            Command.ChangePCLATH(address);
            LST_File.JumpToLine(stack, ram[bank, 2]);
            return 2;
        }

        public int RETLW(int value, StackPanel stack)
        {
            RETURN(stack);
            wReg = value;
            return 2;
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
        public int SWAPF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int value = ram[bank, address & 0x7F];
            int newUpper = (value & 0x0F) << 4;
            int newLower = (value & 0xF0) >> 4;
            int newValue = newUpper | newLower;
            Command.DecideSaving(newValue, address);
            return 1;
        }
        public int IORLW(int value)
        {
            wReg = wReg | value;
            Command.Zeroflag(wReg);
            return 1;
        }
        public int SUBLW(int value)
        {
            int kom = (wReg ^ 0xFF) + 1;
            int result = ADD(value, kom);
            //kom = (result ^ 0xFF) + 1;
            wReg = result;
            return 1;
        }
        public int SUBWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | ram[bank, 4];
            int kom = (wReg ^ 0xFF) + 1;
            int result = ADD(ram[bank, address & 0x7F], kom);
            //kom = (result ^ 0xFF) + 1;
            Command.DecideSaving(result, address);
            return 1;
        }

        public int CLRWDT()
        {
            Command.watchdog = 18000;
            Command.SetPrescaler();
            ram[0, 3] = ram[0, 3] | 0b00011000;
            return 1;
        }
    }
}
