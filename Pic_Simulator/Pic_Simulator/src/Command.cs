using Pic_Simulator;
using System.DirectoryServices;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Xps;

public class Command
{
    public static int wReg = 0;
    public static int[,] ram = new int[2, 128];
    public static int bank = 0;
    public static int prescaler;
    public static int watchdog;
    public static int[] callStack = { 0, 0, 0, 0, 0, 0, 0, 0 };
    public static int callPosition = 0;
    private static int setTMR = 0;
    public static int quarzfrequenz = 4000000;
    static int lastEdge = 0;
    static bool prescalerToWatchdog = true;
    static int oldBank = 0;
    static int oldRB0 = 0;
    static int[] oldRBValues = new int[8];
    public static int interruptPos = 0;
    public static bool sleepModus = false;
    public static int PCLATH = 0;
    public static int[] EEPROMStorage = new int[64];
    static bool firstWriteEEPROMMuster = false;

    public static InstructionProcessor GetInstructionProcessor()
    {
        return new InstructionProcessor(ram, bank, wReg);
    }

    public static void setQuarzfrequenz(int newQuarzfrezuenz)
    {
        quarzfrequenz = newQuarzfrezuenz; 
    }

    internal static void DecideSaving(int value, int address = -1)
    {
        if ((address & 0x0080) == 0x0080)
        {
            if (address == -1) return;
            if (bank == 0 && address == 1) SetPrescaler();
            if (address == 2)
            {
                ChangePCLATH(PCLATH + value);
                return;
            }
            ram[bank, address & 0x7F] = value;
        }
        else
        {
            wReg = value;
        }
    }

    //Methods for setting the falgs in the Status register
    internal static void Zeroflag(int value)
    {
        if (value == 0)
        {
            ram[bank, 3] = ram[bank, 3] | 0b00000100; // Zeroflag
        }
        else
        {
            ram[bank, 3] = ram[bank, 3] & 0b11111011; //Zeroflag
        }
    }

    public static void Carry(int value)
    {
        if (value > 256)
        {
            ram[bank, 3] = ram[bank, 3] | 0b00000001; // Carryflag
        }
        else
        {
            ram[bank, 3] = ram[bank, 3] & 0b11111110; // Carryflag
        }
    }

    public static void HalfCarry(int value1, int value2)
    {
        if (value1 == 256) value1 = 0xF;
        else value1 = value1 & 0xF;
        if (value2 == 256) value2 = 0xF;
        value2 = value2 & 0xF;
        if (value1 + value2 > 15) // wann wird der gesetzt?
        {
            ram[bank, 3] = ram[bank, 3] | 0b00000010; //Half Carryflag
        }
        else
        {
            ram[bank, 3] = ram[bank, 3] & 0b11111101; //Half Carryflag
        }
    }
    public static void ChangePCLATH(int value)
    {
        PCLATH = value;
        ram[bank, 2] = PCLATH & 0xFF;
    }

    public static int GetSelectedBit(int value, int pos)
    {
        int bit = 1;
        while (pos != 0)
        {
            bit = bit << 1;
            pos--;
        }
        if ((value & bit) == bit) return 1;
        else return 0;
    }
    

    public static int SetSelectedBit(int value, int pos, int bit)
    {
        int rotatedBit;
        if(bit == 0)
        {
            rotatedBit = 0b01111111;
            pos = 7 - pos;
            rotatedBit = rotatedBit >> pos;
            rotatedBit = (rotatedBit + 1) ^ 0xFF; 
            return value & rotatedBit;
        }
        rotatedBit = 0b00000001;
        rotatedBit = rotatedBit << pos;
        return (value | rotatedBit);
    }

    public static void SLEEP()
    {
        ram[bank,3] = SetSelectedBit(ram[bank, 3], 3, 0);
        ram[bank,3] = SetSelectedBit(ram[bank, 3], 4, 1);
        SetPrescaler();
        watchdog = 0;
        sleepModus = true;
    }
    public static void WakeUp()
    {
        ram[bank, 3] = SetSelectedBit(ram[bank, 3], 3, 1);
        ram[bank, 3] = SetSelectedBit(ram[bank, 3], 4, 0);
        sleepModus = false;
    }
    public static void Timer0(StackPanel stack, int steps)
    {
        if (GetSelectedBit(ram[1, 1], 5) == 0)
        {
            if (prescalerToWatchdog)
            {
                ram[0, 1] += steps;
            }
            else
            {
                setTMR += steps;
                if (setTMR >= prescaler)
                {
                    ram[0, 1] += 1;
                    setTMR = setTMR % prescaler;
                }
            }
        }
        else
        {
            //check if option bit 4 is 0 or 1
            if (GetSelectedBit(ram[1, 1], 4) == 0)
            {
                //if option bit 4 is zero reacting on rising edge
                if (GetSelectedBit(ram[0, 5], 4) == 1 && lastEdge == 0)
                {
                    setTMR += 1;
                    if (setTMR >= prescaler)
                    {
                        ram[0, 1] += 1;
                        setTMR = setTMR % prescaler;
                    }
                    lastEdge = 1;
                }
                else if (GetSelectedBit(ram[0, 5], 4) == 0 && lastEdge == 1) lastEdge = 0;
            }
            else
            {
                //if option bit 4 is one reacting on falling edge
                if (GetSelectedBit(ram[0, 5], 4) == 0 && lastEdge == 1)
                {
                    setTMR += 1;
                    if (setTMR >= prescaler)
                    {
                        ram[0, 1] += 1;
                        setTMR = setTMR % prescaler;
                    }
                    lastEdge = 0;
                }
                else if (GetSelectedBit(ram[0, 5], 4) == 1 && lastEdge == 0) lastEdge = 1;
            }
        }
        Timer0Interrupt(stack);
    }

    public static void SetPrescaler()
    {
        if (GetSelectedBit(ram[1, 1], 3) == 1)
        {
            prescaler = (int) Math.Pow(2,ram[1, 1] & 0x7);
        }
        else
        {
            int value = (ram[1, 1] & 0x7);
            prescaler = (int)Math.Pow(2, (ram[1, 1] & 0x7)) *2;
        }
        setTMR = 0;
        PSA();
    }
    public static void ResetTimer0()
    {
        //Timer
        if (GetSelectedBit(ram[1, 1], 5) == 0)
        {
            ram[0, 1] = 0;
        }
        SetPrescaler();
    }

    public static void PSA()
    {
        if (GetSelectedBit(ram[1,1],3) == 0)
        {
            prescalerToWatchdog = false;
        }
        else
        {
            prescalerToWatchdog = true;
        }
    }
    public static void Watchdog(StackPanel stack, int deltaT)
    {
        deltaT = deltaT * 4000000 / quarzfrequenz;
        if (watchdog + deltaT >= 18000)
        {
            if(prescalerToWatchdog)
            {
                if (prescaler != 0)
                {
                    prescaler--;
                    watchdog = watchdog - 18000;
                }
                else
                {
                    if (sleepModus)
                    {
                        SetSelectedBit(ram[0, 3],4, 0);
                        watchdog = 0;
                        return;
                    }
                    MessageBox.Show("Some text", "Watchdog", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetController(stack);
                }
            }
            else
            {
                if(sleepModus)
                {
                    SetSelectedBit(ram[0, 3], 4, 0);
                    watchdog = 0;
                    return;
                }
                MessageBox.Show("Some text", "Watchdog oo", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetController(stack);
            }

        }
        watchdog += deltaT;
    }

    public static void Interrupts(StackPanel stack)
    {
        RB0Interrupt(stack);
        RB4RB7Interrupt(stack);
    }
    public static void Timer0Interrupt(StackPanel stack)
    {
        if (ram[0,1] >= 256)
        {
            ram[0, 1] = 0;
            ram[0, 11] = ram[0, 11] | 0b00000100;
            if (GetSelectedBit(ram[0, 11], 2) == 1 && GetSelectedBit(ram[0, 11], 5) == 1 && GetSelectedBit(ram[0, 11], 7) == 1)
            {
                interruptPos = ram[bank, 2] - 1;
                LST_File.JumpToLine(stack, 4);
                WakeUp();
            }
        }
    }
    public static void RB0Interrupt(StackPanel stack)
    {
        bool flanke = false;
        if (GetSelectedBit(ram[1, 1], 6) == 1 && oldRB0 == 0 && GetSelectedBit(ram[bank, 6], 0) == 1) flanke = true;
        if (GetSelectedBit(ram[1, 1], 6) == 0 && oldRB0 == 1 && GetSelectedBit(ram[bank, 6], 0) == 0) flanke = true;
        if (oldRB0 == 0 && GetSelectedBit(ram[bank, 6], 0) == 1) oldRB0 = 1;
        if (oldRB0 == 1 && GetSelectedBit(ram[bank, 6], 0) == 0) oldRB0 = 0;
        if (flanke) ram[bank, 11] = SetSelectedBit(ram[bank, 11], 1, 1);
        if (flanke && GetSelectedBit(ram[0, 11], 1) == 1 && GetSelectedBit(ram[0, 11], 4) == 1 && GetSelectedBit(ram[0, 11], 7) == 1)
        {
            interruptPos = ram[bank, 2] - 1;
            LST_File.JumpToLine(stack, 4);
            WakeUp();
        }
    }

    public static void EEPROM()
    {
        if (GetSelectedBit(ram[1,8],0) == 1)
        {
            ReadEEPROMValue();
        }
        if(GetSelectedBit(ram[1, 8], 1) == 1 && GetSelectedBit(ram[1, 8], 2) == 1 && GetSelectedBit(ram[bank, 11], 7) == 0)
        {
            EEPROMStorage[ram[0, 9]] = ram[0, 8];
            ram[1, 8] = SetSelectedBit(ram[1, 8], 4, 1);
        }
    }

    public static void ReadEEPROMValue()
    {
        ram[0, 8] = EEPROMStorage[ram[0, 9]];
        ram[1, 8] = SetSelectedBit(ram[1, 8], 0, 0);
    }
    public static void WriteEEPROMValue()
    {
        EEPROMStorage[ram[0, 9]] = ram[0, 8];
        ram[1, 8] = SetSelectedBit(ram[1, 8], 4, 1);
    }
    public static void CheckWriteEEPROM()
    {
        if (ram[1, 9] == 0x55) firstWriteEEPROMMuster = true;
        if (GetSelectedBit(ram[1, 8], 1) == 1 && GetSelectedBit(ram[1, 8], 2) == 1 && GetSelectedBit(ram[bank, 11], 7) == 0)
        {
            if (firstWriteEEPROMMuster && ram[1, 9] == 0xAA)
            {
                WriteEEPROMValue();
                firstWriteEEPROMMuster = false;
                ram[1,8] = SetSelectedBit(ram[1, 8], 1, 0);
                ram[1,8] = SetSelectedBit(ram[1, 8], 4, 1);
            }
        }
    }
    public static void RB4RB7Interrupt(StackPanel stack)
    {
        bool isInterrupt = false;
        for (int i = 0; i < 8; i++)
        {
            if (i < 4) continue;
            if (oldRBValues[i] == GetSelectedBit(ram[0, 6], i)) continue;
            if (GetSelectedBit(ram[1, 6], i) == 1)
            {
                isInterrupt = true;
                oldRBValues[i] = GetSelectedBit(ram[0, 6], i);
            }
        }
        if(isInterrupt) ram[bank, 11] = SetSelectedBit(ram[bank, 11], 0, 1);
        if (isInterrupt && GetSelectedBit(ram[0, 11], 0) == 1 && GetSelectedBit(ram[0, 11], 3) == 1 && GetSelectedBit(ram[0, 11], 7) == 1)
        {
            interruptPos = ram[bank, 2] - 1;
            LST_File.JumpToLine(stack, 4);
            WakeUp();
        }
    }
    public static void ResetController(StackPanel stack)
    {
        //todo change to reset 0b1111111;
        //ram[1, 1] = 0b11111111;
        ram[1, 1] = 0b11111111;
        ram[0,11] = 0b00100000;
        ram[0, 3] = 0b00011000;
        ram[1, 5] = 0b11111111;
        ram[1, 6] = 0b11111111;
        SetPrescaler();
        if(stack.Children.Count != 0) LST_File.JumpToLine(stack, 0);
    }

    public static void Mirroring()
    {
        if((oldBank == 0 && bank == 0) || (oldBank == 0 && bank == 1))
        {
            ram[1, 2] = ram[0, 2];
            ram[1, 3] = ram[0, 3];
            ram[1, 4] = ram[0, 4];
            ram[1, 10] = ram[0, 10];
            ram[1, 11] = ram[0, 11];
        }
        else
        {
            ram[0, 2] = ram[1, 2];
            ram[0, 3] = ram[1, 3];
            ram[0, 4] = ram[1, 4];
            ram[0, 10] = ram[1, 10];
            ram[0, 11] = ram[1, 11];
        }

        if (oldBank == 0 && bank == 1) oldBank = 1;
        if (oldBank == 1 && bank == 0) oldBank = 0;

    }
    //Set Values in ram that are not 0 at the beginning
    public static void startUpRam()
    {
        ram[1, 5] = 0b11111111;
        ram[1, 6] = 0b11111111;
    }
}