using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Pic_Simulator
{
    public interface IBitOperations
    {
        int BCF(int address);
        int BSF(int address);
        int BTFSC(int address, StackPanel stack);
        int BTFSS(int address, StackPanel stack);
    }
}
