using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pic_Simulator
{
    // Observer Interface für Simulator-Zustandsänderungen
    public interface ISimulatorObserver
    {
        // Wird aufgerufen, wenn sich RAM-Inhalte ändern
        // bank = RAM-Bank (0 oder 1)
        // address = Speicheradresse
        // newValue = Neuer Wert
        void OnRAMChanged(int bank, int address, int newValue);

        // Wird aufgerufen, wenn sich Register ändern
        // name = Name des Registers
        // newValue = Neuer Wert
        void OnRegisterChanged(string registerName, int newValue);
    }
}
