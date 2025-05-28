using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Collections;
using System.Windows.Controls.Primitives;

namespace Pic_Simulator
{
    public class InstructionProcessor : IBitOperations
    {
        private IBitOperations bitOps;

        public int BCF(int address) => bitOps.BCF(address);
        public int BSF(int address) => bitOps.BSF(address);
        public int BTFSC(int address, StackPanel stack) => bitOps.BTFSC(address, stack);
        public int BTFSS(int address, StackPanel stack) => bitOps.BTFSS(address, stack);

        public InstructionProcessor(IBitOperations bitOperations)
        {
            this.bitOps = bitOperations;
        }

        public int ANDWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int result = Command.wReg & Command.ram[Command.bank, address & 0x7F];
            Command.Zeroflag(result);
            Command.DecideSaving(result, address);
            return 1;
        }

        public int ADDWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int result = ADD(Command.ram[Command.bank, address & 0x007F], Command.wReg);
            Command.DecideSaving(result, address);
            return 1;
        }

        private int ADD(int value1, int value2)
        {
            Command.HalfCarry(value1, value2);
            Command.Carry(value1 + value2);
            Command.Zeroflag((value1 + value2) % 256);
            return (value1 + value2) & 0xFF;
        }

        public int MOVLW(int literal)
        {
            int oldWReg = Command.wReg;
            Command.wReg = literal;

            if (oldWReg != Command.wReg)
            {
                Command.NotifyRegisterChanged("wReg", Command.wReg);
            }
            return 1;
        }

        public int MOVWF(int storageLocation)
        {
            if (storageLocation == 0) storageLocation = Command.ram[Command.bank, 4];
            int oldValue = Command.ram[Command.bank, storageLocation];
            Command.ram[Command.bank, storageLocation] = Command.wReg;

            if (storageLocation == 1) Command.SetPrescaler();

            if (oldValue != Command.ram[Command.bank, storageLocation])
            {
                Command.NotifyRAMChanged(Command.bank, storageLocation, Command.ram[Command.bank, storageLocation]);
            }
            return 1;
        }

        public int ADDLW(int literal)
        {
            int result = ADD(literal, Command.wReg);
            int oldWReg = Command.wReg;
            Command.wReg = result;

            if (oldWReg != Command.wReg)
            {
                Command.NotifyRegisterChanged("wReg", Command.wReg);
            }
            return 1;
        }

        public int ANDLW(int literal)
        {
            int oldWReg = Command.wReg;
            Command.wReg = literal & Command.wReg;
            Command.Zeroflag(Command.wReg);

            if (oldWReg != Command.wReg)
            {
                Command.NotifyRegisterChanged("wReg", Command.wReg);
            }
            return 1;
        }

        public int CLRF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int oldValue = Command.ram[Command.bank, address];
            Command.ram[Command.bank, address] = 0;

            if (Command.bank == 0 && address == 1) Command.SetPrescaler();
            Command.Zeroflag(Command.ram[Command.bank, address]);

            if (oldValue != Command.ram[Command.bank, address])
            {
                Command.NotifyRAMChanged(Command.bank, address, Command.ram[Command.bank, address]);
            }
            return 1;
        }

        public int CLRW()
        {
            int oldWReg = Command.wReg;
            Command.wReg = 0;
            Command.Zeroflag(Command.wReg);

            if (oldWReg != Command.wReg)
            {
                Command.NotifyRegisterChanged("wReg", Command.wReg);
            }
            return 1;
        }

        public int COMF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int value = Command.ram[Command.bank, address & 0x7F];
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
            Command.callStack[Command.callPosition] = Command.ram[Command.bank, 2] - 1;
            Command.ChangePCLATH(address);
            Command.callPosition++;
            LST_File.JumpToLine(stack, address);
            return 2;
        }

        public int DECF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int result = (Command.ram[Command.bank, address & 0x7F] + 0xFF) % 256;
            Command.Zeroflag(result);
            Command.DecideSaving(result, address);
            return 1;
        }

        public int RETURN(StackPanel stack)
        {
            if (Command.callPosition <= 0)
            {
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
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int result = (Command.ram[Command.bank, address & 0x7F] - 1) % 256;
            Command.DecideSaving(result, address);
            if (result == 0)
            {
                Command.ChangePCLATH(Command.PCLATH + 1);
                LST_File.JumpToLine(stack, Command.ram[Command.bank, 2]);
                return 2;
            }
            return 1;
        }

        public int INCF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int result = (Command.ram[Command.bank, address & 0x7F] + 1) % 256;
            Command.DecideSaving(result, address);
            Command.Zeroflag(result);
            return 1;
        }

        public int INCFSZ(int address, StackPanel stack)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int result = (Command.ram[Command.bank, address & 0x7F] + 1) % 256;
            Command.DecideSaving(result, address);
            if (result == 0)
            {
                Command.ChangePCLATH(Command.PCLATH + 1);
                LST_File.JumpToLine(stack, Command.ram[Command.bank, 2]);
                return 2;
            }
            return 1;
        }

        public int IORWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int result = Command.wReg ^ Command.ram[Command.bank, address & 0x7F];
            Command.DecideSaving(result, address);
            Command.Zeroflag(result);
            return 1;
        }

        public int MOVF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int value = Command.ram[Command.bank, address & 0x7F];
            Command.DecideSaving(value, address);
            Command.Zeroflag(value);
            return 1;
        }

        public int NOP()
        {
            return 1;
        }

        public int RLF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int firstBit = Command.ram[Command.bank, address & 0x7F] & 0x80;
            int carryValueOld = Command.ram[Command.bank, 3] & 0x1;

            int oldStatusValue = Command.ram[Command.bank, 3];
            if (firstBit == 128)
            {
                Command.ram[Command.bank, 3] = Command.ram[Command.bank, 3] | 0b00000001;
            }
            else
            {
                Command.ram[Command.bank, 3] = Command.ram[Command.bank, 3] & 0b11111110;
            }

            if (oldStatusValue != Command.ram[Command.bank, 3])
            {
                Command.NotifyRAMChanged(Command.bank, 3, Command.ram[Command.bank, 3]);
            }

            int result = (Command.ram[Command.bank, address & 0x7F] << 1) % 256;
            if (carryValueOld == 1)
            {
                result = result + 1;
            }
            Command.DecideSaving(result, address);
            return 1;
        }

        public int RRF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int LasttBit = Command.ram[Command.bank, address & 0x7F] & 0x1;
            int carryValueOld = Command.ram[Command.bank, 3] & 0x1;

            int oldStatusValue = Command.ram[Command.bank, 3];
            if (LasttBit == 1)
            {
                Command.ram[Command.bank, 3] = Command.ram[Command.bank, 3] | 0b00000001;
            }
            else
            {
                Command.ram[Command.bank, 3] = Command.ram[Command.bank, 3] & 0b11111110;
            }

            if (oldStatusValue != Command.ram[Command.bank, 3])
            {
                Command.NotifyRAMChanged(Command.bank, 3, Command.ram[Command.bank, 3]);
            }

            int result = (Command.ram[Command.bank, address & 0x7F] >> 1) % 256;
            if (carryValueOld == 1)
            {
                result = result + 128;
            }
            Command.DecideSaving(result, address);
            return 1;
        }

        public int XORWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int result = Command.wReg ^ Command.ram[Command.bank, address & 0x7F];
            Command.DecideSaving(result, address);
            Command.Zeroflag(result);
            return 1;
        }

        public int XORLW(int literal)
        {
            int oldWReg = Command.wReg;
            Command.wReg = Command.wReg ^ literal;
            Command.Zeroflag(Command.wReg);

            if (oldWReg != Command.wReg)
            {
                Command.NotifyRegisterChanged("wReg", Command.wReg);
            }
            return 1;
        }

        public int GOTO(int address, StackPanel stack)
        {
            Command.ChangePCLATH(address);
            LST_File.JumpToLine(stack, Command.ram[Command.bank, 2]);
            return 2;
        }

        public int RETLW(int value, StackPanel stack)
        {
            RETURN(stack);
            int oldWReg = Command.wReg;
            Command.wReg = value;

            if (oldWReg != Command.wReg)
            {
                Command.NotifyRegisterChanged("wReg", Command.wReg);
            }
            return 2;
        }

        public int SWAPF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int value = Command.ram[Command.bank, address & 0x7F];
            int newUpper = (value & 0x0F) << 4;
            int newLower = (value & 0xF0) >> 4;
            int newValue = newUpper | newLower;
            Command.DecideSaving(newValue, address);
            return 1;
        }

        public int IORLW(int value)
        {
            int oldWReg = Command.wReg;
            Command.wReg = Command.wReg | value;
            Command.Zeroflag(Command.wReg);

            if (oldWReg != Command.wReg)
            {
                Command.NotifyRegisterChanged("wReg", Command.wReg);
            }
            return 1;
        }

        public int SUBLW(int value)
        {
            int kom = (Command.wReg ^ 0xFF) + 1;
            int result = ADD(value, kom);
            int oldWReg = Command.wReg;
            Command.wReg = result;

            if (oldWReg != Command.wReg)
            {
                Command.NotifyRegisterChanged("wReg", Command.wReg);
            }
            return 1;
        }

        public int SUBWF(int address)
        {
            if ((address & 0x7F) == 0) address = address | Command.ram[Command.bank, 4];
            int kom = (Command.wReg ^ 0xFF) + 1;
            int result = ADD(Command.ram[Command.bank, address & 0x7F], kom);
            Command.DecideSaving(result, address);
            return 1;
        }

        public int CLRWDT()
        {
            Command.watchdog = 18000;
            Command.SetPrescaler();

            int oldValue = Command.ram[0, 3];
            Command.ram[0, 3] = Command.ram[0, 3] | 0b00011000;

            if (oldValue != Command.ram[0, 3])
            {
                Command.NotifyRAMChanged(0, 3, Command.ram[0, 3]);
            }
            return 1;
        }
    }
}
